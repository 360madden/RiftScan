using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Triage;

namespace RiftScan.Tests;

public sealed class SessionComparisonServiceTests
{
    [Fact]
    public void Compare_sessions_matches_same_fixture_region_and_cluster()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionA.Path);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionB.Path);

        var result = new SessionComparisonService().Compare(sessionA.Path, sessionB.Path);

        Assert.True(result.Success);
        Assert.Equal("fixture-valid-session", result.SessionAId);
        Assert.Equal("fixture-valid-session", result.SessionBId);
        Assert.True(result.MatchingRegionCount >= 1);
        Assert.Contains(result.RegionMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.True(result.MatchingClusterCount >= 1);
        Assert.Contains(result.ClusterMatches, match => match.BaseAddressHex == "0x10000000");
        Assert.Contains("comparison_is_candidate_evidence_not_truth_claim", result.Warnings);
    }

    [Fact]
    public void Cli_compare_sessions_returns_success()
    {
        using var sessionA = CopyFixtureToTemp();
        using var sessionB = CopyFixtureToTemp();
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var exitCode = RiftScan.Cli.Program.Main(["compare", "sessions", sessionA.Path, sessionB.Path, "--top", "10"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("matching_region_count", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("matching_cluster_count", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session"), temp.Path);
        return temp;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-compare-tests", Guid.NewGuid().ToString("N"));
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
