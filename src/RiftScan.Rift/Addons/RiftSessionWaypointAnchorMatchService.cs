using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftSessionWaypointAnchorMatchService
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

    public RiftSessionWaypointAnchorMatchResult Match(RiftSessionWaypointAnchorMatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SessionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AnchorPath);
        if (double.IsNaN(options.Tolerance) || double.IsInfinity(options.Tolerance) || options.Tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Tolerance), "Tolerance must be finite and non-negative.");
        }

        if (options.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Top), "Top must be positive.");
        }

        var sessionPath = Path.GetFullPath(options.SessionPath);
        var anchorPath = Path.GetFullPath(options.AnchorPath);
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before waypoint-anchor matching: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(sessionPath, "manifest.json");
        var snapshots = ReadSnapshotIndex(sessionPath);
        var scanResult = JsonSerializer.Deserialize<RiftAddonApiObservationScanResult>(File.ReadAllText(anchorPath), SessionJson.Options)
            ?? throw new InvalidOperationException("Waypoint anchor scan result JSON could not be read.");
        var sourceAnchors = scanResult.WaypointAnchors.Count > 0
            ? scanResult.WaypointAnchors
            : RiftAddonApiObservationService.BuildWaypointAnchors(scanResult.Observations);
        var allAnchors = sourceAnchors
            .Select((anchor, index) => string.IsNullOrWhiteSpace(anchor.AnchorId)
                ? anchor with { AnchorId = $"rift-addon-waypoint-anchor-{index + 1:000000}" }
                : anchor)
            .ToArray();
        var anchors = allAnchors.Where(IsValidAnchor).ToArray();
        var regionFilters = options.RegionBaseAddresses.Distinct().Order().ToArray();
        var regionFilterSet = regionFilters.ToHashSet();
        var warnings = new List<string> { "waypoint_anchor_matches_are_validation_evidence_not_final_truth" };
        var diagnostics = new List<string>
        {
            "session_verified_before_waypoint_anchor_match",
            "offline_snapshot_scan_only",
            $"float_stride_bytes={FloatStrideBytes}",
            $"axis_order_count={AxisOrders.Length}",
            "waypoint_y_is_not_required"
        };

        if (anchors.Length != allAnchors.Length)
        {
            warnings.Add("invalid_or_implausible_waypoint_anchors_ignored");
        }

        if (scanResult.WaypointAnchors.Count == 0 && allAnchors.Length > 0)
        {
            warnings.Add("waypoint_anchors_derived_from_observations_for_legacy_scan_result");
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

        if (anchors.Length == 0)
        {
            warnings.Add("no_valid_waypoint_anchors");
        }

        var rawMatches = new List<RiftSessionWaypointAnchorMatch>();
        long bytesScanned = 0;
        foreach (var snapshot in candidateSnapshots)
        {
            var bytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path));
            bytesScanned += bytes.Length;
            ScanSnapshot(snapshot, bytes, anchors, options.Tolerance, rawMatches);
        }

        var ranked = RankCandidates(rawMatches).ToArray();
        var candidateCount = ranked.Length;
        var topCandidates = ranked
            .Take(options.Top)
            .Select((candidate, index) => candidate with { CandidateId = $"rift-waypoint-anchor-candidate-{index + 1:000000}" })
            .ToArray();
        var candidateIdsByKey = topCandidates.ToDictionary(BuildCandidateKey, candidate => candidate.CandidateId, StringComparer.Ordinal);
        var topMatches = rawMatches
            .Where(match => candidateIdsByKey.ContainsKey(BuildCandidateKey(match)))
            .OrderBy(match => candidateIdsByKey[BuildCandidateKey(match)], StringComparer.Ordinal)
            .ThenBy(match => match.DeltaMaxAbsDistance)
            .ThenBy(match => match.PlayerMaxAbsDistance + match.WaypointMaxAbsDistance)
            .ThenBy(match => match.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.AnchorId, StringComparer.OrdinalIgnoreCase)
            .Take(options.Top)
            .Select((match, index) => match with
            {
                MatchId = $"rift-waypoint-anchor-match-{index + 1:000000}",
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
            warnings.Add("no_snapshot_vec3_pair_matches_waypoint_anchors_within_tolerance");
        }

        return new RiftSessionWaypointAnchorMatchResult
        {
            Success = true,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            AnchorPath = anchorPath,
            AnalyzerSources = ["manifest.json", "snapshots/index.jsonl", "snapshots/*.bin", anchorPath],
            Tolerance = options.Tolerance,
            TopLimit = options.Top,
            RegionBaseFilters = regionFilters.Select(FormatHex).ToArray(),
            AnchorCount = allAnchors.Length,
            AnchorsUsed = anchors.Length,
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

    private static void ScanSnapshot(
        SnapshotIndexEntry snapshot,
        byte[] bytes,
        IReadOnlyList<RiftAddonWaypointAnchor> anchors,
        double tolerance,
        ICollection<RiftSessionWaypointAnchorMatch> matches)
    {
        if (anchors.Count == 0 || bytes.Length < Vec3ByteLength)
        {
            return;
        }

        var baseAddress = ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex);
        var playerHits = new List<WaypointAnchorHit>();
        var waypointHits = new List<WaypointAnchorHit>();
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
                foreach (var anchor in anchors)
                {
                    var playerDistance = PlayerMaxAbsDistance(memory, anchor);
                    if (playerDistance <= tolerance)
                    {
                        playerHits.Add(BuildHit(snapshot, baseAddress, offset, axisOrder.Name, memory, anchor, playerDistance));
                    }

                    var waypointDistance = WaypointMaxAbsDistance(memory, anchor);
                    if (waypointDistance <= tolerance)
                    {
                        waypointHits.Add(BuildHit(snapshot, baseAddress, offset, axisOrder.Name, memory, anchor, waypointDistance));
                    }
                }
            }
        }

        var playerHitsByAnchor = playerHits.GroupBy(hit => hit.Anchor.AnchorId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        foreach (var waypointHit in waypointHits)
        {
            if (!playerHitsByAnchor.TryGetValue(waypointHit.Anchor.AnchorId, out var matchingPlayerHits))
            {
                continue;
            }

            foreach (var playerHit in matchingPlayerHits)
            {
                var memoryDeltaX = waypointHit.X - playerHit.X;
                var memoryDeltaZ = waypointHit.Z - playerHit.Z;
                var deltaDistance = Math.Max(
                    Math.Abs(memoryDeltaX - waypointHit.Anchor.DeltaX),
                    Math.Abs(memoryDeltaZ - waypointHit.Anchor.DeltaZ));
                if (deltaDistance > tolerance)
                {
                    continue;
                }

                matches.Add(BuildMatch(snapshot, playerHit, waypointHit, memoryDeltaX, memoryDeltaZ, deltaDistance));
            }
        }
    }

    private static RiftSessionWaypointAnchorMatch BuildMatch(
        SnapshotIndexEntry snapshot,
        WaypointAnchorHit playerHit,
        WaypointAnchorHit waypointHit,
        double memoryDeltaX,
        double memoryDeltaZ,
        double deltaDistance)
    {
        var anchor = waypointHit.Anchor;
        return new()
        {
            SnapshotId = snapshot.SnapshotId,
            AnchorId = anchor.AnchorId,
            PlayerSourceRegionId = playerHit.SourceRegionId,
            PlayerSourceBaseAddressHex = playerHit.SourceBaseAddressHex,
            PlayerSourceOffsetHex = playerHit.SourceOffsetHex,
            PlayerSourceAbsoluteAddressHex = playerHit.SourceAbsoluteAddressHex,
            PlayerAxisOrder = playerHit.AxisOrder,
            WaypointSourceRegionId = waypointHit.SourceRegionId,
            WaypointSourceBaseAddressHex = waypointHit.SourceBaseAddressHex,
            WaypointSourceOffsetHex = waypointHit.SourceOffsetHex,
            WaypointSourceAbsoluteAddressHex = waypointHit.SourceAbsoluteAddressHex,
            WaypointAxisOrder = waypointHit.AxisOrder,
            MemoryPlayerX = playerHit.X,
            MemoryPlayerY = playerHit.Y,
            MemoryPlayerZ = playerHit.Z,
            MemoryWaypointX = waypointHit.X,
            MemoryWaypointY = waypointHit.Y,
            MemoryWaypointZ = waypointHit.Z,
            AnchorPlayerX = anchor.PlayerX,
            AnchorPlayerY = anchor.PlayerY,
            AnchorPlayerZ = anchor.PlayerZ,
            AnchorWaypointX = anchor.WaypointX,
            AnchorWaypointZ = anchor.WaypointZ,
            MemoryDeltaX = memoryDeltaX,
            MemoryDeltaZ = memoryDeltaZ,
            AnchorDeltaX = anchor.DeltaX,
            AnchorDeltaZ = anchor.DeltaZ,
            PlayerMaxAbsDistance = playerHit.MaxAbsDistance,
            WaypointMaxAbsDistance = waypointHit.MaxAbsDistance,
            DeltaMaxAbsDistance = deltaDistance,
            EvidenceSummary = $"snapshot={snapshot.SnapshotId};anchor={anchor.AnchorId};player_offset={playerHit.SourceOffsetHex};waypoint_offset={waypointHit.SourceOffsetHex};memory_delta=({memoryDeltaX:F6},{memoryDeltaZ:F6});anchor_delta=({anchor.DeltaX:F6},{anchor.DeltaZ:F6});delta_max_abs_distance={deltaDistance:F6}"
        };
    }

    private static IEnumerable<RiftSessionWaypointAnchorCandidate> RankCandidates(IReadOnlyList<RiftSessionWaypointAnchorMatch> matches)
    {
        var groups = matches.GroupBy(BuildCandidateKey, StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var best = group
                .OrderBy(match => match.DeltaMaxAbsDistance)
                .ThenBy(match => match.PlayerMaxAbsDistance + match.WaypointMaxAbsDistance)
                .ThenBy(match => match.SnapshotId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(match => match.AnchorId, StringComparer.OrdinalIgnoreCase)
                .First();
            var snapshotIds = group.Select(match => match.SnapshotId).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var anchorIds = group.Select(match => match.AnchorId).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            yield return new RiftSessionWaypointAnchorCandidate
            {
                PlayerSourceRegionId = best.PlayerSourceRegionId,
                PlayerSourceBaseAddressHex = best.PlayerSourceBaseAddressHex,
                PlayerSourceOffsetHex = best.PlayerSourceOffsetHex,
                PlayerSourceAbsoluteAddressHex = best.PlayerSourceAbsoluteAddressHex,
                PlayerAxisOrder = best.PlayerAxisOrder,
                WaypointSourceRegionId = best.WaypointSourceRegionId,
                WaypointSourceBaseAddressHex = best.WaypointSourceBaseAddressHex,
                WaypointSourceOffsetHex = best.WaypointSourceOffsetHex,
                WaypointSourceAbsoluteAddressHex = best.WaypointSourceAbsoluteAddressHex,
                WaypointAxisOrder = best.WaypointAxisOrder,
                SupportCount = group.Count(),
                AnchorSupportCount = anchorIds.Length,
                BestPlayerMaxAbsDistance = best.PlayerMaxAbsDistance,
                BestWaypointMaxAbsDistance = best.WaypointMaxAbsDistance,
                BestDeltaMaxAbsDistance = best.DeltaMaxAbsDistance,
                BestMemoryDeltaX = best.MemoryDeltaX,
                BestMemoryDeltaZ = best.MemoryDeltaZ,
                BestAnchorDeltaX = best.AnchorDeltaX,
                BestAnchorDeltaZ = best.AnchorDeltaZ,
                SupportingSnapshotIds = snapshotIds,
                SupportingAnchorIds = anchorIds,
                ValidationStatus = group.Count() > 1 || anchorIds.Length > 1
                    ? "waypoint_anchor_supported"
                    : "waypoint_anchor_candidate_single_support",
                EvidenceSummary = $"player={best.PlayerSourceBaseAddressHex}+{best.PlayerSourceOffsetHex};waypoint={best.WaypointSourceBaseAddressHex}+{best.WaypointSourceOffsetHex};support={group.Count()};anchors={anchorIds.Length};best_delta_max_abs_distance={best.DeltaMaxAbsDistance:F6}"
            };
        }
    }

    private static WaypointAnchorHit BuildHit(
        SnapshotIndexEntry snapshot,
        ulong baseAddress,
        int offset,
        string axisOrder,
        Vec3 memory,
        RiftAddonWaypointAnchor anchor,
        double maxAbsDistance)
    {
        var absoluteAddress = CheckedAdd(baseAddress, (ulong)offset);
        return new(
            anchor,
            snapshot.RegionId,
            FormatHex(baseAddress),
            FormatHex((ulong)offset),
            FormatHex(absoluteAddress),
            axisOrder,
            memory.X,
            memory.Y,
            memory.Z,
            maxAbsDistance);
    }

    private static double PlayerMaxAbsDistance(Vec3 memory, RiftAddonWaypointAnchor anchor)
    {
        var distance = Math.Max(Math.Abs(memory.X - anchor.PlayerX), Math.Abs(memory.Z - anchor.PlayerZ));
        if (anchor.PlayerY.HasValue)
        {
            distance = Math.Max(distance, Math.Abs(memory.Y - anchor.PlayerY.Value));
        }

        return distance;
    }

    private static double WaypointMaxAbsDistance(Vec3 memory, RiftAddonWaypointAnchor anchor) =>
        Math.Max(Math.Abs(memory.X - anchor.WaypointX), Math.Abs(memory.Z - anchor.WaypointZ));

    private static bool IsValidAnchor(RiftAddonWaypointAnchor anchor) =>
        IsFinitePlausibleCoordinate(anchor.PlayerX) &&
        (!anchor.PlayerY.HasValue || IsFinitePlausibleCoordinate(anchor.PlayerY.Value)) &&
        IsFinitePlausibleCoordinate(anchor.PlayerZ) &&
        IsFinitePlausibleCoordinate(anchor.WaypointX) &&
        IsFinitePlausibleCoordinate(anchor.WaypointZ) &&
        !string.IsNullOrWhiteSpace(anchor.AnchorId);

    private static bool IsFinitePlausibleCoordinate(double value) =>
        double.IsFinite(value) && Math.Abs(value) <= PlausibleCoordinateAbsMax;

    private static string BuildCandidateKey(RiftSessionWaypointAnchorCandidate candidate) =>
        string.Join('|', candidate.PlayerSourceRegionId, candidate.PlayerSourceOffsetHex, candidate.PlayerAxisOrder, candidate.WaypointSourceRegionId, candidate.WaypointSourceOffsetHex, candidate.WaypointAxisOrder);

    private static string BuildCandidateKey(RiftSessionWaypointAnchorMatch match) =>
        string.Join('|', match.PlayerSourceRegionId, match.PlayerSourceOffsetHex, match.PlayerAxisOrder, match.WaypointSourceRegionId, match.WaypointSourceOffsetHex, match.WaypointAxisOrder);

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = Path.Combine(sessionPath, "snapshots", "index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
            .ToArray();
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Unable to read {relativePath}.");
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static float ReadSingle(byte[] bytes, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, FloatStrideBytes)));

    private static ulong CheckedAdd(ulong left, ulong right)
    {
        checked
        {
            return left + right;
        }
    }

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static string FormatHex(ulong value) => $"0x{value:X}";

    private readonly record struct AxisOrder(string Name, int XIndex, int YIndex, int ZIndex);

    private readonly record struct Vec3(double X, double Y, double Z);

    private sealed record WaypointAnchorHit(
        RiftAddonWaypointAnchor Anchor,
        string SourceRegionId,
        string SourceBaseAddressHex,
        string SourceOffsetHex,
        string SourceAbsoluteAddressHex,
        string AxisOrder,
        double X,
        double Y,
        double Z,
        double MaxAbsDistance);
}
