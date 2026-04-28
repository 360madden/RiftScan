using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Deltas;
using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Values;
using RiftScan.Analysis.Vectors;
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

    [JsonPropertyName("report_json_path")]
    public string ReportJsonPath { get; init; } = string.Empty;
}

internal sealed record SessionMachineReport
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.session_report.v1";

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("markdown_report_path")]
    public string MarkdownReportPath { get; init; } = string.Empty;

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("capture_mode")]
    public string CaptureMode { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("region_count")]
    public int RegionCount { get; init; }

    [JsonPropertyName("total_bytes_stored")]
    public long TotalBytesStored { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("artifact_counts")]
    public SessionReportArtifactCounts ArtifactCounts { get; init; } = new();

    [JsonPropertyName("capture_interruption")]
    public CaptureInterventionHandoffReport? CaptureInterruption { get; init; }
}

internal sealed record SessionReportArtifactCounts
{
    [JsonPropertyName("triage_entries")]
    public int TriageEntries { get; init; }

    [JsonPropertyName("delta_entries")]
    public int DeltaEntries { get; init; }

    [JsonPropertyName("typed_value_candidates")]
    public int TypedValueCandidates { get; init; }

    [JsonPropertyName("vec3_candidates")]
    public int Vec3Candidates { get; init; }

    [JsonPropertyName("structure_clusters")]
    public int StructureClusters { get; init; }

    [JsonPropertyName("structure_candidates")]
    public int StructureCandidates { get; init; }
}

internal sealed record CaptureInterventionHandoffReport
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("elapsed_ms")]
    public long ElapsedMilliseconds { get; init; }

    [JsonPropertyName("samples_targeted")]
    public int SamplesTargeted { get; init; }

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; init; } = string.Empty;

    [JsonPropertyName("region_read_failures")]
    public IReadOnlyList<CaptureInterventionRegionReadFailureReport> RegionReadFailures { get; init; } = [];
}

internal sealed record CaptureInterventionRegionReadFailureReport
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("requested_bytes")]
    public int RequestedBytes { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
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
        if (!File.Exists(ResolveSessionPath(fullSessionPath, "triage.jsonl")))
        {
            _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(fullSessionPath, top);
        }

        var triageEntries = ReadTriageEntries(fullSessionPath)
            .OrderByDescending(entry => entry.RankScore)
            .ThenBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var deltaEntries = ReadDeltaEntries(fullSessionPath)
            .OrderByDescending(entry => entry.RankScore)
            .ThenBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var valueCandidates = ReadValueCandidates(fullSessionPath)
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var structureCandidates = ReadStructureCandidates(fullSessionPath)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var vec3Candidates = ReadVec3Candidates(fullSessionPath)
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var clusters = ReadClusters(fullSessionPath)
            .OrderByDescending(cluster => cluster.RankScore)
            .ThenBy(cluster => cluster.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(cluster => cluster.StartOffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();

        var interventionHandoff = ReadOptionalJson<CaptureInterventionHandoffReport>(fullSessionPath, "intervention_handoff.json");

        var reportPath = ResolveSessionPath(fullSessionPath, "report.md");
        File.WriteAllLines(reportPath, BuildReport(manifest, interventionHandoff, triageEntries, deltaEntries, valueCandidates, vec3Candidates, clusters, structureCandidates));
        var reportJsonPath = ResolveSessionPath(fullSessionPath, "report.json");
        var machineReport = BuildMachineReport(fullSessionPath, reportPath, top, manifest, interventionHandoff, triageEntries, deltaEntries, valueCandidates, vec3Candidates, clusters, structureCandidates);
        File.WriteAllText(reportJsonPath, JsonSerializer.Serialize(machineReport, SessionJson.Options));

        return new SessionReportResult { Success = true, SessionPath = fullSessionPath, ReportPath = reportPath, ReportJsonPath = reportJsonPath };
    }

    private static SessionMachineReport BuildMachineReport(
        string sessionPath,
        string reportPath,
        int top,
        SessionManifest manifest,
        CaptureInterventionHandoffReport? interventionHandoff,
        IReadOnlyList<RegionTriageEntry> triageEntries,
        IReadOnlyList<RegionDeltaEntry> deltaEntries,
        IReadOnlyList<TypedValueCandidate> valueCandidates,
        IReadOnlyList<Vec3Candidate> vec3Candidates,
        IReadOnlyList<StructureCluster> clusters,
        IReadOnlyList<StructureCandidate> structureCandidates) =>
        new()
        {
            SessionId = manifest.SessionId,
            SessionPath = sessionPath,
            MarkdownReportPath = reportPath,
            ProcessName = manifest.ProcessName,
            ProcessId = manifest.ProcessId,
            CaptureMode = manifest.CaptureMode,
            Status = manifest.Status,
            SnapshotCount = manifest.SnapshotCount,
            RegionCount = manifest.RegionCount,
            TotalBytesStored = manifest.TotalBytesStored,
            TopLimit = top,
            ArtifactCounts = new SessionReportArtifactCounts
            {
                TriageEntries = triageEntries.Count,
                DeltaEntries = deltaEntries.Count,
                TypedValueCandidates = valueCandidates.Count,
                Vec3Candidates = vec3Candidates.Count,
                StructureClusters = clusters.Count,
                StructureCandidates = structureCandidates.Count
            },
            CaptureInterruption = interventionHandoff
        };

    private static IEnumerable<string> BuildReport(
        SessionManifest manifest,
        CaptureInterventionHandoffReport? interventionHandoff,
        IReadOnlyList<RegionTriageEntry> triageEntries,
        IReadOnlyList<RegionDeltaEntry> deltaEntries,
        IReadOnlyList<TypedValueCandidate> valueCandidates,
        IReadOnlyList<Vec3Candidate> vec3Candidates,
        IReadOnlyList<StructureCluster> clusters,
        IReadOnlyList<StructureCandidate> structureCandidates)
    {
        yield return $"# RiftScan Session Report - {manifest.SessionId}";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"- Process: `{manifest.ProcessName}` PID `{manifest.ProcessId}`";
        yield return $"- Capture mode: `{manifest.CaptureMode}`";
        yield return $"- Status: `{manifest.Status}`";
        if (interventionHandoff is not null)
        {
            yield return $"- Elapsed: `{interventionHandoff.ElapsedMilliseconds}` ms";
        }

        yield return $"- Snapshots: `{manifest.SnapshotCount}`";
        yield return $"- Regions: `{manifest.RegionCount}`";
        yield return $"- Bytes stored: `{manifest.TotalBytesStored}`";
        if (interventionHandoff is not null)
        {
            yield return string.Empty;
            yield return "## Capture interruption";
            yield return string.Empty;
            yield return $"- Reason: `{interventionHandoff.Reason}`";
            yield return $"- Elapsed: `{interventionHandoff.ElapsedMilliseconds}` ms";
            yield return $"- Samples targeted: `{interventionHandoff.SamplesTargeted}`";
            yield return $"- Recommended next action: `{interventionHandoff.RecommendedNextAction}`";
            yield return string.Empty;
            yield return "| Region | Base address | Requested bytes | Failure reason |";
            yield return "|---|---:|---:|---|";

            foreach (var failure in interventionHandoff.RegionReadFailures)
            {
                yield return $"| `{failure.RegionId}` | `{failure.BaseAddressHex}` | {failure.RequestedBytes} | `{failure.Reason}` |";
            }

            if (interventionHandoff.RegionReadFailures.Count == 0)
            {
                yield return "| none | - | 0 | - |";
            }
        }

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
        yield return "## Dynamic byte deltas";
        yield return string.Empty;
        yield return "| Rank | Region | Score | Changed bytes | Changed ratio | Ranges | Recommendation |";
        yield return "|---:|---|---:|---:|---:|---|---|";

        var deltaRank = 1;
        foreach (var entry in deltaEntries)
        {
            var ranges = entry.ChangedRanges.Count == 0
                ? "-"
                : string.Join(", ", entry.ChangedRanges.Take(3).Select(range => $"{range.StartOffsetHex}-{range.EndOffsetHex}"));
            yield return $"| {deltaRank} | `{entry.RegionId}` | {entry.RankScore:F3} | {entry.ChangedByteCount} | {entry.ChangedByteRatio:F6} | `{ranges}` | `{entry.Recommendation}` |";
            deltaRank++;
        }

        if (deltaEntries.Count == 0)
        {
            yield return "| 0 | none | 0 | 0 | 0 | - | - |";
        }

        yield return string.Empty;
        yield return "## Typed value lanes";
        yield return string.Empty;
        yield return "| Rank | Candidate | Region | Offset | Type | Score | Distinct | Preview | Recommendation |";
        yield return "|---:|---|---|---:|---|---:|---:|---|---|";

        var valueRank = 1;
        foreach (var candidate in valueCandidates)
        {
            yield return $"| {valueRank} | `{candidate.CandidateId}` | `{candidate.RegionId}` | `{candidate.OffsetHex}` | `{candidate.DataType}` | {candidate.RankScore:F3} | {candidate.DistinctValueCount} | `{string.Join(", ", candidate.ValuePreview.Take(4))}` | `{candidate.Recommendation}` |";
            valueRank++;
        }

        if (valueCandidates.Count == 0)
        {
            yield return "| 0 | none | - | - | - | 0 | 0 | - | - |";
        }

        yield return string.Empty;
        yield return "## Vec3 candidates";
        yield return string.Empty;
        yield return "| Rank | Candidate | Region | Offset | Score | Behavior | Stimulus | Support | Preview | Recommendation |";
        yield return "|---:|---|---|---:|---:|---:|---|---:|---|---|";

        var vec3Rank = 1;
        foreach (var candidate in vec3Candidates)
        {
            var stimulus = string.IsNullOrWhiteSpace(candidate.StimulusLabel) ? "-" : candidate.StimulusLabel;
            yield return $"| {vec3Rank} | `{candidate.CandidateId}` | `{candidate.RegionId}` | `{candidate.OffsetHex}` | {candidate.RankScore:F3} | {candidate.BehaviorScore:F3} | `{stimulus}` | {candidate.SnapshotSupport} | `{string.Join(", ", candidate.ValuePreview.Select(value => value.ToString("G6")))}` | `{candidate.Recommendation}` |";
            vec3Rank++;
        }

        if (vec3Candidates.Count == 0)
        {
            yield return "| 0 | none | - | - | 0 | 0 | - | 0 | - | - |";
        }

        yield return string.Empty;
        yield return "## Structure clusters";
        yield return string.Empty;
        yield return "| Rank | Cluster | Region | Span | Candidates | Score | Recommendation |";
        yield return "|---:|---|---|---:|---:|---:|---|";

        var clusterRank = 1;
        foreach (var cluster in clusters)
        {
            yield return $"| {clusterRank} | `{cluster.ClusterId}` | `{cluster.RegionId}` | `{cluster.StartOffsetHex}-{cluster.EndOffsetHex}` | {cluster.CandidateCount} | {cluster.RankScore:F3} | `{cluster.Recommendation}` |";
            clusterRank++;
        }

        if (clusters.Count == 0)
        {
            yield return "| 0 | none | - | - | 0 | 0 | - |";
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
        yield return clusters.Count > 0
            ? "Review top structure clusters across a larger passive capture before player-specific matching."
            : "Capture more samples or wider regions before making layout claims.";
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static T? ReadOptionalJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            : default;
    }

    private static IReadOnlyList<RegionTriageEntry> ReadTriageEntries(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "triage.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RegionTriageEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid triage entry."))
            .ToArray();
    }

    private static IReadOnlyList<RegionDeltaEntry> ReadDeltaEntries(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "deltas.jsonl");
        if (!File.Exists(path))
        {
            _ = new ByteDeltaAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RegionDeltaEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid delta entry."))
            .ToArray();
    }

    private static IReadOnlyList<TypedValueCandidate> ReadValueCandidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "typed_value_candidates.jsonl");
        if (!File.Exists(path))
        {
            _ = new TypedValueLaneAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<TypedValueCandidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid typed value candidate."))
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

    private static IReadOnlyList<Vec3Candidate> ReadVec3Candidates(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "vec3_candidates.jsonl");
        if (!File.Exists(path))
        {
            _ = new Vec3CandidateAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Vec3Candidate>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid vec3 candidate."))
            .ToArray();
    }

    private static IReadOnlyList<StructureCluster> ReadClusters(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "clusters.jsonl");
        if (!File.Exists(path))
        {
            _ = new StructureClusterAnalyzer().AnalyzeSession(sessionPath);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StructureCluster>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid structure cluster."))
            .ToArray();
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
