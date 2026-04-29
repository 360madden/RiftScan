using System.Text.Json.Serialization;

namespace RiftScan.Capture.Passive;

public sealed record PassiveCapturePlanDocument
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.next_capture_plan.v1";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = string.Empty;

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

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}

public sealed record PassiveComparisonCapturePlanDocument
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("target_region_priorities")]
    public IReadOnlyList<PassiveComparisonCaptureTarget> TargetRegionPriorities { get; init; } = [];
}

public sealed record PassiveComparisonCaptureTarget
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("session_a_region_id")]
    public string SessionARegionId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_region_id")]
    public string SessionBRegionId { get; init; } = string.Empty;

    [JsonPropertyName("priority_score")]
    public double PriorityScore { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
