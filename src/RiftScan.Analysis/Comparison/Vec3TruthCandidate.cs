using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3TruthCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_truth_candidate.v1";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_schema_version")]
    public string SourceSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "vec3_float32";

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("score_total")]
    public double ScoreTotal { get; init; }

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = string.Empty;

    [JsonPropertyName("truth_readiness")]
    public string TruthReadiness { get; init; } = string.Empty;

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = string.Empty;

    [JsonPropertyName("claim_level")]
    public string ClaimLevel { get; init; } = string.Empty;

    [JsonPropertyName("source_session_count")]
    public int SourceSessionCount { get; init; }

    [JsonPropertyName("session_a_id")]
    public string SessionAId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_id")]
    public string SessionBId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_stimulus_label")]
    public string SessionAStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_b_stimulus_label")]
    public string SessionBStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("passive_stable")]
    public bool PassiveStable { get; init; }

    [JsonPropertyName("move_forward_changed")]
    public bool MoveForwardChanged { get; init; }

    [JsonPropertyName("session_a_value_delta_magnitude")]
    public double SessionAValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_b_value_delta_magnitude")]
    public double SessionBValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_a_value_sequence_summary")]
    public string SessionAValueSequenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("session_b_value_sequence_summary")]
    public string SessionBValueSequenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("session_a_analyzer_sources")]
    public IReadOnlyList<string> SessionAAnalyzerSources { get; init; } = [];

    [JsonPropertyName("session_b_analyzer_sources")]
    public IReadOnlyList<string> SessionBAnalyzerSources { get; init; } = [];

    [JsonPropertyName("corroboration_status")]
    public string CorroborationStatus { get; init; } = "not_requested";

    [JsonPropertyName("corroboration_sources")]
    public IReadOnlyList<string> CorroborationSources { get; init; } = [];

    [JsonPropertyName("corroboration_summary")]
    public string CorroborationSummary { get; init; } = string.Empty;

    [JsonPropertyName("supporting_reasons")]
    public IReadOnlyList<string> SupportingReasons { get; init; } = [];

    [JsonPropertyName("rejection_reasons")]
    public IReadOnlyList<string> RejectionReasons { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_validation_step")]
    public string NextValidationStep { get; init; } = string.Empty;

    [JsonPropertyName("external_truth_source_hint")]
    public string ExternalTruthSourceHint { get; init; } = "addon_waypoint_or_player_coord_truth";

    [JsonPropertyName("warning")]
    public string Warning { get; init; } = "candidate_evidence_not_recovered_coordinate_truth";
}
