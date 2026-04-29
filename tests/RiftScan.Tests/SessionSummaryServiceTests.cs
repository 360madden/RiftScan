using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionSummaryServiceTests
{
    [Fact]
    public void Summary_reports_manifest_fields_and_generated_artifacts_without_creating_files()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "report.md"), "# report\n");
            File.WriteAllText(Path.Combine(sessionPath, "clusters.jsonl"), "{}\n");

            var beforeFiles = RelativeFilePaths(sessionPath);
            var result = new SessionSummaryService().Summarize(sessionPath);
            var afterFiles = RelativeFilePaths(sessionPath);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            Assert.Equal("fixture-valid-session", result.SessionId);
            Assert.Equal("riftscan.session.v1", result.SchemaVersion);
            Assert.Equal("rift_x64", result.ProcessName);
            Assert.Equal("fixture", result.CaptureMode);
            Assert.Equal("complete", result.Status);
            Assert.Equal(1, result.SnapshotCount);
            Assert.Equal(1, result.RegionCount);
            Assert.Equal(2, result.ArtifactCount);
            Assert.Equal(result.GeneratedArtifacts.Sum(artifact => artifact.Bytes), result.ArtifactBytes);
            Assert.Equal(["clusters.jsonl", "report.md"], result.GeneratedArtifacts.Select(artifact => artifact.Path).ToArray());
            Assert.Equal(beforeFiles, afterFiles);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_session_summary_emits_machine_readable_result()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "next_capture_plan.json"), "{}\n");

            var result = RunCli("session", "summary", sessionPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.Equal("riftscan.session_summary_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("fixture-valid-session", document.RootElement.GetProperty("session_id").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("artifact_count").GetInt32());
            Assert.Equal("next_capture_plan.json", document.RootElement.GetProperty("generated_artifacts")[0].GetProperty("path").GetString());
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

    private static string[] RelativeFilePaths(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directory, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

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
