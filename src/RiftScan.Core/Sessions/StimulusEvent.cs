using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record StimulusEvent
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.stimulus.v1";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("stimulus_id")]
    public string StimulusId { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("start_snapshot_id")]
    public string StartSnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("end_snapshot_id")]
    public string EndSnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = "capture_option";

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
