using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarTruthCorroborationVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_truth_corroboration_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("entry_count")]
    public int EntryCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<ScalarTruthCorroborationVerificationIssue> Issues { get; init; } = [];
}

public sealed record ScalarTruthCorroborationVerificationIssue
{
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("line_number")]
    public int? LineNumber { get; init; }
}
