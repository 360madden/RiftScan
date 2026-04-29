using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3TruthRecoveryResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_truth_recovery.v1";

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
    public IReadOnlyList<Vec3RecoveredTruthCandidate> RecoveredCandidates { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record Vec3RecoveredTruthCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_recovered_truth_candidate.v1";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "vec3_float32";

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

    [JsonPropertyName("external_truth_source_hint")]
    public string ExternalTruthSourceHint { get; init; } = "addon_waypoint_or_player_coord_truth";

    [JsonPropertyName("warning")]
    public string Warning { get; init; } = "recovered_coordinate_candidate_requires_addon_waypoint_review_before_final_truth_claim";
}
