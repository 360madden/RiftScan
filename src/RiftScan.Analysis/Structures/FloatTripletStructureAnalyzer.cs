using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Structures;

public sealed class FloatTripletStructureAnalyzer
{
    private const float MaxCoordinateMagnitude = 100_000f;
    private const float MinComponentMagnitude = 0.001f;

    public IReadOnlyList<StructureCandidate> AnalyzeSession(string sessionPath, int maxCandidates = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCandidates);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before structure analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var snapshotEntries = ReadSnapshotIndex(fullSessionPath);
        var candidates = snapshotEntries
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => AnalyzeRegion(fullSessionPath, manifest.SessionId, group.ToArray()))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(maxCandidates)
            .Select((candidate, index) => candidate with { CandidateId = $"structure-{index + 1:000000}" })
            .ToArray();

        WriteJsonLines(fullSessionPath, "structures.jsonl", candidates);
        return candidates;
    }

    private static IEnumerable<StructureCandidate> AnalyzeRegion(string sessionPath, string sessionId, IReadOnlyList<SnapshotIndexEntry> snapshots)
    {
        var observed = new Dictionary<int, CandidateAccumulator>();

        foreach (var snapshot in snapshots)
        {
            var bytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path));
            for (var offset = 0; offset <= bytes.Length - 12; offset += 4)
            {
                var values = ReadTriplet(bytes.AsSpan(offset, 12));
                if (!IsPlausibleTriplet(values))
                {
                    continue;
                }

                if (!observed.TryGetValue(offset, out var accumulator))
                {
                    accumulator = new CandidateAccumulator(snapshot.RegionId, snapshot.BaseAddressHex, offset, values);
                    observed[offset] = accumulator;
                }

                accumulator.Support++;
            }
        }

        foreach (var accumulator in observed.Values)
        {
            var supportRatio = accumulator.Support / (double)snapshots.Count;
            if (supportRatio < 0.5)
            {
                continue;
            }

            var baseAddress = ParseHex(accumulator.BaseAddressHex);
            var absoluteAddress = baseAddress + (ulong)accumulator.Offset;
            var score = Math.Round(supportRatio * 100.0, 3);
            var diagnostics = new List<string> { "finite_float32_triplet" };
            if (accumulator.Support < snapshots.Count)
            {
                diagnostics.Add("partial_snapshot_support");
            }

            yield return new StructureCandidate
            {
                SessionId = sessionId,
                RegionId = accumulator.RegionId,
                BaseAddressHex = accumulator.BaseAddressHex,
                OffsetHex = $"0x{accumulator.Offset:X}",
                AbsoluteAddressHex = $"0x{absoluteAddress:X}",
                SnapshotSupport = accumulator.Support,
                Score = score,
                ConfidenceLevel = ToConfidenceLevel(score),
                ExplanationShort = $"finite_float32_triplet_supported_in_{accumulator.Support}_of_{snapshots.Count}_snapshots",
                ValuePreview = accumulator.FirstValues,
                Diagnostics = diagnostics
            };
        }
    }

    private static string ToConfidenceLevel(double score)
    {
        if (score >= 75.0)
        {
            return "high";
        }

        return score >= 50.0 ? "medium" : "low";
    }

    private static float[] ReadTriplet(ReadOnlySpan<byte> bytes) =>
    [
        ReadSingleLittleEndian(bytes[..4]),
        ReadSingleLittleEndian(bytes.Slice(4, 4)),
        ReadSingleLittleEndian(bytes.Slice(8, 4))
    ];

    private static float ReadSingleLittleEndian(ReadOnlySpan<byte> bytes)
    {
        var raw = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        return BitConverter.Int32BitsToSingle(raw);
    }

    private static bool IsPlausibleTriplet(IReadOnlyList<float> values)
    {
        if (values.Any(value => !float.IsFinite(value) || MathF.Abs(value) > MaxCoordinateMagnitude))
        {
            return false;
        }

        var meaningfulComponents = values.Count(value => MathF.Abs(value) >= MinComponentMagnitude);
        if (meaningfulComponents < 2)
        {
            return false;
        }

        return values.All(value => value == 0 || MathF.Abs(value) >= MinComponentMagnitude);
    }

    private static ulong ParseHex(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return Convert.ToUInt64(normalized, 16);
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

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed class CandidateAccumulator(string regionId, string baseAddressHex, int offset, IReadOnlyList<float> firstValues)
    {
        public string RegionId { get; } = regionId;

        public string BaseAddressHex { get; } = baseAddressHex;

        public int Offset { get; } = offset;

        public IReadOnlyList<float> FirstValues { get; } = firstValues;

        public int Support { get; set; }
    }
}
