using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftWaypointScalarFollowupPlanOptions
{
    public string ScalarMatchPath { get; init; } = string.Empty;

    public int TopPairs { get; init; } = 10;

    public int Samples { get; init; } = 8;

    public int IntervalMilliseconds { get; init; } = 100;

    public int MaxBytesPerRegion { get; init; } = 65536;
}

public sealed record RiftWaypointScalarFollowupPlanResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_waypoint_scalar_followup_plan.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_waypoint_scalar_followup_plan";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("scalar_match_path")]
    public string ScalarMatchPath { get; init; } = string.Empty;

    [JsonPropertyName("source_session_id")]
    public string SourceSessionId { get; init; } = string.Empty;

    [JsonPropertyName("source_session_path")]
    public string SourceSessionPath { get; init; } = string.Empty;

    [JsonPropertyName("top_pairs")]
    public int TopPairs { get; init; }

    [JsonPropertyName("selected_pair_candidate_count")]
    public int SelectedPairCandidateCount { get; init; }

    [JsonPropertyName("base_address_count")]
    public int BaseAddressCount { get; init; }

    [JsonPropertyName("base_addresses")]
    public IReadOnlyList<string> BaseAddresses { get; init; } = [];

    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    [JsonPropertyName("interval_ms")]
    public int IntervalMilliseconds { get; init; }

    [JsonPropertyName("max_regions")]
    public int MaxRegions { get; init; }

    [JsonPropertyName("max_bytes_per_region")]
    public int MaxBytesPerRegion { get; init; }

    [JsonPropertyName("max_total_bytes")]
    public long MaxTotalBytes { get; init; }

    [JsonPropertyName("recommended_capture_args")]
    public IReadOnlyList<string> RecommendedCaptureArgs { get; init; } = [];

    [JsonPropertyName("recommended_capture_command_template")]
    public string RecommendedCaptureCommandTemplate { get; init; } = string.Empty;

    [JsonPropertyName("candidate_summaries")]
    public IReadOnlyList<RiftWaypointScalarFollowupCandidateSummary> CandidateSummaries { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftWaypointScalarFollowupCandidateSummary
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("x_source_base_address_hex")]
    public string XSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("x_source_offset_hex")]
    public string XSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("z_source_base_address_hex")]
    public string ZSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("z_source_offset_hex")]
    public string ZSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("anchor_support_count")]
    public int AnchorSupportCount { get; init; }

    [JsonPropertyName("best_distance_total")]
    public double BestDistanceTotal { get; init; }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = string.Empty;

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; init; } = "change_waypoint_then_capture_listed_base_addresses";
}
