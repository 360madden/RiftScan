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
    public void Addon_api_observation_scan_exports_target_focus_and_focus_target_context()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                sourceMode = "DirectAPI",
                target = {
                  id = "target-id",
                  name = "Training Dummy",
                  coord = {
                    x = 7240.5,
                    y = 873.25,
                    z = 3054.75
                  }
                },
                focus = {
                  id = "focus-id",
                  name = "Focus Mob",
                  coord = {
                    x = 7250.25,
                    y = 874.5,
                    z = 3060.75
                  }
                },
                focusTarget = {
                  id = "focus-target-id",
                  name = "Focus Target",
                  coord = {
                    x = 7260.25,
                    y = 875.5,
                    z = 3070.75
                  }
                }
              }
            }
            """);

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });

        Assert.True(result.Success);
        Assert.Equal(3, result.ObservationCount);
        Assert.Contains(result.Observations, observation =>
            observation.Kind == "target" &&
            observation.ApiSource == "Inspect.Unit.Detail" &&
            observation.UnitId == "target-id" &&
            observation.CoordX == 7240.5);
        Assert.Contains(result.Observations, observation =>
            observation.Kind == "focus" &&
            observation.ApiSource == "Inspect.Unit.Detail" &&
            observation.UnitId == "focus-id" &&
            observation.UnitName == "Focus Mob" &&
            observation.CoordZ == 3060.75);
        Assert.Contains(result.Observations, observation =>
            observation.Kind == "focus_target" &&
            observation.ApiSource == "Inspect.Unit.Detail" &&
            observation.UnitId == "focus-target-id" &&
            observation.UnitName == "Focus Target" &&
            observation.CoordZ == 3070.75);
    }

    [Fact]
    public void Addon_api_observation_scan_builds_player_waypoint_anchor_from_same_snapshot()
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
                  id = "u035400012FA2D207",
                  name = "Atank",
                  locationName = "Sanctum Watch",
                  zone = "z487C9102D2EA79BE",
                  coord = {
                    x = 7237.6196289062,
                    y = 873.46997070312,
                    z = 3051.0598144531
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
                }
              }
            }
            """);

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });

        Assert.Equal(3, result.ObservationCount);
        Assert.Equal(1, result.WaypointAnchorCount);
        var anchor = Assert.Single(result.WaypointAnchors);
        Assert.Equal("rift-addon-waypoint-anchor-000001", anchor.AnchorId);
        Assert.Equal("ReaderBridgeExport", anchor.SourceAddon);
        Assert.Equal("rift-addon-api-obs-000001", anchor.PlayerObservationId);
        Assert.Equal("rift-addon-api-obs-000002", anchor.WaypointObservationId);
        Assert.Equal("rift-addon-api-obs-000003", anchor.WaypointStatusObservationId);
        Assert.Equal(7237.6196289062, anchor.PlayerX);
        Assert.Equal(873.46997070312, anchor.PlayerY);
        Assert.Equal(3051.0598144531, anchor.PlayerZ);
        Assert.Equal(7257.6196289062, anchor.WaypointX);
        Assert.Equal(3051.0598144531, anchor.WaypointZ);
        Assert.Equal(20, anchor.DeltaX, precision: 6);
        Assert.Equal(0, anchor.DeltaZ, precision: 6);
        Assert.Equal(20, anchor.HorizontalDistance, precision: 6);
        Assert.Equal("z487C9102D2EA79BE", anchor.ZoneId);
        Assert.Equal("Sanctum Watch", anchor.LocationName);
        Assert.Equal("api_player_to_waypoint_pair", anchor.ConfidenceLevel);
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
    public void Addon_api_observation_scan_exports_waypoint_status_without_active_waypoint()
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
                waypointStatus = {
                  apiAvailable = true,
                  clearApiAvailable = true,
                  hasWaypoint = false,
                  lastCommand = "waypoint-clear",
                  lastUpdateAt = 104970.125,
                  setApiAvailable = true,
                  source = "Inspect.Map.Waypoint.Get",
                  unit = "player",
                  updateCount = 2
                }
              }
            }
            """);

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = temp.Path
        });

        var observation = Assert.Single(result.Observations);
        Assert.Equal("waypoint_status", observation.Kind);
        Assert.Equal("waypoint_status_table", observation.SourcePattern);
        Assert.Equal("Inspect.Map.Waypoint.Get", observation.ApiSource);
        Assert.Equal("DirectAPI", observation.SourceMode);
        Assert.Equal("map_xz_status", observation.CoordinateSpace);
        Assert.Equal("addon_api_status", observation.ConfidenceLevel);
        Assert.True(observation.WaypointApiAvailable);
        Assert.True(observation.WaypointSetApiAvailable);
        Assert.True(observation.WaypointClearApiAvailable);
        Assert.False(observation.WaypointHasWaypoint);
        Assert.Equal(2, observation.WaypointUpdateCount);
        Assert.Equal(104970.125, observation.WaypointLastUpdateAt);
        Assert.Equal("waypoint-clear", observation.WaypointLastCommand);
        Assert.Null(observation.WaypointX);
        Assert.Null(observation.WaypointZ);
        Assert.Contains("has_waypoint=false", observation.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
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
    public void Addon_api_observation_scan_preserves_loc_equivalent_source()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              current = {
                loc = {
                  raw = "/loc-equivalent Tavril Plaza 7331.589844 3053.909912",
                  source = "Inspect.Unit.Detail.coordX_coordZ",
                  x = 7331.58984375,
                  y = 873.35998535156,
                  z = 3053.9099121094
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
        Assert.Equal("Inspect.Unit.Detail.coordX_coordZ", observation.ApiSource);
        Assert.Equal("loc_equivalent_from_api", observation.ConfidenceLevel);
        Assert.Equal(7331.58984375, observation.LocX);
        Assert.Equal(873.35998535156, observation.LocY);
        Assert.Equal(3053.9099121094, observation.LocZ);
        Assert.Equal("/loc-equivalent Tavril Plaza 7331.589844 3053.909912", observation.RawText);
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
