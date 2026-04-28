using System.Text.Json;
using RiftScan.Analysis.Regions;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Values;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class KnownSystemRegionClassifierTests
{
    [Fact]
    public void Analyze_session_downranks_known_system_region_and_excludes_it_from_capture_plan()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new MixedSignalProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 2,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 64
        });

        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);

        var triageEntries = ReadJsonLines<RegionTriageEntry>(session.Path, "triage.jsonl");
        var systemEntry = Assert.Single(triageEntries, entry => entry.BaseAddressHex == "0x7FFE0000");
        Assert.Equal(KnownSystemRegionClassifier.TriageRecommendation, systemEntry.Recommendation);
        Assert.True(systemEntry.RankScore <= KnownSystemRegionClassifier.TriageRankScoreCap);
        Assert.Contains(KnownSystemRegionClassifier.Diagnostic, systemEntry.Diagnostics);

        var plan = ReadJson<NextCapturePlan>(session.Path, "next_capture_plan.json");
        Assert.DoesNotContain(plan.Regions, region => region.BaseAddressHex == "0x7FFE0000");
        Assert.Contains(plan.Regions, region => region.BaseAddressHex == "0x2000");
    }

    [Fact]
    public void Typed_value_analysis_caps_known_system_region_candidates()
    {
        using var session = new TempDirectory();
        _ = new PassiveCaptureService(new MixedSignalProcessMemoryReader()).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = session.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 2,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 64
        });

        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);

        var candidates = ReadJsonLines<TypedValueCandidate>(session.Path, "typed_value_candidates.jsonl");
        var systemCandidates = candidates.Where(candidate => candidate.BaseAddressHex == "0x7FFE0000").ToArray();
        Assert.NotEmpty(systemCandidates);
        Assert.All(systemCandidates, candidate =>
        {
            Assert.Equal(KnownSystemRegionClassifier.ValueRecommendation, candidate.Recommendation);
            Assert.True(candidate.RankScore <= KnownSystemRegionClassifier.ValueRankScoreCap);
            Assert.Contains(KnownSystemRegionClassifier.Diagnostic, candidate.Diagnostics);
        });
    }

    private static T ReadJson<T>(string sessionPath, string relativePath) =>
        JsonSerializer.Deserialize<T>(
            File.ReadAllText(Path.Combine(sessionPath, relativePath)),
            SessionJson.Options) ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");

    private static IReadOnlyList<T> ReadJsonLines<T>(string sessionPath, string relativePath) =>
        File.ReadLines(Path.Combine(sessionPath, relativePath))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid {relativePath}."))
            .ToArray();

    private sealed class MixedSignalProcessMemoryReader : IProcessMemoryReader
    {
        private int _gameReadCount;
        private int _systemReadCount;

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
        [
            new VirtualMemoryRegion("region-game", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
            new VirtualMemoryRegion("region-system", 0x7FFE0000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadOnly, MemoryRegionConstants.MemMapped)
        ];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            var bytes = new byte[byteCount];
            if (baseAddress == 0x7FFE0000)
            {
                BitConverter.GetBytes(100 + _systemReadCount++).CopyTo(bytes, 0);
                return bytes;
            }

            BitConverter.GetBytes(1.5f + _gameReadCount++).CopyTo(bytes, 4);
            return bytes;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-system-region-tests", Guid.NewGuid().ToString("N"));
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
