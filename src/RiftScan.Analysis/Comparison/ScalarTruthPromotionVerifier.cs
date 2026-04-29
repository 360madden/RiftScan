using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthPromotionVerifier
{
    public ScalarTruthPromotionVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ScalarTruthPromotionVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Scalar truth promotion file does not exist."));
            return new ScalarTruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        ScalarTruthPromotionResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ScalarTruthPromotionResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid scalar truth promotion JSON: {ex.Message}"));
            return new ScalarTruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Scalar truth promotion JSON did not contain an object."));
            return new ScalarTruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new ScalarTruthPromotionVerificationResult
        {
            Path = fullPath,
            PromotedCandidateCount = result.PromotedCandidateCount,
            BlockedCandidateCount = result.BlockedCandidateCount,
            Issues = issues
        };
    }

    private static void ValidateResult(ScalarTruthPromotionResult result, ICollection<ScalarTruthPromotionVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.scalar_truth_promotion.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.scalar_truth_promotion.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable scalar truth promotion packet."));
        }

        Require(result.RecoveryPath, "recovery_path_missing", "recovery_path is required.", issues);
        Require(result.CorroborationPath, "corroboration_path_missing", "corroboration_path is required.", issues);
        if (result.PromotedCandidateCount != result.PromotedCandidates.Count)
        {
            issues.Add(Error("promoted_candidate_count_mismatch", "promoted_candidate_count must match promoted_candidates length."));
        }

        if (result.BlockedCandidateCount != result.BlockedCandidates.Count)
        {
            issues.Add(Error("blocked_candidate_count_mismatch", "blocked_candidate_count must match blocked_candidates length."));
        }

        if (result.PromotedCandidateCount + result.BlockedCandidateCount > result.RecoveredCandidateCount)
        {
            issues.Add(Error("candidate_count_invalid", "promoted_candidate_count plus blocked_candidate_count must be less than or equal to recovered_candidate_count."));
        }

        if (!result.Warnings.Contains("scalar_truth_promotion_is_not_final_truth_without_manual_review", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include scalar_truth_promotion_is_not_final_truth_without_manual_review."));
        }

        ValidateCandidates(result.PromotedCandidates, expectedPromoted: true, issues);
        ValidateCandidates(result.BlockedCandidates, expectedPromoted: false, issues);
    }

    private static void ValidateCandidates(
        IReadOnlyList<ScalarPromotedTruthCandidate> candidates,
        bool expectedPromoted,
        ICollection<ScalarTruthPromotionVerificationIssue> issues)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            Require(candidate.CandidateId, "candidate_id_missing", "candidate_id is required.", issues, index);
            Require(candidate.SourceRecoveredCandidateId, "source_recovered_candidate_id_missing", "source_recovered_candidate_id is required.", issues, index);
            Require(candidate.BaseAddressHex, "candidate_base_address_missing", "base_address_hex is required.", issues, index);
            Require(candidate.OffsetHex, "candidate_offset_missing", "offset_hex is required.", issues, index);
            Require(candidate.DataType, "candidate_data_type_missing", "data_type is required.", issues, index);
            Require(candidate.Classification, "candidate_classification_missing", "classification is required.", issues, index);
            Require(candidate.PromotionStatus, "candidate_promotion_status_missing", "promotion_status is required.", issues, index);
            Require(candidate.TruthReadiness, "candidate_truth_readiness_missing", "truth_readiness is required.", issues, index);
            Require(candidate.ClaimLevel, "candidate_claim_level_missing", "claim_level is required.", issues, index);
            Require(candidate.CorroborationStatus, "candidate_corroboration_status_missing", "corroboration_status is required.", issues, index);
            Require(candidate.EvidenceSummary, "candidate_evidence_summary_missing", "evidence_summary is required.", issues, index);
            Require(candidate.NextValidationStep, "candidate_next_validation_step_missing", "next_validation_step is required.", issues, index);
            Require(candidate.Warning, "candidate_warning_missing", "warning is required.", issues, index);

            if (expectedPromoted && !string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_promotion_status_invalid", "promoted_candidates must use promotion_status=corroborated_candidate.", index));
            }

            if (expectedPromoted && !string.Equals(candidate.TruthReadiness, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_truth_readiness_invalid", "promoted_candidates must use truth_readiness=corroborated_candidate.", index));
            }

            if (!expectedPromoted &&
                !string.Equals(candidate.PromotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate.PromotionStatus, "recovered_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_blocked_status_invalid", "blocked_candidates must use promotion_status=blocked_conflict or recovered_candidate.", index));
            }

            if (candidate.SupportingFileCount < 2)
            {
                issues.Add(Error("candidate_supporting_file_count_too_low", "promoted candidates require supporting_file_count >= 2.", index));
            }

            if (candidate.SupportingTruthCandidateIds.Count < 2)
            {
                issues.Add(Error("candidate_supporting_ids_too_few", "promoted candidates require at least two supporting truth candidate IDs.", index));
            }

            if (candidate.BestScoreTotal is < 0 or > 100 || double.IsNaN(candidate.BestScoreTotal) || double.IsInfinity(candidate.BestScoreTotal))
            {
                issues.Add(Error("candidate_score_invalid", "best_score_total must be finite and between 0 and 100.", index));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<ScalarTruthPromotionVerificationIssue> issues,
        int? candidateIndex = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
    }

    private static ScalarTruthPromotionVerificationIssue Error(string code, string message, int? candidateIndex = null) =>
        new()
        {
            Code = code,
            Message = message,
            CandidateIndex = candidateIndex
        };
}
