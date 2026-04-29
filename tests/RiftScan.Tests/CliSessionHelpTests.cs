using System.Text.Json;

namespace RiftScan.Tests;

public sealed class CliSessionHelpTests
{
    [Theory]
    [InlineData("--help", "riftscan capture passive")]
    [InlineData("capture passive --help", "riftscan capture passive --process <name>")]
    [InlineData("capture plan --help", "riftscan capture plan <source-session-or-plan-json>")]
    [InlineData("analyze session --help", "riftscan analyze session <session-path>")]
    [InlineData("analyze xrefs --help", "riftscan analyze xrefs <session-path>")]
    [InlineData("analyze xref-chain --help", "riftscan analyze xref-chain <xref-json>")]
    [InlineData("report session --help", "riftscan report session <session-path>")]
    [InlineData("rift match-addon-coords --help", "riftscan rift match-addon-coords <session-path>")]
    [InlineData("rift compare-addon-coordinate-motion --help", "riftscan rift compare-addon-coordinate-motion <pre-match-json>")]
    [InlineData("rift coordinate-mirror-context --help", "riftscan rift coordinate-mirror-context <motion-comparison-json>")]
    [InlineData("compare sessions --help", "riftscan compare sessions <session-a> <session-b>")]
    [InlineData("migrate session --help", "riftscan migrate session <session-path>")]
    [InlineData("session prune --help", "riftscan session prune <session-path>")]
    [InlineData("session inventory --help", "riftscan session inventory <session-path>")]
    [InlineData("session summary --help", "riftscan session summary <session-path>")]
    [InlineData("verify session --help", "riftscan verify session <session-path>")]
    [InlineData("verify xref-chain-summary --help", "riftscan verify xref-chain-summary <xref-chain-summary.json>")]
    public void Cli_public_help_prints_usage_without_error(string commandLine, string expectedUsage)
    {
        var result = RunCli(commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(expectedUsage, result.Stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    [InlineData("version")]
    public void Cli_version_prints_version_without_error(string argument)
    {
        var result = RunCli(argument);

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"^riftscan 0\.1\.0(?:\+[0-9a-f]+)?\r?\n$", result.Stdout);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Theory]
    [InlineData("--version --json")]
    [InlineData("-v --json")]
    [InlineData("version --json")]
    public void Cli_version_json_prints_machine_readable_version(string commandLine)
    {
        var result = RunCli(commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.Equal("riftscan.version_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("riftscan", document.RootElement.GetProperty("executable").GetString());
        Assert.Equal("0.1.0", document.RootElement.GetProperty("version").GetString());
        Assert.StartsWith("0.1.0", document.RootElement.GetProperty("informational_version").GetString(), StringComparison.Ordinal);
        Assert.True(document.RootElement.TryGetProperty("source_revision", out _));
    }

    [Fact]
    public void Cli_version_unknown_option_returns_machine_readable_error()
    {
        var result = RunCli("--version", "--unknown");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.Stdout);
        using var document = JsonDocument.Parse(result.Stderr);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("command_failed", document.RootElement.GetProperty("issues")[0].GetProperty("code").GetString());
        Assert.Contains("Unknown version option: --unknown", document.RootElement.GetProperty("issues")[0].GetProperty("message").GetString(), StringComparison.Ordinal);
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
