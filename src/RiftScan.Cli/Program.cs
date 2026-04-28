using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
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

        try
        {
            if (args.Length >= 2 && Is(args[0], "capture") && Is(args[1], "passive"))
            {
                return CapturePassive(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "capture") && Is(args[1], "plan"))
            {
                return CapturePlan(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "analyze") && Is(args[1], "session"))
            {
                return AnalyzeSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "report") && Is(args[1], "session"))
            {
                return ReportSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "sessions"))
            {
                return CompareSessions(args[2..]);
            }

            if (args.Length == 3 && Is(args[0], "verify") && Is(args[1], "session"))
            {
                return VerifySession(args[2]);
            }

            return PrintMachineReadableError("unknown_command", $"Unknown command: {string.Join(' ', args)}");
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or PlatformNotSupportedException)
        {
            return PrintMachineReadableError("command_failed", ex.Message);
        }
    }

    private static int CapturePassive(string[] args)
    {
        var options = ParsePassiveCaptureOptions(args);
        var result = new PassiveCaptureService(new WindowsProcessMemoryReader()).Capture(options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CapturePlan(string[] args)
    {
        var options = ParsePassiveCapturePlanOptions(args);
        var result = new PassiveCapturePlanService(new WindowsProcessMemoryReader()).CaptureFromPlan(options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int AnalyzeSession(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Analyze requires a session path.");
        }

        var sessionPath = args[0];
        var top = ParseTop(args[1..]);
        var result = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionPath, top);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int ReportSession(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Report requires a session path.");
        }

        var sessionPath = args[0];
        var top = ParseTop(args[1..]);
        var result = new SessionReportGenerator().Generate(sessionPath, top);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareSessions(string[] args)
    {
        if (args.Length < 2)
        {
            throw new ArgumentException("Compare requires two session paths.");
        }

        var (top, outputPath) = ParseCompareOptions(args[2..]);
        var result = new SessionComparisonService().Compare(args[0], args[1], top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
            result = result with { ComparisonPath = fullOutputPath };
            File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static PassiveCaptureOptions ParsePassiveCaptureOptions(string[] args)
    {
        string? processName = null;
        int? processId = null;
        string? outputPath = null;
        var samples = 1;
        var intervalMilliseconds = 100;
        var maxRegions = 8;
        var maxBytesPerRegion = 64 * 1024;
        long maxTotalBytes = 1024 * 1024;
        var includeImageRegions = false;
        IReadOnlySet<string> regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--process":
                    processName = RequireValue(args, ref index, arg);
                    break;
                case "--pid":
                    processId = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--samples":
                    samples = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--interval-ms":
                    intervalMilliseconds = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--max-regions":
                    maxRegions = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--max-bytes-per-region":
                    maxBytesPerRegion = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--max-total-bytes":
                    maxTotalBytes = long.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--include-image-regions":
                    includeImageRegions = true;
                    break;
                case "--region-ids":
                    regionIds = ParseRegionIds(RequireValue(args, ref index, arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown capture passive option: {arg}");
            }
        }

        return new PassiveCaptureOptions
        {
            ProcessName = processName,
            ProcessId = processId,
            OutputPath = outputPath ?? throw new ArgumentException("Passive capture requires --out <session-path>."),
            Samples = samples,
            IntervalMilliseconds = intervalMilliseconds,
            MaxRegions = maxRegions,
            MaxBytesPerRegion = maxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            IncludeImageRegions = includeImageRegions,
            RegionIds = regionIds
        };
    }

    private static PassiveCapturePlanOptions ParsePassiveCapturePlanOptions(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Capture plan requires a source session path.");
        }

        var sourceSessionPath = args[0];
        string? processName = null;
        int? processId = null;
        string? outputPath = null;
        var topRegions = 5;
        var samples = 3;
        var intervalMilliseconds = 100;
        var maxBytesPerRegion = 64 * 1024;
        long maxTotalBytes = 1024 * 1024;
        var includeImageRegions = false;

        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--process":
                    processName = RequireValue(args, ref index, arg);
                    break;
                case "--pid":
                    processId = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--top-regions":
                    topRegions = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--samples":
                    samples = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--interval-ms":
                    intervalMilliseconds = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--max-bytes-per-region":
                    maxBytesPerRegion = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--max-total-bytes":
                    maxTotalBytes = long.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--include-image-regions":
                    includeImageRegions = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown capture plan option: {arg}");
            }
        }

        return new PassiveCapturePlanOptions
        {
            SourceSessionPath = sourceSessionPath,
            ProcessName = processName,
            ProcessId = processId,
            OutputPath = outputPath ?? throw new ArgumentException("Capture plan requires --out <session-path>."),
            TopRegions = topRegions,
            Samples = samples,
            IntervalMilliseconds = intervalMilliseconds,
            MaxBytesPerRegion = maxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            IncludeImageRegions = includeImageRegions
        };
    }

    private static IReadOnlySet<string> ParseRegionIds(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static int ParseTop(string[] args)
    {
        var top = 100;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--all":
                    top = int.MaxValue;
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {arg}");
            }
        }

        return top;
    }

    private static (int Top, string? OutputPath) ParseCompareOptions(string[] args)
    {
        var top = 100;
        string? outputPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--all":
                    top = int.MaxValue;
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown compare option: {arg}");
            }
        }

        return (top, outputPath);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Option {option} requires a value.");
        }

        index++;
        return args[index];
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
        Console.WriteLine("riftscan capture passive --process <name> --out sessions/<id> [--samples 1] [--interval-ms 100]");
        Console.WriteLine("riftscan capture passive --pid <id> --out sessions/<id> [--samples 1] [--interval-ms 100] [--region-ids region-000001,region-000002]");
        Console.WriteLine("riftscan capture plan <source-session> --pid <id> --out sessions/<id> [--top-regions 5]");
        Console.WriteLine("riftscan analyze session <session-path> [--all|--top 100]");
        Console.WriteLine("riftscan report session <session-path> [--top 100]");
        Console.WriteLine("riftscan compare sessions <session-a> <session-b> [--top 100] [--out reports/generated/comparison.json]");
        Console.WriteLine("riftscan verify session <session-path>");
    }

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsHelp(string value) =>
        Is(value, "--help") || Is(value, "-h") || Is(value, "help");
}
