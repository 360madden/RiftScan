using System.Text.Json.Serialization;

namespace RiftScan.Capture.Passive;

public sealed record CaptureInterventionHandoff
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.capture_intervention_handoff.v1";

    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_start_time_utc")]
    public DateTimeOffset? ProcessStartTimeUtc { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("elapsed_ms")]
    public long ElapsedMilliseconds { get; init; }

    [JsonPropertyName("regions_captured")]
    public int RegionCount { get; init; }

    [JsonPropertyName("snapshots_captured")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("bytes_captured")]
    public long BytesCaptured { get; init; }

    [JsonPropertyName("region_read_failures")]
    public IReadOnlyList<CaptureInterventionRegionReadFailure> RegionReadFailures { get; init; } = [];

    [JsonPropertyName("samples_targeted")]
    public int SamplesTargeted { get; init; }

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; init; } = "resume_capture_when_process_returns_or_review_partial_session";

    [JsonPropertyName("intervention_wait_ms")]
    public int InterventionWaitMilliseconds { get; init; }

    [JsonPropertyName("intervention_poll_ms")]
    public int InterventionPollIntervalMilliseconds { get; init; }
}

public sealed record CaptureInterventionRegionReadFailure
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("requested_bytes")]
    public int RequestedBytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
