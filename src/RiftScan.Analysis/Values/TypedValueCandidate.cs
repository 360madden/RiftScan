using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Values;

public sealed record TypedValueCandidate
{
    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "typed_value_lane";

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
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("sample_count")]
    public int SampleCount { get; init; }

    [JsonPropertyName("distinct_value_count")]
    public int DistinctValueCount { get; init; }

    [JsonPropertyName("changed_sample_count")]
    public int ChangedSampleCount { get; init; }

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("score_breakdown")]
    public IReadOnlyDictionary<string, double> ScoreBreakdown { get; init; } = new Dictionary<string, double>();

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
