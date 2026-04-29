using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionMigrationPlan
{
    [JsonPropertyName("plan_schema_version")]
    public string PlanSchemaVersion { get; init; } = "riftscan.session_migration_plan.v1";

    [JsonPropertyName("source_session_path")]
    public string SourceSessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("from_schema_version")]
    public string? FromSchemaVersion { get; init; }

    [JsonPropertyName("to_schema_version")]
    public string ToSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("dry_run")]
    public bool DryRun { get; init; } = true;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("can_apply")]
    public bool CanApply { get; init; }

    [JsonPropertyName("raw_data_policy")]
    public string RawDataPolicy { get; init; } = "preserve_raw_artifacts_no_mutation";

    [JsonPropertyName("actions")]
    public IReadOnlyList<SessionMigrationPlanAction> Actions { get; init; } = [];
}

public sealed record SessionMigrationPlanAction
{
    [JsonPropertyName("action_id")]
    public string ActionId { get; init; } = string.Empty;

    [JsonPropertyName("action_type")]
    public string ActionType { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("writes_raw_artifacts")]
    public bool WritesRawArtifacts { get; init; }

    [JsonPropertyName("target_path")]
    public string? TargetPath { get; init; }
}
