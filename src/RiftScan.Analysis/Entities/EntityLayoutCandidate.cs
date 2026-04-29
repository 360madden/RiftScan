using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Entities;

public sealed record EntityLayoutCandidate
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.entity_layout_candidate.v1";

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "entity_layout";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("start_offset_hex")]
    public string StartOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("end_offset_hex")]
    public string EndOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("layout_kind")]
    public string LayoutKind { get; init; } = string.Empty;

    [JsonPropertyName("stride_bytes")]
    public int StrideBytes { get; init; }

    [JsonPropertyName("cluster_count")]
    public int ClusterCount { get; init; }

    [JsonPropertyName("vec3_candidate_count")]
    public int Vec3CandidateCount { get; init; }

    [JsonPropertyName("scalar_candidate_count")]
    public int ScalarCandidateCount { get; init; }

    [JsonPropertyName("score_total")]
    public double ScoreTotal { get; init; }

    [JsonPropertyName("score_breakdown")]
    public IReadOnlyDictionary<string, double> ScoreBreakdown { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("feature_vector")]
    public IReadOnlyDictionary<string, double> FeatureVector { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "unvalidated_candidate";

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = "low";

    [JsonPropertyName("explanation_short")]
    public string ExplanationShort { get; init; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
