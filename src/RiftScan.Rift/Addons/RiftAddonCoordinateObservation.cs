using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftAddonCoordinateObservation
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_addon_coordinate_observation.v1";

    [JsonPropertyName("observation_id")]
    public string ObservationId { get; init; } = string.Empty;

    [JsonPropertyName("source_file_name")]
    public string SourceFileName { get; init; } = string.Empty;

    [JsonPropertyName("source_path_redacted")]
    public string SourcePathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("addon_name")]
    public string AddonName { get; init; } = string.Empty;

    [JsonPropertyName("source_pattern")]
    public string SourcePattern { get; init; } = string.Empty;

    [JsonPropertyName("line_number")]
    public int LineNumber { get; init; }

    [JsonPropertyName("file_last_write_utc")]
    public DateTimeOffset FileLastWriteUtc { get; init; }

    [JsonPropertyName("coord_x")]
    public double CoordX { get; init; }

    [JsonPropertyName("coord_y")]
    public double CoordY { get; init; }

    [JsonPropertyName("coord_z")]
    public double CoordZ { get; init; }

    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
