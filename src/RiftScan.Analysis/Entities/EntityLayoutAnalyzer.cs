using System.Globalization;
using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Scalars;
using RiftScan.Analysis.Vectors;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Entities;

public sealed class EntityLayoutAnalyzer
{
    private const int MinimumStrideBytes = 16;
    private const int MaximumStrideBytes = 4096;

    public IReadOnlyList<EntityLayoutCandidate> AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before entity layout analysis: {issues}");
        }

        var clusters = ReadClusters(fullSessionPath);
        var vec3Candidates = ReadVec3Candidates(fullSessionPath);
        var scalarCandidates = ReadScalarCandidates(fullSessionPath);
        var candidates = clusters
            .GroupBy(cluster => cluster.RegionId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => AnalyzeRegion(group.ToArray(), vec3Candidates, scalarCandidates))
            .OrderByDescending(candidate => candidate.ScoreTotal)
            .ThenByDescending(candidate => candidate.ClusterCount)
            .ThenByDescending(candidate => candidate.Vec3CandidateCount)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.StartOffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => candidate with { CandidateId = $"entity-layout-{index + 1:000000}" })
            .ToArray();

        WriteJsonLines(fullSessionPath, "entity_layout_candidates.jsonl", candidates);
        return candidates;
    }

    private static IEnumerable<EntityLayoutCandidate> AnalyzeRegion(
        IReadOnlyList<StructureCluster> clusters,
        IReadOnlyList<Vec3Candidate> vec3Candidates,
        IReadOnlyList<ScalarCandidate> scalarCandidates)
    {
        if (clusters.Count == 0)
        {
            yield break;
        }

        var orderedClusters = clusters
            .OrderBy(cluster => ParseHex(cluster.StartOffsetHex))
            .ToArray();
        var regionId = orderedClusters[0].RegionId;
        var regionVec3Candidates = vec3Candidates
            .Where(candidate => string.Equals(candidate.RegionId, regionId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var regionScalarCandidates = scalarCandidates
            .Where(candidate => string.Equals(candidate.RegionId, regionId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        yield return BuildDenseLayoutCandidate(orderedClusters, regionVec3Candidates, regionScalarCandidates);

        if (orderedClusters.Length < 2)
        {
            yield break;
        }

        foreach (var strideGroup in BuildStrideGroups(orderedClusters))
        {
            yield return BuildStrideCandidate(strideGroup, regionVec3Candidates, regionScalarCandidates);
        }
    }

    private static IEnumerable<IReadOnlyList<StructureCluster>> BuildStrideGroups(IReadOnlyList<StructureCluster> clusters)
    {
        var groups = new Dictionary<int, List<StructureCluster>>();
        for (var index = 1; index < clusters.Count; index++)
        {
            var previousStart = ParseHex(clusters[index - 1].StartOffsetHex);
            var currentStart = ParseHex(clusters[index].StartOffsetHex);
            var stride = currentStart - previousStart;
            if (stride < MinimumStrideBytes || stride > MaximumStrideBytes)
            {
                continue;
            }

            var normalizedStride = RoundStride(stride);
            if (!groups.TryGetValue(normalizedStride, out var group))
            {
                group = [clusters[index - 1]];
                groups[normalizedStride] = group;
            }

            group.Add(clusters[index]);
        }

        foreach (var group in groups.Values)
        {
            yield return group
                .DistinctBy(cluster => cluster.ClusterId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(cluster => ParseHex(cluster.StartOffsetHex))
                .ToArray();
        }
    }

    private static EntityLayoutCandidate BuildDenseLayoutCandidate(
        IReadOnlyList<StructureCluster> clusters,
        IReadOnlyList<Vec3Candidate> vec3Candidates,
        IReadOnlyList<ScalarCandidate> scalarCandidates)
    {
        var startOffset = clusters.Min(cluster => ParseHex(cluster.StartOffsetHex));
        var endOffset = clusters.Max(cluster => ParseHex(cluster.EndOffsetHex));
        var vec3Count = CountCandidatesInSpan(vec3Candidates.Select(candidate => candidate.OffsetHex), startOffset, endOffset);
        var scalarCount = CountCandidatesInSpan(scalarCandidates.Select(candidate => candidate.OffsetHex), startOffset, endOffset);
        var averageClusterScore = clusters.Average(cluster => cluster.RankScore);
        var clusterScore = Math.Min(clusters.Count * 12.0, 36.0);
        var vec3Score = Math.Min(vec3Count * 10.0, 30.0);
        var scalarScore = Math.Min(scalarCount * 2.0, 12.0);
        var densityScore = Math.Min((vec3Count + scalarCount) / Math.Max((endOffset - startOffset) / 64.0, 1.0) * 10.0, 12.0);
        var supportScore = Math.Min(averageClusterScore * 0.10, 10.0);
        var score = Math.Round(Math.Clamp(clusterScore + vec3Score + scalarScore + densityScore + supportScore, 0, 100), 3);

        var diagnostics = new List<string>
        {
            "dense_cluster_layout_candidate",
            "entity_layout_candidate_not_player_identity",
            "candidate_not_truth_claim"
        };
        if (clusters.Count < 2)
        {
            diagnostics.Add("single_cluster_layout_requires_cross_session_followup");
        }

        var first = clusters[0];
        return new EntityLayoutCandidate
        {
            SessionId = first.SessionId,
            RegionId = first.RegionId,
            BaseAddressHex = first.BaseAddressHex,
            StartOffsetHex = $"0x{startOffset:X}",
            EndOffsetHex = $"0x{endOffset:X}",
            LayoutKind = "dense_cluster_layout",
            StrideBytes = 0,
            ClusterCount = clusters.Count,
            Vec3CandidateCount = vec3Count,
            ScalarCandidateCount = scalarCount,
            ScoreTotal = score,
            ScoreBreakdown = BuildScoreBreakdown(clusterScore, 0, vec3Score, scalarScore, densityScore, supportScore, score),
            FeatureVector = BuildFeatureVector(clusters.Count, vec3Count, scalarCount, startOffset, endOffset, 0),
            AnalyzerSources = ["clusters.jsonl", "vec3_candidates.jsonl", "scalar_candidates.jsonl"],
            ConfidenceLevel = ToConfidenceLevel(score),
            ExplanationShort = $"dense_layout_with_{clusters.Count}_clusters_{vec3Count}_vec3_candidates",
            Recommendation = "prioritize_for_entity_layout_cross_session_validation",
            Diagnostics = diagnostics
        };
    }

    private static EntityLayoutCandidate BuildStrideCandidate(
        IReadOnlyList<StructureCluster> clusters,
        IReadOnlyList<Vec3Candidate> vec3Candidates,
        IReadOnlyList<ScalarCandidate> scalarCandidates)
    {
        var startOffset = clusters.Min(cluster => ParseHex(cluster.StartOffsetHex));
        var endOffset = clusters.Max(cluster => ParseHex(cluster.EndOffsetHex));
        var stride = CalculateDominantStride(clusters);
        var vec3Count = CountCandidatesInSpan(vec3Candidates.Select(candidate => candidate.OffsetHex), startOffset, endOffset);
        var scalarCount = CountCandidatesInSpan(scalarCandidates.Select(candidate => candidate.OffsetHex), startOffset, endOffset);
        var averageClusterScore = clusters.Average(cluster => cluster.RankScore);
        var clusterScore = Math.Min(clusters.Count * 16.0, 40.0);
        var strideScore = clusters.Count >= 3 ? 18.0 : 8.0;
        var vec3Score = Math.Min(vec3Count * 10.0, 25.0);
        var scalarScore = Math.Min(scalarCount * 2.0, 10.0);
        var densityScore = Math.Min((vec3Count + scalarCount) / Math.Max((endOffset - startOffset) / 64.0, 1.0) * 8.0, 7.0);
        var supportScore = Math.Min(averageClusterScore * 0.08, 8.0);
        var score = Math.Round(Math.Clamp(clusterScore + strideScore + vec3Score + scalarScore + densityScore + supportScore, 0, 100), 3);
        var diagnostics = new List<string>
        {
            "repeated_cluster_stride_candidate",
            $"stride_bytes_{stride}",
            "entity_layout_candidate_not_player_identity",
            "candidate_not_truth_claim"
        };
        if (clusters.Count < 3)
        {
            diagnostics.Add("stride_from_two_clusters_requires_more_support");
        }

        var first = clusters[0];
        return new EntityLayoutCandidate
        {
            SessionId = first.SessionId,
            RegionId = first.RegionId,
            BaseAddressHex = first.BaseAddressHex,
            StartOffsetHex = $"0x{startOffset:X}",
            EndOffsetHex = $"0x{endOffset:X}",
            LayoutKind = "repeated_cluster_stride",
            StrideBytes = stride,
            ClusterCount = clusters.Count,
            Vec3CandidateCount = vec3Count,
            ScalarCandidateCount = scalarCount,
            ScoreTotal = score,
            ScoreBreakdown = BuildScoreBreakdown(clusterScore, strideScore, vec3Score, scalarScore, densityScore, supportScore, score),
            FeatureVector = BuildFeatureVector(clusters.Count, vec3Count, scalarCount, startOffset, endOffset, stride),
            AnalyzerSources = ["clusters.jsonl", "vec3_candidates.jsonl", "scalar_candidates.jsonl"],
            ConfidenceLevel = ToConfidenceLevel(score),
            ExplanationShort = $"stride_layout_{stride}_bytes_with_{clusters.Count}_clusters_{vec3Count}_vec3_candidates",
            Recommendation = "prioritize_for_stride_and_entity_pool_followup",
            Diagnostics = diagnostics
        };
    }

    private static IReadOnlyDictionary<string, double> BuildScoreBreakdown(
        double clusterScore,
        double strideScore,
        double vec3Score,
        double scalarScore,
        double densityScore,
        double supportScore,
        double scoreTotal) =>
        new Dictionary<string, double>
        {
            ["cluster_score"] = Math.Round(clusterScore, 3),
            ["stride_score"] = Math.Round(strideScore, 3),
            ["vec3_score"] = Math.Round(vec3Score, 3),
            ["scalar_score"] = Math.Round(scalarScore, 3),
            ["density_score"] = Math.Round(densityScore, 3),
            ["support_score"] = Math.Round(supportScore, 3),
            ["score_total"] = Math.Round(scoreTotal, 3)
        };

    private static IReadOnlyDictionary<string, double> BuildFeatureVector(
        int clusterCount,
        int vec3Count,
        int scalarCount,
        int startOffset,
        int endOffset,
        int strideBytes) =>
        new Dictionary<string, double>
        {
            ["cluster_count"] = clusterCount,
            ["vec3_candidate_count"] = vec3Count,
            ["scalar_candidate_count"] = scalarCount,
            ["span_bytes"] = Math.Max(endOffset - startOffset, 0),
            ["stride_bytes"] = strideBytes
        };

    private static int CalculateDominantStride(IReadOnlyList<StructureCluster> clusters)
    {
        if (clusters.Count < 2)
        {
            return 0;
        }

        return clusters
            .Zip(clusters.Skip(1), (left, right) => ParseHex(right.StartOffsetHex) - ParseHex(left.StartOffsetHex))
            .Where(stride => stride >= MinimumStrideBytes && stride <= MaximumStrideBytes)
            .GroupBy(RoundStride)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();
    }

    private static int CountCandidatesInSpan(IEnumerable<string> offsets, int startOffset, int endOffset) =>
        offsets
            .Select(ParseHex)
            .Count(offset => offset >= startOffset && offset < endOffset);

    private static int RoundStride(int stride) =>
        (int)(Math.Round(stride / 16.0, MidpointRounding.AwayFromZero) * 16);

    private static string ToConfidenceLevel(double score)
    {
        if (score >= 75.0)
        {
            return "high";
        }

        return score >= 50.0 ? "medium" : "low";
    }

    private static IReadOnlyList<StructureCluster> ReadClusters(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "clusters.jsonl");
        if (!File.Exists(path))
        {
            _ = new StructureClusterAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StructureCluster>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid structure cluster."))
            .ToArray();
    }

    private static IReadOnlyList<Vec3Candidate> ReadVec3Candidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "vec3_candidates.jsonl");
        if (!File.Exists(path))
        {
            _ = new Vec3CandidateAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Vec3Candidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid vec3 candidate."))
            .ToArray();
    }

    private static IReadOnlyList<ScalarCandidate> ReadScalarCandidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "scalar_candidates.jsonl");
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<ScalarCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid scalar candidate."))
            .ToArray();
    }

    private static int ParseHex(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return int.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
