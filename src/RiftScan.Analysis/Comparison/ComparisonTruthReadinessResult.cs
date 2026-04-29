using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ComparisonTruthReadinessResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.comparison_truth_readiness.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_a_id")]
    public string SessionAId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_id")]
    public string SessionBId { get; init; } = string.Empty;

    [JsonPropertyName("session_a_path")]
    public string SessionAPath { get; init; } = string.Empty;

    [JsonPropertyName("session_b_path")]
    public string SessionBPath { get; init; } = string.Empty;

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("entity_layout")]
    public ComparisonTruthReadinessStatus EntityLayout { get; init; } = new();

    [JsonPropertyName("position")]
    public ComparisonTruthReadinessStatus Position { get; init; } = new();

    [JsonPropertyName("actor_yaw")]
    public ComparisonTruthReadinessStatus ActorYaw { get; init; } = new();

    [JsonPropertyName("camera_orientation")]
    public ComparisonTruthReadinessStatus CameraOrientation { get; init; } = new();

    [JsonPropertyName("next_required_capture")]
    public ComparisonTruthReadinessCaptureRequirement NextRequiredCapture { get; init; } = new();

    [JsonPropertyName("top_entity_layout_matches")]
    public IReadOnlyList<EntityLayoutComparison> TopEntityLayoutMatches { get; init; } = [];

    [JsonPropertyName("top_vec3_behavior_candidates")]
    public IReadOnlyList<Vec3BehaviorContrastCandidate> TopVec3BehaviorCandidates { get; init; } = [];

    [JsonPropertyName("top_scalar_behavior_candidates")]
    public IReadOnlyList<ScalarBehaviorCandidate> TopScalarBehaviorCandidates { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record ComparisonTruthReadinessStatus
{
    [JsonPropertyName("component")]
    public string Component { get; init; } = string.Empty;

    [JsonPropertyName("readiness")]
    public string Readiness { get; init; } = "missing";

    [JsonPropertyName("evidence_count")]
    public int EvidenceCount { get; init; }

    [JsonPropertyName("confidence_score")]
    public double ConfidenceScore { get; init; }

    [JsonPropertyName("primary_reason")]
    public string PrimaryReason { get; init; } = string.Empty;

    [JsonPropertyName("next_action")]
    public string NextAction { get; init; } = string.Empty;

    [JsonPropertyName("blocking_gaps")]
    public IReadOnlyList<string> BlockingGaps { get; init; } = [];
}

public sealed record ComparisonTruthReadinessCaptureRequirement
{
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("expected_signal")]
    public string ExpectedSignal { get; init; } = string.Empty;

    [JsonPropertyName("stop_condition")]
    public string StopCondition { get; init; } = string.Empty;

    [JsonPropertyName("target_count")]
    public int TargetCount { get; init; }

    [JsonPropertyName("target_preview")]
    public IReadOnlyList<ComparisonTruthReadinessTargetPreview> TargetPreview { get; init; } = [];
}

public sealed record ComparisonTruthReadinessTargetPreview
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("priority_score")]
    public double PriorityScore { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
