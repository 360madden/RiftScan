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

            if (args.Length >= 2 && Is(args[0], "migrate") && Is(args[1], "session"))
            {
                return MigrateSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "session") && Is(args[1], "prune"))
            {
                return PruneSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "session") && Is(args[1], "inventory"))
            {
                return InventorySession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "session") && Is(args[1], "summary"))
            {
                return SummarizeSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "session"))
            {
                return VerifySession(args[2..]);
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
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCapturePassiveUsage();
            return 0;
        }

        var options = ParsePassiveCaptureOptions(args);
        var result = new PassiveCaptureService(new WindowsProcessMemoryReader()).Capture(options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CapturePlan(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCapturePlanUsage();
            return 0;
        }

        var options = ParsePassiveCapturePlanOptions(args);
        var result = new PassiveCapturePlanService(new WindowsProcessMemoryReader()).CaptureFromPlan(options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int AnalyzeSession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintAnalyzeSessionUsage();
            return 0;
        }

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
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintReportSessionUsage();
            return 0;
        }

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
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareSessionsUsage();
            return 0;
        }

        if (args.Length < 2)
        {
            throw new ArgumentException("Compare requires two session paths.");
        }

        var (top, outputPath, reportPath, nextPlanPath) = ParseCompareOptions(args[2..]);
        var result = new SessionComparisonService().Compare(args[0], args[1], top);
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { ComparisonReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(nextPlanPath))
        {
            result = result with { ComparisonNextCapturePlanPath = Path.GetFullPath(nextPlanPath) };
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { ComparisonPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.ComparisonReportPath))
        {
            _ = new SessionComparisonReportGenerator().Generate(result, result.ComparisonReportPath, top);
        }

        if (!string.IsNullOrWhiteSpace(result.ComparisonNextCapturePlanPath))
        {
            _ = new SessionComparisonNextCapturePlanGenerator().Generate(result, result.ComparisonNextCapturePlanPath, top);
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullOutputPath = result.ComparisonPath!;
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
            File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int MigrateSession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintMigrateUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Migrate requires a session path.");
        }

        var sessionPath = args[0];
        var (toSchemaVersion, dryRun, planOutputPath, migrationOutputPath) = ParseMigrateOptions(args[1..]);
        var result = new SessionMigrationService().Migrate(sessionPath, toSchemaVersion, dryRun, planOutputPath, migrationOutputPath);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }



    private static int SummarizeSession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintSessionSummaryUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Summary requires a session path.");
        }

        var summaryOutputPath = ParseSummaryOptions(args[1..]);
        var result = new SessionSummaryService().Summarize(args[0], summaryOutputPath);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int PruneSession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintSessionPruneUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Prune requires a session path.");
        }

        var sessionPath = args[0];
        var (dryRun, inventoryOutputPath) = ParsePruneOptions(args[1..]);
        var result = new SessionPruneService().Prune(sessionPath, dryRun, inventoryOutputPath);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int InventorySession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintSessionInventoryUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Inventory requires a session path.");
        }

        var inventoryOutputPath = ParseInventoryOptions(args[1..]);
        var result = new SessionInventoryService().Inventory(args[0], inventoryOutputPath);
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
        string? stimulusLabel = null;
        string? stimulusNotes = null;
        var interventionWaitMilliseconds = 20 * 60 * 1000;
        var interventionPollMilliseconds = 2_000;

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
                case "--stimulus":
                    stimulusLabel = RequireValue(args, ref index, arg);
                    break;
                case "--stimulus-note":
                    stimulusNotes = RequireValue(args, ref index, arg);
                    break;
                case "--intervention-wait-ms":
                    interventionWaitMilliseconds = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--intervention-poll-ms":
                    interventionPollMilliseconds = int.Parse(RequireValue(args, ref index, arg));
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
            RegionIds = regionIds,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes,
            InterventionWaitMilliseconds = interventionWaitMilliseconds,
            InterventionPollIntervalMilliseconds = interventionPollMilliseconds
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
        string? stimulusLabel = null;
        string? stimulusNotes = null;
        var interventionWaitMilliseconds = 20 * 60 * 1000;
        var interventionPollMilliseconds = 2_000;

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
                case "--stimulus":
                    stimulusLabel = RequireValue(args, ref index, arg);
                    break;
                case "--stimulus-note":
                    stimulusNotes = RequireValue(args, ref index, arg);
                    break;
                case "--intervention-wait-ms":
                    interventionWaitMilliseconds = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--intervention-poll-ms":
                    interventionPollMilliseconds = int.Parse(RequireValue(args, ref index, arg));
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
            IncludeImageRegions = includeImageRegions,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes,
            InterventionWaitMilliseconds = interventionWaitMilliseconds,
            InterventionPollIntervalMilliseconds = interventionPollMilliseconds
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

    private static (int Top, string? OutputPath, string? ReportPath, string? NextPlanPath) ParseCompareOptions(string[] args)
    {
        var top = 100;
        string? outputPath = null;
        string? reportPath = null;
        string? nextPlanPath = null;
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
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                case "--next-plan":
                    nextPlanPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown compare option: {arg}");
            }
        }

        return (top, outputPath, reportPath, nextPlanPath);
    }

    private static string? ParseSummaryOptions(string[] args)
    {
        string? summaryOutputPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--json-out":
                    summaryOutputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown session summary option: {arg}");
            }
        }

        return summaryOutputPath;
    }

    private static string? ParseInventoryOptions(string[] args)
    {
        string? inventoryOutputPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--json-out":
                    inventoryOutputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown session inventory option: {arg}");
            }
        }

        return inventoryOutputPath;
    }

    private static (bool DryRun, string? InventoryOutputPath) ParsePruneOptions(string[] args)
    {
        var dryRun = true;
        string? inventoryOutputPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--apply":
                    dryRun = false;
                    break;
                case "--json-out":
                    inventoryOutputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown session prune option: {arg}");
            }
        }

        return (dryRun, inventoryOutputPath);
    }

    private static (string ToSchemaVersion, bool DryRun, string? PlanOutputPath, string? MigrationOutputPath) ParseMigrateOptions(string[] args)
    {
        string? toSchemaVersion = null;
        string? planOutputPath = null;
        string? migrationOutputPath = null;
        var dryRun = true;
        var applyRequested = false;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--to-schema":
                    toSchemaVersion = RequireValue(args, ref index, arg);
                    break;
                case "--dry-run":
                    if (applyRequested)
                    {
                        throw new ArgumentException("Migrate options --dry-run and --apply cannot be used together.");
                    }

                    dryRun = true;
                    break;
                case "--apply":
                    if (applyRequested)
                    {
                        throw new ArgumentException("Migrate option --apply was specified more than once.");
                    }

                    applyRequested = true;
                    dryRun = false;
                    break;
                case "--plan-out":
                    planOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                    migrationOutputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown migrate option: {arg}");
            }
        }

        return (toSchemaVersion ?? throw new ArgumentException("Migrate requires --to-schema <schema-version>."), dryRun, planOutputPath, migrationOutputPath);
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

    private static int VerifySession(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifySessionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify requires a session path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify session option: {args[1]}");
        }

        return VerifySessionPath(args[0]);
    }

    private static int VerifySessionPath(string sessionPath)
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
        PrintCapturePassiveUsage();
        Console.WriteLine("riftscan capture passive --pid <id> [--process <name>] --out sessions/<id> [--samples 1] [--interval-ms 100] [--region-ids region-000001,region-000002] [--stimulus move_forward]");
        PrintCapturePlanUsage();
        PrintAnalyzeSessionUsage();
        PrintReportSessionUsage();
        PrintCompareSessionsUsage();
        PrintMigrateUsage();
        PrintSessionPruneUsage();
        PrintSessionInventoryUsage();
        PrintSessionSummaryUsage();
        PrintVerifySessionUsage();
    }

    private static void PrintCapturePassiveUsage() =>
        Console.WriteLine("riftscan capture passive --process <name> --out sessions/<id> [--samples 1] [--interval-ms 100] [--stimulus passive_idle] [--intervention-wait-ms 1200000] [--intervention-poll-ms 2000]");

    private static void PrintCapturePlanUsage() =>
        Console.WriteLine("riftscan capture plan <source-session-or-plan-json> --pid <id> [--process <name>] --out sessions/<id> [--top-regions 5] [--stimulus move_forward] [--intervention-wait-ms 1200000] [--intervention-poll-ms 2000]");

    private static void PrintAnalyzeSessionUsage() =>
        Console.WriteLine("riftscan analyze session <session-path> [--all|--top 100]");

    private static void PrintReportSessionUsage() =>
        Console.WriteLine("riftscan report session <session-path> [--top 100]");

    private static void PrintCompareSessionsUsage() =>
        Console.WriteLine("riftscan compare sessions <session-a> <session-b> [--top 100] [--out reports/generated/comparison.json] [--report-md reports/generated/comparison.md] [--next-plan reports/generated/next-capture-plan.json]");

    private static void PrintMigrateUsage() =>
        Console.WriteLine("riftscan migrate session <session-path> --to-schema riftscan.session.v1 [--dry-run|--apply] [--out sessions/<migrated-id>] [--plan-out reports/generated/migration-plan.json]");

    private static void PrintSessionPruneUsage() =>
        Console.WriteLine("riftscan session prune <session-path> [--dry-run] [--json-out reports/generated/prune-inventory.json]");

    private static void PrintSessionInventoryUsage() =>
        Console.WriteLine("riftscan session inventory <session-path> [--json-out reports/generated/session-inventory.json]");

    private static void PrintSessionSummaryUsage() =>
        Console.WriteLine("riftscan session summary <session-path> [--json-out reports/generated/session-summary.json]");

    private static void PrintVerifySessionUsage() =>
        Console.WriteLine("riftscan verify session <session-path>");

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsHelp(string value) =>
        Is(value, "--help") || Is(value, "-h") || Is(value, "help");
}
