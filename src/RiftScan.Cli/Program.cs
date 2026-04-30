using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Analysis.Comparison;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Xrefs;
using RiftScan.Capture.Passive;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;
using RiftScan.Rift.Validation;

namespace RiftScan.Cli;

public static class Program
{
    private const string FallbackVersion = "0.1.0";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            if (args.Length >= 1 && IsVersion(args[0]))
            {
                return Version(args[1..]);
            }

            if (args.Length >= 2 && Is(args[0], "capture") && Is(args[1], "passive"))
            {
                return CapturePassive(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "capture") && Is(args[1], "plan"))
            {
                return CapturePlan(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "process") && Is(args[1], "inventory"))
            {
                return ProcessInventory(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "analyze") && Is(args[1], "session"))
            {
                return AnalyzeSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "analyze") && Is(args[1], "xrefs"))
            {
                return AnalyzeXrefs(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "analyze") && Is(args[1], "xref-chain"))
            {
                return AnalyzeXrefChain(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "report") && Is(args[1], "session"))
            {
                return ReportSession(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "report") && Is(args[1], "capability"))
            {
                return ReportCapability(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "addon-coords"))
            {
                return RiftAddonCoords(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "addon-api-observations"))
            {
                return RiftAddonApiObservations(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "addon-api-truth"))
            {
                return RiftAddonApiTruth(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "match-addon-coords"))
            {
                return RiftMatchAddonCoords(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "match-waypoint-anchors"))
            {
                return RiftMatchWaypointAnchors(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "match-waypoint-scalars"))
            {
                return RiftMatchWaypointScalars(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "compare-waypoint-scalars"))
            {
                return RiftCompareWaypointScalars(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "plan-waypoint-scalar-followup"))
            {
                return RiftPlanWaypointScalarFollowup(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "compare-addon-coordinate-motion"))
            {
                return RiftCompareAddonCoordinateMotion(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "coordinate-mirror-context"))
            {
                return RiftCoordinateMirrorContext(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "addon-corroboration"))
            {
                return RiftAddonCorroboration(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "rift") && Is(args[1], "verify-promoted-coordinate"))
            {
                return RiftVerifyPromotedCoordinate(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "sessions"))
            {
                return CompareSessions(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "scalar-set"))
            {
                return CompareScalarSet(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "scalar-truth"))
            {
                return CompareScalarTruth(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "vec3-truth"))
            {
                return CompareVec3Truth(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "vec3-promotion"))
            {
                return CompareVec3Promotion(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "compare") && Is(args[1], "scalar-promotion"))
            {
                return CompareScalarPromotion(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "review") && Is(args[1], "scalar-promotion"))
            {
                return ReviewScalarPromotion(args[2..]);
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

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "xref-chain-summary"))
            {
                return VerifyXrefChainSummary(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "scalar-corroboration"))
            {
                return VerifyScalarCorroboration(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "vec3-corroboration"))
            {
                return VerifyVec3Corroboration(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "scalar-evidence-set"))
            {
                return VerifyScalarEvidenceSet(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "scalar-truth-recovery"))
            {
                return VerifyScalarTruthRecovery(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "vec3-truth-recovery"))
            {
                return VerifyVec3TruthRecovery(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "scalar-truth-promotion"))
            {
                return VerifyScalarTruthPromotion(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "vec3-truth-promotion"))
            {
                return VerifyVec3TruthPromotion(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "rift-promoted-coordinate-live"))
            {
                return VerifyRiftPromotedCoordinateLive(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "scalar-promotion-review"))
            {
                return VerifyScalarPromotionReview(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "comparison-readiness"))
            {
                return VerifyComparisonReadiness(args[2..]);
            }

            if (args.Length >= 2 && Is(args[0], "verify") && Is(args[1], "capability-status"))
            {
                return VerifyCapabilityStatus(args[2..]);
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

        if (args.Any(arg => Is(arg, "--dry-run")))
        {
            var (dryRunOptions, jsonOutputPath) = ParsePassiveCaptureDryRunOptions(args.Where(arg => !Is(arg, "--dry-run")).ToArray(), allowSessionOutOption: true);
            var dryRun = new PassiveCaptureDryRunService(new WindowsProcessMemoryReader()).Inspect(dryRunOptions);
            WriteOptionalJson(jsonOutputPath, dryRun);
            Console.WriteLine(JsonSerializer.Serialize(dryRun, SessionJson.Options));
            return dryRun.Success ? 0 : 1;
        }

        var options = ParsePassiveCaptureOptions(args);
        var result = new PassiveCaptureService(new WindowsProcessMemoryReader()).Capture(options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int ProcessInventory(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintProcessInventoryUsage();
            return 0;
        }

        var (options, jsonOutputPath) = ParsePassiveCaptureDryRunOptions(args, allowSessionOutOption: false);
        var result = new PassiveCaptureDryRunService(new WindowsProcessMemoryReader()).Inspect(options);
        WriteOptionalJson(jsonOutputPath, result);
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

    private static int AnalyzeXrefs(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintAnalyzeXrefsUsage();
            return 0;
        }

        var (options, outputPath, reportPath) = ParseXrefOptions(args);
        var result = new SessionXrefAnalysisService().Analyze(options);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.MarkdownReportPath))
        {
            new SessionXrefAnalysisService().WriteMarkdown(result, result.MarkdownReportPath);
        }

        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int AnalyzeXrefChain(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintAnalyzeXrefChainUsage();
            return 0;
        }

        var (options, outputPath, reportPath) = ParseXrefChainOptions(args);
        var service = new SessionXrefChainSummaryService();
        var result = service.Summarize(options);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.MarkdownReportPath))
        {
            service.WriteMarkdown(result, result.MarkdownReportPath);
        }

        WriteOptionalJson(result.OutputPath, result);
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

        var (top, outputPath, reportPath, nextPlanPath, truthReadinessPath, vec3TruthCandidatePath, vec3CorroborationPath) = ParseCompareOptions(args[2..]);
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

        if (!string.IsNullOrWhiteSpace(truthReadinessPath))
        {
            result = result with { ComparisonTruthReadinessPath = Path.GetFullPath(truthReadinessPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.ComparisonReportPath))
        {
            _ = new SessionComparisonReportGenerator().Generate(result, result.ComparisonReportPath, top);
        }

        if (!string.IsNullOrWhiteSpace(result.ComparisonNextCapturePlanPath))
        {
            _ = new SessionComparisonNextCapturePlanGenerator().Generate(result, result.ComparisonNextCapturePlanPath, top);
        }

        if (!string.IsNullOrWhiteSpace(result.ComparisonTruthReadinessPath))
        {
            _ = new ComparisonTruthReadinessService().Write(result, result.ComparisonTruthReadinessPath, top);
        }

        if (!string.IsNullOrWhiteSpace(vec3TruthCandidatePath))
        {
            _ = new Vec3TruthCandidateExporter().Export(result, Path.GetFullPath(vec3TruthCandidatePath), top, vec3CorroborationPath);
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

    private static int ReportCapability(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintReportCapabilityUsage();
            return 0;
        }

        var (truthReadinessPaths, scalarEvidenceSetPaths, scalarTruthRecoveryPaths, scalarTruthPromotionPaths, scalarPromotionReviewPaths, riftPromotedCoordinateLivePaths, jsonOutputPath, reportMarkdownPath) = ParseReportCapabilityOptions(args);
        var result = new CapabilityStatusService().Build(truthReadinessPaths, scalarEvidenceSetPaths, scalarTruthRecoveryPaths, scalarTruthPromotionPaths, scalarPromotionReviewPaths, riftPromotedCoordinateLivePaths);
        WriteOptionalJson(jsonOutputPath, result);
        WriteOptionalCapabilityReport(reportMarkdownPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftAddonCoords(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftAddonCoordsUsage();
            return 0;
        }

        string? rootPath = null;
        string? jsonOutputPath = null;
        string? jsonlOutputPath = null;
        var maxFiles = 5000;
        var includeAddonNames = new List<string>();
        DateTimeOffset? minFileWriteUtc = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--json-out":
                    jsonOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--jsonl-out":
                    jsonlOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--max-files":
                    maxFiles = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--addon-name":
                case "--include-addon":
                    includeAddonNames.Add(RequireValue(args, ref index, arg));
                    break;
                case "--min-file-write-utc":
                    minFileWriteUtc = DateTimeOffset.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                default:
                    if (rootPath is not null)
                    {
                        throw new ArgumentException($"Unknown rift addon-coords option: {arg}");
                    }

                    rootPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("rift addon-coords requires a SavedVariables file or directory path.");
        }

        var result = new RiftAddonCoordinateObservationService().Scan(new RiftAddonCoordinateScanOptions
        {
            Path = rootPath,
            MaxFiles = maxFiles,
            JsonlOutputPath = jsonlOutputPath,
            IncludeAddonNames = includeAddonNames,
            MinFileLastWriteUtc = minFileWriteUtc
        });
        WriteOptionalJson(jsonOutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftAddonApiObservations(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftAddonApiObservationsUsage();
            return 0;
        }

        string? rootPath = null;
        string? jsonOutputPath = null;
        string? jsonlOutputPath = null;
        var maxFiles = 5000;
        var includeAddonNames = new List<string>();
        DateTimeOffset? minFileWriteUtc = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--json-out":
                    jsonOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--jsonl-out":
                    jsonlOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--max-files":
                    maxFiles = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--addon-name":
                case "--include-addon":
                    includeAddonNames.Add(RequireValue(args, ref index, arg));
                    break;
                case "--min-file-write-utc":
                    minFileWriteUtc = DateTimeOffset.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                default:
                    if (rootPath is not null)
                    {
                        throw new ArgumentException($"Unknown rift addon-api-observations option: {arg}");
                    }

                    rootPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("rift addon-api-observations requires a SavedVariables file or directory path.");
        }

        var result = new RiftAddonApiObservationService().Scan(new RiftAddonApiObservationScanOptions
        {
            Path = rootPath,
            MaxFiles = maxFiles,
            JsonlOutputPath = jsonlOutputPath,
            IncludeAddonNames = includeAddonNames,
            MinFileLastWriteUtc = minFileWriteUtc
        });
        WriteOptionalJson(jsonOutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftAddonApiTruth(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftAddonApiTruthUsage();
            return 0;
        }

        string? scanPath = null;
        string? outputPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (scanPath is not null || arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift addon-api-truth option: {arg}");
                    }

                    scanPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(scanPath))
        {
            throw new ArgumentException("rift addon-api-truth requires an addon API observation scan JSON path.");
        }

        var result = new RiftAddonApiTruthSummaryService().Summarize(new RiftAddonApiTruthSummaryOptions
        {
            ScanPath = scanPath
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftMatchAddonCoords(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftMatchAddonCoordsUsage();
            return 0;
        }

        string? sessionPath = null;
        string? observationPath = null;
        string? outputPath = null;
        string? reportPath = null;
        var regionBaseAddresses = new List<ulong>();
        var tolerance = 5d;
        var top = 100;
        var latestOnly = false;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--observations":
                    observationPath = RequireValue(args, ref index, arg);
                    break;
                case "--region-base":
                case "--region-bases":
                    regionBaseAddresses.AddRange(ParseBaseAddresses(RequireValue(args, ref index, arg)));
                    break;
                case "--tolerance":
                    tolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--latest-only":
                    latestOnly = true;
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (sessionPath is not null || arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift match-addon-coords option: {arg}");
                    }

                    sessionPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(sessionPath))
        {
            throw new ArgumentException("rift match-addon-coords requires a session path.");
        }

        if (string.IsNullOrWhiteSpace(observationPath))
        {
            throw new ArgumentException("rift match-addon-coords requires --observations <addon-coordinate-observations.jsonl>.");
        }

        var service = new RiftSessionAddonCoordinateMatchService();
        var result = service.Match(new RiftSessionAddonCoordinateMatchOptions
        {
            SessionPath = sessionPath,
            ObservationPath = observationPath,
            RegionBaseAddresses = regionBaseAddresses.Distinct().Order().ToArray(),
            Tolerance = tolerance,
            Top = top,
            LatestOnly = latestOnly
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.MarkdownReportPath))
        {
            service.WriteMarkdown(result, result.MarkdownReportPath);
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftMatchWaypointAnchors(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftMatchWaypointAnchorsUsage();
            return 0;
        }

        string? sessionPath = null;
        string? anchorPath = null;
        string? outputPath = null;
        var regionBaseAddresses = new List<ulong>();
        var tolerance = 5d;
        var top = 100;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--anchors":
                case "--anchor-scan":
                    anchorPath = RequireValue(args, ref index, arg);
                    break;
                case "--region-base":
                case "--region-bases":
                    regionBaseAddresses.AddRange(ParseBaseAddresses(RequireValue(args, ref index, arg)));
                    break;
                case "--tolerance":
                    tolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (sessionPath is not null || arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift match-waypoint-anchors option: {arg}");
                    }

                    sessionPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(sessionPath))
        {
            throw new ArgumentException("rift match-waypoint-anchors requires a session path.");
        }

        if (string.IsNullOrWhiteSpace(anchorPath))
        {
            throw new ArgumentException("rift match-waypoint-anchors requires --anchors <addon-api-observation-scan.json>.");
        }

        var result = new RiftSessionWaypointAnchorMatchService().Match(new RiftSessionWaypointAnchorMatchOptions
        {
            SessionPath = sessionPath,
            AnchorPath = anchorPath,
            RegionBaseAddresses = regionBaseAddresses.Distinct().Order().ToArray(),
            Tolerance = tolerance,
            Top = top
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftMatchWaypointScalars(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftMatchWaypointScalarsUsage();
            return 0;
        }

        string? sessionPath = null;
        string? anchorPath = null;
        string? outputPath = null;
        string? scalarHitsOutputPath = null;
        var regionBaseAddresses = new List<ulong>();
        var tolerance = 5d;
        var top = 100;
        var maxScalarHitsPerSnapshotAxis = 64;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--anchors":
                case "--anchor-scan":
                    anchorPath = RequireValue(args, ref index, arg);
                    break;
                case "--region-base":
                case "--region-bases":
                    regionBaseAddresses.AddRange(ParseBaseAddresses(RequireValue(args, ref index, arg)));
                    break;
                case "--tolerance":
                    tolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--max-scalar-hits-per-snapshot-axis":
                    maxScalarHitsPerSnapshotAxis = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--scalar-hits-out":
                case "--all-scalar-hits-out":
                    scalarHitsOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (sessionPath is not null || arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift match-waypoint-scalars option: {arg}");
                    }

                    sessionPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(sessionPath))
        {
            throw new ArgumentException("rift match-waypoint-scalars requires a session path.");
        }

        if (string.IsNullOrWhiteSpace(anchorPath))
        {
            throw new ArgumentException("rift match-waypoint-scalars requires --anchors <addon-api-observation-scan.json>.");
        }

        var result = new RiftSessionWaypointScalarMatchService().Match(new RiftSessionWaypointScalarMatchOptions
        {
            SessionPath = sessionPath,
            AnchorPath = anchorPath,
            RegionBaseAddresses = regionBaseAddresses.Distinct().Order().ToArray(),
            Tolerance = tolerance,
            Top = top,
            MaxScalarHitsPerSnapshotAxis = maxScalarHitsPerSnapshotAxis,
            ScalarHitsOutputPath = scalarHitsOutputPath
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftCompareWaypointScalars(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftCompareWaypointScalarsUsage();
            return 0;
        }

        var inputPaths = new List<string>();
        string? outputPath = null;
        var deltaTolerance = 5d;
        var top = 100;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--delta-tolerance":
                    deltaTolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift compare-waypoint-scalars option: {arg}");
                    }

                    inputPaths.Add(arg);
                    break;
            }
        }

        if (inputPaths.Count < 2)
        {
            throw new ArgumentException("rift compare-waypoint-scalars requires at least two waypoint scalar match JSON paths.");
        }

        var result = new RiftWaypointScalarComparisonService().Compare(new RiftWaypointScalarComparisonOptions
        {
            InputPaths = inputPaths,
            DeltaTolerance = deltaTolerance,
            Top = top
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftPlanWaypointScalarFollowup(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftPlanWaypointScalarFollowupUsage();
            return 0;
        }

        string? scalarMatchPath = null;
        string? outputPath = null;
        var topPairs = 10;
        var samples = 8;
        var intervalMs = 100;
        var maxBytesPerRegion = 65536;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--top-pairs":
                    topPairs = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--samples":
                    samples = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--interval-ms":
                    intervalMs = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--max-bytes-per-region":
                    maxBytesPerRegion = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (scalarMatchPath is not null || arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift plan-waypoint-scalar-followup option: {arg}");
                    }

                    scalarMatchPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(scalarMatchPath))
        {
            throw new ArgumentException("rift plan-waypoint-scalar-followup requires a waypoint scalar match JSON path.");
        }

        var result = new RiftWaypointScalarFollowupPlanService().Plan(new RiftWaypointScalarFollowupPlanOptions
        {
            ScalarMatchPath = scalarMatchPath,
            TopPairs = topPairs,
            Samples = samples,
            IntervalMilliseconds = intervalMs,
            MaxBytesPerRegion = maxBytesPerRegion
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftCompareAddonCoordinateMotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftCompareAddonCoordinateMotionUsage();
            return 0;
        }

        var inputPaths = new List<string>();
        string? outputPath = null;
        string? reportPath = null;
        var minDeltaDistance = 1d;
        var mirrorEpsilon = 0.001d;
        var top = 100;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--min-delta-distance":
                    minDeltaDistance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--mirror-epsilon":
                    mirrorEpsilon = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown rift compare-addon-coordinate-motion option: {arg}");
                    }

                    inputPaths.Add(arg);
                    break;
            }
        }

        if (inputPaths.Count != 2)
        {
            throw new ArgumentException("rift compare-addon-coordinate-motion requires <pre-match-json> and <post-match-json>.");
        }

        var service = new RiftAddonCoordinateMotionComparisonService();
        var result = service.Compare(new RiftAddonCoordinateMotionComparisonOptions
        {
            PreMatchPath = inputPaths[0],
            PostMatchPath = inputPaths[1],
            MinDeltaDistance = minDeltaDistance,
            MirrorEpsilon = mirrorEpsilon,
            Top = top
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.MarkdownReportPath))
        {
            service.WriteMarkdown(result, result.MarkdownReportPath);
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftCoordinateMirrorContext(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftCoordinateMirrorContextUsage();
            return 0;
        }

        string? motionComparisonPath = null;
        string? sessionPath = null;
        string? outputPath = null;
        string? reportPath = null;
        var windowBytes = 256;
        var maxPointerHits = 32;
        var top = 100;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--session":
                    sessionPath = RequireValue(args, ref index, arg);
                    break;
                case "--window-bytes":
                    windowBytes = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--max-pointer-hits":
                    maxPointerHits = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal) || motionComparisonPath is not null)
                    {
                        throw new ArgumentException($"Unknown rift coordinate-mirror-context option: {arg}");
                    }

                    motionComparisonPath = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(motionComparisonPath))
        {
            throw new ArgumentException("rift coordinate-mirror-context requires <motion-comparison-json>.");
        }

        if (string.IsNullOrWhiteSpace(sessionPath))
        {
            throw new ArgumentException("rift coordinate-mirror-context requires --session <session-path>.");
        }

        var service = new RiftCoordinateMirrorContextService();
        var result = service.Analyze(new RiftCoordinateMirrorContextOptions
        {
            MotionComparisonPath = motionComparisonPath,
            SessionPath = sessionPath,
            WindowBytes = windowBytes,
            MaxPointerHits = maxPointerHits,
            Top = top
        });
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
        }

        if (!string.IsNullOrWhiteSpace(result.MarkdownReportPath))
        {
            service.WriteMarkdown(result, result.MarkdownReportPath);
        }

        WriteOptionalJson(result.OutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftAddonCorroboration(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftAddonCorroborationUsage();
            return 0;
        }

        string? candidatePath = null;
        string? observationPath = null;
        string? outputPath = null;
        string? jsonOutputPath = null;
        var tolerance = 5d;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--candidates":
                    candidatePath = RequireValue(args, ref index, arg);
                    break;
                case "--observations":
                    observationPath = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--json-out":
                    jsonOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--tolerance":
                    tolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown rift addon-corroboration option: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            throw new ArgumentException("rift addon-corroboration requires --candidates <vec3-truth-candidates.jsonl>.");
        }

        if (string.IsNullOrWhiteSpace(observationPath))
        {
            throw new ArgumentException("rift addon-corroboration requires --observations <addon-coordinate-observations.jsonl>.");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("rift addon-corroboration requires --out <vec3-truth-corroboration.jsonl>.");
        }

        var result = new RiftAddonCoordinateCorroborationService().Build(candidatePath, observationPath, outputPath, tolerance);
        WriteOptionalJson(jsonOutputPath, result);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int RiftVerifyPromotedCoordinate(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintRiftVerifyPromotedCoordinateUsage();
            return 0;
        }

        string? promotionPath = null;
        string? savedVariablesPath = null;
        string? outputPath = null;
        string? candidateId = null;
        string? processName = null;
        int? processId = null;
        var tolerance = 5d;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--promotion":
                    promotionPath = RequireValue(args, ref index, arg);
                    break;
                case "--savedvariables":
                    savedVariablesPath = RequireValue(args, ref index, arg);
                    break;
                case "--pid":
                    processId = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--process":
                    processName = RequireValue(args, ref index, arg);
                    break;
                case "--candidate-id":
                    candidateId = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--tolerance":
                    tolerance = double.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown rift verify-promoted-coordinate option: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(promotionPath))
        {
            throw new ArgumentException("rift verify-promoted-coordinate requires --promotion <vec3-truth-promotion.json>.");
        }

        if (string.IsNullOrWhiteSpace(savedVariablesPath))
        {
            throw new ArgumentException("rift verify-promoted-coordinate requires --savedvariables <SavedVariables-file-or-directory>.");
        }

        var result = new RiftPromotedCoordinateLiveVerificationService(new WindowsProcessMemoryReader())
            .Verify(promotionPath, savedVariablesPath, processId, processName, candidateId, tolerance);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareScalarSet(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareScalarSetUsage();
            return 0;
        }

        var sessionPaths = new List<string>();
        var top = 100;
        string? outputPath = null;
        string? reportPath = null;
        string? truthCandidatePath = null;
        string? corroborationPath = null;
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
                case "--truth-out":
                    truthCandidatePath = RequireValue(args, ref index, arg);
                    break;
                case "--corroboration":
                    corroborationPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    sessionPaths.Add(arg);
                    break;
            }
        }

        if (sessionPaths.Count < 2)
        {
            throw new ArgumentException("Scalar-set compare requires at least two session paths.");
        }

        var result = new ScalarEvidenceSetService().Aggregate(sessionPaths, top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { ReportPath = Path.GetFullPath(reportPath) };
            _ = new ScalarEvidenceSetReportGenerator().Generate(result, result.ReportPath, top);
        }

        if (!string.IsNullOrWhiteSpace(truthCandidatePath))
        {
            result = result with { TruthCandidatePath = Path.GetFullPath(truthCandidatePath) };
            _ = new ScalarTruthCandidateExporter().Export(result, result.TruthCandidatePath, top, corroborationPath);
        }

        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareScalarTruth(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareScalarTruthUsage();
            return 0;
        }

        var paths = new List<string>();
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
                    paths.Add(arg);
                    break;
            }
        }

        if (paths.Count < 2)
        {
            throw new ArgumentException("Scalar-truth compare requires at least two scalar truth candidate JSONL files.");
        }

        var result = new ScalarTruthRecoveryService().Recover(paths, top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareVec3Truth(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareVec3TruthUsage();
            return 0;
        }

        var paths = new List<string>();
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
                    paths.Add(arg);
                    break;
            }
        }

        if (paths.Count < 2)
        {
            throw new ArgumentException("Vec3-truth compare requires at least two vec3 truth candidate JSONL files.");
        }

        var result = new Vec3TruthRecoveryService().Recover(paths, top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareVec3Promotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareVec3PromotionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Vec3-promotion compare requires a vec3 truth recovery JSON path.");
        }

        var recoveryPath = args[0];
        string? corroborationPath = null;
        string? actorYawRecoveryPath = null;
        string? outputPath = null;
        var top = 100;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--corroboration":
                    corroborationPath = RequireValue(args, ref index, arg);
                    break;
                case "--actor-yaw-recovery":
                    actorYawRecoveryPath = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--all":
                    top = int.MaxValue;
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown vec3-promotion option: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(corroborationPath))
        {
            throw new ArgumentException("Vec3-promotion compare requires --corroboration <vec3_truth_corroboration.jsonl>.");
        }

        var result = new Vec3TruthPromotionService().Promote(recoveryPath, corroborationPath, actorYawRecoveryPath, top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int CompareScalarPromotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintCompareScalarPromotionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Scalar-promotion compare requires a scalar truth recovery JSON path.");
        }

        var recoveryPath = args[0];
        string? corroborationPath = null;
        string? outputPath = null;
        var top = 100;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--corroboration":
                    corroborationPath = RequireValue(args, ref index, arg);
                    break;
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--all":
                    top = int.MaxValue;
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown scalar-promotion option: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(corroborationPath))
        {
            throw new ArgumentException("Scalar-promotion compare requires --corroboration <scalar_truth_corroboration.jsonl>.");
        }

        var result = new ScalarTruthPromotionService().Promote(recoveryPath, corroborationPath, top);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int ReviewScalarPromotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintReviewScalarPromotionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Scalar-promotion review requires a scalar truth promotion JSON path.");
        }

        var promotionPath = args[0];
        string? outputPath = null;
        string? reportPath = null;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown scalar-promotion review option: {arg}");
            }
        }

        var result = new ScalarPromotionReviewService().Review(promotionPath);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            result = result with { OutputPath = Path.GetFullPath(outputPath) };
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
        }

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            result = result with { MarkdownReportPath = Path.GetFullPath(reportPath) };
            _ = new ScalarPromotionReviewReportGenerator().Generate(result, result.MarkdownReportPath);
            if (!string.IsNullOrWhiteSpace(result.OutputPath))
            {
                File.WriteAllText(result.OutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
            }
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
        IReadOnlySet<ulong> baseAddresses = new HashSet<ulong>();
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
                case "--base-addresses":
                    baseAddresses = ParseBaseAddresses(RequireValue(args, ref index, arg));
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
            BaseAddresses = baseAddresses,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes,
            InterventionWaitMilliseconds = interventionWaitMilliseconds,
            InterventionPollIntervalMilliseconds = interventionPollMilliseconds
        };
    }

    private static (PassiveCaptureDryRunOptions Options, string? JsonOutputPath) ParsePassiveCaptureDryRunOptions(string[] args, bool allowSessionOutOption)
    {
        string? processName = null;
        int? processId = null;
        string? jsonOutputPath = null;
        var samples = 1;
        var maxRegions = 8;
        var maxBytesPerRegion = 64 * 1024;
        long maxTotalBytes = 1024 * 1024;
        var regionOutputLimit = 250;
        var includeImageRegions = false;
        IReadOnlySet<string> regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlySet<ulong> baseAddresses = new HashSet<ulong>();

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
                case "--out" when allowSessionOutOption:
                    _ = RequireValue(args, ref index, arg);
                    break;
                case "--json-out":
                    jsonOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--samples":
                    samples = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--interval-ms" when allowSessionOutOption:
                    _ = RequireValue(args, ref index, arg);
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
                case "--region-output-limit":
                    regionOutputLimit = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--all-regions":
                    regionOutputLimit = 0;
                    break;
                case "--include-image-regions":
                    includeImageRegions = true;
                    break;
                case "--region-ids":
                    regionIds = ParseRegionIds(RequireValue(args, ref index, arg));
                    break;
                case "--base-addresses":
                    baseAddresses = ParseBaseAddresses(RequireValue(args, ref index, arg));
                    break;
                case "--stimulus" when allowSessionOutOption:
                case "--stimulus-note" when allowSessionOutOption:
                case "--intervention-wait-ms" when allowSessionOutOption:
                case "--intervention-poll-ms" when allowSessionOutOption:
                    _ = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown {(allowSessionOutOption ? "capture passive dry-run" : "process inventory")} option: {arg}");
            }
        }

        return (new PassiveCaptureDryRunOptions
        {
            ProcessName = processName,
            ProcessId = processId,
            Samples = samples,
            MaxRegions = maxRegions,
            MaxBytesPerRegion = maxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            RegionOutputLimit = regionOutputLimit,
            IncludeImageRegions = includeImageRegions,
            RegionIds = regionIds,
            BaseAddresses = baseAddresses
        }, jsonOutputPath);
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
        var windowsPerRegion = 1;
        IReadOnlyList<ulong> windowOffsets = [];
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
                case "--windows-per-region":
                    windowsPerRegion = int.Parse(RequireValue(args, ref index, arg));
                    break;
                case "--window-offsets":
                    windowOffsets = ParseOffsetList(RequireValue(args, ref index, arg));
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
            WindowsPerRegion = windowsPerRegion,
            WindowOffsets = windowOffsets,
            StimulusLabel = stimulusLabel,
            StimulusNotes = stimulusNotes,
            InterventionWaitMilliseconds = interventionWaitMilliseconds,
            InterventionPollIntervalMilliseconds = interventionPollMilliseconds
        };
    }

    private static IReadOnlySet<string> ParseRegionIds(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<ulong> ParseBaseAddresses(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseUnsignedHexOrDecimal)
            .ToHashSet();

    private static IReadOnlyList<ulong> ParseOffsetList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseUnsignedHexOrDecimal)
            .Distinct()
            .Order()
            .ToArray();

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToUInt64(normalized, 16)
            : ulong.Parse(normalized);
    }

    private static string FormatHex(ulong value) => $"0x{value:X}";

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

    private static (SessionXrefAnalysisOptions Options, string? OutputPath, string? ReportPath) ParseXrefOptions(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Xref analysis requires a session path.");
        }

        var sessionPath = args[0];
        ulong? targetBaseAddress = null;
        ulong? targetSizeBytes = null;
        IReadOnlyList<ulong> targetOffsets = [];
        IReadOnlyList<ulong> patternOffsets = [];
        var patternLengthBytes = 12;
        var maxHits = 500;
        string? outputPath = null;
        string? reportPath = null;

        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--target-base":
                    targetBaseAddress = ParseUnsignedHexOrDecimal(RequireValue(args, ref index, arg));
                    break;
                case "--target-size":
                    targetSizeBytes = ParseUnsignedHexOrDecimal(RequireValue(args, ref index, arg));
                    break;
                case "--target-offsets":
                    targetOffsets = ParseOffsetList(RequireValue(args, ref index, arg));
                    break;
                case "--pattern-offsets":
                    patternOffsets = ParseOffsetList(RequireValue(args, ref index, arg));
                    break;
                case "--pattern-length":
                    patternLengthBytes = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--max-hits":
                    maxHits = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown analyze xrefs option: {arg}");
            }
        }

        return (new SessionXrefAnalysisOptions
        {
            SessionPath = sessionPath,
            TargetBaseAddress = targetBaseAddress ?? throw new ArgumentException("Xref analysis requires --target-base <address>."),
            TargetSizeBytes = targetSizeBytes,
            TargetOffsets = targetOffsets,
            PatternOffsets = patternOffsets,
            PatternLengthBytes = patternLengthBytes,
            MaxHits = maxHits
        }, outputPath, reportPath);
    }

    private static (string Path, SessionXrefChainSummaryVerificationOptions Options) ParseVerifyXrefChainSummaryOptions(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Verify xref-chain-summary requires a JSON path.");
        }

        var requiredEdges = new List<SessionXrefRequiredEdge>();
        var requiredPairs = new List<SessionXrefRequiredReciprocalPair>();
        var minSupport = 1;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--min-support":
                    minSupport = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--require-edge":
                    requiredEdges.Add(ParseRequiredEdge(RequireValue(args, ref index, arg)));
                    break;
                case "--require-reciprocal":
                    requiredPairs.Add(ParseRequiredPair(RequireValue(args, ref index, arg)));
                    break;
                default:
                    throw new ArgumentException($"Unknown verify xref-chain-summary option: {arg}");
            }
        }

        return (args[0], new SessionXrefChainSummaryVerificationOptions
        {
            MinSupport = minSupport,
            RequiredEdges = requiredEdges,
            RequiredReciprocalPairs = requiredPairs
        });
    }

    private static SessionXrefRequiredEdge ParseRequiredEdge(string value)
    {
        var parts = SplitRequiredPairValue(value, "--require-edge");
        return new SessionXrefRequiredEdge
        {
            SourceBaseAddressHex = FormatHex(ParseUnsignedHexOrDecimal(parts[0])),
            PointerValueHex = FormatHex(ParseUnsignedHexOrDecimal(parts[1]))
        };
    }

    private static SessionXrefRequiredReciprocalPair ParseRequiredPair(string value)
    {
        var parts = SplitRequiredPairValue(value, "--require-reciprocal");
        return new SessionXrefRequiredReciprocalPair
        {
            FirstBaseAddressHex = FormatHex(ParseUnsignedHexOrDecimal(parts[0])),
            SecondBaseAddressHex = FormatHex(ParseUnsignedHexOrDecimal(parts[1]))
        };
    }

    private static string[] SplitRequiredPairValue(string value, string option)
    {
        var parts = value.Contains("=", StringComparison.Ordinal)
            ? value.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : value.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"{option} requires two addresses formatted as 0xSOURCE=0xTARGET.");
        }

        return parts;
    }

    private static (SessionXrefChainSummaryOptions Options, string? OutputPath, string? ReportPath) ParseXrefChainOptions(string[] args)
    {
        var inputPaths = new List<string>();
        var minSupport = 2;
        var top = 100;
        string? outputPath = null;
        string? reportPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--min-support":
                    minSupport = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--top":
                    top = int.Parse(RequireValue(args, ref index, arg), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                case "--json-out":
                    outputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown analyze xref-chain option: {arg}");
                    }

                    inputPaths.Add(arg);
                    break;
            }
        }

        return (new SessionXrefChainSummaryOptions
        {
            InputPaths = inputPaths,
            MinSupport = minSupport,
            Top = top
        }, outputPath, reportPath);
    }

    private static (int Top, string? OutputPath, string? ReportPath, string? NextPlanPath, string? TruthReadinessPath, string? Vec3TruthCandidatePath, string? Vec3CorroborationPath) ParseCompareOptions(string[] args)
    {
        var top = 100;
        string? outputPath = null;
        string? reportPath = null;
        string? nextPlanPath = null;
        string? truthReadinessPath = null;
        string? vec3TruthCandidatePath = null;
        string? vec3CorroborationPath = null;
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
                case "--truth-readiness":
                    truthReadinessPath = RequireValue(args, ref index, arg);
                    break;
                case "--vec3-truth-out":
                case "--coordinate-truth-out":
                    vec3TruthCandidatePath = RequireValue(args, ref index, arg);
                    break;
                case "--vec3-corroboration":
                case "--coordinate-corroboration":
                    vec3CorroborationPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown compare option: {arg}");
            }
        }

        return (top, outputPath, reportPath, nextPlanPath, truthReadinessPath, vec3TruthCandidatePath, vec3CorroborationPath);
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

    private static (IReadOnlyList<string> TruthReadinessPaths, IReadOnlyList<string> ScalarEvidenceSetPaths, IReadOnlyList<string> ScalarTruthRecoveryPaths, IReadOnlyList<string> ScalarTruthPromotionPaths, IReadOnlyList<string> ScalarPromotionReviewPaths, IReadOnlyList<string> RiftPromotedCoordinateLivePaths, string? JsonOutputPath, string? ReportMarkdownPath) ParseReportCapabilityOptions(string[] args)
    {
        var truthReadinessPaths = new List<string>();
        var scalarEvidenceSetPaths = new List<string>();
        var scalarTruthRecoveryPaths = new List<string>();
        var scalarTruthPromotionPaths = new List<string>();
        var scalarPromotionReviewPaths = new List<string>();
        var riftPromotedCoordinateLivePaths = new List<string>();
        string? jsonOutputPath = null;
        string? reportMarkdownPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--truth-readiness":
                    truthReadinessPaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--scalar-evidence-set":
                    scalarEvidenceSetPaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--scalar-truth-recovery":
                    scalarTruthRecoveryPaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--scalar-truth-promotion":
                    scalarTruthPromotionPaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--scalar-promotion-review":
                    scalarPromotionReviewPaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--rift-promoted-coordinate-live":
                    riftPromotedCoordinateLivePaths.Add(RequireValue(args, ref index, arg));
                    break;
                case "--json-out":
                    jsonOutputPath = RequireValue(args, ref index, arg);
                    break;
                case "--report-md":
                    reportMarkdownPath = RequireValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown report capability option: {arg}");
            }
        }

        return (truthReadinessPaths, scalarEvidenceSetPaths, scalarTruthRecoveryPaths, scalarTruthPromotionPaths, scalarPromotionReviewPaths, riftPromotedCoordinateLivePaths, jsonOutputPath, reportMarkdownPath);
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

    private static void WriteOptionalJson<T>(string? outputPath, T result)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, JsonSerializer.Serialize(result, SessionJson.Options));
    }

    private static void WriteOptionalCapabilityReport(string? outputPath, CapabilityStatusResult result)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        _ = new CapabilityStatusReportGenerator().Generate(result, outputPath);
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

    private static int VerifyXrefChainSummary(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyXrefChainSummaryUsage();
            return 0;
        }

        var (path, options) = ParseVerifyXrefChainSummaryOptions(args);
        var result = new SessionXrefChainSummaryVerifier().Verify(path, options);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyScalarCorroboration(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyScalarCorroborationUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify scalar-corroboration requires a JSONL path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify scalar-corroboration option: {args[1]}");
        }

        var result = new ScalarTruthCorroborationVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyScalarEvidenceSet(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyScalarEvidenceSetUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify scalar-evidence-set requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify scalar-evidence-set option: {args[1]}");
        }

        var result = new ScalarEvidenceSetVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyVec3Corroboration(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyVec3CorroborationUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify vec3-corroboration requires a JSONL path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify vec3-corroboration option: {args[1]}");
        }

        var result = new Vec3TruthCorroborationVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyScalarTruthRecovery(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyScalarTruthRecoveryUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify scalar-truth-recovery requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify scalar-truth-recovery option: {args[1]}");
        }

        var result = new ScalarTruthRecoveryVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyVec3TruthRecovery(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyVec3TruthRecoveryUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify vec3-truth-recovery requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify vec3-truth-recovery option: {args[1]}");
        }

        var result = new Vec3TruthRecoveryVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyScalarTruthPromotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyScalarTruthPromotionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify scalar-truth-promotion requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify scalar-truth-promotion option: {args[1]}");
        }

        var result = new ScalarTruthPromotionVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyVec3TruthPromotion(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyVec3TruthPromotionUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify vec3-truth-promotion requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify vec3-truth-promotion option: {args[1]}");
        }

        var result = new Vec3TruthPromotionVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyRiftPromotedCoordinateLive(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyRiftPromotedCoordinateLiveUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify rift-promoted-coordinate-live requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify rift-promoted-coordinate-live option: {args[1]}");
        }

        var result = new RiftPromotedCoordinateLiveVerificationVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyScalarPromotionReview(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyScalarPromotionReviewUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify scalar-promotion-review requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify scalar-promotion-review option: {args[1]}");
        }

        var result = new ScalarPromotionReviewVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyComparisonReadiness(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyComparisonReadinessUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify comparison-readiness requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify comparison-readiness option: {args[1]}");
        }

        var result = new ComparisonTruthReadinessVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
    }

    private static int VerifyCapabilityStatus(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            PrintVerifyCapabilityStatusUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            throw new ArgumentException("Verify capability-status requires a JSON path.");
        }

        if (args.Length > 1)
        {
            throw new ArgumentException($"Unknown verify capability-status option: {args[1]}");
        }

        var result = new CapabilityStatusVerifier().Verify(args[0]);
        Console.WriteLine(JsonSerializer.Serialize(result, SessionJson.Options));
        return result.Success ? 0 : 1;
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
        PrintProcessInventoryUsage();
        PrintAnalyzeSessionUsage();
        PrintAnalyzeXrefsUsage();
        PrintAnalyzeXrefChainUsage();
        PrintReportSessionUsage();
        PrintReportCapabilityUsage();
        PrintRiftAddonCoordsUsage();
        PrintRiftAddonApiObservationsUsage();
        PrintRiftAddonApiTruthUsage();
        PrintRiftMatchAddonCoordsUsage();
        PrintRiftMatchWaypointAnchorsUsage();
        PrintRiftMatchWaypointScalarsUsage();
        PrintRiftCompareWaypointScalarsUsage();
        PrintRiftPlanWaypointScalarFollowupUsage();
        PrintRiftCompareAddonCoordinateMotionUsage();
        PrintRiftCoordinateMirrorContextUsage();
        PrintRiftAddonCorroborationUsage();
        PrintRiftVerifyPromotedCoordinateUsage();
        PrintCompareSessionsUsage();
        PrintCompareScalarSetUsage();
        PrintCompareScalarTruthUsage();
        PrintCompareVec3TruthUsage();
        PrintCompareVec3PromotionUsage();
        PrintCompareScalarPromotionUsage();
        PrintReviewScalarPromotionUsage();
        PrintMigrateUsage();
        PrintSessionPruneUsage();
        PrintSessionInventoryUsage();
        PrintSessionSummaryUsage();
        PrintVerifySessionUsage();
        PrintVerifyXrefChainSummaryUsage();
        PrintVerifyScalarCorroborationUsage();
        PrintVerifyVec3CorroborationUsage();
        PrintVerifyScalarEvidenceSetUsage();
        PrintVerifyScalarTruthRecoveryUsage();
        PrintVerifyVec3TruthRecoveryUsage();
        PrintVerifyScalarTruthPromotionUsage();
        PrintVerifyVec3TruthPromotionUsage();
        PrintVerifyRiftPromotedCoordinateLiveUsage();
        PrintVerifyScalarPromotionReviewUsage();
        PrintVerifyComparisonReadinessUsage();
        PrintVerifyCapabilityStatusUsage();
        Console.WriteLine("riftscan --version [--json]");
    }

    private static int Version(string[] args)
    {
        var json = false;
        foreach (var arg in args)
        {
            if (Is(arg, "--json"))
            {
                json = true;
                continue;
            }

            throw new ArgumentException($"Unknown version option: {arg}");
        }

        var result = BuildVersionResult();
        Console.WriteLine(json
            ? JsonSerializer.Serialize(result, SessionJson.Options)
            : $"riftscan {result.InformationalVersion}");
        return 0;
    }

    private static VersionResult BuildVersionResult()
    {
        var version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var informationalVersion = FirstNonEmpty(version, FallbackVersion);
        var sourceRevisionSeparator = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return new VersionResult
        {
            Version = sourceRevisionSeparator < 0
                ? informationalVersion
                : informationalVersion[..sourceRevisionSeparator],
            InformationalVersion = informationalVersion,
            SourceRevision = sourceRevisionSeparator < 0 || sourceRevisionSeparator == informationalVersion.Length - 1
                ? null
                : informationalVersion[(sourceRevisionSeparator + 1)..]
        };
    }

    private static void PrintCapturePassiveUsage() =>
        Console.WriteLine("riftscan capture passive --process <name> --out sessions/<id> [--dry-run] [--samples 1] [--interval-ms 100] [--max-regions 8] [--max-bytes-per-region 65536] [--max-total-bytes 1048576] [--region-ids ids] [--base-addresses hexes] [--region-output-limit 250|--all-regions] [--stimulus passive_idle]");

    private static void PrintCapturePlanUsage() =>
        Console.WriteLine("riftscan capture plan <source-session-or-plan-json> --pid <id> [--process <name>] --out sessions/<id> [--top-regions 5] [--windows-per-region 3|--window-offsets 0,0x10000] [--stimulus move_forward] [--intervention-wait-ms 1200000] [--intervention-poll-ms 2000]");

    private static void PrintProcessInventoryUsage() =>
        Console.WriteLine("riftscan process inventory --process <name>|--pid <id> [--max-regions 8] [--max-bytes-per-region 65536] [--max-total-bytes 1048576] [--region-ids ids] [--base-addresses hexes] [--region-output-limit 250|--all-regions] [--include-image-regions] [--json-out reports/generated/process-inventory.json]");

    private static void PrintAnalyzeSessionUsage() =>
        Console.WriteLine("riftscan analyze session <session-path> [--all|--top 100]");

    private static void PrintAnalyzeXrefsUsage() =>
        Console.WriteLine("riftscan analyze xrefs <session-path> --target-base 0xADDR [--target-size 0xSIZE] [--target-offsets 0xOFF,...] [--pattern-offsets 0xOFF,...] [--pattern-length 12] [--max-hits 500] [--out reports/generated/session-xrefs.json] [--report-md reports/generated/session-xrefs.md]");

    private static void PrintAnalyzeXrefChainUsage() =>
        Console.WriteLine("riftscan analyze xref-chain <xref-json> [xref-json ...] [--min-support 2] [--top 100] [--out reports/generated/xref-chain.json] [--report-md reports/generated/xref-chain.md]");

    private static void PrintReportSessionUsage() =>
        Console.WriteLine("riftscan report session <session-path> [--top 100]");

    private static void PrintReportCapabilityUsage() =>
        Console.WriteLine("riftscan report capability [--truth-readiness reports/generated/truth-readiness.json ...] [--scalar-evidence-set reports/generated/scalar-evidence-set.json ...] [--scalar-truth-recovery reports/generated/scalar-truth-recovery.json ...] [--scalar-truth-promotion reports/generated/scalar-truth-promotion.json ...] [--scalar-promotion-review reports/generated/scalar-promotion-review.json ...] [--rift-promoted-coordinate-live reports/generated/rift-promoted-coordinate-live.json ...] [--json-out reports/generated/capability-status.json] [--report-md reports/generated/capability-status.md]");

    private static void PrintRiftAddonCoordsUsage() =>
        Console.WriteLine("riftscan rift addon-coords <savedvariables-file-or-directory> [--jsonl-out reports/generated/addon-coordinate-observations.jsonl] [--json-out reports/generated/addon-coordinate-scan.json] [--max-files 5000] [--addon-name ReaderBridgeExport] [--min-file-write-utc 2026-04-29T23:16:00Z]");

    private static void PrintRiftAddonApiObservationsUsage() =>
        Console.WriteLine("riftscan rift addon-api-observations <savedvariables-file-or-directory> [--jsonl-out reports/generated/addon-api-observations.jsonl] [--json-out reports/generated/addon-api-observation-scan.json] [--max-files 5000] [--addon-name ReaderBridgeExport] [--min-file-write-utc 2026-04-29T23:16:00Z]");

    private static void PrintRiftAddonApiTruthUsage() =>
        Console.WriteLine("riftscan rift addon-api-truth <addon-api-observation-scan.json> [--out reports/generated/addon-api-truth-summary.json]");

    private static void PrintRiftMatchAddonCoordsUsage() =>
        Console.WriteLine("riftscan rift match-addon-coords <session-path> --observations reports/generated/addon-coordinate-observations.jsonl [--region-base 0xADDR] [--tolerance 5] [--top 100] [--out reports/generated/session-addon-coordinate-matches.json] [--report-md reports/generated/session-addon-coordinate-matches.md] [--latest-only]");

    private static void PrintRiftMatchWaypointAnchorsUsage() =>
        Console.WriteLine("riftscan rift match-waypoint-anchors <session-path> --anchors reports/generated/addon-api-observation-scan.json [--region-base 0xADDR] [--tolerance 5] [--top 100] [--out reports/generated/session-waypoint-anchor-matches.json]");

    private static void PrintRiftMatchWaypointScalarsUsage() =>
        Console.WriteLine("riftscan rift match-waypoint-scalars <session-path> --anchors reports/generated/addon-api-observation-scan.json [--region-base 0xADDR] [--tolerance 5] [--top 100] [--max-scalar-hits-per-snapshot-axis 64] [--scalar-hits-out reports/generated/session-waypoint-scalar-hits.jsonl] [--out reports/generated/session-waypoint-scalar-matches.json]");

    private static void PrintRiftCompareWaypointScalarsUsage() =>
        Console.WriteLine("riftscan rift compare-waypoint-scalars <scalar-match-json-a> <scalar-match-json-b> [scalar-match-json-c ...] [--delta-tolerance 5] [--top 100] [--out reports/generated/waypoint-scalar-comparison.json]");

    private static void PrintRiftPlanWaypointScalarFollowupUsage() =>
        Console.WriteLine("riftscan rift plan-waypoint-scalar-followup <scalar-match-json> [--top-pairs 10] [--samples 8] [--interval-ms 100] [--max-bytes-per-region 65536] [--out reports/generated/waypoint-scalar-followup-plan.json]");

    private static void PrintRiftCompareAddonCoordinateMotionUsage() =>
        Console.WriteLine("riftscan rift compare-addon-coordinate-motion <pre-match-json> <post-match-json> [--min-delta-distance 1] [--mirror-epsilon 0.001] [--top 100] [--out reports/generated/addon-coordinate-motion.json] [--report-md reports/generated/addon-coordinate-motion.md]");

    private static void PrintRiftCoordinateMirrorContextUsage() =>
        Console.WriteLine("riftscan rift coordinate-mirror-context <motion-comparison-json> --session <session-path> [--window-bytes 256] [--max-pointer-hits 32] [--top 100] [--out reports/generated/coordinate-mirror-context.json] [--report-md reports/generated/coordinate-mirror-context.md]");

    private static void PrintRiftAddonCorroborationUsage() =>
        Console.WriteLine("riftscan rift addon-corroboration --candidates reports/generated/vec3-truth-candidates.jsonl --observations reports/generated/addon-coordinate-observations.jsonl --out reports/generated/vec3-truth-corroboration.jsonl [--json-out reports/generated/addon-coordinate-corroboration.json] [--tolerance 5]");

    private static void PrintRiftVerifyPromotedCoordinateUsage() =>
        Console.WriteLine("riftscan rift verify-promoted-coordinate --promotion reports/generated/vec3-truth-promotion.json --pid <id>|--process rift_x64 --savedvariables \"C:\\Users\\<user>\\OneDrive\\Documents\\RIFT\\Interface\\Saved\" [--candidate-id vec3-promoted-000001] [--out reports/generated/rift-promoted-coordinate-live.json] [--tolerance 5]");

    private static void PrintCompareSessionsUsage() =>
        Console.WriteLine("riftscan compare sessions <session-a> <session-b> [--top 100] [--out reports/generated/comparison.json] [--report-md reports/generated/comparison.md] [--next-plan reports/generated/next-capture-plan.json] [--truth-readiness reports/generated/truth-readiness.json] [--vec3-truth-out reports/generated/vec3-truth-candidates.jsonl] [--vec3-corroboration reports/generated/vec3-truth-corroboration.jsonl]");

    private static void PrintCompareScalarSetUsage() =>
        Console.WriteLine("riftscan compare scalar-set <session-a> <session-b> [session-c ...] [--top 100] [--out reports/generated/scalar-evidence-set.json] [--report-md reports/generated/scalar-evidence-set.md] [--truth-out reports/generated/scalar_truth_candidates.jsonl] [--corroboration reports/generated/scalar_truth_corroboration.jsonl]");

    private static void PrintCompareScalarTruthUsage() =>
        Console.WriteLine("riftscan compare scalar-truth <truth-a.jsonl> <truth-b.jsonl> [truth-c.jsonl ...] [--top 100] [--out reports/generated/scalar-truth-recovery.json]");

    private static void PrintCompareVec3TruthUsage() =>
        Console.WriteLine("riftscan compare vec3-truth <truth-a.jsonl> <truth-b.jsonl> [truth-c.jsonl ...] [--top 100] [--out reports/generated/vec3-truth-recovery.json]");

    private static void PrintCompareVec3PromotionUsage() =>
        Console.WriteLine("riftscan compare vec3-promotion <vec3-truth-recovery.json> --corroboration reports/generated/vec3-truth-corroboration.jsonl [--actor-yaw-recovery reports/generated/scalar-truth-recovery.json] [--top 100] [--out reports/generated/vec3-truth-promotion.json]");

    private static void PrintCompareScalarPromotionUsage() =>
        Console.WriteLine("riftscan compare scalar-promotion <scalar-truth-recovery.json> --corroboration reports/generated/scalar_truth_corroboration.jsonl [--top 100] [--out reports/generated/scalar-truth-promotion.json]");

    private static void PrintReviewScalarPromotionUsage() =>
        Console.WriteLine("riftscan review scalar-promotion <scalar-truth-promotion.json> [--out reports/generated/scalar-promotion-review.json] [--report-md reports/generated/scalar-promotion-review.md]");

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

    private static void PrintVerifyXrefChainSummaryUsage() =>
        Console.WriteLine("riftscan verify xref-chain-summary <xref-chain-summary.json> [--min-support 1] [--require-edge 0xSOURCE=0xTARGET] [--require-reciprocal 0xA=0xB]");

    private static void PrintVerifyScalarCorroborationUsage() =>
        Console.WriteLine("riftscan verify scalar-corroboration <corroboration.jsonl>");

    private static void PrintVerifyVec3CorroborationUsage() =>
        Console.WriteLine("riftscan verify vec3-corroboration <vec3-truth-corroboration.jsonl>");

    private static void PrintVerifyScalarEvidenceSetUsage() =>
        Console.WriteLine("riftscan verify scalar-evidence-set <scalar-evidence-set.json>");

    private static void PrintVerifyScalarTruthRecoveryUsage() =>
        Console.WriteLine("riftscan verify scalar-truth-recovery <scalar-truth-recovery.json>");

    private static void PrintVerifyVec3TruthRecoveryUsage() =>
        Console.WriteLine("riftscan verify vec3-truth-recovery <vec3-truth-recovery.json>");

    private static void PrintVerifyScalarTruthPromotionUsage() =>
        Console.WriteLine("riftscan verify scalar-truth-promotion <scalar-truth-promotion.json>");

    private static void PrintVerifyVec3TruthPromotionUsage() =>
        Console.WriteLine("riftscan verify vec3-truth-promotion <vec3-truth-promotion.json>");

    private static void PrintVerifyRiftPromotedCoordinateLiveUsage() =>
        Console.WriteLine("riftscan verify rift-promoted-coordinate-live <rift-promoted-coordinate-live.json>");

    private static void PrintVerifyScalarPromotionReviewUsage() =>
        Console.WriteLine("riftscan verify scalar-promotion-review <scalar-promotion-review.json>");

    private static void PrintVerifyComparisonReadinessUsage() =>
        Console.WriteLine("riftscan verify comparison-readiness <truth-readiness.json>");

    private static void PrintVerifyCapabilityStatusUsage() =>
        Console.WriteLine("riftscan verify capability-status <capability-status.json>");

    private static bool Is(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsHelp(string value) =>
        Is(value, "--help") || Is(value, "-h") || Is(value, "help");

    private static bool IsVersion(string value) =>
        Is(value, "--version") || Is(value, "-v") || Is(value, "version");

    private static string FirstNonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private sealed class VersionResult
    {
        [JsonPropertyName("result_schema_version")]
        public string ResultSchemaVersion { get; init; } = "riftscan.version_result.v1";

        [JsonPropertyName("success")]
        public bool Success { get; init; } = true;

        [JsonPropertyName("executable")]
        public string Executable { get; init; } = "riftscan";

        [JsonPropertyName("version")]
        public string Version { get; init; } = FallbackVersion;

        [JsonPropertyName("informational_version")]
        public string InformationalVersion { get; init; } = FallbackVersion;

        [JsonPropertyName("source_revision")]
        public string? SourceRevision { get; init; }
    }
}
