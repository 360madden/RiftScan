using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftSessionWaypointScalarMatchService
{
    private const int FloatStrideBytes = sizeof(float);
    private const double PlausibleCoordinateAbsMax = 100_000;
    private const string WaypointXAxis = "waypoint_x";
    private const string WaypointZAxis = "waypoint_z";

    public RiftSessionWaypointScalarMatchResult Match(RiftSessionWaypointScalarMatchOptions options)
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

        if (options.MaxScalarHitsPerSnapshotAxis <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxScalarHitsPerSnapshotAxis), "Max scalar hits per snapshot axis must be positive.");
        }

        var sessionPath = Path.GetFullPath(options.SessionPath);
        var anchorPath = Path.GetFullPath(options.AnchorPath);
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before waypoint-scalar matching: {issues}");
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
        var regionFilters = options.RegionBaseAddresses.Distinct().OrderBy(address => address).ToArray();
        var regionFilterSet = regionFilters.ToHashSet();
        var warnings = new List<string> { "waypoint_scalar_matches_are_validation_evidence_not_final_truth" };
        var diagnostics = new List<string>
        {
            "session_verified_before_waypoint_scalar_match",
            "offline_snapshot_scan_only",
            $"float_stride_bytes={FloatStrideBytes}",
            "searches_waypoint_x_and_waypoint_z_independently",
            "pair_candidates_can_have_non_adjacent_x_z_scalars"
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

        var retainedHits = new List<RiftSessionWaypointScalarHit>();
        var scalarHitCount = 0;
        var scalarAxisHitCounts = CreateAxisCountDictionary();
        var retainedScalarAxisHitCounts = CreateAxisCountDictionary();
        var retainedHitTruncation = false;
        long bytesScanned = 0;
        foreach (var snapshot in candidateSnapshots)
        {
            var bytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path));
            bytesScanned += bytes.Length;
            var scan = ScanSnapshot(snapshot, bytes, anchors, options.Tolerance, options.MaxScalarHitsPerSnapshotAxis);
            scalarHitCount += scan.RawHitCount;
            AddAxisCounts(scalarAxisHitCounts, scan.RawAxisHitCounts);
            AddAxisCounts(retainedScalarAxisHitCounts, scan.RetainedAxisHitCounts);
            retainedHitTruncation |= scan.Truncated;
            retainedHits.AddRange(scan.RetainedHits);
        }

        var rankedPairCandidates = RankPairCandidates(retainedHits).ToArray();
        var pairCandidateCount = rankedPairCandidates.Length;
        var topPairCandidates = rankedPairCandidates
            .Take(options.Top)
            .Select((candidate, index) => candidate with { CandidateId = $"rift-waypoint-scalar-pair-candidate-{index + 1:000000}" })
            .ToArray();
        var rankedScalarHits = retainedHits
            .OrderBy(hit => hit.AbsDistance)
            .ThenBy(hit => hit.Axis, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.SourceRegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => ParseUnsignedHexOrDecimal(hit.SourceOffsetHex))
            .Select((hit, index) => hit with { HitId = $"rift-waypoint-scalar-hit-{index + 1:000000}" })
            .ToArray();
        var topScalarHits = rankedScalarHits
            .Take(options.Top)
            .ToArray();
        var scalarHitsOutputPath = WriteJsonLines(options.ScalarHitsOutputPath, rankedScalarHits);

        if (retainedHitTruncation)
        {
            warnings.Add("scalar_hits_retained_for_pairing_truncated_by_snapshot_axis_limit");
        }

        if (scalarHitCount > topScalarHits.Length)
        {
            warnings.Add("scalar_hit_output_truncated_by_top_limit");
        }

        if (pairCandidateCount > topPairCandidates.Length)
        {
            warnings.Add("pair_candidate_output_truncated_by_top_limit");
        }

        if (scalarHitCount == 0)
        {
            warnings.Add("no_snapshot_scalar_matches_waypoint_values_within_tolerance");
        }
        else
        {
            if (scalarAxisHitCounts[WaypointXAxis] == 0)
            {
                warnings.Add("no_waypoint_x_scalar_hits_within_tolerance");
            }

            if (scalarAxisHitCounts[WaypointZAxis] == 0)
            {
                warnings.Add("no_waypoint_z_scalar_hits_within_tolerance");
            }
        }

        if (scalarHitCount > 0 && pairCandidateCount == 0)
        {
            warnings.Add("scalar_hits_found_but_no_x_z_pair_candidates_retained");
        }

        return new RiftSessionWaypointScalarMatchResult
        {
            Success = true,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            AnchorPath = anchorPath,
            AnalyzerSources = ["manifest.json", "snapshots/index.jsonl", "snapshots/*.bin", anchorPath],
            Tolerance = options.Tolerance,
            TopLimit = options.Top,
            MaxScalarHitsPerSnapshotAxis = options.MaxScalarHitsPerSnapshotAxis,
            RegionBaseFilters = regionFilters.Select(FormatHex).ToArray(),
            AnchorCount = allAnchors.Length,
            AnchorsUsed = anchors.Length,
            SnapshotCount = candidateSnapshots.Length,
            RegionsScanned = candidateSnapshots.Select(snapshot => snapshot.RegionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            BytesScanned = bytesScanned,
            ScalarHitCount = scalarHitCount,
            ScalarAxisHitCounts = scalarAxisHitCounts,
            RetainedScalarHitCount = retainedHits.Count,
            RetainedScalarAxisHitCounts = retainedScalarAxisHitCounts,
            PairCandidateCount = pairCandidateCount,
            ScalarHitsOutputPath = scalarHitsOutputPath,
            ScalarHits = topScalarHits,
            PairCandidates = topPairCandidates,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static SnapshotScalarScan ScanSnapshot(
        SnapshotIndexEntry snapshot,
        byte[] bytes,
        IReadOnlyList<RiftAddonWaypointAnchor> anchors,
        double tolerance,
        int maxScalarHitsPerSnapshotAxis)
    {
        if (anchors.Count == 0 || bytes.Length < FloatStrideBytes)
        {
            return new([], 0, CreateAxisCountDictionary(), CreateAxisCountDictionary(), Truncated: false);
        }

        var baseAddress = ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex);
        var rawHits = new List<RiftSessionWaypointScalarHit>();
        for (var offset = 0; offset <= bytes.Length - FloatStrideBytes; offset += FloatStrideBytes)
        {
            var memoryValue = ReadSingle(bytes, offset);
            if (!IsFinitePlausibleCoordinate(memoryValue))
            {
                continue;
            }

            foreach (var anchor in anchors)
            {
                AddHitIfWithinTolerance(snapshot, baseAddress, offset, memoryValue, anchor, WaypointXAxis, anchor.WaypointX, tolerance, rawHits);
                AddHitIfWithinTolerance(snapshot, baseAddress, offset, memoryValue, anchor, WaypointZAxis, anchor.WaypointZ, tolerance, rawHits);
            }
        }

        var retainedHits = new List<RiftSessionWaypointScalarHit>();
        var truncated = false;
        foreach (var group in rawHits.GroupBy(hit => string.Join('|', hit.AnchorId, hit.Axis), StringComparer.Ordinal))
        {
            var ordered = group
                .OrderBy(hit => hit.AbsDistance)
                .ThenBy(hit => hit.SourceRegionId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(hit => ParseUnsignedHexOrDecimal(hit.SourceOffsetHex))
                .ToArray();
            retainedHits.AddRange(ordered.Take(maxScalarHitsPerSnapshotAxis));
            truncated |= ordered.Length > maxScalarHitsPerSnapshotAxis;
        }

        return new(
            retainedHits,
            rawHits.Count,
            CountAxes(rawHits),
            CountAxes(retainedHits),
            truncated);
    }

    private static void AddHitIfWithinTolerance(
        SnapshotIndexEntry snapshot,
        ulong baseAddress,
        int offset,
        double memoryValue,
        RiftAddonWaypointAnchor anchor,
        string axis,
        double anchorValue,
        double tolerance,
        ICollection<RiftSessionWaypointScalarHit> hits)
    {
        var absDistance = Math.Abs(memoryValue - anchorValue);
        if (absDistance > tolerance)
        {
            return;
        }

        var absoluteAddress = CheckedAdd(baseAddress, (ulong)offset);
        var sourceOffsetHex = FormatHex((ulong)offset);
        hits.Add(new()
        {
            AnchorId = anchor.AnchorId,
            Axis = axis,
            SnapshotId = snapshot.SnapshotId,
            SourceRegionId = snapshot.RegionId,
            SourceBaseAddressHex = FormatHex(baseAddress),
            SourceOffsetHex = sourceOffsetHex,
            SourceAbsoluteAddressHex = FormatHex(absoluteAddress),
            MemoryValue = memoryValue,
            AnchorValue = anchorValue,
            AbsDistance = absDistance,
            EvidenceSummary = $"snapshot={snapshot.SnapshotId};anchor={anchor.AnchorId};axis={axis};offset={sourceOffsetHex};memory={memoryValue:F6};anchor={anchorValue:F6};abs_distance={absDistance:F6}"
        });
    }

    private static IEnumerable<RiftSessionWaypointScalarPairCandidate> RankPairCandidates(IReadOnlyList<RiftSessionWaypointScalarHit> hits)
    {
        var aggregates = new Dictionary<string, PairAggregate>(StringComparer.Ordinal);
        foreach (var snapshotAnchorGroup in hits.GroupBy(hit => string.Join('|', hit.SnapshotId, hit.AnchorId), StringComparer.Ordinal))
        {
            var xHits = snapshotAnchorGroup
                .Where(hit => string.Equals(hit.Axis, WaypointXAxis, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var zHits = snapshotAnchorGroup
                .Where(hit => string.Equals(hit.Axis, WaypointZAxis, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (xHits.Length == 0 || zHits.Length == 0)
            {
                continue;
            }

            foreach (var xHit in xHits)
            {
                foreach (var zHit in zHits)
                {
                    if (string.Equals(xHit.SourceAbsoluteAddressHex, zHit.SourceAbsoluteAddressHex, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = BuildPairCandidateKey(xHit, zHit);
                    if (!aggregates.TryGetValue(key, out var aggregate))
                    {
                        aggregate = new PairAggregate(xHit, zHit);
                        aggregates.Add(key, aggregate);
                    }

                    aggregate.Add(xHit, zHit);
                }
            }
        }

        return aggregates.Values
            .Select(aggregate => aggregate.ToCandidate())
            .OrderByDescending(candidate => candidate.SupportCount)
            .ThenByDescending(candidate => candidate.AnchorSupportCount)
            .ThenBy(candidate => candidate.BestDistanceTotal)
            .ThenBy(candidate => candidate.XSourceRegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.XSourceOffsetHex))
            .ThenBy(candidate => candidate.ZSourceRegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.ZSourceOffsetHex));
    }

    private static string BuildPairCandidateKey(RiftSessionWaypointScalarHit xHit, RiftSessionWaypointScalarHit zHit) =>
        string.Join('|', xHit.SourceRegionId, xHit.SourceOffsetHex, zHit.SourceRegionId, zHit.SourceOffsetHex);

    private static bool IsValidAnchor(RiftAddonWaypointAnchor anchor) =>
        IsFinitePlausibleCoordinate(anchor.WaypointX) &&
        IsFinitePlausibleCoordinate(anchor.WaypointZ) &&
        !string.IsNullOrWhiteSpace(anchor.AnchorId);

    private static bool IsFinitePlausibleCoordinate(double value) =>
        double.IsFinite(value) && Math.Abs(value) <= PlausibleCoordinateAbsMax;

    private static Dictionary<string, int> CreateAxisCountDictionary() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [WaypointXAxis] = 0,
            [WaypointZAxis] = 0
        };

    private static Dictionary<string, int> CountAxes(IEnumerable<RiftSessionWaypointScalarHit> hits)
    {
        var counts = CreateAxisCountDictionary();
        foreach (var hit in hits)
        {
            counts.TryGetValue(hit.Axis, out var existing);
            counts[hit.Axis] = existing + 1;
        }

        return counts;
    }

    private static void AddAxisCounts(IDictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach (var (axis, count) in source)
        {
            target.TryGetValue(axis, out var existing);
            target[axis] = existing + count;
        }
    }

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

    private static string? WriteJsonLines(string? outputPath, IReadOnlyList<RiftSessionWaypointScalarHit> hits)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllLines(fullOutputPath, hits.Select(hit => JsonSerializer.Serialize(hit, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return fullOutputPath;
    }

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

    private sealed record SnapshotScalarScan(
        IReadOnlyList<RiftSessionWaypointScalarHit> RetainedHits,
        int RawHitCount,
        IReadOnlyDictionary<string, int> RawAxisHitCounts,
        IReadOnlyDictionary<string, int> RetainedAxisHitCounts,
        bool Truncated);

    private sealed class PairAggregate
    {
        private readonly HashSet<string> snapshotIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> anchorIds = new(StringComparer.OrdinalIgnoreCase);
        private string bestAnchorId = string.Empty;
        private double bestDistanceTotal = double.MaxValue;
        private double bestXAbsDistance;
        private double bestZAbsDistance;
        private double bestMemoryWaypointX;
        private double bestMemoryWaypointZ;
        private double anchorWaypointX;
        private double anchorWaypointZ;

        public PairAggregate(RiftSessionWaypointScalarHit xHit, RiftSessionWaypointScalarHit zHit)
        {
            XSourceRegionId = xHit.SourceRegionId;
            XSourceBaseAddressHex = xHit.SourceBaseAddressHex;
            XSourceOffsetHex = xHit.SourceOffsetHex;
            XSourceAbsoluteAddressHex = xHit.SourceAbsoluteAddressHex;
            ZSourceRegionId = zHit.SourceRegionId;
            ZSourceBaseAddressHex = zHit.SourceBaseAddressHex;
            ZSourceOffsetHex = zHit.SourceOffsetHex;
            ZSourceAbsoluteAddressHex = zHit.SourceAbsoluteAddressHex;
        }

        public string XSourceRegionId { get; }

        public string XSourceBaseAddressHex { get; }

        public string XSourceOffsetHex { get; }

        public string XSourceAbsoluteAddressHex { get; }

        public string ZSourceRegionId { get; }

        public string ZSourceBaseAddressHex { get; }

        public string ZSourceOffsetHex { get; }

        public string ZSourceAbsoluteAddressHex { get; }

        public int SupportCount { get; private set; }

        public void Add(RiftSessionWaypointScalarHit xHit, RiftSessionWaypointScalarHit zHit)
        {
            SupportCount++;
            snapshotIds.Add(xHit.SnapshotId);
            anchorIds.Add(xHit.AnchorId);

            var distanceTotal = xHit.AbsDistance + zHit.AbsDistance;
            if (distanceTotal >= bestDistanceTotal)
            {
                return;
            }

            bestDistanceTotal = distanceTotal;
            bestAnchorId = xHit.AnchorId;
            bestXAbsDistance = xHit.AbsDistance;
            bestZAbsDistance = zHit.AbsDistance;
            bestMemoryWaypointX = xHit.MemoryValue;
            bestMemoryWaypointZ = zHit.MemoryValue;
            anchorWaypointX = xHit.AnchorValue;
            anchorWaypointZ = zHit.AnchorValue;
        }

        public RiftSessionWaypointScalarPairCandidate ToCandidate()
        {
            var supportingSnapshotIds = snapshotIds.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var supportingAnchorIds = anchorIds.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            return new()
            {
                AnchorId = bestAnchorId,
                XSourceRegionId = XSourceRegionId,
                XSourceBaseAddressHex = XSourceBaseAddressHex,
                XSourceOffsetHex = XSourceOffsetHex,
                XSourceAbsoluteAddressHex = XSourceAbsoluteAddressHex,
                ZSourceRegionId = ZSourceRegionId,
                ZSourceBaseAddressHex = ZSourceBaseAddressHex,
                ZSourceOffsetHex = ZSourceOffsetHex,
                ZSourceAbsoluteAddressHex = ZSourceAbsoluteAddressHex,
                SupportCount = SupportCount,
                AnchorSupportCount = supportingAnchorIds.Length,
                BestXAbsDistance = bestXAbsDistance,
                BestZAbsDistance = bestZAbsDistance,
                BestDistanceTotal = bestDistanceTotal,
                BestMemoryWaypointX = bestMemoryWaypointX,
                BestMemoryWaypointZ = bestMemoryWaypointZ,
                AnchorWaypointX = anchorWaypointX,
                AnchorWaypointZ = anchorWaypointZ,
                SupportingSnapshotIds = supportingSnapshotIds,
                SupportingAnchorIds = supportingAnchorIds,
                ValidationStatus = SupportCount > 1 || supportingAnchorIds.Length > 1
                    ? "waypoint_scalar_pair_supported"
                    : "waypoint_scalar_pair_single_support",
                EvidenceSummary = $"x={XSourceBaseAddressHex}+{XSourceOffsetHex};z={ZSourceBaseAddressHex}+{ZSourceOffsetHex};support={SupportCount};anchors={supportingAnchorIds.Length};best_distance_total={bestDistanceTotal:F6}"
            };
        }
    }
}
