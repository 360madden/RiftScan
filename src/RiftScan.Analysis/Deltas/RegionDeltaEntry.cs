using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Deltas;

public sealed record RegionDeltaEntry
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.region_delta_entry.v1";

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "byte_delta";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("compared_pair_count")]
    public int ComparedPairCount { get; init; }

    [JsonPropertyName("changed_byte_count")]
    public int ChangedByteCount { get; init; }

    [JsonPropertyName("changed_byte_ratio")]
    public double ChangedByteRatio { get; init; }

    [JsonPropertyName("pair_change_ratio")]
    public double PairChangeRatio { get; init; }

    [JsonPropertyName("changed_range_count")]
    public int ChangedRangeCount { get; init; }

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("changed_ranges")]
    public IReadOnlyList<ByteDeltaRange> ChangedRanges { get; init; } = [];
}
