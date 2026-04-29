using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3BehaviorSummary
{
    [JsonPropertyName("matching_vec3_candidate_count")]
    public int MatchingVec3CandidateCount { get; init; }

    [JsonPropertyName("behavior_contrast_count")]
    public int BehaviorContrastCount { get; init; }

    [JsonPropertyName("behavior_consistent_match_count")]
    public int BehaviorConsistentMatchCount { get; init; }

    [JsonPropertyName("unlabeled_match_count")]
    public int UnlabeledMatchCount { get; init; }

    [JsonPropertyName("stimulus_labels")]
    public IReadOnlyList<string> StimulusLabels { get; init; } = [];

    [JsonPropertyName("behavior_contrast_candidates")]
    public IReadOnlyList<Vec3BehaviorContrastCandidate> BehaviorContrastCandidates { get; init; } = [];

    [JsonPropertyName("next_recommended_action")]
    public string NextRecommendedAction { get; init; } = string.Empty;
}

public sealed record Vec3BehaviorContrastCandidate
{
    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "vec3_float32";

    [JsonPropertyName("session_a_candidate_id")]
    public string SessionACandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_candidate_id")]
    public string SessionBCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_stimulus_label")]
    public string SessionAStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_b_stimulus_label")]
    public string SessionBStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_a_value_delta_magnitude")]
    public double SessionAValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_b_value_delta_magnitude")]
    public double SessionBValueDeltaMagnitude { get; init; }

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
