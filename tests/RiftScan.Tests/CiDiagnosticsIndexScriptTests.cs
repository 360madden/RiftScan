using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class CiDiagnosticsIndexScriptTests
{
    [Fact]
    public void Write_ci_diagnostics_index_lists_files_with_hashes()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            var runInfoPath = Path.Combine(diagnosticsRoot, "run-info.json");
            var logPath = Path.Combine(diagnosticsRoot, "dotnet-test.log");
            File.WriteAllText(runInfoPath, "{\"ok\":true}\n");
            File.WriteAllText(logPath, "test output\n");

            var result = RunIndexScript("-Root", diagnosticsRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);

            var indexPath = Path.Combine(diagnosticsRoot, "index.json");
            using var document = JsonDocument.Parse(File.ReadAllText(indexPath));
            var root = document.RootElement;
            Assert.Equal("riftscan.ci_diagnostics_index.v1", root.GetProperty("schema_version").GetString());
            Assert.Equal(2, root.GetProperty("file_count").GetInt32());
            Assert.Equal(new FileInfo(runInfoPath).Length + new FileInfo(logPath).Length, root.GetProperty("total_bytes").GetInt64());

            var files = root.GetProperty("files").EnumerateArray().ToArray();
            Assert.Equal("dotnet-test.log", files[0].GetProperty("path").GetString());
            Assert.Equal("run-info.json", files[1].GetProperty("path").GetString());
            Assert.Equal(Sha256Lower(logPath), files[0].GetProperty("sha256").GetString());
            Assert.Equal(Sha256Lower(runInfoPath), files[1].GetProperty("sha256").GetString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_ci_diagnostics_index_excludes_existing_index_file()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "ci-diagnostics");
            Directory.CreateDirectory(diagnosticsRoot);
            File.WriteAllText(Path.Combine(diagnosticsRoot, "dotnet-build.log"), "build output\n");
            File.WriteAllText(Path.Combine(diagnosticsRoot, "index.json"), "{\"stale\":true}\n");

            var result = RunIndexScript("-Root", diagnosticsRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);

            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(diagnosticsRoot, "index.json")));
            var files = document.RootElement.GetProperty("files").EnumerateArray().ToArray();
            Assert.Single(files);
            Assert.Equal("dotnet-build.log", files[0].GetProperty("path").GetString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_ci_diagnostics_index_creates_empty_root()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var diagnosticsRoot = Path.Combine(tempDirectory, "missing-diagnostics");

            var result = RunIndexScript("-Root", diagnosticsRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            Assert.True(File.Exists(Path.Combine(diagnosticsRoot, "index.json")));

            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(diagnosticsRoot, "index.json")));
            Assert.Equal(0, document.RootElement.GetProperty("file_count").GetInt32());
            Assert.Equal(0, document.RootElement.GetProperty("total_bytes").GetInt64());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string Sha256Lower(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

    private static (int ExitCode, string Stdout, string Stderr) RunIndexScript(params string[] arguments)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "write-ci-diagnostics-index.ps1");
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
            throw new TimeoutException("write-ci-diagnostics-index.ps1 did not exit within 30 seconds.");
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
