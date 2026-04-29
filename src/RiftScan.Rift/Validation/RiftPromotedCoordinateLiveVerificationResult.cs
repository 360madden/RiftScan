using System.Text.Json.Serialization;

namespace RiftScan.Rift.Validation;

public sealed record RiftPromotedCoordinateLiveVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_promoted_coordinate_live_verification.v1";

    [JsonPropertyName("success")]
    public bool Success =>
        string.Equals(ValidationStatus, "live_memory_and_addon_coordinate_matched_candidate", StringComparison.OrdinalIgnoreCase) &&
        Issues.All(issue => !string.Equals(issue.Severity, "error", StringComparison.OrdinalIgnoreCase));

    [JsonPropertyName("promotion_path")]
    public string PromotionPath { get; init; } = string.Empty;

    [JsonPropertyName("savedvariables_path_redacted")]
    public string SavedVariablesPathRedacted { get; init; } = string.Empty;

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("process_start_time_utc")]
    public DateTimeOffset? ProcessStartTimeUtc { get; init; }

    [JsonPropertyName("read_utc")]
    public DateTimeOffset ReadUtc { get; init; }

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("source_recovered_candidate_id")]
    public string SourceRecoveredCandidateId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("absolute_address_hex")]
    public string AbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("x_offset_hex")]
    public string XOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("y_offset_hex")]
    public string YOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("z_offset_hex")]
    public string ZOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("read_byte_count")]
    public int ReadByteCount { get; init; }

    [JsonPropertyName("memory_x")]
    public double? MemoryX { get; init; }

    [JsonPropertyName("memory_y")]
    public double? MemoryY { get; init; }

    [JsonPropertyName("memory_z")]
    public double? MemoryZ { get; init; }

    [JsonPropertyName("addon_observation_id")]
    public string AddonObservationId { get; init; } = string.Empty;

    [JsonPropertyName("addon_source")]
    public string AddonSource { get; init; } = string.Empty;

    [JsonPropertyName("addon_file_last_write_utc")]
    public DateTimeOffset? AddonFileLastWriteUtc { get; init; }

    [JsonPropertyName("addon_observed_x")]
    public double? AddonObservedX { get; init; }

    [JsonPropertyName("addon_observed_y")]
    public double? AddonObservedY { get; init; }

    [JsonPropertyName("addon_observed_z")]
    public double? AddonObservedZ { get; init; }

    [JsonPropertyName("addon_observation_count")]
    public int AddonObservationCount { get; init; }

    [JsonPropertyName("max_abs_distance")]
    public double? MaxAbsDistance { get; init; }

    [JsonPropertyName("tolerance")]
    public double Tolerance { get; init; }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = "verification_incomplete";

    [JsonPropertyName("claim_level")]
    public string ClaimLevel { get; init; } = "candidate_validation";

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftPromotedCoordinateLiveVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftPromotedCoordinateLiveVerificationIssue
{
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
