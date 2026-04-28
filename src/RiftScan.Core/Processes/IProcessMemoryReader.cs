using RiftScan.Core.Sessions;

namespace RiftScan.Core.Processes;

public interface IProcessMemoryReader
{
    IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName);

    ProcessDescriptor GetProcessById(int processId);

    IReadOnlyList<ProcessModuleInfo> GetModules(int processId);

    IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId);

    byte[] ReadMemory(int processId, ulong baseAddress, int byteCount);
}
