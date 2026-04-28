using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Triage;

public sealed record SessionAnalysisResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("regions_analyzed")]
    public int RegionsAnalyzed { get; init; }

    [JsonPropertyName("artifacts_written")]
    public IReadOnlyList<string> ArtifactsWritten { get; init; } = [];
}
