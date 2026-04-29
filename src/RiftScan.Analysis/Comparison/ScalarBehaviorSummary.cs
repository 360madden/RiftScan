using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarBehaviorSummary
{
    [JsonPropertyName("matching_scalar_candidate_count")]
    public int MatchingScalarCandidateCount { get; init; }

    [JsonPropertyName("heuristic_candidate_count")]
    public int HeuristicCandidateCount { get; init; }

    [JsonPropertyName("strong_candidate_count")]
    public int StrongCandidateCount { get; init; }

    [JsonPropertyName("stimulus_labels")]
    public IReadOnlyList<string> StimulusLabels { get; init; } = [];

    [JsonPropertyName("scalar_behavior_candidates")]
    public IReadOnlyList<ScalarBehaviorCandidate> ScalarBehaviorCandidates { get; init; } = [];

    [JsonPropertyName("next_recommended_action")]
    public string NextRecommendedAction { get; init; } = string.Empty;
}

public sealed record ScalarBehaviorCandidate
{
    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("session_a_candidate_id")]
    public string SessionACandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_candidate_id")]
    public string SessionBCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_stimulus_label")]
    public string SessionAStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_b_stimulus_label")]
    public string SessionBStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_a_changed_sample_count")]
    public int SessionAChangedSampleCount { get; init; }

    [JsonPropertyName("session_b_changed_sample_count")]
    public int SessionBChangedSampleCount { get; init; }

    [JsonPropertyName("session_a_value_delta_magnitude")]
    public double SessionAValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_b_value_delta_magnitude")]
    public double SessionBValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_a_circular_delta_magnitude")]
    public double SessionACircularDeltaMagnitude { get; init; }

    [JsonPropertyName("session_b_circular_delta_magnitude")]
    public double SessionBCircularDeltaMagnitude { get; init; }

    [JsonPropertyName("session_a_signed_circular_delta")]
    public double SessionASignedCircularDelta { get; init; }

    [JsonPropertyName("session_b_signed_circular_delta")]
    public double SessionBSignedCircularDelta { get; init; }

    [JsonPropertyName("session_a_dominant_direction")]
    public string SessionADominantDirection { get; init; } = string.Empty;

    [JsonPropertyName("session_b_dominant_direction")]
    public string SessionBDominantDirection { get; init; } = string.Empty;

    [JsonPropertyName("turn_polarity_relationship")]
    public string TurnPolarityRelationship { get; init; } = string.Empty;

    [JsonPropertyName("camera_turn_separation_relationship")]
    public string CameraTurnSeparationRelationship { get; init; } = string.Empty;

    [JsonPropertyName("value_family")]
    public string ValueFamily { get; init; } = string.Empty;

    [JsonPropertyName("score_total")]
    public double ScoreTotal { get; init; }

    [JsonPropertyName("score_breakdown")]
    public IReadOnlyDictionary<string, double> ScoreBreakdown { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = "weak_candidate";

    [JsonPropertyName("supporting_reasons")]
    public IReadOnlyList<string> SupportingReasons { get; init; } = [];

    [JsonPropertyName("rejection_reasons")]
    public IReadOnlyList<string> RejectionReasons { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_validation_step")]
    public string NextValidationStep { get; init; } = string.Empty;
}
