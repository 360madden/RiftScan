using System.Text.Json.Serialization;
using RiftScan.Analysis.Comparison;

namespace RiftScan.Rift.Addons;

public sealed record RiftActorCoordinateOwnerMoveCapturePlanOptions
{
    public string HypothesesPath { get; init; } = string.Empty;

    public int Samples { get; init; } = 8;

    public int IntervalMilliseconds { get; init; } = 100;

    public int MaxBytesPerRegion { get; init; } = 98304;

    public int InterventionWaitMilliseconds { get; init; } = 120000;

    public int InterventionPollMilliseconds { get; init; } = 2000;

    public string? OutputPath { get; init; }
}

public sealed record RiftActorCoordinateOwnerMoveCapturePlanResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_move_capture_plan.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_actor_coordinate_owner_move_capture_plan";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("hypotheses_path")]
    public string HypothesesPath { get; init; } = string.Empty;

    [JsonPropertyName("hypotheses_verification_path")]
    public string HypothesesVerificationPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("target_region_base_hex")]
    public string TargetRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("owner_candidate_region_base_hex")]
    public string OwnerCandidateRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("target_region_count")]
    public int TargetRegionCount { get; init; }

    [JsonPropertyName("target_region_bases")]
    public IReadOnlyList<string> TargetRegionBases { get; init; } = [];

    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    [JsonPropertyName("interval_ms")]
    public int IntervalMilliseconds { get; init; }

    [JsonPropertyName("max_bytes_per_region")]
    public int MaxBytesPerRegion { get; init; }

    [JsonPropertyName("max_total_bytes")]
    public long MaxTotalBytes { get; init; }

    [JsonPropertyName("intervention_wait_ms")]
    public int InterventionWaitMilliseconds { get; init; }

    [JsonPropertyName("intervention_poll_ms")]
    public int InterventionPollMilliseconds { get; init; }

    [JsonPropertyName("capture_plan_path")]
    public string? CapturePlanPath { get; init; }

    [JsonPropertyName("capture_plan")]
    public ComparisonNextCapturePlan CapturePlan { get; init; } = new();

    [JsonPropertyName("recommended_capture_args")]
    public IReadOnlyList<string> RecommendedCaptureArgs { get; init; } = [];

    [JsonPropertyName("recommended_capture_command_template")]
    public string RecommendedCaptureCommandTemplate { get; init; } = string.Empty;

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
