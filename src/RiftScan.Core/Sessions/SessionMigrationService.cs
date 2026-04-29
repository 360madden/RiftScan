using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionMigrationService
{
    public const string SupportedSessionSchemaVersion = "riftscan.session.v1";

    public SessionMigrationResult Migrate(string sessionPath, string toSchemaVersion, bool dryRun = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(toSchemaVersion);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            return CreateResult(
                fullSessionPath,
                verification.SessionId,
                fromSchemaVersion: null,
                toSchemaVersion,
                dryRun,
                status: "verification_failed",
                issues: verification.Issues);
        }

        var manifest = ReadManifest(fullSessionPath);
        if (!string.Equals(manifest.SchemaVersion, SupportedSessionSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun,
                status: "unsupported_source_schema",
                issues:
                [
                    Error(
                        "unsupported_source_schema",
                        $"Unsupported source session schema_version '{manifest.SchemaVersion}'. Expected '{SupportedSessionSchemaVersion}'.",
                        "manifest.json")
                ]);
        }

        if (!string.Equals(toSchemaVersion, SupportedSessionSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun,
                status: "unsupported_target_schema",
                issues:
                [
                    Error(
                        "unsupported_target_schema",
                        $"Unsupported target session schema_version '{toSchemaVersion}'. Expected '{SupportedSessionSchemaVersion}'.",
                        null)
                ]);
        }

        return CreateResult(
            fullSessionPath,
            manifest.SessionId,
            manifest.SchemaVersion,
            toSchemaVersion,
            dryRun,
            status: "noop_current_schema",
            issues: []);
    }

    private static SessionMigrationResult CreateResult(
        string sessionPath,
        string? sessionId,
        string? fromSchemaVersion,
        string toSchemaVersion,
        bool dryRun,
        string status,
        IReadOnlyList<VerificationIssue> issues) =>
        new()
        {
            SessionPath = sessionPath,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId,
            FromSchemaVersion = string.IsNullOrWhiteSpace(fromSchemaVersion) ? null : fromSchemaVersion,
            ToSchemaVersion = toSchemaVersion,
            DryRun = dryRun,
            Status = status,
            ArtifactsWritten = [],
            Issues = issues
        };

    private static SessionManifest ReadManifest(string sessionPath)
    {
        var manifestPath = Path.Combine(sessionPath, "manifest.json");
        return JsonSerializer.Deserialize<SessionManifest>(File.ReadAllText(manifestPath), SessionJson.Options)
            ?? throw new InvalidOperationException("manifest.json did not contain a valid session manifest object.");
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
