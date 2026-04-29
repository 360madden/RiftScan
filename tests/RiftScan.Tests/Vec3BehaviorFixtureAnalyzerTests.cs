using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Vectors;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class Vec3BehaviorFixtureAnalyzerTests
{
    [Fact]
    public void Vec3_behavior_fixtures_verify_as_valid_sessions()
    {
        var passive = new SessionVerifier().Verify(PassiveFixturePath);
        var moving = new SessionVerifier().Verify(MoveForwardFixturePath);

        Assert.True(passive.Success, FormatIssues(passive.Issues));
        Assert.True(moving.Success, FormatIssues(moving.Issues));
        Assert.Equal("fixture-vec3-passive-session", passive.SessionId);
        Assert.Equal("fixture-vec3-move-forward-session", moving.SessionId);
        Assert.Contains("stimuli.jsonl", passive.ArtifactsVerified);
        Assert.Contains("stimuli.jsonl", moving.ArtifactsVerified);
        Assert.Contains("snapshots/region-0001-snapshot-000001.bin", passive.ArtifactsVerified);
        Assert.Contains("snapshots/region-0001-snapshot-000003.bin", moving.ArtifactsVerified);
    }

    [Fact]
    public void Vec3_behavior_fixtures_prove_passive_to_move_contrast_from_stored_snapshots()
    {
        using var passive = CopyFixtureToTemp(PassiveFixturePath);
        using var moving = CopyFixtureToTemp(MoveForwardFixturePath);

        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(passive.Path, top: 10);
        _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(moving.Path, top: 10);

        var passiveVec3 = ReadJsonLines<Vec3Candidate>(passive.Path, "vec3_candidates.jsonl")
            .Single(candidate => candidate.OffsetHex == "0x0");
        var movingVec3 = ReadJsonLines<Vec3Candidate>(moving.Path, "vec3_candidates.jsonl")
            .Single(candidate => candidate.OffsetHex == "0x0");
        var comparison = new SessionComparisonService().Compare(passive.Path, moving.Path, top: 10);
        var match = comparison.Vec3CandidateMatches.Single(candidate =>
            candidate.BaseAddressHex == "0x30000000" &&
            candidate.OffsetHex == "0x0");
        var plan = new SessionComparisonNextCapturePlanGenerator().Build(comparison);
        var passiveVerification = new SessionVerifier().Verify(passive.Path);
        var movingVerification = new SessionVerifier().Verify(moving.Path);

        Assert.Equal("passive_idle", passiveVec3.StimulusLabel);
        Assert.Equal(0, passiveVec3.ValueDeltaMagnitude);
        Assert.Equal(20, passiveVec3.BehaviorScore);
        Assert.Equal("behavior_consistent_candidate", passiveVec3.ValidationStatus);
        Assert.Equal("passive_idle_stable_vec3_candidate_followup", passiveVec3.Recommendation);
        Assert.Contains("stimuli.jsonl", passiveVec3.AnalyzerSources);

        Assert.Equal("move_forward", movingVec3.StimulusLabel);
        Assert.Equal(2, movingVec3.ValueDeltaMagnitude);
        Assert.Equal(25, movingVec3.BehaviorScore);
        Assert.Equal("behavior_consistent_candidate", movingVec3.ValidationStatus);
        Assert.Equal("move_forward_vec3_candidate_followup", movingVec3.Recommendation);
        Assert.Contains("stimuli.jsonl", movingVec3.AnalyzerSources);

        Assert.True(comparison.Success);
        Assert.Equal("fixture-vec3-passive-session", comparison.SessionAId);
        Assert.Equal("fixture-vec3-move-forward-session", comparison.SessionBId);
        Assert.Equal("passive_idle", match.SessionAStimulusLabel);
        Assert.Equal("move_forward", match.SessionBStimulusLabel);
        Assert.Equal(0, match.SessionAValueDeltaMagnitude);
        Assert.Equal(2, match.SessionBValueDeltaMagnitude);
        Assert.Equal(20, match.SessionABehaviorScore);
        Assert.Equal(25, match.SessionBBehaviorScore);
        Assert.Equal("passive_to_move_vec3_behavior_contrast_candidate", match.Recommendation);
        Assert.Equal(1, comparison.Vec3BehaviorSummary.BehaviorContrastCount);
        Assert.Equal("review_behavior_contrast_candidates_before_truth_claim", comparison.Vec3BehaviorSummary.NextRecommendedAction);

        Assert.Equal("review_existing_behavior_contrast", plan.RecommendedMode);
        Assert.Contains(plan.TargetRegionPriorities, priority =>
            priority.BaseAddressHex == "0x30000000" &&
            priority.OffsetHex == "0x0" &&
            priority.Reason == "passive_to_move_vec3_behavior_contrast_candidate");

        Assert.True(passiveVerification.Success, FormatIssues(passiveVerification.Issues));
        Assert.True(movingVerification.Success, FormatIssues(movingVerification.Issues));
        Assert.Contains("vec3_candidates.jsonl", passiveVerification.ArtifactsVerified);
        Assert.Contains("vec3_candidates.jsonl", movingVerification.ArtifactsVerified);
    }

    private static string PassiveFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "vec3-passive-session");

    private static string MoveForwardFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "vec3-move-forward-session");

    private static IReadOnlyList<T> ReadJsonLines<T>(string sessionPath, string relativePath)
    {
        return File.ReadLines(Path.Combine(sessionPath, relativePath))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid {relativePath} record."))
            .ToArray();
    }

    private static TempDirectory CopyFixtureToTemp(string fixturePath)
    {
        var temp = new TempDirectory();
        CopyDirectory(fixturePath, temp.Path);
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

    private static string FormatIssues(IEnumerable<VerificationIssue> issues) =>
        string.Join(Environment.NewLine, issues.Select(issue => $"{issue.Code}: {issue.Message}"));

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-vec3-behavior-fixture-tests", Guid.NewGuid().ToString("N"));
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
