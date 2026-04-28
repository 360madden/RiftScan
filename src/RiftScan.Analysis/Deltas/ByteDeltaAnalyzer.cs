using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Deltas;

public sealed class ByteDeltaAnalyzer
{
    private const int MaxRangesPerRegion = 64;

    public IReadOnlyList<RegionDeltaEntry> AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before delta analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var entries = ReadSnapshotIndex(fullSessionPath)
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Select(group => AnalyzeRegion(fullSessionPath, manifest.SessionId, group.ToArray()))
            .Where(entry => entry.ComparedPairCount > 0)
            .OrderByDescending(entry => entry.RankScore)
            .ThenBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();

        WriteJsonLines(fullSessionPath, "deltas.jsonl", entries);
        return entries;
    }

    private static RegionDeltaEntry AnalyzeRegion(string sessionPath, string sessionId, IReadOnlyList<SnapshotIndexEntry> snapshots)
    {
        var changedOffsets = new HashSet<int>();
        long comparedBytes = 0;
        long changedPairBytes = 0;
        var maxBytes = snapshots.Count == 0 ? 0 : snapshots.Max(snapshot => snapshot.SizeBytes);

        for (var index = 1; index < snapshots.Count; index++)
        {
            var previous = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshots[index - 1].Path));
            var current = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshots[index].Path));
            var commonLength = Math.Min(previous.Length, current.Length);
            var maxLength = Math.Max(previous.Length, current.Length);
            comparedBytes += maxLength;

            for (var offset = 0; offset < commonLength; offset++)
            {
                if (previous[offset] == current[offset])
                {
                    continue;
                }

                changedOffsets.Add(offset);
                changedPairBytes++;
            }

            for (var offset = commonLength; offset < maxLength; offset++)
            {
                changedOffsets.Add(offset);
                changedPairBytes++;
            }
        }

        var ranges = BuildRanges(changedOffsets)
            .Take(MaxRangesPerRegion)
            .ToArray();
        var changedByteRatio = maxBytes == 0 ? 0 : changedOffsets.Count / (double)maxBytes;
        var pairChangeRatio = comparedBytes == 0 ? 0 : changedPairBytes / (double)comparedBytes;
        var rankScore = Math.Round(Math.Min(100.0, pairChangeRatio * 100.0), 3);

        return new RegionDeltaEntry
        {
            SessionId = sessionId,
            RegionId = snapshots[0].RegionId,
            BaseAddressHex = snapshots[0].BaseAddressHex,
            SnapshotCount = snapshots.Count,
            ComparedPairCount = Math.Max(0, snapshots.Count - 1),
            ChangedByteCount = changedOffsets.Count,
            ChangedByteRatio = Math.Round(changedByteRatio, 6),
            PairChangeRatio = Math.Round(pairChangeRatio, 6),
            ChangedRangeCount = CountRanges(changedOffsets),
            RankScore = rankScore,
            Recommendation = Recommend(changedOffsets.Count, changedByteRatio),
            ChangedRanges = ranges
        };
    }

    private static string Recommend(int changedByteCount, double changedByteRatio)
    {
        if (changedByteCount == 0)
        {
            return "no_changed_bytes_observed";
        }

        if (changedByteRatio <= 0.10)
        {
            return "sparse_offset_followup";
        }

        return "broad_dynamic_region_followup";
    }

    private static IEnumerable<ByteDeltaRange> BuildRanges(IReadOnlySet<int> offsets)
    {
        if (offsets.Count == 0)
        {
            yield break;
        }

        var ordered = offsets.Order().ToArray();
        var start = ordered[0];
        var previous = ordered[0];
        for (var index = 1; index < ordered.Length; index++)
        {
            var current = ordered[index];
            if (current == previous + 1)
            {
                previous = current;
                continue;
            }

            yield return ToRange(start, previous);
            start = current;
            previous = current;
        }

        yield return ToRange(start, previous);
    }

    private static int CountRanges(IReadOnlySet<int> offsets) => BuildRanges(offsets).Count();

    private static ByteDeltaRange ToRange(int start, int end) =>
        new()
        {
            StartOffsetHex = $"0x{start:X}",
            EndOffsetHex = $"0x{end:X}",
            LengthBytes = end - start + 1
        };

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

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
