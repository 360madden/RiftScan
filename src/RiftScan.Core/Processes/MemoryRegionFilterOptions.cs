namespace RiftScan.Core.Processes;

public sealed record MemoryRegionFilterOptions
{
    public bool IncludeImageRegions { get; init; }

    public ulong MaxRegionBytes { get; init; } = 64 * 1024;
}
