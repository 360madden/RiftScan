namespace RiftScan.Analysis.Comparison;

public sealed class ScalarEvidenceSetReportGenerator
{
    public string Generate(ScalarEvidenceSetResult result, string reportPath, int top = 100)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllLines(fullPath, BuildReport(result, top));
        return fullPath;
    }

    private static IEnumerable<string> BuildReport(ScalarEvidenceSetResult result, int top)
    {
        yield return "# RiftScan Scalar Evidence Set";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"- Sessions: `{result.SessionCount}`";
        yield return $"- Scalar candidate keys: `{result.ScalarCandidateKeyCount}`";
        yield return $"- Ranked candidates: `{result.RankedCandidateCount}`";
        yield return string.Empty;
        yield return "## Sessions";
        yield return string.Empty;
        yield return "| Session | Stimulus | Scalar candidates |";
        yield return "|---|---|---:|";
        foreach (var session in result.SessionSummaries)
        {
            yield return $"| `{session.SessionId}` | `{Format(session.StimulusLabel)}` | {session.ScalarCandidateCount} |";
        }

        yield return string.Empty;
        yield return "## Ranked scalar evidence candidates";
        yield return string.Empty;
        yield return "| Rank | Classification | Score | Confidence | Truth readiness | Base | Offset | Family | Labels | Passive stable | L/R polarity | Camera/turn | Next validation |";
        yield return "|---:|---|---:|---|---|---|---:|---|---|---|---|---|---|";
        var rank = 1;
        foreach (var candidate in result.RankedCandidates.Take(top))
        {
            yield return $"| {rank} | `{candidate.Classification}` | {candidate.ScoreTotal:F3} | `{candidate.ConfidenceLevel}` | `{candidate.TruthReadiness}` | `{candidate.BaseAddressHex}` | `{candidate.OffsetHex}` | `{candidate.ValueFamily}` | `{string.Join(",", candidate.LabelsPresent)}` | `{candidate.PassiveStable}` | `{candidate.OppositeTurnPolarity}` | `{Format(candidate.CameraTurnSeparation)}` | `{candidate.NextValidationStep}` |";
            rank++;
        }

        if (rank == 1)
        {
            yield return "| 0 | none | 0 | - | - | - | - | - | - | - | - | - | - |";
        }

        yield return string.Empty;
        yield return "## Rejected scalar evidence summaries";
        yield return string.Empty;
        yield return "| Reason | Count | Examples |";
        yield return "|---|---:|---|";
        foreach (var summary in result.RejectedCandidateSummaries)
        {
            var examples = string.Join("; ", summary.ExampleCandidates.Select(candidate => $"{candidate.BaseAddressHex}+{candidate.OffsetHex} score={candidate.ScoreTotal:F3}"));
            yield return $"| `{summary.Reason}` | {summary.Count} | `{examples}` |";
        }

        if (result.RejectedCandidateSummaries.Count == 0)
        {
            yield return "| none | 0 | - |";
        }

        yield return string.Empty;
        yield return "## Warnings";
        yield return string.Empty;
        foreach (var warning in result.Warnings)
        {
            yield return $"- `{warning}`";
        }
    }

    private static string Format(string value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;
}
