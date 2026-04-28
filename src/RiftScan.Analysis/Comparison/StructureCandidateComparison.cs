using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record StructureCandidateComparison
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("structure_kind")]
    public string StructureKind { get; init; } = string.Empty;

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

    [JsonPropertyName("session_a_snapshot_support")]
    public int SessionASnapshotSupport { get; init; }

    [JsonPropertyName("session_b_snapshot_support")]
    public int SessionBSnapshotSupport { get; init; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}
