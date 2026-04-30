using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftAddonApiTruthSummaryOptions
{
    public string ScanPath { get; init; } = string.Empty;
}

public sealed record RiftAddonApiTruthSummaryResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_addon_api_truth_summary.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_addon_api_truth_summary";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("scan_path")]
    public string ScanPath { get; init; } = string.Empty;

    [JsonPropertyName("scan_root_path_redacted")]
    public string ScanRootPathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("observation_count")]
    public int ObservationCount { get; init; }

    [JsonPropertyName("waypoint_anchor_count")]
    public int WaypointAnchorCount { get; init; }

    [JsonPropertyName("observation_kind_counts")]
    public IReadOnlyDictionary<string, int> ObservationKindCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("truth_record_count")]
    public int TruthRecordCount { get; init; }

    [JsonPropertyName("latest_player")]
    public RiftAddonApiTruthRecord? LatestPlayer { get; init; }

    [JsonPropertyName("latest_target")]
    public RiftAddonApiTruthRecord? LatestTarget { get; init; }

    [JsonPropertyName("latest_focus")]
    public RiftAddonApiTruthRecord? LatestFocus { get; init; }

    [JsonPropertyName("latest_focus_target")]
    public RiftAddonApiTruthRecord? LatestFocusTarget { get; init; }

    [JsonPropertyName("latest_player_loc")]
    public RiftAddonApiTruthRecord? LatestPlayerLoc { get; init; }

    [JsonPropertyName("latest_waypoint")]
    public RiftAddonApiTruthRecord? LatestWaypoint { get; init; }

    [JsonPropertyName("latest_waypoint_status")]
    public RiftAddonApiTruthRecord? LatestWaypointStatus { get; init; }

    [JsonPropertyName("latest_player_waypoint_anchor")]
    public RiftAddonApiTruthRecord? LatestPlayerWaypointAnchor { get; init; }

    [JsonPropertyName("truth_records")]
    public IReadOnlyList<RiftAddonApiTruthRecord> TruthRecords { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftAddonApiTruthRecord
{
    [JsonPropertyName("truth_id")]
    public string TruthId { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("source_observation_id")]
    public string SourceObservationId { get; init; } = string.Empty;

    [JsonPropertyName("source_anchor_id")]
    public string SourceAnchorId { get; init; } = string.Empty;

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

    [JsonPropertyName("api_source")]
    public string ApiSource { get; init; } = string.Empty;

    [JsonPropertyName("source_mode")]
    public string SourceMode { get; init; } = string.Empty;

    [JsonPropertyName("coordinate_space")]
    public string CoordinateSpace { get; init; } = string.Empty;

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = string.Empty;

    [JsonPropertyName("unit_id")]
    public string UnitId { get; init; } = string.Empty;

    [JsonPropertyName("unit_name")]
    public string UnitName { get; init; } = string.Empty;

    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("location_name")]
    public string LocationName { get; init; } = string.Empty;

    [JsonPropertyName("coordinate_x")]
    public double? CoordinateX { get; init; }

    [JsonPropertyName("coordinate_y")]
    public double? CoordinateY { get; init; }

    [JsonPropertyName("coordinate_z")]
    public double? CoordinateZ { get; init; }

    [JsonPropertyName("player_x")]
    public double? PlayerX { get; init; }

    [JsonPropertyName("player_y")]
    public double? PlayerY { get; init; }

    [JsonPropertyName("player_z")]
    public double? PlayerZ { get; init; }

    [JsonPropertyName("target_x")]
    public double? TargetX { get; init; }

    [JsonPropertyName("target_y")]
    public double? TargetY { get; init; }

    [JsonPropertyName("target_z")]
    public double? TargetZ { get; init; }

    [JsonPropertyName("waypoint_x")]
    public double? WaypointX { get; init; }

    [JsonPropertyName("waypoint_z")]
    public double? WaypointZ { get; init; }

    [JsonPropertyName("loc_x")]
    public double? LocX { get; init; }

    [JsonPropertyName("loc_y")]
    public double? LocY { get; init; }

    [JsonPropertyName("loc_z")]
    public double? LocZ { get; init; }

    [JsonPropertyName("delta_x")]
    public double? DeltaX { get; init; }

    [JsonPropertyName("delta_z")]
    public double? DeltaZ { get; init; }

    [JsonPropertyName("horizontal_distance")]
    public double? HorizontalDistance { get; init; }

    [JsonPropertyName("waypoint_has_waypoint")]
    public bool? WaypointHasWaypoint { get; init; }

    [JsonPropertyName("waypoint_update_count")]
    public int? WaypointUpdateCount { get; init; }

    [JsonPropertyName("waypoint_last_update_at")]
    public double? WaypointLastUpdateAt { get; init; }

    [JsonPropertyName("waypoint_last_command")]
    public string WaypointLastCommand { get; init; } = string.Empty;

    [JsonPropertyName("raw_text")]
    public string RawText { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
