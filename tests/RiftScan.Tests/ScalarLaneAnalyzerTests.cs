using RiftScan.Analysis.Scalars;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ScalarLaneAnalyzerTests
{
    [Fact]
    public void Analyze_session_writes_stable_float32_scalar_lanes()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new StableFloatProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = "passive_idle"
        });

        var candidates = new ScalarLaneAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.OffsetHex == "0x4");
        Assert.Equal("scalar_lane", candidate.AnalyzerId);
        Assert.Equal("0.1.0", candidate.AnalyzerVersion);
        Assert.Equal("float32", candidate.DataType);
        Assert.Equal("passive_idle", candidate.StimulusLabel);
        Assert.Equal(3, candidate.SampleCount);
        Assert.Equal(1, candidate.DistinctValueCount);
        Assert.Equal(0, candidate.ChangedSampleCount);
        Assert.Equal(0, candidate.ValueDeltaMagnitude);
        Assert.Equal("unvalidated_candidate", candidate.ValidationStatus);
        Assert.Equal("high", candidate.ConfidenceLevel);
        Assert.Equal("stable_float32_scalar_lane_3_samples", candidate.ExplanationShort);
        Assert.Equal(candidate.RankScore, candidate.ScoreBreakdown["score_total"]);
        Assert.Equal(1, candidate.FeatureVector["stability_ratio"]);
        Assert.Equal("angle_radians_0_to_2pi", candidate.ValueFamily);
        Assert.Equal("stable_angle", candidate.RetentionBucket);
        Assert.Equal(0, candidate.CircularDeltaMagnitude);
        Assert.Equal(0, candidate.SignedCircularDelta);
        Assert.Equal("stable", candidate.DominantDirection);
        Assert.Equal(1, candidate.DirectionConsistencyRatio);
        Assert.Equal(15, candidate.ScoreBreakdown["angle_shape_score"]);
        Assert.Contains("snapshots/*.bin", candidate.AnalyzerSources);
        Assert.Contains("stimuli.jsonl", candidate.AnalyzerSources);
        Assert.StartsWith("samples=3;distinct=1;changed_pairs=0;delta=0;circular_delta=0;signed_circular_delta=0;direction=stable;family=angle_radians_0_to_2pi;preview=", candidate.ValueSequenceSummary, StringComparison.Ordinal);
        Assert.Equal("passive_stable_scalar_baseline_for_behavior_contrast", candidate.Recommendation);
        Assert.Contains("1.5", candidate.ValuePreview);
        Assert.Contains("stable_scalar_lane_included", candidate.Diagnostics);
        Assert.True(File.Exists(Path.Combine(session.Path, "scalar_candidates.jsonl")));
    }

    [Fact]
    public void Analyze_session_writes_changing_float32_scalar_lanes()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new ChangingFloatProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = "turn_left"
        });

        var candidates = new ScalarLaneAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.OffsetHex == "0x4");
        Assert.Equal("turn_left", candidate.StimulusLabel);
        Assert.Equal(3, candidate.DistinctValueCount);
        Assert.Equal(2, candidate.ChangedSampleCount);
        Assert.True(candidate.ValueDeltaMagnitude > 0);
        Assert.Equal("changing_float32_scalar_lane_2_of_2_pairs", candidate.ExplanationShort);
        Assert.Equal(1, candidate.FeatureVector["change_ratio"]);
        Assert.Equal("angle_radians_0_to_2pi", candidate.ValueFamily);
        Assert.Equal("changing_angle", candidate.RetentionBucket);
        Assert.True(candidate.CircularDeltaMagnitude > 0);
        Assert.True(candidate.SignedCircularDelta > 0);
        Assert.Equal("positive", candidate.DominantDirection);
        Assert.Equal(1, candidate.DirectionConsistencyRatio);
        Assert.Equal("labeled_scalar_behavior_followup", candidate.Recommendation);
        Assert.Contains("changing_scalar_lane_included", candidate.Diagnostics);
    }

    [Fact]
    public void Analyze_session_uses_circular_delta_for_wrapped_radian_angles()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new WrappedRadianProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = "turn_right"
        });

        var candidates = new ScalarLaneAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.OffsetHex == "0x4");
        Assert.Equal("angle_radians_0_to_2pi", candidate.ValueFamily);
        Assert.True(candidate.ValueDeltaMagnitude > 6.0);
        Assert.InRange(candidate.CircularDeltaMagnitude, 0.2, 0.4);
        Assert.InRange(candidate.SignedCircularDelta, 0.2, 0.4);
        Assert.Equal("positive", candidate.DominantDirection);
        Assert.Equal(1, candidate.DirectionConsistencyRatio);
        Assert.Equal(15, candidate.ScoreBreakdown["angle_shape_score"]);
    }

    [Fact]
    public void Analyze_session_balances_retention_buckets_before_taking_top_limit()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new MixedScalarProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = "turn_left"
        });

        var candidates = new ScalarLaneAnalyzer().AnalyzeSession(session.Path, top: 2);

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, candidate => candidate.OffsetHex == "0x0" && candidate.RetentionBucket == "stable_angle");
        Assert.Contains(candidates, candidate => candidate.OffsetHex == "0x8" && candidate.RetentionBucket == "changing_generic");
        Assert.DoesNotContain(candidates, candidate => candidate.OffsetHex == "0x4" && candidate.RetentionBucket == "stable_generic");
    }

    private sealed class StableFloatProcessMemoryReader : IProcessMemoryReader
    {
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
            BitConverter.GetBytes(1.5f).CopyTo(bytes, 4);
            return bytes;
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

    private sealed class WrappedRadianProcessMemoryReader : IProcessMemoryReader
    {
        private readonly float[] _values = [6.20f, 0.05f, 0.15f];
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
            BitConverter.GetBytes(_values[Math.Min(_readCount++, _values.Length - 1)]).CopyTo(bytes, 4);
            return bytes;
        }
    }

    private sealed class MixedScalarProcessMemoryReader : IProcessMemoryReader
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
            BitConverter.GetBytes(1.5f).CopyTo(bytes, 0);
            BitConverter.GetBytes(5_000.0f).CopyTo(bytes, 4);
            BitConverter.GetBytes(10_000.0f + _readCount++).CopyTo(bytes, 8);
            return bytes;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-scalar-tests", Guid.NewGuid().ToString("N"));
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
