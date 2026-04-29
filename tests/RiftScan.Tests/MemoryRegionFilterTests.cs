using RiftScan.Core.Processes;

namespace RiftScan.Tests;

public sealed class MemoryRegionFilterTests
{
    [Fact]
    public void Default_filter_accepts_small_committed_readwrite_private_region()
    {
        var region = new VirtualMemoryRegion(
            "region-000001",
            0x1000,
            4096,
            MemoryRegionConstants.MemCommit,
            MemoryRegionConstants.PageReadWrite,
            MemoryRegionConstants.MemPrivate);

        Assert.True(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions()));
    }

    [Theory]
    [InlineData(MemoryRegionConstants.PageNoAccess)]
    [InlineData(MemoryRegionConstants.PageReadWrite | MemoryRegionConstants.PageGuard)]
    public void Default_filter_rejects_unreadable_or_guarded_regions(uint protection)
    {
        var region = new VirtualMemoryRegion(
            "region-000001",
            0x1000,
            4096,
            MemoryRegionConstants.MemCommit,
            protection,
            MemoryRegionConstants.MemPrivate);

        Assert.False(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions()));
    }

    [Fact]
    public void Default_filter_rejects_image_regions_unless_enabled()
    {
        var region = new VirtualMemoryRegion(
            "region-000001",
            0x1000,
            4096,
            MemoryRegionConstants.MemCommit,
            MemoryRegionConstants.PageExecuteRead,
            MemoryRegionConstants.MemImage);

        Assert.False(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions()));
        Assert.True(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions { IncludeImageRegions = true }));
    }

    [Fact]
    public void Default_filter_rejects_regions_over_capture_limit()
    {
        var region = new VirtualMemoryRegion(
            "region-000001",
            0x1000,
            4096,
            MemoryRegionConstants.MemCommit,
            MemoryRegionConstants.PageReadOnly,
            MemoryRegionConstants.MemPrivate);

        Assert.False(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions { MaxRegionBytes = 1024 }));
    }

    [Fact]
    public void Default_filter_can_accept_large_regions_when_reader_uses_per_region_read_cap()
    {
        var region = new VirtualMemoryRegion(
            "region-000001",
            0x1000,
            4096,
            MemoryRegionConstants.MemCommit,
            MemoryRegionConstants.PageReadOnly,
            MemoryRegionConstants.MemPrivate);

        Assert.True(MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions
        {
            MaxRegionBytes = 1024,
            RejectRegionsLargerThanMaxRegionBytes = false
        }));
    }
}
