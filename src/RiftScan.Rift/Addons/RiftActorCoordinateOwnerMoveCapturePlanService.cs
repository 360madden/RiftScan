using System.Globalization;
using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerMoveCapturePlanService
{
    public RiftActorCoordinateOwnerMoveCapturePlanResult Plan(RiftActorCoordinateOwnerMoveCapturePlanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.HypothesesPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.IntervalMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxBytesPerRegion);
        ArgumentOutOfRangeException.ThrowIfNegative(options.InterventionWaitMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.InterventionPollMilliseconds);

        var hypothesesPath = Path.GetFullPath(options.HypothesesPath);
        var verification = new RiftActorCoordinateOwnerPathHypothesesVerifier().Verify(hypothesesPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Actor-coordinate owner path hypotheses verification failed: {issues}");
        }

        var targetRegionBases = new[]
        {
            verification.TargetRegionBaseHex,
            verification.OwnerCandidateRegionBaseHex
        }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var maxTotalBytes = checked((long)targetRegionBases.Length * options.Samples * options.MaxBytesPerRegion);
        var capturePlan = BuildCapturePlan(verification, targetRegionBases);
        var capturePlanPath = string.IsNullOrWhiteSpace(options.OutputPath)
            ? null
            : Path.GetFullPath(options.OutputPath);
        if (!string.IsNullOrWhiteSpace(capturePlanPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(capturePlanPath)!);
            File.WriteAllText(capturePlanPath, JsonSerializer.Serialize(capturePlan, SessionJson.Options));
        }

        var recommendedCaptureArgs = BuildRecommendedCaptureArgs(capturePlanPath, options, targetRegionBases, maxTotalBytes);
        return new RiftActorCoordinateOwnerMoveCapturePlanResult
        {
            Success = true,
            HypothesesPath = hypothesesPath,
            HypothesesVerificationPath = verification.Path,
            SessionId = verification.SessionId,
            TargetRegionBaseHex = verification.TargetRegionBaseHex,
            OwnerCandidateRegionBaseHex = verification.OwnerCandidateRegionBaseHex,
            TargetRegionCount = targetRegionBases.Length,
            TargetRegionBases = targetRegionBases,
            Samples = options.Samples,
            IntervalMilliseconds = options.IntervalMilliseconds,
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            InterventionWaitMilliseconds = options.InterventionWaitMilliseconds,
            InterventionPollMilliseconds = options.InterventionPollMilliseconds,
            CapturePlanPath = capturePlanPath,
            CapturePlan = capturePlan,
            RecommendedCaptureArgs = recommendedCaptureArgs,
            RecommendedCaptureCommandTemplate = "riftscan " + string.Join(' ', recommendedCaptureArgs.Select(QuoteIfNeeded)),
            Warnings =
            [
                "move_capture_plan_is_capture_scaffolding_not_truth",
                "do_not_run_or_label_move_forward_capture_unless_real_player_movement_occurs",
                "no_game_window_input_is_performed_by_this_plan"
            ],
            Diagnostics =
            [
                "generated_from_verified_actor_coordinate_owner_path_hypotheses",
                "targets_combined_coordinate_region_and_owner_candidate_region_bases",
                "post_capture_expected_followup_is_passive_vs_move_xref_and_addon_motion_comparison"
            ]
        };
    }

    private static ComparisonNextCapturePlan BuildCapturePlan(
        RiftActorCoordinateOwnerPathHypothesesVerificationResult verification,
        IReadOnlyList<string> targetRegionBases)
    {
        var targets = targetRegionBases
            .Select((baseAddressHex, index) => new ComparisonCaptureTarget
            {
                BaseAddressHex = baseAddressHex,
                OffsetHex = "0x0",
                DataType = index == 0
                    ? "actor_coordinate_target_region_base"
                    : "actor_coordinate_owner_candidate_region_base",
                PriorityScore = index == 0 ? 100 : 95,
                Reason = index == 0
                    ? "coordinate_target_region_for_move_forward_contrast"
                    : "owner_candidate_region_for_base_anchor_contrast"
            })
            .ToArray();

        return new ComparisonNextCapturePlan
        {
            SessionAId = verification.SessionId,
            SessionBId = "<new-controlled-move-forward-session>",
            RecommendedMode = "controlled_move_forward_combined_target_owner_capture",
            TargetRegionPriorities = targets,
            Reason = "verified_passive_owner_path_hypotheses_require_behavior_contrast",
            ExpectedSignal = "stable_base_edge_remains_while_one_coordinate_family_moves_with_addon_player_delta_and_separates_from_mirrors",
            StopCondition = "one_candidate_path_has_addon_match_behavior_stable_xref_support_and_no_conflicting_mirror_family_evidence",
            Warnings =
            [
                "next_capture_plan_is_recommendation_not_truth_claim",
                "actor_coordinate_owner_not_promoted_without_movement_addon_contrast",
                "operator_must_produce_real_movement_for_move_forward_label"
            ]
        };
    }

    private static IReadOnlyList<string> BuildRecommendedCaptureArgs(
        string? capturePlanPath,
        RiftActorCoordinateOwnerMoveCapturePlanOptions options,
        IReadOnlyList<string> targetRegionBases,
        long maxTotalBytes)
    {
        var args = new List<string>
        {
            "capture",
            "passive",
            "--pid",
            "<rift_pid>",
            "--process",
            "rift_x64",
            "--out",
            "sessions/<new-actor-coordinate-owner-move-forward>",
            "--samples",
            options.Samples.ToString(CultureInfo.InvariantCulture),
            "--interval-ms",
            options.IntervalMilliseconds.ToString(CultureInfo.InvariantCulture),
            "--base-addresses",
            string.Join(',', targetRegionBases),
            "--max-regions",
            targetRegionBases.Count.ToString(CultureInfo.InvariantCulture),
            "--max-bytes-per-region",
            options.MaxBytesPerRegion.ToString(CultureInfo.InvariantCulture),
            "--max-total-bytes",
            maxTotalBytes.ToString(CultureInfo.InvariantCulture),
            "--stimulus",
            "move_forward",
            "--stimulus-note",
            "controlled_move_forward_combined_target_owner_real_movement_required",
            "--intervention-wait-ms",
            options.InterventionWaitMilliseconds.ToString(CultureInfo.InvariantCulture),
            "--intervention-poll-ms",
            options.InterventionPollMilliseconds.ToString(CultureInfo.InvariantCulture)
        };

        return args;
    }

    private static string QuoteIfNeeded(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;
}
