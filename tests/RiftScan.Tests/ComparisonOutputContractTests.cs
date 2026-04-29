using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ComparisonOutputContractTests
{
    [Fact]
    public void Session_comparison_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionComparisonResult(),
            "success",
            "session_a_path",
            "session_b_path",
            "comparison_path",
            "comparison_report_path",
            "comparison_next_capture_plan_path",
            "session_a_id",
            "session_b_id",
            "same_process_name",
            "matching_region_count",
            "matching_cluster_count",
            "matching_structure_candidate_count",
            "matching_vec3_candidate_count",
            "matching_value_candidate_count",
            "region_matches",
            "cluster_matches",
            "structure_candidate_matches",
            "vec3_candidate_matches",
            "vec3_behavior_summary",
            "value_candidate_matches",
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
            "session_a_value_sequence_summary",
            "session_b_value_sequence_summary",
            "recommendation");

    [Fact]
    public void Vec3_behavior_summary_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new Vec3BehaviorSummary(),
            "matching_vec3_candidate_count",
            "behavior_contrast_count",
            "behavior_consistent_match_count",
            "unlabeled_match_count",
            "stimulus_labels",
            "next_recommended_action");

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
