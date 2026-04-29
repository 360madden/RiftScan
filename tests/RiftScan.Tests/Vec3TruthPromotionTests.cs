using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class Vec3TruthPromotionTests
{
    [Fact]
    public void Vec3_truth_promotion_ranks_corroborated_candidate_nearest_actor_yaw_first()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = WriteVec3Recovery(temp.Path);
        var corroborationPath = WriteVec3Corroboration(temp.Path);
        var actorYawRecoveryPath = WriteActorYawRecovery(temp.Path);

        var promotion = new Vec3TruthPromotionService().Promote(recoveryPath, corroborationPath, actorYawRecoveryPath);

        Assert.True(promotion.Success);
        Assert.Equal(Path.GetFullPath(recoveryPath), promotion.RecoveryPath);
        Assert.Equal(Path.GetFullPath(corroborationPath), promotion.CorroborationPath);
        Assert.Equal(Path.GetFullPath(actorYawRecoveryPath), promotion.ActorYawRecoveryPath);
        Assert.Equal(3, promotion.RecoveredCandidateCount);
        Assert.Equal(2, promotion.PromotedCandidateCount);
        Assert.Equal(1, promotion.BlockedCandidateCount);
        Assert.Equal("vec3-promoted-000001", promotion.RecommendedManualReviewCandidateId);
        var recommended = promotion.PromotedCandidates[0];
        Assert.Equal("vec3-recovered-000001", recommended.SourceRecoveredCandidateId);
        Assert.Equal("0x975E1D8000", recommended.BaseAddressHex);
        Assert.Equal("0x47EC", recommended.OffsetHex);
        Assert.Equal("0x47EC", recommended.XOffsetHex);
        Assert.Equal("0x47F0", recommended.YOffsetHex);
        Assert.Equal("0x47F4", recommended.ZOffsetHex);
        Assert.Equal("corroborated_candidate", recommended.PromotionStatus);
        Assert.Equal("corroborated", recommended.CorroborationStatus);
        Assert.Equal("scalar-recovered-000001", recommended.ActorYawSourceCandidateId);
        Assert.Equal("0x47D0", recommended.ActorYawOffsetHex);
        Assert.Equal(28, recommended.ActorYawProximityBytes);
        Assert.Contains("actor_yaw_proximity_available", recommended.SupportingReasons);
        Assert.Contains("vec3_truth_promotion_contains_uncorroborated_recovered_candidates", promotion.Warnings);
    }

    [Fact]
    public void Vec3_truth_promotion_verifier_accepts_packet_and_cli()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = WriteVec3Recovery(temp.Path);
        var corroborationPath = WriteVec3Corroboration(temp.Path);
        var actorYawRecoveryPath = WriteActorYawRecovery(temp.Path);
        var promotionPath = Path.Combine(temp.Path, "vec3-truth-promotion.json");
        var promotion = new Vec3TruthPromotionService().Promote(recoveryPath, corroborationPath, actorYawRecoveryPath) with { OutputPath = promotionPath };
        File.WriteAllText(promotionPath, JsonSerializer.Serialize(promotion, SessionJson.Options));

        var verification = new Vec3TruthPromotionVerifier().Verify(promotionPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "vec3-truth-promotion", promotionPath]);

        Assert.True(verification.Success);
        Assert.Equal(2, verification.PromotedCandidateCount);
        Assert.Equal(1, verification.BlockedCandidateCount);
        Assert.Equal("vec3-promoted-000001", verification.RecommendedManualReviewCandidateId);
        Assert.Empty(verification.Issues);
        Assert.Equal(0, cliExitCode);
    }

    [Fact]
    public void Compare_vec3_promotion_cli_writes_promotion_packet()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var recoveryPath = WriteVec3Recovery(temp.Path);
        var corroborationPath = WriteVec3Corroboration(temp.Path);
        var actorYawRecoveryPath = WriteActorYawRecovery(temp.Path);
        var promotionPath = Path.Combine(temp.Path, "vec3-truth-promotion.json");

        var exitCode = RiftScan.Cli.Program.Main(
        [
            "compare",
            "vec3-promotion",
            recoveryPath,
            "--corroboration",
            corroborationPath,
            "--actor-yaw-recovery",
            actorYawRecoveryPath,
            "--out",
            promotionPath
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(promotionPath));
        var promotion = JsonSerializer.Deserialize<Vec3TruthPromotionResult>(File.ReadAllText(promotionPath), SessionJson.Options)!;
        Assert.Equal("vec3-promoted-000001", promotion.RecommendedManualReviewCandidateId);
        Assert.Equal("0x47EC", promotion.PromotedCandidates[0].OffsetHex);
        Assert.Equal(28, promotion.PromotedCandidates[0].ActorYawProximityBytes);
    }

    private static string WriteVec3Recovery(string outputRoot)
    {
        var path = Path.Combine(outputRoot, "vec3-truth-recovery.json");
        var result = new Vec3TruthRecoveryResult
        {
            Success = true,
            TruthCandidatePaths = [Path.Combine(outputRoot, "a.jsonl"), Path.Combine(outputRoot, "b.jsonl")],
            OutputPath = path,
            InputCandidateCount = 6,
            RecoveredCandidateCount = 3,
            RecoveredCandidates =
            [
                Vec3Recovered("vec3-recovered-000001", "0x47EC", 80),
                Vec3Recovered("vec3-recovered-000002", "0x50BC", 100),
                Vec3Recovered("vec3-recovered-000003", "0x7000", 90)
            ],
            Warnings = ["vec3_recovery_is_repeated_candidate_evidence_not_unconditional_coordinate_truth"]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static string WriteVec3Corroboration(string outputRoot)
    {
        var path = Path.Combine(outputRoot, "vec3-truth-corroboration.jsonl");
        var entries = new[]
        {
            Vec3Corroboration("0x47EC", "fixture addon coord near actor yaw"),
            Vec3Corroboration("0x50BC", "fixture addon coord farther from actor yaw")
        };
        File.WriteAllLines(path, entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return path;
    }

    private static string WriteActorYawRecovery(string outputRoot)
    {
        var path = Path.Combine(outputRoot, "scalar-truth-recovery.json");
        var result = new ScalarTruthRecoveryResult
        {
            Success = true,
            TruthCandidatePaths = [Path.Combine(outputRoot, "scalar-a.jsonl"), Path.Combine(outputRoot, "scalar-b.jsonl")],
            OutputPath = path,
            InputCandidateCount = 2,
            RecoveredCandidateCount = 1,
            RecoveredCandidates =
            [
                new ScalarRecoveredTruthCandidate
                {
                    CandidateId = "scalar-recovered-000001",
                    BaseAddressHex = "0x975E1D8000",
                    OffsetHex = "0x47D0",
                    DataType = "float32",
                    ValueFamily = "angle",
                    Classification = "actor_yaw_angle_scalar_candidate",
                    SupportingTruthCandidateIds = ["scalar-a.jsonl:scalar-truth-000001", "scalar-b.jsonl:scalar-truth-000001"],
                    SupportingFileCount = 2,
                    BestScoreTotal = 100,
                    LabelsPresent = ["turn_left", "turn_right"],
                    SupportingReasons = ["repeated_truth_candidate_match"],
                    EvidenceSummary = "fixture recovered actor yaw"
                }
            ],
            Warnings = ["scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth"]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static Vec3RecoveredTruthCandidate Vec3Recovered(string candidateId, string offsetHex, double score) =>
        new()
        {
            CandidateId = candidateId,
            BaseAddressHex = "0x975E1D8000",
            OffsetHex = offsetHex,
            DataType = "vec3_float32",
            Classification = "position_like_vec3_candidate",
            SupportingTruthCandidateIds = [$"a.jsonl:{candidateId}", $"b.jsonl:{candidateId}"],
            SupportingFileCount = 2,
            BestScoreTotal = score,
            LabelsPresent = ["move_forward", "passive_idle"],
            SupportingReasons = ["repeated_truth_candidate_match"],
            EvidenceSummary = $"fixture recovered coordinate {offsetHex}"
        };

    private static Vec3TruthCorroborationEntry Vec3Corroboration(string offsetHex, string evidenceSummary) =>
        new()
        {
            BaseAddressHex = "0x975E1D8000",
            OffsetHex = offsetHex,
            DataType = "vec3_float32",
            Classification = "position_like_vec3_candidate",
            CorroborationStatus = "corroborated",
            Source = "fixture_readerbridge_coordX_coordY_coordZ",
            AddonSourceType = "readerbridge_player_coord",
            AddonObservedX = 7559.1,
            AddonObservedY = 817.93,
            AddonObservedZ = 3173.18,
            Tolerance = 5,
            EvidenceSummary = evidenceSummary
        };

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-vec3-promotion-tests", Guid.NewGuid().ToString("N"));
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
