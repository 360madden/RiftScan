using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionInventoryResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_inventory_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.All(issue => !StringComparer.OrdinalIgnoreCase.Equals(issue.Severity, "error"));

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("inventory_path")]
    public string? InventoryPath { get; init; }

    [JsonPropertyName("raw_data_policy")]
    public string RawDataPolicy { get; init; } = "preserve_raw_artifacts_no_mutation";

    [JsonPropertyName("summary")]
    public SessionSummaryResult Summary { get; init; } = new();

    [JsonPropertyName("prune_inventory")]
    public SessionPruneResult PruneInventory { get; init; } = new();

    [JsonPropertyName("issues")]
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
}
