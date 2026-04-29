using System.Text.Json;
using RiftScan.Analysis.Triage;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class GeneratedAnalyzerArtifactSchemaTests
{
    [Fact]
    public void Fixture_generated_analyzer_artifacts_include_schema_versions()
    {
        using var session = CopyFixtureToTemp();

        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);

        AssertJsonLineSchema(session.Path, "triage.jsonl", "riftscan.region_triage_entry.v1");
        AssertJsonLineSchema(session.Path, "structures.jsonl", "riftscan.structure_candidate.v1");
        AssertJsonLineSchema(session.Path, "vec3_candidates.jsonl", "riftscan.vec3_candidate.v1");
        AssertJsonLineSchema(session.Path, "clusters.jsonl", "riftscan.structure_cluster.v1");
        AssertJsonFileSchema(session.Path, "next_capture_plan.json", "riftscan.next_capture_plan.v1");
    }

    [Fact]
    public void Changing_float_generated_analyzer_artifacts_include_schema_versions()
    {
        using var session = new TempDirectory("riftscan-generated-schema-tests");
        _ = new PassiveCaptureService(new ChangingFloatProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 3,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 48
        });

        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);

        AssertJsonLineSchema(session.Path, "deltas.jsonl", "riftscan.region_delta_entry.v1");
        AssertJsonLineSchema(session.Path, "typed_value_candidates.jsonl", "riftscan.typed_value_candidate.v1");
    }

    private static void AssertJsonFileSchema(string sessionPath, string relativePath, string expectedSchemaVersion)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(sessionPath, relativePath)));
        Assert.Equal(expectedSchemaVersion, document.RootElement.GetProperty("schema_version").GetString());
    }

    private static void AssertJsonLineSchema(string sessionPath, string relativePath, string expectedSchemaVersion)
    {
        var line = File.ReadLines(Path.Combine(sessionPath, relativePath)).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        Assert.False(string.IsNullOrWhiteSpace(line), $"{relativePath} should contain at least one JSONL record.");
        using var document = JsonDocument.Parse(line);
        Assert.Equal(expectedSchemaVersion, document.RootElement.GetProperty("schema_version").GetString());
    }

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory("riftscan-generated-schema-fixture-tests");
        CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session"), temp.Path);
        return temp;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
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

    private sealed class ChangingFloatProcessMemoryReader : IProcessMemoryReader
    {
        private int _readCount;

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            BitConverter.GetBytes(1.5f + _readCount++).CopyTo(bytes, 4);
            return bytes;
        }
    }
}
