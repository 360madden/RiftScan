using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Xrefs;
using RiftScan.Core.Sessions;

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
