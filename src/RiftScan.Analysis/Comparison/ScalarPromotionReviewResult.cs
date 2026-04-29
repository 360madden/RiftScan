using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarPromotionReviewResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_promotion_review.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("promotion_path")]
    public string PromotionPath { get; init; } = string.Empty;

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("decision_state")]
    public string DecisionState { get; init; } = "do_not_promote";

    [JsonPropertyName("review_candidate_count")]
    public int ReviewCandidateCount { get; init; }

    [JsonPropertyName("ready_for_manual_truth_review_count")]
    public int ReadyForManualTruthReviewCount { get; init; }

    [JsonPropertyName("blocked_conflict_count")]
    public int BlockedConflictCount { get; init; }

    [JsonPropertyName("needs_more_corroboration_count")]
    public int NeedsMoreCorroborationCount { get; init; }

    [JsonPropertyName("needs_repeat_capture_count")]
    public int NeedsRepeatCaptureCount { get; init; }

    [JsonPropertyName("do_not_promote_count")]
    public int DoNotPromoteCount { get; init; }

    [JsonPropertyName("candidate_reviews")]
    public IReadOnlyList<ScalarPromotionReviewCandidate> CandidateReviews { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record ScalarPromotionReviewCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_promotion_review_candidate.v1";

    [JsonPropertyName("review_candidate_id")]
    public string ReviewCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_promotion_candidate_id")]
    public string SourcePromotionCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_recovered_candidate_id")]
    public string SourceRecoveredCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("source_promotion_status")]
    public string SourcePromotionStatus { get; init; } = string.Empty;

    [JsonPropertyName("source_truth_readiness")]
    public string SourceTruthReadiness { get; init; } = string.Empty;

    [JsonPropertyName("source_claim_level")]
    public string SourceClaimLevel { get; init; } = string.Empty;

    [JsonPropertyName("source_corroboration_status")]
    public string SourceCorroborationStatus { get; init; } = string.Empty;

    [JsonPropertyName("best_score_total")]
    public double BestScoreTotal { get; init; }

    [JsonPropertyName("supporting_file_count")]
    public int SupportingFileCount { get; init; }

    [JsonPropertyName("supporting_truth_candidate_ids")]
    public IReadOnlyList<string> SupportingTruthCandidateIds { get; init; } = [];

    [JsonPropertyName("corroboration_sources")]
    public IReadOnlyList<string> CorroborationSources { get; init; } = [];

    [JsonPropertyName("decision_state")]
    public string DecisionState { get; init; } = "do_not_promote";

    [JsonPropertyName("decision_reason")]
    public string DecisionReason { get; init; } = string.Empty;

    [JsonPropertyName("blocking_gaps")]
    public IReadOnlyList<string> BlockingGaps { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_action")]
    public string NextAction { get; init; } = string.Empty;

    [JsonPropertyName("manual_confirmation_required")]
    public bool ManualConfirmationRequired { get; init; } = true;

    [JsonPropertyName("final_truth_claim")]
    public bool FinalTruthClaim { get; init; }
}
