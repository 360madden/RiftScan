using System.Text.Json;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class PassiveCaptureDryRunServiceTests
{
    [Fact]
    public void Dry_run_inventories_regions_without_reading_memory()
    {
        var readCalled = false;
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000002", 0x2000, 32, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadOnly, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000003", 0x3000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageNoAccess, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000004", 0x4000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemImage)
            ],
            ReadMemoryFunc = (_, _, _) =>
            {
                readCalled = true;
                return [];
            }
        };

        var result = new PassiveCaptureDryRunService(reader).Inspect(new PassiveCaptureDryRunOptions
        {
            ProcessName = "fixture_process",
            Samples = 3,
            MaxRegions = 2,
            MaxBytesPerRegion = 64,
            MaxTotalBytes = 64
        });

        Assert.True(result.Success);
        Assert.False(readCalled);
        Assert.Equal(4, result.TotalRegionCount);
        Assert.Equal(2, result.SelectedRegionCount);
        Assert.Equal(48, result.EstimatedBytesPerSample);
        Assert.Equal(144, result.EstimatedTotalBytes);
        Assert.Equal("dry_run_only_no_process_memory_was_read", result.Warnings[0]);

        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-000001" &&
            region.Selected &&
            region.SelectedOrder == 1 &&
            region.EstimatedReadBytes == 16);
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-000002" &&
            region.Selected &&
            region.SelectedOrder == 2 &&
            region.EstimatedReadBytes == 32);
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-000003" &&
            !region.Selected &&
            region.SkipReasons.Contains("unreadable_protection"));
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-000004" &&
            !region.Selected &&
            region.SkipReasons.Contains("image_region_excluded"));
    }

    [Fact]
    public void Dry_run_honors_region_and_byte_budgets()
    {
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000002", 0x2000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000003", 0x3000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCaptureDryRunService(reader).Inspect(new PassiveCaptureDryRunOptions
        {
            ProcessName = "fixture_process",
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.SelectedRegionCount);
        Assert.Equal(16, result.EstimatedBytesPerSample);
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-000002" &&
            !region.Selected &&
            region.Reason == "skipped_after_max_regions");
    }

    [Fact]
    public void Dry_run_default_selection_includes_large_writable_regions_with_read_cap()
    {
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-readonly-small", 0x1000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadOnly, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-writable-large", 0x2000, 4096, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCaptureDryRunService(reader).Inspect(new PassiveCaptureDryRunOptions
        {
            ProcessName = "fixture_process",
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.SelectedRegionCount);
        Assert.Contains("some_selected_regions_read_capped_to_max_bytes_per_region", result.Warnings);
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-writable-large" &&
            region.Selected &&
            region.SelectedOrder == 1 &&
            region.EstimatedReadBytes == 16 &&
            region.SkipReasons.Count == 0);
        Assert.Contains(result.Regions, region =>
            region.RegionId == "region-readonly-small" &&
            !region.Selected &&
            region.Reason == "skipped_after_max_regions");
    }

    [Fact]
    public void Dry_run_truncates_reported_regions_but_preserves_counts()
    {
        var reader = new FakeProcessMemoryReader
        {
            Regions =
            [
                new VirtualMemoryRegion("region-000001", 0x1000, 16, 0x2000, MemoryRegionConstants.PageNoAccess, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000002", 0x2000, 16, 0x2000, MemoryRegionConstants.PageNoAccess, MemoryRegionConstants.MemPrivate),
                new VirtualMemoryRegion("region-000003", 0x3000, 16, MemoryRegionConstants.MemCommit, MemoryRegionConstants.PageReadWrite, MemoryRegionConstants.MemPrivate)
            ]
        };

        var result = new PassiveCaptureDryRunService(reader).Inspect(new PassiveCaptureDryRunOptions
        {
            ProcessName = "fixture_process",
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 16,
            RegionOutputLimit = 1
        });

        Assert.True(result.Success);
        Assert.Equal(3, result.TotalRegionCount);
        Assert.Equal(2, result.ReportedRegionCount);
        Assert.True(result.RegionOutputTruncated);
        Assert.Contains("region_output_truncated_use_all_regions_for_full_inventory", result.Warnings);
        Assert.Contains(result.Regions, region => region.RegionId == "region-000001");
        Assert.Contains(result.Regions, region => region.RegionId == "region-000003" && region.Selected);
    }

    [Fact]
    public void Cli_capture_passive_dry_run_writes_json_output()
    {
        using var output = new TempDirectory();
        var outputPath = Path.Combine(output.Path, "dry-run.json");
        var originalOut = Console.Out;
        using var stdout = new StringWriter();

        try
        {
            Console.SetOut(stdout);
            var exitCode = RiftScan.Cli.Program.Main(
            [
                "capture",
                "passive",
                "--dry-run",
                "--process",
                $"missing_process_{Guid.NewGuid():N}",
                "--json-out",
                outputPath
            ]);

            Assert.Equal(2, exitCode);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Dry_run_result_pins_json_contract_fields()
    {
        var result = new PassiveCaptureDryRunResult
        {
            Success = true,
            ProcessId = 100,
            ProcessName = "fixture_process",
            Samples = 2,
            MaxRegions = 1,
            MaxBytesPerRegion = 16,
            MaxTotalBytes = 32,
            RegionOutputLimit = 250,
            TotalRegionCount = 1,
            ReportedRegionCount = 1,
            RegionOutputTruncated = false,
            CandidateRegionCount = 1,
            SelectedRegionCount = 1,
            SkippedRegionCount = 0,
            EstimatedBytesPerSample = 16,
            EstimatedTotalBytes = 32,
            Regions =
            [
                new PassiveCaptureDryRunRegion
                {
                    RegionId = "region-000001",
                    BaseAddressHex = "0x1000",
                    SizeBytes = 16,
                    State = "MEM_COMMIT",
                    Protection = "PAGE_READWRITE",
                    Type = "MEM_PRIVATE",
                    Selected = true,
                    SelectedOrder = 1,
                    EstimatedReadBytes = 16,
                    Reason = "selected_for_passive_capture"
                }
            ],
            Warnings = ["dry_run_only_no_process_memory_was_read"]
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, SessionJson.Options));
        var root = document.RootElement;
        var region = root.GetProperty("regions")[0];

        Assert.Equal("riftscan.passive_capture_dry_run_result.v1", root.GetProperty("result_schema_version").GetString());
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.TryGetProperty("estimated_total_bytes", out _));
        Assert.True(root.TryGetProperty("reported_region_count", out _));
        Assert.True(root.TryGetProperty("region_output_truncated", out _));
        Assert.True(region.TryGetProperty("selected_order", out _));
        Assert.True(region.TryGetProperty("skip_reasons", out _));
    }

    private sealed class FakeProcessMemoryReader : IProcessMemoryReader
    {
        public IReadOnlyList<ProcessDescriptor> Processes { get; init; } =
        [
            new ProcessDescriptor(100, "fixture_process", DateTimeOffset.Parse("2026-04-28T17:00:00Z"), "fixture.exe")
        ];

        public IReadOnlyList<VirtualMemoryRegion> Regions { get; init; } = [];

        public Func<int, ulong, int, byte[]>? ReadMemoryFunc { get; init; }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
            Processes.Where(process => string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)).ToArray();

        public ProcessDescriptor GetProcessById(int processId) =>
            Processes.Single(process => process.ProcessId == processId);

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) => Regions;

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount) =>
            ReadMemoryFunc?.Invoke(processId, baseAddress, byteCount) ?? throw new InvalidOperationException("Dry-run tests should not read memory.");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-dry-run-tests", Guid.NewGuid().ToString("N"));
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
