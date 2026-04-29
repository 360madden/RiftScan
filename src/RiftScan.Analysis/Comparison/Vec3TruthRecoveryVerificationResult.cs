using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3TruthRecoveryVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_truth_recovery_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("truth_candidate_path_count")]
    public int TruthCandidatePathCount { get; init; }

    [JsonPropertyName("input_candidate_count")]
    public int InputCandidateCount { get; init; }

    [JsonPropertyName("recovered_candidate_count")]
    public int RecoveredCandidateCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<Vec3TruthRecoveryVerificationIssue> Issues { get; init; } = [];
}

public sealed record Vec3TruthRecoveryVerificationIssue
{
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("candidate_index")]
    public int? CandidateIndex { get; init; }
}
