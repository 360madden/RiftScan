using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class SessionComparisonNextCapturePlanGenerator
{
    public string Generate(SessionComparisonResult result, string planPath, int top = 10)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(planPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullPlanPath = Path.GetFullPath(planPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPlanPath)!);
        var plan = Build(result, top);
        File.WriteAllText(fullPlanPath, JsonSerializer.Serialize(plan, SessionJson.Options));
        return fullPlanPath;
    }

    public ComparisonNextCapturePlan Build(SessionComparisonResult result, int top = 10)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var mode = RecommendMode(result);
        return new ComparisonNextCapturePlan
        {
            SessionAId = result.SessionAId,
            SessionBId = result.SessionBId,
            RecommendedMode = mode,
            TargetRegionPriorities = BuildTargets(result, top),
            Reason = BuildReason(result),
            ExpectedSignal = BuildExpectedSignal(mode),
            StopCondition = BuildStopCondition(mode),
            Warnings = BuildWarnings(result)
        };
    }

    private static IReadOnlyList<ComparisonCaptureTarget> BuildTargets(SessionComparisonResult result, int top) =>
        result.Vec3CandidateMatches
            .OrderByDescending(match => TargetPriority(match))
            .ThenByDescending(match => Math.Min(match.SessionARankScore, match.SessionBRankScore))
            .ThenBy(match => match.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select(match => new ComparisonCaptureTarget
            {
                BaseAddressHex = match.BaseAddressHex,
                OffsetHex = match.OffsetHex,
                DataType = match.DataType,
                SessionARegionId = match.SessionARegionId,
                SessionBRegionId = match.SessionBRegionId,
                SessionACandidateId = match.SessionACandidateId,
                SessionBCandidateId = match.SessionBCandidateId,
                PriorityScore = TargetPriority(match),
                Reason = match.Recommendation
            })
            .ToArray();

    private static double TargetPriority(Vec3CandidateComparison match)
    {
        if (match.Recommendation.Contains("behavior_contrast", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (string.IsNullOrWhiteSpace(match.SessionAStimulusLabel) ^ string.IsNullOrWhiteSpace(match.SessionBStimulusLabel))
        {
            return 85;
        }

        if (string.Equals(match.SessionAValidationStatus, "behavior_consistent_candidate", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(match.SessionBValidationStatus, "behavior_consistent_candidate", StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        return Math.Min(match.SessionARankScore, match.SessionBRankScore);
    }

    private static string RecommendMode(SessionComparisonResult result)
    {
        if (result.Vec3BehaviorSummary.BehaviorContrastCount > 0)
        {
            return "review_existing_behavior_contrast";
        }

        if (result.Vec3BehaviorSummary.UnlabeledMatchCount > 0)
        {
            return HasPassiveLabel(result) && !HasMoveForwardLabel(result)
                ? "capture_labeled_move_forward"
                : "recapture_with_explicit_stimulus_labels";
        }

        if (result.Vec3BehaviorSummary.BehaviorConsistentMatchCount > 0)
        {
            return HasMoveForwardLabel(result)
                ? "capture_labeled_passive_idle"
                : "capture_labeled_move_forward";
        }

        return result.MatchingVec3CandidateCount > 0
            ? "capture_labeled_contrast_session"
            : "capture_more_labeled_passive_samples";
    }

    private static string BuildReason(SessionComparisonResult result)
    {
        if (result.Vec3BehaviorSummary.BehaviorContrastCount > 0)
        {
            return "comparison_already_contains_behavior_contrast_candidates";
        }

        if (result.Vec3BehaviorSummary.UnlabeledMatchCount > 0)
        {
            return "matching_vec3_candidates_include_unlabeled_sessions";
        }

        if (result.Vec3BehaviorSummary.BehaviorConsistentMatchCount > 0)
        {
            return "vec3_candidates_are_behavior_consistent_but_need_contrast";
        }

        return result.MatchingVec3CandidateCount > 0
            ? "matching_vec3_candidates_need_labeled_behavior_evidence"
            : "no_matching_vec3_candidates_for_behavior_comparison";
    }

    private static string BuildExpectedSignal(string mode) =>
        mode switch
        {
            "review_existing_behavior_contrast" => "existing_vec3_candidates_show_stimulus_specific_delta",
            "capture_labeled_move_forward" => "target_vec3_candidates_change_during_move_forward",
            "capture_labeled_passive_idle" => "target_vec3_candidates_remain_stable_during_passive_idle",
            "recapture_with_explicit_stimulus_labels" => "previous_matches_gain_explicit_stimulus_labels",
            "capture_labeled_contrast_session" => "same_base_offsets_can_be_compared_across_contrasting_labels",
            _ => "more_labeled_snapshots_create_replayable_candidate_evidence"
        };

    private static string BuildStopCondition(string mode) =>
        mode switch
        {
            "review_existing_behavior_contrast" => "candidate_reviewed_before_truth_claim",
            "capture_labeled_move_forward" => "at_least_one_matching_vec3_candidate_has_move_forward_delta",
            "capture_labeled_passive_idle" => "at_least_one_matching_vec3_candidate_has_passive_stability",
            "recapture_with_explicit_stimulus_labels" => "comparison_has_no_unlabeled_vec3_matches",
            "capture_labeled_contrast_session" => "comparison_emits_behavior_contrast_or_rejection",
            _ => "new_session_verifies_and_analyzes_successfully"
        };

    private static IReadOnlyList<string> BuildWarnings(SessionComparisonResult result)
    {
        var warnings = new List<string>(result.Warnings);
        warnings.Add("next_capture_plan_is_recommendation_not_truth_claim");
        if (result.Vec3BehaviorSummary.UnlabeledMatchCount > 0)
        {
            warnings.Add("unlabeled_matches_block_behavior_claims");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool HasPassiveLabel(SessionComparisonResult result) =>
        result.Vec3BehaviorSummary.StimulusLabels.Any(label =>
            string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase));

    private static bool HasMoveForwardLabel(SessionComparisonResult result) =>
        result.Vec3BehaviorSummary.StimulusLabels.Any(label =>
            string.Equals(label, "move_forward", StringComparison.OrdinalIgnoreCase));
}
