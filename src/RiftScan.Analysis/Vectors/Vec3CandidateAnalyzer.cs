using System.Text.Json;
using System.Buffers.Binary;
using RiftScan.Analysis.Regions;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Vectors;

public sealed class Vec3CandidateAnalyzer
{
    private const double MovementEpsilon = 0.01;

    public IReadOnlyList<Vec3Candidate> AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before vec3 analysis: {issues}");
        }

        var snapshotEntries = ReadSnapshotIndex(fullSessionPath)
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var stimulusLabel = ReadStimulusLabel(fullSessionPath);
        var candidates = ReadStructureCandidates(fullSessionPath)
            .Where(IsVec3Source)
            .Where(candidate => !KnownSystemRegionClassifier.IsKnownSystemNoise(candidate.BaseAddressHex))
            .Select(candidate => ToVec3Candidate(fullSessionPath, candidate, snapshotEntries, stimulusLabel))
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenByDescending(candidate => candidate.BehaviorScore)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => candidate with { CandidateId = $"vec3-{index + 1:000000}" })
            .ToArray();

        WriteJsonLines(fullSessionPath, "vec3_candidates.jsonl", candidates);
        return candidates;
    }

    private static bool IsVec3Source(StructureCandidate candidate) =>
        string.Equals(candidate.StructureKind, "float32_triplet", StringComparison.OrdinalIgnoreCase) &&
        candidate.ValuePreview.Count == 3 &&
        candidate.ValuePreview.All(float.IsFinite);

    private static Vec3Candidate ToVec3Candidate(
        string sessionPath,
        StructureCandidate source,
        IReadOnlyDictionary<string, SnapshotIndexEntry[]> snapshotEntries,
        string stimulusLabel)
    {
        var values = ReadVec3Values(sessionPath, source, snapshotEntries);
        var deltaMagnitude = CalculateDeltaMagnitude(values);
        var behavior = ScoreBehavior(stimulusLabel, deltaMagnitude);
        var rankScore = Math.Round(Math.Min(100.0, source.Score * 0.75 + behavior.Score), 3);
        var diagnostics = source.Diagnostics
            .Concat(behavior.Diagnostics)
            .Append("promoted_from_float32_triplet_structure")
            .Append("candidate_not_truth_claim")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new Vec3Candidate
        {
            SessionId = source.SessionId,
            RegionId = source.RegionId,
            BaseAddressHex = source.BaseAddressHex,
            OffsetHex = source.OffsetHex,
            AbsoluteAddressHex = source.AbsoluteAddressHex,
            SourceStructureKind = source.StructureKind,
            SnapshotSupport = source.SnapshotSupport,
            StimulusLabel = stimulusLabel,
            SampleValueCount = values.Count,
            ValueDeltaMagnitude = Math.Round(deltaMagnitude, 6),
            BehaviorScore = behavior.Score,
            RankScore = rankScore,
            ValidationStatus = behavior.ValidationStatus,
            ConfidenceLevel = ToConfidenceLevel(rankScore),
            ExplanationShort = behavior.Recommendation,
            Recommendation = behavior.Recommendation,
            ValuePreview = source.ValuePreview,
            Diagnostics = diagnostics
        };
    }

    private static string ToConfidenceLevel(double rankScore)
    {
        if (rankScore >= 75.0)
        {
            return "high";
        }

        return rankScore >= 50.0 ? "medium" : "low";
    }

    private static IReadOnlyList<IReadOnlyList<float>> ReadVec3Values(
        string sessionPath,
        StructureCandidate source,
        IReadOnlyDictionary<string, SnapshotIndexEntry[]> snapshotEntries)
    {
        if (!snapshotEntries.TryGetValue(source.RegionId, out var entries))
        {
            return [];
        }

        var offset = ParseOffset(source.OffsetHex);
        var values = new List<IReadOnlyList<float>>();
        foreach (var entry in entries)
        {
            var bytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, entry.Path));
            if (offset + 12 > bytes.Length)
            {
                continue;
            }

            values.Add(ReadTriplet(bytes.AsSpan(offset, 12)));
        }

        return values;
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

    private static double CalculateDeltaMagnitude(IReadOnlyList<IReadOnlyList<float>> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var first = values[0];
        var last = values[^1];
        var dx = last[0] - first[0];
        var dy = last[1] - first[1];
        var dz = last[2] - first[2];
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static BehaviorScore ScoreBehavior(string stimulusLabel, double deltaMagnitude)
    {
        if (string.IsNullOrWhiteSpace(stimulusLabel))
        {
            return new BehaviorScore(
                0,
                "unvalidated_candidate",
                "vec3_candidate_followup",
                ["no_stimulus_label_available"]);
        }

        if (string.Equals(stimulusLabel, "move_forward", StringComparison.OrdinalIgnoreCase))
        {
            return deltaMagnitude > MovementEpsilon
                ? new BehaviorScore(25, "behavior_consistent_candidate", "move_forward_vec3_candidate_followup", ["move_forward_vec3_changed"])
                : new BehaviorScore(0, "behavior_unconfirmed_candidate", "move_forward_vec3_candidate_no_delta", ["move_forward_label_without_vec3_delta"]);
        }

        if (string.Equals(stimulusLabel, "passive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stimulusLabel, "passive_idle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stimulusLabel, "turn_left", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stimulusLabel, "turn_right", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stimulusLabel, "camera_only", StringComparison.OrdinalIgnoreCase))
        {
            return deltaMagnitude <= MovementEpsilon
                ? new BehaviorScore(20, "behavior_consistent_candidate", $"{stimulusLabel}_stable_vec3_candidate_followup", [$"{stimulusLabel}_vec3_stable"])
                : new BehaviorScore(5, "behavior_unconfirmed_candidate", $"{stimulusLabel}_vec3_changed_followup", [$"{stimulusLabel}_vec3_changed"]);
        }

        return new BehaviorScore(0, "unvalidated_candidate", "vec3_candidate_followup", ["unknown_stimulus_label"]);
    }

    private static IReadOnlyList<StructureCandidate> ReadStructureCandidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "structures.jsonl");
        if (!File.Exists(path))
        {
            _ = new FloatTripletStructureAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StructureCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid structure candidate."))
            .ToArray();
    }

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
            .ToArray();
    }

    private static string ReadStimulusLabel(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "stimuli.jsonl");
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var stimulus = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StimulusEvent>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid stimulus entry."))
            .FirstOrDefault();
        return stimulus?.Label ?? string.Empty;
    }

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static int ParseOffset(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return Convert.ToInt32(text, 16);
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed record BehaviorScore(
        double Score,
        string ValidationStatus,
        string Recommendation,
        IReadOnlyList<string> Diagnostics);
}
