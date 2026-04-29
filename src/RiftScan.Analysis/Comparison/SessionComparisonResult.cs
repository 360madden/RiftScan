using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record SessionComparisonResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.session_comparison.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_a_path")]
    public string SessionAPath { get; init; } = string.Empty;

    [JsonPropertyName("session_b_path")]
    public string SessionBPath { get; init; } = string.Empty;

    [JsonPropertyName("comparison_path")]
    public string? ComparisonPath { get; init; }

    [JsonPropertyName("comparison_report_path")]
    public string? ComparisonReportPath { get; init; }

    [JsonPropertyName("comparison_next_capture_plan_path")]
    public string? ComparisonNextCapturePlanPath { get; init; }

    [JsonPropertyName("session_a_id")]
    public string SessionAId { get; init; } = string.Empty;

    [JsonPropertyName("session_b_id")]
    public string SessionBId { get; init; } = string.Empty;

    [JsonPropertyName("same_process_name")]
    public bool SameProcessName { get; init; }

    [JsonPropertyName("matching_region_count")]
    public int MatchingRegionCount { get; init; }

    [JsonPropertyName("matching_cluster_count")]
    public int MatchingClusterCount { get; init; }

    [JsonPropertyName("matching_structure_candidate_count")]
    public int MatchingStructureCandidateCount { get; init; }

    [JsonPropertyName("matching_vec3_candidate_count")]
    public int MatchingVec3CandidateCount { get; init; }

    [JsonPropertyName("matching_value_candidate_count")]
    public int MatchingValueCandidateCount { get; init; }

    [JsonPropertyName("region_matches")]
    public IReadOnlyList<RegionComparison> RegionMatches { get; init; } = [];

    [JsonPropertyName("cluster_matches")]
    public IReadOnlyList<ClusterComparison> ClusterMatches { get; init; } = [];

    [JsonPropertyName("structure_candidate_matches")]
    public IReadOnlyList<StructureCandidateComparison> StructureCandidateMatches { get; init; } = [];

    [JsonPropertyName("vec3_candidate_matches")]
    public IReadOnlyList<Vec3CandidateComparison> Vec3CandidateMatches { get; init; } = [];

    [JsonPropertyName("vec3_behavior_summary")]
    public Vec3BehaviorSummary Vec3BehaviorSummary { get; init; } = new();

    [JsonPropertyName("value_candidate_matches")]
    public IReadOnlyList<ValueCandidateComparison> ValueCandidateMatches { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
