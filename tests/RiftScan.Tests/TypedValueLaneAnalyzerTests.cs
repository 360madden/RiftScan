using RiftScan.Analysis.Deltas;
using RiftScan.Analysis.Values;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class TypedValueLaneAnalyzerTests
{
    [Fact]
    public void Analyze_session_writes_float32_lanes_from_delta_ranges()
    {
        using var session = new TempDirectory();
        var reader = new ChangingFloatProcessMemoryReader();
        _ = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48
        });
        _ = new ByteDeltaAnalyzer().AnalyzeSession(session.Path);

        var candidates = new TypedValueLaneAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.OffsetHex == "0x4");
        Assert.Equal("typed_value_lane", candidate.AnalyzerId);
        Assert.Equal("0.1.0", candidate.AnalyzerVersion);
        Assert.Equal("float32", candidate.DataType);
        Assert.Equal("unvalidated_candidate", candidate.ValidationStatus);
        Assert.Equal("high", candidate.ConfidenceLevel);
        Assert.Equal("float32_lane_changed_2_of_2_pairs", candidate.ExplanationShort);
        Assert.Equal(candidate.RankScore, candidate.ScoreBreakdown["score_total"]);
        Assert.Equal(100, candidate.ScoreBreakdown["score_cap"]);
        Assert.Equal(3, candidate.FeatureVector["sample_count"]);
        Assert.Equal(1, candidate.FeatureVector["change_ratio"]);
        Assert.Equal("float_lane_followup", candidate.Recommendation);
        Assert.Equal(3, candidate.DistinctValueCount);
        Assert.Contains("1.5", candidate.ValuePreview);
        Assert.True(File.Exists(Path.Combine(session.Path, "typed_value_candidates.jsonl")));
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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-value-tests", Guid.NewGuid().ToString("N"));
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
