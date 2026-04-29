using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarPromotionReviewVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_promotion_review_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("decision_state")]
    public string DecisionState { get; init; } = string.Empty;

    [JsonPropertyName("review_candidate_count")]
    public int ReviewCandidateCount { get; init; }

    [JsonPropertyName("ready_for_manual_truth_review_count")]
    public int ReadyForManualTruthReviewCount { get; init; }

    [JsonPropertyName("blocked_conflict_count")]
    public int BlockedConflictCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<ScalarPromotionReviewVerificationIssue> Issues { get; init; } = [];
}

public sealed record ScalarPromotionReviewVerificationIssue
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
