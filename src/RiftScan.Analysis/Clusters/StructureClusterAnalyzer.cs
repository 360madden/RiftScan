using System.Text.Json;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Clusters;

public sealed class StructureClusterAnalyzer
{
    private const int MaxGapBytes = 16;
    private const int MinimumCandidatesPerCluster = 2;

    public IReadOnlyList<StructureCluster> AnalyzeSession(string sessionPath, int maxClusters = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxClusters);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before cluster analysis: {issues}");
        }

        var structuresPath = ResolveSessionPath(fullSessionPath, "structures.jsonl");
        if (!File.Exists(structuresPath))
        {
            _ = new FloatTripletStructureAnalyzer().AnalyzeSession(fullSessionPath, maxClusters * 4);
        }

        var structures = ReadStructureCandidates(fullSessionPath);
        var clusters = structures
            .GroupBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => ClusterRegion(group.ToArray()))
            .OrderByDescending(cluster => cluster.RankScore)
            .ThenBy(cluster => cluster.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(cluster => cluster.StartOffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(maxClusters)
            .ToArray();

        WriteJsonLines(fullSessionPath, "clusters.jsonl", clusters);
        return clusters;
    }

    private static IEnumerable<StructureCluster> ClusterRegion(IReadOnlyList<StructureCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            yield break;
        }

        var ordered = candidates
            .OrderBy(candidate => ParseHex(candidate.OffsetHex))
            .ToArray();
        var clusterIndex = 1;
        var current = new List<StructureCandidate> { ordered[0] };
        var previousOffset = ParseHex(ordered[0].OffsetHex);

        foreach (var candidate in ordered.Skip(1))
        {
            var offset = ParseHex(candidate.OffsetHex);
            if (offset - previousOffset > MaxGapBytes)
            {
                if (TryBuildCluster(current, clusterIndex, out var cluster))
                {
                    yield return cluster;
                    clusterIndex++;
                }

                current = [];
            }

            current.Add(candidate);
            previousOffset = offset;
        }

        if (TryBuildCluster(current, clusterIndex, out var finalCluster))
        {
            yield return finalCluster;
        }
    }

    private static bool TryBuildCluster(IReadOnlyList<StructureCandidate> candidates, int clusterIndex, out StructureCluster cluster)
    {
        cluster = new StructureCluster();
        if (candidates.Count < MinimumCandidatesPerCluster)
        {
            return false;
        }

        var startOffset = candidates.Min(candidate => ParseHex(candidate.OffsetHex));
        var endOffset = candidates.Max(candidate => ParseHex(candidate.OffsetHex)) + 12;
        var spanBytes = checked((int)(endOffset - startOffset));
        var averageScore = candidates.Average(candidate => candidate.Score);
        var density = candidates.Count / Math.Max(spanBytes / 4.0, 1.0);
        var rankScore = Math.Round(averageScore * Math.Min(density, 1.0), 3);
        var first = candidates[0];
        var diagnostics = new List<string>
        {
            "adjacent_structure_candidates",
            $"max_gap_bytes_{MaxGapBytes}"
        };

        cluster = new StructureCluster
        {
            SessionId = first.SessionId,
            ClusterId = $"cluster-{clusterIndex:000000}",
            RegionId = first.RegionId,
            BaseAddressHex = first.BaseAddressHex,
            StartOffsetHex = $"0x{startOffset:X}",
            EndOffsetHex = $"0x{endOffset:X}",
            CandidateCount = candidates.Count,
            SpanBytes = spanBytes,
            AverageScore = Math.Round(averageScore, 3),
            RankScore = rankScore,
            Recommendation = candidates.Count >= 4 ? "prioritize_for_layout_followup" : "watch_for_repeated_layout_evidence",
            Diagnostics = diagnostics
        };
        return true;
    }

    private static IReadOnlyList<StructureCandidate> ReadStructureCandidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "structures.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StructureCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid structure candidate."))
            .ToArray();
    }

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static int ParseHex(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return Convert.ToInt32(normalized, 16);
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
