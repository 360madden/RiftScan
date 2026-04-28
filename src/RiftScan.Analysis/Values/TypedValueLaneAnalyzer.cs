using System.Globalization;
using System.Text.Json;
using RiftScan.Analysis.Deltas;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Values;

public sealed class TypedValueLaneAnalyzer
{
    public IReadOnlyList<TypedValueCandidate> AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before typed value analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var deltaEntries = ReadDeltaEntries(fullSessionPath);
        var snapshotsByRegion = ReadSnapshotIndex(fullSessionPath)
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var candidates = deltaEntries
            .SelectMany(delta => AnalyzeRegion(fullSessionPath, manifest.SessionId, delta, snapshotsByRegion))
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => candidate with { CandidateId = $"value-{index + 1:000000}" })
            .ToArray();

        WriteJsonLines(fullSessionPath, "typed_value_candidates.jsonl", candidates);
        return candidates;
    }

    private static IEnumerable<TypedValueCandidate> AnalyzeRegion(
        string sessionPath,
        string sessionId,
        RegionDeltaEntry delta,
        IReadOnlyDictionary<string, SnapshotIndexEntry[]> snapshotsByRegion)
    {
        if (!snapshotsByRegion.TryGetValue(delta.RegionId, out var snapshots) || snapshots.Length < 2)
        {
            yield break;
        }

        var snapshotBytes = snapshots
            .Select(snapshot => File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path)))
            .ToArray();

        foreach (var offset in EnumerateAlignedOffsets(delta.ChangedRanges))
        {
            if (snapshotBytes.Any(bytes => offset + sizeof(uint) > bytes.Length))
            {
                continue;
            }

            var candidate = BuildCandidate(sessionId, delta, snapshotBytes, offset);
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    private static TypedValueCandidate? BuildCandidate(
        string sessionId,
        RegionDeltaEntry delta,
        IReadOnlyList<byte[]> snapshots,
        int offset)
    {
        var rawValues = snapshots
            .Select(bytes => BitConverter.ToUInt32(bytes, offset))
            .ToArray();
        if (rawValues.Distinct().Count() <= 1)
        {
            return null;
        }

        var floatValues = snapshots
            .Select(bytes => BitConverter.ToSingle(bytes, offset))
            .ToArray();
        var intValues = snapshots
            .Select(bytes => BitConverter.ToInt32(bytes, offset))
            .ToArray();

        var diagnostics = new List<string> { "derived_from_changed_byte_range" };
        string dataType;
        IReadOnlyList<string> valuePreview;
        double typeBonus;

        if (LooksLikeFloatLane(floatValues))
        {
            dataType = "float32";
            valuePreview = floatValues.Select(value => value.ToString("G9", CultureInfo.InvariantCulture)).ToArray();
            typeBonus = 15.0;
            diagnostics.Add("finite_plausible_float32_values");
        }
        else if (LooksLikeIntLane(intValues))
        {
            dataType = "int32";
            valuePreview = intValues.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray();
            typeBonus = 10.0;
            diagnostics.Add("plausible_int32_values");
        }
        else
        {
            dataType = "uint32";
            valuePreview = rawValues.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray();
            typeBonus = 0.0;
            diagnostics.Add("raw_uint32_fallback_not_semantic_claim");
        }

        var distinctValueCount = valuePreview.Distinct(StringComparer.Ordinal).Count();
        var changedSampleCount = CountAdjacentChanges(valuePreview);
        var comparedPairCount = Math.Max(1, valuePreview.Count - 1);
        var changeRatio = changedSampleCount / (double)comparedPairCount;
        var distinctRatio = distinctValueCount / (double)valuePreview.Count;
        var rankScore = Math.Min(100.0, changeRatio * 60.0 + distinctRatio * 25.0 + typeBonus);
        if (dataType == "uint32")
        {
            rankScore = Math.Min(rankScore, 55.0);
        }

        rankScore = Math.Round(rankScore, 3);
        var absoluteAddress = ParseHex(delta.BaseAddressHex) + (ulong)offset;

        return new TypedValueCandidate
        {
            SessionId = sessionId,
            RegionId = delta.RegionId,
            BaseAddressHex = delta.BaseAddressHex,
            OffsetHex = $"0x{offset:X}",
            AbsoluteAddressHex = $"0x{absoluteAddress:X}",
            DataType = dataType,
            SampleCount = valuePreview.Count,
            DistinctValueCount = distinctValueCount,
            ChangedSampleCount = changedSampleCount,
            RankScore = rankScore,
            Recommendation = Recommend(dataType, changedSampleCount),
            ValuePreview = valuePreview.Take(8).ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static bool LooksLikeFloatLane(IReadOnlyList<float> values) =>
        values.All(float.IsFinite) &&
        values.All(value => Math.Abs(value) <= 1_000_000.0f) &&
        values.Any(value => Math.Abs(value) >= 0.0001f);

    private static bool LooksLikeIntLane(IReadOnlyList<int> values) =>
        values.All(value => Math.Abs((long)value) <= 1_000_000L);

    private static string Recommend(string dataType, int changedSampleCount) =>
        changedSampleCount == 0
            ? "no_typed_value_change_observed"
            : dataType switch
            {
                "float32" => "float_lane_followup",
                "int32" => "integer_lane_followup",
                _ => "raw_lane_followup"
            };

    private static int CountAdjacentChanges(IReadOnlyList<string> values)
    {
        var changes = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (!string.Equals(values[index - 1], values[index], StringComparison.Ordinal))
            {
                changes++;
            }
        }

        return changes;
    }

    private static IEnumerable<int> EnumerateAlignedOffsets(IReadOnlyList<ByteDeltaRange> ranges)
    {
        var offsets = new SortedSet<int>();
        foreach (var range in ranges)
        {
            var start = ParseOffset(range.StartOffsetHex);
            var end = ParseOffset(range.EndOffsetHex);
            for (var offset = start; offset <= end; offset++)
            {
                offsets.Add(offset - (offset % sizeof(uint)));
            }
        }

        return offsets;
    }

    private static IReadOnlyList<RegionDeltaEntry> ReadDeltaEntries(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "deltas.jsonl");
        if (!File.Exists(path))
        {
            _ = new ByteDeltaAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RegionDeltaEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid delta entry."))
            .ToArray();
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

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static int ParseOffset(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return int.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static ulong ParseHex(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
