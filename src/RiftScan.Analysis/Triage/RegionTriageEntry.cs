using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Triage;

public sealed record RegionTriageEntry
{
    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "dynamic_region_triage";

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

    [JsonPropertyName("unique_checksum_count")]
    public int UniqueChecksumCount { get; init; }

    [JsonPropertyName("total_bytes")]
    public long TotalBytes { get; init; }

    [JsonPropertyName("byte_entropy")]
    public double ByteEntropy { get; init; }

    [JsonPropertyName("zero_byte_ratio")]
    public double ZeroByteRatio { get; init; }

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
