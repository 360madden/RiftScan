using System.Text.Json;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class PassiveCaptureServiceTests
{
    [Fact]
    public void Capture_writes_verifiable_session_from_readable_fixture_region()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader();
        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Equal(1, result.SnapshotsCaptured);
        Assert.Equal(16, result.BytesCaptured);
        Assert.True(File.Exists(Path.Combine(output.Path, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "regions.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "snapshots", "index.jsonl")));

        var verification = new SessionVerifier().Verify(output.Path);
        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
    }

    [Fact]
    public void Capture_can_target_specific_region_ids()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000002", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxRegions = 8,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            RegionIds = new HashSet<string>(["region-000002"], StringComparer.OrdinalIgnoreCase)
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Contains("snapshots/region-000002-sample-000001.bin", result.ArtifactsWritten);
        Assert.DoesNotContain("snapshots/region-000001-sample-000001.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_can_target_specific_base_addresses()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000099", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxRegions = 8,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            BaseAddresses = new HashSet<ulong>([0x2000])
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Contains("snapshots/region-000099-sample-000001.bin", result.ArtifactsWritten);
        Assert.DoesNotContain("snapshots/region-000001-sample-000001.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_writes_optional_stimulus_label()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader();

        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            StimulusLabel = "move_forward",
            StimulusNotes = "fixture label"
        });

        Assert.True(result.Success);
        Assert.Contains("stimuli.jsonl", result.ArtifactsWritten);
        var stimulusLine = File.ReadLines(Path.Combine(output.Path, "stimuli.jsonl")).Single();
        var stimulus = JsonSerializer.Deserialize<StimulusEvent>(stimulusLine, SessionJson.Options)!;
        Assert.Equal("move_forward", stimulus.Label);
        Assert.Equal("snapshot-000001", stimulus.StartSnapshotId);
        Assert.Equal("snapshot-000002", stimulus.EndSnapshotId);

        var checksums = JsonSerializer.Deserialize<ChecksumManifest>(
            File.ReadAllText(Path.Combine(output.Path, "checksums.json")),
            SessionJson.Options)!;
        Assert.Contains(checksums.Entries, entry => entry.Path == "stimuli.jsonl");
    }

    [Fact]
    public void Capture_plan_uses_top_planned_region_ids()
    {
        using var source = new TempDirectory();
        using var output = new TempDirectory();
        Directory.CreateDirectory(source.Path);
        File.WriteAllText(Path.Combine(source.Path, "next_capture_plan.json"), """
            {
              "session_id": "source-session",
              "analyzer_id": "dynamic_region_triage",
              "recommendation": "test",
              "regions": [
                { "region_id": "region-000002", "rank_score": 20, "reason": "test" },
                { "region_id": "region-000001", "rank_score": 10, "reason": "test" }
              ]
            }
            """);
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000002", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCapturePlanService(reader).CaptureFromPlan(new PassiveCapturePlanOptions
        {
            SourceSessionPath = source.Path,
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            TopRegions = 1,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Contains("snapshots/region-000002-sample-000001.bin", result.ArtifactsWritten);
        Assert.DoesNotContain("snapshots/region-000001-sample-000001.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_plan_follows_source_base_addresses_when_region_ids_change()
    {
        using var source = new TempDirectory();
        using var output = new TempDirectory();
        Directory.CreateDirectory(source.Path);
        File.WriteAllText(Path.Combine(source.Path, "next_capture_plan.json"), """
            {
              "session_id": "source-session",
              "analyzer_id": "dynamic_region_triage",
              "recommendation": "test",
              "regions": [
                { "region_id": "region-000002", "rank_score": 20, "reason": "test" }
              ]
            }
            """);
        File.WriteAllText(Path.Combine(source.Path, "regions.json"), """
            {
              "regions": [
                {
                  "region_id": "region-000002",
                  "base_address_hex": "0x2000",
                  "size_bytes": 16,
                  "protection": "PAGE_READWRITE",
                  "state": "MEM_COMMIT",
                  "type": "MEM_PRIVATE"
                }
              ]
            }
            """);
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000099", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCapturePlanService(reader).CaptureFromPlan(new PassiveCapturePlanOptions
        {
            SourceSessionPath = source.Path,
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            TopRegions = 1,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Contains("snapshots/region-000099-sample-000001.bin", result.ArtifactsWritten);
        Assert.DoesNotContain("snapshots/region-000001-sample-000001.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_plan_can_read_comparison_next_capture_plan_file()
    {
        using var output = new TempDirectory();
        var planPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "comparison-next-capture-plan.json");
        using var planDocument = JsonDocument.Parse(File.ReadAllText(planPath));
        Assert.Equal("comparison_next_capture_plan.v1", planDocument.RootElement.GetProperty("schema_version").GetString());
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000099", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCapturePlanService(reader).CaptureFromPlan(new PassiveCapturePlanOptions
        {
            SourceSessionPath = planPath,
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            TopRegions = 1,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Contains("snapshots/region-000099-sample-000001.bin", result.ArtifactsWritten);
        Assert.DoesNotContain("snapshots/region-000001-sample-000001.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_plan_rejects_unsupported_comparison_next_capture_plan_schema()
    {
        using var source = new TempDirectory();
        using var output = new TempDirectory();
        Directory.CreateDirectory(source.Path);
        var planPath = Path.Combine(source.Path, "bad-comparison-next-capture-plan.json");
        File.WriteAllText(planPath, """
            {
              "schema_version": "comparison_next_capture_plan.v999",
              "target_region_priorities": [
                {
                  "base_address_hex": "0x2000",
                  "priority_score": 85,
                  "reason": "bad_schema_fixture"
                }
              ]
            }
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => new PassiveCapturePlanService(new FakeProcessMemoryReader()).CaptureFromPlan(new PassiveCapturePlanOptions
        {
            SourceSessionPath = planPath,
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            TopRegions = 1,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        }));

        Assert.Contains("Unsupported comparison capture plan schema_version", ex.Message, StringComparison.Ordinal);
        Assert.Contains("comparison_next_capture_plan.v1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Capture_refuses_to_choose_between_duplicate_process_names()
    {
        using var output = new TempDirectory();
        var reader = new FakeProcessMemoryReader
        {
            Processes =
            [
                new ProcessDescriptor(100, "fixture_process", DateTimeOffset.UtcNow, null),
                new ProcessDescriptor(101, "fixture_process", DateTimeOffset.UtcNow, null)
            ]
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path
        }));
        Assert.Contains("Use --pid", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Capture_waits_for_process_return_then_resumes_capture()
    {
        using var output = new TempDirectory();
        var process = new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");
        var processLookupAttempts = 0;
        var readAttempts = 0;

        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion(
                    "region-000001",
                    0x1000,
                    16,
                    MemoryRegionConstants.MemCommit,
                    MemoryRegionConstants.PageReadWrite,
                    MemoryRegionConstants.MemPrivate)
            ],
            Processes = [process],
            FindProcessesByNameFunc = processName =>
            {
                if (processLookupAttempts == 0)
                {
                    processLookupAttempts++;
                    return [process];
                }

                processLookupAttempts++;
                return processLookupAttempts >= 3 ? [process] : [];
            },
            ReadMemoryFunc = (_, _, byteCount) =>
            {
                readAttempts++;
                return readAttempts == 1
                    ? throw new InvalidOperationException("process unavailable")
                    : Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray();
            }
        };

        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessName = "fixture_process",
            OutputPath = output.Path,
            Samples = 2,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            InterventionWaitMilliseconds = 2000,
            InterventionPollIntervalMilliseconds = 50
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.RegionsCaptured);
        Assert.Equal(1, result.SnapshotsCaptured);
        Assert.Contains("snapshots/region-000001-sample-000002.bin", result.ArtifactsWritten);
    }

    [Fact]
    public void Capture_times_out_waiting_for_process_and_writes_intervention_handoff()
    {
        using var output = new TempDirectory();
        var process = new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe");
        var getProcessByIdAttempts = 0;

        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion(
                    "region-000001",
                    0x1000,
                    16,
                    MemoryRegionConstants.MemCommit,
                    MemoryRegionConstants.PageReadWrite,
                    MemoryRegionConstants.MemPrivate)
            ],
            Processes = [process],
            FindProcessesByNameFunc = _ => [],
            ReadMemoryFunc = (_, _, _) => throw new InvalidOperationException("process unavailable"),
            GetProcessByIdFunc = _ =>
            {
                getProcessByIdAttempts++;
                return getProcessByIdAttempts == 1
                    ? process
                    : throw new InvalidOperationException("process unavailable");
            }
        };

        var result = new PassiveCaptureService(reader).Capture(new PassiveCaptureOptions
        {
            ProcessId = 100,
            OutputPath = output.Path,
            Samples = 1,
            IntervalMilliseconds = 0,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16,
            InterventionWaitMilliseconds = 250,
            InterventionPollIntervalMilliseconds = 50
        });

        Assert.False(result.Success);
        Assert.Equal(0, result.SnapshotsCaptured);
        Assert.Contains("intervention_handoff.json", result.ArtifactsWritten);
        var handoff = JsonSerializer.Deserialize<CaptureInterventionHandoff>(File.ReadAllText(Path.Combine(output.Path, "intervention_handoff.json")), SessionJson.Options)!;
        Assert.Equal("no_snapshot_data_before_intervention_timeout", handoff.Reason);
        Assert.Equal(0, handoff.SnapshotCount);
        Assert.Equal(1, handoff.SamplesTargeted);
        Assert.Equal(100, handoff.ProcessId);
        Assert.False(File.Exists(Path.Combine(output.Path, "manifest.json")));
    }

    private sealed class FakeProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> Processes { get; init; } =
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public IReadOnlyList<VirtualMemoryRegion> Regions { get; init; } =
        [
            new VirtualMemoryRegion(
                "region-000001",
                0x1000,
                16,
                MemoryRegionConstants.MemCommit,
                MemoryRegionConstants.PageReadWrite,
                MemoryRegionConstants.MemPrivate)
        ];

        public Func<string, IReadOnlyList<ProcessDescriptor>>? FindProcessesByNameFunc { get; init; }

        public Func<int, ProcessDescriptor>? GetProcessByIdFunc { get; init; }

        public Func<int, IReadOnlyList<VirtualMemoryRegion>>? EnumerateRegionsFunc { get; init; }

        public Func<int, ulong, int, byte[]>? ReadMemoryFunc { get; init; }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
            FindProcessesByNameFunc?.Invoke(processName)
            ?? Processes.Where(process => string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)).ToArray();

        public ProcessDescriptor GetProcessById(int processId) =>
            GetProcessByIdFunc?.Invoke(processId)
            ?? Processes.Single(process => process.ProcessId == processId);

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) =>
            EnumerateRegionsFunc?.Invoke(processId) ?? Regions;

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount) =>
            ReadMemoryFunc?.Invoke(processId, baseAddress, byteCount)
            ?? Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-capture-tests", Guid.NewGuid().ToString("N"));
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
