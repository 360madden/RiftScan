using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed record RiftAddonCoordinateScanOptions
{
    public string Path { get; init; } = string.Empty;

    public int MaxFiles { get; init; } = 5000;

    public string? JsonlOutputPath { get; init; }

    public IReadOnlyList<string> IncludeAddonNames { get; init; } = [];

    public DateTimeOffset? MinFileLastWriteUtc { get; init; }
}

public sealed record RiftAddonCoordinateScanResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_addon_coordinate_scan.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    [JsonPropertyName("root_path_redacted")]
    public string RootPathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("jsonl_output_path")]
    public string? JsonlOutputPath { get; init; }

    [JsonPropertyName("files_scanned")]
    public int FilesScanned { get; init; }

    [JsonPropertyName("observation_count")]
    public int ObservationCount { get; init; }

    [JsonPropertyName("observations")]
    public IReadOnlyList<RiftAddonCoordinateObservation> Observations { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
