using System.Text.Json.Serialization;

namespace RiftScan.Capture.Passive;

public sealed record PassiveCaptureResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("regions_captured")]
    public int RegionsCaptured { get; init; }

    [JsonPropertyName("snapshots_captured")]
    public int SnapshotsCaptured { get; init; }

    [JsonPropertyName("bytes_captured")]
    public long BytesCaptured { get; init; }

    [JsonPropertyName("handoff_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HandoffPath { get; init; }

    [JsonPropertyName("artifacts_written")]
    public IReadOnlyList<string> ArtifactsWritten { get; init; } = [];
}
