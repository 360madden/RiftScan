using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthPromotionService
{
    public ScalarTruthPromotionResult Promote(string scalarTruthRecoveryPath, string scalarTruthCorroborationPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarTruthRecoveryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarTruthCorroborationPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var recoveryPath = Path.GetFullPath(scalarTruthRecoveryPath);
        var corroborationPath = Path.GetFullPath(scalarTruthCorroborationPath);
        var recovery = ReadRecovery(recoveryPath);
        var corroboration = ReadCorroboration(corroborationPath);
        var reviewCandidates = recovery.RecoveredCandidates
            .OrderByDescending(candidate => candidate.BestScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select(candidate => BuildCandidate(candidate, FindCorroboration(corroboration, candidate)))
            .ToArray();
        var promoted = reviewCandidates
            .Where(candidate => string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            .Select((candidate, index) => candidate with { CandidateId = $"scalar-promoted-{index + 1:000000}" })
            .ToArray();
        var blocked = reviewCandidates
            .Where(candidate => !string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            .Select((candidate, index) => candidate with { CandidateId = $"scalar-blocked-{index + 1:000000}" })
            .ToArray();

        return new ScalarTruthPromotionResult
        {
            Success = true,
            RecoveryPath = recoveryPath,
            CorroborationPath = corroborationPath,
            RecoveredCandidateCount = recovery.RecoveredCandidateCount,
            PromotedCandidateCount = promoted.Length,
            BlockedCandidateCount = blocked.Length,
            PromotedCandidates = promoted,
            BlockedCandidates = blocked,
            Warnings = BuildWarnings(promoted, blocked)
        };
    }

    private static ScalarTruthRecoveryResult ReadRecovery(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Scalar truth recovery file does not exist.", path);
        }

        var verification = new ScalarTruthRecoveryVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar truth recovery verification failed: {issues}");
        }

        return JsonSerializer.Deserialize<ScalarTruthRecoveryResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize scalar truth recovery packet.");
    }

    private static IReadOnlyDictionary<string, ScalarTruthCorroborationEntry> ReadCorroboration(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Scalar truth corroboration file does not exist.", path);
        }

        var verification = new ScalarTruthCorroborationVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar truth corroboration verification failed: {issues}");
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<ScalarTruthCorroborationEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid scalar truth corroboration JSONL entry in {path}."))
            .GroupBy(entry => CorroborationKey(entry.BaseAddressHex, entry.OffsetHex, entry.DataType, entry.Classification), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static ScalarPromotedTruthCandidate BuildCandidate(
        ScalarRecoveredTruthCandidate recovered,
        ScalarTruthCorroborationEntry? corroboration)
    {
        var corroborationStatus = corroboration?.CorroborationStatus ?? "uncorroborated";
        var promotionStatus = PromotionStatus(corroborationStatus);
        var supportingReasons = recovered.SupportingReasons
            .Append($"external_corroboration_{corroborationStatus}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ScalarPromotedTruthCandidate
        {
            SourceRecoveredCandidateId = recovered.CandidateId,
            BaseAddressHex = recovered.BaseAddressHex,
            OffsetHex = recovered.OffsetHex,
            DataType = recovered.DataType,
            ValueFamily = recovered.ValueFamily,
            Classification = recovered.Classification,
            PromotionStatus = promotionStatus,
            TruthReadiness = TruthReadiness(promotionStatus),
            ClaimLevel = ClaimLevel(promotionStatus),
            CorroborationStatus = corroborationStatus,
            CorroborationSources = string.IsNullOrWhiteSpace(corroboration?.Source) ? [] : [corroboration.Source],
            CorroborationSummary = corroboration?.EvidenceSummary ?? string.Empty,
            SupportingTruthCandidateIds = recovered.SupportingTruthCandidateIds,
            SupportingFileCount = recovered.SupportingFileCount,
            BestScoreTotal = recovered.BestScoreTotal,
            LabelsPresent = recovered.LabelsPresent,
            SupportingReasons = supportingReasons,
            EvidenceSummary = $"{recovered.EvidenceSummary};corroboration_status={corroborationStatus}",
            NextValidationStep = NextValidationStep(promotionStatus),
            Warning = Warning(promotionStatus)
        };
    }

    private static ScalarTruthCorroborationEntry? FindCorroboration(
        IReadOnlyDictionary<string, ScalarTruthCorroborationEntry> corroboration,
        ScalarRecoveredTruthCandidate candidate)
    {
        var exactKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, candidate.Classification);
        if (corroboration.TryGetValue(exactKey, out var exact))
        {
            return exact;
        }

        var wildcardKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, string.Empty);
        return corroboration.TryGetValue(wildcardKey, out var wildcard) ? wildcard : null;
    }

    private static string PromotionStatus(string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
        {
            return "corroborated_candidate";
        }

        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "blocked_conflict";
        }

        return "recovered_candidate";
    }

    private static string TruthReadiness(string promotionStatus) =>
        string.Equals(promotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase)
            ? "corroborated_candidate"
            : promotionStatus;

    private static string ClaimLevel(string promotionStatus) =>
        string.Equals(promotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase)
            ? "blocked_conflict"
            : TruthReadiness(promotionStatus);

    private static string NextValidationStep(string promotionStatus) =>
        promotionStatus.ToLowerInvariant() switch
        {
            "corroborated_candidate" => "manual_review_promoted_candidate_before_final_truth_claim",
            "blocked_conflict" => "resolve_external_corroboration_conflict_before_promotion",
            _ => "add_external_or_addon_corroboration"
        };

    private static string Warning(string promotionStatus) =>
        promotionStatus.ToLowerInvariant() switch
        {
            "corroborated_candidate" => "corroborated_candidate_requires_manual_review_before_final_truth_claim",
            "blocked_conflict" => "candidate_conflicts_with_external_corroboration",
            _ => "recovered_candidate_requires_external_corroboration_before_promotion"
        };

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<ScalarPromotedTruthCandidate> promoted,
        IReadOnlyList<ScalarPromotedTruthCandidate> blocked)
    {
        var warnings = new List<string> { "scalar_truth_promotion_is_not_final_truth_without_manual_review" };
        if (promoted.Count == 0)
        {
            warnings.Add("no_scalar_truth_candidates_promoted");
        }

        if (blocked.Any(candidate => string.Equals(candidate.PromotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("scalar_truth_promotion_contains_external_conflicts");
        }

        if (blocked.Any(candidate => string.Equals(candidate.PromotionStatus, "recovered_candidate", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("scalar_truth_promotion_contains_uncorroborated_recovered_candidates");
        }

        return warnings;
    }

    private static string CorroborationKey(string baseAddressHex, string offsetHex, string dataType, string classification) =>
        string.Join("|", baseAddressHex, offsetHex, dataType, classification);
}
