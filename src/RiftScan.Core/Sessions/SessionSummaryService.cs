using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionSummaryService
{
    private static readonly IReadOnlyDictionary<string, string> KnownGeneratedArtifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["triage.jsonl"] = "analysis",
        ["deltas.jsonl"] = "analysis",
        ["typed_value_candidates.jsonl"] = "analysis",
        ["structures.jsonl"] = "analysis",
        ["vec3_candidates.jsonl"] = "analysis",
        ["clusters.jsonl"] = "analysis",
        ["candidates.jsonl"] = "analysis",
        ["next_capture_plan.json"] = "plan",
        ["report.md"] = "report",
        ["report.json"] = "report",
        ["intervention_handoff.json"] = "capture_handoff"
    };

    public SessionSummaryResult Summarize(string sessionPath, string? summaryOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        var fullSessionPath = Path.GetFullPath(sessionPath);
        if (!Directory.Exists(fullSessionPath))
        {
            return WriteSummaryIfRequested(new SessionSummaryResult
            {
                SessionPath = fullSessionPath,
                Issues =
                [
                    Error("session_root_missing", "Session directory does not exist.", fullSessionPath)
                ]
            }, summaryOutputPath);
        }

        var verification = new SessionVerifier().Verify(fullSessionPath);
        var manifest = ReadManifestIfPresent(fullSessionPath);
        var generatedArtifacts = KnownGeneratedArtifacts
            .Select(pair => CreateArtifact(fullSessionPath, pair.Key, pair.Value))
            .Where(artifact => artifact is not null)
            .Cast<SessionSummaryArtifact>()
            .OrderBy(artifact => artifact.Path, StringComparer.Ordinal)
            .ToArray();

        return WriteSummaryIfRequested(new SessionSummaryResult
        {
            SessionPath = fullSessionPath,
            SessionId = manifest?.SessionId ?? verification.SessionId,
            SchemaVersion = manifest?.SchemaVersion,
            ProcessName = manifest?.ProcessName,
            CaptureMode = manifest?.CaptureMode,
            Status = manifest?.Status,
            SnapshotCount = manifest?.SnapshotCount ?? 0,
            RegionCount = manifest?.RegionCount ?? 0,
            TotalBytesRaw = manifest?.TotalBytesRaw ?? 0,
            TotalBytesStored = manifest?.TotalBytesStored ?? 0,
            ArtifactCount = generatedArtifacts.Length,
            ArtifactBytes = generatedArtifacts.Sum(artifact => artifact.Bytes),
            GeneratedArtifacts = generatedArtifacts,
            Issues = verification.Issues
        }, summaryOutputPath);
    }

    private static SessionSummaryResult WriteSummaryIfRequested(SessionSummaryResult result, string? summaryOutputPath)
    {
        if (string.IsNullOrWhiteSpace(summaryOutputPath))
        {
            return result;
        }

        var fullSummaryPath = Path.GetFullPath(summaryOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullSummaryPath) ?? ".");
        var resultWithSummaryPath = result with { SummaryPath = fullSummaryPath };
        File.WriteAllText(fullSummaryPath, JsonSerializer.Serialize(resultWithSummaryPath, SessionJson.Options));
        return resultWithSummaryPath;
    }

    private static SessionManifest? ReadManifestIfPresent(string sessionPath)
    {
        var manifestPath = Path.Combine(sessionPath, "manifest.json");
        return File.Exists(manifestPath)
            ? JsonSerializer.Deserialize<SessionManifest>(File.ReadAllText(manifestPath), SessionJson.Options)
            : null;
    }

    private static SessionSummaryArtifact? CreateArtifact(string sessionPath, string relativePath, string kind)
    {
        var absolutePath = Path.Combine(sessionPath, relativePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return new SessionSummaryArtifact
        {
            Path = relativePath.Replace('\\', '/'),
            Bytes = new FileInfo(absolutePath).Length,
            Kind = kind
        };
    }

    private static VerificationIssue Error(string code, string message, string? path) =>
        new()
        {
            Severity = "error",
            Code = code,
            Message = message,
            Path = path
        };
}
