using RiftScan.Analysis.Deltas;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ByteDeltaAnalyzerTests
{
    [Fact]
    public void Analyze_session_writes_changed_byte_ranges()
    {
        using var session = new TempDirectory();
        var reader = new ChangingProcessMemoryReader();
        _ = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32
        });

        var entries = new ByteDeltaAnalyzer().AnalyzeSession(session.Path);

        var entry = Assert.Single(entries);
        Assert.Equal("region-000001", entry.RegionId);
        Assert.Equal(1, entry.ChangedByteCount);
        Assert.Equal("sparse_offset_followup", entry.Recommendation);
        Assert.Contains(entry.ChangedRanges, range => range.StartOffsetHex == "0x0" && range.EndOffsetHex == "0x0");
        Assert.True(File.Exists(Path.Combine(session.Path, "deltas.jsonl")));
    }

    private sealed class ChangingProcessMemoryReader : IProcessMemoryReader
    {
        private int _readCount;

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            bytes[0] = (byte)_readCount++;
            return bytes;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-delta-tests", Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
