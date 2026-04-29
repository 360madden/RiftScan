using System.Diagnostics;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class SmokeArtifactSummaryScriptTests
{
    [Fact]
    public void Write_smoke_artifact_summary_writes_manifest_table()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            var smokeRoot = Path.Combine(artifactRoot, "smoke-fixture");
            Directory.CreateDirectory(smokeRoot);
            File.WriteAllText(Path.Combine(smokeRoot, "proof.json"), "{\"ok\":true}\n");
            WriteManifest(smokeRoot, "fixture", "proof.json", bytes: 12);
            var summaryPath = Path.Combine(tempDirectory, "summary.md");

            var result = RunSummaryScript(
                "-Root", artifactRoot,
                "-ArtifactName", "test-artifact",
                "-RetentionDays", "7",
                "-SummaryPath", summaryPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            var summary = File.ReadAllText(summaryPath);
            Assert.Contains("## RiftScan smoke artifacts", summary, StringComparison.Ordinal);
            Assert.Contains("- Artifact: `test-artifact`", summary, StringComparison.Ordinal);
            Assert.Contains("- Retention: 7 days", summary, StringComparison.Ordinal);
            Assert.Contains("- Manifest count: 1", summary, StringComparison.Ordinal);
            Assert.Contains("| fixture | 1 | 12 | `smoke-fixture/smoke-manifest.json` |", summary, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_smoke_artifact_summary_sorts_manifests_and_sums_file_bytes()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            var migrationRoot = Path.Combine(artifactRoot, "smoke-migration");
            var fixtureRoot = Path.Combine(artifactRoot, "smoke-fixture");
            Directory.CreateDirectory(migrationRoot);
            Directory.CreateDirectory(fixtureRoot);
            WriteManifest(migrationRoot, "migration", ("migration-plan.json", 20), ("migrated/manifest.json", 30));
            WriteManifest(fixtureRoot, "fixture", ("report.md", 7), ("reports/fixture-comparison.json", 11));
            var summaryPath = Path.Combine(tempDirectory, "summary.md");

            var result = RunSummaryScript("-Root", artifactRoot, "-SummaryPath", summaryPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            var summary = File.ReadAllText(summaryPath);
            Assert.Contains("- Manifest count: 2", summary, StringComparison.Ordinal);
            Assert.Contains("| fixture | 2 | 18 | `smoke-fixture/smoke-manifest.json` |", summary, StringComparison.Ordinal);
            Assert.Contains("| migration | 2 | 50 | `smoke-migration/smoke-manifest.json` |", summary, StringComparison.Ordinal);
            Assert.True(
                summary.IndexOf("`smoke-fixture/smoke-manifest.json`", StringComparison.Ordinal) <
                summary.IndexOf("`smoke-migration/smoke-manifest.json`", StringComparison.Ordinal),
                summary);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_smoke_artifact_summary_writes_stdout_when_summary_path_is_blank()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            var smokeRoot = Path.Combine(artifactRoot, "smoke-fixture");
            Directory.CreateDirectory(smokeRoot);
            WriteManifest(smokeRoot, "fixture", "proof.json", bytes: 12);

            var result = RunSummaryScript("-Root", artifactRoot, "-SummaryPath", string.Empty);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            Assert.Contains("## RiftScan smoke artifacts", result.Stdout, StringComparison.Ordinal);
            Assert.Contains("| fixture | 1 | 12 | `smoke-fixture/smoke-manifest.json` |", result.Stdout, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_smoke_artifact_summary_rejects_missing_root()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var missingRoot = Path.Combine(tempDirectory, "missing");
            var summaryPath = Path.Combine(tempDirectory, "summary.md");

            var result = RunSummaryScript("-Root", missingRoot, "-SummaryPath", summaryPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Smoke artifact root does not exist", result.Stderr, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(summaryPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_smoke_artifact_summary_rejects_empty_root()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var summaryPath = Path.Combine(tempDirectory, "summary.md");

            var result = RunSummaryScript("-Root", tempDirectory, "-SummaryPath", summaryPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("No smoke manifests were found", result.Stderr, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(summaryPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void WriteManifest(string outputRoot, string smokeName, string relativePath, long bytes)
    {
        WriteManifest(outputRoot, smokeName, (relativePath, bytes));
    }

    private static void WriteManifest(string outputRoot, string smokeName, params (string RelativePath, long Bytes)[] entries)
    {
        var manifest = new
        {
            schema_version = "riftscan.smoke_manifest.v1",
            smoke_name = smokeName,
            output_root = Path.GetFullPath(outputRoot),
            created_utc = DateTimeOffset.UtcNow.ToString("O"),
            file_count = entries.Length,
            files = entries.Select(entry => new
            {
                path = entry.RelativePath,
                bytes = entry.Bytes,
                sha256 = new string('0', 64)
            }).ToArray()
        };

        File.WriteAllText(Path.Combine(outputRoot, "smoke-manifest.json"), JsonSerializer.Serialize(manifest));
    }

    private static (int ExitCode, string Stdout, string Stderr) RunSummaryScript(params string[] arguments)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "write-smoke-artifact-summary.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("write-smoke-artifact-summary.ps1 did not exit within 30 seconds.");
        }

        return (process.ExitCode, stdout, stderr);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RiftScan.slnx")) && Directory.Exists(Path.Combine(current.FullName, "scripts")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find RiftScan repo root.");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
