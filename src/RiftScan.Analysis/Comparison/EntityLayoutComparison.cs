using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record EntityLayoutComparison
{
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

    [JsonPropertyName("session_a_candidate_id")]
    public string SessionACandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_candidate_id")]
    public string SessionBCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_region_id")]
    public string SessionARegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_region_id")]
    public string SessionBRegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_score")]
    public double SessionAScore { get; init; }

    [JsonPropertyName("session_b_score")]
    public double SessionBScore { get; init; }

    [JsonPropertyName("score_delta")]
    public double ScoreDelta { get; init; }

    [JsonPropertyName("session_a_cluster_count")]
    public int SessionAClusterCount { get; init; }

    [JsonPropertyName("session_b_cluster_count")]
    public int SessionBClusterCount { get; init; }

    [JsonPropertyName("session_a_vec3_candidate_count")]
    public int SessionAVec3CandidateCount { get; init; }

    [JsonPropertyName("session_b_vec3_candidate_count")]
    public int SessionBVec3CandidateCount { get; init; }

    [JsonPropertyName("overlap_bytes")]
    public int OverlapBytes { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}
