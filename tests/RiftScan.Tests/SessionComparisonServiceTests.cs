using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Triage;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionComparisonServiceTests
{
    [Fact]
    public void Compare_sessions_matches_same_fixture_region_and_cluster()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionA.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionB.Path);

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path);

        Assert.True(result.Success);
        Assert.Equal("fixture-valid-session", result.SessionAId);
        Assert.Equal("fixture-valid-session", result.SessionBId);
        Assert.True(result.MatchingRegionCount >= 1);
        Assert.Contains(result.RegionMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.True(result.MatchingClusterCount >= 1);
        Assert.Contains(result.ClusterMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.Contains("comparison_is_candidate_evidence_not_truth_claim", result.Warnings);
    }

    [Fact]
    public void Compare_sessions_matches_typed_value_candidates_by_base_offset_and_type()
    {
        using var sessionA = CaptureChangingFloatSession();
        using var sessionB = CaptureChangingFloatSession();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionA.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionB.Path);

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path);

        Assert.True(result.Success);
        Assert.True(result.MatchingValueCandidateCount >= 1);
        Assert.Contains(result.ValueCandidateMatches, match =>
            match.BaseAddressHex == "0x1000" &&
            match.OffsetHex == "0x4" &&
            match.DataType == "float32" &&
            match.Recommendation == "stable_typed_value_lane_candidate");
    }

    [Fact]
    public void Cli_compare_sessions_returns_success()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var outputPath = Path.Combine(sessionA.Path, "comparison.json");
            var exitCode = RiftScan.Cli.Program.Main(["compare", "sessions", sessionA.Path, sessionB.Path, "--top", "10", "--out", outputPath]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
            Assert.Contains("matching_region_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_cluster_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_value_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_path", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static TempDirectory CaptureChangingFloatSession()
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(new ChangingFloatProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48
        });

        return session;
    }

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session"), temp.Path);
        return temp;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-compare-tests", Guid.NewGuid().ToString("N"));
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

    private sealed class ChangingFloatProcessMemoryReader : IProcessMemoryReader
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
            BitConverter.GetBytes(1.5f + _readCount++).CopyTo(bytes, 4);
            return bytes;
        }
    }
}
