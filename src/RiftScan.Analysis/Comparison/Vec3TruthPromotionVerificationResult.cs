using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3TruthPromotionVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_truth_promotion_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("promoted_candidate_count")]
    public int PromotedCandidateCount { get; init; }

    [JsonPropertyName("blocked_candidate_count")]
    public int BlockedCandidateCount { get; init; }

    [JsonPropertyName("recommended_manual_review_candidate_id")]
    public string? RecommendedManualReviewCandidateId { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<Vec3TruthPromotionVerificationIssue> Issues { get; init; } = [];
}

public sealed record Vec3TruthPromotionVerificationIssue
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
