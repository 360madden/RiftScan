using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthRecoveryService
{
    public ScalarTruthRecoveryResult Recover(IReadOnlyList<string> truthCandidatePaths, int top = 100)
    {
        ArgumentNullException.ThrowIfNull(truthCandidatePaths);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);
        if (truthCandidatePaths.Count < 2)
        {
            throw new ArgumentException("Scalar truth recovery requires at least two truth candidate files.", nameof(truthCandidatePaths));
        }

        var loaded = truthCandidatePaths
            .Select(path => LoadTruthCandidates(Path.GetFullPath(path)))
            .ToArray();
        var entries = loaded
            .SelectMany(file => file.Candidates.Select(candidate => new TruthCandidateEntry(file.Path, candidate)))
            .ToArray();
        var recovered = entries
            .GroupBy(entry => RecoveryKey(entry.Candidate), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(entry => entry.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
            .Select(group => BuildRecoveredCandidate(group.ToArray()))
            .OrderByDescending(candidate => candidate.BestScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((candidate, index) => candidate with { CandidateId = $"scalar-recovered-{index + 1:000000}" })
            .ToArray();

        return new ScalarTruthRecoveryResult
        {
            Success = true,
            TruthCandidatePaths = loaded.Select(file => file.Path).ToArray(),
            InputCandidateCount = entries.Length,
            RecoveredCandidateCount = recovered.Length,
            RecoveredCandidates = recovered,
            Warnings = BuildWarnings(recovered)
        };
    }

    private static LoadedTruthCandidateFile LoadTruthCandidates(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Scalar truth candidate file does not exist.", path);
        }

        var candidates = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<ScalarTruthCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid scalar truth candidate JSONL entry in {path}."))
            .ToArray();
        return new LoadedTruthCandidateFile(path, candidates);
    }

    private static ScalarRecoveredTruthCandidate BuildRecoveredCandidate(IReadOnlyList<TruthCandidateEntry> entries)
    {
        var best = entries
            .OrderByDescending(entry => entry.Candidate.ScoreTotal)
            .ThenBy(entry => entry.Candidate.CandidateId, StringComparer.OrdinalIgnoreCase)
            .First()
            .Candidate;
        var labels = entries
            .SelectMany(entry => entry.Candidate.LabelsPresent)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var supportingReasons = entries
            .SelectMany(entry => entry.Candidate.SupportingReasons)
            .Append("repeated_truth_candidate_match")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ScalarRecoveredTruthCandidate
        {
            BaseAddressHex = best.BaseAddressHex,
            OffsetHex = best.OffsetHex,
            DataType = best.DataType,
            ValueFamily = best.ValueFamily,
            Classification = best.Classification,
            SupportingTruthCandidateIds = entries
                .Select(entry => $"{Path.GetFileName(entry.Path)}:{entry.Candidate.CandidateId}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SupportingFileCount = entries.Select(entry => entry.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            BestScoreTotal = entries.Max(entry => entry.Candidate.ScoreTotal),
            LabelsPresent = labels,
            SupportingReasons = supportingReasons,
            EvidenceSummary = $"matched_files={entries.Select(entry => entry.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()};best_score={entries.Max(entry => entry.Candidate.ScoreTotal):F3};labels={string.Join(",", labels)}"
        };
    }

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<ScalarRecoveredTruthCandidate> recovered)
    {
        var warnings = new List<string> { "scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth" };
        if (recovered.Count == 0)
        {
            warnings.Add("no_recovered_scalar_truth_candidates");
        }

        return warnings;
    }

    private static string RecoveryKey(ScalarTruthCandidate candidate) =>
        string.Join("|", candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, candidate.Classification);

    private sealed record LoadedTruthCandidateFile(string Path, IReadOnlyList<ScalarTruthCandidate> Candidates);

    private sealed record TruthCandidateEntry(string Path, ScalarTruthCandidate Candidate);
}
