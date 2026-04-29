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
