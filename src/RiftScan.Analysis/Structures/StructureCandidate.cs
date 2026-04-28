using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Structures;

public sealed record StructureCandidate
{
    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "finite_float_triplet_structure";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("absolute_address_hex")]
    public string AbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("structure_kind")]
    public string StructureKind { get; init; } = "float32_triplet";

    [JsonPropertyName("snapshot_support")]
    public int SnapshotSupport { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("value_preview")]
    public IReadOnlyList<float> ValuePreview { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
