namespace RiftScan.Tests;

public sealed class CliSessionHelpTests
{
    [Theory]
    [InlineData("prune", "riftscan session prune <session-path>")]
    [InlineData("inventory", "riftscan session inventory <session-path>")]
    [InlineData("summary", "riftscan session summary <session-path>")]
    public void Cli_session_subcommand_help_prints_usage_without_error(string subcommand, string expectedUsage)
    {
        var result = RunCli("session", subcommand, "--help");

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
