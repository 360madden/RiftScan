using System.Text.Json.Serialization;

namespace RiftScan.Capture.Passive;

public sealed record PassiveCaptureDryRunResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.passive_capture_dry_run_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("process_start_time_utc")]
    public DateTimeOffset? ProcessStartTimeUtc { get; init; }

    [JsonPropertyName("main_module_path")]
    public string? MainModulePath { get; init; }

    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    [JsonPropertyName("max_regions")]
    public int MaxRegions { get; init; }

    [JsonPropertyName("max_bytes_per_region")]
    public int MaxBytesPerRegion { get; init; }

    [JsonPropertyName("max_total_bytes")]
    public long MaxTotalBytes { get; init; }

    [JsonPropertyName("include_image_regions")]
    public bool IncludeImageRegions { get; init; }

    [JsonPropertyName("region_output_limit")]
    public int RegionOutputLimit { get; init; }

    [JsonPropertyName("total_region_count")]
    public int TotalRegionCount { get; init; }

    [JsonPropertyName("reported_region_count")]
    public int ReportedRegionCount { get; init; }

    [JsonPropertyName("region_output_truncated")]
    public bool RegionOutputTruncated { get; init; }

    [JsonPropertyName("candidate_region_count")]
    public int CandidateRegionCount { get; init; }

    [JsonPropertyName("selected_region_count")]
    public int SelectedRegionCount { get; init; }

    [JsonPropertyName("skipped_region_count")]
    public int SkippedRegionCount { get; init; }

    [JsonPropertyName("estimated_bytes_per_sample")]
    public long EstimatedBytesPerSample { get; init; }

    [JsonPropertyName("estimated_total_bytes")]
    public long EstimatedTotalBytes { get; init; }

    [JsonPropertyName("regions")]
    public IReadOnlyList<PassiveCaptureDryRunRegion> Regions { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record PassiveCaptureDryRunRegion
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public ulong SizeBytes { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("protection")]
    public string Protection { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("selected")]
    public bool Selected { get; init; }

    [JsonPropertyName("selected_order")]
    public int? SelectedOrder { get; init; }

    [JsonPropertyName("estimated_read_bytes")]
    public int EstimatedReadBytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("skip_reasons")]
    public IReadOnlyList<string> SkipReasons { get; init; } = [];
}
