namespace RiftScan.Core.Processes;

public static class MemoryRegionConstants
{
    public const uint MemCommit = 0x1000;
    public const uint MemPrivate = 0x20000;
    public const uint MemMapped = 0x40000;
    public const uint MemImage = 0x1000000;

    public const uint PageNoAccess = 0x01;
    public const uint PageReadOnly = 0x02;
    public const uint PageReadWrite = 0x04;
    public const uint PageWriteCopy = 0x08;
    public const uint PageExecute = 0x10;
    public const uint PageExecuteRead = 0x20;
    public const uint PageExecuteReadWrite = 0x40;
    public const uint PageExecuteWriteCopy = 0x80;
    public const uint PageGuard = 0x100;
}
