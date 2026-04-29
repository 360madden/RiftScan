using RiftScan.Analysis.Deltas;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Values;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ChangingFloatFixtureAnalyzerTests
{
    [Fact]
    public void Changing_float_fixture_verifies_as_valid_session()
    {
        var result = new SessionVerifier().Verify(ChangingFloatFixturePath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("fixture-changing-float-session", result.SessionId);
        Assert.Contains("snapshots/region-0001-snapshot-0001.bin", result.ArtifactsVerified);
        Assert.Contains("snapshots/region-0001-snapshot-0002.bin", result.ArtifactsVerified);
        Assert.Contains("snapshots/region-0001-snapshot-0003.bin", result.ArtifactsVerified);
    }

    [Fact]
    public void Changing_float_fixture_proves_delta_and_typed_float_lane()
    {
        using var session = CopyChangingFixtureToTemp();

        var analysisResult = new DynamicRegionTriageAnalyzer().AnalyzeSession(session.Path, top: 10);
        var triage = ReadJsonLines<RiftScan.Analysis.Triage.RegionTriageEntry>(session.Path, "triage.jsonl").Single();
        var delta = ReadJsonLines<RegionDeltaEntry>(session.Path, "deltas.jsonl").Single();
        var value = ReadJsonLines<TypedValueCandidate>(session.Path, "typed_value_candidates.jsonl")
            .Single(candidate => candidate.OffsetHex == "0x4");
        var verification = new SessionVerifier().Verify(session.Path);

        Assert.True(analysisResult.Success);
        Assert.Equal("fixture-changing-float-session", analysisResult.SessionId);
        Assert.Equal(3, triage.SnapshotCount);
        Assert.Equal(3, triage.UniqueChecksumCount);
        Assert.Equal("prioritize_for_delta_followup", triage.Recommendation);
        Assert.Contains("checksum_changed_across_samples", triage.Diagnostics);

        Assert.Equal(3, delta.SnapshotCount);
        Assert.Equal(2, delta.ComparedPairCount);
        Assert.Equal(2, delta.ChangedByteCount);
        Assert.Equal("0x6", delta.ChangedRanges.Single().StartOffsetHex);
        Assert.Equal("0x7", delta.ChangedRanges.Single().EndOffsetHex);

        Assert.Equal("float32", value.DataType);
        Assert.Equal("0x20000004", value.AbsoluteAddressHex);
        Assert.Equal(3, value.SampleCount);
        Assert.Equal(3, value.DistinctValueCount);
        Assert.Equal(2, value.ChangedSampleCount);
        Assert.Equal(100, value.RankScore);
        Assert.Equal("float_lane_followup", value.Recommendation);
        Assert.Equal(["1.5", "2.5", "3.5"], value.ValuePreview);
        Assert.Equal("samples=3;distinct=3;changed_pairs=2;preview=1.5|2.5|3.5", value.ValueSequenceSummary);

        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Contains("triage.jsonl", verification.ArtifactsVerified);
        Assert.Contains("deltas.jsonl", verification.ArtifactsVerified);
        Assert.Contains("typed_value_candidates.jsonl", verification.ArtifactsVerified);
    }

    private static string ChangingFloatFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "changing-float-session");

    private static IReadOnlyList<T> ReadJsonLines<T>(string sessionPath, string relativePath)
    {
        return File.ReadLines(Path.Combine(sessionPath, relativePath))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => System.Text.Json.JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid {relativePath} record."))
            .ToArray();
    }

    private static TempDirectory CopyChangingFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(ChangingFloatFixturePath, temp.Path);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-changing-fixture-tests", Guid.NewGuid().ToString("N"));
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
