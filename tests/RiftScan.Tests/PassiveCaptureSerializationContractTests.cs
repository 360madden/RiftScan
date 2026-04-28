using System.Text.Json;
using RiftScan.Capture.Passive;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class PassiveCaptureSerializationContractTests
{
    [Fact]
    public void Passive_capture_result_serializes_summary_fields()
    {
        var result = new PassiveCaptureResult
        {
            Success = false,
            SessionPath = "sessions/test-session",
            SessionId = "test-session",
            ProcessId = 1234,
            ProcessName = "fixture_process",
            Status = "interrupted",
            SamplesRequested = 3,
            SamplesAttempted = 2,
            InterruptionReason = "intervention_wait_timed_out",
            RegionsCaptured = 1,
            SnapshotsCaptured = 1,
            BytesCaptured = 16,
            RegionReadFailureCount = 1,
            HandoffPath = "sessions/test-session/intervention_handoff.json",
            ArtifactsWritten = ["manifest.json", "intervention_handoff.json"]
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, SessionJson.Options));
        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("interrupted", root.GetProperty("status").GetString());
        Assert.Equal(3, root.GetProperty("samples_requested").GetInt32());
        Assert.Equal(2, root.GetProperty("samples_attempted").GetInt32());
        Assert.Equal("intervention_wait_timed_out", root.GetProperty("interruption_reason").GetString());
        Assert.Equal(1, root.GetProperty("region_read_failure_count").GetInt32());
        Assert.Equal("sessions/test-session/intervention_handoff.json", root.GetProperty("handoff_path").GetString());
        Assert.True(root.TryGetProperty("artifacts_written", out _));
    }

    [Fact]
    public void Passive_capture_result_omits_null_optional_paths_and_reasons()
    {
        var result = new PassiveCaptureResult
        {
            Success = true,
            SessionPath = "sessions/test-session",
            SessionId = "test-session",
            ProcessId = 1234,
            ProcessName = "fixture_process",
            Status = "complete",
            SamplesRequested = 1,
            SamplesAttempted = 1,
            RegionsCaptured = 1,
            SnapshotsCaptured = 1,
            BytesCaptured = 16,
            ArtifactsWritten = ["manifest.json"]
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, SessionJson.Options));
        var root = document.RootElement;

        Assert.Equal("complete", root.GetProperty("status").GetString());
        Assert.False(root.TryGetProperty("interruption_reason", out _));
        Assert.False(root.TryGetProperty("handoff_path", out _));
    }

    [Fact]
    public void Capture_intervention_handoff_serializes_read_failure_contract()
    {
        var handoff = new CaptureInterventionHandoff
        {
            CreatedUtc = DateTimeOffset.Parse("2026-04-28T23:30:00Z"),
            SessionPath = "sessions/test-session",
            SessionId = "test-session",
            ProcessName = "fixture_process",
            ProcessId = 1234,
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-28T23:00:00Z"),
            Reason = "selected_regions_unreadable",
            RegionCount = 1,
            SnapshotCount = 1,
            BytesCaptured = 16,
            RegionReadFailures =
            [
                new CaptureInterventionRegionReadFailure
                {
                    RegionId = "region-000001",
                    BaseAddressHex = "0x1000",
                    RequestedBytes = 16,
                    Reason = "selected region unreadable"
                }
            ],
            SamplesTargeted = 2,
            RecommendedNextAction = "review_region_read_failures_or_capture_from_a_fresh_plan",
            InterventionWaitMilliseconds = 250,
            InterventionPollIntervalMilliseconds = 50
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(handoff, SessionJson.Options));
        var root = document.RootElement;
        var failure = root.GetProperty("region_read_failures")[0];

        Assert.Equal("riftscan.capture_intervention_handoff.v1", root.GetProperty("schema_version").GetString());
        Assert.Equal("selected_regions_unreadable", root.GetProperty("reason").GetString());
        Assert.Equal("review_region_read_failures_or_capture_from_a_fresh_plan", root.GetProperty("recommended_next_action").GetString());
        Assert.Equal("region-000001", failure.GetProperty("region_id").GetString());
        Assert.Equal("0x1000", failure.GetProperty("base_address_hex").GetString());
        Assert.Equal(16, failure.GetProperty("requested_bytes").GetInt32());
        Assert.Equal("selected region unreadable", failure.GetProperty("reason").GetString());
    }
}
