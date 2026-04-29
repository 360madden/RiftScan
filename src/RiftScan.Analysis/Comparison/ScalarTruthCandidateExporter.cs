using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthCandidateExporter
{
    public IReadOnlyList<ScalarTruthCandidate> Export(ScalarEvidenceSetResult result, string outputPath, int top = 100, string? corroborationPath = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var corroboration = ReadCorroboration(corroborationPath);
        var candidates = result.RankedCandidates
            .Where(IsExportable)
            .OrderByDescending(candidate => candidate.ScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => ToTruthCandidate(result, candidate, index, corroboration))
            .ToArray();

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllLines(fullPath, candidates.Select(candidate => JsonSerializer.Serialize(candidate, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return candidates;
    }

    private static bool IsExportable(ScalarEvidenceAggregateCandidate candidate) =>
        string.Equals(candidate.TruthReadiness, "strong_candidate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(candidate.TruthReadiness, "validated_candidate", StringComparison.OrdinalIgnoreCase);

    private static ScalarTruthCandidate ToTruthCandidate(
        ScalarEvidenceSetResult result,
        ScalarEvidenceAggregateCandidate candidate,
        int index,
        IReadOnlyDictionary<string, ScalarTruthCorroborationEntry>? corroboration)
    {
        var corroborationEntry = FindCorroboration(corroboration, candidate);
        var corroborationStatus = CorroborationStatus(corroboration, corroborationEntry);
        return new()
        {
            CandidateId = $"scalar-truth-{index + 1:000000}",
            SourceSchemaVersion = result.SchemaVersion,
            BaseAddressHex = candidate.BaseAddressHex,
            OffsetHex = candidate.OffsetHex,
            DataType = candidate.DataType,
            ValueFamily = candidate.ValueFamily,
            Classification = candidate.Classification,
            ScoreTotal = candidate.ScoreTotal,
            ConfidenceLevel = candidate.ConfidenceLevel,
            TruthReadiness = candidate.TruthReadiness,
            ValidationStatus = ToValidationStatus(candidate.TruthReadiness, corroborationStatus),
            ClaimLevel = ToClaimLevel(candidate.TruthReadiness, corroborationStatus),
            CorroborationStatus = corroborationStatus,
            CorroborationSources = CorroborationSources(corroborationEntry),
            CorroborationSummary = corroborationEntry?.EvidenceSummary ?? string.Empty,
            LabelsPresent = candidate.LabelsPresent,
            SourceSessionCount = result.SessionCount,
            PassiveStable = candidate.PassiveStable,
            OppositeTurnPolarity = candidate.OppositeTurnPolarity,
            CameraTurnSeparation = candidate.CameraTurnSeparation,
            SupportingReasons = candidate.SupportingReasons,
            RejectionReasons = candidate.RejectionReasons,
            EvidenceSummary = candidate.EvidenceSummary,
            NextValidationStep = NextValidationStep(candidate.NextValidationStep, corroborationStatus),
            Warning = Warning(corroborationStatus)
        };
    }

    private static string ToValidationStatus(string truthReadiness, string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "external_corroboration_conflicted_candidate";
        }

        if (string.Equals(corroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
        {
            return "behavior_and_external_corroborated_candidate";
        }

        return string.Equals(truthReadiness, "validated_candidate", StringComparison.OrdinalIgnoreCase)
            ? "behavior_validated_candidate"
            : "behavior_strong_candidate";
    }

    private static string ToClaimLevel(string truthReadiness, string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "conflicted_candidate";
        }

        return string.Equals(truthReadiness, "validated_candidate", StringComparison.OrdinalIgnoreCase)
            ? "validated_candidate"
            : "candidate";
    }

    private static string NextValidationStep(string fallback, string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "not_requested", StringComparison.OrdinalIgnoreCase))
        {
            return "add_external_or_addon_corroboration";
        }

        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "resolve_external_corroboration_conflict_before_promotion";
        }

        return fallback;
    }

    private static string Warning(string corroborationStatus) =>
        string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase)
            ? "candidate_conflicts_with_external_corroboration"
            : "candidate_evidence_not_recovered_truth";

    private static IReadOnlyList<string> CorroborationSources(ScalarTruthCorroborationEntry? entry) =>
        entry is null || string.IsNullOrWhiteSpace(entry.Source) ? [] : [entry.Source];

    private static string CorroborationStatus(
        IReadOnlyDictionary<string, ScalarTruthCorroborationEntry>? corroboration,
        ScalarTruthCorroborationEntry? entry)
    {
        if (corroboration is null)
        {
            return "not_requested";
        }

        return entry?.CorroborationStatus ?? "uncorroborated";
    }

    private static ScalarTruthCorroborationEntry? FindCorroboration(
        IReadOnlyDictionary<string, ScalarTruthCorroborationEntry>? corroboration,
        ScalarEvidenceAggregateCandidate candidate)
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

    private static IReadOnlyDictionary<string, ScalarTruthCorroborationEntry>? ReadCorroboration(string? corroborationPath)
    {
        if (string.IsNullOrWhiteSpace(corroborationPath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(corroborationPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Scalar truth corroboration file does not exist.", fullPath);
        }

        return File.ReadLines(fullPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<ScalarTruthCorroborationEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid scalar truth corroboration JSONL entry in {fullPath}."))
            .GroupBy(entry => CorroborationKey(entry.BaseAddressHex, entry.OffsetHex, entry.DataType, entry.Classification), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static string CorroborationKey(string baseAddressHex, string offsetHex, string dataType, string classification) =>
        string.Join("|", baseAddressHex, offsetHex, dataType, classification);
}
