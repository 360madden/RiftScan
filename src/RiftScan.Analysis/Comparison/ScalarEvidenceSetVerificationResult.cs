using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarEvidenceSetVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_evidence_set_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("session_count")]
    public int SessionCount { get; init; }

    [JsonPropertyName("ranked_candidate_count")]
    public int RankedCandidateCount { get; init; }

    [JsonPropertyName("rejected_summary_count")]
    public int RejectedSummaryCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<ScalarEvidenceSetVerificationIssue> Issues { get; init; } = [];
}

public sealed record ScalarEvidenceSetVerificationIssue
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
