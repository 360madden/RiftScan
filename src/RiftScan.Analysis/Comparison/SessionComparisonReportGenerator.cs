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
        yield return $"| Matching entity layouts | {result.MatchingEntityLayoutCount} |";
        yield return $"| Matching structures | {result.MatchingStructureCandidateCount} |";
        yield return $"| Matching vec3 candidates | {result.MatchingVec3CandidateCount} |";
        yield return $"| Matching typed values | {result.MatchingValueCandidateCount} |";
        yield return $"| Scalar heuristic candidates | {result.ScalarBehaviorSummary.HeuristicCandidateCount} |";
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
        yield return "### Behavior contrast candidates";
        yield return string.Empty;
        yield return "| Rank | Classification | Score | Confidence | Base | Offset | A stimulus | B stimulus | A delta | B delta | Next validation |";
        yield return "|---:|---|---:|---|---|---:|---|---|---:|---:|---|";

        var contrastRank = 1;
        foreach (var candidate in result.Vec3BehaviorSummary.BehaviorContrastCandidates.Take(top))
        {
            yield return $"| {contrastRank} | `{candidate.Classification}` | {candidate.ScoreTotal:F3} | `{candidate.ConfidenceLevel}` | `{candidate.BaseAddressHex}` | `{candidate.OffsetHex}` | `{FormatLabel(candidate.SessionAStimulusLabel)}` | `{FormatLabel(candidate.SessionBStimulusLabel)}` | {candidate.SessionAValueDeltaMagnitude:F6} | {candidate.SessionBValueDeltaMagnitude:F6} | `{candidate.NextValidationStep}` |";
            contrastRank++;
        }

        if (contrastRank == 1)
        {
            yield return "| 0 | none | 0 | - | - | - | - | - | 0 | 0 | - |";
        }

        yield return string.Empty;
        yield return "## Top vec3 matches";
        yield return string.Empty;
        yield return "| Rank | A candidate | B candidate | Base | Offset | A stimulus | B stimulus | A delta | B delta | A summary | B summary | Sources | Recommendation |";
        yield return "|---:|---|---|---|---:|---|---|---:|---:|---|---|---|---|";

        var rank = 1;
        foreach (var match in result.Vec3CandidateMatches.Take(top))
        {
            yield return $"| {rank} | `{match.SessionACandidateId}` | `{match.SessionBCandidateId}` | `{match.BaseAddressHex}` | `{match.OffsetHex}` | `{FormatLabel(match.SessionAStimulusLabel)}` | `{FormatLabel(match.SessionBStimulusLabel)}` | {match.SessionAValueDeltaMagnitude:F6} | {match.SessionBValueDeltaMagnitude:F6} | `{match.SessionAValueSequenceSummary}` | `{match.SessionBValueSequenceSummary}` | `{FormatSources(match.SessionAAnalyzerSources, match.SessionBAnalyzerSources)}` | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | none | - | - | - | - | 0 | 0 | - | - | - | - |";
        }

        yield return string.Empty;
        yield return "## Top structure matches";
        yield return string.Empty;
        yield return "| Rank | A candidate | B candidate | Base | Offset | Kind | A score | B score | A summary | B summary | Sources | Recommendation |";
        yield return "|---:|---|---|---|---:|---|---:|---:|---|---|---|---|";

        rank = 1;
        foreach (var match in result.StructureCandidateMatches.Take(top))
        {
            yield return $"| {rank} | `{match.SessionACandidateId}` | `{match.SessionBCandidateId}` | `{match.BaseAddressHex}` | `{match.OffsetHex}` | `{match.StructureKind}` | {match.SessionAScore:F3} | {match.SessionBScore:F3} | `{match.SessionAValueSequenceSummary}` | `{match.SessionBValueSequenceSummary}` | `{FormatSources(match.SessionAAnalyzerSources, match.SessionBAnalyzerSources)}` | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | none | - | - | - | 0 | 0 | - | - | - | - |";
        }

        yield return string.Empty;
        yield return "## Top entity layout matches";
        yield return string.Empty;
        yield return "| Rank | A candidate | B candidate | Base | Span | Kind | Stride | A score | B score | A clusters | B clusters | A vec3 | B vec3 | Overlap | Recommendation |";
        yield return "|---:|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|";

        rank = 1;
        foreach (var match in result.EntityLayoutMatches.Take(top))
        {
            yield return $"| {rank} | `{match.SessionACandidateId}` | `{match.SessionBCandidateId}` | `{match.BaseAddressHex}` | `{match.StartOffsetHex}-{match.EndOffsetHex}` | `{match.LayoutKind}` | {match.StrideBytes} | {match.SessionAScore:F3} | {match.SessionBScore:F3} | {match.SessionAClusterCount} | {match.SessionBClusterCount} | {match.SessionAVec3CandidateCount} | {match.SessionBVec3CandidateCount} | {match.OverlapBytes} | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | none | - | - | - | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | - |";
        }

        yield return "## Scalar behavior summary";
        yield return string.Empty;
        yield return "| Metric | Value |";
        yield return "|---|---:|";
        yield return $"| Matching scalar candidates | {result.ScalarBehaviorSummary.MatchingScalarCandidateCount} |";
        yield return $"| Heuristic scalar candidates | {result.ScalarBehaviorSummary.HeuristicCandidateCount} |";
        yield return $"| Strong scalar candidates | {result.ScalarBehaviorSummary.StrongCandidateCount} |";
        yield return string.Empty;
        yield return $"- Stimulus labels: `{FormatLabels(result.ScalarBehaviorSummary.StimulusLabels)}`";
        yield return $"- Next recommended action: `{result.ScalarBehaviorSummary.NextRecommendedAction}`";
        yield return string.Empty;
        yield return "| Rank | Classification | Score | Confidence | Base | Offset | Type | Family | A stimulus | B stimulus | A changed | B changed | A signed delta | B signed delta | Polarity | Camera/turn split | Next validation |";
        yield return "|---:|---|---:|---|---|---:|---|---|---|---|---:|---:|---:|---:|---|---|---|";

        rank = 1;
        foreach (var candidate in result.ScalarBehaviorSummary.ScalarBehaviorCandidates.Take(top))
        {
            yield return $"| {rank} | `{candidate.Classification}` | {candidate.ScoreTotal:F3} | `{candidate.ConfidenceLevel}` | `{candidate.BaseAddressHex}` | `{candidate.OffsetHex}` | `{candidate.DataType}` | `{FormatLabel(candidate.ValueFamily)}` | `{FormatLabel(candidate.SessionAStimulusLabel)}` | `{FormatLabel(candidate.SessionBStimulusLabel)}` | {candidate.SessionAChangedSampleCount} | {candidate.SessionBChangedSampleCount} | {candidate.SessionASignedCircularDelta:F6} | {candidate.SessionBSignedCircularDelta:F6} | `{FormatLabel(candidate.TurnPolarityRelationship)}` | `{FormatLabel(candidate.CameraTurnSeparationRelationship)}` | `{candidate.NextValidationStep}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | 0 | - | - | - | - | - | - | - | 0 | 0 | 0 | 0 | - | - | - |";
        }

        yield return string.Empty;
        yield return "## Top typed value matches";
        yield return string.Empty;
        yield return "| Rank | A candidate | B candidate | Base | Offset | Type | A score | B score | A summary | B summary | Sources | Recommendation |";
        yield return "|---:|---|---|---|---:|---|---:|---:|---|---|---|---|";

        rank = 1;
        foreach (var match in result.ValueCandidateMatches.Take(top))
        {
            yield return $"| {rank} | `{match.SessionACandidateId}` | `{match.SessionBCandidateId}` | `{match.BaseAddressHex}` | `{match.OffsetHex}` | `{match.DataType}` | {match.SessionARankScore:F3} | {match.SessionBRankScore:F3} | `{match.SessionAValueSequenceSummary}` | `{match.SessionBValueSequenceSummary}` | `{FormatSources(match.SessionAAnalyzerSources, match.SessionBAnalyzerSources)}` | `{match.Recommendation}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | none | - | - | - | 0 | 0 | - | - | - | - |";
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

    private static string FormatSources(IReadOnlyList<string> sessionASources, IReadOnlyList<string> sessionBSources)
    {
        var sources = sessionASources
            .Concat(sessionBSources)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return sources.Length == 0 ? "-" : string.Join(", ", sources);
    }
}
