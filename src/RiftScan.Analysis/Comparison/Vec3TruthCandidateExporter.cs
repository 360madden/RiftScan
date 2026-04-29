using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class Vec3TruthCandidateExporter
{
    public IReadOnlyList<Vec3TruthCandidate> Export(SessionComparisonResult result, string outputPath, int top = 100, string? corroborationPath = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var corroboration = ReadCorroboration(corroborationPath);
        var matches = result.Vec3CandidateMatches
            .GroupBy(match => CandidateKey(match.BaseAddressHex, match.OffsetHex, match.DataType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var candidates = result.Vec3BehaviorSummary.BehaviorContrastCandidates
            .Where(IsExportable)
            .OrderByDescending(candidate => candidate.ScoreTotal)
            .ThenByDescending(candidate => MoveDelta(candidate))
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => ToTruthCandidate(result, candidate, index, matches, corroboration))
            .ToArray();

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllLines(fullPath, candidates.Select(candidate => JsonSerializer.Serialize(candidate, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return candidates;
    }

    private static bool IsExportable(Vec3BehaviorContrastCandidate candidate) =>
        string.Equals(candidate.Classification, "position_like_vec3_candidate", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(candidate.ConfidenceLevel, "strong_candidate", StringComparison.OrdinalIgnoreCase) &&
        candidate.ScoreTotal >= 75 &&
        candidate.RejectionReasons.Count == 0;

    private static Vec3TruthCandidate ToTruthCandidate(
        SessionComparisonResult result,
        Vec3BehaviorContrastCandidate candidate,
        int index,
        IReadOnlyDictionary<string, Vec3CandidateComparison> matches,
        IReadOnlyDictionary<string, Vec3TruthCorroborationEntry>? corroboration)
    {
        var passiveStable = PassiveDelta(candidate) == 0;
        var moveForwardChanged = MoveDelta(candidate) > 0;
        var match = FindMatch(matches, candidate);
        var corroborationEntry = FindCorroboration(corroboration, candidate);
        var corroborationStatus = CorroborationStatus(corroboration, corroborationEntry);
        return new()
        {
            CandidateId = $"vec3-truth-{index + 1:000000}",
            SourceSchemaVersion = result.SchemaVersion,
            BaseAddressHex = candidate.BaseAddressHex,
            OffsetHex = candidate.OffsetHex,
            DataType = candidate.DataType,
            Classification = candidate.Classification,
            ScoreTotal = candidate.ScoreTotal,
            ConfidenceLevel = candidate.ConfidenceLevel,
            TruthReadiness = "strong_candidate",
            ValidationStatus = ValidationStatus(corroborationStatus),
            ClaimLevel = ClaimLevel(corroborationStatus),
            SourceSessionCount = 2,
            SessionAId = result.SessionAId,
            SessionBId = result.SessionBId,
            SessionAStimulusLabel = candidate.SessionAStimulusLabel,
            SessionBStimulusLabel = candidate.SessionBStimulusLabel,
            PassiveStable = passiveStable,
            MoveForwardChanged = moveForwardChanged,
            SessionAValueDeltaMagnitude = candidate.SessionAValueDeltaMagnitude,
            SessionBValueDeltaMagnitude = candidate.SessionBValueDeltaMagnitude,
            SessionAValueSequenceSummary = match?.SessionAValueSequenceSummary ?? string.Empty,
            SessionBValueSequenceSummary = match?.SessionBValueSequenceSummary ?? string.Empty,
            SessionAAnalyzerSources = match?.SessionAAnalyzerSources ?? [],
            SessionBAnalyzerSources = match?.SessionBAnalyzerSources ?? [],
            CorroborationStatus = corroborationStatus,
            CorroborationSources = CorroborationSources(corroborationEntry),
            CorroborationSummary = corroborationEntry?.EvidenceSummary ?? string.Empty,
            SupportingReasons = candidate.SupportingReasons,
            RejectionReasons = candidate.RejectionReasons,
            EvidenceSummary = candidate.EvidenceSummary,
            NextValidationStep = NextValidationStep(corroborationStatus),
            Warning = Warning(corroborationStatus)
        };
    }

    private static Vec3CandidateComparison? FindMatch(
        IReadOnlyDictionary<string, Vec3CandidateComparison> matches,
        Vec3BehaviorContrastCandidate candidate) =>
        matches.TryGetValue(CandidateKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType), out var match)
            ? match
            : null;

    private static Vec3TruthCorroborationEntry? FindCorroboration(
        IReadOnlyDictionary<string, Vec3TruthCorroborationEntry>? corroboration,
        Vec3BehaviorContrastCandidate candidate)
    {
        if (corroboration is null)
        {
            return null;
        }

        var exactKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, candidate.Classification);
        if (corroboration.TryGetValue(exactKey, out var exact))
        {
            return exact;
        }

        var wildcardKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, string.Empty);
        return corroboration.TryGetValue(wildcardKey, out var wildcard) ? wildcard : null;
    }

    private static IReadOnlyList<string> CorroborationSources(Vec3TruthCorroborationEntry? entry) =>
        entry is null || string.IsNullOrWhiteSpace(entry.Source) ? [] : [entry.Source];

    private static string CorroborationStatus(
        IReadOnlyDictionary<string, Vec3TruthCorroborationEntry>? corroboration,
        Vec3TruthCorroborationEntry? entry)
    {
        if (corroboration is null)
        {
            return "not_requested";
        }

        return entry?.CorroborationStatus ?? "uncorroborated";
    }

    private static string NextValidationStep(string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "not_requested", StringComparison.OrdinalIgnoreCase))
        {
            return "add_addon_waypoint_or_player_coord_corroboration";
        }

        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "resolve_addon_waypoint_coordinate_conflict_before_recovery";
        }

        if (string.Equals(corroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
        {
            return "repeat_move_forward_contrast_or_recover_across_sessions";
        }

        return "validate_against_addon_waypoint_or_player_coord_truth_or_repeat_move_forward_contrast";
    }

    private static string ValidationStatus(string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "addon_waypoint_corroboration_conflicted_candidate";
        }

        if (string.Equals(corroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
        {
            return "behavior_and_addon_waypoint_corroborated_candidate";
        }

        return "behavior_strong_candidate";
    }

    private static string ClaimLevel(string corroborationStatus) =>
        string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase)
            ? "conflicted_candidate"
            : "candidate";

    private static string Warning(string corroborationStatus) =>
        string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase)
            ? "candidate_conflicts_with_addon_waypoint_corroboration"
            : "candidate_evidence_not_recovered_coordinate_truth";

    private static IReadOnlyDictionary<string, Vec3TruthCorroborationEntry>? ReadCorroboration(string? corroborationPath)
    {
        if (string.IsNullOrWhiteSpace(corroborationPath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(corroborationPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Vec3 truth corroboration file does not exist.", fullPath);
        }

        return File.ReadLines(fullPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Vec3TruthCorroborationEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid vec3 truth corroboration JSONL entry in {fullPath}."))
            .GroupBy(entry => CorroborationKey(entry.BaseAddressHex, entry.OffsetHex, entry.DataType, entry.Classification), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static string CandidateKey(string baseAddressHex, string offsetHex, string dataType) =>
        string.Join("|", baseAddressHex, offsetHex, dataType);

    private static string CorroborationKey(string baseAddressHex, string offsetHex, string dataType, string classification) =>
        string.Join("|", baseAddressHex, offsetHex, dataType, classification);

    private static double PassiveDelta(Vec3BehaviorContrastCandidate candidate) =>
        IsPassive(candidate.SessionAStimulusLabel)
            ? candidate.SessionAValueDeltaMagnitude
            : IsPassive(candidate.SessionBStimulusLabel)
                ? candidate.SessionBValueDeltaMagnitude
                : double.NaN;

    private static double MoveDelta(Vec3BehaviorContrastCandidate candidate) =>
        IsMoveForward(candidate.SessionAStimulusLabel)
            ? candidate.SessionAValueDeltaMagnitude
            : IsMoveForward(candidate.SessionBStimulusLabel)
                ? candidate.SessionBValueDeltaMagnitude
                : 0;

    private static bool IsPassive(string label) =>
        string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase);

    private static bool IsMoveForward(string label) =>
        string.Equals(label, "move_forward", StringComparison.OrdinalIgnoreCase);
}
