using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RiftScan.Core.Sessions;

namespace RiftScan.Core.Processes;

public sealed class WindowsProcessMemoryReader : IProcessMemoryReader
{
    public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        var normalizedName = NormalizeProcessName(processName);

        return Process.GetProcessesByName(normalizedName)
            .Select(CreateDescriptor)
            .OrderBy(process => process.ProcessId)
            .ToArray();
    }

    public ProcessDescriptor GetProcessById(int processId)
    {
        using var process = Process.GetProcessById(processId);
        return CreateDescriptor(process);
    }

    public IReadOnlyList<ProcessModuleInfo> GetModules(int processId)
    {
        using var process = Process.GetProcessById(processId);
        try
        {
            var mainModule = process.MainModule;
            if (mainModule is null)
            {
                return [];
            }

            return
            [
                new ProcessModuleInfo
                {
                    ModuleId = "module-0001",
                    Name = mainModule.ModuleName,
                    Path = mainModule.FileName,
                    BaseAddressHex = $"0x{mainModule.BaseAddress.ToInt64():X}",
                    SizeBytes = mainModule.ModuleMemorySize
                }
            ];
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return [];
        }
    }

    public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId)
    {
        EnsureWindows();
        using var handle = OpenReadableProcess(processId);
        GetSystemInfo(out var systemInfo);

        var regions = new List<VirtualMemoryRegion>();
        var address = systemInfo.MinimumApplicationAddress.ToUInt64();
        var maxAddress = systemInfo.MaximumApplicationAddress.ToUInt64();
        var mbiSize = (nuint)Marshal.SizeOf<MemoryBasicInformation>();
        var index = 1;

        while (address < maxAddress)
        {
            var result = VirtualQueryEx(handle, new IntPtr(unchecked((long)address)), out var mbi, mbiSize);
            if (result == 0)
            {
                break;
            }

            var baseAddress = mbi.BaseAddress.ToUInt64();
            var regionSize = mbi.RegionSize.ToUInt64();
            if (regionSize == 0)
            {
                break;
            }

            regions.Add(new VirtualMemoryRegion(
                $"region-{index:000000}",
                baseAddress,
                regionSize,
                mbi.State,
                mbi.Protect,
                mbi.Type));
            index++;

            var nextAddress = baseAddress + regionSize;
            if (nextAddress <= address)
            {
                break;
            }

            address = nextAddress;
        }

        return regions;
    }

    public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
    {
        EnsureWindows();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(byteCount);

        using var handle = OpenReadableProcess(processId);
        var buffer = new byte[byteCount];
        if (!ReadProcessMemory(handle, new IntPtr(unchecked((long)baseAddress)), buffer, buffer.Length, out var bytesRead))
        {
            if (bytesRead <= 0)
            {
                throw new InvalidOperationException($"ReadProcessMemory failed at 0x{baseAddress:X} for {byteCount} bytes. Win32Error={Marshal.GetLastWin32Error()}.");
            }
        }

        if (bytesRead == buffer.Length)
        {
            return buffer;
        }

        return buffer[..bytesRead.ToInt32()];
    }

    private static ProcessDescriptor CreateDescriptor(Process process)
    {
        DateTimeOffset? startTimeUtc = null;
        string? mainModulePath = null;

        try
        {
            startTimeUtc = process.StartTime.ToUniversalTime();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
        }

        try
        {
            mainModulePath = process.MainModule?.FileName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
        }

        return new ProcessDescriptor(process.Id, process.ProcessName, startTimeUtc, mainModulePath);
    }

    private static string NormalizeProcessName(string processName)
    {
        var fileName = Path.GetFileName(processName);
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;
    }

    private static SafeProcessHandle OpenReadableProcess(int processId)
    {
        var handle = OpenProcess(ProcessAccess.QueryLimitedInformation | ProcessAccess.VirtualMemoryRead, false, processId);
        if (handle.IsInvalid)
        {
            throw new InvalidOperationException($"OpenProcess failed for PID {processId}. Win32Error={Marshal.GetLastWin32Error()}.");
        }

        return handle;
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("RiftScan live process scanning is currently implemented for Windows only.");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(ProcessAccess desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQueryEx(SafeProcessHandle processHandle, IntPtr address, out MemoryBasicInformation buffer, nuint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(SafeProcessHandle processHandle, IntPtr baseAddress, [Out] byte[] buffer, int size, out IntPtr bytesRead);

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SystemInfo systemInfo);

    [Flags]
    private enum ProcessAccess : uint
    {
        VirtualMemoryRead = 0x0010,
        QueryLimitedInformation = 0x1000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public UIntPtr BaseAddress;
        public UIntPtr AllocationBase;
        public uint AllocationProtect;
        public UIntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemInfo
    {
        public ushort ProcessorArchitecture;
        public ushort Reserved;
        public uint PageSize;
        public UIntPtr MinimumApplicationAddress;
        public UIntPtr MaximumApplicationAddress;
        public UIntPtr ActiveProcessorMask;
        public uint NumberOfProcessors;
        public uint ProcessorType;
        public uint AllocationGranularity;
        public ushort ProcessorLevel;
        public ushort ProcessorRevision;
    }
}
