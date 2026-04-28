using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SnapshotIndexEntry
{
    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("checksum_sha256_hex")]
    public string ChecksumSha256Hex { get; init; } = string.Empty;
}
