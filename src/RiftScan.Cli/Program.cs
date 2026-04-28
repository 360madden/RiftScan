using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        if (args.Length == 3 && Is(args[0], "verify") && Is(args[1], "session"))
        {
            return VerifySession(args[2]);
        }

        if (args.Length >= 1 && (Is(args[0], "capture") || Is(args[0], "analyze") || Is(args[0], "report")))
        {
            return PrintMachineReadableError("command_not_implemented", $"Command '{string.Join(' ', args)}' is declared for the future CLI surface but is not implemented in the foundation slice.");
        }

        return PrintMachineReadableError("unknown_command", $"Unknown command: {string.Join(' ', args)}");
    }

    private static int VerifySession(string sessionPath)
    {
        var result = new SessionVerifier().Verify(sessionPath);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int PrintMachineReadableError(string code, string message)
    {
        var payload = new
        {
            success = false,
            issues = new[]
            {
                new VerificationIssue
                {
                    Code = code,
                    Message = message,
                    Severity = "error"
                }
            }
        };
        Console.Error.WriteLine(JsonSerializer.Serialize(payload, SessionJson.Options));
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("riftscan verify session <session-path>");
    }

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsHelp(string value) =>
        Is(value, "--help") || Is(value, "-h") || Is(value, "help");
}
