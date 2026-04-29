using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
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
        Assert.Equal("riftscan.session_summary_result.v1", ReadSchema(new SessionSummaryResult()));
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
