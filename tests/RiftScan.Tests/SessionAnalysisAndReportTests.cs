using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionAnalysisAndReportTests
{
    [Fact]
    public void Analyze_session_writes_triage_structures_and_next_capture_plan()
    {
        using var session = CopyFixtureToTemp();

        var result = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path);

        Assert.True(result.Success);
        Assert.Equal("fixture-valid-session", result.SessionId);
        Assert.Equal(1, result.RegionsAnalyzed);
        Assert.True(File.Exists(Path.Combine(session.Path, "triage.jsonl")));
        Assert.True(File.Exists(Path.Combine(session.Path, "deltas.jsonl")));
        Assert.True(File.Exists(Path.Combine(session.Path, "typed_value_candidates.jsonl")));
        Assert.True(File.Exists(Path.Combine(session.Path, "next_capture_plan.json")));
        Assert.True(File.Exists(Path.Combine(session.Path, "structures.jsonl")));
        Assert.True(File.Exists(Path.Combine(session.Path, "vec3_candidates.jsonl")));
        Assert.True(File.Exists(Path.Combine(session.Path, "clusters.jsonl")));

        var triageLine = File.ReadLines(Path.Combine(session.Path, "triage.jsonl")).Single();
        var triage = JsonSerializer.Deserialize<RegionTriageEntry>(triageLine, SessionJson.Options)!;
        Assert.Equal("region-0001", triage.RegionId);
        Assert.Equal("capture_more_samples_before_dynamic_claim", triage.Recommendation);

        var plan = JsonSerializer.Deserialize<NextCapturePlan>(
            File.ReadAllText(Path.Combine(session.Path, "next_capture_plan.json")),
            SessionJson.Options)!;
        Assert.Equal("0x10000000", plan.Regions.Single().BaseAddressHex);
        Assert.Equal(16, plan.Regions.Single().SizeBytes);
    }

    [Fact]
    public void Report_session_writes_markdown_report_from_stored_artifacts()
    {
        using var session = CopyFixtureToTemp();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path);

        var result = new SessionReportGenerator().Generate(session.Path, top: 10);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.ReportPath));
        var report = File.ReadAllText(result.ReportPath);
        Assert.Contains("# RiftScan Session Report - fixture-valid-session", report, StringComparison.Ordinal);
        Assert.Contains("Dynamic region triage", report, StringComparison.Ordinal);
        Assert.Contains("Dynamic byte deltas", report, StringComparison.Ordinal);
        Assert.Contains("Typed value lanes", report, StringComparison.Ordinal);
        Assert.Contains("Vec3 candidates", report, StringComparison.Ordinal);
        Assert.Contains("Structure clusters", report, StringComparison.Ordinal);
        Assert.Contains("Structure candidates", report, StringComparison.Ordinal);
        Assert.Contains("region-0001", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_session_includes_interrupted_capture_handoff_details()
    {
        using var session = CaptureInterruptedSession();

        var result = new SessionReportGenerator().Generate(session.Path, top: 10);

        Assert.True(result.Success);
        var report = File.ReadAllText(result.ReportPath);
        Assert.Contains("- Status: `interrupted`", report, StringComparison.Ordinal);
        Assert.Contains("## Capture interruption", report, StringComparison.Ordinal);
        Assert.Contains("intervention_wait_timed_out", report, StringComparison.Ordinal);
        Assert.Contains("restart_or_reselect_process_then_resume_capture", report, StringComparison.Ordinal);
        Assert.Contains("region-000001", report, StringComparison.Ordinal);
        Assert.Contains("process unavailable", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_analyze_and_report_session_return_success()
    {
        using var session = CopyFixtureToTemp();
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var analyzeExit = RiftScan.Cli.Program.Main(["analyze", "session", session.Path, "--all"]);
            var reportExit = RiftScan.Cli.Program.Main(["report", "session", session.Path, "--top", "10"]);

            Assert.Equal(0, analyzeExit);
            Assert.Equal(0, reportExit);
            Assert.Contains("triage.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("deltas.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("typed_value_candidates.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("structures.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("vec3_candidates.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("clusters.jsonl", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("report.md", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session"), temp.Path);
        return temp;
    }

    private static TempDirectory CaptureInterruptedSession()
    {
        var session = new TempDirectory();
        var processLookupAttempts = 0;
        var readAttempts = 0;
        var process = new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");
        var reader = new FakeProcessMemoryReader
        {
            Processes = [process],
            FindProcessesByNameFunc = processName =>
            {
                processLookupAttempts++;
                return processLookupAttempts == 1 && string.Equals(processName, "fixture_process", StringComparison.OrdinalIgnoreCase)
                    ? [process]
                    : [];
            },
            ReadMemoryFunc = (_, _, byteCount) =>
            {
                readAttempts++;
                return readAttempts == 1
                    ? Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray()
                    : throw new InvalidOperationException("process unavailable");
            }
        };

        _ = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            InterventionWaitMilliseconds = 250,
            InterventionPollIntervalMilliseconds = 50
        });

        return session;
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-analysis-tests", Guid.NewGuid().ToString("N"));
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

    private sealed class FakeProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> Processes { get; init; } =
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public Func<string, IReadOnlyList<ProcessDescriptor>>? FindProcessesByNameFunc { get; init; }

        public Func<int, ulong, int, byte[]>? ReadMemoryFunc { get; init; }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
            FindProcessesByNameFunc?.Invoke(processName)
            ?? Processes.Where(process => string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)).ToArray();

        public ProcessDescriptor GetProcessById(int processId) =>
            Processes.Single(process => process.ProcessId == processId);

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount) =>
            ReadMemoryFunc?.Invoke(processId, baseAddress, byteCount)
            ?? Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray();
    }
}
