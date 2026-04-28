using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record Vec3BehaviorSummary
{
    [JsonPropertyName("matching_vec3_candidate_count")]
    public int MatchingVec3CandidateCount { get; init; }

    [JsonPropertyName("behavior_contrast_count")]
    public int BehaviorContrastCount { get; init; }

    [JsonPropertyName("behavior_consistent_match_count")]
    public int BehaviorConsistentMatchCount { get; init; }

    [JsonPropertyName("unlabeled_match_count")]
    public int UnlabeledMatchCount { get; init; }

    [JsonPropertyName("stimulus_labels")]
    public IReadOnlyList<string> StimulusLabels { get; init; } = [];

    [JsonPropertyName("next_recommended_action")]
    public string NextRecommendedAction { get; init; } = string.Empty;
}
