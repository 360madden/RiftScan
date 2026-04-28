using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record SessionComparisonResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_a_path")]
    public string SessionAPath { get; init; } = string.Empty;

    [JsonPropertyName("session_b_path")]
    public string SessionBPath { get; init; } = string.Empty;

    [JsonPropertyName("comparison_path")]
    public string? ComparisonPath { get; init; }

    [JsonPropertyName("session_a_id")]
    public string SessionAId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_id")]
    public string SessionBId { get; init; } = string.Empty;

    [JsonPropertyName("same_process_name")]
    public bool SameProcessName { get; init; }

    [JsonPropertyName("matching_region_count")]
    public int MatchingRegionCount { get; init; }

    [JsonPropertyName("matching_cluster_count")]
    public int MatchingClusterCount { get; init; }

    [JsonPropertyName("region_matches")]
    public IReadOnlyList<RegionComparison> RegionMatches { get; init; } = [];

    [JsonPropertyName("cluster_matches")]
    public IReadOnlyList<ClusterComparison> ClusterMatches { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
