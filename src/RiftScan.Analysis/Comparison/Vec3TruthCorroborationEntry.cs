using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3TruthCorroborationEntry
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.vec3_truth_corroboration.v1";

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = "vec3_float32";

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("corroboration_status")]
    public string CorroborationStatus { get; init; } = "uncorroborated";

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("addon_source_type")]
    public string AddonSourceType { get; init; } = "addon_waypoint_or_player_coord_truth";

    [JsonPropertyName("addon_observed_x")]
    public double? AddonObservedX { get; init; }

    [JsonPropertyName("addon_observed_y")]
    public double? AddonObservedY { get; init; }

    [JsonPropertyName("addon_observed_z")]
    public double? AddonObservedZ { get; init; }

    [JsonPropertyName("axis_order")]
    public string AxisOrder { get; init; } = "x_y_z";

    [JsonPropertyName("tolerance")]
    public double? Tolerance { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
