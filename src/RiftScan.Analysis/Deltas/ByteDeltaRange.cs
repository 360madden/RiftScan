using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Deltas;

public sealed record ByteDeltaRange
{
    [JsonPropertyName("start_offset_hex")]
    public string StartOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("end_offset_hex")]
    public string EndOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("length_bytes")]
    public int LengthBytes { get; init; }
}
