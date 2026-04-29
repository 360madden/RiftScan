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
        Assert.True(result.MatchingStructureCandidateCount >= 1);
        Assert.Contains(result.StructureCandidateMatches, match =>
            match.BaseAddressHex == "0x10000000" &&
            match.OffsetHex == "0x0" &&
            match.SessionACandidateId == "structure-000001" &&
            match.SessionBCandidateId == "structure-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("support=", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("snapshots/*.bin") &&
            match.StructureKind == "float32_triplet" &&
            match.Recommendation == "stable_structure_candidate");
        Assert.True(result.MatchingVec3CandidateCount >= 1);
        Assert.Contains(result.Vec3CandidateMatches, match =>
            match.BaseAddressHex == "0x10000000" &&
            match.OffsetHex == "0x0" &&
            match.SessionACandidateId == "vec3-000001" &&
            match.SessionBCandidateId == "vec3-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("samples=", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("structures.jsonl") &&
            match.DataType == "vec3_float32" &&
            match.Recommendation == "stable_vec3_candidate_across_sessions");
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
            match.SessionACandidateId == "value-000001" &&
            match.SessionBCandidateId == "value-000001" &&
            match.SessionAValueSequenceSummary.StartsWith("samples=3", StringComparison.Ordinal) &&
            match.SessionAAnalyzerSources.Contains("deltas.jsonl") &&
            match.Recommendation == "stable_typed_value_lane_candidate");
    }

    [Fact]
    public void Compare_sessions_reports_vec3_behavior_contrast_between_passive_and_move_forward()
    {
        using var passive = CaptureVec3Session(new FixedVec3ProcessMemoryReader(), "passive_idle");
        using var moving = CaptureVec3Session(new MovingVec3ProcessMemoryReader(), "move_forward");
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(passive.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(moving.Path);

        var result = new SessionComparisonService().Compare(passive.Path, moving.Path);

        var match = Assert.Single(result.Vec3CandidateMatches, match => match.BaseAddressHex == "0x1000" && match.OffsetHex == "0x0");
        Assert.Equal("passive_idle", match.SessionAStimulusLabel);
        Assert.Equal("move_forward", match.SessionBStimulusLabel);
        Assert.Equal(20, match.SessionABehaviorScore);
        Assert.Equal(25, match.SessionBBehaviorScore);
        Assert.Equal(5, match.BehaviorScoreDelta);
        Assert.Equal(0, match.SessionAValueDeltaMagnitude);
        Assert.True(match.SessionBValueDeltaMagnitude > 0);
        Assert.StartsWith("samples=", match.SessionAValueSequenceSummary, StringComparison.Ordinal);
        Assert.Contains("delta=", match.SessionBValueSequenceSummary, StringComparison.Ordinal);
        Assert.Contains("stimuli.jsonl", match.SessionBAnalyzerSources);
        Assert.Equal("behavior_consistent_candidate", match.SessionAValidationStatus);
        Assert.Equal("behavior_consistent_candidate", match.SessionBValidationStatus);
        Assert.Equal("passive_to_move_vec3_behavior_contrast_candidate", match.Recommendation);
        Assert.Equal(result.Vec3CandidateMatches.Count, result.Vec3BehaviorSummary.MatchingVec3CandidateCount);
        Assert.Equal(1, result.Vec3BehaviorSummary.BehaviorContrastCount);
        Assert.Equal(1, result.Vec3BehaviorSummary.BehaviorConsistentMatchCount);
        Assert.Equal(0, result.Vec3BehaviorSummary.UnlabeledMatchCount);
        Assert.Equal(["move_forward", "passive_idle"], result.Vec3BehaviorSummary.StimulusLabels);
        Assert.Equal("review_behavior_contrast_candidates_before_truth_claim", result.Vec3BehaviorSummary.NextRecommendedAction);

        var plan = new SessionComparisonNextCapturePlanGenerator().Build(result);

        Assert.Equal("review_existing_behavior_contrast", plan.RecommendedMode);
        Assert.Equal("comparison_already_contains_behavior_contrast_candidates", plan.Reason);
        var target = plan.TargetRegionPriorities.Single(candidate =>
            candidate.BaseAddressHex == "0x1000" &&
            candidate.OffsetHex == "0x0");
        Assert.Equal("0x1000", target.BaseAddressHex);
        Assert.Equal("0x0", target.OffsetHex);
        Assert.Equal("passive_to_move_vec3_behavior_contrast_candidate", target.Reason);
        Assert.Equal(100, target.PriorityScore);
        Assert.Contains("next_capture_plan_is_recommendation_not_truth_claim", plan.Warnings);
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
            var reportPath = Path.Combine(sessionA.Path, "comparison.md");
            var nextPlanPath = Path.Combine(sessionA.Path, "comparison-next-capture-plan.json");
            var exitCode = RiftScan.Cli.Program.Main(["compare", "sessions", sessionA.Path, sessionB.Path, "--top", "10", "--out", outputPath, "--report-md", reportPath, "--next-plan", nextPlanPath]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(reportPath));
            Assert.True(File.Exists(nextPlanPath));
            Assert.Contains("matching_region_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_cluster_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_structure_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_vec3_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("vec3_behavior_summary", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_value_candidate_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_path", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_report_path", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("comparison_next_capture_plan_path", output.ToString(), StringComparison.Ordinal);
            var report = File.ReadAllText(reportPath);
            Assert.Contains("Vec3 behavior summary", report, StringComparison.Ordinal);
            Assert.Contains("Top typed value matches", report, StringComparison.Ordinal);
            Assert.Contains("structure-000001", report, StringComparison.Ordinal);
            Assert.Contains("candidate evidence, not recovered truth", report, StringComparison.Ordinal);
            var plan = File.ReadAllText(nextPlanPath);
            Assert.Contains("recommended_mode", plan, StringComparison.Ordinal);
            Assert.Contains("next_capture_plan_is_recommendation_not_truth_claim", plan, StringComparison.Ordinal);
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

    private static TempDirectory CaptureVec3Session(IProcessMemoryReader reader, string stimulusLabel)
    {
        var session = new TempDirectory();
        _ = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48,
            StimulusLabel = stimulusLabel
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

    private sealed class FixedVec3ProcessMemoryReader : IProcessMemoryReader
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
            BitConverter.GetBytes(1.0f).CopyTo(bytes, 0);
            BitConverter.GetBytes(2.0f).CopyTo(bytes, 4);
            BitConverter.GetBytes(-3.0f).CopyTo(bytes, 8);
            return bytes;
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
