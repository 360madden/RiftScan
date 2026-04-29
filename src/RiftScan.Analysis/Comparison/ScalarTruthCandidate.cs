using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarTruthCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_truth_candidate.v1";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_schema_version")]
    public string SourceSchemaVersion { get; init; } = string.Empty;

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

    [JsonPropertyName("corroboration_status")]
    public string CorroborationStatus { get; init; } = "not_requested";

    [JsonPropertyName("corroboration_sources")]
    public IReadOnlyList<string> CorroborationSources { get; init; } = [];

    [JsonPropertyName("corroboration_summary")]
    public string CorroborationSummary { get; init; } = string.Empty;

    [JsonPropertyName("labels_present")]
    public IReadOnlyList<string> LabelsPresent { get; init; } = [];

    [JsonPropertyName("source_session_count")]
    public int SourceSessionCount { get; init; }

    [JsonPropertyName("passive_stable")]
    public bool PassiveStable { get; init; }

    [JsonPropertyName("opposite_turn_polarity")]
    public bool OppositeTurnPolarity { get; init; }

    [JsonPropertyName("camera_turn_separation")]
    public string CameraTurnSeparation { get; init; } = string.Empty;

    [JsonPropertyName("supporting_reasons")]
    public IReadOnlyList<string> SupportingReasons { get; init; } = [];

    [JsonPropertyName("rejection_reasons")]
    public IReadOnlyList<string> RejectionReasons { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_validation_step")]
    public string NextValidationStep { get; init; } = string.Empty;

    [JsonPropertyName("warning")]
    public string Warning { get; init; } = "candidate_evidence_not_recovered_truth";
}
