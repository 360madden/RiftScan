using System.Diagnostics;
using System.Text.Json;

namespace RiftScan.Tests;

public sealed class CiStepInvokerScriptTests
{
    [Fact]
    public void Invoke_ci_step_writes_log_and_status_on_success()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var logPath = Path.Combine(tempDirectory, "probe.log");
            var statusPath = Path.Combine(tempDirectory, "probe-status.json");

            var result = RunCiStepScript(
                "Probe",
                logPath,
                statusPath,
                "pwsh",
                "-NoProfile",
                "-Command",
                "Write-Output 'probe-ok'; exit 0");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            Assert.Contains("probe-ok", result.Stdout, StringComparison.Ordinal);
            Assert.Contains("probe-ok", File.ReadAllText(logPath), StringComparison.Ordinal);

            using var document = JsonDocument.Parse(File.ReadAllText(statusPath));
            var root = document.RootElement;
            Assert.Equal("riftscan.ci_step_status.v1", root.GetProperty("schema_version").GetString());
            Assert.Equal("Probe", root.GetProperty("step").GetString());
            Assert.Equal(0, root.GetProperty("exit_code").GetInt32());
            Assert.True(root.GetProperty("elapsed_ms").GetInt64() >= 0);
            Assert.EndsWith("probe.log", root.GetProperty("log_path").GetString(), StringComparison.Ordinal);
            Assert.Equal("pwsh", root.GetProperty("command")[0].GetString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Invoke_ci_step_preserves_status_when_command_fails()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var logPath = Path.Combine(tempDirectory, "failure.log");
            var statusPath = Path.Combine(tempDirectory, "failure-status.json");

            var result = RunCiStepScript(
                "FailureProbe",
                logPath,
                statusPath,
                "pwsh",
                "-NoProfile",
                "-Command",
                "Write-Output 'probe-failed'; exit 7");

            Assert.NotEqual(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            Assert.Contains("probe-failed", File.ReadAllText(logPath), StringComparison.Ordinal);

            using var document = JsonDocument.Parse(File.ReadAllText(statusPath));
            var root = document.RootElement;
            Assert.Equal("FailureProbe", root.GetProperty("step").GetString());
            Assert.Equal(7, root.GetProperty("exit_code").GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCiStepScript(
        string step,
        string logPath,
        string statusPath,
        params string[] command)
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "invoke-ci-step.ps1");
        var commandLiteral = string.Join(", ", command.Select(ToPowerShellSingleQuotedLiteral));
        var scriptBlock =
            $"& {ToPowerShellSingleQuotedLiteral(scriptPath)} " +
            $"-Step {ToPowerShellSingleQuotedLiteral(step)} " +
            $"-LogPath {ToPowerShellSingleQuotedLiteral(logPath)} " +
            $"-StatusPath {ToPowerShellSingleQuotedLiteral(statusPath)} " +
            $"-Command @({commandLiteral})";

        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepoRoot()
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(scriptBlock);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("invoke-ci-step.ps1 did not exit within 30 seconds.");
        }

        return (process.ExitCode, stdout, stderr);
    }

    private static string ToPowerShellSingleQuotedLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
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
