using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarPromotionReviewService
{
    public ScalarPromotionReviewResult Review(string scalarTruthPromotionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarTruthPromotionPath);

        var promotionPath = Path.GetFullPath(scalarTruthPromotionPath);
        var promotion = ReadPromotion(promotionPath);
        var reviews = promotion.PromotedCandidates
            .Concat(promotion.BlockedCandidates)
            .Select(BuildReviewCandidate)
            .Select((candidate, index) => candidate with { ReviewCandidateId = $"scalar-promotion-review-{index + 1:000000}" })
            .ToArray();

        var readyCount = CountByState(reviews, "ready_for_manual_truth_review");
        var conflictCount = CountByState(reviews, "blocked_conflict");
        var moreCorroborationCount = CountByState(reviews, "needs_more_corroboration");
        var repeatCaptureCount = CountByState(reviews, "needs_repeat_capture");
        var doNotPromoteCount = CountByState(reviews, "do_not_promote");

        return new ScalarPromotionReviewResult
        {
            Success = true,
            PromotionPath = promotionPath,
            DecisionState = OverallDecisionState(conflictCount, doNotPromoteCount, repeatCaptureCount, moreCorroborationCount, readyCount),
            ReviewCandidateCount = reviews.Length,
            ReadyForManualTruthReviewCount = readyCount,
            BlockedConflictCount = conflictCount,
            NeedsMoreCorroborationCount = moreCorroborationCount,
            NeedsRepeatCaptureCount = repeatCaptureCount,
            DoNotPromoteCount = doNotPromoteCount,
            CandidateReviews = reviews,
            Warnings = BuildWarnings(readyCount, conflictCount, moreCorroborationCount, repeatCaptureCount, doNotPromoteCount)
        };
    }

    private static ScalarTruthPromotionResult ReadPromotion(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Scalar truth promotion file does not exist.", path);
        }

        var verification = new ScalarTruthPromotionVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar truth promotion verification failed: {issues}");
        }

        return JsonSerializer.Deserialize<ScalarTruthPromotionResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize scalar truth promotion packet.");
    }

    private static ScalarPromotionReviewCandidate BuildReviewCandidate(ScalarPromotedTruthCandidate candidate)
    {
        var decision = Decide(candidate);
        return new ScalarPromotionReviewCandidate
        {
            SourcePromotionCandidateId = candidate.CandidateId,
            SourceRecoveredCandidateId = candidate.SourceRecoveredCandidateId,
            BaseAddressHex = candidate.BaseAddressHex,
            OffsetHex = candidate.OffsetHex,
            DataType = candidate.DataType,
            Classification = candidate.Classification,
            SourcePromotionStatus = candidate.PromotionStatus,
            SourceTruthReadiness = candidate.TruthReadiness,
            SourceClaimLevel = candidate.ClaimLevel,
            SourceCorroborationStatus = candidate.CorroborationStatus,
            BestScoreTotal = candidate.BestScoreTotal,
            SupportingFileCount = candidate.SupportingFileCount,
            SupportingTruthCandidateIds = candidate.SupportingTruthCandidateIds,
            CorroborationSources = candidate.CorroborationSources,
            DecisionState = decision.DecisionState,
            DecisionReason = decision.DecisionReason,
            BlockingGaps = decision.BlockingGaps,
            EvidenceSummary = candidate.EvidenceSummary,
            NextAction = decision.NextAction,
            ManualConfirmationRequired = true,
            FinalTruthClaim = false
        };
    }

    private static ReviewDecision Decide(ScalarPromotedTruthCandidate candidate)
    {
        if (Is(candidate.PromotionStatus, "blocked_conflict") || Is(candidate.CorroborationStatus, "conflicted"))
        {
            return new ReviewDecision(
                "blocked_conflict",
                "external_or_addon_corroboration_conflicts_with_recovered_candidate",
                ["resolve_corroboration_conflict_before_any_truth_claim"],
                "resolve_conflict_then_repeat_review");
        }

        if (candidate.BestScoreTotal is < 0 or > 100 || double.IsNaN(candidate.BestScoreTotal) || double.IsInfinity(candidate.BestScoreTotal))
        {
            return new ReviewDecision(
                "do_not_promote",
                "candidate_score_is_invalid",
                ["invalid_score"],
                "discard_or_rebuild_candidate_evidence");
        }

        var repeatCaptureGaps = new List<string>();
        if (candidate.SupportingFileCount < 2)
        {
            repeatCaptureGaps.Add("supporting_file_count_below_2");
        }

        if (candidate.SupportingTruthCandidateIds.Count < 2)
        {
            repeatCaptureGaps.Add("supporting_truth_candidate_ids_below_2");
        }

        if (repeatCaptureGaps.Count > 0)
        {
            return new ReviewDecision(
                "needs_repeat_capture",
                "repeat_recovery_evidence_is_insufficient_for_manual_truth_review",
                repeatCaptureGaps,
                "repeat_independent_capture_and_recovery");
        }

        if (Is(candidate.PromotionStatus, "recovered_candidate") || !Is(candidate.CorroborationStatus, "corroborated"))
        {
            return new ReviewDecision(
                "needs_more_corroboration",
                "recovered_candidate_needs_matching_external_or_addon_corroboration",
                ["matching_corroboration_missing"],
                "add_or_fix_external_corroboration_then_rerun_scalar_promotion");
        }

        if (Is(candidate.PromotionStatus, "corroborated_candidate") &&
            Is(candidate.TruthReadiness, "corroborated_candidate") &&
            Is(candidate.CorroborationStatus, "corroborated"))
        {
            return new ReviewDecision(
                "ready_for_manual_truth_review",
                "recovered_candidate_is_repeated_and_externally_corroborated",
                [],
                "manual_review_required_before_final_truth_claim");
        }

        return new ReviewDecision(
            "do_not_promote",
            "candidate_state_does_not_match_any_safe_review_path",
            ["unsupported_promotion_state"],
            "inspect_promotion_packet_and_rebuild_if_needed");
    }

    private static int CountByState(IEnumerable<ScalarPromotionReviewCandidate> candidates, string state) =>
        candidates.Count(candidate => Is(candidate.DecisionState, state));

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

    private static IReadOnlyList<string> BuildWarnings(
        int readyCount,
        int conflictCount,
        int moreCorroborationCount,
        int repeatCaptureCount,
        int doNotPromoteCount)
    {
        var warnings = new List<string>
        {
            "scalar_promotion_review_is_not_final_truth_without_manual_confirmation",
            "manual_review_packet_preserves_conflicts_and_gaps"
        };

        if (readyCount > 0)
        {
            warnings.Add("ready_candidates_still_require_manual_truth_review");
        }

        if (conflictCount > 0)
        {
            warnings.Add("scalar_promotion_review_contains_blocked_conflicts");
        }

        if (moreCorroborationCount > 0)
        {
            warnings.Add("scalar_promotion_review_contains_uncorroborated_candidates");
        }

        if (repeatCaptureCount > 0)
        {
            warnings.Add("scalar_promotion_review_requires_repeat_capture");
        }

        if (doNotPromoteCount > 0)
        {
            warnings.Add("scalar_promotion_review_contains_do_not_promote_candidates");
        }

        if (readyCount == 0)
        {
            warnings.Add("no_candidates_ready_for_manual_truth_review");
        }

        return warnings;
    }

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private sealed record ReviewDecision(
        string DecisionState,
        string DecisionReason,
        IReadOnlyList<string> BlockingGaps,
        string NextAction);
}
