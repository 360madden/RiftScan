using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Vectors;

public sealed record Vec3Candidate
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

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

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "vec3_float32";

    [JsonPropertyName("source_structure_kind")]
    public string SourceStructureKind { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_support")]
    public int SnapshotSupport { get; init; }

    [JsonPropertyName("stimulus_label")]
    public string StimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("sample_value_count")]
    public int SampleValueCount { get; init; }

    [JsonPropertyName("value_delta_magnitude")]
    public double ValueDeltaMagnitude { get; init; }

    [JsonPropertyName("behavior_score")]
    public double BehaviorScore { get; init; }

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "unvalidated_candidate";

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("value_preview")]
    public IReadOnlyList<float> ValuePreview { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
