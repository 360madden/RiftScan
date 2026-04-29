using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftAddonCoordinateObservationTests
{
    [Fact]
    public void Addon_coordinate_scan_exports_readerbridge_and_leader_saved_variables()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "ReaderBridgeExport.lua"),
            """
            ReaderBridgeExport_State = {
              snapshot = {
                source = "player",
                player = {
                  coord = {
                    x = 7449.1796875,
                    y = 863.58996582031,
                    z = 2973.0698242188
                  },
                  zone = "z487C9102D2EA79BE"
                }
              }
            }
            """);
        File.WriteAllText(
            Path.Combine(temp.Path, "Leader.lua"),
            """
            LeaderConfig = {
              dump = {
                entries = {
                  {
                    coordX = 7408.1499023438,
                    coordY = 863.58996582031,
                    coordZ = 2978.1298828125,
                    zoneId = "z487C9102D2EA79BE"
                  }
                }
              }
            }
            """);
        var observationsPath = Path.Combine(temp.Path, "addon-coordinate-observations.jsonl");
        var resultPath = Path.Combine(temp.Path, "addon-coordinate-scan.json");

        var result = new RiftAddonCoordinateObservationService().Scan(temp.Path, jsonlOutputPath: observationsPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["rift", "addon-coords", temp.Path, "--jsonl-out", observationsPath, "--json-out", resultPath]);

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesScanned);
        Assert.Equal(2, result.ObservationCount);
        Assert.True(File.Exists(observationsPath));
        Assert.True(File.Exists(resultPath));
        Assert.Contains(result.Observations, observation => observation.AddonName == "ReaderBridgeExport" && observation.SourcePattern == "coord_table_xyz");
        Assert.Contains(result.Observations, observation => observation.AddonName == "Leader" && observation.SourcePattern == "coordX_coordY_coordZ");
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Addon_coordinate_scan_can_filter_current_player_addon_sources()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var currentTime = DateTime.Parse("2026-04-29T23:16:01Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var oldTime = DateTime.Parse("2026-04-25T23:16:01Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        WriteCoordinateFile(Path.Combine(temp.Path, "ReaderBridgeExport.lua"), 7229.07, 872.59, 3029.19, currentTime);
        WriteCoordinateFile(Path.Combine(temp.Path, "AutoFish.lua"), 7229.07, 872.59, 3029.19, currentTime);
        WriteCoordinateFile(Path.Combine(temp.Path, "RiftReaderValidator.lua"), 7222.65, 873.14, 3026.55, currentTime);
        WriteCoordinateFile(Path.Combine(temp.Path, "Leader.lua"), 7408.15, 863.59, 2978.13, oldTime);
        var observationsPath = Path.Combine(temp.Path, "addon-coordinate-observations-filtered.jsonl");
        var resultPath = Path.Combine(temp.Path, "addon-coordinate-scan-filtered.json");

        var cliExitCode = RiftScan.Cli.Program.Main([
            "rift",
            "addon-coords",
            temp.Path,
            "--jsonl-out",
            observationsPath,
            "--json-out",
            resultPath,
            "--addon-name",
            "ReaderBridgeExport,AutoFish",
            "--min-file-write-utc",
            "2026-04-29T23:16:00Z"
        ]);

        Assert.Equal(0, cliExitCode);
        var result = JsonSerializer.Deserialize<RiftAddonCoordinateScanResult>(File.ReadAllText(resultPath), SessionJson.Options)
            ?? throw new InvalidOperationException("scan result missing");
        Assert.Equal(2, result.ObservationCount);
        Assert.All(result.Observations, observation => Assert.Contains(observation.AddonName, new[] { "ReaderBridgeExport", "AutoFish" }));
        Assert.Contains("addon_coordinate_observations_filtered_by_addon_name", result.Warnings);
        Assert.Contains("addon_coordinate_observations_filtered_by_min_file_write_utc", result.Warnings);
        Assert.Equal(2, File.ReadLines(observationsPath).Count(line => !string.IsNullOrWhiteSpace(line)));
    }

    [Fact]
    public void Addon_coordinate_corroboration_matches_vec3_truth_candidate_preview()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var candidatesPath = Path.Combine(temp.Path, "vec3-truth-candidates.jsonl");
        var observationsPath = Path.Combine(temp.Path, "addon-coordinate-observations.jsonl");
        var corroborationPath = Path.Combine(temp.Path, "vec3-truth-corroboration.jsonl");
        var resultPath = Path.Combine(temp.Path, "addon-coordinate-corroboration.json");
        File.WriteAllLines(candidatesPath,
        [
            JsonSerializer.Serialize(new Vec3TruthCandidate
            {
                CandidateId = "vec3-truth-000001",
                BaseAddressHex = "0x1000",
                OffsetHex = "0x20",
                DataType = "vec3_float32",
                Classification = "position_like_vec3_candidate",
                SessionAValueSequenceSummary = "samples=3;delta=0;preview=7449.18|863.59|2973.07",
                SessionBValueSequenceSummary = "samples=3;delta=4;preview=7450.18|863.59|2977.07"
            }, SessionJson.Options).ReplaceLineEndings(string.Empty)
        ]);
        File.WriteAllLines(observationsPath,
        [
            JsonSerializer.Serialize(new RiftAddonCoordinateObservation
            {
                ObservationId = "rift-addon-coord-000001",
                SourceFileName = "ReaderBridgeExport.lua",
                SourcePathRedacted = "ReaderBridgeExport.lua",
                AddonName = "ReaderBridgeExport",
                SourcePattern = "coord_table_xyz",
                CoordX = 7449.1796875,
                CoordY = 863.58996582031,
                CoordZ = 2973.0698242188
            }, SessionJson.Options).ReplaceLineEndings(string.Empty)
        ]);

        var result = new RiftAddonCoordinateCorroborationService().Build(candidatesPath, observationsPath, corroborationPath, tolerance: 1);
        var cliExitCode = RiftScan.Cli.Program.Main([
            "rift", "addon-corroboration",
            "--candidates", candidatesPath,
            "--observations", observationsPath,
            "--out", corroborationPath,
            "--json-out", resultPath,
            "--tolerance", "1"
        ]);
        var verification = new Vec3TruthCorroborationVerifier().Verify(corroborationPath);

        Assert.True(result.Success);
        Assert.Equal(1, result.CorroborationEntryCount);
        Assert.True(verification.Success);
        Assert.Empty(verification.Issues);
        Assert.True(File.Exists(resultPath));
        Assert.Equal(0, cliExitCode);
    }

    private static void WriteCoordinateFile(string path, double x, double y, double z, DateTime lastWriteUtc)
    {
        File.WriteAllText(
            path,
            $$"""
            State = {
              player = {
                coord = {
                  x = {{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                  y = {{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                  z = {{z.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
                },
                zone = "zFixture"
              }
            }
            """);
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-addon-coords-" + Guid.NewGuid().ToString("N"));
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
