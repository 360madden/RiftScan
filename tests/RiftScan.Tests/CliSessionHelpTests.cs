namespace RiftScan.Tests;

public sealed class CliSessionHelpTests
{
    [Theory]
    [InlineData("--help", "riftscan capture passive")]
    [InlineData("capture passive --help", "riftscan capture passive --process <name>")]
    [InlineData("capture plan --help", "riftscan capture plan <source-session-or-plan-json>")]
    [InlineData("analyze session --help", "riftscan analyze session <session-path>")]
    [InlineData("report session --help", "riftscan report session <session-path>")]
    [InlineData("compare sessions --help", "riftscan compare sessions <session-a> <session-b>")]
    [InlineData("migrate session --help", "riftscan migrate session <session-path>")]
    [InlineData("session prune --help", "riftscan session prune <session-path>")]
    [InlineData("session inventory --help", "riftscan session inventory <session-path>")]
    [InlineData("session summary --help", "riftscan session summary <session-path>")]
    [InlineData("verify session --help", "riftscan verify session <session-path>")]
    public void Cli_public_help_prints_usage_without_error(string commandLine, string expectedUsage)
    {
        var result = RunCli(commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(expectedUsage, result.Stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Stderr);
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
