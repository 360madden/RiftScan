using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record SessionVerificationResult
{
    [JsonPropertyName("success")]
    public bool Success => Issues.All(issue => !StringComparer.OrdinalIgnoreCase.Equals(issue.Severity, "error"));

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("artifacts_verified")]
    public IReadOnlyList<string> ArtifactsVerified { get; init; } = [];

    [JsonPropertyName("issues")]
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
}

public sealed record VerificationIssue
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
