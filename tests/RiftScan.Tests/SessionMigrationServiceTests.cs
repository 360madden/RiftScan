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

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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
