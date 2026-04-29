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
    public void Cli_migrate_session_emits_machine_readable_noop_result()
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var exitCode = RiftScan.Cli.Program.Main(
                [
                    "migrate",
                    "session",
                    ValidFixturePath,
                    "--to-schema",
                    SessionMigrationService.SupportedSessionSchemaVersion
                ]);

            Assert.Equal(0, exitCode);
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.session_migration_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("noop_current_schema", document.RootElement.GetProperty("status").GetString());
            Assert.Equal(0, document.RootElement.GetProperty("artifacts_written").GetArrayLength());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");
}
