using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarTruthRecoveryResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_truth_recovery.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("truth_candidate_paths")]
    public IReadOnlyList<string> TruthCandidatePaths { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("input_candidate_count")]
    public int InputCandidateCount { get; init; }

    [JsonPropertyName("recovered_candidate_count")]
    public int RecoveredCandidateCount { get; init; }

    [JsonPropertyName("recovered_candidates")]
    public IReadOnlyList<ScalarRecoveredTruthCandidate> RecoveredCandidates { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record ScalarRecoveredTruthCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_recovered_truth_candidate.v1";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

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

    [JsonPropertyName("truth_readiness")]
    public string TruthReadiness { get; init; } = "recovered_candidate";

    [JsonPropertyName("claim_level")]
    public string ClaimLevel { get; init; } = "recovered_candidate";

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

    [JsonPropertyName("warning")]
    public string Warning { get; init; } = "recovered_candidate_requires_review_before_final_truth_claim";
}
