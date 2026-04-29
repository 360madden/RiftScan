using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class SmokeManifestVerifierScriptTests
{
    [Fact]
    public void Verify_smoke_manifest_accepts_valid_manifest()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactPath = Path.Combine(tempDirectory, "reports", "proof.json");
            Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
            File.WriteAllText(artifactPath, "{\"ok\":true}\n");
            var manifestPath = WriteManifest(tempDirectory, artifactPath, sha256Override: null, fileCountOverride: null);

            var result = RunVerifier(manifestPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.Equal("fixture", document.RootElement.GetProperty("smoke_name").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("file_count").GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_rejects_bad_sha256()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactPath = Path.Combine(tempDirectory, "proof.bin");
            File.WriteAllText(artifactPath, "proof\n");
            var manifestPath = WriteManifest(tempDirectory, artifactPath, sha256Override: new string('0', 64), fileCountOverride: null);

            var result = RunVerifier(manifestPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("SHA256 mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_rejects_file_count_mismatch()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifactPath = Path.Combine(tempDirectory, "proof.bin");
            File.WriteAllText(artifactPath, "proof\n");
            var manifestPath = WriteManifest(tempDirectory, artifactPath, sha256Override: null, fileCountOverride: 2);

            var result = RunVerifier(manifestPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("file_count mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_rejects_missing_listed_file()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var manifestPath = WriteManifestEntry(
                outputRoot: tempDirectory,
                relativePath: "missing-proof.bin",
                bytes: 4,
                sha256: new string('0', 64),
                fileCount: 1);

            var result = RunVerifier(manifestPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("file is missing", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_rejects_path_escape()
    {
        var tempDirectory = CreateTempDirectory();
        var outsideFile = Path.Combine(Path.GetTempPath(), "riftscan-tests", $"outside-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllText(outsideFile, "escape\n");
            var outsideBytes = File.ReadAllBytes(outsideFile);
            var manifestPath = WriteManifestEntry(
                outputRoot: tempDirectory,
                relativePath: Path.GetRelativePath(tempDirectory, outsideFile).Replace('\\', '/'),
                bytes: outsideBytes.LongLength,
                sha256: Convert.ToHexString(SHA256.HashData(outsideBytes)).ToLowerInvariant(),
                fileCount: 1);

            var result = RunVerifier(manifestPath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("escapes output_root", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(outsideFile))
            {
                File.Delete(outsideFile);
            }

            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_root_mode_discovers_all_manifests()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var fixtureRoot = Path.Combine(tempDirectory, "smoke-fixture");
            var migrationRoot = Path.Combine(tempDirectory, "smoke-migration");
            Directory.CreateDirectory(fixtureRoot);
            Directory.CreateDirectory(migrationRoot);
            var fixtureArtifact = Path.Combine(fixtureRoot, "fixture-proof.json");
            var migrationArtifact = Path.Combine(migrationRoot, "migration-proof.json");
            File.WriteAllText(fixtureArtifact, "{\"fixture\":true}\n");
            File.WriteAllText(migrationArtifact, "{\"migration\":true}\n");
            WriteManifest(fixtureRoot, fixtureArtifact, sha256Override: null, fileCountOverride: null);
            WriteManifest(migrationRoot, migrationArtifact, sha256Override: null, fileCountOverride: null);

            var result = RunVerifier("-Root", tempDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.Equal(2, document.RootElement.GetArrayLength());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_smoke_manifest_root_mode_rejects_empty_root()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var result = RunVerifier("-Root", tempDirectory);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("No smoke manifests were found", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string WriteManifest(string outputRoot, string artifactPath, string? sha256Override, int? fileCountOverride)
    {
        var artifactBytes = File.ReadAllBytes(artifactPath);
        var hash = Convert.ToHexString(SHA256.HashData(artifactBytes)).ToLowerInvariant();
        return WriteManifestEntry(
            outputRoot,
            Path.GetRelativePath(outputRoot, artifactPath).Replace('\\', '/'),
            artifactBytes.LongLength,
            sha256Override ?? hash,
            fileCountOverride ?? 1);
    }

    private static string WriteManifestEntry(string outputRoot, string relativePath, long bytes, string sha256, int fileCount)
    {
        var manifestPath = Path.Combine(outputRoot, "smoke-manifest.json");
        var manifest = new
        {
            schema_version = "riftscan.smoke_manifest.v1",
            smoke_name = "fixture",
            output_root = Path.GetFullPath(outputRoot),
            created_utc = DateTimeOffset.UtcNow.ToString("O"),
            file_count = fileCount,
            files = new[]
            {
                new
                {
                    path = relativePath,
                    bytes,
                    sha256
                }
            }
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
        return manifestPath;
    }

    private static (int ExitCode, string Stdout, string Stderr) RunVerifier(string manifestPath)
    {
        return RunVerifier("-ManifestPath", manifestPath);
    }

    private static (int ExitCode, string Stdout, string Stderr) RunVerifier(params string[] arguments)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "verify-smoke-manifest.ps1");
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
            throw new TimeoutException("verify-smoke-manifest.ps1 did not exit within 30 seconds.");
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
