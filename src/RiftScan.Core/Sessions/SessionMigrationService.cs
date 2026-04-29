using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionMigrationService
{
    public const string LegacySessionSchemaVersionV0 = "riftscan.session.v0";
    public const string SupportedSessionSchemaVersion = "riftscan.session.v1";

    public SessionMigrationResult Migrate(
        string sessionPath,
        string toSchemaVersion,
        bool dryRun = true,
        string? planOutputPath = null,
        string? migrationOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(toSchemaVersion);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var verificationFailedResult = CreateResult(
                fullSessionPath,
                verification.SessionId,
                fromSchemaVersion: null,
                toSchemaVersion,
                dryRun,
                status: "verification_failed",
                issues: verification.Issues);

            return WritePlanIfRequested(verificationFailedResult, planOutputPath);
        }

        var manifest = ReadManifest(fullSessionPath);
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

        if (!dryRun)
        {
            if (string.Equals(manifest.SchemaVersion, LegacySessionSchemaVersionV0, StringComparison.OrdinalIgnoreCase))
            {
                return ApplyV0ToV1(fullSessionPath, manifest, toSchemaVersion, planOutputPath, migrationOutputPath);
            }

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
                        "Session migration apply is not implemented for this source schema. Re-run with --dry-run and --plan-out to produce a non-mutating plan.",
                        null)
                ]);

            return WritePlanIfRequested(applyResult, planOutputPath);
        }

        if (string.Equals(manifest.SchemaVersion, LegacySessionSchemaVersionV0, StringComparison.OrdinalIgnoreCase))
        {
            var plannedUpgradeResult = CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun,
                status: "planned_source_schema_upgrade",
                issues: []);

            return WritePlanIfRequested(plannedUpgradeResult, planOutputPath);
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
                        $"Unsupported source session schema_version '{manifest.SchemaVersion}'. Expected '{SupportedSessionSchemaVersion}' or '{LegacySessionSchemaVersionV0}'.",
                        "manifest.json")
                ]);

            return WritePlanIfRequested(unsupportedSourceResult, planOutputPath);
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

    private static SessionMigrationResult ApplyV0ToV1(
        string fullSessionPath,
        SessionManifest manifest,
        string toSchemaVersion,
        string? planOutputPath,
        string? migrationOutputPath)
    {
        if (string.IsNullOrWhiteSpace(migrationOutputPath))
        {
            var outputRequiredResult = CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun: false,
                status: "apply_output_required",
                issues:
                [
                    Error(
                        "apply_output_required",
                        "Applying a session migration requires --out <new-session-path>; in-place migration is forbidden.",
                        null)
                ]);

            return WritePlanIfRequested(outputRequiredResult, planOutputPath);
        }

        var fullOutputPath = Path.GetFullPath(migrationOutputPath);
        if (Directory.Exists(fullOutputPath) && Directory.EnumerateFileSystemEntries(fullOutputPath).Any())
        {
            var outputExistsResult = CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun: false,
                status: "apply_output_exists",
                issues:
                [
                    Error(
                        "apply_output_exists",
                        "Migration output directory already exists and is not empty.",
                        fullOutputPath)
                ]);

            return WritePlanIfRequested(outputExistsResult with { MigrationOutputPath = fullOutputPath }, planOutputPath);
        }

        CopyDirectory(fullSessionPath, fullOutputPath);
        RewriteManifestSchemaVersion(fullOutputPath, toSchemaVersion);
        var artifactsWritten = new[]
        {
            fullOutputPath,
            Path.Combine(fullOutputPath, "manifest.json"),
            Path.Combine(fullOutputPath, "checksums.json")
        };

        var verification = new SessionVerifier().Verify(fullOutputPath);
        if (!verification.Success)
        {
            var verificationFailedResult = CreateResult(
                fullSessionPath,
                manifest.SessionId,
                manifest.SchemaVersion,
                toSchemaVersion,
                dryRun: false,
                status: "apply_verification_failed",
                issues: verification.Issues) with
            {
                MigrationOutputPath = fullOutputPath,
                ArtifactsWritten = artifactsWritten
            };

            return WritePlanIfRequested(verificationFailedResult, planOutputPath);
        }

        var appliedResult = CreateResult(
            fullSessionPath,
            manifest.SessionId,
            manifest.SchemaVersion,
            toSchemaVersion,
            dryRun: false,
            status: "applied_source_schema_upgrade",
            issues: []) with
        {
            MigrationOutputPath = fullOutputPath,
            ArtifactsWritten = artifactsWritten
        };

        return WritePlanIfRequested(appliedResult, planOutputPath);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile);
        }
    }

    private static void RewriteManifestSchemaVersion(string sessionPath, string schemaVersion)
    {
        var manifestPath = Path.Combine(sessionPath, "manifest.json");
        var manifest = JsonSerializer.Deserialize<SessionManifest>(File.ReadAllText(manifestPath), SessionJson.Options)
            ?? throw new InvalidOperationException("manifest.json did not contain a valid session manifest object.");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest with { SchemaVersion = schemaVersion }, SessionJson.Options));

        var checksumsPath = Path.Combine(sessionPath, "checksums.json");
        var checksums = JsonSerializer.Deserialize<ChecksumManifest>(File.ReadAllText(checksumsPath), SessionJson.Options)
            ?? throw new InvalidOperationException("checksums.json did not contain a valid checksum manifest object.");
        var manifestInfo = new FileInfo(manifestPath);
        var updatedEntries = checksums.Entries
            .Select(entry => string.Equals(entry.Path, "manifest.json", StringComparison.OrdinalIgnoreCase)
                ? entry with { Sha256Hex = SessionChecksum.ComputeSha256Hex(manifestPath), Bytes = manifestInfo.Length }
                : entry)
            .ToArray();

        File.WriteAllText(checksumsPath, JsonSerializer.Serialize(checksums with { Entries = updatedEntries }, SessionJson.Options));
    }

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
            "planned_source_schema_upgrade" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "copy-session-artifacts-to-migrated-output",
                    ActionType = "planned",
                    Description = "Copy session artifacts to a future migrated output directory; never mutate the source session in place.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                },
                new SessionMigrationPlanAction
                {
                    ActionId = "rewrite-generated-manifest-schema-version",
                    ActionType = "planned",
                    Description = "Rewrite the generated manifest copy from riftscan.session.v0 to riftscan.session.v1.",
                    WritesRawArtifacts = false,
                    TargetPath = "manifest.json"
                },
                new SessionMigrationPlanAction
                {
                    ActionId = "recompute-generated-checksums",
                    ActionType = "planned",
                    Description = "Recompute checksums for generated migrated artifacts after manifest rewrite.",
                    WritesRawArtifacts = false,
                    TargetPath = "checksums.json"
                }
            ],
            "applied_source_schema_upgrade" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "wrote-migrated-session-output",
                    ActionType = "applied",
                    Description = "Wrote a migrated session copy to a separate output directory and verified the migrated artifacts.",
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
            "apply_output_required" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "provide-migration-output-directory",
                    ActionType = "blocked",
                    Description = "Provide --out <new-session-path> so apply mode can write a separate migrated copy instead of mutating the source session.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "apply_output_exists" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "choose-empty-migration-output-directory",
                    ActionType = "blocked",
                    Description = "Choose a missing or empty output directory for migrated artifacts.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "apply_verification_failed" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "repair-generated-migration-output",
                    ActionType = "blocked",
                    Description = "Generated migrated output failed verification and must not be promoted.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            "verification_failed" =>
            [
                new SessionMigrationPlanAction
                {
                    ActionId = "repair-session-integrity",
                    ActionType = "blocked",
                    Description = "Session verification failed. Preserve the original artifacts and fix or recapture the session before migration.",
                    WritesRawArtifacts = false,
                    TargetPath = null
                }
            ],
            _ => []
        };
}
