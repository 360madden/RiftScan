using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftWaypointScalarComparisonOptions
{
    public IReadOnlyList<string> InputPaths { get; init; } = [];

    public double DeltaTolerance { get; init; } = 5;

    public int Top { get; init; } = 100;
}

public sealed record RiftWaypointScalarComparisonResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_waypoint_scalar_comparison_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_waypoint_scalar_comparison";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("input_paths")]
    public IReadOnlyList<string> InputPaths { get; init; } = [];

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("delta_tolerance")]
    public double DeltaTolerance { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("input_count")]
    public int InputCount { get; init; }

    [JsonPropertyName("input_summaries")]
    public IReadOnlyList<RiftWaypointScalarComparisonInputSummary> InputSummaries { get; init; } = [];

    [JsonPropertyName("comparison_count")]
    public int ComparisonCount { get; init; }

    [JsonPropertyName("classification_counts")]
    public IReadOnlyDictionary<string, int> ClassificationCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("comparisons")]
    public IReadOnlyList<RiftWaypointScalarComparisonCandidate> Comparisons { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftWaypointScalarComparisonInputSummary
{
    [JsonPropertyName("input_index")]
    public int InputIndex { get; init; }

    [JsonPropertyName("input_path")]
    public string InputPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("anchor_path")]
    public string AnchorPath { get; init; } = string.Empty;

    [JsonPropertyName("primary_anchor_id")]
    public string PrimaryAnchorId { get; init; } = string.Empty;

    [JsonPropertyName("primary_waypoint_x")]
    public double? PrimaryWaypointX { get; init; }

    [JsonPropertyName("primary_waypoint_z")]
    public double? PrimaryWaypointZ { get; init; }

    [JsonPropertyName("primary_delta_x")]
    public double? PrimaryDeltaX { get; init; }

    [JsonPropertyName("primary_delta_z")]
    public double? PrimaryDeltaZ { get; init; }

    [JsonPropertyName("scalar_hit_count")]
    public int ScalarHitCount { get; init; }

    [JsonPropertyName("emitted_scalar_hit_count")]
    public int EmittedScalarHitCount { get; init; }

    [JsonPropertyName("scalar_axis_hit_counts")]
    public IReadOnlyDictionary<string, int> ScalarAxisHitCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("pair_candidate_count")]
    public int PairCandidateCount { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record RiftWaypointScalarComparisonCandidate
{
    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("axis")]
    public string Axis { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_region_ids")]
    public IReadOnlyList<string> SourceRegionIds { get; init; } = [];

    [JsonPropertyName("present_input_indexes")]
    public IReadOnlyList<int> PresentInputIndexes { get; init; } = [];

    [JsonPropertyName("missing_input_indexes")]
    public IReadOnlyList<int> MissingInputIndexes { get; init; } = [];

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("baseline_memory_value")]
    public double? BaselineMemoryValue { get; init; }

    [JsonPropertyName("latest_memory_value")]
    public double? LatestMemoryValue { get; init; }

    [JsonPropertyName("baseline_anchor_value")]
    public double? BaselineAnchorValue { get; init; }

    [JsonPropertyName("latest_anchor_value")]
    public double? LatestAnchorValue { get; init; }

    [JsonPropertyName("observed_delta")]
    public double? ObservedDelta { get; init; }

    [JsonPropertyName("waypoint_delta")]
    public double? WaypointDelta { get; init; }

    [JsonPropertyName("delta_error")]
    public double? DeltaError { get; init; }

    [JsonPropertyName("best_abs_distance")]
    public double? BestAbsDistance { get; init; }

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "candidate_unverified";

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
