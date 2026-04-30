using System.Text.Json.Serialization;
using RiftScan.Analysis.Comparison;

namespace RiftScan.Rift.Addons;

public sealed record RiftActorCoordinateOwnerFollowupPlanOptions
{
    public string PacketPath { get; init; } = string.Empty;

    public int TopOffsets { get; init; } = 18;

    public int Samples { get; init; } = 8;

    public int IntervalMilliseconds { get; init; } = 100;

    public int MaxBytesPerRegion { get; init; } = 65536;

    public int WindowsPerRegion { get; init; } = 3;

    public string? OutputPath { get; init; }
}

public sealed record RiftActorCoordinateOwnerFollowupPlanResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_followup_plan.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_actor_coordinate_owner_followup_plan";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("packet_path")]
    public string PacketPath { get; init; } = string.Empty;

    [JsonPropertyName("packet_verification_path")]
    public string PacketVerificationPath { get; init; } = string.Empty;

    [JsonPropertyName("passive_session_id")]
    public string PassiveSessionId { get; init; } = string.Empty;

    [JsonPropertyName("move_session_id")]
    public string MoveSessionId { get; init; } = string.Empty;

    [JsonPropertyName("target_base_address_hex")]
    public string TargetBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("top_offsets")]
    public int TopOffsets { get; init; }

    [JsonPropertyName("selected_offset_count")]
    public int SelectedOffsetCount { get; init; }

    [JsonPropertyName("target_address_count")]
    public int TargetAddressCount { get; init; }

    [JsonPropertyName("target_addresses")]
    public IReadOnlyList<string> TargetAddresses { get; init; } = [];

    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    [JsonPropertyName("interval_ms")]
    public int IntervalMilliseconds { get; init; }

    [JsonPropertyName("windows_per_region")]
    public int WindowsPerRegion { get; init; }

    [JsonPropertyName("max_bytes_per_region")]
    public int MaxBytesPerRegion { get; init; }

    [JsonPropertyName("max_total_bytes")]
    public long MaxTotalBytes { get; init; }

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
