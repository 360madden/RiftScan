namespace RiftScan.Core.Processes;

public static class MemoryRegionFilter
{
    public static bool IsDefaultCaptureCandidate(VirtualMemoryRegion region, MemoryRegionFilterOptions options)
    {
        if (region.SizeBytes == 0)
        {
            return false;
        }

        if (options.RejectRegionsLargerThanMaxRegionBytes && region.SizeBytes > options.MaxRegionBytes)
        {
            return false;
        }

        if (region.State != MemoryRegionConstants.MemCommit)
        {
            return false;
        }

        if (!options.IncludeImageRegions && region.Type == MemoryRegionConstants.MemImage)
        {
            return false;
        }

        if ((region.Protect & MemoryRegionConstants.PageGuard) != 0)
        {
            return false;
        }

        var baseProtect = region.Protect & 0xFF;
        return baseProtect is MemoryRegionConstants.PageReadOnly
            or MemoryRegionConstants.PageReadWrite
            or MemoryRegionConstants.PageWriteCopy
            or MemoryRegionConstants.PageExecuteRead
            or MemoryRegionConstants.PageExecuteReadWrite
            or MemoryRegionConstants.PageExecuteWriteCopy;
    }
}
