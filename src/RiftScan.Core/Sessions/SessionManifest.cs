using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionManifest
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("project_version")]
    public string ProjectVersion { get; init; } = string.Empty;

    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; init; }

    [JsonPropertyName("machine_name")]
    public string MachineName { get; init; } = string.Empty;

    [JsonPropertyName("os_version")]
    public string OsVersion { get; init; } = string.Empty;

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_start_time_utc")]
    public DateTimeOffset ProcessStartTimeUtc { get; init; }

    [JsonPropertyName("capture_mode")]
    public string CaptureMode { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("region_count")]
    public int RegionCount { get; init; }

    [JsonPropertyName("total_bytes_raw")]
    public long TotalBytesRaw { get; init; }

    [JsonPropertyName("total_bytes_stored")]
    public long TotalBytesStored { get; init; }

    [JsonPropertyName("compression")]
    public string Compression { get; init; } = string.Empty;

    [JsonPropertyName("checksum_algorithm")]
    public string ChecksumAlgorithm { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
