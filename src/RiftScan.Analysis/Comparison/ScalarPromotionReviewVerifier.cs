using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarPromotionReviewVerifier
{
    private static readonly HashSet<string> AllowedDecisionStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "ready_for_manual_truth_review",
        "blocked_conflict",
        "needs_more_corroboration",
        "needs_repeat_capture",
        "do_not_promote"
    };

    public ScalarPromotionReviewVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ScalarPromotionReviewVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Scalar promotion review file does not exist.", null));
            return new ScalarPromotionReviewVerificationResult { Path = fullPath, Issues = issues };
        }

        ScalarPromotionReviewResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ScalarPromotionReviewResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid scalar promotion review JSON: {ex.Message}", null));
            return new ScalarPromotionReviewVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Scalar promotion review JSON did not contain an object.", null));
            return new ScalarPromotionReviewVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new ScalarPromotionReviewVerificationResult
        {
            Path = fullPath,
            DecisionState = result.DecisionState,
            ReviewCandidateCount = result.ReviewCandidateCount,
            ReadyForManualTruthReviewCount = result.ReadyForManualTruthReviewCount,
            BlockedConflictCount = result.BlockedConflictCount,
            Issues = issues
        };
    }

    private static void ValidateResult(
        ScalarPromotionReviewResult result,
        ICollection<ScalarPromotionReviewVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.scalar_promotion_review.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.scalar_promotion_review.v1.", null));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable scalar promotion review packet.", null));
        }

        Require(result.PromotionPath, "promotion_path_missing", "promotion_path is required.", issues, null);
        ValidateDecisionState(result.DecisionState, "decision_state_invalid", "decision_state is invalid.", issues, null);

        if (result.ReviewCandidateCount != result.CandidateReviews.Count)
        {
            issues.Add(Error("review_candidate_count_mismatch", "review_candidate_count must match candidate_reviews length.", null));
        }

        ValidateCount(result, "ready_for_manual_truth_review", result.ReadyForManualTruthReviewCount, "ready_for_manual_truth_review_count_mismatch", issues);
        ValidateCount(result, "blocked_conflict", result.BlockedConflictCount, "blocked_conflict_count_mismatch", issues);
        ValidateCount(result, "needs_more_corroboration", result.NeedsMoreCorroborationCount, "needs_more_corroboration_count_mismatch", issues);
        ValidateCount(result, "needs_repeat_capture", result.NeedsRepeatCaptureCount, "needs_repeat_capture_count_mismatch", issues);
        ValidateCount(result, "do_not_promote", result.DoNotPromoteCount, "do_not_promote_count_mismatch", issues);

        if (!result.Warnings.Contains("scalar_promotion_review_is_not_final_truth_without_manual_confirmation", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include scalar_promotion_review_is_not_final_truth_without_manual_confirmation.", null));
        }

        var expectedDecisionState = OverallDecisionState(
            result.BlockedConflictCount,
            result.DoNotPromoteCount,
            result.NeedsRepeatCaptureCount,
            result.NeedsMoreCorroborationCount,
            result.ReadyForManualTruthReviewCount);
        if (!string.Equals(result.DecisionState, expectedDecisionState, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("decision_state_count_mismatch", $"decision_state must be {expectedDecisionState} for the supplied candidate counts.", null));
        }

        for (var index = 0; index < result.CandidateReviews.Count; index++)
        {
            ValidateCandidate(result.CandidateReviews[index], index, issues);
        }
    }

    private static void ValidateCandidate(
        ScalarPromotionReviewCandidate candidate,
        int index,
        ICollection<ScalarPromotionReviewVerificationIssue> issues)
    {
        if (!string.Equals(candidate.SchemaVersion, "riftscan.scalar_promotion_review_candidate.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("candidate_schema_version_invalid", "candidate schema_version must be riftscan.scalar_promotion_review_candidate.v1.", index));
        }

        Require(candidate.ReviewCandidateId, "review_candidate_id_missing", "review_candidate_id is required.", issues, index);
        Require(candidate.SourcePromotionCandidateId, "source_promotion_candidate_id_missing", "source_promotion_candidate_id is required.", issues, index);
        Require(candidate.SourceRecoveredCandidateId, "source_recovered_candidate_id_missing", "source_recovered_candidate_id is required.", issues, index);
        Require(candidate.BaseAddressHex, "candidate_base_address_missing", "base_address_hex is required.", issues, index);
        Require(candidate.OffsetHex, "candidate_offset_missing", "offset_hex is required.", issues, index);
        Require(candidate.DataType, "candidate_data_type_missing", "data_type is required.", issues, index);
        Require(candidate.Classification, "candidate_classification_missing", "classification is required.", issues, index);
        Require(candidate.SourcePromotionStatus, "candidate_source_promotion_status_missing", "source_promotion_status is required.", issues, index);
        Require(candidate.SourceTruthReadiness, "candidate_source_truth_readiness_missing", "source_truth_readiness is required.", issues, index);
        Require(candidate.SourceClaimLevel, "candidate_source_claim_level_missing", "source_claim_level is required.", issues, index);
        Require(candidate.SourceCorroborationStatus, "candidate_source_corroboration_status_missing", "source_corroboration_status is required.", issues, index);
        Require(candidate.DecisionReason, "candidate_decision_reason_missing", "decision_reason is required.", issues, index);
        Require(candidate.EvidenceSummary, "candidate_evidence_summary_missing", "evidence_summary is required.", issues, index);
        Require(candidate.NextAction, "candidate_next_action_missing", "next_action is required.", issues, index);
        ValidateDecisionState(candidate.DecisionState, "candidate_decision_state_invalid", "candidate decision_state is invalid.", issues, index);

        if (candidate.FinalTruthClaim)
        {
            issues.Add(Error("final_truth_claim_forbidden", "scalar promotion review candidates must not assert final_truth_claim=true.", index));
        }

        if (!candidate.ManualConfirmationRequired)
        {
            issues.Add(Error("manual_confirmation_required_missing", "manual_confirmation_required must remain true.", index));
        }

        if ((Is(candidate.SourcePromotionStatus, "blocked_conflict") || Is(candidate.SourceCorroborationStatus, "conflicted")) &&
            !Is(candidate.DecisionState, "blocked_conflict"))
        {
            issues.Add(Error("candidate_conflict_hidden", "conflicted source candidates must remain blocked_conflict in review.", index));
        }

        if (Is(candidate.DecisionState, "ready_for_manual_truth_review"))
        {
            if (!Is(candidate.SourcePromotionStatus, "corroborated_candidate") ||
                !Is(candidate.SourceTruthReadiness, "corroborated_candidate") ||
                !Is(candidate.SourceCorroborationStatus, "corroborated"))
            {
                issues.Add(Error("ready_candidate_source_state_invalid", "ready candidates must come from corroborated scalar promotion candidates.", index));
            }

            if (candidate.BlockingGaps.Count > 0)
            {
                issues.Add(Error("ready_candidate_has_blocking_gaps", "ready candidates must not contain blocking_gaps.", index));
            }
        }

        if (!Is(candidate.DecisionState, "ready_for_manual_truth_review") && candidate.BlockingGaps.Count == 0)
        {
            issues.Add(Error("blocking_gaps_missing", "non-ready candidates must include at least one blocking gap.", index));
        }
    }

    private static void ValidateCount(
        ScalarPromotionReviewResult result,
        string state,
        int expectedCount,
        string code,
        ICollection<ScalarPromotionReviewVerificationIssue> issues)
    {
        var actualCount = result.CandidateReviews.Count(candidate => Is(candidate.DecisionState, state));
        if (actualCount != expectedCount)
        {
            issues.Add(Error(code, $"{code} must match candidate_reviews decision_state counts.", null));
        }
    }

    private static string OverallDecisionState(
        int conflictCount,
        int doNotPromoteCount,
        int repeatCaptureCount,
        int moreCorroborationCount,
        int readyCount)
    {
        if (conflictCount > 0)
        {
            return "blocked_conflict";
        }

        if (doNotPromoteCount > 0)
        {
            return "do_not_promote";
        }

        if (repeatCaptureCount > 0)
        {
            return "needs_repeat_capture";
        }

        if (moreCorroborationCount > 0)
        {
            return "needs_more_corroboration";
        }

        return readyCount > 0
            ? "ready_for_manual_truth_review"
            : "do_not_promote";
    }

    private static void ValidateDecisionState(
        string value,
        string code,
        string message,
        ICollection<ScalarPromotionReviewVerificationIssue> issues,
        int? candidateIndex)
    {
        if (string.IsNullOrWhiteSpace(value) || !AllowedDecisionStates.Contains(value))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<ScalarPromotionReviewVerificationIssue> issues,
        int? candidateIndex)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
    }

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static ScalarPromotionReviewVerificationIssue Error(string code, string message, int? candidateIndex) =>
        new()
        {
            Code = code,
            Message = message,
            CandidateIndex = candidateIndex
        };
}
