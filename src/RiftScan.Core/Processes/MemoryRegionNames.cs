namespace RiftScan.Core.Processes;

public static class MemoryRegionNames
{
    public static string StateName(uint state) => state switch
    {
        MemoryRegionConstants.MemCommit => "MEM_COMMIT",
        _ => $"0x{state:X}"
    };

    public static string TypeName(uint type) => type switch
    {
        MemoryRegionConstants.MemPrivate => "MEM_PRIVATE",
        MemoryRegionConstants.MemMapped => "MEM_MAPPED",
        MemoryRegionConstants.MemImage => "MEM_IMAGE",
        _ => $"0x{type:X}"
    };

    public static string ProtectionName(uint protect)
    {
        var baseProtect = protect & 0xFF;
        var name = baseProtect switch
        {
            MemoryRegionConstants.PageNoAccess => "PAGE_NOACCESS",
            MemoryRegionConstants.PageReadOnly => "PAGE_READONLY",
            MemoryRegionConstants.PageReadWrite => "PAGE_READWRITE",
            MemoryRegionConstants.PageWriteCopy => "PAGE_WRITECOPY",
            MemoryRegionConstants.PageExecute => "PAGE_EXECUTE",
            MemoryRegionConstants.PageExecuteRead => "PAGE_EXECUTE_READ",
            MemoryRegionConstants.PageExecuteReadWrite => "PAGE_EXECUTE_READWRITE",
            MemoryRegionConstants.PageExecuteWriteCopy => "PAGE_EXECUTE_WRITECOPY",
            _ => $"0x{baseProtect:X}"
        };

        if ((protect & MemoryRegionConstants.PageGuard) != 0)
        {
            name += "|PAGE_GUARD";
        }

        return name;
    }
}
