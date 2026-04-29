using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Clusters;

public sealed record StructureCluster
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.structure_cluster.v1";

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "structure_cluster";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("cluster_id")]
    public string ClusterId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("start_offset_hex")]
    public string StartOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("end_offset_hex")]
    public string EndOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("candidate_count")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("span_bytes")]
    public int SpanBytes { get; init; }

    [JsonPropertyName("average_score")]
    public double AverageScore { get; init; }

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
