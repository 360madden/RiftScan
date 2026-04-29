using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionMigrationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_migration_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.All(issue => !StringComparer.OrdinalIgnoreCase.Equals(issue.Severity, "error"));

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

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

    [JsonPropertyName("migration_output_path")]
    public string? MigrationOutputPath { get; init; }

    [JsonPropertyName("artifacts_written")]
    public IReadOnlyList<string> ArtifactsWritten { get; init; } = [];

    [JsonPropertyName("issues")]
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
}
