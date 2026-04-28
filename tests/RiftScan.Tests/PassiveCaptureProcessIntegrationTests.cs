using System.Diagnostics;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class PassiveCaptureProcessIntegrationTests
{
    [Fact]
    public async Task Capture_with_pid_and_process_name_recovers_after_real_process_restart()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var output = new TempDirectory();
        using var ready = new TempDirectory();
        var fixturePath = ResolveFixturePath();
        using var oldProcess = StartFixture(fixturePath, Path.Combine(ready.Path, "old.ready"), exitAfterMilliseconds: 2_500);
        var fixtureProcessName = oldProcess.ProcessName;
        var oldProcessId = oldProcess.Id;
        Process? newProcess = null;

        try
        {
            var resultTask = Task.Run(() => new PassiveCaptureService(new WindowsProcessMemoryReader()).Capture(new PassiveCaptureOptions
            {
                ProcessId = oldProcessId,
                ProcessName = fixtureProcessName,
                OutputPath = output.Path,
                Samples = 2,
                IntervalMilliseconds = 5_000,
                MaxRegions = 1,
                MaxBytesPerRegion = 1024 * 1024,
                MaxTotalBytes = 2 * 1024 * 1024,
                InterventionWaitMilliseconds = 5_000,
                InterventionPollIntervalMilliseconds = 250
            }));

            Assert.True(WaitForSnapshot(output.Path, TimeSpan.FromSeconds(5)), "Timed out waiting for the first fixture-process snapshot.");
            Assert.False(resultTask.IsCompleted, "Passive capture completed before fixture-process restart could be exercised.");
            Assert.True(oldProcess.WaitForExit(5_000), "Old fixture process did not exit before restart.");
            Assert.Throws<ArgumentException>(() => Process.GetProcessById(oldProcessId));
            oldProcess.Dispose();
            newProcess = StartFixture(fixturePath, Path.Combine(ready.Path, "new.ready"));
            Assert.Contains(new WindowsProcessMemoryReader().FindProcessesByName(fixtureProcessName), process => process.ProcessId == newProcess.Id);

            var completedTask = await Task.WhenAny(resultTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(resultTask, completedTask);
            var result = await resultTask;

            Assert.True(
                result.Success,
                $"Capture failed; pid={result.ProcessId}; snapshots={result.SnapshotsCaptured}; bytes={result.BytesCaptured}; handoff={result.HandoffPath}; artifacts={string.Join(',', result.ArtifactsWritten)}");
            Assert.True(
                result.ProcessId == newProcess.Id,
                $"Expected restored PID {newProcess.Id}, got {result.ProcessId}; snapshots={result.SnapshotsCaptured}; bytes={result.BytesCaptured}; artifacts={string.Join(',', result.ArtifactsWritten)}");
            Assert.True(result.SnapshotsCaptured >= 2);
            Assert.Null(result.HandoffPath);

            var manifest = new SessionVerifier().Verify(output.Path);
            Assert.True(manifest.Success, string.Join(Environment.NewLine, manifest.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        }
        finally
        {
            if (newProcess is not null)
            {
                StopProcess(newProcess);
                newProcess.Dispose();
            }

            StopProcess(oldProcess);
        }
    }

    [Fact]
    public void Windows_reader_finds_dotted_process_name_and_exe_name()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var ready = new TempDirectory();
        var fixturePath = ResolveFixturePath();
        using var process = StartFixture(fixturePath, Path.Combine(ready.Path, "lookup.ready"));

        try
        {
            var reader = new WindowsProcessMemoryReader();
            Assert.Contains(reader.FindProcessesByName(process.ProcessName), match => match.ProcessId == process.Id);
            Assert.Contains(reader.FindProcessesByName($"{process.ProcessName}.exe"), match => match.ProcessId == process.Id);
            Assert.Contains(reader.FindProcessesByName(Path.GetFileName(fixturePath)), match => match.ProcessId == process.Id);
        }
        finally
        {
            StopProcess(process);
        }
    }

    private static Process StartFixture(string fixturePath, string readyFile, int? exitAfterMilliseconds = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fixturePath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--ready-file");
        startInfo.ArgumentList.Add(readyFile);
        if (exitAfterMilliseconds is { } milliseconds)
        {
            startInfo.ArgumentList.Add("--exit-after-ms");
            startInfo.ArgumentList.Add(milliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start fixture process: {fixturePath}");

        if (!WaitForFile(readyFile, TimeSpan.FromSeconds(5)))
        {
            StopProcess(process);
            throw new InvalidOperationException($"Fixture process did not write readiness file: {readyFile}");
        }

        var fixtureProcessId = int.Parse(File.ReadAllText(readyFile), System.Globalization.CultureInfo.InvariantCulture);
        if (fixtureProcessId == process.Id)
        {
            return process;
        }

        process.Dispose();
        return Process.GetProcessById(fixtureProcessId);
    }

    private static string ResolveFixturePath()
    {
        var extension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var configuration = Directory.GetParent(baseDirectory)?.Name ?? "Release";
        var path = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "RiftScan.ProcessFixture",
            "bin",
            configuration,
            "net10.0",
            $"RiftScan.ProcessFixture{extension}"));

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("RiftScan process fixture executable was not built.", path);
        }

        return path;
    }

    private static bool WaitForSnapshot(string sessionPath, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var snapshotsPath = Path.Combine(sessionPath, "snapshots");
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (Directory.Exists(snapshotsPath) && Directory.EnumerateFiles(snapshotsPath, "*.bin").Any())
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    private static bool WaitForFile(string path, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    private static void StopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-process-integration-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
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
