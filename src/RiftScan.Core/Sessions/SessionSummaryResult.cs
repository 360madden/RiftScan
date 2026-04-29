using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionSummaryResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_summary_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.All(issue => !StringComparer.OrdinalIgnoreCase.Equals(issue.Severity, "error"));

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("schema_version")]
    public string? SchemaVersion { get; init; }

    [JsonPropertyName("process_name")]
    public string? ProcessName { get; init; }

    [JsonPropertyName("capture_mode")]
    public string? CaptureMode { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("region_count")]
    public int RegionCount { get; init; }

    [JsonPropertyName("total_bytes_raw")]
    public long TotalBytesRaw { get; init; }

    [JsonPropertyName("total_bytes_stored")]
    public long TotalBytesStored { get; init; }

    [JsonPropertyName("summary_path")]
    public string? SummaryPath { get; init; }

    [JsonPropertyName("artifact_count")]
    public int ArtifactCount { get; init; }

    [JsonPropertyName("artifact_bytes")]
    public long ArtifactBytes { get; init; }

    [JsonPropertyName("generated_artifacts")]
    public IReadOnlyList<SessionSummaryArtifact> GeneratedArtifacts { get; init; } = [];

    [JsonPropertyName("issues")]
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
}

public sealed record SessionSummaryArtifact
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("bytes")]
    public long Bytes { get; init; }

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;
}
