using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionPruneResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_prune_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.All(issue => !StringComparer.OrdinalIgnoreCase.Equals(issue.Severity, "error"));

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("dry_run")]
    public bool DryRun { get; init; } = true;

    [JsonPropertyName("raw_data_policy")]
    public string RawDataPolicy { get; init; } = "preserve_raw_artifacts_no_mutation";

    [JsonPropertyName("inventory_path")]
    public string? InventoryPath { get; init; }

    [JsonPropertyName("candidate_count")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("bytes_reclaimable")]
    public long BytesReclaimable { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<SessionPruneCandidate> Candidates { get; init; } = [];

    [JsonPropertyName("issues")]
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
}

public sealed record SessionPruneCandidate
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("bytes")]
    public long Bytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
