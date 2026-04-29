using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionPruneServiceTests
{
    [Fact]
    public void Prune_dry_run_lists_generated_artifacts_without_deleting_them()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "triage.jsonl"), "{}\n");
            File.WriteAllText(Path.Combine(sessionPath, "report.md"), "# report\n");
            File.WriteAllText(Path.Combine(sessionPath, "next_capture_plan.json"), "{}\n");

            var result = new SessionPruneService().Prune(sessionPath);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            Assert.True(result.DryRun);
            Assert.Equal("preserve_raw_artifacts_no_mutation", result.RawDataPolicy);
            Assert.Equal(3, result.CandidateCount);
            Assert.Equal(result.Candidates.Sum(candidate => candidate.Bytes), result.BytesReclaimable);
            Assert.Equal(["next_capture_plan.json", "report.md", "triage.jsonl"], result.Candidates.Select(candidate => candidate.Path).ToArray());
            Assert.All(result.Candidates, candidate => Assert.False(string.IsNullOrWhiteSpace(candidate.Reason)));
            Assert.True(File.Exists(Path.Combine(sessionPath, "triage.jsonl")));
            Assert.True(File.Exists(Path.Combine(sessionPath, "report.md")));
            Assert.True(File.Exists(Path.Combine(sessionPath, "next_capture_plan.json")));
            Assert.True(File.Exists(Path.Combine(sessionPath, "manifest.json")));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Prune_apply_is_rejected_without_deleting_anything()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            var generatedPath = Path.Combine(sessionPath, "triage.jsonl");
            File.WriteAllText(generatedPath, "{}\n");

            var result = new SessionPruneService().Prune(sessionPath, dryRun: false);

            Assert.False(result.Success);
            Assert.False(result.DryRun);
            Assert.Equal(0, result.CandidateCount);
            Assert.Contains(result.Issues, issue => issue.Code == "prune_apply_not_supported");
            Assert.True(File.Exists(generatedPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_session_prune_emits_machine_readable_dry_run_result()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "clusters.jsonl"), "{}\n");

            var result = RunCli("session", "prune", sessionPath, "--dry-run");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.Equal("riftscan.session_prune_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.True(document.RootElement.GetProperty("dry_run").GetBoolean());
            Assert.Equal(1, document.RootElement.GetProperty("candidate_count").GetInt32());
            Assert.Equal("clusters.jsonl", document.RootElement.GetProperty("candidates")[0].GetProperty("path").GetString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile);
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(args);
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
