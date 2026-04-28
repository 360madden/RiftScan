using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Values;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class SessionComparisonService
{
    public SessionComparisonResult Compare(string sessionAPath, string sessionBPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionAPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionBPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullAPath = Path.GetFullPath(sessionAPath);
        var fullBPath = Path.GetFullPath(sessionBPath);
        EnsureAnalyzed(fullAPath, top);
        EnsureAnalyzed(fullBPath, top);

        var manifestA = ReadJson<SessionManifest>(fullAPath, "manifest.json");
        var manifestB = ReadJson<SessionManifest>(fullBPath, "manifest.json");
        var regionMatches = CompareRegions(ReadTriage(fullAPath), ReadTriage(fullBPath), top);
        var clusterMatches = CompareClusters(ReadClusters(fullAPath), ReadClusters(fullBPath), top);
        var structureCandidateMatches = CompareStructureCandidates(ReadStructureCandidates(fullAPath), ReadStructureCandidates(fullBPath), top);
        var valueCandidateMatches = CompareValueCandidates(ReadValueCandidates(fullAPath), ReadValueCandidates(fullBPath), top);
        var warnings = BuildWarnings(manifestA, manifestB, regionMatches, clusterMatches, structureCandidateMatches, valueCandidateMatches);

        return new SessionComparisonResult
        {
            Success = true,
            SessionAPath = fullAPath,
            SessionBPath = fullBPath,
            SessionAId = manifestA.SessionId,
            SessionBId = manifestB.SessionId,
            SameProcessName = string.Equals(manifestA.ProcessName, manifestB.ProcessName, StringComparison.OrdinalIgnoreCase),
            MatchingRegionCount = regionMatches.Count,
            MatchingClusterCount = clusterMatches.Count,
            MatchingStructureCandidateCount = structureCandidateMatches.Count,
            MatchingValueCandidateCount = valueCandidateMatches.Count,
            RegionMatches = regionMatches,
            ClusterMatches = clusterMatches,
            StructureCandidateMatches = structureCandidateMatches,
            ValueCandidateMatches = valueCandidateMatches,
            Warnings = warnings
        };
    }

    private static void EnsureAnalyzed(string sessionPath, int top)
    {
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before comparison: {issues}");
        }

        if (!File.Exists(ResolveSessionPath(sessionPath, "triage.jsonl")) ||
            !File.Exists(ResolveSessionPath(sessionPath, "clusters.jsonl")) ||
            !File.Exists(ResolveSessionPath(sessionPath, "structures.jsonl")) ||
            !File.Exists(ResolveSessionPath(sessionPath, "typed_value_candidates.jsonl")))
        {
            _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionPath, top);
        }
    }

    private static IReadOnlyList<RegionComparison> CompareRegions(IReadOnlyList<RegionTriageEntry> a, IReadOnlyList<RegionTriageEntry> b, int top)
    {
        var bByBase = b
            .GroupBy(entry => entry.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(entry => entry.RankScore).First(), StringComparer.OrdinalIgnoreCase);

        return a
            .Where(entry => bByBase.ContainsKey(entry.BaseAddressHex))
            .Select(entry => BuildRegionComparison(entry, bByBase[entry.BaseAddressHex]))
            .OrderByDescending(match => Math.Min(match.SessionARankScore, match.SessionBRankScore))
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }

    private static RegionComparison BuildRegionComparison(RegionTriageEntry a, RegionTriageEntry b) =>
        new()
        {
            BaseAddressHex = a.BaseAddressHex,
            SessionARegionId = a.RegionId,
            SessionBRegionId = b.RegionId,
            SessionARankScore = a.RankScore,
            SessionBRankScore = b.RankScore,
            ScoreDelta = Math.Round(b.RankScore - a.RankScore, 3),
            SessionAUniqueHashes = a.UniqueChecksumCount,
            SessionBUniqueHashes = b.UniqueChecksumCount,
            Recommendation = RecommendRegion(a, b)
        };

    private static string RecommendRegion(RegionTriageEntry a, RegionTriageEntry b)
    {
        if (a.UniqueChecksumCount > 1 && b.UniqueChecksumCount > 1)
        {
            return "stable_dynamic_region_candidate";
        }

        return "shared_region_low_dynamic_evidence";
    }

    private static IReadOnlyList<ClusterComparison> CompareClusters(IReadOnlyList<StructureCluster> a, IReadOnlyList<StructureCluster> b, int top)
    {
        var bByBase = b
            .GroupBy(cluster => cluster.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        return a
            .Where(cluster => bByBase.ContainsKey(cluster.BaseAddressHex))
            .SelectMany(cluster => bByBase[cluster.BaseAddressHex]
                .Select(candidate => BuildClusterComparison(cluster, candidate))
                .Where(match => match.OverlapBytes > 0))
            .OrderByDescending(match => Math.Min(match.SessionARankScore, match.SessionBRankScore))
            .ThenByDescending(match => match.OverlapBytes)
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.StartOffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }

    private static ClusterComparison BuildClusterComparison(StructureCluster a, StructureCluster b)
    {
        var start = Math.Max(ParseHex(a.StartOffsetHex), ParseHex(b.StartOffsetHex));
        var end = Math.Min(ParseHex(a.EndOffsetHex), ParseHex(b.EndOffsetHex));
        var overlapBytes = Math.Max(0, end - start);

        return new ClusterComparison
        {
            BaseAddressHex = a.BaseAddressHex,
            StartOffsetHex = $"0x{start:X}",
            EndOffsetHex = $"0x{end:X}",
            SessionAClusterId = a.ClusterId,
            SessionBClusterId = b.ClusterId,
            SessionARegionId = a.RegionId,
            SessionBRegionId = b.RegionId,
            SessionARankScore = a.RankScore,
            SessionBRankScore = b.RankScore,
            CandidateCountDelta = b.CandidateCount - a.CandidateCount,
            OverlapBytes = overlapBytes,
            Recommendation = overlapBytes >= 16 && a.CandidateCount >= 4 && b.CandidateCount >= 4
                ? "stable_overlapping_layout_cluster_candidate"
                : "overlapping_cluster_needs_more_evidence"
        };
    }

    private static IReadOnlyList<StructureCandidateComparison> CompareStructureCandidates(
        IReadOnlyList<StructureCandidate> a,
        IReadOnlyList<StructureCandidate> b,
        int top)
    {
        var bByKey = b
            .GroupBy(StructureCandidateKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(candidate => candidate.Score).First(), StringComparer.OrdinalIgnoreCase);

        return a
            .Where(candidate => bByKey.ContainsKey(StructureCandidateKey(candidate)))
            .Select(candidate => BuildStructureCandidateComparison(candidate, bByKey[StructureCandidateKey(candidate)]))
            .OrderByDescending(match => Math.Min(match.SessionAScore, match.SessionBScore))
            .ThenByDescending(match => Math.Min(match.SessionASnapshotSupport, match.SessionBSnapshotSupport))
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }

    private static StructureCandidateComparison BuildStructureCandidateComparison(StructureCandidate a, StructureCandidate b) =>
        new()
        {
            BaseAddressHex = a.BaseAddressHex,
            OffsetHex = a.OffsetHex,
            StructureKind = a.StructureKind,
            SessionARegionId = a.RegionId,
            SessionBRegionId = b.RegionId,
            SessionAScore = a.Score,
            SessionBScore = b.Score,
            ScoreDelta = Math.Round(b.Score - a.Score, 3),
            SessionASnapshotSupport = a.SnapshotSupport,
            SessionBSnapshotSupport = b.SnapshotSupport,
            Recommendation = a.Score >= 75 && b.Score >= 75
                ? "stable_structure_candidate"
                : "matching_structure_candidate_needs_more_evidence"
        };

    private static string StructureCandidateKey(StructureCandidate candidate) =>
        string.Join("|", candidate.BaseAddressHex, candidate.OffsetHex, candidate.StructureKind);

    private static IReadOnlyList<ValueCandidateComparison> CompareValueCandidates(
        IReadOnlyList<TypedValueCandidate> a,
        IReadOnlyList<TypedValueCandidate> b,
        int top)
    {
        var bByKey = b
            .GroupBy(ValueCandidateKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(candidate => candidate.RankScore).First(), StringComparer.OrdinalIgnoreCase);

        return a
            .Where(candidate => bByKey.ContainsKey(ValueCandidateKey(candidate)))
            .Select(candidate => BuildValueCandidateComparison(candidate, bByKey[ValueCandidateKey(candidate)]))
            .OrderByDescending(match => Math.Min(match.SessionARankScore, match.SessionBRankScore))
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.DataType, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }

    private static ValueCandidateComparison BuildValueCandidateComparison(TypedValueCandidate a, TypedValueCandidate b) =>
        new()
        {
            BaseAddressHex = a.BaseAddressHex,
            OffsetHex = a.OffsetHex,
            DataType = a.DataType,
            SessionACandidateId = a.CandidateId,
            SessionBCandidateId = b.CandidateId,
            SessionARegionId = a.RegionId,
            SessionBRegionId = b.RegionId,
            SessionARankScore = a.RankScore,
            SessionBRankScore = b.RankScore,
            ScoreDelta = Math.Round(b.RankScore - a.RankScore, 3),
            SessionADistinctValues = a.DistinctValueCount,
            SessionBDistinctValues = b.DistinctValueCount,
            Recommendation = RecommendValueCandidate(a, b)
        };

    private static string ValueCandidateKey(TypedValueCandidate candidate) =>
        string.Join("|", candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType);

    private static string RecommendValueCandidate(TypedValueCandidate a, TypedValueCandidate b)
    {
        if (string.Equals(a.DataType, "float32", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.DataType, "int32", StringComparison.OrdinalIgnoreCase))
        {
            return "stable_typed_value_lane_candidate";
        }

        return "stable_raw_value_lane_candidate_not_semantic_claim";
    }

    private static int ParseHex(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return Convert.ToInt32(normalized, 16);
    }
    private static IReadOnlyList<string> BuildWarnings(
        SessionManifest manifestA,
        SessionManifest manifestB,
        IReadOnlyList<RegionComparison> regionMatches,
        IReadOnlyList<ClusterComparison> clusterMatches,
        IReadOnlyList<StructureCandidateComparison> structureCandidateMatches,
        IReadOnlyList<ValueCandidateComparison> valueCandidateMatches)
    {
        var warnings = new List<string>();
        if (!string.Equals(manifestA.ProcessName, manifestB.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("process_names_differ");
        }

        if (regionMatches.Count == 0)
        {
            warnings.Add("no_matching_base_addresses_between_triage_outputs");
        }

        if (clusterMatches.Count == 0)
        {
            warnings.Add("no_overlapping_cluster_matches_by_base_address_and_span");
        }

        if (structureCandidateMatches.Count == 0)
        {
            warnings.Add("no_matching_structure_candidates_by_base_offset_kind");
        }

        if (valueCandidateMatches.Count == 0)
        {
            warnings.Add("no_matching_typed_value_candidates_by_base_offset_type");
        }

        warnings.Add("comparison_is_candidate_evidence_not_truth_claim");
        return warnings;
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<RegionTriageEntry> ReadTriage(string sessionPath) =>
        ReadJsonLines<RegionTriageEntry>(sessionPath, "triage.jsonl");

    private static IReadOnlyList<StructureCluster> ReadClusters(string sessionPath) =>
        ReadJsonLines<StructureCluster>(sessionPath, "clusters.jsonl");

    private static IReadOnlyList<StructureCandidate> ReadStructureCandidates(string sessionPath) =>
        ReadJsonLines<StructureCandidate>(sessionPath, "structures.jsonl");

    private static IReadOnlyList<TypedValueCandidate> ReadValueCandidates(string sessionPath) =>
        ReadJsonLines<TypedValueCandidate>(sessionPath, "typed_value_candidates.jsonl");

    private static IReadOnlyList<T> ReadJsonLines<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid JSONL entry in {relativePath}."))
            .ToArray();
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
