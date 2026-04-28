namespace RiftScan.Analysis.Comparison;

public sealed class SessionComparisonReportGenerator
{
    public string Generate(SessionComparisonResult result, string reportPath, int top = 100)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullReportPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullReportPath)!);
        File.WriteAllLines(fullReportPath, BuildReport(result, top));
        return fullReportPath;
    }

    private static IEnumerable<string> BuildReport(SessionComparisonResult result, int top)
    {
        yield return $"# RiftScan Session Comparison - {result.SessionAId} vs {result.SessionBId}";
        yield return string.Empty;
        yield return "This report is candidate evidence, not recovered truth.";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"| Field | Value |";
        yield return "|---|---|";
        yield return $"| Session A | `{result.SessionAId}` |";
        yield return $"| Session B | `{result.SessionBId}` |";
        yield return $"| Same process name | `{result.SameProcessName}` |";
        yield return $"| Matching regions | {result.MatchingRegionCount} |";
        yield return $"| Matching clusters | {result.MatchingClusterCount} |";
        yield return $"| Matching structures | {result.MatchingStructureCandidateCount} |";
        yield return $"| Matching vec3 candidates | {result.MatchingVec3CandidateCount} |";
        yield return $"| Matching typed values | {result.MatchingValueCandidateCount} |";
        yield return string.Empty;
        yield return "## Vec3 behavior summary";
        yield return string.Empty;
        yield return "| Metric | Value |";
        yield return "|---|---:|";
        yield return $"| Matching vec3 candidates | {result.Vec3BehaviorSummary.MatchingVec3CandidateCount} |";
        yield return $"| Behavior contrast matches | {result.Vec3BehaviorSummary.BehaviorContrastCount} |";
        yield return $"| Behavior-consistent matches | {result.Vec3BehaviorSummary.BehaviorConsistentMatchCount} |";
        yield return $"| Unlabeled matches | {result.Vec3BehaviorSummary.UnlabeledMatchCount} |";
        yield return string.Empty;
        yield return $"- Stimulus labels: `{FormatLabels(result.Vec3BehaviorSummary.StimulusLabels)}`";
        yield return $"- Next recommended action: `{result.Vec3BehaviorSummary.NextRecommendedAction}`";
        yield return string.Empty;
        yield return "## Top vec3 matches";
        yield return string.Empty;
        yield return "| Rank | Base | Offset | A stimulus | B stimulus | A score | B score | A delta | B delta | Recommendation |";
        yield return "|---:|---|---:|---|---|---:|---:|---:|---:|---|";

        var rank = 1;
        foreach (var match in result.Vec3CandidateMatches.Take(top))
        {
            yield return $"| {rank} | `{match.BaseAddressHex}` | `{match.OffsetHex}` | `{FormatLabel(match.SessionAStimulusLabel)}` | `{FormatLabel(match.SessionBStimulusLabel)}` | {match.SessionABehaviorScore:F3} | {match.SessionBBehaviorScore:F3} | {match.SessionAValueDeltaMagnitude:F6} | {match.SessionBValueDeltaMagnitude:F6} | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | - | - | - | 0 | 0 | 0 | 0 | - |";
        }

        yield return string.Empty;
        yield return "## Top structure matches";
        yield return string.Empty;
        yield return "| Rank | Base | Offset | Kind | A score | B score | Recommendation |";
        yield return "|---:|---|---:|---|---:|---:|---|";

        rank = 1;
        foreach (var match in result.StructureCandidateMatches.Take(top))
        {
            yield return $"| {rank} | `{match.BaseAddressHex}` | `{match.OffsetHex}` | `{match.StructureKind}` | {match.SessionAScore:F3} | {match.SessionBScore:F3} | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | - | - | 0 | 0 | - |";
        }

        yield return string.Empty;
        yield return "## Warnings";
        yield return string.Empty;

        if (result.Warnings.Count == 0)
        {
            yield return "- none";
        }
        else
        {
            foreach (var warning in result.Warnings)
            {
                yield return $"- `{warning}`";
            }
        }
    }

    private static string FormatLabels(IReadOnlyList<string> labels) =>
        labels.Count == 0 ? "-" : string.Join("`, `", labels);

    private static string FormatLabel(string label) =>
        string.IsNullOrWhiteSpace(label) ? "-" : label;
}
