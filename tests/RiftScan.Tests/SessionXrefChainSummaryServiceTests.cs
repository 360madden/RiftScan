using System.Text.Json;
using RiftScan.Analysis.Xrefs;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionXrefChainSummaryServiceTests
{
    [Fact]
    public void Summarize_finds_stable_edges_and_reciprocal_pairs_from_xref_json()
    {
        using var workspace = CreateReciprocalXrefReports();

        var result = new SessionXrefChainSummaryService().Summarize(new SessionXrefChainSummaryOptions
        {
            InputPaths = [workspace.FirstReportPath, workspace.SecondReportPath],
            MinSupport = 3,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("riftscan.session_xref_chain_summary_result.v1", result.ResultSchemaVersion);
        Assert.Equal(2, result.InputCount);
        Assert.Equal(2, result.StableEdgeCount);
        Assert.Equal(1, result.ReciprocalPairCount);
        Assert.All(result.StableEdges, edge => Assert.Equal(3, edge.SupportCount));
        Assert.Contains(result.StableEdges, edge =>
            edge.SourceBaseAddressHex == "0x1000" &&
            edge.SourceAbsoluteAddressHex == "0x1010" &&
            edge.PointerValueHex == "0x2000" &&
            edge.Classification == "outside_exact_target_pointer_edge");
        Assert.Contains(result.StableEdges, edge =>
            edge.SourceBaseAddressHex == "0x2000" &&
            edge.SourceAbsoluteAddressHex == "0x2020" &&
            edge.PointerValueHex == "0x1000" &&
            edge.Classification == "outside_exact_target_pointer_edge");

        var pair = Assert.Single(result.ReciprocalPairs);
        Assert.Equal("0x1000", pair.FirstBaseAddressHex);
        Assert.Equal("0x2000", pair.SecondBaseAddressHex);
        Assert.Equal(3, pair.SupportCount);
        Assert.NotEmpty(pair.FirstToSecondEdgeIds);
        Assert.NotEmpty(pair.SecondToFirstEdgeIds);
    }

    [Fact]
    public void Summarize_marks_truncation_after_complete_exact_target_hits()
    {
        using var workspace = new XrefReportWorkspace();
        var report = BuildReport(
            targetBaseAddressHex: "0x2000",
            sourceRegionId: "region-a",
            sourceBaseAddressHex: "0x1000",
            sourceOffsetHex: "0x10",
            sourceAbsoluteAddressHex: "0x1010",
            pointerValueHex: "0x2000") with
        {
            PointerHitCount = 10,
            Warnings = ["pointer_hits_truncated_by_max_hits"]
        };
        WriteJson(workspace.FirstReportPath, report);

        var result = new SessionXrefChainSummaryService().Summarize(new SessionXrefChainSummaryOptions
        {
            InputPaths = [workspace.FirstReportPath],
            MinSupport = 1,
            Top = 10
        });

        Assert.Contains(result.Warnings, warning => warning.StartsWith("input_pointer_hits_truncated_after_exact_targets:", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Warnings, warning => warning.StartsWith("input_pointer_hits_truncated:", StringComparison.Ordinal));
    }

    [Fact]
    public void Cli_analyze_xref_chain_writes_json_and_markdown_report()
    {
        using var workspace = CreateReciprocalXrefReports();
        var jsonPath = Path.Combine(workspace.Path, "xref-chain.json");
        var markdownPath = Path.Combine(workspace.Path, "xref-chain.md");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(
            [
                "analyze",
                "xref-chain",
                workspace.FirstReportPath,
                workspace.SecondReportPath,
                "--min-support",
                "3",
                "--out",
                jsonPath,
                "--report-md",
                markdownPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(markdownPath));

            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.session_xref_chain_summary_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(2, stdoutJson.RootElement.GetProperty("stable_edge_count").GetInt32());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("reciprocal_pair_count").GetInt32());

            using var resultJson = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(Path.GetFullPath(markdownPath), resultJson.RootElement.GetProperty("markdown_report_path").GetString());
            var report = File.ReadAllText(markdownPath);
            Assert.Contains("# RiftScan Xref Chain Summary", report, StringComparison.Ordinal);
            Assert.Contains("xref-pair-000001", report, StringComparison.Ordinal);
            Assert.Contains("0x1000", report, StringComparison.Ordinal);
            Assert.Contains("0x2000", report, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Verifier_accepts_required_edges_and_reciprocal_pairs()
    {
        using var workspace = CreateReciprocalXrefReports();
        var summaryPath = WriteFixtureSummary(workspace);

        var result = new SessionXrefChainSummaryVerifier().Verify(summaryPath, new SessionXrefChainSummaryVerificationOptions
        {
            MinSupport = 3,
            RequiredEdges =
            [
                new SessionXrefRequiredEdge
                {
                    SourceBaseAddressHex = "0x1000",
                    PointerValueHex = "0x2000"
                }
            ],
            RequiredReciprocalPairs =
            [
                new SessionXrefRequiredReciprocalPair
                {
                    FirstBaseAddressHex = "0x1000",
                    SecondBaseAddressHex = "0x2000"
                }
            ]
        });

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.session_xref_chain_summary_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal(2, result.StableEdgeCount);
        Assert.Equal(1, result.ReciprocalPairCount);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Verifier_rejects_missing_required_edge()
    {
        using var workspace = CreateReciprocalXrefReports();
        var summaryPath = WriteFixtureSummary(workspace);

        var result = new SessionXrefChainSummaryVerifier().Verify(summaryPath, new SessionXrefChainSummaryVerificationOptions
        {
            MinSupport = 3,
            RequiredEdges =
            [
                new SessionXrefRequiredEdge
                {
                    SourceBaseAddressHex = "0x1000",
                    PointerValueHex = "0x9999"
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "required_edge_missing");
    }

    [Fact]
    public void Cli_verify_xref_chain_summary_prints_machine_readable_result()
    {
        using var workspace = CreateReciprocalXrefReports();
        var summaryPath = WriteFixtureSummary(workspace);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(
            [
                "verify",
                "xref-chain-summary",
                summaryPath,
                "--min-support",
                "3",
                "--require-edge",
                "0x1000=0x2000",
                "--require-reciprocal",
                "0x1000=0x2000"
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.session_xref_chain_summary_verification_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(stdoutJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(2, stdoutJson.RootElement.GetProperty("stable_edge_count").GetInt32());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("reciprocal_pair_count").GetInt32());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteFixtureSummary(XrefReportWorkspace workspace)
    {
        var summary = new SessionXrefChainSummaryService().Summarize(new SessionXrefChainSummaryOptions
        {
            InputPaths = [workspace.FirstReportPath, workspace.SecondReportPath],
            MinSupport = 3,
            Top = 10
        });
        var summaryPath = Path.Combine(workspace.Path, "summary.json");
        WriteJson(summaryPath, summary);
        return summaryPath;
    }

    private static XrefReportWorkspace CreateReciprocalXrefReports()
    {
        var workspace = new XrefReportWorkspace();
        WriteJson(workspace.FirstReportPath, BuildReport(
            targetBaseAddressHex: "0x2000",
            sourceRegionId: "region-a",
            sourceBaseAddressHex: "0x1000",
            sourceOffsetHex: "0x10",
            sourceAbsoluteAddressHex: "0x1010",
            pointerValueHex: "0x2000"));
        WriteJson(workspace.SecondReportPath, BuildReport(
            targetBaseAddressHex: "0x1000",
            sourceRegionId: "region-b",
            sourceBaseAddressHex: "0x2000",
            sourceOffsetHex: "0x20",
            sourceAbsoluteAddressHex: "0x2020",
            pointerValueHex: "0x1000"));
        return workspace;
    }

    private static SessionXrefAnalysisResult BuildReport(
        string targetBaseAddressHex,
        string sourceRegionId,
        string sourceBaseAddressHex,
        string sourceOffsetHex,
        string sourceAbsoluteAddressHex,
        string pointerValueHex)
    {
        var hits = Enumerable.Range(1, 3)
            .Select(index => new SessionXrefPointerHit
            {
                SnapshotId = $"snapshot-00000{index}",
                SourceRegionId = sourceRegionId,
                SourceBaseAddressHex = sourceBaseAddressHex,
                SourceOffsetHex = sourceOffsetHex,
                SourceAbsoluteAddressHex = sourceAbsoluteAddressHex,
                PointerValueHex = pointerValueHex,
                TargetOffsetHex = "0x0",
                MatchKind = "exact_target_offset_pointer",
                SourceIsTargetRegion = false
            })
            .ToArray();

        return new SessionXrefAnalysisResult
        {
            Success = true,
            SessionPath = "fixture-session",
            SessionId = "fixture-session",
            AnalyzerSources = ["fixture"],
            TargetBaseAddressHex = targetBaseAddressHex,
            TargetSizeBytes = 64,
            TargetRegionIds = ["target-region"],
            TargetOffsets =
            [
                new SessionXrefTargetOffset
                {
                    OffsetHex = "0x0",
                    AbsoluteAddressHex = targetBaseAddressHex
                }
            ],
            SnapshotCount = 3,
            RegionCount = 2,
            RegionsScanned = 2,
            BytesScanned = 384,
            PointerHitCount = hits.Length,
            ExactTargetPointerCount = hits.Length,
            OutsideTargetRegionPointerCount = hits.Length,
            OutsideExactTargetPointerCount = hits.Length,
            PointerHits = hits,
            Diagnostics = ["fixture_xref_report"]
        };
    }

    private static void WriteJson<T>(string path, T value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, SessionJson.Options));

    private sealed class XrefReportWorkspace : IDisposable
    {
        public XrefReportWorkspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-xref-chain-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
            FirstReportPath = System.IO.Path.Combine(Path, "first-xrefs.json");
            SecondReportPath = System.IO.Path.Combine(Path, "second-xrefs.json");
        }

        public string Path { get; }

        public string FirstReportPath { get; }

        public string SecondReportPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
