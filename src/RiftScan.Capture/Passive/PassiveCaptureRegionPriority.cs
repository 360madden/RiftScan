using RiftScan.Core.Processes;

namespace RiftScan.Capture.Passive;

internal static class PassiveCaptureRegionPriority
{
    public static IOrderedEnumerable<VirtualMemoryRegion> OrderForDefaultCapture(
        IEnumerable<VirtualMemoryRegion> regions,
        int maxBytesPerRegion) =>
        regions
            .OrderByDescending(CapturePriorityBucket)
            .ThenByDescending(region => Math.Min(region.SizeBytes, (ulong)Math.Max(1, maxBytesPerRegion)))
            .ThenBy(region => region.BaseAddress);

    private static int CapturePriorityBucket(VirtualMemoryRegion region)
    {
        var baseProtect = region.Protect & 0xFF;
        var writable = baseProtect is MemoryRegionConstants.PageReadWrite
            or MemoryRegionConstants.PageWriteCopy
            or MemoryRegionConstants.PageExecuteReadWrite
            or MemoryRegionConstants.PageExecuteWriteCopy;

        var privateRegion = region.Type == MemoryRegionConstants.MemPrivate;
        var mappedRegion = region.Type == MemoryRegionConstants.MemMapped;

        return (privateRegion, mappedRegion, writable) switch
        {
            (true, _, true) => 50,
            (_, true, true) => 40,
            (true, _, false) => 30,
            (_, true, false) => 20,
            (_, _, true) => 10,
            _ => 0
        };
    }
}
