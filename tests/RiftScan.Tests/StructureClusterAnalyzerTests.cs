using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class StructureClusterAnalyzerTests
{
    [Fact]
    public void Analyze_session_writes_clusters_from_adjacent_structure_candidates()
    {
        using var session = CopyFixtureToTemp();
        _ = new FloatTripletStructureAnalyzer().AnalyzeSession(session.Path);

        var clusters = new StructureClusterAnalyzer().AnalyzeSession(session.Path);

        Assert.NotEmpty(clusters);
        Assert.True(File.Exists(Path.Combine(session.Path, "clusters.jsonl")));
        Assert.Contains(clusters, cluster => cluster.RegionId == "region-0001" && cluster.CandidateCount >= 2);

        var firstLine = File.ReadLines(Path.Combine(session.Path, "clusters.jsonl")).First();
        var first = JsonSerializer.Deserialize<StructureCluster>(firstLine, SessionJson.Options)!;
        Assert.StartsWith("cluster-", first.ClusterId, StringComparison.Ordinal);
        Assert.True(first.RankScore > 0);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-cluster-tests", Guid.NewGuid().ToString("N"));
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
