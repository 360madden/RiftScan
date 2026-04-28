using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record RegionMap
{
    [JsonPropertyName("regions")]
    public IReadOnlyList<MemoryRegion> Regions { get; init; } = [];
}

public sealed record MemoryRegion
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("protection")]
    public string Protection { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
