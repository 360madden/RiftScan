namespace RiftScan.Core.Sessions;

public sealed class SessionPruneService
{
    private static readonly IReadOnlyDictionary<string, string> GeneratedArtifactReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["triage.jsonl"] = "generated_analysis_artifact",
        ["deltas.jsonl"] = "generated_analysis_artifact",
        ["typed_value_candidates.jsonl"] = "generated_analysis_artifact",
        ["structures.jsonl"] = "generated_analysis_artifact",
        ["vec3_candidates.jsonl"] = "generated_analysis_artifact",
        ["clusters.jsonl"] = "generated_analysis_artifact",
        ["candidates.jsonl"] = "generated_analysis_artifact",
        ["next_capture_plan.json"] = "generated_followup_plan",
        ["report.md"] = "generated_report",
        ["report.json"] = "generated_report"
    };

    public SessionPruneResult Prune(string sessionPath, bool dryRun = true, string? inventoryOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        var fullSessionPath = Path.GetFullPath(sessionPath);
        if (!dryRun)
        {
            return WriteInventoryIfRequested(CreateResult(
                fullSessionPath,
                dryRun,
                candidates: [],
                issues:
                [
                    Error(
                        "prune_apply_not_supported",
                        "Session prune currently supports --dry-run only; deletion is intentionally disabled until an explicit apply contract exists.",
                        null)
                ]), inventoryOutputPath);
        }

        if (!Directory.Exists(fullSessionPath))
        {
            return WriteInventoryIfRequested(CreateResult(
                fullSessionPath,
                dryRun,
                candidates: [],
                issues:
                [
                    Error("session_root_missing", "Session directory does not exist.", fullSessionPath)
                ]), inventoryOutputPath);
        }

        var candidates = GeneratedArtifactReasons
            .Select(pair => CreateCandidate(fullSessionPath, pair.Key, pair.Value))
            .Where(candidate => candidate is not null)
            .Cast<SessionPruneCandidate>()
            .OrderBy(candidate => candidate.Path, StringComparer.Ordinal)
            .ToArray();

        return WriteInventoryIfRequested(CreateResult(fullSessionPath, dryRun, candidates, issues: []), inventoryOutputPath);
    }

    private static SessionPruneCandidate? CreateCandidate(string sessionPath, string relativePath, string reason)
    {
        var absolutePath = Path.Combine(sessionPath, relativePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return new SessionPruneCandidate
        {
            Path = relativePath.Replace('\\', '/'),
            Bytes = new FileInfo(absolutePath).Length,
            Reason = reason
        };
    }


    private static SessionPruneResult WriteInventoryIfRequested(SessionPruneResult result, string? inventoryOutputPath)
    {
        if (string.IsNullOrWhiteSpace(inventoryOutputPath))
        {
            return result;
        }

        var fullInventoryPath = Path.GetFullPath(inventoryOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullInventoryPath) ?? ".");
        var resultWithInventoryPath = result with { InventoryPath = fullInventoryPath };
        File.WriteAllText(fullInventoryPath, System.Text.Json.JsonSerializer.Serialize(resultWithInventoryPath, SessionJson.Options));
        return resultWithInventoryPath;
    }

    private static SessionPruneResult CreateResult(
        string sessionPath,
        bool dryRun,
        IReadOnlyList<SessionPruneCandidate> candidates,
        IReadOnlyList<VerificationIssue> issues) =>
        new()
        {
            SessionPath = sessionPath,
            DryRun = dryRun,
            CandidateCount = candidates.Count,
            BytesReclaimable = candidates.Sum(candidate => candidate.Bytes),
            Candidates = candidates,
            Issues = issues
        };

    private static VerificationIssue Error(string code, string message, string? path) =>
        new()
        {
            Severity = "error",
            Code = code,
            Message = message,
            Path = path
        };
}
