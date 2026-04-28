namespace RiftScan.Core.Processes;

public sealed record VirtualMemoryRegion(
    string RegionId,
    ulong BaseAddress,
    ulong SizeBytes,
    uint State,
    uint Protect,
    uint Type)
{
    public string BaseAddressHex => $"0x{BaseAddress:X}";

    public string StateName => MemoryRegionNames.StateName(State);

    public string ProtectName => MemoryRegionNames.ProtectionName(Protect);

    public string TypeName => MemoryRegionNames.TypeName(Type);
}
