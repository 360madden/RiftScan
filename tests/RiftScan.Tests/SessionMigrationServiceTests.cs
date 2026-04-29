using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionMigrationServiceTests
{
    [Fact]
    public void Migrate_current_schema_dry_run_is_noop_and_writes_no_artifacts()
    {
        var result = new SessionMigrationService().Migrate(
            ValidFixturePath,
            SessionMigrationService.SupportedSessionSchemaVersion);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("fixture-valid-session", result.SessionId);
        Assert.Equal(SessionMigrationService.SupportedSessionSchemaVersion, result.FromSchemaVersion);
        Assert.Equal(SessionMigrationService.SupportedSessionSchemaVersion, result.ToSchemaVersion);
        Assert.True(result.DryRun);
        Assert.Equal("noop_current_schema", result.Status);
        Assert.Empty(result.ArtifactsWritten);
    }

    [Fact]
    public void Migrate_unsupported_target_schema_fails_without_writing_artifacts()
    {
        var result = new SessionMigrationService().Migrate(ValidFixturePath, "riftscan.session.v2");

        Assert.False(result.Success);
        Assert.Equal("unsupported_target_schema", result.Status);
        Assert.Empty(result.ArtifactsWritten);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported_target_schema");
    }

    [Fact]
    public void Migrate_with_plan_out_writes_machine_readable_non_mutating_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var planPath = Path.Combine(tempDirectory, "migration-plan.json");

        try
        {
            var result = new SessionMigrationService().Migrate(
                ValidFixturePath,
                SessionMigrationService.SupportedSessionSchemaVersion,
                planOutputPath: planPath);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);
            Assert.True(File.Exists(fullPlanPath));

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.Equal("riftscan.session_migration_plan.v1", document.RootElement.GetProperty("plan_schema_version").GetString());
            Assert.Equal("fixture-valid-session", document.RootElement.GetProperty("session_id").GetString());
            Assert.Equal("noop_current_schema", document.RootElement.GetProperty("status").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal("preserve_raw_artifacts_no_mutation", document.RootElement.GetProperty("raw_data_policy").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_unsupported_target_with_plan_out_writes_blocked_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var planPath = Path.Combine(tempDirectory, "blocked-migration-plan.json");

        try
        {
            var result = new SessionMigrationService().Migrate(
                ValidFixturePath,
                "riftscan.session.v2",
                planOutputPath: planPath);

            Assert.False(result.Success);
            Assert.Equal("unsupported_target_schema", result.Status);
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.Equal("unsupported_target_schema", document.RootElement.GetProperty("status").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal("blocked", document.RootElement.GetProperty("actions")[0].GetProperty("action_type").GetString());
            Assert.Equal("define-target-schema-contract", document.RootElement.GetProperty("actions")[0].GetProperty("action_id").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_unsupported_source_with_plan_out_writes_blocked_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var legacySessionPath = Path.Combine(tempDirectory, "legacy-session");
        var planPath = Path.Combine(tempDirectory, "source-blocked-plan.json");

        try
        {
            CopyDirectory(ValidFixturePath, legacySessionPath);
            RewriteManifestSchemaVersion(legacySessionPath, "riftscan.session.v9");

            var result = new SessionMigrationService().Migrate(
                legacySessionPath,
                SessionMigrationService.SupportedSessionSchemaVersion,
                planOutputPath: planPath);

            Assert.False(result.Success);
            Assert.Equal("unsupported_source_schema", result.Status);
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.Equal("riftscan.session.v9", document.RootElement.GetProperty("from_schema_version").GetString());
            Assert.Equal("unsupported_source_schema", document.RootElement.GetProperty("status").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal("define-source-schema-migrator", document.RootElement.GetProperty("actions")[0].GetProperty("action_id").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }


    [Fact]
    public void Migrate_v0_source_with_plan_out_writes_dry_run_upgrade_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var legacySessionPath = Path.Combine(tempDirectory, "legacy-v0-session");
        var planPath = Path.Combine(tempDirectory, "v0-to-v1-plan.json");

        try
        {
            CopyDirectory(ValidFixturePath, legacySessionPath);
            RewriteManifestSchemaVersion(legacySessionPath, SessionMigrationService.LegacySessionSchemaVersionV0);

            var result = new SessionMigrationService().Migrate(
                legacySessionPath,
                SessionMigrationService.SupportedSessionSchemaVersion,
                planOutputPath: planPath);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            Assert.Equal("planned_source_schema_upgrade", result.Status);
            Assert.Equal(SessionMigrationService.LegacySessionSchemaVersionV0, result.FromSchemaVersion);
            Assert.Equal(SessionMigrationService.SupportedSessionSchemaVersion, result.ToSchemaVersion);
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.Equal("planned_source_schema_upgrade", document.RootElement.GetProperty("status").GetString());
            Assert.Equal(SessionMigrationService.LegacySessionSchemaVersionV0, document.RootElement.GetProperty("from_schema_version").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal(3, document.RootElement.GetProperty("actions").GetArrayLength());
            Assert.Equal("copy-session-artifacts-to-migrated-output", document.RootElement.GetProperty("actions")[0].GetProperty("action_id").GetString());
            Assert.Equal("rewrite-generated-manifest-schema-version", document.RootElement.GetProperty("actions")[1].GetProperty("action_id").GetString());
            Assert.Equal("recompute-generated-checksums", document.RootElement.GetProperty("actions")[2].GetProperty("action_id").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_apply_request_fails_without_mutating_and_can_write_blocked_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var planPath = Path.Combine(tempDirectory, "apply-blocked-plan.json");

        try
        {
            var result = new SessionMigrationService().Migrate(
                ValidFixturePath,
                SessionMigrationService.SupportedSessionSchemaVersion,
                dryRun: false,
                planOutputPath: planPath);

            Assert.False(result.Success);
            Assert.False(result.DryRun);
            Assert.Equal("apply_not_supported", result.Status);
            Assert.Contains(result.Issues, issue => issue.Code == "apply_not_supported");
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.False(document.RootElement.GetProperty("dry_run").GetBoolean());
            Assert.Equal("apply_not_supported", document.RootElement.GetProperty("status").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal("implement-apply-path", document.RootElement.GetProperty("actions")[0].GetProperty("action_id").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_verification_failure_with_plan_out_writes_blocked_plan()
    {
        var tempDirectory = CreateTempDirectory();
        var missingSessionPath = Path.Combine(tempDirectory, "missing-session");
        var planPath = Path.Combine(tempDirectory, "verification-blocked-plan.json");

        try
        {
            var result = new SessionMigrationService().Migrate(
                missingSessionPath,
                SessionMigrationService.SupportedSessionSchemaVersion,
                planOutputPath: planPath);

            Assert.False(result.Success);
            Assert.Equal("verification_failed", result.Status);
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal([fullPlanPath], result.ArtifactsWritten);

            using var document = JsonDocument.Parse(File.ReadAllText(fullPlanPath));
            Assert.Equal("verification_failed", document.RootElement.GetProperty("status").GetString());
            Assert.False(document.RootElement.GetProperty("can_apply").GetBoolean());
            Assert.Equal("repair-session-integrity", document.RootElement.GetProperty("actions")[0].GetProperty("action_id").GetString());
            Assert.False(document.RootElement.GetProperty("actions")[0].GetProperty("writes_raw_artifacts").GetBoolean());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_migrate_session_emits_machine_readable_noop_result()
    {
        var result = RunCli(
            "migrate",
            "session",
            ValidFixturePath,
            "--to-schema",
            SessionMigrationService.SupportedSessionSchemaVersion);

        Assert.Equal(0, result.ExitCode);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.Equal("riftscan.session_migration_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("noop_current_schema", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("artifacts_written").GetArrayLength());
    }

    [Fact]
    public void Cli_migrate_session_help_prints_usage_without_error()
    {
        var result = RunCli("migrate", "session", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("riftscan migrate session <session-path>", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public void Cli_migrate_session_unknown_option_returns_machine_readable_error()
    {
        var result = RunCli(
            "migrate",
            "session",
            ValidFixturePath,
            "--unknown");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.Stdout);
        using var document = JsonDocument.Parse(result.Stderr);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("command_failed", document.RootElement.GetProperty("issues")[0].GetProperty("code").GetString());
        Assert.Contains("Unknown migrate option: --unknown", document.RootElement.GetProperty("issues")[0].GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_migrate_session_plan_out_reports_written_artifact()
    {
        var tempDirectory = CreateTempDirectory();
        var planPath = Path.Combine(tempDirectory, "cli-migration-plan.json");

        try
        {
            var result = RunCli(
                "migrate",
                "session",
                ValidFixturePath,
                "--to-schema",
                SessionMigrationService.SupportedSessionSchemaVersion,
                "--plan-out",
                planPath);

            Assert.Equal(0, result.ExitCode);
            using var document = JsonDocument.Parse(result.Stdout);
            var fullPlanPath = Path.GetFullPath(planPath);
            Assert.Equal(fullPlanPath, document.RootElement.GetProperty("artifacts_written")[0].GetString());
            Assert.True(File.Exists(fullPlanPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_migrate_session_apply_returns_json_failure_not_exception()
    {
        var result = RunCli(
            "migrate",
            "session",
            ValidFixturePath,
            "--to-schema",
            SessionMigrationService.SupportedSessionSchemaVersion,
            "--apply");

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.False(document.RootElement.GetProperty("dry_run").GetBoolean());
        Assert.Equal("apply_not_supported", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void Cli_migrate_session_rejects_apply_and_dry_run_together()
    {
        var result = RunCli(
            "migrate",
            "session",
            ValidFixturePath,
            "--to-schema",
            SessionMigrationService.SupportedSessionSchemaVersion,
            "--apply",
            "--dry-run");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.Stdout);
        using var document = JsonDocument.Parse(result.Stderr);
        Assert.Equal("command_failed", document.RootElement.GetProperty("issues")[0].GetProperty("code").GetString());
        Assert.Contains("--dry-run and --apply cannot be used together", document.RootElement.GetProperty("issues")[0].GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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
        var manifest = JsonSerializer.Deserialize<SessionManifest>(File.ReadAllText(manifestPath), SessionJson.Options)!;
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest with { SchemaVersion = schemaVersion }, SessionJson.Options));

        var checksumsPath = Path.Combine(sessionPath, "checksums.json");
        var checksums = JsonSerializer.Deserialize<ChecksumManifest>(File.ReadAllText(checksumsPath), SessionJson.Options)!;
        var manifestInfo = new FileInfo(manifestPath);
        var updatedEntries = checksums.Entries
            .Select(entry => entry.Path == "manifest.json"
                ? entry with { Sha256Hex = SessionChecksum.ComputeSha256Hex(manifestPath), Bytes = manifestInfo.Length }
                : entry)
            .ToArray();

        File.WriteAllText(checksumsPath, JsonSerializer.Serialize(checksums with { Entries = updatedEntries }, SessionJson.Options));
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(args);
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
