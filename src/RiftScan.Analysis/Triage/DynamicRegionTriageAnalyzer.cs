using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Deltas;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Triage;

public sealed class DynamicRegionTriageAnalyzer
{
    public SessionAnalysisResult AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var snapshotEntries = ReadSnapshotIndex(fullSessionPath);
        var triageEntries = snapshotEntries
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Select(group => AnalyzeRegion(fullSessionPath, manifest.SessionId, group.ToArray()))
            .OrderByDescending(entry => entry.RankScore)
            .ThenBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();

        WriteJsonLines(fullSessionPath, "triage.jsonl", triageEntries);
        _ = new ByteDeltaAnalyzer().AnalyzeSession(fullSessionPath, top);
        _ = new FloatTripletStructureAnalyzer().AnalyzeSession(fullSessionPath, top);
        _ = new StructureClusterAnalyzer().AnalyzeSession(fullSessionPath, top);
        WriteJson(fullSessionPath, "next_capture_plan.json", BuildNextCapturePlan(manifest.SessionId, triageEntries));

        return new SessionAnalysisResult
        {
            Success = true,
            SessionPath = fullSessionPath,
            SessionId = manifest.SessionId,
            RegionsAnalyzed = triageEntries.Length,
            ArtifactsWritten = ["triage.jsonl", "deltas.jsonl", "structures.jsonl", "clusters.jsonl", "next_capture_plan.json"]
        };
    }

    private static RegionTriageEntry AnalyzeRegion(string sessionPath, string sessionId, IReadOnlyList<SnapshotIndexEntry> snapshots)
    {
        var bytes = snapshots
            .SelectMany(snapshot => File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path)))
            .ToArray();
        var entropy = CalculateEntropy(bytes);
        var zeroRatio = bytes.Length == 0 ? 0 : bytes.Count(value => value == 0) / (double)bytes.Length;
        var uniqueChecksumCount = snapshots.Select(snapshot => snapshot.ChecksumSha256Hex).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var diagnostics = new List<string>();

        if (snapshots.Count == 1)
        {
            diagnostics.Add("single_snapshot_no_delta_available");
        }

        if (uniqueChecksumCount > 1)
        {
            diagnostics.Add("checksum_changed_across_samples");
        }

        if (entropy < 1.0)
        {
            diagnostics.Add("low_entropy_region");
        }

        var dynamicScore = snapshots.Count > 1 ? uniqueChecksumCount / (double)snapshots.Count * 45.0 : 0.0;
        var entropyScore = Math.Min(entropy / 8.0, 1.0) * 35.0;
        var densityScore = (1.0 - zeroRatio) * 20.0;
        var rankScore = Math.Round(dynamicScore + entropyScore + densityScore, 3);

        return new RegionTriageEntry
        {
            SessionId = sessionId,
            RegionId = snapshots[0].RegionId,
            BaseAddressHex = snapshots[0].BaseAddressHex,
            SnapshotCount = snapshots.Count,
            UniqueChecksumCount = uniqueChecksumCount,
            TotalBytes = snapshots.Sum(snapshot => snapshot.SizeBytes),
            ByteEntropy = Math.Round(entropy, 6),
            ZeroByteRatio = Math.Round(zeroRatio, 6),
            RankScore = rankScore,
            Recommendation = Recommend(snapshots.Count, uniqueChecksumCount, entropy, zeroRatio),
            Diagnostics = diagnostics
        };
    }

    private static string Recommend(int snapshotCount, int uniqueChecksumCount, double entropy, double zeroRatio)
    {
        if (snapshotCount < 2)
        {
            return "capture_more_samples_before_dynamic_claim";
        }

        if (uniqueChecksumCount > 1 && entropy >= 0.5 && zeroRatio < 0.98)
        {
            return "prioritize_for_delta_followup";
        }

        return "low_priority_until_more_signal";
    }

    private static NextCapturePlan BuildNextCapturePlan(string sessionId, IReadOnlyList<RegionTriageEntry> triageEntries) =>
        new()
        {
            SessionId = sessionId,
            Recommendation = triageEntries.Any(entry => entry.SnapshotCount < 2)
                ? "capture_at_least_two_samples_for_delta_triage"
                : "prioritize_regions_with_checksum_changes_clusters_and_structures",
            Regions = triageEntries
                .Take(25)
                .Select(entry => new NextCaptureRegion
                {
                    RegionId = entry.RegionId,
                    RankScore = entry.RankScore,
                    BaseAddressHex = entry.BaseAddressHex,
                    SizeBytes = entry.TotalBytes / entry.SnapshotCount,
                    Reason = entry.Recommendation
                })
                .ToArray()
        };

    private static double CalculateEntropy(IReadOnlyCollection<byte> bytes)
    {
        if (bytes.Count == 0)
        {
            return 0;
        }

        Span<int> counts = stackalloc int[256];
        foreach (var value in bytes)
        {
            counts[value]++;
        }

        var entropy = 0.0;
        foreach (var count in counts)
        {
            if (count == 0)
            {
                continue;
            }

            var probability = count / (double)bytes.Count;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
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

    private static void WriteJson<T>(string sessionPath, string relativePath, T payload) =>
        File.WriteAllText(ResolveSessionPath(sessionPath, relativePath), JsonSerializer.Serialize(payload, SessionJson.Options));

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
