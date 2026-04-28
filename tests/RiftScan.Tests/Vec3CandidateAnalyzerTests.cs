using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Vectors;

namespace RiftScan.Tests;

public sealed class Vec3CandidateAnalyzerTests
{
    [Fact]
    public void Analyze_session_promotes_float_triplet_structures_to_vec3_candidates()
    {
        using var session = CopyFixtureToTemp();
        _ = new FloatTripletStructureAnalyzer().AnalyzeSession(session.Path);

        var candidates = new Vec3CandidateAnalyzer().AnalyzeSession(session.Path);

        var candidate = Assert.Single(candidates, candidate => candidate.RegionId == "region-0001" && candidate.OffsetHex == "0x0");
        Assert.Equal("vec3_float32", candidate.DataType);
        Assert.Equal("unvalidated_candidate", candidate.ValidationStatus);
        Assert.Equal("vec3_candidate_followup", candidate.Recommendation);
        Assert.Contains("candidate_not_truth_claim", candidate.Diagnostics);
        Assert.True(File.Exists(Path.Combine(session.Path, "vec3_candidates.jsonl")));
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-vec3-tests", Guid.NewGuid().ToString("N"));
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
