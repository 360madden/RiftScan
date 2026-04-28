using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Triage;
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
        var warnings = BuildWarnings(manifestA, manifestB, regionMatches, clusterMatches);

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
            RegionMatches = regionMatches,
            ClusterMatches = clusterMatches,
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
            !File.Exists(ResolveSessionPath(sessionPath, "clusters.jsonl")))
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
        var bByKey = b
            .GroupBy(ClusterKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(cluster => cluster.RankScore).First(), StringComparer.OrdinalIgnoreCase);

        return a
            .Where(cluster => bByKey.ContainsKey(ClusterKey(cluster)))
            .Select(cluster => BuildClusterComparison(cluster, bByKey[ClusterKey(cluster)]))
            .OrderByDescending(match => Math.Min(match.SessionARankScore, match.SessionBRankScore))
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.StartOffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }

    private static ClusterComparison BuildClusterComparison(StructureCluster a, StructureCluster b) =>
        new()
        {
            BaseAddressHex = a.BaseAddressHex,
            StartOffsetHex = a.StartOffsetHex,
            EndOffsetHex = a.EndOffsetHex,
            SessionAClusterId = a.ClusterId,
            SessionBClusterId = b.ClusterId,
            SessionARegionId = a.RegionId,
            SessionBRegionId = b.RegionId,
            SessionARankScore = a.RankScore,
            SessionBRankScore = b.RankScore,
            CandidateCountDelta = b.CandidateCount - a.CandidateCount,
            Recommendation = a.CandidateCount >= 4 && b.CandidateCount >= 4
                ? "stable_layout_cluster_candidate"
                : "shared_cluster_needs_more_evidence"
        };

    private static string ClusterKey(StructureCluster cluster) =>
        string.Join('|', cluster.BaseAddressHex, cluster.StartOffsetHex, cluster.EndOffsetHex);

    private static IReadOnlyList<string> BuildWarnings(
        SessionManifest manifestA,
        SessionManifest manifestB,
        IReadOnlyList<RegionComparison> regionMatches,
        IReadOnlyList<ClusterComparison> clusterMatches)
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
            warnings.Add("no_exact_cluster_matches_by_base_address_and_span");
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
