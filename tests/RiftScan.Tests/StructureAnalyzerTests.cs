using System.Text.Json;
using RiftScan.Analysis.Structures;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class StructureAnalyzerTests
{
    [Fact]
    public void Analyze_session_writes_float_triplet_structure_candidates()
    {
        using var session = CopyFixtureToTemp();

        var candidates = new FloatTripletStructureAnalyzer().AnalyzeSession(session.Path);

        Assert.Contains(candidates, candidate => candidate.RegionId == "region-0001" && candidate.OffsetHex == "0x0");
        Assert.True(File.Exists(Path.Combine(session.Path, "structures.jsonl")));

        var firstLine = File.ReadLines(Path.Combine(session.Path, "structures.jsonl")).First();
        var first = JsonSerializer.Deserialize<StructureCandidate>(firstLine, SessionJson.Options)!;
        Assert.Equal("structure-000001", first.CandidateId);
        Assert.Equal("float32_triplet", first.StructureKind);
        Assert.Equal("unvalidated_candidate", first.ValidationStatus);
        Assert.Equal("high", first.ConfidenceLevel);
        Assert.Equal("finite_float32_triplet_supported_in_1_of_1_snapshots", first.ExplanationShort);
        Assert.Equal(first.Score, first.ScoreBreakdown["score_total"]);
        Assert.Equal(1, first.ScoreBreakdown["snapshot_support_ratio"]);
        Assert.Equal(3, first.FeatureVector["component_count"]);
        Assert.Equal(1, first.FeatureVector["snapshot_support_ratio"]);
        Assert.Contains("snapshots/index.jsonl", first.AnalyzerSources);
        Assert.Contains("snapshots/*.bin", first.AnalyzerSources);
        Assert.True(first.Score > 0);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-structure-tests", Guid.NewGuid().ToString("N"));
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
