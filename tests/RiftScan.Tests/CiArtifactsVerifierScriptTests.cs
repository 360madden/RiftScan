using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class CiArtifactsVerifierScriptTests
{
    [Fact]
    public void Verify_ci_artifacts_accepts_valid_artifact_root()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            WriteSmokeArtifacts(Path.Combine(artifactRoot, "smoke-fixture"));
            WriteDiagnosticsArtifacts(Path.Combine(artifactRoot, "ci-diagnostics"));

            var result = RunVerifier("-Root", artifactRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            var root = document.RootElement;
            Assert.Equal("riftscan.ci_artifacts_verification.v1", root.GetProperty("result_schema_version").GetString());
            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal(1, root.GetProperty("smoke_manifest_count").GetInt32());
            Assert.True(root.GetProperty("diagnostics_index").GetProperty("success").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_artifacts_rejects_missing_diagnostics_directory()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            WriteSmokeArtifacts(Path.Combine(artifactRoot, "smoke-fixture"));

            var result = RunVerifier("-Root", artifactRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("CI diagnostics directory does not exist", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_artifacts_rejects_bad_smoke_manifest()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactRoot = Path.Combine(tempDirectory, "artifacts");
            var smokeRoot = Path.Combine(artifactRoot, "smoke-fixture");
            WriteSmokeArtifacts(smokeRoot);
            WriteDiagnosticsArtifacts(Path.Combine(artifactRoot, "ci-diagnostics"));
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(smokeRoot, "smoke-manifest.json")));
            var manifest = JsonSerializer.Deserialize<Dictionary<string, object?>>(document.RootElement.GetRawText())!;
            manifest["file_count"] = 99;
            File.WriteAllText(Path.Combine(smokeRoot, "smoke-manifest.json"), JsonSerializer.Serialize(manifest));

            var result = RunVerifier("-Root", artifactRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("file_count mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void WriteSmokeArtifacts(string smokeRoot)
    {
        Directory.CreateDirectory(smokeRoot);
        var proofPath = Path.Combine(smokeRoot, "proof.json");
        File.WriteAllText(proofPath, "{\"ok\":true}\n");
        var proofBytes = File.ReadAllBytes(proofPath);
        var manifest = new
        {
            schema_version = "riftscan.smoke_manifest.v1",
            smoke_name = "fixture",
            output_root = Path.GetFullPath(smokeRoot),
            created_utc = DateTimeOffset.UtcNow.ToString("O"),
            file_count = 1,
            files = new[]
            {
                new
                {
                    path = "proof.json",
                    bytes = proofBytes.LongLength,
                    sha256 = Convert.ToHexString(SHA256.HashData(proofBytes)).ToLowerInvariant()
                }
            }
        };

        File.WriteAllText(Path.Combine(smokeRoot, "smoke-manifest.json"), JsonSerializer.Serialize(manifest));
    }

    private static void WriteDiagnosticsArtifacts(string diagnosticsRoot)
    {
        Directory.CreateDirectory(diagnosticsRoot);
        var runInfoPath = Path.Combine(diagnosticsRoot, "run-info.json");
        var logPath = Path.Combine(diagnosticsRoot, "dotnet-test.log");
        File.WriteAllText(runInfoPath, "{\"ok\":true}\n");
        File.WriteAllText(logPath, "test output\n");
        var entries = new[] { logPath, runInfoPath }
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                var bytes = File.ReadAllBytes(path);
                return new
                {
                    path = Path.GetRelativePath(diagnosticsRoot, path).Replace('\\', '/'),
                    bytes = bytes.LongLength,
                    sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
                };
            })
            .ToArray();
        var index = new
        {
            schema_version = "riftscan.ci_diagnostics_index.v1",
            created_utc = DateTimeOffset.UtcNow.ToString("O"),
            root = Path.GetFullPath(diagnosticsRoot),
            file_count = entries.Length,
            total_bytes = entries.Sum(entry => entry.bytes),
            files = entries
        };

        File.WriteAllText(Path.Combine(diagnosticsRoot, "index.json"), JsonSerializer.Serialize(index));
    }

    private static (int ExitCode, string Stdout, string Stderr) RunVerifier(params string[] arguments)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "verify-ci-artifacts.ps1");
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
            throw new TimeoutException("verify-ci-artifacts.ps1 did not exit within 30 seconds.");
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
