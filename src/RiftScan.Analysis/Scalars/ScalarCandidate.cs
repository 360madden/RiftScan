using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Scalars;

public sealed record ScalarCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_candidate.v1";

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "scalar_lane";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("absolute_address_hex")]
    public string AbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "float32";

    [JsonPropertyName("sample_count")]
    public int SampleCount { get; init; }

    [JsonPropertyName("distinct_value_count")]
    public int DistinctValueCount { get; init; }

    [JsonPropertyName("changed_sample_count")]
    public int ChangedSampleCount { get; init; }

    [JsonPropertyName("value_delta_magnitude")]
    public double ValueDeltaMagnitude { get; init; }

    [JsonPropertyName("circular_delta_magnitude")]
    public double CircularDeltaMagnitude { get; init; }

    [JsonPropertyName("signed_circular_delta")]
    public double SignedCircularDelta { get; init; }

    [JsonPropertyName("dominant_direction")]
    public string DominantDirection { get; init; } = string.Empty;

    [JsonPropertyName("value_family")]
    public string ValueFamily { get; init; } = string.Empty;

    [JsonPropertyName("direction_consistency_ratio")]
    public double DirectionConsistencyRatio { get; init; }

    [JsonPropertyName("retention_bucket")]
    public string RetentionBucket { get; init; } = string.Empty;

    [JsonPropertyName("stimulus_label")]
    public string StimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("score_breakdown")]
    public IReadOnlyDictionary<string, double> ScoreBreakdown { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("feature_vector")]
    public IReadOnlyDictionary<string, double> FeatureVector { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("value_sequence_summary")]
    public string ValueSequenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "unvalidated_candidate";

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = "low";

    [JsonPropertyName("explanation_short")]
    public string ExplanationShort { get; init; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("value_preview")]
    public IReadOnlyList<string> ValuePreview { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
