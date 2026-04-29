using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarTruthPromotionResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_truth_promotion.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("recovery_path")]
    public string RecoveryPath { get; init; } = string.Empty;

    [JsonPropertyName("corroboration_path")]
    public string CorroborationPath { get; init; } = string.Empty;

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("recovered_candidate_count")]
    public int RecoveredCandidateCount { get; init; }

    [JsonPropertyName("promoted_candidate_count")]
    public int PromotedCandidateCount { get; init; }

    [JsonPropertyName("blocked_candidate_count")]
    public int BlockedCandidateCount { get; init; }

    [JsonPropertyName("promoted_candidates")]
    public IReadOnlyList<ScalarPromotedTruthCandidate> PromotedCandidates { get; init; } = [];

    [JsonPropertyName("blocked_candidates")]
    public IReadOnlyList<ScalarPromotedTruthCandidate> BlockedCandidates { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record ScalarPromotedTruthCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_promoted_truth_candidate.v1";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_recovered_candidate_id")]
    public string SourceRecoveredCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("value_family")]
    public string ValueFamily { get; init; } = string.Empty;

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("promotion_status")]
    public string PromotionStatus { get; init; } = "recovered_candidate";

    [JsonPropertyName("truth_readiness")]
    public string TruthReadiness { get; init; } = "recovered_candidate";

    [JsonPropertyName("claim_level")]
    public string ClaimLevel { get; init; } = "recovered_candidate";

    [JsonPropertyName("corroboration_status")]
    public string CorroborationStatus { get; init; } = "uncorroborated";

    [JsonPropertyName("corroboration_sources")]
    public IReadOnlyList<string> CorroborationSources { get; init; } = [];

    [JsonPropertyName("corroboration_summary")]
    public string CorroborationSummary { get; init; } = string.Empty;

    [JsonPropertyName("supporting_truth_candidate_ids")]
    public IReadOnlyList<string> SupportingTruthCandidateIds { get; init; } = [];

    [JsonPropertyName("supporting_file_count")]
    public int SupportingFileCount { get; init; }

    [JsonPropertyName("best_score_total")]
    public double BestScoreTotal { get; init; }

    [JsonPropertyName("labels_present")]
    public IReadOnlyList<string> LabelsPresent { get; init; } = [];

    [JsonPropertyName("supporting_reasons")]
    public IReadOnlyList<string> SupportingReasons { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_validation_step")]
    public string NextValidationStep { get; init; } = string.Empty;

    [JsonPropertyName("warning")]
    public string Warning { get; init; } = "promoted_candidate_requires_manual_review_before_final_truth_claim";
}
