using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Reports;

public sealed record CapabilityStatusResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.capability_status.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    [JsonPropertyName("generated_utc")]
    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("project")]
    public string Project { get; init; } = "RiftScan";

    [JsonPropertyName("capability_count")]
    public int CapabilityCount => Capabilities.Count;

    [JsonPropertyName("capabilities")]
    public IReadOnlyList<CapabilityStatusEntry> Capabilities { get; init; } = [];

    [JsonPropertyName("truth_readiness_path")]
    public string? TruthReadinessPath { get; init; }

    [JsonPropertyName("truth_readiness_paths")]
    public IReadOnlyList<string> TruthReadinessPaths { get; init; } = [];

    [JsonPropertyName("scalar_evidence_set_path")]
    public string? ScalarEvidenceSetPath { get; init; }

    [JsonPropertyName("scalar_evidence_set_paths")]
    public IReadOnlyList<string> ScalarEvidenceSetPaths { get; init; } = [];

    [JsonPropertyName("scalar_truth_recovery_path")]
    public string? ScalarTruthRecoveryPath { get; init; }

    [JsonPropertyName("scalar_truth_recovery_paths")]
    public IReadOnlyList<string> ScalarTruthRecoveryPaths { get; init; } = [];

    [JsonPropertyName("truth_components")]
    public IReadOnlyList<CapabilityTruthComponentStatus> TruthComponents { get; init; } = [];

    [JsonPropertyName("evidence_missing")]
    public IReadOnlyList<string> EvidenceMissing { get; init; } = [];

    [JsonPropertyName("next_recommended_actions")]
    public IReadOnlyList<string> NextRecommendedActions { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record CapabilityStatusEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = "implemented";

    [JsonPropertyName("primary_command")]
    public string PrimaryCommand { get; init; } = string.Empty;

    [JsonPropertyName("evidence_surface")]
    public string EvidenceSurface { get; init; } = string.Empty;

    [JsonPropertyName("output_artifacts")]
    public IReadOnlyList<string> OutputArtifacts { get; init; } = [];

    [JsonPropertyName("remaining_gap")]
    public string RemainingGap { get; init; } = string.Empty;
}

public sealed record CapabilityTruthComponentStatus
{
    [JsonPropertyName("component")]
    public string Component { get; init; } = string.Empty;

    [JsonPropertyName("code_status")]
    public string CodeStatus { get; init; } = "coded";

    [JsonPropertyName("evidence_readiness")]
    public string EvidenceReadiness { get; init; } = "unknown";

    [JsonPropertyName("evidence_count")]
    public int EvidenceCount { get; init; }

    [JsonPropertyName("next_action")]
    public string NextAction { get; init; } = string.Empty;
}
