using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionMigrationService
{
    public const string SupportedSessionSchemaVersion = "riftscan.session.v1";

    public SessionMigrationResult Migrate(string sessionPath, string toSchemaVersion, bool dryRun = true, string? planOutputPath = null)
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
        if (!dryRun)
        {
            var applyResult = CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun,
                status: "apply_not_supported",
                issues:
                [
                    Error(
                        "apply_not_supported",
                        "Session migration apply is not implemented yet. Re-run with --dry-run and --plan-out to produce a non-mutating plan.",
                        null)
                ]);

            return WritePlanIfRequested(applyResult, planOutputPath);
        }

        if (!string.Equals(manifest.SchemaVersion, SupportedSessionSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            var unsupportedSourceResult = CreateResult(
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

            return WritePlanIfRequested(unsupportedSourceResult, planOutputPath);
        }

        if (!string.Equals(toSchemaVersion, SupportedSessionSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            var unsupportedTargetResult = CreateResult(
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

            return WritePlanIfRequested(unsupportedTargetResult, planOutputPath);
        }

        var result = CreateResult(
            fullSessionPath,
            manifest.SessionId,
            manifest.SchemaVersion,
            toSchemaVersion,
            dryRun,
            status: "noop_current_schema",
            issues: []);

        return WritePlanIfRequested(result, planOutputPath);
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

    private static SessionMigrationResult WritePlanIfRequested(SessionMigrationResult result, string? planOutputPath)
    {
        if (string.IsNullOrWhiteSpace(planOutputPath))
        {
            return result;
        }

        var fullPlanPath = Path.GetFullPath(planOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPlanPath) ?? ".");
        var plan = new SessionMigrationPlan
        {
            SourceSessionPath = result.SessionPath,
            SessionId = result.SessionId,
            FromSchemaVersion = result.FromSchemaVersion,
            ToSchemaVersion = result.ToSchemaVersion,
            DryRun = result.DryRun,
            Status = result.Status,
            CanApply = false,
            Actions = CreatePlanActions(result)
        };

        File.WriteAllText(fullPlanPath, JsonSerializer.Serialize(plan, SessionJson.Options));
        return result with { ArtifactsWritten = [.. result.ArtifactsWritten, fullPlanPath] };
    }

    private static IReadOnlyList<SessionMigrationPlanAction> CreatePlanActions(SessionMigrationResult result) =>
        result.Status switch
        {
            "noop_current_schema" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "verify-current-schema-noop",
                    ActionType = "noop",
                    Description = "Source session already uses the supported schema; no raw or generated session artifacts need migration.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "unsupported_source_schema" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "define-source-schema-migrator",
                    ActionType = "blocked",
                    Description = "No source-schema migrator is registered for this session schema. Add an explicit fixture-backed migrator before applying changes.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "unsupported_target_schema" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "define-target-schema-contract",
                    ActionType = "blocked",
                    Description = "No target-schema contract is registered for the requested schema. Define and test the target schema before applying changes.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "apply_not_supported" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "implement-apply-path",
                    ActionType = "blocked",
                    Description = "Apply mode is intentionally disabled until a fixture-backed migrator can write migrated generated artifacts without mutating raw evidence.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            _ => []
        };
}
