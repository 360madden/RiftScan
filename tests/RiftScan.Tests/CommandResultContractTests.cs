using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Xrefs;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class CommandResultContractTests
{
    [Fact]
    public void Session_analysis_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionAnalysisResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "regions_analyzed",
            "artifacts_written");

    [Fact]
    public void Session_report_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionReportResult(),
            "result_schema_version",
            "success",
            "session_path",
            "report_path",
            "report_json_path");

    [Fact]
    public void Session_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionVerificationResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "artifacts_verified",
            "issues");

    [Fact]
    public void Session_migration_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionMigrationResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "from_schema_version",
            "to_schema_version",
            "dry_run",
            "status",
            "migration_output_path",
            "artifacts_written",
            "issues");

    [Fact]
    public void Session_prune_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionPruneResult(),
            "result_schema_version",
            "success",
            "session_path",
            "dry_run",
            "raw_data_policy",
            "inventory_path",
            "candidate_count",
            "bytes_reclaimable",
            "candidates",
            "issues");

    [Fact]
    public void Session_inventory_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionInventoryResult(),
            "result_schema_version",
            "success",
            "session_path",
            "inventory_path",
            "raw_data_policy",
            "summary",
            "prune_inventory",
            "issues");

    [Fact]
    public void Session_summary_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionSummaryResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "schema_version",
            "process_name",
            "capture_mode",
            "status",
            "snapshot_count",
            "region_count",
            "total_bytes_raw",
            "total_bytes_stored",
            "summary_path",
            "artifact_count",
            "artifact_bytes",
            "generated_artifacts",
            "issues");

    [Fact]
    public void Session_summary_artifact_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionSummaryArtifact(),
            "path",
            "bytes",
            "kind");

    [Fact]
    public void Session_xref_analysis_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefAnalysisResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "analyzer_id",
            "analyzer_version",
            "analyzer_sources",
            "target_base_address_hex",
            "target_size_bytes",
            "target_region_ids",
            "target_offsets",
            "snapshot_count",
            "region_count",
            "regions_scanned",
            "bytes_scanned",
            "pointer_hit_count",
            "exact_target_pointer_count",
            "outside_target_region_pointer_count",
            "outside_exact_target_pointer_count",
            "pointer_hits",
            "pattern_definition_count",
            "pattern_hit_count",
            "outside_target_region_pattern_hit_count",
            "pattern_hits",
            "output_path",
            "markdown_report_path",
            "warnings",
            "diagnostics");

    [Fact]
    public void Session_xref_target_offset_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefTargetOffset(),
            "offset_hex",
            "absolute_address_hex");

    [Fact]
    public void Session_xref_pointer_hit_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefPointerHit(),
            "snapshot_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "pointer_value_hex",
            "target_offset_hex",
            "match_kind",
            "source_is_target_region");

    [Fact]
    public void Session_xref_pattern_hit_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefPatternHit(),
            "pattern_id",
            "pattern_source_snapshot_id",
            "pattern_source_offset_hex",
            "pattern_length_bytes",
            "snapshot_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "match_kind",
            "source_is_target_region");

    [Fact]
    public void Session_xref_chain_summary_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefChainSummaryResult(),
            "result_schema_version",
            "success",
            "input_paths",
            "input_count",
            "min_support",
            "top_limit",
            "stable_edge_count",
            "reciprocal_pair_count",
            "stable_edges",
            "reciprocal_pairs",
            "output_path",
            "markdown_report_path",
            "warnings",
            "diagnostics");

    [Fact]
    public void Session_xref_stable_edge_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefStableEdge(),
            "edge_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "pointer_value_hex",
            "target_offset_hex",
            "match_kind",
            "source_is_target_region",
            "classification",
            "support_count",
            "input_path_count",
            "supporting_input_paths",
            "supporting_snapshot_ids",
            "target_base_address_hexes",
            "evidence_summary");

    [Fact]
    public void Session_xref_reciprocal_pair_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefReciprocalPair(),
            "pair_id",
            "first_base_address_hex",
            "second_base_address_hex",
            "first_to_second_edge_ids",
            "second_to_first_edge_ids",
            "support_count",
            "evidence_summary");

    [Fact]
    public void Session_xref_required_edge_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefRequiredEdge(),
            "source_base_address_hex",
            "pointer_value_hex");

    [Fact]
    public void Session_xref_required_reciprocal_pair_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefRequiredReciprocalPair(),
            "first_base_address_hex",
            "second_base_address_hex");

    [Fact]
    public void Session_xref_chain_summary_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefChainSummaryVerificationResult(),
            "result_schema_version",
            "success",
            "path",
            "min_support",
            "required_edge_count",
            "required_reciprocal_pair_count",
            "stable_edge_count",
            "reciprocal_pair_count",
            "issues");

    [Fact]
    public void Session_xref_chain_summary_verification_issue_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionXrefChainSummaryVerificationIssue(),
            "code",
            "message",
            "severity");

    [Fact]
    public void Rift_session_addon_coordinate_match_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftSessionAddonCoordinateMatchResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "observation_path",
            "analyzer_id",
            "analyzer_version",
            "analyzer_sources",
            "tolerance",
            "top_limit",
            "latest_only",
            "region_base_filters",
            "latest_observation_utc",
            "observation_count",
            "observations_used",
            "snapshot_count",
            "regions_scanned",
            "bytes_scanned",
            "match_count",
            "candidate_count",
            "candidates",
            "matches",
            "output_path",
            "markdown_report_path",
            "warnings",
            "diagnostics");

    [Fact]
    public void Rift_session_addon_coordinate_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftSessionAddonCoordinateCandidate(),
            "candidate_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "axis_order",
            "support_count",
            "observation_support_count",
            "best_max_abs_distance",
            "best_memory_x",
            "best_memory_y",
            "best_memory_z",
            "best_addon_x",
            "best_addon_y",
            "best_addon_z",
            "supporting_snapshot_ids",
            "supporting_observation_ids",
            "addon_sources",
            "zone_ids",
            "validation_status",
            "evidence_summary");

    [Fact]
    public void Rift_session_addon_coordinate_match_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftSessionAddonCoordinateMatch(),
            "match_id",
            "candidate_id",
            "snapshot_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "axis_order",
            "memory_x",
            "memory_y",
            "memory_z",
            "observation_id",
            "addon_name",
            "source_pattern",
            "addon_observed_x",
            "addon_observed_y",
            "addon_observed_z",
            "zone_id",
            "max_abs_distance",
            "evidence_summary");

    [Fact]
    public void Rift_addon_api_observation_scan_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftAddonApiObservationScanResult(),
            "result_schema_version",
            "success",
            "root_path_redacted",
            "jsonl_output_path",
            "files_scanned",
            "observation_count",
            "observations",
            "warnings");

    [Fact]
    public void Rift_addon_api_observation_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftAddonApiObservation(),
            "schema_version",
            "observation_id",
            "kind",
            "source_addon",
            "source_file_name",
            "source_path_redacted",
            "source_pattern",
            "line_number",
            "file_last_write_utc",
            "realtime",
            "api_source",
            "source_mode",
            "unit_id",
            "unit_name",
            "zone_id",
            "location_name",
            "coordinate_space",
            "confidence_level",
            "coord_x",
            "coord_y",
            "coord_z",
            "waypoint_x",
            "waypoint_z",
            "evidence_summary");

    [Fact]
    public void Rift_addon_coordinate_motion_comparison_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftAddonCoordinateMotionComparisonResult(),
            "result_schema_version",
            "success",
            "pre_match_path",
            "post_match_path",
            "pre_session_id",
            "post_session_id",
            "min_delta_distance",
            "mirror_epsilon",
            "top_limit",
            "pre_candidate_count",
            "post_candidate_count",
            "common_candidate_count",
            "moved_candidate_count",
            "candidate_deltas",
            "motion_cluster_count",
            "synchronized_mirror_cluster_count",
            "canonical_promotion_status",
            "motion_clusters",
            "output_path",
            "markdown_report_path",
            "warnings",
            "diagnostics");

    [Fact]
    public void Rift_addon_coordinate_motion_delta_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftAddonCoordinateMotionDelta(),
            "delta_id",
            "source_region_id",
            "source_base_address_hex",
            "source_offset_hex",
            "source_absolute_address_hex",
            "axis_order",
            "pre_memory_x",
            "pre_memory_y",
            "pre_memory_z",
            "post_memory_x",
            "post_memory_y",
            "post_memory_z",
            "delta_x",
            "delta_y",
            "delta_z",
            "delta_distance",
            "pre_best_max_abs_distance_to_addon",
            "post_best_max_abs_distance_to_addon",
            "pre_support_count",
            "post_support_count",
            "classification",
            "evidence_summary");

    [Fact]
    public void Rift_addon_coordinate_motion_cluster_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftAddonCoordinateMotionCluster(),
            "cluster_id",
            "representative_source_region_id",
            "representative_source_base_address_hex",
            "representative_source_offset_hex",
            "representative_source_absolute_address_hex",
            "axis_order",
            "candidate_count",
            "source_offsets",
            "source_absolute_addresses",
            "pre_memory_x",
            "pre_memory_y",
            "pre_memory_z",
            "post_memory_x",
            "post_memory_y",
            "post_memory_z",
            "delta_x",
            "delta_y",
            "delta_z",
            "delta_distance",
            "classification",
            "promotion_status",
            "evidence_summary");

    [Fact]
    public void Rift_coordinate_mirror_context_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftCoordinateMirrorContextResult(),
            "result_schema_version",
            "success",
            "motion_comparison_path",
            "session_path",
            "session_id",
            "analyzer_id",
            "analyzer_version",
            "analyzer_sources",
            "window_bytes",
            "max_pointer_hits",
            "top_limit",
            "motion_cluster_count",
            "context_count",
            "contexts",
            "output_path",
            "markdown_report_path",
            "warnings",
            "diagnostics");

    [Fact]
    public void Rift_coordinate_mirror_context_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftCoordinateMirrorContext(),
            "context_id",
            "motion_cluster_id",
            "representative_source_region_id",
            "representative_source_base_address_hex",
            "representative_source_offset_hex",
            "representative_source_absolute_address_hex",
            "axis_order",
            "candidate_count",
            "source_offsets",
            "source_absolute_addresses",
            "member_relative_offsets_bytes",
            "member_gap_bytes",
            "member_span_bytes",
            "local_window_start_offset_hex",
            "local_window_end_offset_hex",
            "local_window_size_bytes",
            "snapshots_inspected",
            "first_snapshot_id",
            "last_snapshot_id",
            "first_snapshot_finite_float32_count",
            "first_snapshot_plausible_vec3_count",
            "first_snapshot_readable_member_count",
            "first_snapshot_unique_member_value_count",
            "last_snapshot_unique_member_value_count",
            "first_snapshot_member_values",
            "last_snapshot_member_values",
            "pointer_like_value_count",
            "pointer_like_hits",
            "canonical_discriminator_status",
            "evidence_summary");

    [Fact]
    public void Rift_coordinate_mirror_member_value_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftCoordinateMirrorMemberValue(),
            "source_offset_hex",
            "source_absolute_address_hex",
            "readable",
            "x",
            "y",
            "z");

    [Fact]
    public void Rift_coordinate_mirror_pointer_hit_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RiftCoordinateMirrorPointerHit(),
            "source_offset_hex",
            "source_absolute_address_hex",
            "pointer_value_hex",
            "target_region_id",
            "target_base_address_hex",
            "target_offset_hex",
            "source_is_representative_region");

    [Fact]
    public void Session_prune_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionPruneCandidate(),
            "path",
            "bytes",
            "reason");

    [Fact]
    public void Session_migration_plan_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionMigrationPlan(),
            "plan_schema_version",
            "source_session_path",
            "session_id",
            "from_schema_version",
            "to_schema_version",
            "dry_run",
            "status",
            "can_apply",
            "raw_data_policy",
            "actions");

    [Fact]
    public void Session_migration_plan_action_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionMigrationPlanAction(),
            "action_id",
            "action_type",
            "description",
            "writes_raw_artifacts",
            "target_path");

    [Fact]
    public void Command_result_schema_versions_are_stable()
    {
        Assert.Equal("riftscan.session_analysis_result.v1", ReadSchema(new SessionAnalysisResult()));
        Assert.Equal("riftscan.session_report_result.v1", ReadSchema(new SessionReportResult()));
        Assert.Equal("riftscan.session_verification_result.v1", ReadSchema(new SessionVerificationResult()));
        Assert.Equal("riftscan.session_migration_result.v1", ReadSchema(new SessionMigrationResult()));
        Assert.Equal("riftscan.session_prune_result.v1", ReadSchema(new SessionPruneResult()));
        Assert.Equal("riftscan.session_inventory_result.v1", ReadSchema(new SessionInventoryResult()));
        Assert.Equal("riftscan.session_summary_result.v1", ReadSchema(new SessionSummaryResult()));
        Assert.Equal("riftscan.session_xref_analysis_result.v1", ReadSchema(new SessionXrefAnalysisResult()));
        Assert.Equal("riftscan.session_xref_chain_summary_result.v1", ReadSchema(new SessionXrefChainSummaryResult()));
        Assert.Equal("riftscan.session_xref_chain_summary_verification_result.v1", ReadSchema(new SessionXrefChainSummaryVerificationResult()));
        Assert.Equal("riftscan.rift_session_addon_coordinate_match_result.v1", ReadSchema(new RiftSessionAddonCoordinateMatchResult()));
        Assert.Equal("riftscan.rift_addon_api_observation_scan_result.v1", ReadSchema(new RiftAddonApiObservationScanResult()));
        Assert.Equal("riftscan.rift_addon_coordinate_motion_comparison_result.v1", ReadSchema(new RiftAddonCoordinateMotionComparisonResult()));
        Assert.Equal("riftscan.rift_coordinate_mirror_context_result.v1", ReadSchema(new RiftCoordinateMirrorContextResult()));
    }

    private static string? ReadSchema<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, SessionJson.Options));
        return document.RootElement.GetProperty("result_schema_version").GetString();
    }

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
