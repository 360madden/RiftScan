using System.Text.Json.Serialization;

namespace RiftScan.Capture.Passive;

public sealed record PassiveCapturePlanDocument
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("regions")]
    public IReadOnlyList<PassiveCapturePlanRegion> Regions { get; init; } = [];
}

public sealed record PassiveCapturePlanRegion
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
