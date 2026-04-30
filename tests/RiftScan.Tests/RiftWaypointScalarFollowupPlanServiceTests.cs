using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftWaypointScalarFollowupPlanServiceTests
{
    [Fact]
    public void Plan_extracts_unique_pair_base_addresses_for_targeted_capture()
    {
        using var temp = new TempDirectory();
        var matchPath = WriteScalarMatch(temp.Path);

        var result = new RiftWaypointScalarFollowupPlanService().Plan(new RiftWaypointScalarFollowupPlanOptions
        {
            ScalarMatchPath = matchPath,
            TopPairs = 2,
            Samples = 8,
            IntervalMilliseconds = 100,
            MaxBytesPerRegion = 65536
        });

        Assert.True(result.Success);
        Assert.Equal("riftscan.rift_waypoint_scalar_followup_plan.v1", result.ResultSchemaVersion);
        Assert.Equal("fixture-session", result.SourceSessionId);
        Assert.Equal(2, result.SelectedPairCandidateCount);
        Assert.Equal(["0x60000000", "0x70000000", "0x80000000"], result.BaseAddresses);
        Assert.Equal(3, result.MaxRegions);
        Assert.Equal(1572864, result.MaxTotalBytes);
        Assert.Contains("--base-addresses", result.RecommendedCaptureArgs);
        Assert.Contains("0x60000000,0x70000000,0x80000000", result.RecommendedCaptureArgs);
        Assert.Contains("change_waypoint_before_running_recommended_capture", result.Diagnostics);
    }

    [Fact]
    public void Cli_plan_waypoint_scalar_followup_writes_json_output()
    {
        using var temp = new TempDirectory();
        var matchPath = WriteScalarMatch(temp.Path);
        var outputPath = Path.Combine(temp.Path, "followup-plan.json");
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
                "plan-waypoint-scalar-followup",
                matchPath,
                "--top-pairs",
                "1",
                "--out",
                outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(outputPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_waypoint_scalar_followup_plan.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("selected_pair_candidate_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal(Path.GetFullPath(outputPath), fileJson.RootElement.GetProperty("output_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteScalarMatch(string directory)
    {
        var path = Path.Combine(directory, "scalar-match.json");
        var result = new RiftSessionWaypointScalarMatchResult
        {
            Success = true,
            SessionPath = Path.Combine(directory, "session"),
            SessionId = "fixture-session",
            PairCandidateCount = 2,
            PairCandidates =
            [
                BuildPair("candidate-a", "0x70000000", "0x20", "0x60000000", "0x40", 8),
                BuildPair("candidate-b", "0x80000000", "0x60", "0x70000000", "0x80", 4)
            ]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static RiftSessionWaypointScalarPairCandidate BuildPair(
        string candidateId,
        string xBase,
        string xOffset,
        string zBase,
        string zOffset,
        int support) =>
        new()
        {
            CandidateId = candidateId,
            XSourceBaseAddressHex = xBase,
            XSourceOffsetHex = xOffset,
            ZSourceBaseAddressHex = zBase,
            ZSourceOffsetHex = zOffset,
            SupportCount = support,
            AnchorSupportCount = 1,
            BestDistanceTotal = 1.5,
            ValidationStatus = "waypoint_scalar_pair_supported"
        };

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-waypoint-scalar-followup-" + Guid.NewGuid().ToString("N"));
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
