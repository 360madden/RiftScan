using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Reports;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ComparisonOutputContractTests
{
    [Fact]
    public void Session_comparison_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionComparisonResult(),
            "schema_version",
            "success",
            "session_a_path",
            "session_b_path",
            "comparison_path",
            "comparison_report_path",
            "comparison_next_capture_plan_path",
            "comparison_truth_readiness_path",
            "session_a_id",
            "session_b_id",
            "same_process_name",
            "matching_region_count",
            "matching_cluster_count",
            "matching_entity_layout_count",
            "matching_structure_candidate_count",
            "matching_vec3_candidate_count",
            "matching_value_candidate_count",
            "region_matches",
            "cluster_matches",
            "entity_layout_matches",
            "structure_candidate_matches",
            "vec3_candidate_matches",
            "vec3_behavior_summary",
            "value_candidate_matches",
            "scalar_behavior_summary",
            "warnings");

    [Fact]
    public void Region_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RegionComparison(),
            "base_address_hex",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_rank_score",
            "session_b_rank_score",
            "score_delta",
            "session_a_unique_hashes",
            "session_b_unique_hashes",
            "recommendation");

    [Fact]
    public void Cluster_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ClusterComparison(),
            "base_address_hex",
            "start_offset_hex",
            "end_offset_hex",
            "session_a_cluster_id",
            "session_b_cluster_id",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_rank_score",
            "session_b_rank_score",
            "candidate_count_delta",
            "overlap_bytes",
            "recommendation");

    [Fact]
    public void Entity_layout_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new EntityLayoutComparison(),
            "base_address_hex",
            "start_offset_hex",
            "end_offset_hex",
            "layout_kind",
            "stride_bytes",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_score",
            "session_b_score",
            "score_delta",
            "session_a_cluster_count",
            "session_b_cluster_count",
            "session_a_vec3_candidate_count",
            "session_b_vec3_candidate_count",
            "overlap_bytes",
            "recommendation");

    [Fact]
    public void Structure_candidate_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new StructureCandidateComparison(),
            "base_address_hex",
            "offset_hex",
            "structure_kind",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_score",
            "session_b_score",
            "score_delta",
            "session_a_snapshot_support",
            "session_b_snapshot_support",
            "session_a_value_sequence_summary",
            "session_b_value_sequence_summary",
            "session_a_analyzer_sources",
            "session_b_analyzer_sources",
            "recommendation");

    [Fact]
    public void Vec3_candidate_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new Vec3CandidateComparison(),
            "base_address_hex",
            "offset_hex",
            "data_type",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_rank_score",
            "session_b_rank_score",
            "score_delta",
            "session_a_snapshot_support",
            "session_b_snapshot_support",
            "session_a_stimulus_label",
            "session_b_stimulus_label",
            "session_a_behavior_score",
            "session_b_behavior_score",
            "behavior_score_delta",
            "session_a_value_delta_magnitude",
            "session_b_value_delta_magnitude",
            "session_a_validation_status",
            "session_b_validation_status",
            "session_a_value_sequence_summary",
            "session_b_value_sequence_summary",
            "session_a_analyzer_sources",
            "session_b_analyzer_sources",
            "recommendation");

    [Fact]
    public void Value_candidate_comparison_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ValueCandidateComparison(),
            "base_address_hex",
            "offset_hex",
            "data_type",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_rank_score",
            "session_b_rank_score",
            "score_delta",
            "session_a_distinct_values",
            "session_b_distinct_values",
            "session_a_changed_sample_count",
            "session_b_changed_sample_count",
            "session_a_stimulus_label",
            "session_b_stimulus_label",
            "session_a_value_sequence_summary",
            "session_b_value_sequence_summary",
            "session_a_analyzer_sources",
            "session_b_analyzer_sources",
            "recommendation");

    [Fact]
    public void Scalar_behavior_summary_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarBehaviorSummary(),
            "matching_scalar_candidate_count",
            "heuristic_candidate_count",
            "strong_candidate_count",
            "stimulus_labels",
            "scalar_behavior_candidates",
            "next_recommended_action");

    [Fact]
    public void Scalar_behavior_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarBehaviorCandidate(),
            "classification",
            "base_address_hex",
            "offset_hex",
            "data_type",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_stimulus_label",
            "session_b_stimulus_label",
            "session_a_changed_sample_count",
            "session_b_changed_sample_count",
            "session_a_value_delta_magnitude",
            "session_b_value_delta_magnitude",
            "session_a_circular_delta_magnitude",
            "session_b_circular_delta_magnitude",
            "session_a_signed_circular_delta",
            "session_b_signed_circular_delta",
            "session_a_dominant_direction",
            "session_b_dominant_direction",
            "turn_polarity_relationship",
            "camera_turn_separation_relationship",
            "value_family",
            "score_total",
            "score_breakdown",
            "confidence_level",
            "supporting_reasons",
            "rejection_reasons",
            "evidence_summary",
            "next_validation_step");

    [Fact]
    public void Vec3_behavior_summary_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new Vec3BehaviorSummary(),
            "matching_vec3_candidate_count",
            "behavior_contrast_count",
            "behavior_consistent_match_count",
            "unlabeled_match_count",
            "stimulus_labels",
            "behavior_contrast_candidates",
            "next_recommended_action");

    [Fact]
    public void Vec3_behavior_contrast_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new Vec3BehaviorContrastCandidate(),
            "classification",
            "base_address_hex",
            "offset_hex",
            "data_type",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "session_a_stimulus_label",
            "session_b_stimulus_label",
            "session_a_value_delta_magnitude",
            "session_b_value_delta_magnitude",
            "score_total",
            "score_breakdown",
            "confidence_level",
            "supporting_reasons",
            "rejection_reasons",
            "evidence_summary",
            "next_validation_step");

    [Fact]
    public void Comparison_next_capture_plan_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonNextCapturePlan(),
            "schema_version",
            "session_a_id",
            "session_b_id",
            "recommended_mode",
            "target_region_priorities",
            "reason",
            "expected_signal",
            "stop_condition",
            "warnings");

    [Fact]
    public void Comparison_capture_target_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonCaptureTarget(),
            "base_address_hex",
            "offset_hex",
            "data_type",
            "session_a_region_id",
            "session_b_region_id",
            "session_a_candidate_id",
            "session_b_candidate_id",
            "priority_score",
            "reason");

    [Fact]
    public void Comparison_truth_readiness_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessResult(),
            "schema_version",
            "success",
            "session_a_id",
            "session_b_id",
            "session_a_path",
            "session_b_path",
            "output_path",
            "entity_layout",
            "position",
            "actor_yaw",
            "camera_orientation",
            "next_required_capture",
            "top_entity_layout_matches",
            "top_vec3_behavior_candidates",
            "top_scalar_behavior_candidates",
            "warnings");

    [Fact]
    public void Comparison_truth_readiness_status_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessStatus(),
            "component",
            "readiness",
            "evidence_count",
            "confidence_score",
            "primary_reason",
            "next_action",
            "blocking_gaps");

    [Fact]
    public void Comparison_truth_readiness_capture_requirement_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessCaptureRequirement(),
            "mode",
            "reason",
            "expected_signal",
            "stop_condition",
            "target_count",
            "target_preview");

    [Fact]
    public void Comparison_truth_readiness_target_preview_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessTargetPreview(),
            "base_address_hex",
            "offset_hex",
            "data_type",
            "priority_score",
            "reason");

    [Fact]
    public void Comparison_truth_readiness_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessVerificationResult(),
            "schema_version",
            "success",
            "path",
            "issues");

    [Fact]
    public void Comparison_truth_readiness_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ComparisonTruthReadinessVerificationIssue(),
            "severity",
            "code",
            "message");

    [Fact]
    public void Scalar_evidence_set_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarEvidenceSetVerificationResult(),
            "schema_version",
            "success",
            "path",
            "session_count",
            "ranked_candidate_count",
            "rejected_summary_count",
            "issues");

    [Fact]
    public void Scalar_evidence_set_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarEvidenceSetVerificationIssue(),
            "severity",
            "code",
            "message",
            "candidate_index");

    [Fact]
    public void Scalar_truth_recovery_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarTruthRecoveryVerificationResult(),
            "schema_version",
            "success",
            "path",
            "truth_candidate_path_count",
            "input_candidate_count",
            "recovered_candidate_count",
            "issues");

    [Fact]
    public void Scalar_truth_recovery_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarTruthRecoveryVerificationIssue(),
            "severity",
            "code",
            "message",
            "candidate_index");

    [Fact]
    public void Scalar_truth_promotion_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarTruthPromotionResult(),
            "schema_version",
            "success",
            "recovery_path",
            "corroboration_path",
            "output_path",
            "recovered_candidate_count",
            "promoted_candidate_count",
            "blocked_candidate_count",
            "promoted_candidates",
            "blocked_candidates",
            "warnings");

    [Fact]
    public void Scalar_promoted_truth_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarPromotedTruthCandidate(),
            "schema_version",
            "candidate_id",
            "source_recovered_candidate_id",
            "base_address_hex",
            "offset_hex",
            "data_type",
            "value_family",
            "classification",
            "promotion_status",
            "truth_readiness",
            "claim_level",
            "corroboration_status",
            "corroboration_sources",
            "corroboration_summary",
            "supporting_truth_candidate_ids",
            "supporting_file_count",
            "best_score_total",
            "labels_present",
            "supporting_reasons",
            "evidence_summary",
            "next_validation_step",
            "warning");

    [Fact]
    public void Scalar_truth_promotion_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarTruthPromotionVerificationResult(),
            "schema_version",
            "success",
            "path",
            "promoted_candidate_count",
            "blocked_candidate_count",
            "issues");

    [Fact]
    public void Scalar_truth_promotion_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarTruthPromotionVerificationIssue(),
            "severity",
            "code",
            "message",
            "candidate_index");

    [Fact]
    public void Scalar_promotion_review_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarPromotionReviewResult(),
            "schema_version",
            "success",
            "promotion_path",
            "output_path",
            "markdown_report_path",
            "decision_state",
            "review_candidate_count",
            "ready_for_manual_truth_review_count",
            "blocked_conflict_count",
            "needs_more_corroboration_count",
            "needs_repeat_capture_count",
            "do_not_promote_count",
            "candidate_reviews",
            "warnings");

    [Fact]
    public void Scalar_promotion_review_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarPromotionReviewCandidate(),
            "schema_version",
            "review_candidate_id",
            "source_promotion_candidate_id",
            "source_recovered_candidate_id",
            "base_address_hex",
            "offset_hex",
            "data_type",
            "classification",
            "source_promotion_status",
            "source_truth_readiness",
            "source_claim_level",
            "source_corroboration_status",
            "best_score_total",
            "supporting_file_count",
            "supporting_truth_candidate_ids",
            "corroboration_sources",
            "decision_state",
            "decision_reason",
            "blocking_gaps",
            "evidence_summary",
            "next_action",
            "manual_confirmation_required",
            "final_truth_claim");

    [Fact]
    public void Scalar_promotion_review_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarPromotionReviewVerificationResult(),
            "schema_version",
            "success",
            "path",
            "decision_state",
            "review_candidate_count",
            "ready_for_manual_truth_review_count",
            "blocked_conflict_count",
            "issues");

    [Fact]
    public void Scalar_promotion_review_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new ScalarPromotionReviewVerificationIssue(),
            "severity",
            "code",
            "message",
            "candidate_index");

    [Fact]
    public void Capability_status_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new CapabilityStatusResult(),
            "schema_version",
            "success",
            "generated_utc",
            "project",
            "capability_count",
            "capabilities",
            "truth_readiness_path",
            "truth_readiness_paths",
            "scalar_evidence_set_path",
            "scalar_evidence_set_paths",
            "scalar_truth_recovery_path",
            "scalar_truth_recovery_paths",
            "scalar_truth_promotion_path",
            "scalar_truth_promotion_paths",
            "scalar_promotion_review_path",
            "scalar_promotion_review_paths",
            "truth_components",
            "evidence_missing",
            "next_recommended_actions",
            "warnings");

    [Fact]
    public void Capability_status_entry_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new CapabilityStatusEntry(),
            "name",
            "status",
            "primary_command",
            "evidence_surface",
            "output_artifacts",
            "remaining_gap");

    [Fact]
    public void Capability_truth_component_status_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new CapabilityTruthComponentStatus(),
            "component",
            "code_status",
            "evidence_readiness",
            "evidence_count",
            "next_action");

    [Fact]
    public void Capability_status_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new CapabilityStatusVerificationResult(),
            "schema_version",
            "success",
            "path",
            "capability_count",
            "issues");

    [Fact]
    public void Capability_status_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new CapabilityStatusVerificationIssue(),
            "severity",
            "code",
            "message");

    private static void AssertJsonPropertySet<T>(T value, params string[] expectedProperties)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, SessionJson.Options));
        var actualProperties = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedProperties
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actualProperties);
    }
}
