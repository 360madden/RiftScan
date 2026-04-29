namespace RiftScan.Analysis.Comparison;

public sealed class ScalarPromotionReviewReportGenerator
{
    public string Generate(ScalarPromotionReviewResult result, string reportPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllLines(fullPath, BuildReport(result));
        return fullPath;
    }

    private static IEnumerable<string> BuildReport(ScalarPromotionReviewResult result)
    {
        yield return "# RiftScan Scalar Promotion Review";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"- Decision state: `{result.DecisionState}`";
        yield return $"- Candidate reviews: `{result.ReviewCandidateCount}`";
        yield return $"- Ready for manual truth review: `{result.ReadyForManualTruthReviewCount}`";
        yield return $"- Blocked conflicts: `{result.BlockedConflictCount}`";
        yield return $"- Needs more corroboration: `{result.NeedsMoreCorroborationCount}`";
        yield return $"- Needs repeat capture: `{result.NeedsRepeatCaptureCount}`";
        yield return $"- Do not promote: `{result.DoNotPromoteCount}`";
        yield return string.Empty;
        yield return "This report is **not** a final truth claim. Manual confirmation is still required before promoting any candidate to recovered truth.";
        yield return string.Empty;
        yield return "## Source artifacts";
        yield return string.Empty;
        yield return $"- Promotion packet: `{Format(result.PromotionPath)}`";
        yield return $"- Review packet: `{Format(result.OutputPath)}`";
        yield return string.Empty;
        yield return "## Candidate reviews";
        yield return string.Empty;
        yield return "| Review ID | Decision | Classification | Base | Offset | Source promotion | Corroboration | Score | Blocking gaps | Next action |";
        yield return "|---|---|---|---|---:|---|---|---:|---|---|";
        foreach (var candidate in result.CandidateReviews)
        {
            yield return $"| `{Safe(candidate.ReviewCandidateId)}` | `{Safe(candidate.DecisionState)}` | `{Safe(candidate.Classification)}` | `{Safe(candidate.BaseAddressHex)}` | `{Safe(candidate.OffsetHex)}` | `{Safe(candidate.SourcePromotionStatus)}` | `{Safe(candidate.SourceCorroborationStatus)}` | {candidate.BestScoreTotal:F3} | `{Safe(string.Join(",", candidate.BlockingGaps))}` | `{Safe(candidate.NextAction)}` |";
        }

        if (result.CandidateReviews.Count == 0)
        {
            yield return "| none | `do_not_promote` | - | - | - | - | - | 0 | `no_candidates` | `repeat_recovery_or_stop` |";
        }

        yield return string.Empty;
        yield return "## Warnings";
        yield return string.Empty;
        foreach (var warning in result.Warnings)
        {
            yield return $"- `{Safe(warning)}`";
        }
    }

    private static string Format(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string Safe(string? value) =>
        Format(value).Replace("|", "\\|", StringComparison.Ordinal);
}
