using System.Text.Json;
using RiftScan.Analysis.Regions;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Vectors;

public sealed class Vec3CandidateAnalyzer
{
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

        var candidates = ReadStructureCandidates(fullSessionPath)
            .Where(IsVec3Source)
            .Where(candidate => !KnownSystemRegionClassifier.IsKnownSystemNoise(candidate.BaseAddressHex))
            .Select(ToVec3Candidate)
            .OrderByDescending(candidate => candidate.RankScore)
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

    private static Vec3Candidate ToVec3Candidate(StructureCandidate source)
    {
        var diagnostics = source.Diagnostics
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
            RankScore = source.Score,
            Recommendation = source.Score >= 75
                ? "vec3_candidate_followup"
                : "vec3_candidate_needs_more_samples",
            ValuePreview = source.ValuePreview,
            Diagnostics = diagnostics
        };
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

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
