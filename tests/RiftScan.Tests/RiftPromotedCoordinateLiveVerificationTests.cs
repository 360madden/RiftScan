using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Validation;

namespace RiftScan.Tests;

public sealed class RiftPromotedCoordinateLiveVerificationTests
{
    [Fact]
    public void Live_verification_reads_promoted_coordinate_and_matches_latest_addon_observation()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var promotionPath = WritePromotion(temp.Path);
        var savedVariablesPath = WriteSavedVariables(temp.Path, x: 10.25, y: 19.75, z: 30.5);
        var reader = new FixedVec3ProcessMemoryReader(0x1020, 10, 20, 30);

        var result = new RiftPromotedCoordinateLiveVerificationService(reader)
            .Verify(promotionPath, savedVariablesPath, processId: 1234, processName: null, tolerance: 1);

        Assert.True(result.Success);
        Assert.Equal(1234, result.ProcessId);
        Assert.Equal("rift_x64", result.ProcessName);
        Assert.Equal("vec3-promoted-000001", result.CandidateId);
        Assert.Equal("0x1020", result.AbsoluteAddressHex);
        Assert.Equal(10, result.MemoryX);
        Assert.Equal(20, result.MemoryY);
        Assert.Equal(30, result.MemoryZ);
        Assert.Equal(10.25, result.AddonObservedX);
        Assert.Equal(19.75, result.AddonObservedY);
        Assert.Equal(30.5, result.AddonObservedZ);
        Assert.Equal("ReaderBridgeExport:coord_table_xyz", result.AddonSource);
        Assert.Equal(0.5, result.MaxAbsDistance);
        Assert.Equal("live_memory_and_addon_coordinate_matched_candidate", result.ValidationStatus);
        Assert.Contains("addon_observation_filtered_to_promotion_corroboration_source", result.Warnings);
        Assert.Contains("rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review", result.Warnings);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Live_verification_verifier_accepts_matching_artifact()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var promotionPath = WritePromotion(temp.Path);
        var savedVariablesPath = WriteSavedVariables(temp.Path, x: 10.25, y: 19.75, z: 30.5);
        var outputPath = Path.Combine(temp.Path, "rift-promoted-coordinate-live.json");
        var result = new RiftPromotedCoordinateLiveVerificationService(new FixedVec3ProcessMemoryReader(0x1020, 10, 20, 30))
            .Verify(promotionPath, savedVariablesPath, processId: 1234, processName: null, tolerance: 1) with
        {
            OutputPath = outputPath
        };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(result, SessionJson.Options));

        var verification = new RiftPromotedCoordinateLiveVerificationVerifier().Verify(outputPath);
        var cliExitCode = RiftScan.Cli.Program.Main(["verify", "rift-promoted-coordinate-live", outputPath]);

        Assert.True(verification.Success);
        Assert.Equal("live_memory_and_addon_coordinate_matched_candidate", verification.ValidationStatus);
        Assert.Equal("vec3-promoted-000001", verification.CandidateId);
        Assert.Empty(verification.Issues);
        Assert.Equal(0, cliExitCode);
    }

    private static string WritePromotion(string outputRoot)
    {
        var path = Path.Combine(outputRoot, "vec3-truth-promotion.json");
        var result = new Vec3TruthPromotionResult
        {
            Success = true,
            RecoveryPath = Path.Combine(outputRoot, "vec3-truth-recovery.json"),
            CorroborationPath = Path.Combine(outputRoot, "vec3-truth-corroboration.jsonl"),
            RecoveredCandidateCount = 1,
            PromotedCandidateCount = 1,
            BlockedCandidateCount = 0,
            RecommendedManualReviewCandidateId = "vec3-promoted-000001",
            PromotedCandidates =
            [
                new Vec3PromotedTruthCandidate
                {
                    CandidateId = "vec3-promoted-000001",
                    SourceRecoveredCandidateId = "vec3-recovered-000001",
                    BaseAddressHex = "0x1000",
                    OffsetHex = "0x20",
                    XOffsetHex = "0x20",
                    YOffsetHex = "0x24",
                    ZOffsetHex = "0x28",
                    DataType = "vec3_float32",
                    Classification = "position_like_vec3_candidate",
                    PromotionStatus = "corroborated_candidate",
                    TruthReadiness = "corroborated_candidate",
                    ClaimLevel = "corroborated_candidate",
                    CorroborationStatus = "corroborated",
                    CorroborationSources = ["ReaderBridgeExport:coord_table_xyz"],
                    CorroborationSummary = "fixture corroboration",
                    SupportingTruthCandidateIds = ["a.jsonl:vec3-truth-000001", "b.jsonl:vec3-truth-000001"],
                    SupportingFileCount = 2,
                    BestScoreTotal = 100,
                    LabelsPresent = ["move_forward", "passive_idle"],
                    SupportingReasons = ["repeated_truth_candidate_match", "addon_coordinate_corroboration_corroborated"],
                    EvidenceSummary = "fixture promoted coordinate",
                    NextValidationStep = "manual_review_addon_corroborated_coordinate_candidate_before_final_truth_claim"
                }
            ],
            Warnings = ["vec3_truth_promotion_is_not_final_truth_without_manual_review"]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static string WriteSavedVariables(string outputRoot, double x, double y, double z)
    {
        var readerBridgePath = Path.Combine(outputRoot, "ReaderBridgeExport.lua");
        var validatorPath = Path.Combine(outputRoot, "RiftReaderValidator.lua");
        File.WriteAllText(readerBridgePath, $"ReaderBridgeExport = {{ coord = {{ x = {x}, y = {y}, z = {z} }}, zone = \"fixture\" }}");
        File.WriteAllText(validatorPath, "RiftReaderValidator = { coord = { x = 1000, y = 2000, z = 3000 }, zone = \"fixture\" }");
        File.SetLastWriteTimeUtc(readerBridgePath, DateTime.UtcNow.AddSeconds(-5));
        File.SetLastWriteTimeUtc(validatorPath, DateTime.UtcNow);
        return outputRoot;
    }

    private sealed class FixedVec3ProcessMemoryReader : IProcessMemoryReader
    {
        private readonly ulong expectedAddress;
        private readonly byte[] bytes;

        public FixedVec3ProcessMemoryReader(ulong expectedAddress, float x, float y, float z)
        {
            this.expectedAddress = expectedAddress;
            bytes =
            [
                .. BitConverter.GetBytes(x),
                .. BitConverter.GetBytes(y),
                .. BitConverter.GetBytes(z)
            ];
        }

        public IReadOnlyList<ProcessDescriptor> FindProcessesByName(string processName) =>
            [new ProcessDescriptor(1234, processName, DateTimeOffset.UnixEpoch, @"C:\RIFT\rift_x64.exe")];

        public ProcessDescriptor GetProcessById(int processId) =>
            new(processId, "rift_x64", DateTimeOffset.UnixEpoch, @"C:\RIFT\rift_x64.exe");

        public IReadOnlyList<ProcessModuleInfo> GetModules(int processId) => [];

        public IReadOnlyList<VirtualMemoryRegion> EnumerateRegions(int processId) => [];

        public byte[] ReadMemory(int processId, ulong baseAddress, int byteCount)
        {
            Assert.Equal(1234, processId);
            Assert.Equal(expectedAddress, baseAddress);
            Assert.Equal(12, byteCount);
            return bytes;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-rift-live-coordinate-tests", Guid.NewGuid().ToString("N"));
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
