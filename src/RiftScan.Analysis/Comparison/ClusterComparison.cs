using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ClusterComparison
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("start_offset_hex")]
    public string StartOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("end_offset_hex")]
    public string EndOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("session_a_cluster_id")]
    public string SessionAClusterId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_cluster_id")]
    public string SessionBClusterId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_region_id")]
    public string SessionARegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_region_id")]
    public string SessionBRegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_rank_score")]
    public double SessionARankScore { get; init; }

    [JsonPropertyName("session_b_rank_score")]
    public double SessionBRankScore { get; init; }

    [JsonPropertyName("candidate_count_delta")]
    public int CandidateCountDelta { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}
