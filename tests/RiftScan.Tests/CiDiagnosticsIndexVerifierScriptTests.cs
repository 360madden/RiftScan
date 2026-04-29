using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class CiDiagnosticsIndexVerifierScriptTests
{
    [Fact]
    public void Verify_ci_diagnostics_index_accepts_valid_index()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "run-info.json"), "{\"ok\":true}\n");
            File.WriteAllText(Path.Combine(diagnosticsRoot, "dotnet-test.log"), "test output\n");
            WriteIndex(diagnosticsRoot, recordedRoot: @"D:\a\RiftScan\RiftScan\artifacts\ci-diagnostics");

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("riftscan.ci_diagnostics_index_verification.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(2, document.RootElement.GetProperty("file_count").GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_diagnostics_index_rejects_file_count_mismatch()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "run-info.json"), "{\"ok\":true}\n");
            WriteIndex(diagnosticsRoot, fileCountOverride: 2);

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("file_count mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_diagnostics_index_rejects_total_bytes_mismatch()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "dotnet-build.log"), "build output\n");
            WriteIndex(diagnosticsRoot, totalBytesOverride: 1);

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("total_bytes mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_diagnostics_index_rejects_bad_sha256()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "dotnet-test.log"), "test output\n");
            WriteIndex(diagnosticsRoot, entryMutation: entry => entry["sha256"] = new string('0', 64));

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("SHA256 mismatch", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_diagnostics_index_rejects_malformed_sha256()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "dotnet-test.log"), "test output\n");
            WriteIndex(diagnosticsRoot, entryMutation: entry => entry["sha256"] = "BADHASH");

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("SHA256 must be 64 lowercase hex characters", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Verify_ci_diagnostics_index_rejects_path_escape()
    {
        var tempDirectory = CreateTempDirectory();
        var outsideFile = Path.Combine(Path.GetTempPath(), "riftscan-tests", $"outside-{Guid.NewGuid():N}.log");
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(outsideFile)!);
            File.WriteAllText(outsideFile, "escape\n");
            WriteIndex(diagnosticsRoot, extraEntry: outsideFile);

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("escapes diagnostics root", result.Stderr, StringComparison.OrdinalIgnoreCase);
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
    public void Verify_ci_diagnostics_index_rejects_self_reference()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            WriteIndex(diagnosticsRoot, includeSelfReference: true);

            var result = RunVerifier("-Root", diagnosticsRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("must not list itself", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void WriteIndex(
        string diagnosticsRoot,
        string? recordedRoot = null,
        int? fileCountOverride = null,
        long? totalBytesOverride = null,
        Action<Dictionary<string, object?>>? entryMutation = null,
        string? extraEntry = null,
        bool includeSelfReference = false)
    {
        var entries = Directory.GetFiles(diagnosticsRoot)
            .Where(path => !string.Equals(Path.GetFileName(path), "index.json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(path => BuildEntry(diagnosticsRoot, path))
            .ToList();

        if (extraEntry is not null)
        {
            entries.Add(BuildEntry(diagnosticsRoot, extraEntry));
        }

        if (includeSelfReference)
        {
            var indexPath = Path.Combine(diagnosticsRoot, "index.json");
            File.WriteAllText(indexPath, "{}");
            entries.Add(BuildEntry(diagnosticsRoot, indexPath));
        }

        if (entries.Count > 0)
        {
            entryMutation?.Invoke(entries[0]);
        }

        var totalBytes = entries.Sum(entry => Convert.ToInt64(entry["bytes"]));
        var index = new Dictionary<string, object?>
        {
            ["schema_version"] = "riftscan.ci_diagnostics_index.v1",
            ["created_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["root"] = recordedRoot ?? Path.GetFullPath(diagnosticsRoot),
            ["file_count"] = fileCountOverride ?? entries.Count,
            ["total_bytes"] = totalBytesOverride ?? totalBytes,
            ["files"] = entries
        };

        File.WriteAllText(Path.Combine(diagnosticsRoot, "index.json"), JsonSerializer.Serialize(index));
    }

    private static Dictionary<string, object?> BuildEntry(string diagnosticsRoot, string path)
    {
        return new Dictionary<string, object?>
        {
            ["path"] = Path.GetRelativePath(diagnosticsRoot, path).Replace('\\', '/'),
            ["bytes"] = new FileInfo(path).Length,
            ["sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
        };
    }

    private static (int ExitCode, string Stdout, string Stderr) RunVerifier(params string[] arguments)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "verify-ci-diagnostics-index.ps1");
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
            throw new TimeoutException("verify-ci-diagnostics-index.ps1 did not exit within 30 seconds.");
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
