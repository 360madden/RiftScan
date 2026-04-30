using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftAddonApiTruthSummaryServiceTests
{
    [Fact]
    public void Summary_extracts_latest_player_target_waypoint_loc_and_anchor_truth()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                generatedAtRealtime = 116070.96875,
                sourceMode = "DirectAPI",
                player = {
                  id = "player-id",
                  name = "Atank",
                  locationName = "Sanctum Watch",
                  zone = "z487C9102D2EA79BE",
                  coord = {
                    x = 7237.6196289062,
                    y = 873.46997070312,
                    z = 3051.0598144531
                  }
                },
                target = {
                  id = "target-id",
                  name = "Training Dummy",
                  coord = {
                    x = 7240.5,
                    y = 873.25,
                    z = 3054.75
                  }
                },
                waypoint = {
                  source = "Inspect.Map.Waypoint.Get",
                  unit = "player",
                  x = 7257.6196289062,
                  z = 3051.0598144531
                },
                waypointStatus = {
                  apiAvailable = true,
                  clearApiAvailable = true,
                  hasWaypoint = true,
                  lastCommand = "waypoint-test",
                  lastUpdateAt = 116062.40625,
                  setApiAvailable = true,
                  source = "Inspect.Map.Waypoint.Get",
                  unit = "player",
                  updateCount = 1,
                  x = 7257.6196289062,
                  z = 3051.0598144531
                },
                loc = {
                  raw = "/loc Sanctum Watch 75.0888671875 292.11111450195",
                  x = 75.0888671875,
                  z = 292.11111450195
                }
              }
            }
            """);
        var scan = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });
        var scanPath = Path.Combine(temp.Path, "addon-api-observation-scan.json");
        File.WriteAllText(scanPath, JsonSerializer.Serialize(scan, SessionJson.Options));

        var summary = new RiftAddonApiTruthSummaryService().Summarize(new RiftAddonApiTruthSummaryOptions
        {
            ScanPath = scanPath
        });

        Assert.True(summary.Success);
        Assert.Equal("riftscan.rift_addon_api_truth_summary.v1", summary.ResultSchemaVersion);
        Assert.Equal(5, summary.ObservationCount);
        Assert.Equal(1, summary.WaypointAnchorCount);
        Assert.Equal(6, summary.TruthRecordCount);
        Assert.NotNull(summary.LatestPlayer);
        Assert.Equal(7237.6196289062, summary.LatestPlayer.CoordinateX);
        Assert.Equal(3051.0598144531, summary.LatestPlayer.CoordinateZ);
        Assert.Equal("Atank", summary.LatestPlayer.UnitName);
        Assert.NotNull(summary.LatestTarget);
        Assert.Equal(7240.5, summary.LatestTarget.TargetX);
        Assert.Equal(3054.75, summary.LatestTarget.TargetZ);
        Assert.Equal("Training Dummy", summary.LatestTarget.UnitName);
        Assert.NotNull(summary.LatestWaypoint);
        Assert.Equal(7257.6196289062, summary.LatestWaypoint.WaypointX);
        Assert.Equal(3051.0598144531, summary.LatestWaypoint.WaypointZ);
        Assert.NotNull(summary.LatestWaypointStatus);
        Assert.True(summary.LatestWaypointStatus.WaypointHasWaypoint);
        Assert.Equal("waypoint-test", summary.LatestWaypointStatus.WaypointLastCommand);
        Assert.NotNull(summary.LatestPlayerLoc);
        Assert.Equal(75.0888671875, summary.LatestPlayerLoc.LocX);
        Assert.Equal(292.11111450195, summary.LatestPlayerLoc.LocZ);
        Assert.NotNull(summary.LatestPlayerWaypointAnchor);
        Assert.Equal(20, summary.LatestPlayerWaypointAnchor.DeltaX!.Value, precision: 6);
        Assert.Equal(0, summary.LatestPlayerWaypointAnchor.DeltaZ!.Value, precision: 6);
        Assert.Contains("addon_api_truth_summary_is_snapshot_evidence_not_memory_truth", summary.Warnings);
        Assert.DoesNotContain("no_target_coordinate_truth_observed", summary.Warnings);
    }

    [Fact]
    public void Cli_addon_api_truth_writes_summary_json()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var scanPath = Path.Combine(temp.Path, "scan.json");
        var outputPath = Path.Combine(temp.Path, "truth-summary.json");
        var scan = new RiftAddonApiObservationScanResult
        {
            ObservationCount = 1,
            Observations =
            [
                new()
                {
                    ObservationId = "rift-addon-api-obs-000001",
                    Kind = "current_player",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T04:00:00Z"),
                    ApiSource = "Inspect.Unit.Detail",
                    SourceMode = "DirectAPI",
                    CoordinateSpace = "world_xyz",
                    ConfidenceLevel = "addon_api_direct_savedvariables",
                    CoordX = 7237.6196289062,
                    CoordY = 873.46997070312,
                    CoordZ = 3051.0598144531,
                    EvidenceSummary = "kind=current_player;x=7237.619629;z=3051.059814"
                }
            ]
        };
        File.WriteAllText(scanPath, JsonSerializer.Serialize(scan, SessionJson.Options));
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
                "addon-api-truth",
                scanPath,
                "--out",
                outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(outputPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_addon_api_truth_summary.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("truth_record_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal(Path.GetFullPath(outputPath), fileJson.RootElement.GetProperty("output_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-addon-api-truth-summary-" + Guid.NewGuid().ToString("N"));
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
