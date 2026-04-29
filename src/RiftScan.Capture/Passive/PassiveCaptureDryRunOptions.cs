namespace RiftScan.Capture.Passive;

public sealed record PassiveCaptureDryRunOptions
{
    public string? ProcessName { get; init; }

    public int? ProcessId { get; init; }

    public int Samples { get; init; } = 1;

    public int MaxRegions { get; init; } = 8;

    public int MaxBytesPerRegion { get; init; } = 64 * 1024;

    public long MaxTotalBytes { get; init; } = 1024 * 1024;

    public int RegionOutputLimit { get; init; } = 250;

    public bool IncludeImageRegions { get; init; }

    public IReadOnlySet<string> RegionIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<ulong> BaseAddresses { get; init; } = new HashSet<ulong>();
}
