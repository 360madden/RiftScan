using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Triage;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Reports;

public sealed record SessionReportResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("report_path")]
    public string ReportPath { get; init; } = string.Empty;
}

public sealed class SessionReportGenerator
{
    public SessionReportResult Generate(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before report generation: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var triagePath = ResolveSessionPath(fullSessionPath, "triage.jsonl");
        if (!File.Exists(triagePath))
        {
            _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(fullSessionPath, top);
        }

        var triageEntries = ReadTriageEntries(fullSessionPath)
            .OrderByDescending(entry => entry.RankScore)
            .ThenBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var structureCandidates = ReadStructureCandidates(fullSessionPath)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();

        var reportPath = ResolveSessionPath(fullSessionPath, "report.md");
        File.WriteAllLines(reportPath, BuildReport(manifest, triageEntries, structureCandidates));

        return new SessionReportResult { Success = true, SessionPath = fullSessionPath, ReportPath = reportPath };
    }

    private static IEnumerable<string> BuildReport(SessionManifest manifest, IReadOnlyList<RegionTriageEntry> triageEntries, IReadOnlyList<StructureCandidate> structureCandidates)
    {
        yield return $"# RiftScan Session Report - {manifest.SessionId}";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"- Process: `{manifest.ProcessName}` PID `{manifest.ProcessId}`";
        yield return $"- Capture mode: `{manifest.CaptureMode}`";
        yield return $"- Snapshots: `{manifest.SnapshotCount}`";
        yield return $"- Regions: `{manifest.RegionCount}`";
        yield return $"- Bytes stored: `{manifest.TotalBytesStored}`";
        yield return string.Empty;
        yield return "## Dynamic region triage";
        yield return string.Empty;
        yield return "| Rank | Region | Score | Snapshots | Unique hashes | Entropy | Zero ratio | Recommendation |";
        yield return "|---:|---|---:|---:|---:|---:|---:|---|";

        var rank = 1;
        foreach (var entry in triageEntries)
        {
            yield return $"| {rank} | `{entry.RegionId}` | {entry.RankScore:F3} | {entry.SnapshotCount} | {entry.UniqueChecksumCount} | {entry.ByteEntropy:F3} | {entry.ZeroByteRatio:F3} | `{entry.Recommendation}` |";
            rank++;
        }

        yield return string.Empty;
        yield return "## Structure candidates";
        yield return string.Empty;
        yield return "| Rank | Region | Offset | Score | Support | Kind | Preview |";
        yield return "|---:|---|---:|---:|---:|---|---|";

        var structureRank = 1;
        foreach (var candidate in structureCandidates)
        {
            yield return $"| {structureRank} | `{candidate.RegionId}` | `{candidate.OffsetHex}` | {candidate.Score:F3} | {candidate.SnapshotSupport} | `{candidate.StructureKind}` | `{string.Join(", ", candidate.ValuePreview.Select(value => value.ToString("G6")))}` |";
            structureRank++;
        }

        if (structureCandidates.Count == 0)
        {
            yield return "| 0 | none | - | 0 | 0 | - | - |";
        }

        yield return string.Empty;
        yield return "## Next smallest action";
        yield return string.Empty;
        yield return triageEntries.Any(entry => entry.SnapshotCount < 2)
            ? "Capture at least two samples before making dynamic or behavior claims."
            : "Review regions with checksum changes and structure candidates before adding cluster detection.";
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<RegionTriageEntry> ReadTriageEntries(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "triage.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RegionTriageEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid triage entry."))
            .ToArray();
    }

    private static IReadOnlyList<StructureCandidate> ReadStructureCandidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "structures.jsonl");
        if (!File.Exists(path))
        {
            _ = new FloatTripletStructureAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StructureCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid structure candidate."))
            .ToArray();
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
