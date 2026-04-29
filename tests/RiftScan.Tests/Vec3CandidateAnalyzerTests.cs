using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Vectors;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class Vec3CandidateAnalyzerTests
{
    [Fact]
    public void Analyze_session_promotes_float_triplet_structures_to_vec3_candidates()
    {
        using var session = CopyFixtureToTemp();
        _ = new FloatTripletStructureAnalyzer().AnalyzeSession(session.Path);

        var candidates = new Vec3CandidateAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.RegionId == "region-0001" && candidate.OffsetHex == "0x0");
        Assert.Equal("vec3_candidate", candidate.AnalyzerId);
        Assert.Equal("0.1.0", candidate.AnalyzerVersion);
        Assert.Equal("vec3_float32", candidate.DataType);
        Assert.Equal("unvalidated_candidate", candidate.ValidationStatus);
        Assert.Equal("high", candidate.ConfidenceLevel);
        Assert.Equal("vec3_candidate_followup", candidate.ExplanationShort);
        Assert.Equal("vec3_candidate_followup", candidate.Recommendation);
        Assert.Contains("candidate_not_truth_claim", candidate.Diagnostics);
        Assert.True(File.Exists(Path.Combine(session.Path, "vec3_candidates.jsonl")));
    }

    [Fact]
    public void Analyze_session_scores_move_forward_stimulus_when_vec3_changes()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new MovingVec3ProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = "move_forward"
        });
        _ = new FloatTripletStructureAnalyzer().AnalyzeSession(session.Path);

        var candidates = new Vec3CandidateAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.OffsetHex == "0x0");
        Assert.Equal("move_forward", candidate.StimulusLabel);
        Assert.Equal(25, candidate.BehaviorScore);
        Assert.Equal("behavior_consistent_candidate", candidate.ValidationStatus);
        Assert.Equal("high", candidate.ConfidenceLevel);
        Assert.Equal("move_forward_vec3_candidate_followup", candidate.ExplanationShort);
        Assert.Equal("move_forward_vec3_candidate_followup", candidate.Recommendation);
        Assert.True(candidate.ValueDeltaMagnitude > 0);
        Assert.Contains("move_forward_vec3_changed", candidate.Diagnostics);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-vec3-tests", Guid.NewGuid().ToString("N"));
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

    private sealed class MovingVec3ProcessMemoryReader : IProcessMemoryReader
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
            BitConverter.GetBytes(1.0f + _readCount++).CopyTo(bytes, 0);
            BitConverter.GetBytes(2.0f).CopyTo(bytes, 4);
            BitConverter.GetBytes(-3.0f).CopyTo(bytes, 8);
            return bytes;
        }
    }
}
