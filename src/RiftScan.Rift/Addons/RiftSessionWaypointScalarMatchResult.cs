using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftSessionWaypointScalarMatchOptions
{
    public string SessionPath { get; init; } = string.Empty;

    public string AnchorPath { get; init; } = string.Empty;

    public IReadOnlyList<ulong> RegionBaseAddresses { get; init; } = [];

    public double Tolerance { get; init; } = 5;

    public int Top { get; init; } = 100;

    public int MaxScalarHitsPerSnapshotAxis { get; init; } = 64;

    public string? ScalarHitsOutputPath { get; init; }
}

public sealed record RiftSessionWaypointScalarMatchResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_session_waypoint_scalar_match_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_path")]
    public string AnchorPath { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_session_waypoint_scalar_match";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("tolerance")]
    public double Tolerance { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("max_scalar_hits_per_snapshot_axis")]
    public int MaxScalarHitsPerSnapshotAxis { get; init; }

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

    [JsonPropertyName("scalar_hit_count")]
    public int ScalarHitCount { get; init; }

    [JsonPropertyName("scalar_axis_hit_counts")]
    public IReadOnlyDictionary<string, int> ScalarAxisHitCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("retained_scalar_hit_count")]
    public int RetainedScalarHitCount { get; init; }

    [JsonPropertyName("retained_scalar_axis_hit_counts")]
    public IReadOnlyDictionary<string, int> RetainedScalarAxisHitCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("pair_candidate_count")]
    public int PairCandidateCount { get; init; }

    [JsonPropertyName("scalar_hits_output_path")]
    public string? ScalarHitsOutputPath { get; init; }

    [JsonPropertyName("scalar_hits")]
    public IReadOnlyList<RiftSessionWaypointScalarHit> ScalarHits { get; init; } = [];

    [JsonPropertyName("pair_candidates")]
    public IReadOnlyList<RiftSessionWaypointScalarPairCandidate> PairCandidates { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftSessionWaypointScalarHit
{
    [JsonPropertyName("hit_id")]
    public string HitId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_id")]
    public string AnchorId { get; init; } = string.Empty;

    [JsonPropertyName("axis")]
    public string Axis { get; init; } = string.Empty;

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

    [JsonPropertyName("memory_value")]
    public double MemoryValue { get; init; }

    [JsonPropertyName("anchor_value")]
    public double AnchorValue { get; init; }

    [JsonPropertyName("abs_distance")]
    public double AbsDistance { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record RiftSessionWaypointScalarPairCandidate
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_id")]
    public string AnchorId { get; init; } = string.Empty;

    [JsonPropertyName("x_source_region_id")]
    public string XSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("x_source_base_address_hex")]
    public string XSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("x_source_offset_hex")]
    public string XSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("x_source_absolute_address_hex")]
    public string XSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("z_source_region_id")]
    public string ZSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("z_source_base_address_hex")]
    public string ZSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("z_source_offset_hex")]
    public string ZSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("z_source_absolute_address_hex")]
    public string ZSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("anchor_support_count")]
    public int AnchorSupportCount { get; init; }

    [JsonPropertyName("best_x_abs_distance")]
    public double BestXAbsDistance { get; init; }

    [JsonPropertyName("best_z_abs_distance")]
    public double BestZAbsDistance { get; init; }

    [JsonPropertyName("best_distance_total")]
    public double BestDistanceTotal { get; init; }

    [JsonPropertyName("best_memory_waypoint_x")]
    public double BestMemoryWaypointX { get; init; }

    [JsonPropertyName("best_memory_waypoint_z")]
    public double BestMemoryWaypointZ { get; init; }

    [JsonPropertyName("anchor_waypoint_x")]
    public double AnchorWaypointX { get; init; }

    [JsonPropertyName("anchor_waypoint_z")]
    public double AnchorWaypointZ { get; init; }

    [JsonPropertyName("supporting_snapshot_ids")]
    public IReadOnlyList<string> SupportingSnapshotIds { get; init; } = [];

    [JsonPropertyName("supporting_anchor_ids")]
    public IReadOnlyList<string> SupportingAnchorIds { get; init; } = [];

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "candidate_unverified";

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
