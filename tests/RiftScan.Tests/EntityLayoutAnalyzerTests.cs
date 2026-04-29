using System.Text.Json;
using RiftScan.Analysis.Entities;
using RiftScan.Analysis.Triage;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class EntityLayoutAnalyzerTests
{
    [Fact]
    public void Entity_layout_analyzer_promotes_clusters_and_vec3s_without_player_claim()
    {
        using var session = CopyFixtureToTemp();
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);

        var candidates = new EntityLayoutAnalyzer().AnalyzeSession(session.Path, top: 10);

        Assert.NotEmpty(candidates);
        var top = candidates[0];
        Assert.Equal("entity_layout", top.AnalyzerId);
        Assert.Equal("riftscan.entity_layout_candidate.v1", top.SchemaVersion);
        Assert.Equal("unvalidated_candidate", top.ValidationStatus);
        Assert.Contains("entity_layout_candidate_not_player_identity", top.Diagnostics);
        Assert.True(top.ClusterCount > 0);
        Assert.True(top.Vec3CandidateCount > 0);
        Assert.True(top.ScoreTotal > 0);
        Assert.True(File.Exists(Path.Combine(session.Path, "entity_layout_candidates.jsonl")));

        var firstLine = File.ReadLines(Path.Combine(session.Path, "entity_layout_candidates.jsonl")).First();
        using var document = JsonDocument.Parse(firstLine);
        Assert.Equal("riftscan.entity_layout_candidate.v1", document.RootElement.GetProperty("schema_version").GetString());
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-entity-layout-tests", Guid.NewGuid().ToString("N"));
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
