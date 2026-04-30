using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftWaypointScalarComparisonServiceTests
{
    [Fact]
    public void Compare_marks_prior_scalar_hit_missing_after_waypoint_change()
    {
        using var temp = new TempDirectory();
        var firstAnchorPath = WriteAnchorScan(temp.Path, "anchor-a.json", waypointX: 120, waypointZ: 300, deltaX: 20, deltaZ: 0);
        var secondAnchorPath = WriteAnchorScan(temp.Path, "anchor-b.json", waypointX: 160, waypointZ: 360, deltaX: 60, deltaZ: 60);
        var firstResultPath = WriteScalarResult(
            temp.Path,
            "scalar-a.json",
            firstAnchorPath,
            "session-a",
            [
                BuildHit(axis: "waypoint_z", memoryValue: 300, anchorValue: 300, offsetHex: "0x20")
            ]);
        var secondResultPath = WriteScalarResult(temp.Path, "scalar-b.json", secondAnchorPath, "session-b", []);

        var result = new RiftWaypointScalarComparisonService().Compare(new RiftWaypointScalarComparisonOptions
        {
            InputPaths = [firstResultPath, secondResultPath],
            DeltaTolerance = 0.01,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.InputCount);
        Assert.Equal(1, result.ComparisonCount);
        Assert.Equal(1, result.ClassificationCounts["missing_after_waypoint_change"]);
        var comparison = Assert.Single(result.Comparisons);
        Assert.Equal("rift-waypoint-scalar-comparison-000001", comparison.CandidateId);
        Assert.Equal("waypoint_z", comparison.Axis);
        Assert.Equal("0x20", comparison.SourceOffsetHex);
        Assert.Equal([1], comparison.PresentInputIndexes);
        Assert.Equal([2], comparison.MissingInputIndexes);
        Assert.Equal(60, comparison.WaypointDelta!.Value, precision: 6);
        Assert.Equal("missing_after_waypoint_change", comparison.Classification);
        Assert.Equal("candidate_rejected_missing_after_waypoint_change", comparison.ValidationStatus);
    }

    [Fact]
    public void Compare_marks_repeated_scalar_hit_tracking_waypoint_delta()
    {
        using var temp = new TempDirectory();
        var firstAnchorPath = WriteAnchorScan(temp.Path, "anchor-a.json", waypointX: 120, waypointZ: 300, deltaX: 20, deltaZ: 0);
        var secondAnchorPath = WriteAnchorScan(temp.Path, "anchor-b.json", waypointX: 140, waypointZ: 300, deltaX: 40, deltaZ: 0);
        var firstResultPath = WriteScalarResult(
            temp.Path,
            "scalar-a.json",
            firstAnchorPath,
            "session-a",
            [
                BuildHit(axis: "waypoint_x", memoryValue: 120, anchorValue: 120, offsetHex: "0x24")
            ]);
        var secondResultPath = WriteScalarResult(
            temp.Path,
            "scalar-b.json",
            secondAnchorPath,
            "session-b",
            [
                BuildHit(axis: "waypoint_x", memoryValue: 140, anchorValue: 140, offsetHex: "0x24")
            ]);

        var result = new RiftWaypointScalarComparisonService().Compare(new RiftWaypointScalarComparisonOptions
        {
            InputPaths = [firstResultPath, secondResultPath],
            DeltaTolerance = 0.01,
            Top = 10
        });

        var comparison = Assert.Single(result.Comparisons);
        Assert.Equal("tracks_waypoint_candidate", comparison.Classification);
        Assert.Equal("candidate_supported_by_waypoint_delta", comparison.ValidationStatus);
        Assert.Equal(20, comparison.ObservedDelta!.Value, precision: 6);
        Assert.Equal(20, comparison.WaypointDelta!.Value, precision: 6);
        Assert.Equal(0, comparison.DeltaError!.Value, precision: 6);
    }

    [Fact]
    public void Compare_uses_scalar_hits_output_path_when_embedded_hits_are_truncated()
    {
        using var temp = new TempDirectory();
        var firstAnchorPath = WriteAnchorScan(temp.Path, "anchor-a.json", waypointX: 120, waypointZ: 300, deltaX: 20, deltaZ: 0);
        var secondAnchorPath = WriteAnchorScan(temp.Path, "anchor-b.json", waypointX: 140, waypointZ: 300, deltaX: 40, deltaZ: 0);
        var firstResultPath = WriteScalarResult(
            temp.Path,
            "scalar-a.json",
            firstAnchorPath,
            "session-a",
            [
                BuildHit(axis: "waypoint_x", memoryValue: 120, anchorValue: 120, offsetHex: "0x24")
            ],
            embedHits: false,
            writeScalarHitsOutput: true);
        var secondResultPath = WriteScalarResult(
            temp.Path,
            "scalar-b.json",
            secondAnchorPath,
            "session-b",
            [
                BuildHit(axis: "waypoint_x", memoryValue: 140, anchorValue: 140, offsetHex: "0x24")
            ],
            embedHits: false,
            writeScalarHitsOutput: true);

        var result = new RiftWaypointScalarComparisonService().Compare(new RiftWaypointScalarComparisonOptions
        {
            InputPaths = [firstResultPath, secondResultPath],
            DeltaTolerance = 0.01,
            Top = 10
        });

        Assert.DoesNotContain("one_or_more_inputs_have_truncated_scalar_hit_outputs", result.Warnings);
        Assert.All(result.InputSummaries, summary =>
        {
            Assert.Equal(0, summary.EmittedScalarHitCount);
            Assert.Equal(1, summary.ComparisonScalarHitCount);
            Assert.EndsWith("-hits.jsonl", summary.ScalarHitsOutputPath, StringComparison.OrdinalIgnoreCase);
        });
        var comparison = Assert.Single(result.Comparisons);
        Assert.Equal("tracks_waypoint_candidate", comparison.Classification);
        Assert.Equal(20, comparison.ObservedDelta!.Value, precision: 6);
        Assert.Equal(20, comparison.WaypointDelta!.Value, precision: 6);
    }

    [Fact]
    public void Cli_compare_waypoint_scalars_writes_json_output()
    {
        using var temp = new TempDirectory();
        var firstAnchorPath = WriteAnchorScan(temp.Path, "anchor-a.json", waypointX: 120, waypointZ: 300, deltaX: 20, deltaZ: 0);
        var secondAnchorPath = WriteAnchorScan(temp.Path, "anchor-b.json", waypointX: 160, waypointZ: 360, deltaX: 60, deltaZ: 60);
        var firstResultPath = WriteScalarResult(
            temp.Path,
            "scalar-a.json",
            firstAnchorPath,
            "session-a",
            [
                BuildHit(axis: "waypoint_z", memoryValue: 300, anchorValue: 300, offsetHex: "0x20")
            ]);
        var secondResultPath = WriteScalarResult(temp.Path, "scalar-b.json", secondAnchorPath, "session-b", []);
        var outputPath = Path.Combine(temp.Path, "comparison.json");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main([
                "rift",
                "compare-waypoint-scalars",
                firstResultPath,
                secondResultPath,
                "--delta-tolerance",
                "0.01",
                "--out",
                outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(outputPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_waypoint_scalar_comparison_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("comparison_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal(Path.GetFullPath(outputPath), fileJson.RootElement.GetProperty("output_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteAnchorScan(string directory, string fileName, double waypointX, double waypointZ, double deltaX, double deltaZ)
    {
        var path = Path.Combine(directory, fileName);
        var result = new RiftAddonApiObservationScanResult
        {
            Success = true,
            WaypointAnchorCount = 1,
            WaypointAnchors =
            [
                new RiftAddonWaypointAnchor
                {
                    AnchorId = "rift-addon-waypoint-anchor-000001",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T02:20:53Z"),
                    Realtime = 116070.96875,
                    PlayerObservationId = "rift-addon-api-obs-000001",
                    WaypointObservationId = "rift-addon-api-obs-000002",
                    WaypointStatusObservationId = "rift-addon-api-obs-000003",
                    PlayerX = 100,
                    PlayerY = 200,
                    PlayerZ = 300,
                    WaypointX = waypointX,
                    WaypointZ = waypointZ,
                    DeltaX = deltaX,
                    DeltaZ = deltaZ,
                    HorizontalDistance = Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ)),
                    ZoneId = "zFixture",
                    LocationName = "Fixture"
                }
            ]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static string WriteScalarResult(
        string directory,
        string fileName,
        string anchorPath,
        string sessionId,
        IReadOnlyList<RiftSessionWaypointScalarHit> hits,
        bool embedHits = true,
        bool writeScalarHitsOutput = false)
    {
        var path = Path.Combine(directory, fileName);
        var scalarHitsOutputPath = writeScalarHitsOutput
            ? Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName) + "-hits.jsonl")
            : null;
        if (!string.IsNullOrWhiteSpace(scalarHitsOutputPath))
        {
            File.WriteAllLines(scalarHitsOutputPath, hits.Select(hit => JsonSerializer.Serialize(hit, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        }

        var result = new RiftSessionWaypointScalarMatchResult
        {
            Success = true,
            SessionPath = Path.Combine(directory, sessionId),
            SessionId = sessionId,
            AnchorPath = anchorPath,
            Tolerance = 0.01,
            TopLimit = 100,
            AnchorCount = 1,
            AnchorsUsed = 1,
            ScalarHitCount = hits.Count,
            ScalarAxisHitCounts = CountHitsByAxis(hits),
            RetainedScalarHitCount = hits.Count,
            RetainedScalarAxisHitCounts = CountHitsByAxis(hits),
            PairCandidateCount = 0,
            ScalarHitsOutputPath = scalarHitsOutputPath,
            ScalarHits = embedHits ? hits : []
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static RiftSessionWaypointScalarHit BuildHit(string axis, double memoryValue, double anchorValue, string offsetHex) =>
        new()
        {
            HitId = "rift-waypoint-scalar-hit-000001",
            AnchorId = "rift-addon-waypoint-anchor-000001",
            Axis = axis,
            SnapshotId = "snapshot-000001",
            SourceRegionId = "region-000001",
            SourceBaseAddressHex = "0x60000000",
            SourceOffsetHex = offsetHex,
            SourceAbsoluteAddressHex = "0x60000020",
            MemoryValue = memoryValue,
            AnchorValue = anchorValue,
            AbsDistance = Math.Abs(memoryValue - anchorValue),
            EvidenceSummary = "fixture"
        };

    private static IReadOnlyDictionary<string, int> CountHitsByAxis(IReadOnlyList<RiftSessionWaypointScalarHit> hits) =>
        hits
            .GroupBy(hit => hit.Axis, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-waypoint-scalar-comparison-" + Guid.NewGuid().ToString("N"));
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
