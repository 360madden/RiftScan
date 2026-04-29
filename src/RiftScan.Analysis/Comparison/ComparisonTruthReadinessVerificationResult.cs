using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ComparisonTruthReadinessVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.comparison_truth_readiness_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("issues")]
    public IReadOnlyList<ComparisonTruthReadinessVerificationIssue> Issues { get; init; } = [];
}

public sealed record ComparisonTruthReadinessVerificationIssue
{
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
