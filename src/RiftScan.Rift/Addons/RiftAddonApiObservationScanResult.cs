using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftAddonApiObservationScanOptions
{
    public string Path { get; init; } = string.Empty;

    public int MaxFiles { get; init; } = 5000;

    public string? JsonlOutputPath { get; init; }

    public IReadOnlyList<string> IncludeAddonNames { get; init; } = [];

    public DateTimeOffset? MinFileLastWriteUtc { get; init; }
}

public sealed record RiftAddonApiObservationScanResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_addon_api_observation_scan_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    [JsonPropertyName("root_path_redacted")]
    public string RootPathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("jsonl_output_path")]
    public string? JsonlOutputPath { get; init; }

    [JsonPropertyName("files_scanned")]
    public int FilesScanned { get; init; }

    [JsonPropertyName("observation_count")]
    public int ObservationCount { get; init; }

    [JsonPropertyName("observations")]
    public IReadOnlyList<RiftAddonApiObservation> Observations { get; init; } = [];

    [JsonPropertyName("waypoint_anchor_count")]
    public int WaypointAnchorCount { get; init; }

    [JsonPropertyName("waypoint_anchors")]
    public IReadOnlyList<RiftAddonWaypointAnchor> WaypointAnchors { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record RiftAddonWaypointAnchor
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_addon_waypoint_anchor.v1";

    [JsonPropertyName("anchor_id")]
    public string AnchorId { get; init; } = string.Empty;

    [JsonPropertyName("source_addon")]
    public string SourceAddon { get; init; } = string.Empty;

    [JsonPropertyName("source_file_name")]
    public string SourceFileName { get; init; } = string.Empty;

    [JsonPropertyName("source_path_redacted")]
    public string SourcePathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("file_last_write_utc")]
    public DateTimeOffset FileLastWriteUtc { get; init; }

    [JsonPropertyName("realtime")]
    public double? Realtime { get; init; }

    [JsonPropertyName("player_observation_id")]
    public string PlayerObservationId { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_observation_id")]
    public string WaypointObservationId { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_status_observation_id")]
    public string WaypointStatusObservationId { get; init; } = string.Empty;

    [JsonPropertyName("player_x")]
    public double PlayerX { get; init; }

    [JsonPropertyName("player_y")]
    public double? PlayerY { get; init; }

    [JsonPropertyName("player_z")]
    public double PlayerZ { get; init; }

    [JsonPropertyName("waypoint_x")]
    public double WaypointX { get; init; }

    [JsonPropertyName("waypoint_z")]
    public double WaypointZ { get; init; }

    [JsonPropertyName("delta_x")]
    public double DeltaX { get; init; }

    [JsonPropertyName("delta_z")]
    public double DeltaZ { get; init; }

    [JsonPropertyName("horizontal_distance")]
    public double HorizontalDistance { get; init; }

    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("location_name")]
    public string LocationName { get; init; } = string.Empty;

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record RiftAddonApiObservation
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_addon_api_observation.v1";

    [JsonPropertyName("observation_id")]
    public string ObservationId { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("source_addon")]
    public string SourceAddon { get; init; } = string.Empty;

    [JsonPropertyName("source_file_name")]
    public string SourceFileName { get; init; } = string.Empty;

    [JsonPropertyName("source_path_redacted")]
    public string SourcePathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("source_pattern")]
    public string SourcePattern { get; init; } = string.Empty;

    [JsonPropertyName("line_number")]
    public int LineNumber { get; init; }

    [JsonPropertyName("file_last_write_utc")]
    public DateTimeOffset FileLastWriteUtc { get; init; }

    [JsonPropertyName("realtime")]
    public double? Realtime { get; init; }

    [JsonPropertyName("api_source")]
    public string ApiSource { get; init; } = string.Empty;

    [JsonPropertyName("source_mode")]
    public string SourceMode { get; init; } = string.Empty;

    [JsonPropertyName("unit_id")]
    public string UnitId { get; init; } = string.Empty;

    [JsonPropertyName("unit_name")]
    public string UnitName { get; init; } = string.Empty;

    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("location_name")]
    public string LocationName { get; init; } = string.Empty;

    [JsonPropertyName("coordinate_space")]
    public string CoordinateSpace { get; init; } = string.Empty;

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = string.Empty;

    [JsonPropertyName("coord_x")]
    public double? CoordX { get; init; }

    [JsonPropertyName("coord_y")]
    public double? CoordY { get; init; }

    [JsonPropertyName("coord_z")]
    public double? CoordZ { get; init; }

    [JsonPropertyName("waypoint_x")]
    public double? WaypointX { get; init; }

    [JsonPropertyName("waypoint_z")]
    public double? WaypointZ { get; init; }

    [JsonPropertyName("waypoint_api_available")]
    public bool? WaypointApiAvailable { get; init; }

    [JsonPropertyName("waypoint_set_api_available")]
    public bool? WaypointSetApiAvailable { get; init; }

    [JsonPropertyName("waypoint_clear_api_available")]
    public bool? WaypointClearApiAvailable { get; init; }

    [JsonPropertyName("waypoint_has_waypoint")]
    public bool? WaypointHasWaypoint { get; init; }

    [JsonPropertyName("waypoint_update_count")]
    public int? WaypointUpdateCount { get; init; }

    [JsonPropertyName("waypoint_last_update_at")]
    public double? WaypointLastUpdateAt { get; init; }

    [JsonPropertyName("waypoint_last_command")]
    public string WaypointLastCommand { get; init; } = string.Empty;

    [JsonPropertyName("loc_x")]
    public double? LocX { get; init; }

    [JsonPropertyName("loc_y")]
    public double? LocY { get; init; }

    [JsonPropertyName("loc_z")]
    public double? LocZ { get; init; }

    [JsonPropertyName("raw_text")]
    public string RawText { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
