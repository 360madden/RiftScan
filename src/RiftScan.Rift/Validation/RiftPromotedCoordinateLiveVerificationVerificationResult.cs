using System.Text.Json.Serialization;

namespace RiftScan.Rift.Validation;

public sealed record RiftPromotedCoordinateLiveVerificationVerificationResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.rift_promoted_coordinate_live_verification_verification.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; init; } = string.Empty;

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; init; } = string.Empty;

    [JsonPropertyName("max_abs_distance")]
    public double? MaxAbsDistance { get; init; }

    [JsonPropertyName("tolerance")]
    public double Tolerance { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftPromotedCoordinateLiveVerificationVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftPromotedCoordinateLiveVerificationVerificationIssue
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
