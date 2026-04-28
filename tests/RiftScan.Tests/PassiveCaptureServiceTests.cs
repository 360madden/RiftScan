using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class PassiveCaptureServiceTests
{
    [Fact]
    public void Capture_writes_verifiable_session_from_readable_fixture_region()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader();
        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Equal(1, result.SnapshotsCaptured);
        Assert.Equal(16, result.BytesCaptured);
        Assert.True(File.Exists(Path.Combine(output.Path, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "regions.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "snapshots", "index.jsonl")));

        var verification = new SessionVerifier().Verify(output.Path);
        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
    }

    [Fact]
    public void Capture_refuses_to_choose_between_duplicate_process_names()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader
        {
            Processes =
            [
                new ProcessDescriptor(100, "fixture_process", DateTimeOffset.UtcNow, null),
                new ProcessDescriptor(101, "fixture_process", DateTimeOffset.UtcNow, null)
            ]
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path
        }));
        Assert.Contains("Use --pid", ex.Message, StringComparison.Ordinal);
    }

    private sealed class FakeProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> Processes { get; init; } =
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
            Processes.Where(process => string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)).ToArray();

        public ProcessDescriptor GetProcessById(int processId) =>
            Processes.Single(process => process.ProcessId == processId);

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion(
                "region-000001",
                0x1000,
                16,
                MemoryRegionConstants.MemCommit,
                MemoryRegionConstants.PageReadWrite,
                MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount) =>
            Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-capture-tests", Guid.NewGuid().ToString("N"));
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
