using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record RegionComparison
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

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

    [JsonPropertyName("session_a_unique_hashes")]
    public int SessionAUniqueHashes { get; init; }

    [JsonPropertyName("session_b_unique_hashes")]
    public int SessionBUniqueHashes { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}
