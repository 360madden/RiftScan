using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftSessionAddonCoordinateMatchService
{
    private const int FloatStrideBytes = sizeof(float);
    private const int Vec3ByteLength = sizeof(float) * 3;
    private const double PlausibleCoordinateAbsMax = 100_000;

    private static readonly AxisOrder[] AxisOrders =
    [
        new("xyz", 0, 1, 2),
        new("xzy", 0, 2, 1),
        new("yxz", 1, 0, 2),
        new("yzx", 1, 2, 0),
        new("zxy", 2, 0, 1),
        new("zyx", 2, 1, 0)
    ];

    public RiftSessionAddonCoordinateMatchResult Match(RiftSessionAddonCoordinateMatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SessionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ObservationPath);
        if (double.IsNaN(options.Tolerance) || double.IsInfinity(options.Tolerance) || options.Tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Tolerance), "Tolerance must be finite and non-negative.");
        }

        if (options.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Top), "Top must be positive.");
        }

        var sessionPath = Path.GetFullPath(options.SessionPath);
        var observationPath = Path.GetFullPath(options.ObservationPath);
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before addon-coordinate matching: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(sessionPath, "manifest.json");
        var snapshots = ReadSnapshotIndex(sessionPath);
        var regionFilters = options.RegionBaseAddresses
            .Distinct()
            .Order()
            .ToArray();
        var regionFilterSet = regionFilters.ToHashSet();
        var warnings = new List<string> { "addon_coordinate_matches_are_validation_evidence_not_final_truth" };
        var diagnostics = new List<string>
        {
            "session_verified_before_addon_coordinate_match",
            "offline_snapshot_scan_only",
            $"float_stride_bytes={FloatStrideBytes}",
            $"axis_order_count={AxisOrders.Length}"
        };

        var allObservations = ReadObservations(observationPath).ToArray();
        DateTimeOffset? latestObservationUtc = allObservations.Length == 0 ? null : allObservations.Max(observation => observation.FileLastWriteUtc);
        var observations = NormalizeObservationIds(allObservations)
            .Where(IsFinitePlausibleObservation)
            .ToArray();
        if (observations.Length != allObservations.Length)
        {
            warnings.Add("invalid_or_implausible_addon_observations_ignored");
        }

        if (options.LatestOnly && latestObservationUtc.HasValue)
        {
            observations = observations
                .Where(observation => observation.FileLastWriteUtc == latestObservationUtc.Value)
                .ToArray();
            diagnostics.Add("latest_only_observation_filter_applied");
        }

        var candidateSnapshots = snapshots
            .Where(snapshot => regionFilterSet.Count == 0 || regionFilterSet.Contains(ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex)))
            .OrderBy(snapshot => snapshot.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(snapshot => snapshot.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (regionFilterSet.Count > 0 && candidateSnapshots.Length == 0)
        {
            warnings.Add("region_base_filter_matched_no_snapshots");
        }

        if (observations.Length == 0)
        {
            warnings.Add("no_valid_addon_coordinate_observations");
        }

        var rawMatches = new List<RiftSessionAddonCoordinateMatch>();
        long bytesScanned = 0;
        foreach (var snapshot in candidateSnapshots)
        {
            var bytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path));
            bytesScanned += bytes.Length;
            ScanSnapshot(snapshot, bytes, observations, options.Tolerance, rawMatches);
        }

        var ranked = RankCandidates(rawMatches).ToArray();
        var candidateCount = ranked.Length;
        var topCandidates = ranked
            .Take(options.Top)
            .Select((candidate, index) => candidate with { CandidateId = $"rift-addon-coordinate-candidate-{index + 1:000000}" })
            .ToArray();
        var candidateIdsByKey = topCandidates.ToDictionary(BuildCandidateKey, candidate => candidate.CandidateId, StringComparer.Ordinal);
        var topMatches = rawMatches
            .Where(match => candidateIdsByKey.ContainsKey(BuildCandidateKey(match)))
            .OrderBy(match => candidateIdsByKey[BuildCandidateKey(match)], StringComparer.Ordinal)
            .ThenBy(match => match.MaxAbsDistance)
            .ThenBy(match => match.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.ObservationId, StringComparer.OrdinalIgnoreCase)
            .Take(options.Top)
            .Select((match, index) => match with
            {
                MatchId = $"rift-addon-coordinate-match-{index + 1:000000}",
                CandidateId = candidateIdsByKey[BuildCandidateKey(match)]
            })
            .ToArray();

        if (candidateCount > topCandidates.Length)
        {
            warnings.Add("candidate_output_truncated_by_top_limit");
        }

        if (rawMatches.Count > topMatches.Length)
        {
            warnings.Add("match_output_truncated_by_top_limit");
        }

        if (rawMatches.Count == 0)
        {
            warnings.Add("no_snapshot_vec3_matches_addon_coordinates_within_tolerance");
        }

        return new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            ObservationPath = observationPath,
            AnalyzerSources = ["manifest.json", "snapshots/index.jsonl", "snapshots/*.bin", observationPath],
            Tolerance = options.Tolerance,
            TopLimit = options.Top,
            LatestOnly = options.LatestOnly,
            RegionBaseFilters = regionFilters.Select(FormatHex).ToArray(),
            LatestObservationUtc = latestObservationUtc,
            ObservationCount = allObservations.Length,
            ObservationsUsed = observations.Length,
            SnapshotCount = candidateSnapshots.Length,
            RegionsScanned = candidateSnapshots.Select(snapshot => snapshot.RegionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            BytesScanned = bytesScanned,
            MatchCount = rawMatches.Count,
            CandidateCount = candidateCount,
            Candidates = topCandidates,
            Matches = topMatches,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    public void WriteMarkdown(RiftSessionAddonCoordinateMatchResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, BuildMarkdown(result));
    }

    private static void ScanSnapshot(
        SnapshotIndexEntry snapshot,
        byte[] bytes,
        IReadOnlyList<RiftAddonCoordinateObservation> observations,
        double tolerance,
        ICollection<RiftSessionAddonCoordinateMatch> matches)
    {
        if (observations.Count == 0 || bytes.Length < Vec3ByteLength)
        {
            return;
        }

        var baseAddress = ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex);
        for (var offset = 0; offset <= bytes.Length - Vec3ByteLength; offset += FloatStrideBytes)
        {
            var raw = new[]
            {
                ReadSingle(bytes, offset),
                ReadSingle(bytes, offset + FloatStrideBytes),
                ReadSingle(bytes, offset + FloatStrideBytes + FloatStrideBytes)
            };
            if (!raw.All(value => IsFinitePlausibleCoordinate(value)))
            {
                continue;
            }

            foreach (var axisOrder in AxisOrders)
            {
                var memory = new Vec3(raw[axisOrder.XIndex], raw[axisOrder.YIndex], raw[axisOrder.ZIndex]);
                foreach (var observation in observations)
                {
                    var distance = MaxAbsDistance(memory, observation);
                    if (distance > tolerance)
                    {
                        continue;
                    }

                    var offsetHex = FormatHex((ulong)offset);
                    var absoluteAddress = CheckedAdd(baseAddress, (ulong)offset);
                    matches.Add(new RiftSessionAddonCoordinateMatch
                    {
                        SnapshotId = snapshot.SnapshotId,
                        SourceRegionId = snapshot.RegionId,
                        SourceBaseAddressHex = FormatHex(baseAddress),
                        SourceOffsetHex = offsetHex,
                        SourceAbsoluteAddressHex = FormatHex(absoluteAddress),
                        AxisOrder = axisOrder.Name,
                        MemoryX = memory.X,
                        MemoryY = memory.Y,
                        MemoryZ = memory.Z,
                        ObservationId = observation.ObservationId,
                        AddonName = observation.AddonName,
                        SourcePattern = observation.SourcePattern,
                        AddonObservedX = observation.CoordX,
                        AddonObservedY = observation.CoordY,
                        AddonObservedZ = observation.CoordZ,
                        ZoneId = observation.ZoneId,
                        MaxAbsDistance = distance,
                        EvidenceSummary = $"snapshot={snapshot.SnapshotId};offset={offsetHex};axis={axisOrder.Name};observation={observation.ObservationId};max_abs_distance={distance:F6}"
                    });
                }
            }
        }
    }

    private static IEnumerable<RiftSessionAddonCoordinateCandidate> RankCandidates(IReadOnlyList<RiftSessionAddonCoordinateMatch> matches)
    {
        var groups = matches.GroupBy(BuildCandidateKey, StringComparer.Ordinal);
        var candidates = new List<RiftSessionAddonCoordinateCandidate>();
        foreach (var group in groups)
        {
            var best = group
                .OrderBy(match => match.MaxAbsDistance)
                .ThenBy(match => match.SnapshotId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(match => match.ObservationId, StringComparer.OrdinalIgnoreCase)
                .First();
            var snapshotIds = group.Select(match => match.SnapshotId).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var observationIds = group.Select(match => match.ObservationId).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var addonSources = group
                .Select(match => $"{match.AddonName}:{match.SourcePattern}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var zoneIds = group
                .Select(match => match.ZoneId)
                .Where(zone => !string.IsNullOrWhiteSpace(zone))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            candidates.Add(new RiftSessionAddonCoordinateCandidate
            {
                SourceRegionId = best.SourceRegionId,
                SourceBaseAddressHex = best.SourceBaseAddressHex,
                SourceOffsetHex = best.SourceOffsetHex,
                SourceAbsoluteAddressHex = best.SourceAbsoluteAddressHex,
                AxisOrder = best.AxisOrder,
                SupportCount = snapshotIds.Length,
                ObservationSupportCount = observationIds.Length,
                BestMaxAbsDistance = best.MaxAbsDistance,
                BestMemoryX = best.MemoryX,
                BestMemoryY = best.MemoryY,
                BestMemoryZ = best.MemoryZ,
                BestAddonX = best.AddonObservedX,
                BestAddonY = best.AddonObservedY,
                BestAddonZ = best.AddonObservedZ,
                SupportingSnapshotIds = snapshotIds,
                SupportingObservationIds = observationIds,
                AddonSources = addonSources,
                ZoneIds = zoneIds,
                EvidenceSummary = $"support_snapshots={snapshotIds.Length};support_observations={observationIds.Length};best_max_abs_distance={best.MaxAbsDistance:F6};source={best.SourceBaseAddressHex}+{best.SourceOffsetHex};axis={best.AxisOrder}"
            });
        }

        return candidates
            .OrderByDescending(candidate => candidate.SupportCount)
            .ThenByDescending(candidate => candidate.ObservationSupportCount)
            .ThenBy(candidate => candidate.BestMaxAbsDistance)
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.SourceBaseAddressHex))
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.SourceOffsetHex))
            .ThenBy(candidate => candidate.AxisOrder, StringComparer.Ordinal);
    }

    private static string BuildMarkdown(RiftSessionAddonCoordinateMatchResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# RiftScan Addon Coordinate Match Report - {result.SessionId}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Session: `{result.SessionPath}`");
        builder.AppendLine($"- Addon observations: `{result.ObservationPath}`");
        builder.AppendLine($"- Observations used: {result.ObservationsUsed}/{result.ObservationCount}");
        builder.AppendLine($"- Snapshots scanned: {result.SnapshotCount}");
        builder.AppendLine($"- Regions scanned: {result.RegionsScanned}");
        builder.AppendLine($"- Bytes scanned: {result.BytesScanned}");
        builder.AppendLine($"- Tolerance: {result.Tolerance.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"- Match count: {result.MatchCount}");
        builder.AppendLine($"- Candidate count: {result.CandidateCount}");
        builder.AppendLine();
        builder.AppendLine("## Top candidates");
        builder.AppendLine();
        builder.AppendLine("| Candidate | Address | Axis | Support | Best distance | Addon sources | Evidence |");
        builder.AppendLine("|---|---:|---|---:|---:|---|---|");
        foreach (var candidate in result.Candidates)
        {
            builder.AppendLine($"| `{Escape(candidate.CandidateId)}` | `{Escape(candidate.SourceBaseAddressHex)}+{Escape(candidate.SourceOffsetHex)}` | `{Escape(candidate.AxisOrder)}` | {candidate.SupportCount} | {candidate.BestMaxAbsDistance:F6} | {Escape(string.Join(", ", candidate.AddonSources))} | {Escape(candidate.EvidenceSummary)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in result.Warnings)
        {
            builder.AppendLine($"- {Escape(warning)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Next recommended capture");
        builder.AppendLine();
        builder.AppendLine("- Re-capture this same region after a small player translation and re-run this matcher to separate live player position from copied/cache vec3 mirrors.");
        return builder.ToString();
    }

    private static IReadOnlyList<RiftAddonCoordinateObservation> NormalizeObservationIds(IReadOnlyList<RiftAddonCoordinateObservation> observations) =>
        observations
            .Select((observation, index) => string.IsNullOrWhiteSpace(observation.ObservationId)
                ? observation with { ObservationId = $"rift-addon-coord-{index + 1:000000}" }
                : observation)
            .ToArray();

    private static IEnumerable<RiftAddonCoordinateObservation> ReadObservations(string observationPath)
    {
        if (!File.Exists(observationPath))
        {
            throw new FileNotFoundException("Addon coordinate observation JSONL file does not exist.", observationPath);
        }

        return File.ReadLines(observationPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RiftAddonCoordinateObservation>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid addon coordinate observation entry in {observationPath}."));
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
            .ToArray();
    }

    private static bool IsFinitePlausibleObservation(RiftAddonCoordinateObservation observation) =>
        IsFinitePlausibleCoordinate(observation.CoordX) &&
        IsFinitePlausibleCoordinate(observation.CoordY) &&
        IsFinitePlausibleCoordinate(observation.CoordZ);

    private static bool IsFinitePlausibleCoordinate(double value) =>
        !double.IsNaN(value) &&
        !double.IsInfinity(value) &&
        Math.Abs(value) <= PlausibleCoordinateAbsMax;

    private static float ReadSingle(byte[] bytes, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, FloatStrideBytes)));

    private static double MaxAbsDistance(Vec3 memory, RiftAddonCoordinateObservation observation) =>
        Math.Max(
            Math.Abs(memory.X - observation.CoordX),
            Math.Max(Math.Abs(memory.Y - observation.CoordY), Math.Abs(memory.Z - observation.CoordZ)));

    private static string BuildCandidateKey(RiftSessionAddonCoordinateCandidate candidate) =>
        string.Join('|', candidate.SourceRegionId, candidate.SourceBaseAddressHex, candidate.SourceOffsetHex, candidate.AxisOrder);

    private static string BuildCandidateKey(RiftSessionAddonCoordinateMatch match) =>
        string.Join('|', match.SourceRegionId, match.SourceBaseAddressHex, match.SourceOffsetHex, match.AxisOrder);

    private static ulong CheckedAdd(ulong baseAddress, ulong offset)
    {
        if (ulong.MaxValue - baseAddress < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Address calculation overflowed.");
        }

        return baseAddress + offset;
    }

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static string FormatHex(ulong value) => $"0x{value:X}";

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");

    private readonly record struct AxisOrder(string Name, int XIndex, int YIndex, int ZIndex);

    private readonly record struct Vec3(double X, double Y, double Z);
}
