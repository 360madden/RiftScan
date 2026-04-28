namespace RiftScan.Capture.Passive;

public sealed record PassiveCaptureOptions
{
    public string? ProcessName { get; init; }

    public int? ProcessId { get; init; }

    public required string OutputPath { get; init; }

    public int Samples { get; init; } = 1;

    public int IntervalMilliseconds { get; init; } = 100;

    public int MaxRegions { get; init; } = 8;

    public int MaxBytesPerRegion { get; init; } = 64 * 1024;

    public long MaxTotalBytes { get; init; } = 1024 * 1024;

    public bool IncludeImageRegions { get; init; }

    public IReadOnlySet<string> RegionIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
