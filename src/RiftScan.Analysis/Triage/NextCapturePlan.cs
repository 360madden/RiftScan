using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Triage;

public sealed record NextCapturePlan
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.next_capture_plan.v1";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "dynamic_region_triage";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "0.1.0";

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("regions")]
    public IReadOnlyList<NextCaptureRegion> Regions { get; init; } = [];
}

public sealed record NextCaptureRegion
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("rank_score")]
    public double RankScore { get; init; }

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
