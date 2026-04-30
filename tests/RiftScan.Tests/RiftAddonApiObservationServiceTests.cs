using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftAddonApiObservationServiceTests
{
    [Fact]
    public void Addon_api_observation_scan_exports_player_and_waypoint_context()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                exportAddon = "ReaderBridgeExport",
                generatedAtRealtime = 104979.671875,
                sourceAddon = "RiftAPI",
                sourceMode = "DirectAPI",
                player = {
                  id = "u035400012FA2D207",
                  name = "Atank",
                  locationName = "Sanctum Watch",
                  zone = "z487C9102D2EA79BE",
                  coord = {
                    x = 7229.0698242188,
                    y = 872.58996582031,
                    z = 3029.1899414062
                  }
                }
              }
            }
            """);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeWaypoint.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                sourceMode = "DirectAPI",
                waypoint = {
                  x = 75.0888671875,
                  z = 292.11111450195
                }
              }
            }
            """);
        var observationsPath = Path.Combine(temp.Path, "addon-api-observations.jsonl");
        var resultPath = Path.Combine(temp.Path, "addon-api-observation-scan.json");

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path,
            JsonlOutputPath = observationsPath
        });
        var cliExitCode = RiftScan.Cli.Program.Main([
            "rift",
            "addon-api-observations",
            temp.Path,
            "--jsonl-out",
            observationsPath,
            "--json-out",
            resultPath
        ]);

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesScanned);
        Assert.Equal(2, result.ObservationCount);
        Assert.Contains(result.Observations, observation =>
            observation.Kind == "current_player" &&
            observation.SourceAddon == "ReaderBridgeExport" &&
            observation.ApiSource == "Inspect.Unit.Detail" &&
            observation.SourceMode == "DirectAPI" &&
            observation.CoordinateSpace == "world_xyz" &&
            observation.UnitName == "Atank");
        Assert.Contains(result.Observations, observation =>
            observation.Kind == "waypoint" &&
            observation.SourceAddon == "ReaderBridgeWaypoint" &&
            observation.SourcePattern == "waypoint_table_xz" &&
            observation.ApiSource == "Inspect.Map.Waypoint.Get" &&
            observation.CoordinateSpace == "map_xz" &&
            observation.WaypointX == 75.0888671875 &&
            observation.WaypointZ == 292.11111450195);
        Assert.True(File.Exists(observationsPath));
        Assert.True(File.Exists(resultPath));
        Assert.Equal(2, File.ReadLines(observationsPath).Count(line => !string.IsNullOrWhiteSpace(line)));
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Addon_api_observation_scan_ignores_tomtom_saved_variables()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "TomTom.lua"),
            """
            TomTomChar = {
              xpos = 75.0888671875,
              ypos = 292.11111450195
            }
            """);

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesScanned);
        Assert.Equal(0, result.ObservationCount);
    }

    [Fact]
    public void Addon_api_observation_scan_exports_ingame_loc_output()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                generatedAtRealtime = 104979.671875,
                sourceMode = "DirectAPI",
                zone = "z487C9102D2EA79BE",
                locationName = "Sanctum Watch",
                loc = {
                  raw = "/loc Sanctum Watch 75.0888671875 292.11111450195",
                  x = 75.0888671875,
                  z = 292.11111450195
                }
              }
            }
            """);

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });

        var observation = Assert.Single(result.Observations);
        Assert.Equal("player_loc", observation.Kind);
        Assert.Equal("ReaderBridgeExport", observation.SourceAddon);
        Assert.Equal("/loc", observation.ApiSource);
        Assert.Equal("game_loc_xz", observation.CoordinateSpace);
        Assert.Equal("ingame_loc_output", observation.ConfidenceLevel);
        Assert.Equal(75.0888671875, observation.LocX);
        Assert.Null(observation.LocY);
        Assert.Equal(292.11111450195, observation.LocZ);
        Assert.Equal("/loc Sanctum Watch 75.0888671875 292.11111450195", observation.RawText);
        Assert.Equal("z487C9102D2EA79BE", observation.ZoneId);
        Assert.Equal("Sanctum Watch", observation.LocationName);
    }

    [Fact]
    public void Addon_api_observation_scan_can_filter_to_fresh_direct_api_sources()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var currentTime = DateTime.Parse("2026-04-29T23:16:01Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var oldTime = DateTime.Parse("2026-04-25T23:16:01Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        WritePlayerState(Path.Combine(temp.Path, "ReaderBridgeExport.lua"), "ReaderBridgeExport", currentTime);
        WritePlayerState(Path.Combine(temp.Path, "AutoFish.lua"), "AutoFish", currentTime);
        WritePlayerState(Path.Combine(temp.Path, "Leader.lua"), "Leader", oldTime);
        var resultPath = Path.Combine(temp.Path, "addon-api-observation-scan-filtered.json");

        var cliExitCode = RiftScan.Cli.Program.Main([
            "rift",
            "addon-api-observations",
            temp.Path,
            "--json-out",
            resultPath,
            "--addon-name",
            "ReaderBridgeExport,AutoFish",
            "--min-file-write-utc",
            "2026-04-29T23:16:00Z"
        ]);

        Assert.Equal(0, cliExitCode);
        var result = JsonSerializer.Deserialize<RiftAddonApiObservationScanResult>(File.ReadAllText(resultPath), SessionJson.Options)
            ?? throw new InvalidOperationException("scan result missing");
        Assert.Equal(2, result.ObservationCount);
        Assert.All(result.Observations, observation => Assert.Contains(observation.SourceAddon, new[] { "ReaderBridgeExport", "AutoFish" }));
        Assert.Contains("addon_api_observations_filtered_by_addon_name", result.Warnings);
        Assert.Contains("addon_api_observations_filtered_by_min_file_write_utc", result.Warnings);
    }

    private static void WritePlayerState(string path, string addonName, DateTime lastWriteUtc)
    {
        File.WriteAllText(
            path,
            $$"""
            State = {
              current = {
                sourceMode = "DirectAPI",
                player = {
                  name = "{{addonName}}Player",
                  coord = {
                    x = 7229.0698242188,
                    y = 872.58996582031,
                    z = 3029.1899414062
                  },
                  zone = "z487C9102D2EA79BE"
                }
              }
            }
            """);
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-addon-api-observations-" + Guid.NewGuid().ToString("N"));
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
