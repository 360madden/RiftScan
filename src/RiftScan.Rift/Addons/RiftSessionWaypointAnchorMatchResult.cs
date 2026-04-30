using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftSessionWaypointAnchorMatchOptions
{
    public string SessionPath { get; init; } = string.Empty;

    public string AnchorPath { get; init; } = string.Empty;

    public IReadOnlyList<ulong> RegionBaseAddresses { get; init; } = [];

    public double Tolerance { get; init; } = 5;

    public int Top { get; init; } = 100;
}

public sealed record RiftSessionWaypointAnchorMatchResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_session_waypoint_anchor_match_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_path")]
    public string AnchorPath { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_session_waypoint_anchor_match";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("tolerance")]
    public double Tolerance { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("region_base_filters")]
    public IReadOnlyList<string> RegionBaseFilters { get; init; } = [];

    [JsonPropertyName("anchor_count")]
    public int AnchorCount { get; init; }

    [JsonPropertyName("anchors_used")]
    public int AnchorsUsed { get; init; }

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("regions_scanned")]
    public int RegionsScanned { get; init; }

    [JsonPropertyName("bytes_scanned")]
    public long BytesScanned { get; init; }

    [JsonPropertyName("match_count")]
    public int MatchCount { get; init; }

    [JsonPropertyName("candidate_count")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<RiftSessionWaypointAnchorCandidate> Candidates { get; init; } = [];

    [JsonPropertyName("matches")]
    public IReadOnlyList<RiftSessionWaypointAnchorMatch> Matches { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftSessionWaypointAnchorCandidate
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("player_source_region_id")]
    public string PlayerSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("player_source_base_address_hex")]
    public string PlayerSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("player_source_offset_hex")]
    public string PlayerSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("player_source_absolute_address_hex")]
    public string PlayerSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("player_axis_order")]
    public string PlayerAxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_region_id")]
    public string WaypointSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_base_address_hex")]
    public string WaypointSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_offset_hex")]
    public string WaypointSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_absolute_address_hex")]
    public string WaypointSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_axis_order")]
    public string WaypointAxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("anchor_support_count")]
    public int AnchorSupportCount { get; init; }

    [JsonPropertyName("best_player_max_abs_distance")]
    public double BestPlayerMaxAbsDistance { get; init; }

    [JsonPropertyName("best_waypoint_max_abs_distance")]
    public double BestWaypointMaxAbsDistance { get; init; }

    [JsonPropertyName("best_delta_max_abs_distance")]
    public double BestDeltaMaxAbsDistance { get; init; }

    [JsonPropertyName("best_memory_delta_x")]
    public double BestMemoryDeltaX { get; init; }

    [JsonPropertyName("best_memory_delta_z")]
    public double BestMemoryDeltaZ { get; init; }

    [JsonPropertyName("best_anchor_delta_x")]
    public double BestAnchorDeltaX { get; init; }

    [JsonPropertyName("best_anchor_delta_z")]
    public double BestAnchorDeltaZ { get; init; }

    [JsonPropertyName("supporting_snapshot_ids")]
    public IReadOnlyList<string> SupportingSnapshotIds { get; init; } = [];

    [JsonPropertyName("supporting_anchor_ids")]
    public IReadOnlyList<string> SupportingAnchorIds { get; init; } = [];

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "candidate_unverified";

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record RiftSessionWaypointAnchorMatch
{
    [JsonPropertyName("match_id")]
    public string MatchId { get; init; } = string.Empty;

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_id")]
    public string AnchorId { get; init; } = string.Empty;

    [JsonPropertyName("player_source_region_id")]
    public string PlayerSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("player_source_base_address_hex")]
    public string PlayerSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("player_source_offset_hex")]
    public string PlayerSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("player_source_absolute_address_hex")]
    public string PlayerSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("player_axis_order")]
    public string PlayerAxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_region_id")]
    public string WaypointSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_base_address_hex")]
    public string WaypointSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_offset_hex")]
    public string WaypointSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_source_absolute_address_hex")]
    public string WaypointSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("waypoint_axis_order")]
    public string WaypointAxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("memory_player_x")]
    public double MemoryPlayerX { get; init; }

    [JsonPropertyName("memory_player_y")]
    public double MemoryPlayerY { get; init; }

    [JsonPropertyName("memory_player_z")]
    public double MemoryPlayerZ { get; init; }

    [JsonPropertyName("memory_waypoint_x")]
    public double MemoryWaypointX { get; init; }

    [JsonPropertyName("memory_waypoint_y")]
    public double MemoryWaypointY { get; init; }

    [JsonPropertyName("memory_waypoint_z")]
    public double MemoryWaypointZ { get; init; }

    [JsonPropertyName("anchor_player_x")]
    public double AnchorPlayerX { get; init; }

    [JsonPropertyName("anchor_player_y")]
    public double? AnchorPlayerY { get; init; }

    [JsonPropertyName("anchor_player_z")]
    public double AnchorPlayerZ { get; init; }

    [JsonPropertyName("anchor_waypoint_x")]
    public double AnchorWaypointX { get; init; }

    [JsonPropertyName("anchor_waypoint_z")]
    public double AnchorWaypointZ { get; init; }

    [JsonPropertyName("memory_delta_x")]
    public double MemoryDeltaX { get; init; }

    [JsonPropertyName("memory_delta_z")]
    public double MemoryDeltaZ { get; init; }

    [JsonPropertyName("anchor_delta_x")]
    public double AnchorDeltaX { get; init; }

    [JsonPropertyName("anchor_delta_z")]
    public double AnchorDeltaZ { get; init; }

    [JsonPropertyName("player_max_abs_distance")]
    public double PlayerMaxAbsDistance { get; init; }

    [JsonPropertyName("waypoint_max_abs_distance")]
    public double WaypointMaxAbsDistance { get; init; }

    [JsonPropertyName("delta_max_abs_distance")]
    public double DeltaMaxAbsDistance { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
