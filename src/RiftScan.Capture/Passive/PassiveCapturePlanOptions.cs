namespace RiftScan.Capture.Passive;

public sealed record PassiveCapturePlanOptions
{
    public required string SourceSessionPath { get; init; }

    public string? ProcessName { get; init; }

    public int? ProcessId { get; init; }

    public required string OutputPath { get; init; }

    public int TopRegions { get; init; } = 5;

    public int Samples { get; init; } = 3;

    public int IntervalMilliseconds { get; init; } = 100;

    public int MaxBytesPerRegion { get; init; } = 64 * 1024;

    public long MaxTotalBytes { get; init; } = 1024 * 1024;

    public bool IncludeImageRegions { get; init; }
}
