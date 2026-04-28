using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record ChecksumManifest
{
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; init; } = string.Empty;

    [JsonPropertyName("entries")]
    public IReadOnlyList<ChecksumEntry> Entries { get; init; } = [];
}

public sealed record ChecksumEntry
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("sha256_hex")]
    public string Sha256Hex { get; init; } = string.Empty;

    [JsonPropertyName("bytes")]
    public long Bytes { get; init; }
}
