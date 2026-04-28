using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ComparisonNextCapturePlan
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "comparison_next_capture_plan.v1";

    [JsonPropertyName("session_a_id")]
    public string SessionAId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_id")]
    public string SessionBId { get; init; } = string.Empty;

    [JsonPropertyName("recommended_mode")]
    public string RecommendedMode { get; init; } = string.Empty;

    [JsonPropertyName("target_region_priorities")]
    public IReadOnlyList<ComparisonCaptureTarget> TargetRegionPriorities { get; init; } = [];

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("expected_signal")]
    public string ExpectedSignal { get; init; } = string.Empty;

    [JsonPropertyName("stop_condition")]
    public string StopCondition { get; init; } = string.Empty;

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
