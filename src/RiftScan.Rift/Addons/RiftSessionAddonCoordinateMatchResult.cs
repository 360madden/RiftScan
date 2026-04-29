using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftSessionAddonCoordinateMatchOptions
{
    public string SessionPath { get; init; } = string.Empty;

    public string ObservationPath { get; init; } = string.Empty;

    public IReadOnlyList<ulong> RegionBaseAddresses { get; init; } = [];

    public double Tolerance { get; init; } = 5;

    public int Top { get; init; } = 100;

    public bool LatestOnly { get; init; }
}

public sealed record RiftSessionAddonCoordinateMatchResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_session_addon_coordinate_match_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("observation_path")]
    public string ObservationPath { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_session_addon_coordinate_match";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("tolerance")]
    public double Tolerance { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("latest_only")]
    public bool LatestOnly { get; init; }

    [JsonPropertyName("region_base_filters")]
    public IReadOnlyList<string> RegionBaseFilters { get; init; } = [];

    [JsonPropertyName("latest_observation_utc")]
    public DateTimeOffset? LatestObservationUtc { get; init; }

    [JsonPropertyName("observation_count")]
    public int ObservationCount { get; init; }

    [JsonPropertyName("observations_used")]
    public int ObservationsUsed { get; init; }

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
    public IReadOnlyList<RiftSessionAddonCoordinateCandidate> Candidates { get; init; } = [];

    [JsonPropertyName("matches")]
    public IReadOnlyList<RiftSessionAddonCoordinateMatch> Matches { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftSessionAddonCoordinateCandidate
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_region_id")]
    public string SourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("axis_order")]
    public string AxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("observation_support_count")]
    public int ObservationSupportCount { get; init; }

    [JsonPropertyName("best_max_abs_distance")]
    public double BestMaxAbsDistance { get; init; }

    [JsonPropertyName("best_memory_x")]
    public double BestMemoryX { get; init; }

    [JsonPropertyName("best_memory_y")]
    public double BestMemoryY { get; init; }

    [JsonPropertyName("best_memory_z")]
    public double BestMemoryZ { get; init; }

    [JsonPropertyName("best_addon_x")]
    public double BestAddonX { get; init; }

    [JsonPropertyName("best_addon_y")]
    public double BestAddonY { get; init; }

    [JsonPropertyName("best_addon_z")]
    public double BestAddonZ { get; init; }

    [JsonPropertyName("supporting_snapshot_ids")]
    public IReadOnlyList<string> SupportingSnapshotIds { get; init; } = [];

    [JsonPropertyName("supporting_observation_ids")]
    public IReadOnlyList<string> SupportingObservationIds { get; init; } = [];

    [JsonPropertyName("addon_sources")]
    public IReadOnlyList<string> AddonSources { get; init; } = [];

    [JsonPropertyName("zone_ids")]
    public IReadOnlyList<string> ZoneIds { get; init; } = [];

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "candidate_unverified";

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record RiftSessionAddonCoordinateMatch
{
    [JsonPropertyName("match_id")]
    public string MatchId { get; init; } = string.Empty;

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("source_region_id")]
    public string SourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("axis_order")]
    public string AxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("memory_x")]
    public double MemoryX { get; init; }

    [JsonPropertyName("memory_y")]
    public double MemoryY { get; init; }

    [JsonPropertyName("memory_z")]
    public double MemoryZ { get; init; }

    [JsonPropertyName("observation_id")]
    public string ObservationId { get; init; } = string.Empty;

    [JsonPropertyName("addon_name")]
    public string AddonName { get; init; } = string.Empty;

    [JsonPropertyName("source_pattern")]
    public string SourcePattern { get; init; } = string.Empty;

    [JsonPropertyName("addon_observed_x")]
    public double AddonObservedX { get; init; }

    [JsonPropertyName("addon_observed_y")]
    public double AddonObservedY { get; init; }

    [JsonPropertyName("addon_observed_z")]
    public double AddonObservedZ { get; init; }

    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("max_abs_distance")]
    public double MaxAbsDistance { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
