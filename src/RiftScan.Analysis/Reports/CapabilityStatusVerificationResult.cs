using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Reports;

public sealed record CapabilityStatusVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.capability_status_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("capability_count")]
    public int CapabilityCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<CapabilityStatusVerificationIssue> Issues { get; init; } = [];
}

public sealed record CapabilityStatusVerificationIssue
{
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
