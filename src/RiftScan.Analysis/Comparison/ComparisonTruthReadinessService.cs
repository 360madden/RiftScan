using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ComparisonTruthReadinessService
{
    public ComparisonTruthReadinessResult Build(SessionComparisonResult result, int top = 25)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var capturePlan = new SessionComparisonNextCapturePlanGenerator().Build(result, top);
        var entityLayout = BuildEntityLayoutStatus(result);
        var position = BuildPositionStatus(result);
        var actorYaw = BuildActorYawStatus(result.ScalarBehaviorSummary);
        var cameraOrientation = BuildCameraOrientationStatus(result.ScalarBehaviorSummary);

        return new ComparisonTruthReadinessResult
        {
            Success = true,
            SessionAId = result.SessionAId,
            SessionBId = result.SessionBId,
            SessionAPath = result.SessionAPath,
            SessionBPath = result.SessionBPath,
            EntityLayout = entityLayout,
            Position = position,
            ActorYaw = actorYaw,
            CameraOrientation = cameraOrientation,
            NextRequiredCapture = BuildNextCaptureRequirement(capturePlan, top),
            TopEntityLayoutMatches = result.EntityLayoutMatches
                .OrderByDescending(match => StableEntityLayoutCount([match]))
                .ThenByDescending(match => Math.Min(match.SessionAScore, match.SessionBScore))
                .ThenByDescending(match => match.OverlapBytes)
                .Take(top)
                .ToArray(),
            TopVec3BehaviorCandidates = result.Vec3BehaviorSummary.BehaviorContrastCandidates
                .OrderByDescending(candidate => candidate.ScoreTotal)
                .Take(top)
                .ToArray(),
            TopScalarBehaviorCandidates = result.ScalarBehaviorSummary.ScalarBehaviorCandidates
                .OrderByDescending(candidate => candidate.ScoreTotal)
                .Take(top)
                .ToArray(),
            Warnings = BuildWarnings(result, entityLayout, position, actorYaw, cameraOrientation)
        };
    }

    public string Write(SessionComparisonResult result, string outputPath, int top = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        var readiness = Build(result, top) with { OutputPath = fullOutputPath };
        File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(readiness, SessionJson.Options));
        return fullOutputPath;
    }

    private static ComparisonTruthReadinessStatus BuildEntityLayoutStatus(SessionComparisonResult result)
    {
        var stableCount = StableEntityLayoutCount(result.EntityLayoutMatches);
        if (stableCount > 0)
        {
            return new ComparisonTruthReadinessStatus
            {
                Component = "entity_layout",
                Readiness = "strong_candidate",
                EvidenceCount = stableCount,
                ConfidenceScore = ClampScore(60 + (stableCount * 10)),
                PrimaryReason = "stable_entity_layout_candidates_exist_across_sessions",
                NextAction = "capture_labeled_behavior_against_stable_entity_layout_targets"
            };
        }

        if (result.MatchingEntityLayoutCount > 0)
        {
            return new ComparisonTruthReadinessStatus
            {
                Component = "entity_layout",
                Readiness = "candidate",
                EvidenceCount = result.MatchingEntityLayoutCount,
                ConfidenceScore = ClampScore(35 + result.MatchingEntityLayoutCount),
                PrimaryReason = "overlapping_entity_layout_candidates_exist_but_need_stronger_cross_session_support",
                NextAction = "compare_more_passive_sessions_or_capture_labeled_followup",
                BlockingGaps = ["missing_stable_entity_layout_recommendation"]
            };
        }

        return new ComparisonTruthReadinessStatus
        {
            Component = "entity_layout",
            Readiness = "missing",
            PrimaryReason = "no_entity_layout_matches_in_comparison",
            NextAction = "capture_wider_passive_windowed_followup",
            BlockingGaps = ["missing_entity_layout_candidates"]
        };
    }

    private static ComparisonTruthReadinessStatus BuildPositionStatus(SessionComparisonResult result)
    {
        var candidates = result.Vec3BehaviorSummary.BehaviorContrastCandidates;
        if (candidates.Count > 0)
        {
            return new ComparisonTruthReadinessStatus
            {
                Component = "position",
                Readiness = candidates.Any(candidate => string.Equals(candidate.ConfidenceLevel, "strong_candidate", StringComparison.OrdinalIgnoreCase))
                    ? "strong_candidate"
                    : "candidate",
                EvidenceCount = candidates.Count,
                ConfidenceScore = ClampScore(candidates.Max(candidate => candidate.ScoreTotal)),
                PrimaryReason = "vec3_behavior_contrast_candidates_exist",
                NextAction = "validate_against_addon_truth_or_repeat_move_forward_contrast"
            };
        }

        if (result.MatchingVec3CandidateCount > 0)
        {
            return new ComparisonTruthReadinessStatus
            {
                Component = "position",
                Readiness = "candidate_needs_labeled_contrast",
                EvidenceCount = result.MatchingVec3CandidateCount,
                ConfidenceScore = ClampScore(30 + Math.Min(result.MatchingVec3CandidateCount, 30)),
                PrimaryReason = "matching_vec3_candidates_exist_without_behavior_contrast",
                NextAction = result.Vec3BehaviorSummary.NextRecommendedAction,
                BlockingGaps = ["missing_passive_vs_move_forward_behavior_contrast"]
            };
        }

        return new ComparisonTruthReadinessStatus
        {
            Component = "position",
            Readiness = "missing",
            PrimaryReason = "no_matching_vec3_candidates",
            NextAction = "capture_labeled_entity_layout_followup_or_wider_passive_samples",
            BlockingGaps = ["missing_vec3_candidate_matches"]
        };
    }

    private static ComparisonTruthReadinessStatus BuildActorYawStatus(ScalarBehaviorSummary summary)
    {
        var actorYawCandidates = summary.ScalarBehaviorCandidates
            .Where(candidate => candidate.Classification.Contains("actor_yaw", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (actorYawCandidates.Length > 0)
        {
            return BuildScalarStatus(
                "actor_yaw",
                actorYawCandidates,
                "actor_yaw_scalar_behavior_candidates_exist",
                "capture_repeat_turn_or_validate_against_addon_truth");
        }

        var turnResponsiveCandidates = summary.ScalarBehaviorCandidates
            .Where(candidate => candidate.Classification.Contains("turn_responsive", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (turnResponsiveCandidates.Length > 0)
        {
            return new ComparisonTruthReadinessStatus
            {
                Component = "actor_yaw",
                Readiness = "candidate_needs_camera_only_separation",
                EvidenceCount = turnResponsiveCandidates.Length,
                ConfidenceScore = ClampScore(turnResponsiveCandidates.Max(candidate => candidate.ScoreTotal)),
                PrimaryReason = "turn_responsive_scalar_candidates_exist_but_camera_separation_missing",
                NextAction = "capture_labeled_camera_only_session",
                BlockingGaps = ["missing_camera_only_separation_for_actor_yaw"]
            };
        }

        return BuildMissingScalarStatus(
            "actor_yaw",
            "missing_actor_yaw_scalar_behavior_evidence",
            summary.NextRecommendedAction);
    }

    private static ComparisonTruthReadinessStatus BuildCameraOrientationStatus(ScalarBehaviorSummary summary)
    {
        var cameraCandidates = summary.ScalarBehaviorCandidates
            .Where(candidate => candidate.Classification.Contains("camera_orientation", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (cameraCandidates.Length > 0)
        {
            return BuildScalarStatus(
                "camera_orientation",
                cameraCandidates,
                "camera_orientation_scalar_behavior_candidates_exist",
                "capture_repeat_camera_only_or_validate_against_addon_truth");
        }

        return BuildMissingScalarStatus(
            "camera_orientation",
            "missing_camera_only_scalar_behavior_evidence",
            "capture_labeled_camera_only_session");
    }

    private static ComparisonTruthReadinessStatus BuildScalarStatus(
        string component,
        IReadOnlyList<ScalarBehaviorCandidate> candidates,
        string primaryReason,
        string fallbackNextAction)
    {
        return new ComparisonTruthReadinessStatus
        {
            Component = component,
            Readiness = candidates.Any(candidate => string.Equals(candidate.ConfidenceLevel, "strong_candidate", StringComparison.OrdinalIgnoreCase))
                ? "strong_candidate"
                : "candidate",
            EvidenceCount = candidates.Count,
            ConfidenceScore = ClampScore(candidates.Max(candidate => candidate.ScoreTotal)),
            PrimaryReason = primaryReason,
            NextAction = FirstNonEmpty(candidates.OrderByDescending(candidate => candidate.ScoreTotal).First().NextValidationStep, fallbackNextAction)
        };
    }

    private static ComparisonTruthReadinessStatus BuildMissingScalarStatus(string component, string missingGap, string fallbackNextAction) =>
        new()
        {
            Component = component,
            Readiness = "missing",
            PrimaryReason = $"no_{component}_scalar_behavior_candidates",
            NextAction = FirstNonEmpty(fallbackNextAction, "capture_labeled_turn_and_camera_only_sessions"),
            BlockingGaps = [missingGap]
        };

    private static ComparisonTruthReadinessCaptureRequirement BuildNextCaptureRequirement(ComparisonNextCapturePlan plan, int top) =>
        new()
        {
            Mode = plan.RecommendedMode,
            Reason = plan.Reason,
            ExpectedSignal = plan.ExpectedSignal,
            StopCondition = plan.StopCondition,
            TargetCount = plan.TargetRegionPriorities.Count,
            TargetPreview = plan.TargetRegionPriorities
                .Take(top)
                .Select(target => new ComparisonTruthReadinessTargetPreview
                {
                    BaseAddressHex = target.BaseAddressHex,
                    OffsetHex = target.OffsetHex,
                    DataType = target.DataType,
                    PriorityScore = target.PriorityScore,
                    Reason = target.Reason
                })
                .ToArray()
        };

    private static IReadOnlyList<string> BuildWarnings(
        SessionComparisonResult result,
        ComparisonTruthReadinessStatus entityLayout,
        ComparisonTruthReadinessStatus position,
        ComparisonTruthReadinessStatus actorYaw,
        ComparisonTruthReadinessStatus cameraOrientation)
    {
        var warnings = new List<string>(result.Warnings)
        {
            "truth_readiness_is_candidate_evidence_not_truth_claim"
        };

        foreach (var status in new[] { entityLayout, position, actorYaw, cameraOrientation })
        {
            if (!string.Equals(status.Readiness, "strong_candidate", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"{status.Component}_not_strongly_ready");
            }
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int StableEntityLayoutCount(IReadOnlyList<EntityLayoutComparison> matches) =>
        matches.Count(match => string.Equals(match.Recommendation, "stable_entity_layout_candidate_across_sessions", StringComparison.OrdinalIgnoreCase));

    private static double ClampScore(double score) =>
        Math.Round(Math.Clamp(score, 0, 100), 3);

    private static string FirstNonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
