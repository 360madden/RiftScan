using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class SessionComparisonServiceTests
{
    [Fact]
    public void Compare_sessions_matches_same_fixture_region_and_cluster()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionA.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionB.Path);

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path);

        Assert.True(result.Success);
        Assert.Equal("fixture-valid-session", result.SessionAId);
        Assert.Equal("fixture-valid-session", result.SessionBId);
        Assert.True(result.MatchingRegionCount >= 1);
        Assert.Contains(result.RegionMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.True(result.MatchingClusterCount >= 1);
        Assert.Contains(result.ClusterMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.True(result.MatchingEntityLayoutCount >= 1);
        Assert.Contains(result.EntityLayoutMatches, match =>
            match.BaseAddressHex == "0x10000000" &&
            match.SessionACandidateId.StartsWith("entity-layout-", StringComparison.Ordinal) &&
            match.SessionBCandidateId.StartsWith("entity-layout-", StringComparison.Ordinal) &&
            match.OverlapBytes > 0 &&
            match.Recommendation.Contains("entity_layout", StringComparison.Ordinal));
        Assert.True(result.MatchingStructureCandidateCount >= 1);
        Assert.Contains(result.StructureCandidateMatches, match =>
            match.BaseAddressHex == "0x10000000" &&
            match.OffsetHex == "0x0" &&
            match.SessionACandidateId == "structure-000001" &&
            match.SessionBCandidateId == "structure-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("support=", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("snapshots/*.bin") &&
            match.StructureKind == "float32_triplet" &&
            match.Recommendation == "stable_structure_candidate");
        Assert.True(result.MatchingVec3CandidateCount >= 1);
        Assert.Contains(result.Vec3CandidateMatches, match =>
            match.BaseAddressHex == "0x10000000" &&
            match.OffsetHex == "0x0" &&
            match.SessionACandidateId == "vec3-000001" &&
            match.SessionBCandidateId == "vec3-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("samples=", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("structures.jsonl") &&
            match.DataType == "vec3_float32" &&
            match.Recommendation == "stable_vec3_candidate_across_sessions");
        Assert.Contains("comparison_is_candidate_evidence_not_truth_claim", result.Warnings);
    }

    [Fact]
    public void Compare_sessions_promotes_wide_entity_layout_to_strong_readiness()
    {
        using var sessionA = CaptureEntityLayoutSession();
        using var sessionB = CaptureEntityLayoutSession();

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path, top: 100);
        var readiness = new ComparisonTruthReadinessService().Build(result);

        Assert.Contains(result.EntityLayoutMatches, match =>
            match.Recommendation == "stable_entity_layout_candidate_across_sessions" &&
            match.OverlapBytes >= 64 &&
            match.SessionAScore >= 75 &&
            match.SessionBScore >= 75);
        Assert.Equal("strong_candidate", readiness.EntityLayout.Readiness);
        Assert.Equal("stable_entity_layout_candidates_exist_across_sessions", readiness.EntityLayout.PrimaryReason);
        Assert.DoesNotContain("entity_layout_not_strongly_ready", readiness.Warnings);
    }

    [Fact]
    public void Compare_sessions_matches_typed_value_candidates_by_base_offset_and_type()
    {
        using var sessionA = CaptureChangingFloatSession();
        using var sessionB = CaptureChangingFloatSession();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionA.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionB.Path);

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path);

        Assert.True(result.Success);
        Assert.True(result.MatchingValueCandidateCount >= 1);
        Assert.Contains(result.ValueCandidateMatches, match =>
            match.BaseAddressHex == "0x1000" &&
            match.OffsetHex == "0x4" &&
            match.DataType == "float32" &&
            match.SessionACandidateId == "value-000001" &&
            match.SessionBCandidateId == "value-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("samples=3", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("deltas.jsonl") &&
            match.Recommendation == "stable_typed_value_lane_candidate");
    }

    [Fact]
    public void Compare_sessions_reports_scalar_behavior_candidates_for_turn_labels()
    {
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(turnLeft.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(turnRight.Path);

        var result = new SessionComparisonService().Compare(turnLeft.Path, turnRight.Path);

        var valueMatch = Assert.Single(result.ValueCandidateMatches, match => match.BaseAddressHex == "0x1000" && match.OffsetHex == "0x4");
        Assert.Equal("turn_left", valueMatch.SessionAStimulusLabel);
        Assert.Equal("turn_right", valueMatch.SessionBStimulusLabel);
        Assert.True(valueMatch.SessionAChangedSampleCount > 0);
        Assert.True(valueMatch.SessionBChangedSampleCount > 0);

        Assert.True(result.ScalarBehaviorSummary.MatchingScalarCandidateCount >= 1);
        Assert.True(result.ScalarBehaviorSummary.HeuristicCandidateCount >= 1);
        Assert.True(result.ScalarBehaviorSummary.StrongCandidateCount >= 1);
        Assert.Equal(["turn_left", "turn_right"], result.ScalarBehaviorSummary.StimulusLabels);
        Assert.Equal("compare_turn_candidates_against_camera_only_session", result.ScalarBehaviorSummary.NextRecommendedAction);
        var scalar = Assert.Single(result.ScalarBehaviorSummary.ScalarBehaviorCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("turn_responsive_angle_scalar_candidate", scalar.Classification);
        Assert.Equal("float32", scalar.DataType);
        Assert.Equal("angle_radians_0_to_2pi", scalar.ValueFamily);
        Assert.True(scalar.SessionACircularDeltaMagnitude > 0);
        Assert.True(scalar.SessionBCircularDeltaMagnitude > 0);
        Assert.True(scalar.SessionASignedCircularDelta > 0);
        Assert.True(scalar.SessionBSignedCircularDelta < 0);
        Assert.Equal("positive", scalar.SessionADominantDirection);
        Assert.Equal("negative", scalar.SessionBDominantDirection);
        Assert.Equal("opposite_signed_turn_directions", scalar.TurnPolarityRelationship);
        Assert.True(scalar.ScoreTotal >= 80);
        Assert.Equal("strong_candidate", scalar.ConfidenceLevel);
        Assert.Contains("turn_stimulus_label_present", scalar.SupportingReasons);
        Assert.Contains("angle_radian_value_range", scalar.SupportingReasons);
        Assert.Contains("directional_circular_delta_under_turn_or_camera", scalar.SupportingReasons);
        Assert.Contains("opposite_turn_polarity_supported", scalar.SupportingReasons);
        Assert.Contains("scalar_changed_under_turn_or_camera_label", scalar.SupportingReasons);
        Assert.DoesNotContain("passive_session_also_changed_scalar", scalar.RejectionReasons);
        Assert.Equal("compare_against_opposite_turn_and_camera_only_sessions", scalar.NextValidationStep);

        var readiness = new ComparisonTruthReadinessService().Build(result);

        Assert.Equal("candidate_needs_camera_only_separation", readiness.ActorYaw.Readiness);
        Assert.Contains("missing_camera_only_separation_for_actor_yaw", readiness.ActorYaw.BlockingGaps);
    }

    [Fact]
    public void Compare_sessions_uses_stable_scalar_baselines_for_passive_to_turn_contrast()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(passive.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(turnLeft.Path);

        var result = new SessionComparisonService().Compare(passive.Path, turnLeft.Path);

        Assert.True(result.MatchingValueCandidateCount == 0);
        Assert.True(result.ScalarBehaviorSummary.MatchingScalarCandidateCount >= 1);
        var scalar = Assert.Single(result.ScalarBehaviorSummary.ScalarBehaviorCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("turn_responsive_angle_scalar_candidate", scalar.Classification);
        Assert.Equal("scalar-000001", scalar.SessionACandidateId);
        Assert.Equal("scalar-000001", scalar.SessionBCandidateId);
        Assert.Equal("passive_idle", scalar.SessionAStimulusLabel);
        Assert.Equal("turn_left", scalar.SessionBStimulusLabel);
        Assert.Equal("angle_radians_0_to_2pi", scalar.ValueFamily);
        Assert.Equal(0, scalar.SessionAChangedSampleCount);
        Assert.True(scalar.SessionBChangedSampleCount > 0);
        Assert.Equal(0, scalar.SessionACircularDeltaMagnitude);
        Assert.True(scalar.SessionBCircularDeltaMagnitude > 0);
        Assert.True(scalar.SessionBSignedCircularDelta > 0);
        Assert.Equal(string.Empty, scalar.TurnPolarityRelationship);
        Assert.True(scalar.ScoreTotal >= 80);
        Assert.Equal("strong_candidate", scalar.ConfidenceLevel);
        Assert.Contains("turn_stimulus_label_present", scalar.SupportingReasons);
        Assert.Contains("angle_radian_value_range", scalar.SupportingReasons);
        Assert.Contains("directional_circular_delta_under_turn_or_camera", scalar.SupportingReasons);
        Assert.Contains("scalar_changed_under_turn_or_camera_label", scalar.SupportingReasons);
        Assert.DoesNotContain("passive_session_also_changed_scalar", scalar.RejectionReasons);
    }

    [Fact]
    public void Compare_sessions_classifies_camera_only_angle_when_camera_changes_and_turn_is_stable()
    {
        using var camera = CaptureChangingFloatSession("camera_only");
        using var turnLeft = CaptureStableFloatSession("turn_left");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(camera.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(turnLeft.Path);

        var result = new SessionComparisonService().Compare(camera.Path, turnLeft.Path);

        var scalar = Assert.Single(result.ScalarBehaviorSummary.ScalarBehaviorCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("camera_orientation_angle_scalar_candidate", scalar.Classification);
        Assert.Equal("camera_only_changes_turn_stable", scalar.CameraTurnSeparationRelationship);
        Assert.True(scalar.SessionAChangedSampleCount > 0);
        Assert.Equal(0, scalar.SessionBChangedSampleCount);
        Assert.Contains("camera_only_separated_from_actor_turn", scalar.SupportingReasons);
        Assert.Contains("camera_only_stimulus_label_present", scalar.SupportingReasons);
        Assert.DoesNotContain("camera_and_turn_both_change_same_scalar", scalar.RejectionReasons);

        var readiness = new ComparisonTruthReadinessService().Build(result);

        Assert.Equal("camera_orientation", readiness.CameraOrientation.Component);
        Assert.Equal("strong_candidate", readiness.CameraOrientation.Readiness);
        Assert.Equal(1, readiness.CameraOrientation.EvidenceCount);
        Assert.Equal("missing", readiness.ActorYaw.Readiness);
    }

    [Fact]
    public void Compare_sessions_classifies_actor_yaw_angle_when_turn_changes_and_camera_only_is_stable()
    {
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var camera = CaptureStableFloatSession("camera_only");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(turnLeft.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(camera.Path);

        var result = new SessionComparisonService().Compare(turnLeft.Path, camera.Path);

        var scalar = Assert.Single(result.ScalarBehaviorSummary.ScalarBehaviorCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("actor_yaw_angle_scalar_candidate", scalar.Classification);
        Assert.Equal("turn_changes_camera_only_stable", scalar.CameraTurnSeparationRelationship);
        Assert.True(scalar.SessionAChangedSampleCount > 0);
        Assert.Equal(0, scalar.SessionBChangedSampleCount);
        Assert.Contains("actor_turn_separated_from_camera_only", scalar.SupportingReasons);
        Assert.Contains("turn_stimulus_label_present", scalar.SupportingReasons);
        Assert.DoesNotContain("camera_and_turn_both_change_same_scalar", scalar.RejectionReasons);

        var readiness = new ComparisonTruthReadinessService().Build(result);

        Assert.Equal("actor_yaw", readiness.ActorYaw.Component);
        Assert.Equal("strong_candidate", readiness.ActorYaw.Readiness);
        Assert.Equal(1, readiness.ActorYaw.EvidenceCount);
        Assert.Equal("missing", readiness.CameraOrientation.Readiness);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_ranks_actor_yaw_from_passive_opposite_turns_and_camera_stable()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        using var camera = CaptureStableFloatSession("camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);

        Assert.True(result.Success);
        Assert.Equal(4, result.SessionCount);
        Assert.Contains("scalar_evidence_is_candidate_evidence_not_truth_claim", result.Warnings);
        Assert.DoesNotContain("missing_opposite_turn_pair", result.Warnings);
        Assert.DoesNotContain("missing_camera_only_session", result.Warnings);
        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("actor_yaw_angle_scalar_candidate", candidate.Classification);
        Assert.Equal("validated_candidate", candidate.ConfidenceLevel);
        Assert.Equal("validated_candidate", candidate.TruthReadiness);
        Assert.True(candidate.PassiveStable);
        Assert.True(candidate.TurnLeftChanged);
        Assert.True(candidate.TurnRightChanged);
        Assert.False(candidate.CameraOnlyChanged);
        Assert.True(candidate.OppositeTurnPolarity);
        Assert.Equal("turn_changes_camera_only_stable", candidate.CameraTurnSeparation);
        Assert.True(candidate.TurnLeftSignedDelta > 0);
        Assert.True(candidate.TurnRightSignedDelta < 0);
        Assert.Contains("passive_baseline_stable", candidate.SupportingReasons);
        Assert.Contains("left_right_opposite_turn_polarity", candidate.SupportingReasons);
        Assert.Contains("actor_turn_separated_from_camera_only", candidate.SupportingReasons);
        Assert.Equal("repeat_labeled_capture_or_validate_against_addon_truth", candidate.NextValidationStep);
        Assert.Empty(result.RejectedCandidateSummaries);

        var truthOut = Path.Combine(passive.Path, "scalar_truth_candidates.jsonl");
        var exported = new ScalarTruthCandidateExporter().Export(result, truthOut);
        var truthCandidate = Assert.Single(exported);
        Assert.True(File.Exists(truthOut));
        Assert.Equal("scalar-truth-000001", truthCandidate.CandidateId);
        Assert.Equal("riftscan.scalar_evidence_set.v1", truthCandidate.SourceSchemaVersion);
        Assert.Equal("behavior_validated_candidate", truthCandidate.ValidationStatus);
        Assert.Equal("validated_candidate", truthCandidate.ClaimLevel);
        Assert.Equal("candidate_evidence_not_recovered_truth", truthCandidate.Warning);

        var repeatOut = Path.Combine(passive.Path, "scalar_truth_candidates_repeat.jsonl");
        _ = new ScalarTruthCandidateExporter().Export(result, repeatOut);
        var recovery = new ScalarTruthRecoveryService().Recover([truthOut, repeatOut]);
        var recovered = Assert.Single(recovery.RecoveredCandidates);
        Assert.Equal("scalar-recovered-000001", recovered.CandidateId);
        Assert.Equal("recovered_candidate", recovered.TruthReadiness);
        Assert.Equal("recovered_candidate", recovered.ClaimLevel);
        Assert.Equal(2, recovered.SupportingFileCount);
        Assert.Contains("repeated_truth_candidate_match", recovered.SupportingReasons);
        Assert.Equal("recovered_candidate_requires_review_before_final_truth_claim", recovered.Warning);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_can_validate_actor_yaw_and_camera_orientation_in_one_packet()
    {
        using var passive = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeft = CaptureDualScalarSession("turn_left", [1.5f, 2.5f, 3.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRight = CaptureDualScalarSession("turn_right", [3.5f, 2.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnly = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [2.0f, 3.0f, 4.0f]);

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, cameraOnly.Path]);

        var actorYaw = Assert.Single(result.RankedCandidates, candidate => candidate.OffsetHex == "0x4");
        var cameraOrientation = Assert.Single(result.RankedCandidates, candidate => candidate.OffsetHex == "0x8");
        Assert.Equal("actor_yaw_angle_scalar_candidate", actorYaw.Classification);
        Assert.Equal("validated_candidate", actorYaw.TruthReadiness);
        Assert.Equal("turn_changes_camera_only_stable", actorYaw.CameraTurnSeparation);
        Assert.Equal("camera_orientation_angle_scalar_candidate", cameraOrientation.Classification);
        Assert.Equal("validated_candidate", cameraOrientation.TruthReadiness);
        Assert.Equal("camera_only_changes_turn_stable", cameraOrientation.CameraTurnSeparation);

        var scalarEvidenceSetPath = Path.Combine(passive.Path, "combined-scalar-evidence-set.json");
        File.WriteAllText(scalarEvidenceSetPath, JsonSerializer.Serialize(result with { OutputPath = scalarEvidenceSetPath }, SessionJson.Options));
        var capability = new CapabilityStatusService().Build(scalarEvidenceSetPath: scalarEvidenceSetPath);

        Assert.Equal("validated_candidate", Assert.Single(capability.TruthComponents, component => component.Component == "actor_yaw").EvidenceReadiness);
        Assert.Equal("validated_candidate", Assert.Single(capability.TruthComponents, component => component.Component == "camera_orientation").EvidenceReadiness);
    }

    [Fact]
    public void Combined_scalar_truth_recovery_recovers_actor_yaw_and_camera_orientation_together()
    {
        using var passiveA = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeftA = CaptureDualScalarSession("turn_left", [1.5f, 2.5f, 3.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRightA = CaptureDualScalarSession("turn_right", [3.5f, 2.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnlyA = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [2.0f, 3.0f, 4.0f]);
        using var passiveB = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeftB = CaptureDualScalarSession("turn_left", [1.5f, 2.5f, 3.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRightB = CaptureDualScalarSession("turn_right", [3.5f, 2.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnlyB = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [2.0f, 3.0f, 4.0f]);
        var resultA = new ScalarEvidenceSetService().Aggregate([passiveA.Path, turnLeftA.Path, turnRightA.Path, cameraOnlyA.Path]);
        var resultB = new ScalarEvidenceSetService().Aggregate([passiveB.Path, turnLeftB.Path, turnRightB.Path, cameraOnlyB.Path]);
        var truthOutA = Path.Combine(passiveA.Path, "combined_scalar_truth_candidates.jsonl");
        var truthOutB = Path.Combine(passiveB.Path, "combined_scalar_truth_candidates.jsonl");

        _ = new ScalarTruthCandidateExporter().Export(resultA, truthOutA);
        _ = new ScalarTruthCandidateExporter().Export(resultB, truthOutB);
        var recovery = new ScalarTruthRecoveryService().Recover([truthOutA, truthOutB]);

        Assert.True(recovery.Success);
        Assert.Equal(4, recovery.InputCandidateCount);
        Assert.Equal(2, recovery.RecoveredCandidateCount);
        var recoveredActorYaw = Assert.Single(recovery.RecoveredCandidates, candidate => candidate.Classification == "actor_yaw_angle_scalar_candidate");
        var recoveredCamera = Assert.Single(recovery.RecoveredCandidates, candidate => candidate.Classification == "camera_orientation_angle_scalar_candidate");
        Assert.Equal("0x4", recoveredActorYaw.OffsetHex);
        Assert.Equal("0x8", recoveredCamera.OffsetHex);
        Assert.Equal("recovered_candidate", recoveredActorYaw.TruthReadiness);
        Assert.Equal("recovered_candidate", recoveredCamera.TruthReadiness);
        Assert.Equal(2, recoveredActorYaw.SupportingFileCount);
        Assert.Equal(2, recoveredCamera.SupportingFileCount);
        Assert.Contains("repeated_truth_candidate_match", recoveredActorYaw.SupportingReasons);
        Assert.Contains("repeated_truth_candidate_match", recoveredCamera.SupportingReasons);
        Assert.Contains("scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth", recovery.Warnings);
    }

    [Fact]
    public void Scalar_evidence_set_verifier_accepts_valid_aggregate_and_cli()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        using var camera = CaptureStableFloatSession("camera_only");
        var scalarEvidenceSet = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);
        var scalarEvidenceSetPath = Path.Combine(passive.Path, "scalar-evidence-set.json");
        File.WriteAllText(scalarEvidenceSetPath, JsonSerializer.Serialize(scalarEvidenceSet with { OutputPath = scalarEvidenceSetPath }, SessionJson.Options));

        var result = new ScalarEvidenceSetVerifier().Verify(scalarEvidenceSetPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "scalar-evidence-set", scalarEvidenceSetPath]);

        Assert.True(result.Success);
        Assert.Equal(4, result.SessionCount);
        Assert.Equal(1, result.RankedCandidateCount);
        Assert.Empty(result.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Scalar_truth_recovery_verifier_accepts_recovered_combined_packet_and_cli()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));

        var result = new ScalarTruthRecoveryVerifier().Verify(recoveryPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "scalar-truth-recovery", recoveryPath]);

        Assert.True(result.Success);
        Assert.Equal(2, result.TruthCandidatePathCount);
        Assert.Equal(4, result.InputCandidateCount);
        Assert.Equal(2, result.RecoveredCandidateCount);
        Assert.Empty(result.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Vec3_truth_recovery_verifier_accepts_recovered_packet_and_cli()
    {
        using var passive = CaptureVec3Session(new FixedVec3ProcessMemoryReader(), "passive_idle");
        using var moving = CaptureVec3Session(new MovingVec3ProcessMemoryReader(), "move_forward");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(passive.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(moving.Path);
        var comparison = new SessionComparisonService().Compare(passive.Path, moving.Path);
        var truthOut = Path.Combine(passive.Path, "vec3_truth_candidates.jsonl");
        var repeatOut = Path.Combine(passive.Path, "vec3_truth_candidates_repeat.jsonl");
        _ = new Vec3TruthCandidateExporter().Export(comparison, truthOut);
        _ = new Vec3TruthCandidateExporter().Export(comparison, repeatOut);
        var recoveryPath = Path.Combine(passive.Path, "vec3-truth-recovery.json");
        var recovery = new Vec3TruthRecoveryService().Recover([truthOut, repeatOut]) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));

        var result = new Vec3TruthRecoveryVerifier().Verify(recoveryPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "vec3-truth-recovery", recoveryPath]);

        Assert.True(result.Success);
        Assert.Equal(2, result.TruthCandidatePathCount);
        Assert.Equal(2, result.InputCandidateCount);
        Assert.Equal(1, result.RecoveredCandidateCount);
        Assert.Empty(result.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Vec3_corroboration_verifier_accepts_addon_waypoint_packet_and_cli()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var corroborationPath = WriteVec3Corroboration(temp.Path, "corroborated");

        var result = new Vec3TruthCorroborationVerifier().Verify(corroborationPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "vec3-corroboration", corroborationPath]);

        Assert.True(result.Success);
        Assert.Equal(1, result.EntryCount);
        Assert.Empty(result.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Scalar_evidence_set_verifier_rejects_missing_truth_claim_warning()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        var scalarEvidenceSet = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path]) with
        {
            Warnings = []
        };
        var scalarEvidenceSetPath = Path.Combine(passive.Path, "scalar-evidence-set-missing-warning.json");
        File.WriteAllText(scalarEvidenceSetPath, JsonSerializer.Serialize(scalarEvidenceSet with { OutputPath = scalarEvidenceSetPath }, SessionJson.Options));

        var result = new ScalarEvidenceSetVerifier().Verify(scalarEvidenceSetPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "truth_claim_warning_missing");
    }

    [Fact]
    public void Scalar_truth_recovery_verifier_rejects_missing_truth_claim_warning()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { Warnings = [] };
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery-missing-warning.json");
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery with { OutputPath = recoveryPath }, SessionJson.Options));

        var result = new ScalarTruthRecoveryVerifier().Verify(recoveryPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "truth_claim_warning_missing");
    }

    [Fact]
    public void Capability_status_uses_scalar_evidence_set_for_actor_yaw_readiness()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        using var camera = CaptureStableFloatSession("camera_only");
        var scalarEvidenceSet = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);
        var scalarEvidenceSetPath = Path.Combine(passive.Path, "scalar-evidence-set.json");
        File.WriteAllText(scalarEvidenceSetPath, JsonSerializer.Serialize(scalarEvidenceSet with { OutputPath = scalarEvidenceSetPath }, SessionJson.Options));

        var result = new CapabilityStatusService().Build(scalarEvidenceSetPath: scalarEvidenceSetPath);

        Assert.Equal(Path.GetFullPath(scalarEvidenceSetPath), result.ScalarEvidenceSetPath);
        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        Assert.Equal("validated_candidate", actorYaw.EvidenceReadiness);
        Assert.Equal(1, actorYaw.EvidenceCount);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("actor_yaw:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("scalar_evidence_is_candidate_evidence_not_truth_claim", result.Warnings);
    }

    [Fact]
    public void Capability_status_uses_scalar_truth_recovery_for_actor_yaw_and_camera_readiness()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));

        var result = new CapabilityStatusService().Build([], [], [recoveryPath]);

        Assert.Equal(Path.GetFullPath(recoveryPath), result.ScalarTruthRecoveryPath);
        Assert.Equal([Path.GetFullPath(recoveryPath)], result.ScalarTruthRecoveryPaths);
        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        var cameraOrientation = Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation");
        Assert.Equal("recovered_candidate", actorYaw.EvidenceReadiness);
        Assert.Equal("recovered_candidate", cameraOrientation.EvidenceReadiness);
        Assert.Equal(1, actorYaw.EvidenceCount);
        Assert.Equal(1, cameraOrientation.EvidenceCount);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("actor_yaw:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("camera_orientation:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth", result.Warnings);
        Assert.Equal("review_recovered_candidate_then_external_corroborate_before_final_truth_claim", actorYaw.NextAction);
        Assert.Equal("review_recovered_candidate_then_external_corroborate_before_final_truth_claim", cameraOrientation.NextAction);
    }

    [Fact]
    public void Report_capability_cli_accepts_scalar_truth_recovery_packet()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var capabilityPath = Path.Combine(temp.Path, "capability-status.json");

        var exitCode = RiftScan.Cli.Program.Main(["report", "capability", "--scalar-truth-recovery", recoveryPath, "--json-out", capabilityPath]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(capabilityPath));
        var result = JsonSerializer.Deserialize<CapabilityStatusResult>(File.ReadAllText(capabilityPath), SessionJson.Options)!;
        Assert.Equal([Path.GetFullPath(recoveryPath)], result.ScalarTruthRecoveryPaths);
        Assert.Equal("recovered_candidate", Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw").EvidenceReadiness);
        Assert.Equal("recovered_candidate", Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation").EvidenceReadiness);
    }

    [Fact]
    public void Scalar_truth_promotion_promotes_corroborated_recovered_candidates_and_blocks_conflicts()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "conflicted");

        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath);

        Assert.True(promotion.Success);
        Assert.Equal(Path.GetFullPath(recoveryPath), promotion.RecoveryPath);
        Assert.Equal(Path.GetFullPath(corroborationPath), promotion.CorroborationPath);
        Assert.Equal(2, promotion.RecoveredCandidateCount);
        Assert.Equal(1, promotion.PromotedCandidateCount);
        Assert.Equal(1, promotion.BlockedCandidateCount);
        var promotedActor = Assert.Single(promotion.PromotedCandidates);
        var blockedCamera = Assert.Single(promotion.BlockedCandidates);
        Assert.Equal("actor_yaw_angle_scalar_candidate", promotedActor.Classification);
        Assert.Equal("corroborated_candidate", promotedActor.PromotionStatus);
        Assert.Equal("corroborated_candidate", promotedActor.TruthReadiness);
        Assert.Equal("manual_review_promoted_candidate_before_final_truth_claim", promotedActor.NextValidationStep);
        Assert.Equal("camera_orientation_angle_scalar_candidate", blockedCamera.Classification);
        Assert.Equal("blocked_conflict", blockedCamera.PromotionStatus);
        Assert.Equal("blocked_conflict", blockedCamera.TruthReadiness);
        Assert.Contains("scalar_truth_promotion_contains_external_conflicts", promotion.Warnings);
    }

    [Fact]
    public void Scalar_truth_promotion_verifier_accepts_promoted_packet_and_cli()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var result = new ScalarTruthPromotionVerifier().Verify(promotionPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "scalar-truth-promotion", promotionPath]);

        Assert.True(result.Success);
        Assert.Equal(2, result.PromotedCandidateCount);
        Assert.Equal(0, result.BlockedCandidateCount);
        Assert.Empty(result.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Compare_scalar_promotion_cli_writes_promotion_packet()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");

        var exitCode = RiftScan.Cli.Program.Main(["compare", "scalar-promotion", recoveryPath, "--corroboration", corroborationPath, "--out", promotionPath]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(promotionPath));
        var promotion = JsonSerializer.Deserialize<ScalarTruthPromotionResult>(File.ReadAllText(promotionPath), SessionJson.Options)!;
        Assert.Equal(2, promotion.PromotedCandidateCount);
        Assert.All(promotion.PromotedCandidates, candidate => Assert.Equal("corroborated_candidate", candidate.TruthReadiness));
    }

    [Fact]
    public void Scalar_promotion_review_accepts_ready_candidates_and_writes_markdown_report()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var reviewPath = Path.Combine(temp.Path, "combined-scalar-promotion-review.json");
        var reviewMarkdownPath = Path.Combine(temp.Path, "combined-scalar-promotion-review.md");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var exitCode = RiftScan.Cli.Program.Main(["review", "scalar-promotion", promotionPath, "--out", reviewPath, "--report-md", reviewMarkdownPath]);
        var verification = new ScalarPromotionReviewVerifier().Verify(reviewPath);
        var cliVerifyExitCode = RiftScan.Cli.Program.Main(["verify", "scalar-promotion-review", reviewPath]);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, cliVerifyExitCode);
        Assert.True(File.Exists(reviewPath));
        Assert.True(File.Exists(reviewMarkdownPath));
        Assert.True(verification.Success);
        Assert.Equal("ready_for_manual_truth_review", verification.DecisionState);
        Assert.Equal(2, verification.ReadyForManualTruthReviewCount);
        Assert.Equal(0, verification.BlockedConflictCount);
        var review = JsonSerializer.Deserialize<ScalarPromotionReviewResult>(File.ReadAllText(reviewPath), SessionJson.Options)!;
        Assert.Equal(Path.GetFullPath(reviewMarkdownPath), review.MarkdownReportPath);
        Assert.All(review.CandidateReviews, candidate =>
        {
            Assert.Equal("ready_for_manual_truth_review", candidate.DecisionState);
            Assert.True(candidate.ManualConfirmationRequired);
            Assert.False(candidate.FinalTruthClaim);
        });
        Assert.Contains("manual confirmation", File.ReadAllText(reviewMarkdownPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scalar_promotion_review_preserves_conflict_as_blocked()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion-conflict.json");
        var reviewPath = Path.Combine(temp.Path, "combined-scalar-promotion-review-conflict.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "conflicted");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var review = new ScalarPromotionReviewService().Review(promotionPath) with { OutputPath = reviewPath };
        File.WriteAllText(reviewPath, JsonSerializer.Serialize(review, SessionJson.Options));
        var verification = new ScalarPromotionReviewVerifier().Verify(reviewPath);

        Assert.True(verification.Success);
        Assert.Equal("blocked_conflict", review.DecisionState);
        Assert.Equal(1, review.ReadyForManualTruthReviewCount);
        Assert.Equal(1, review.BlockedConflictCount);
        var blockedCamera = Assert.Single(review.CandidateReviews, candidate => candidate.Classification == "camera_orientation_angle_scalar_candidate");
        Assert.Equal("blocked_conflict", blockedCamera.DecisionState);
        Assert.Contains("resolve_corroboration_conflict_before_any_truth_claim", blockedCamera.BlockingGaps);
        Assert.False(blockedCamera.FinalTruthClaim);
    }

    [Fact]
    public void Scalar_promotion_review_verifier_rejects_hidden_conflict()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion-conflict.json");
        var reviewPath = Path.Combine(temp.Path, "combined-scalar-promotion-review-hidden-conflict.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "conflicted");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));
        var review = new ScalarPromotionReviewService().Review(promotionPath);
        var hiddenConflictReviews = review.CandidateReviews
            .Select(candidate => candidate.SourceCorroborationStatus == "conflicted"
                ? candidate with { DecisionState = "ready_for_manual_truth_review", BlockingGaps = [] }
                : candidate)
            .ToArray();
        var hiddenConflict = review with
        {
            DecisionState = "ready_for_manual_truth_review",
            BlockedConflictCount = 0,
            ReadyForManualTruthReviewCount = 2,
            CandidateReviews = hiddenConflictReviews,
            OutputPath = reviewPath
        };
        File.WriteAllText(reviewPath, JsonSerializer.Serialize(hiddenConflict, SessionJson.Options));

        var verification = new ScalarPromotionReviewVerifier().Verify(reviewPath);

        Assert.False(verification.Success);
        Assert.Contains(verification.Issues, issue => issue.Code == "candidate_conflict_hidden");
    }

    [Fact]
    public void Capability_status_uses_scalar_truth_promotion_for_actor_yaw_and_camera_readiness()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var result = new CapabilityStatusService().Build([], [], [], [promotionPath]);

        Assert.Equal(Path.GetFullPath(promotionPath), result.ScalarTruthPromotionPath);
        Assert.Equal([Path.GetFullPath(promotionPath)], result.ScalarTruthPromotionPaths);
        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        var cameraOrientation = Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation");
        Assert.Equal("corroborated_candidate", actorYaw.EvidenceReadiness);
        Assert.Equal("corroborated_candidate", cameraOrientation.EvidenceReadiness);
        Assert.Equal("manual_review_promoted_candidate_before_final_truth_claim", actorYaw.NextAction);
        Assert.Equal("manual_review_promoted_candidate_before_final_truth_claim", cameraOrientation.NextAction);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("actor_yaw:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("camera_orientation:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("scalar_truth_promotion_is_not_final_truth_without_manual_review", result.Warnings);
    }

    [Fact]
    public void Report_capability_cli_accepts_scalar_truth_promotion_packet()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var capabilityPath = Path.Combine(temp.Path, "capability-status.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var exitCode = RiftScan.Cli.Program.Main(["report", "capability", "--scalar-truth-promotion", promotionPath, "--json-out", capabilityPath]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(capabilityPath));
        var result = JsonSerializer.Deserialize<CapabilityStatusResult>(File.ReadAllText(capabilityPath), SessionJson.Options)!;
        Assert.Equal([Path.GetFullPath(promotionPath)], result.ScalarTruthPromotionPaths);
        Assert.Equal("corroborated_candidate", Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw").EvidenceReadiness);
        Assert.Equal("corroborated_candidate", Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation").EvidenceReadiness);
    }

    [Fact]
    public void Capability_status_uses_scalar_promotion_review_for_manual_review_readiness()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion.json");
        var reviewPath = Path.Combine(temp.Path, "combined-scalar-promotion-review.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "corroborated");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));
        var review = new ScalarPromotionReviewService().Review(promotionPath) with { OutputPath = reviewPath };
        File.WriteAllText(reviewPath, JsonSerializer.Serialize(review, SessionJson.Options));

        var result = new CapabilityStatusService().Build([], [], [], [], [reviewPath]);

        Assert.Equal(Path.GetFullPath(reviewPath), result.ScalarPromotionReviewPath);
        Assert.Equal([Path.GetFullPath(reviewPath)], result.ScalarPromotionReviewPaths);
        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        var cameraOrientation = Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation");
        Assert.Equal("ready_for_manual_truth_review", actorYaw.EvidenceReadiness);
        Assert.Equal("ready_for_manual_truth_review", cameraOrientation.EvidenceReadiness);
        Assert.Equal("manual_review_required_before_final_truth_claim", actorYaw.NextAction);
        Assert.Equal("manual_review_required_before_final_truth_claim", cameraOrientation.NextAction);
        Assert.Contains("manual_review_required_before_final_truth_claim", result.NextRecommendedActions);
        Assert.Contains("scalar_promotion_review_is_not_final_truth_without_manual_confirmation", result.Warnings);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("actor_yaw:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("camera_orientation:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Report_capability_cli_accepts_scalar_promotion_review_packet_and_preserves_conflicts()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = Path.Combine(temp.Path, "combined-scalar-truth-recovery.json");
        var promotionPath = Path.Combine(temp.Path, "combined-scalar-truth-promotion-conflict.json");
        var reviewPath = Path.Combine(temp.Path, "combined-scalar-promotion-review-conflict.json");
        var capabilityPath = Path.Combine(temp.Path, "capability-status-review-conflict.json");
        var recovery = BuildCombinedScalarTruthRecovery(temp.Path) with { OutputPath = recoveryPath };
        File.WriteAllText(recoveryPath, JsonSerializer.Serialize(recovery, SessionJson.Options));
        var corroborationPath = WriteCombinedCorroboration(temp.Path, actorStatus: "corroborated", cameraStatus: "conflicted");
        var promotion = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));
        var review = new ScalarPromotionReviewService().Review(promotionPath) with { OutputPath = reviewPath };
        File.WriteAllText(reviewPath, JsonSerializer.Serialize(review, SessionJson.Options));

        var exitCode = RiftScan.Cli.Program.Main(["report", "capability", "--scalar-promotion-review", reviewPath, "--json-out", capabilityPath]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(capabilityPath));
        var result = JsonSerializer.Deserialize<CapabilityStatusResult>(File.ReadAllText(capabilityPath), SessionJson.Options)!;
        Assert.Equal([Path.GetFullPath(reviewPath)], result.ScalarPromotionReviewPaths);
        Assert.Equal("ready_for_manual_truth_review", Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw").EvidenceReadiness);
        Assert.Equal("blocked_conflict", Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation").EvidenceReadiness);
        Assert.Contains(result.EvidenceMissing, missing => missing == "camera_orientation:blocked_conflict");
        Assert.Contains("scalar_promotion_review_contains_blocked_conflicts", result.Warnings);
    }

    [Fact]
    public void Capability_status_removes_stale_component_warnings_after_scalar_upgrade()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        using var camera = CaptureStableFloatSession("camera_only");
        var scalarEvidenceSet = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);
        var scalarEvidenceSetPath = Path.Combine(passive.Path, "scalar-evidence-set.json");
        File.WriteAllText(scalarEvidenceSetPath, JsonSerializer.Serialize(scalarEvidenceSet with { OutputPath = scalarEvidenceSetPath }, SessionJson.Options));
        var readinessPath = Path.Combine(passive.Path, "truth-readiness.json");
        var readiness = new ComparisonTruthReadinessResult
        {
            Success = true,
            SessionAId = "a",
            SessionBId = "b",
            EntityLayout = StrongStatus("entity_layout"),
            Position = StrongStatus("position"),
            ActorYaw = MissingStatus("actor_yaw"),
            CameraOrientation = MissingStatus("camera_orientation"),
            NextRequiredCapture = new ComparisonTruthReadinessCaptureRequirement
            {
                Mode = "capture_labeled_turn_session",
                Reason = "fixture",
                ExpectedSignal = "fixture_signal",
                StopCondition = "fixture_stop"
            },
            Warnings =
            [
                "truth_readiness_is_candidate_evidence_not_truth_claim",
                "actor_yaw_not_strongly_ready",
                "camera_orientation_not_strongly_ready"
            ]
        };
        File.WriteAllText(readinessPath, JsonSerializer.Serialize(readiness, SessionJson.Options));

        var result = new CapabilityStatusService().Build(readinessPath, scalarEvidenceSetPath);

        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        Assert.Equal("validated_candidate", actorYaw.EvidenceReadiness);
        Assert.DoesNotContain("actor_yaw_not_strongly_ready", result.Warnings);
        Assert.Contains("camera_orientation_not_strongly_ready", result.Warnings);
        Assert.DoesNotContain("capture_labeled_turn_session", result.NextRecommendedActions);
        Assert.Contains(result.NextRecommendedActions, action => action.Contains("camera", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Capability_status_merges_multiple_scalar_evidence_sets()
    {
        using var actorPassive = CaptureStableFloatSession("passive_idle");
        using var actorTurnLeft = CaptureChangingFloatSession("turn_left");
        using var actorTurnRight = CaptureChangingFloatSession("turn_right");
        using var actorCamera = CaptureStableFloatSession("camera_only");
        var actorEvidenceSet = new ScalarEvidenceSetService().Aggregate([actorPassive.Path, actorTurnLeft.Path, actorTurnRight.Path, actorCamera.Path]);
        var actorEvidenceSetPath = Path.Combine(actorPassive.Path, "actor-scalar-evidence-set.json");
        File.WriteAllText(actorEvidenceSetPath, JsonSerializer.Serialize(actorEvidenceSet with { OutputPath = actorEvidenceSetPath }, SessionJson.Options));

        using var cameraPassive = CaptureStableFloatSession("passive_idle");
        using var cameraTurnLeft = CaptureStableFloatSession("turn_left");
        using var cameraTurnRight = CaptureStableFloatSession("turn_right");
        using var cameraOnly = CaptureChangingFloatSession("camera_only");
        var cameraEvidenceSet = new ScalarEvidenceSetService().Aggregate([cameraPassive.Path, cameraTurnLeft.Path, cameraTurnRight.Path, cameraOnly.Path]);
        var cameraEvidenceSetPath = Path.Combine(cameraPassive.Path, "camera-scalar-evidence-set.json");
        File.WriteAllText(cameraEvidenceSetPath, JsonSerializer.Serialize(cameraEvidenceSet with { OutputPath = cameraEvidenceSetPath }, SessionJson.Options));

        var result = new CapabilityStatusService().Build((string?)null, [actorEvidenceSetPath, cameraEvidenceSetPath]);

        Assert.Equal(Path.GetFullPath(actorEvidenceSetPath), result.ScalarEvidenceSetPath);
        Assert.Equal([Path.GetFullPath(actorEvidenceSetPath), Path.GetFullPath(cameraEvidenceSetPath)], result.ScalarEvidenceSetPaths);
        var actorYaw = Assert.Single(result.TruthComponents, component => component.Component == "actor_yaw");
        var cameraOrientation = Assert.Single(result.TruthComponents, component => component.Component == "camera_orientation");
        Assert.Equal("validated_candidate", actorYaw.EvidenceReadiness);
        Assert.Equal("validated_candidate", cameraOrientation.EvidenceReadiness);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("actor_yaw:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("camera_orientation:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.NextRecommendedActions, action => action.Contains("turn", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.NextRecommendedActions, action => action.Contains("camera", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Capability_status_merges_multiple_truth_readiness_packets()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var entityReadinessPath = Path.Combine(temp.Path, "entity-truth-readiness.json");
        File.WriteAllText(entityReadinessPath, JsonSerializer.Serialize(new ComparisonTruthReadinessResult
        {
            Success = true,
            SessionAId = "entity-a",
            SessionBId = "entity-b",
            EntityLayout = StrongStatus("entity_layout"),
            Position = MissingStatus("position"),
            ActorYaw = MissingStatus("actor_yaw"),
            CameraOrientation = MissingStatus("camera_orientation"),
            NextRequiredCapture = new ComparisonTruthReadinessCaptureRequirement
            {
                Mode = "capture_labeled_move_forward",
                Reason = "fixture",
                ExpectedSignal = "fixture_signal",
                StopCondition = "fixture_stop"
            },
            Warnings =
            [
                "truth_readiness_is_candidate_evidence_not_truth_claim",
                "position_not_strongly_ready",
                "actor_yaw_not_strongly_ready",
                "camera_orientation_not_strongly_ready"
            ]
        }, SessionJson.Options));
        var positionReadinessPath = Path.Combine(temp.Path, "position-truth-readiness.json");
        File.WriteAllText(positionReadinessPath, JsonSerializer.Serialize(new ComparisonTruthReadinessResult
        {
            Success = true,
            SessionAId = "position-a",
            SessionBId = "position-b",
            EntityLayout = MissingStatus("entity_layout"),
            Position = StrongStatus("position"),
            ActorYaw = MissingStatus("actor_yaw"),
            CameraOrientation = MissingStatus("camera_orientation"),
            NextRequiredCapture = new ComparisonTruthReadinessCaptureRequirement
            {
                Mode = "capture_labeled_turn_session",
                Reason = "fixture",
                ExpectedSignal = "fixture_signal",
                StopCondition = "fixture_stop"
            },
            Warnings =
            [
                "truth_readiness_is_candidate_evidence_not_truth_claim",
                "entity_layout_not_strongly_ready",
                "actor_yaw_not_strongly_ready",
                "camera_orientation_not_strongly_ready"
            ]
        }, SessionJson.Options));

        var result = new CapabilityStatusService().Build([entityReadinessPath, positionReadinessPath], []);

        Assert.Equal([Path.GetFullPath(entityReadinessPath), Path.GetFullPath(positionReadinessPath)], result.TruthReadinessPaths);
        Assert.Equal(Path.GetFullPath(entityReadinessPath), result.TruthReadinessPath);
        Assert.Equal("strong_candidate", Assert.Single(result.TruthComponents, component => component.Component == "entity_layout").EvidenceReadiness);
        Assert.Equal("strong_candidate", Assert.Single(result.TruthComponents, component => component.Component == "position").EvidenceReadiness);
        Assert.DoesNotContain("entity_layout_not_strongly_ready", result.Warnings);
        Assert.DoesNotContain("position_not_strongly_ready", result.Warnings);
    }

    [Fact]
    public void Scalar_truth_exporter_applies_external_corroboration_file()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureChangingFloatSession("turn_left");
        using var turnRight = CaptureChangingFloatSession("turn_right");
        using var camera = CaptureStableFloatSession("camera_only");
        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);
        var corroborationPath = Path.Combine(passive.Path, "scalar_truth_corroboration.jsonl");
        var corroboration = new ScalarTruthCorroborationEntry
        {
            BaseAddressHex = "0x1000",
            OffsetHex = "0x4",
            DataType = "float32",
            Classification = "actor_yaw_angle_scalar_candidate",
            CorroborationStatus = "corroborated",
            Source = "fixture_addon_truth",
            EvidenceSummary = "fixture actor yaw corroboration"
        };
        File.WriteAllText(corroborationPath, JsonSerializer.Serialize(corroboration, SessionJson.Options).ReplaceLineEndings(string.Empty));
        var truthOut = Path.Combine(passive.Path, "scalar_truth_candidates_with_corroboration.jsonl");

        var exported = new ScalarTruthCandidateExporter().Export(result, truthOut, corroborationPath: corroborationPath);

        var truthCandidate = Assert.Single(exported);
        Assert.Equal("corroborated", truthCandidate.CorroborationStatus);
        Assert.Equal(["fixture_addon_truth"], truthCandidate.CorroborationSources);
        Assert.Equal("fixture actor yaw corroboration", truthCandidate.CorroborationSummary);
        Assert.Equal("behavior_and_external_corroborated_candidate", truthCandidate.ValidationStatus);
        Assert.Equal("validated_candidate", truthCandidate.ClaimLevel);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_ranks_camera_orientation_from_camera_only_change_and_turn_stable()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureStableFloatSession("turn_left");
        using var turnRight = CaptureStableFloatSession("turn_right");
        using var camera = CaptureChangingFloatSession("camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.Equal("validated_candidate", candidate.TruthReadiness);
        Assert.Equal("validated_candidate", candidate.ConfidenceLevel);
        Assert.True(candidate.PassiveStable);
        Assert.False(candidate.TurnLeftChanged);
        Assert.False(candidate.TurnRightChanged);
        Assert.True(candidate.CameraOnlyChanged);
        Assert.False(candidate.OppositeTurnPolarity);
        Assert.Equal("camera_only_changes_turn_stable", candidate.CameraTurnSeparation);
        Assert.Contains("camera_only_separated_from_actor_turn", candidate.SupportingReasons);
        Assert.Contains("turn_sessions_stable_for_camera_only", candidate.SupportingReasons);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_keeps_zoom_camera_stimulus_out_of_orientation_truth()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureStableFloatSession("turn_left");
        using var turnRight = CaptureStableFloatSession("turn_right");
        using var camera = CaptureChangingFloatSession("camera_only", "fixture_mouse_wheel_zoom_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("camera_zoom_angle_scalar_candidate", candidate.Classification);
        Assert.Equal("validated_candidate", candidate.TruthReadiness);
        Assert.DoesNotContain("camera_orientation", candidate.Classification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("camera_zoom_stimulus_note_present", candidate.SupportingReasons);
        Assert.Equal("add_yaw_or_pitch_camera_only_capture_for_orientation", candidate.NextValidationStep);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_uses_yaw_pitch_note_for_orientation_followup()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var turnLeft = CaptureStableFloatSession("turn_left");
        using var turnRight = CaptureStableFloatSession("turn_right");
        using var camera = CaptureChangingFloatSession("camera_only", "fixture_horizontal_yaw_drag_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, camera.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.Equal("validated_candidate", candidate.TruthReadiness);
        Assert.Contains("camera_yaw_or_pitch_stimulus_note_present", candidate.SupportingReasons);
        Assert.Contains("camera_stimulus=yaw_or_pitch", candidate.EvidenceSummary, StringComparison.Ordinal);
        Assert.Equal("repeat_opposite_camera_yaw_or_pitch_capture_or_validate_against_camera_truth", candidate.NextValidationStep);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_validate_camera_orientation_without_turn_sessions()
    {
        using var passive = CaptureStableFloatSession("passive_idle");
        using var camera = CaptureChangingFloatSession("camera_only", "fixture_horizontal_yaw_drag_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, camera.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.True(candidate.CameraOnlyChanged);
        Assert.Equal("camera_only_changes_turn_untested", candidate.CameraTurnSeparation);
        Assert.NotEqual("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.NotEqual("validated_candidate", candidate.TruthReadiness);
        Assert.Contains("missing_turn_stability_for_camera_only", candidate.RejectionReasons);
        Assert.Equal("add_opposite_turn_sessions_for_camera_actor_split", candidate.NextValidationStep);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_mark_camera_orientation_strong_without_passive_baseline()
    {
        using var turnLeft = CaptureStableFloatSession("turn_left");
        using var turnRight = CaptureStableFloatSession("turn_right");
        using var camera = CaptureChangingFloatSession("camera_only", "fixture_horizontal_yaw_drag_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([turnLeft.Path, turnRight.Path, camera.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x4");
        Assert.Equal("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.Equal("candidate", candidate.TruthReadiness);
        Assert.Contains("missing_passive_baseline_for_candidate", candidate.RejectionReasons);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_promote_zero_net_camera_blip()
    {
        using var passive = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [20.0f, 20.0f, 20.0f]);
        using var turnLeft = CaptureDualScalarSession("turn_left", [1.5f, 1.5f, 1.5f], [20.0f, 20.0f, 20.0f]);
        using var turnRight = CaptureDualScalarSession("turn_right", [1.5f, 1.5f, 1.5f], [20.0f, 20.0f, 20.0f]);
        using var cameraOnly = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [20.0f, 0.0f, 20.0f]);

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, cameraOnly.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x8");
        Assert.False(candidate.CameraOnlyChanged);
        Assert.Equal("camera_and_turn_both_stable", candidate.CameraTurnSeparation);
        Assert.NotEqual("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.NotEqual("validated_candidate", candidate.TruthReadiness);
        Assert.Contains("no_turn_labeled_scalar_change", candidate.RejectionReasons);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_promote_single_step_camera_yaw_jump()
    {
        using var passive = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeft = CaptureDualScalarSession("turn_left", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRight = CaptureDualScalarSession("turn_right", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnly = CaptureDualScalarSession(
            "camera_only",
            [1.5f, 1.5f, 1.5f],
            [2.0f, 2.0f, 2.25f],
            "fixture_horizontal_yaw_drag_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, cameraOnly.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x8");
        Assert.False(candidate.CameraOnlyChanged);
        Assert.Equal("camera_and_turn_both_stable", candidate.CameraTurnSeparation);
        Assert.NotEqual("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.NotEqual("validated_candidate", candidate.TruthReadiness);
        Assert.Contains("camera_yaw_or_pitch_change_not_temporally_smooth", candidate.RejectionReasons);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_promote_tiny_camera_yaw_drift()
    {
        using var passive = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeft = CaptureDualScalarSession("turn_left", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRight = CaptureDualScalarSession("turn_right", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnly = CaptureDualScalarSession(
            "camera_only",
            [1.5f, 1.5f, 1.5f],
            [2.0f, 2.0002f, 2.0004f],
            "fixture_horizontal_yaw_drag_camera_only");

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, cameraOnly.Path]);

        var candidate = Assert.Single(result.RankedCandidates, candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x8");
        Assert.False(candidate.CameraOnlyChanged);
        Assert.Equal("camera_and_turn_both_stable", candidate.CameraTurnSeparation);
        Assert.NotEqual("camera_orientation_angle_scalar_candidate", candidate.Classification);
        Assert.NotEqual("validated_candidate", candidate.TruthReadiness);
        Assert.Contains("camera_yaw_or_pitch_delta_below_threshold", candidate.RejectionReasons);
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_does_not_promote_binary_camera_yaw_toggle()
    {
        using var passive = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f, 1.5f], [0.0f, 0.0f, 0.0f, 0.0f], samples: 4);
        using var turnLeft = CaptureDualScalarSession("turn_left", [1.5f, 1.5f, 1.5f, 1.5f], [0.0f, 0.0f, 0.0f, 0.0f], samples: 4);
        using var turnRight = CaptureDualScalarSession("turn_right", [1.5f, 1.5f, 1.5f, 1.5f], [0.0f, 0.0f, 0.0f, 0.0f], samples: 4);
        using var cameraOnly = CaptureDualScalarSession(
            "camera_only",
            [1.5f, 1.5f, 1.5f, 1.5f],
            [0.0f, 1.0f, 0.0f, 1.0f],
            "fixture_horizontal_yaw_drag_camera_only",
            samples: 4);

        var result = new ScalarEvidenceSetService().Aggregate([passive.Path, turnLeft.Path, turnRight.Path, cameraOnly.Path]);

        Assert.DoesNotContain(result.RankedCandidates, candidate =>
            candidate.Classification == "camera_orientation_angle_scalar_candidate" &&
            candidate.TruthReadiness == "validated_candidate");
    }

    [Fact]
    public void Aggregate_scalar_evidence_set_summarizes_rejected_candidates()
    {
        using var passiveA = CaptureStableFloatSession("passive_idle");
        using var passiveB = CaptureStableFloatSession("passive_idle");

        var result = new ScalarEvidenceSetService().Aggregate([passiveA.Path, passiveB.Path]);

        Assert.Empty(result.RankedCandidates);
        Assert.Contains(result.RejectedCandidateSummaries, summary =>
            summary.Reason == "missing_opposite_turn_pair_for_candidate" &&
            summary.Count >= 1 &&
            summary.ExampleCandidates.Count >= 1);
        Assert.Contains(result.RejectedCandidateSummaries, summary =>
            summary.Reason == "missing_camera_only_for_candidate" &&
            summary.Count >= 1);
        Assert.Contains("no_ranked_scalar_evidence_candidates", result.Warnings);
    }

    [Fact]
    public void Compare_sessions_reports_vec3_behavior_contrast_between_passive_and_move_forward()
    {
        using var passive = CaptureVec3Session(new FixedVec3ProcessMemoryReader(), "passive_idle");
        using var moving = CaptureVec3Session(new MovingVec3ProcessMemoryReader(), "move_forward");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(passive.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(moving.Path);

        var result = new SessionComparisonService().Compare(passive.Path, moving.Path);

        var match = Assert.Single(result.Vec3CandidateMatches, match => match.BaseAddressHex == "0x1000" && match.OffsetHex == "0x0");
        Assert.Equal("passive_idle", match.SessionAStimulusLabel);
        Assert.Equal("move_forward", match.SessionBStimulusLabel);
        Assert.Equal(20, match.SessionABehaviorScore);
        Assert.Equal(25, match.SessionBBehaviorScore);
        Assert.Equal(5, match.BehaviorScoreDelta);
        Assert.Equal(0, match.SessionAValueDeltaMagnitude);
        Assert.True(match.SessionBValueDeltaMagnitude > 0);
        Assert.StartsWith("samples=", match.SessionAValueSequenceSummary, StringComparison.Ordinal);
        Assert.Contains("delta=", match.SessionBValueSequenceSummary, StringComparison.Ordinal);
        Assert.Contains("stimuli.jsonl", match.SessionBAnalyzerSources);
        Assert.Equal("behavior_consistent_candidate", match.SessionAValidationStatus);
        Assert.Equal("behavior_consistent_candidate", match.SessionBValidationStatus);
        Assert.Equal("passive_to_move_vec3_behavior_contrast_candidate", match.Recommendation);
        Assert.Equal(result.Vec3CandidateMatches.Count, result.Vec3BehaviorSummary.MatchingVec3CandidateCount);
        Assert.Equal(1, result.Vec3BehaviorSummary.BehaviorContrastCount);
        Assert.Equal(1, result.Vec3BehaviorSummary.BehaviorConsistentMatchCount);
        Assert.Equal(0, result.Vec3BehaviorSummary.UnlabeledMatchCount);
        Assert.Equal(["move_forward", "passive_idle"], result.Vec3BehaviorSummary.StimulusLabels);
        var contrast = Assert.Single(result.Vec3BehaviorSummary.BehaviorContrastCandidates);
        Assert.Equal("position_like_vec3_candidate", contrast.Classification);
        Assert.Equal("0x1000", contrast.BaseAddressHex);
        Assert.Equal("0x0", contrast.OffsetHex);
        Assert.Equal("passive_idle", contrast.SessionAStimulusLabel);
        Assert.Equal("move_forward", contrast.SessionBStimulusLabel);
        Assert.Equal(0, contrast.SessionAValueDeltaMagnitude);
        Assert.True(contrast.SessionBValueDeltaMagnitude > 0);
        Assert.True(contrast.ScoreTotal >= 75);
        Assert.Equal(15, contrast.ScoreBreakdown["label_contrast_score"]);
        Assert.Equal(15, contrast.ScoreBreakdown["snapshot_support_score"]);
        Assert.Contains("has_passive_and_move_forward_labels", contrast.SupportingReasons);
        Assert.Empty(contrast.RejectionReasons);
        Assert.Equal("strong_candidate", contrast.ConfidenceLevel);
        Assert.Contains("passive_idle_delta=0.000000", contrast.EvidenceSummary, StringComparison.Ordinal);
        Assert.Equal("validate_against_addon_truth_or_repeat_move_forward_contrast", contrast.NextValidationStep);
        Assert.Equal("review_behavior_contrast_candidates_before_truth_claim", result.Vec3BehaviorSummary.NextRecommendedAction);

        var plan = new SessionComparisonNextCapturePlanGenerator().Build(result);

        Assert.Equal("review_existing_behavior_contrast", plan.RecommendedMode);
        Assert.Equal("comparison_already_contains_behavior_contrast_candidates", plan.Reason);
        var target = plan.TargetRegionPriorities.Single(candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x0");
        Assert.Equal("0x1000", target.BaseAddressHex);
        Assert.Equal("0x0", target.OffsetHex);
        Assert.Equal("passive_to_move_vec3_behavior_contrast_candidate", target.Reason);
        Assert.Equal(100, target.PriorityScore);
        Assert.Contains("next_capture_plan_is_recommendation_not_truth_claim", plan.Warnings);

        var readiness = new ComparisonTruthReadinessService().Build(result);

        Assert.Equal("strong_candidate", readiness.Position.Readiness);
        Assert.Equal("vec3_behavior_contrast_candidates_exist", readiness.Position.PrimaryReason);
        Assert.Equal("review_existing_behavior_contrast", readiness.NextRequiredCapture.Mode);
        Assert.Single(readiness.TopVec3BehaviorCandidates);
        Assert.Contains("truth_readiness_is_candidate_evidence_not_truth_claim", readiness.Warnings);

        var truthOut = Path.Combine(passive.Path, "vec3_truth_candidates.jsonl");
        var exported = new Vec3TruthCandidateExporter().Export(result, truthOut);
        var truthCandidate = Assert.Single(exported);
        Assert.True(File.Exists(truthOut));
        Assert.Equal("vec3-truth-000001", truthCandidate.CandidateId);
        Assert.Equal("riftscan.session_comparison.v1", truthCandidate.SourceSchemaVersion);
        Assert.Equal("position_like_vec3_candidate", truthCandidate.Classification);
        Assert.Equal("strong_candidate", truthCandidate.TruthReadiness);
        Assert.Equal("behavior_strong_candidate", truthCandidate.ValidationStatus);
        Assert.Equal("candidate", truthCandidate.ClaimLevel);
        Assert.True(truthCandidate.PassiveStable);
        Assert.True(truthCandidate.MoveForwardChanged);
        Assert.Equal("addon_waypoint_or_player_coord_truth", truthCandidate.ExternalTruthSourceHint);
        Assert.Equal("not_requested", truthCandidate.CorroborationStatus);
        Assert.Contains("preview=", truthCandidate.SessionAValueSequenceSummary, StringComparison.Ordinal);
        Assert.Contains("snapshots/*.bin", truthCandidate.SessionAAnalyzerSources);

        var corroborationPath = WriteVec3Corroboration(passive.Path, "corroborated");
        var corroboratedOut = Path.Combine(passive.Path, "vec3_truth_candidates_corroborated.jsonl");
        var corroboratedCandidate = Assert.Single(new Vec3TruthCandidateExporter().Export(result, corroboratedOut, corroborationPath: corroborationPath));
        Assert.Equal("corroborated", corroboratedCandidate.CorroborationStatus);
        Assert.Equal("behavior_and_addon_waypoint_corroborated_candidate", corroboratedCandidate.ValidationStatus);
        Assert.Equal(["fixture_readerbridge_coordX_coordY_coordZ"], corroboratedCandidate.CorroborationSources);
        Assert.Equal("fixture addon coord corroborated", corroboratedCandidate.CorroborationSummary);
        Assert.Equal("repeat_move_forward_contrast_or_recover_across_sessions", corroboratedCandidate.NextValidationStep);

        var repeatOut = Path.Combine(passive.Path, "vec3_truth_candidates_repeat.jsonl");
        _ = new Vec3TruthCandidateExporter().Export(result, repeatOut);
        var recovery = new Vec3TruthRecoveryService().Recover([truthOut, repeatOut]);
        var recovered = Assert.Single(recovery.RecoveredCandidates);
        Assert.Equal("vec3-recovered-000001", recovered.CandidateId);
        Assert.Equal("recovered_candidate", recovered.TruthReadiness);
        Assert.Equal("recovered_candidate", recovered.ClaimLevel);
        Assert.Equal(2, recovered.SupportingFileCount);
        Assert.Contains("repeated_truth_candidate_match", recovered.SupportingReasons);
        Assert.Equal("recovered_coordinate_candidate_requires_addon_waypoint_review_before_final_truth_claim", recovered.Warning);
    }

    [Fact]
    public void Cli_compare_sessions_returns_success()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var outputPath = Path.Combine(sessionA.Path, "comparison.json");
            var reportPath = Path.Combine(sessionA.Path, "comparison.md");
            var nextPlanPath = Path.Combine(sessionA.Path, "comparison-next-capture-plan.json");
            var truthReadinessPath = Path.Combine(sessionA.Path, "comparison-truth-readiness.json");
            var exitCode = RiftScan.Cli.Program.Main(["compare", "sessions", sessionA.Path, sessionB.Path, "--top", "10", "--out", outputPath, "--report-md", reportPath, "--next-plan", nextPlanPath, "--truth-readiness", truthReadinessPath]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(reportPath));
            Assert.True(File.Exists(nextPlanPath));
            Assert.True(File.Exists(truthReadinessPath));
            using var comparisonJson = JsonDocument.Parse(File.ReadAllText(outputPath));
            var comparisonRoot = comparisonJson.RootElement;
            Assert.Equal("riftscan.session_comparison.v1", comparisonRoot.GetProperty("schema_version").GetString());
            Assert.True(comparisonRoot.GetProperty("structure_candidate_matches").GetArrayLength() >= 1);
            Assert.True(comparisonRoot.GetProperty("entity_layout_matches").GetArrayLength() >= 1);
            Assert.True(comparisonRoot.GetProperty("vec3_candidate_matches").GetArrayLength() >= 1);
            var structureMatch = comparisonRoot.GetProperty("structure_candidate_matches")[0];
            Assert.True(structureMatch.TryGetProperty("session_a_candidate_id", out _));
            Assert.True(structureMatch.TryGetProperty("session_a_value_sequence_summary", out _));
            Assert.True(structureMatch.TryGetProperty("session_a_analyzer_sources", out _));
            Assert.Contains("matching_region_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_cluster_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_entity_layout_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_structure_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_vec3_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("vec3_behavior_summary", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_value_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("scalar_behavior_summary", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_path", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_report_path", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_next_capture_plan_path", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_truth_readiness_path", output.ToString(), StringComparison.Ordinal);
            var report = File.ReadAllText(reportPath);
            Assert.Contains("Vec3 behavior summary", report, StringComparison.Ordinal);
            Assert.Contains("Behavior contrast candidates", report, StringComparison.Ordinal);
            Assert.Contains("Scalar behavior summary", report, StringComparison.Ordinal);
            Assert.Contains("Top typed value matches", report, StringComparison.Ordinal);
            Assert.Contains("structure-000001", report, StringComparison.Ordinal);
            Assert.Contains("snapshots/*.bin", report, StringComparison.Ordinal);
            Assert.Contains("candidate evidence, not recovered truth", report, StringComparison.Ordinal);
            var plan = File.ReadAllText(nextPlanPath);
            Assert.Contains("recommended_mode", plan, StringComparison.Ordinal);
            Assert.Contains("next_capture_plan_is_recommendation_not_truth_claim", plan, StringComparison.Ordinal);
            using var truthReadinessJson = JsonDocument.Parse(File.ReadAllText(truthReadinessPath));
            var truthReadinessRoot = truthReadinessJson.RootElement;
            Assert.Equal("riftscan.comparison_truth_readiness.v1", truthReadinessRoot.GetProperty("schema_version").GetString());
            Assert.Equal("entity_layout", truthReadinessRoot.GetProperty("entity_layout").GetProperty("component").GetString());
            Assert.True(truthReadinessRoot.GetProperty("top_entity_layout_matches").GetArrayLength() >= 1);
            Assert.Contains("truth_readiness_is_candidate_evidence_not_truth_claim", File.ReadAllText(truthReadinessPath), StringComparison.Ordinal);
            var verifyExitCode = RiftScan.Cli.Program.Main(["verify", "comparison-readiness", truthReadinessPath]);
            Assert.Equal(0, verifyExitCode);
            var capabilityPath = Path.Combine(sessionA.Path, "capability-status.json");
            var capabilityExitCode = RiftScan.Cli.Program.Main(["report", "capability", "--truth-readiness", truthReadinessPath, "--json-out", capabilityPath]);
            Assert.Equal(0, capabilityExitCode);
            using var capabilityJson = JsonDocument.Parse(File.ReadAllText(capabilityPath));
            var capabilityRoot = capabilityJson.RootElement;
            Assert.Equal("riftscan.capability_status.v1", capabilityRoot.GetProperty("schema_version").GetString());
            Assert.True(capabilityRoot.GetProperty("capability_count").GetInt32() >= 10);
            Assert.Contains("comparison_truth_readiness_verify", File.ReadAllText(capabilityPath), StringComparison.Ordinal);
            var verifyCapabilityExitCode = RiftScan.Cli.Program.Main(["verify", "capability-status", capabilityPath]);
            Assert.Equal(0, verifyCapabilityExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Comparison_readiness_verifier_rejects_missing_truth_claim_warning()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var path = Path.Combine(temp.Path, "truth-readiness.json");
        var readiness = new ComparisonTruthReadinessResult
        {
            Success = true,
            SessionAId = "a",
            SessionBId = "b",
            EntityLayout = new ComparisonTruthReadinessStatus
            {
                Component = "entity_layout",
                Readiness = "strong_candidate",
                EvidenceCount = 1,
                ConfidenceScore = 80,
                PrimaryReason = "fixture",
                NextAction = "fixture_next"
            },
            Position = MissingStatus("position"),
            ActorYaw = MissingStatus("actor_yaw"),
            CameraOrientation = MissingStatus("camera_orientation"),
            NextRequiredCapture = new ComparisonTruthReadinessCaptureRequirement
            {
                Mode = "capture_labeled_move_forward",
                Reason = "fixture",
                ExpectedSignal = "fixture_signal",
                StopCondition = "fixture_stop"
            },
            Warnings = []
        };
        File.WriteAllText(path, JsonSerializer.Serialize(readiness, SessionJson.Options));

        var result = new ComparisonTruthReadinessVerifier().Verify(path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "truth_claim_warning_missing");
    }

    [Fact]
    public void Capability_status_verifier_rejects_missing_required_capability()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var path = Path.Combine(temp.Path, "capability-status.json");
        var status = new CapabilityStatusResult
        {
            Capabilities =
            [
                new CapabilityStatusEntry
                {
                    Name = "passive_capture",
                    PrimaryCommand = "riftscan capture passive",
                    EvidenceSurface = "fixture",
                    OutputArtifacts = ["manifest.json"]
                }
            ],
            NextRecommendedActions = ["fixture_next"],
            Warnings = ["capability_status_reports_coded_surfaces_and_evidence_readiness_not_recovered_truth"]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(status, SessionJson.Options));

        var result = new CapabilityStatusVerifier().Verify(path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "required_capability_missing");
    }

    private static ComparisonTruthReadinessStatus MissingStatus(string component) =>
        new()
        {
            Component = component,
            Readiness = "missing",
            PrimaryReason = "fixture_missing",
            NextAction = "fixture_next",
            BlockingGaps = [$"{component}_gap"]
        };

    private static ComparisonTruthReadinessStatus StrongStatus(string component) =>
        new()
        {
            Component = component,
            Readiness = "strong_candidate",
            EvidenceCount = 1,
            ConfidenceScore = 90,
            PrimaryReason = "fixture_strong",
            NextAction = "fixture_next"
        };

    private static TempDirectory CaptureChangingFloatSession()
    {
        return CaptureChangingFloatSession(stimulusLabel: null);
    }

    private static TempDirectory CaptureChangingFloatSession(string? stimulusLabel, string? stimulusNotes = null)
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(new ChangingFloatProcessMemoryReader(stimulusLabel == "turn_right" ? -1.0f : 1.0f)).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes
        });

        return session;
    }

    private static TempDirectory CaptureStableFloatSession(string stimulusLabel)
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(new StableFloatProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = stimulusLabel
        });

        return session;
    }

    private static TempDirectory CaptureDualScalarSession(
        string stimulusLabel,
        IReadOnlyList<float> actorValues,
        IReadOnlyList<float> cameraValues,
        string? stimulusNotes = null,
        int samples = 3)
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(new DualScalarProcessMemoryReader(actorValues, cameraValues)).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = samples,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes
        });

        return session;
    }

    private static ScalarTruthRecoveryResult BuildCombinedScalarTruthRecovery(string outputRoot)
    {
        using var passiveA = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeftA = CaptureDualScalarSession("turn_left", [1.5f, 2.5f, 3.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRightA = CaptureDualScalarSession("turn_right", [3.5f, 2.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnlyA = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [2.0f, 3.0f, 4.0f]);
        using var passiveB = CaptureDualScalarSession("passive_idle", [1.5f, 1.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var turnLeftB = CaptureDualScalarSession("turn_left", [1.5f, 2.5f, 3.5f], [2.0f, 2.0f, 2.0f]);
        using var turnRightB = CaptureDualScalarSession("turn_right", [3.5f, 2.5f, 1.5f], [2.0f, 2.0f, 2.0f]);
        using var cameraOnlyB = CaptureDualScalarSession("camera_only", [1.5f, 1.5f, 1.5f], [2.0f, 3.0f, 4.0f]);
        var resultA = new ScalarEvidenceSetService().Aggregate([passiveA.Path, turnLeftA.Path, turnRightA.Path, cameraOnlyA.Path]);
        var resultB = new ScalarEvidenceSetService().Aggregate([passiveB.Path, turnLeftB.Path, turnRightB.Path, cameraOnlyB.Path]);
        var truthOutA = Path.Combine(outputRoot, "combined-scalar-truth-candidates-a.jsonl");
        var truthOutB = Path.Combine(outputRoot, "combined-scalar-truth-candidates-b.jsonl");

        _ = new ScalarTruthCandidateExporter().Export(resultA, truthOutA);
        _ = new ScalarTruthCandidateExporter().Export(resultB, truthOutB);
        return new ScalarTruthRecoveryService().Recover([truthOutA, truthOutB]);
    }

    private static string WriteCombinedCorroboration(string outputRoot, string actorStatus, string cameraStatus)
    {
        var path = Path.Combine(outputRoot, "combined-scalar-truth-corroboration.jsonl");
        var entries = new[]
        {
            new ScalarTruthCorroborationEntry
            {
                BaseAddressHex = "0x1000",
                OffsetHex = "0x4",
                DataType = "float32",
                Classification = "actor_yaw_angle_scalar_candidate",
                CorroborationStatus = actorStatus,
                Source = "fixture_addon_truth",
                EvidenceSummary = $"fixture actor yaw {actorStatus}"
            },
            new ScalarTruthCorroborationEntry
            {
                BaseAddressHex = "0x1000",
                OffsetHex = "0x8",
                DataType = "float32",
                Classification = "camera_orientation_angle_scalar_candidate",
                CorroborationStatus = cameraStatus,
                Source = "fixture_camera_truth",
                EvidenceSummary = $"fixture camera orientation {cameraStatus}"
            }
        };
        File.WriteAllLines(path, entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return path;
    }

    private static string WriteVec3Corroboration(string outputRoot, string status)
    {
        var path = Path.Combine(outputRoot, "vec3-truth-corroboration.jsonl");
        var entries = new[]
        {
            new Vec3TruthCorroborationEntry
            {
                BaseAddressHex = "0x1000",
                OffsetHex = "0x0",
                DataType = "vec3_float32",
                Classification = "position_like_vec3_candidate",
                CorroborationStatus = status,
                Source = "fixture_readerbridge_coordX_coordY_coordZ",
                AddonSourceType = "readerbridge_player_coord",
                AddonObservedX = 1,
                AddonObservedY = 2,
                AddonObservedZ = 3,
                Tolerance = 0.5,
                EvidenceSummary = $"fixture addon coord {status}"
            }
        };
        File.WriteAllLines(path, entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return path;
    }

    private static TempDirectory CaptureVec3Session(IProcessMemoryReader reader, string stimulusLabel)
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = stimulusLabel
        });

        return session;
    }

    private static TempDirectory CaptureEntityLayoutSession()
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(new WideEntityLayoutProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 128,
            MaxTotalBytes = 384
        });

        return session;
    }

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session"), temp.Path);
        return temp;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-compare-tests", Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class ChangingFloatProcessMemoryReader : IProcessMemoryReader
    {
        private readonly float _step;
        private int _readCount;

        public ChangingFloatProcessMemoryReader(float step = 1.0f)
        {
            _step = step;
        }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(3.5f + (_step * _readCount++)).CopyTo(bytes, 4);
            return bytes;
        }
    }

    private sealed class StableFloatProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(1.5f).CopyTo(bytes, 4);
            return bytes;
        }
    }

    private sealed class DualScalarProcessMemoryReader : IProcessMemoryReader
    {
        private readonly IReadOnlyList<float> _actorValues;
        private readonly IReadOnlyList<float> _cameraValues;
        private int _readCount;

        public DualScalarProcessMemoryReader(IReadOnlyList<float> actorValues, IReadOnlyList<float> cameraValues)
        {
            _actorValues = actorValues;
            _cameraValues = cameraValues;
        }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var valueIndex = Math.Min(_readCount++, _actorValues.Count - 1);
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(_actorValues[valueIndex]).CopyTo(bytes, 4);
            BitConverter.GetBytes(_cameraValues[valueIndex]).CopyTo(bytes, 8);
            return bytes;
        }
    }

    private sealed class FixedVec3ProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(1.0f).CopyTo(bytes, 0);
            BitConverter.GetBytes(2.0f).CopyTo(bytes, 4);
            BitConverter.GetBytes(-3.0f).CopyTo(bytes, 8);
            return bytes;
        }
    }

    private sealed class MovingVec3ProcessMemoryReader : IProcessMemoryReader
    {
        private int _readCount;

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(1.0f + _readCount++).CopyTo(bytes, 0);
            BitConverter.GetBytes(2.0f).CopyTo(bytes, 4);
            BitConverter.GetBytes(-3.0f).CopyTo(bytes, 8);
            return bytes;
        }
    }

    private sealed class WideEntityLayoutProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x5000, 128, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            for (var offset = 0; offset <= byteCount - 4; offset += 4)
            {
                BitConverter.GetBytes((offset / 4) + 1.0f).CopyTo(bytes, offset);
            }

            return bytes;
        }
    }
}
