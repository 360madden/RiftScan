using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3CandidateComparison
{
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

    [JsonPropertyName("session_a_region_id")]
    public string SessionARegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_region_id")]
    public string SessionBRegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_rank_score")]
    public double SessionARankScore { get; init; }

    [JsonPropertyName("session_b_rank_score")]
    public double SessionBRankScore { get; init; }

    [JsonPropertyName("score_delta")]
    public double ScoreDelta { get; init; }

    [JsonPropertyName("session_a_snapshot_support")]
    public int SessionASnapshotSupport { get; init; }

    [JsonPropertyName("session_b_snapshot_support")]
    public int SessionBSnapshotSupport { get; init; }

    [JsonPropertyName("session_a_stimulus_label")]
    public string SessionAStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_b_stimulus_label")]
    public string SessionBStimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("session_a_behavior_score")]
    public double SessionABehaviorScore { get; init; }

    [JsonPropertyName("session_b_behavior_score")]
    public double SessionBBehaviorScore { get; init; }

    [JsonPropertyName("behavior_score_delta")]
    public double BehaviorScoreDelta { get; init; }

    [JsonPropertyName("session_a_value_delta_magnitude")]
    public double SessionAValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_b_value_delta_magnitude")]
    public double SessionBValueDeltaMagnitude { get; init; }

    [JsonPropertyName("session_a_validation_status")]
    public string SessionAValidationStatus { get; init; } = string.Empty;

    [JsonPropertyName("session_b_validation_status")]
    public string SessionBValidationStatus { get; init; } = string.Empty;

    [JsonPropertyName("session_a_value_sequence_summary")]
    public string SessionAValueSequenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("session_b_value_sequence_summary")]
    public string SessionBValueSequenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("session_a_analyzer_sources")]
    public IReadOnlyList<string> SessionAAnalyzerSources { get; init; } = [];

    [JsonPropertyName("session_b_analyzer_sources")]
    public IReadOnlyList<string> SessionBAnalyzerSources { get; init; } = [];

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}
