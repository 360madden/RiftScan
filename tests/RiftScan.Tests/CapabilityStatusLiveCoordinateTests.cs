using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Validation;

namespace RiftScan.Tests;

public sealed class CapabilityStatusLiveCoordinateTests
{
    [Fact]
    public void Capability_status_uses_rift_promoted_coordinate_live_for_position_readiness()
    {
        using var temp = new TempDirectory();
        var livePath = WriteMatchedLiveVerification(temp.Path);

        var result = new CapabilityStatusService().Build(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            [livePath]);

        Assert.Equal(Path.GetFullPath(livePath), result.RiftPromotedCoordinateLivePath);
        Assert.Equal([Path.GetFullPath(livePath)], result.RiftPromotedCoordinateLivePaths);
        Assert.Contains(result.Capabilities, capability => capability.Name == "rift_promoted_coordinate_live_verify");
        var position = Assert.Single(result.TruthComponents, component => component.Component == "position");
        Assert.Equal("live_validated_candidate", position.EvidenceReadiness);
        Assert.Equal(1, position.EvidenceCount);
        Assert.Equal("manual_review_live_validated_coordinate_candidate_before_final_truth_claim", position.NextAction);
        Assert.DoesNotContain(result.EvidenceMissing, missing => missing.StartsWith("position:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review", result.Warnings);
    }

    [Fact]
    public void Report_capability_cli_accepts_rift_promoted_coordinate_live_packet()
    {
        using var temp = new TempDirectory();
        var livePath = WriteMatchedLiveVerification(temp.Path);
        var capabilityPath = Path.Combine(temp.Path, "capability-status.json");
        var capabilityReportPath = Path.Combine(temp.Path, "capability-status.md");

        var exitCode = RiftScan.Cli.Program.Main([
            "report",
            "capability",
            "--rift-promoted-coordinate-live",
            livePath,
            "--json-out",
            capabilityPath,
            "--report-md",
            capabilityReportPath
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(capabilityPath));
        Assert.True(File.Exists(capabilityReportPath));
        var result = JsonSerializer.Deserialize<CapabilityStatusResult>(File.ReadAllText(capabilityPath), SessionJson.Options)!;
        Assert.Equal([Path.GetFullPath(livePath)], result.RiftPromotedCoordinateLivePaths);
        Assert.Equal("live_validated_candidate", Assert.Single(result.TruthComponents, component => component.Component == "position").EvidenceReadiness);
        var report = File.ReadAllText(capabilityReportPath);
        Assert.Contains("# RiftScan capability status", report, StringComparison.Ordinal);
        Assert.Contains("live_validated_candidate", report, StringComparison.Ordinal);
        Assert.Contains("RIFT promoted coordinate live", report, StringComparison.Ordinal);
        Assert.Equal(0, RiftScan.Cli.Program.Main(["verify", "capability-status", capabilityPath]));
    }

    private static string WriteMatchedLiveVerification(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "rift-promoted-coordinate-live.json");
        var result = new RiftPromotedCoordinateLiveVerificationResult
        {
            PromotionPath = Path.Combine(directory, "vec3-truth-promotion.json"),
            SavedVariablesPathRedacted = @"C:\Users\<user>\OneDrive\Documents\RIFT\Interface\Saved",
            OutputPath = path,
            ProcessId = 1234,
            ProcessName = "rift_x64",
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-29T18:00:00Z"),
            ReadUtc = DateTimeOffset.Parse("2026-04-29T18:05:00Z"),
            CandidateId = "vec3-promoted-000001",
            SourceRecoveredCandidateId = "vec3-recovered-000043",
            BaseAddressHex = "0x975E1D8000",
            OffsetHex = "0x47EC",
            AbsoluteAddressHex = "0x975E1DC7EC",
            XOffsetHex = "0x47EC",
            YOffsetHex = "0x47F0",
            ZOffsetHex = "0x47F4",
            ReadByteCount = 12,
            MemoryX = 7559.155273,
            MemoryY = 817.966675,
            MemoryZ = 3406.560059,
            AddonObservationId = "ReaderBridgeExport:coord_table_xyz:2026-04-29T18:05:00Z",
            AddonSource = "ReaderBridgeExport:coord_table_xyz",
            AddonFileLastWriteUtc = DateTimeOffset.Parse("2026-04-29T18:05:00Z"),
            AddonObservedX = 7559.159668,
            AddonObservedY = 817.929993,
            AddonObservedZ = 3406.649902,
            AddonObservationCount = 1,
            MaxAbsDistance = 0.08984375,
            Tolerance = 5,
            ValidationStatus = "live_memory_and_addon_coordinate_matched_candidate",
            ClaimLevel = "candidate_validation",
            EvidenceSummary = "Fixture memory coordinate is within tolerance of refreshed addon coordinate observation.",
            Warnings = ["rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review"]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        Assert.True(new RiftPromotedCoordinateLiveVerificationVerifier().Verify(path).Success);
        return path;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-capability-live-coordinate-tests", Guid.NewGuid().ToString("N"));
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
