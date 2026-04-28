using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
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
        Assert.Contains("Structure clusters", report, StringComparison.Ordinal);
        Assert.Contains("Structure candidates", report, StringComparison.Ordinal);
        Assert.Contains("region-0001", report, StringComparison.Ordinal);
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
}
