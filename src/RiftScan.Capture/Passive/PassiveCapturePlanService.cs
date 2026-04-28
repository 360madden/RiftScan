using System.Text.Json;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Capture.Passive;

public sealed class PassiveCapturePlanService(IProcessMemoryReader processMemoryReader)
{
    private const string SupportedComparisonPlanSchemaVersion = "comparison_next_capture_plan.v1";

    public PassiveCaptureResult CaptureFromPlan(PassiveCapturePlanOptions options)
    {
        ValidateOptions(options);

        var (sourceSessionPath, planPath) = ResolvePlanPath(options.SourceSessionPath);
        var plannedRegions = ReadPlannedRegions(planPath)
            .Where(region => !string.IsNullOrWhiteSpace(region.RegionId) || !string.IsNullOrWhiteSpace(region.BaseAddressHex))
            .OrderByDescending(region => region.RankScore)
            .ThenBy(region => region.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(region => region.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .Take(options.TopRegions)
            .ToArray();

        var regionIds = plannedRegions
            .Select(region => region.RegionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseAddresses = ResolvePlannedBaseAddresses(sourceSessionPath, plannedRegions, regionIds);

        if (regionIds.Count == 0 && baseAddresses.Count == 0)
        {
            throw new InvalidOperationException("Capture plan contains no region IDs or base addresses to follow up.");
        }

        return new PassiveCaptureService(processMemoryReader).Capture(new PassiveCaptureOptions
        {
            ProcessName = options.ProcessName,
            ProcessId = options.ProcessId,
            OutputPath = options.OutputPath,
            Samples = options.Samples,
            IntervalMilliseconds = options.IntervalMilliseconds,
            MaxRegions = Math.Max(regionIds.Count, baseAddresses.Count),
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = options.MaxTotalBytes,
            IncludeImageRegions = options.IncludeImageRegions,
            RegionIds = regionIds,
            BaseAddresses = baseAddresses,
            StimulusLabel = options.StimulusLabel,
            StimulusNotes = options.StimulusNotes
        });
    }

    private static IReadOnlySet<ulong> ResolvePlannedBaseAddresses(
        string sourceSessionPath,
        IReadOnlyList<PassiveCapturePlanRegion> plannedRegions,
        IReadOnlySet<string> regionIds)
    {
        var baseAddresses = plannedRegions
            .Where(region => !string.IsNullOrWhiteSpace(region.BaseAddressHex))
            .Select(region => ParseHex(region.BaseAddressHex))
            .ToHashSet();

        if (baseAddresses.Count > 0)
        {
            return baseAddresses;
        }

        var regionsPath = Path.Combine(sourceSessionPath, "regions.json");
        if (!File.Exists(regionsPath))
        {
            return baseAddresses;
        }

        var sourceRegions = JsonSerializer.Deserialize<RegionMap>(File.ReadAllText(regionsPath), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize regions.json.");
        foreach (var region in sourceRegions.Regions.Where(region => regionIds.Contains(region.RegionId)))
        {
            if (!string.IsNullOrWhiteSpace(region.BaseAddressHex))
            {
                baseAddresses.Add(ParseHex(region.BaseAddressHex));
            }
        }

        return baseAddresses;
    }

    private static (string SourceSessionPath, string PlanPath) ResolvePlanPath(string sourceSessionPath)
    {
        var fullPath = Path.GetFullPath(sourceSessionPath);
        if (File.Exists(fullPath))
        {
            return (Path.GetDirectoryName(fullPath)!, fullPath);
        }

        var planPath = Path.Combine(fullPath, "next_capture_plan.json");
        if (!File.Exists(planPath))
        {
            throw new InvalidOperationException($"Capture plan not found: {planPath}");
        }

        return (fullPath, planPath);
    }

    private static IReadOnlyList<PassiveCapturePlanRegion> ReadPlannedRegions(string planPath)
    {
        var json = File.ReadAllText(planPath);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("target_region_priorities", out _))
        {
            var comparisonPlan = JsonSerializer.Deserialize<PassiveComparisonCapturePlanDocument>(json, SessionJson.Options)
                ?? throw new InvalidOperationException($"Could not deserialize comparison capture plan: {planPath}.");
            if (!string.Equals(comparisonPlan.SchemaVersion, SupportedComparisonPlanSchemaVersion, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported comparison capture plan schema_version '{comparisonPlan.SchemaVersion}'. Expected '{SupportedComparisonPlanSchemaVersion}'.");
            }

            return comparisonPlan.TargetRegionPriorities
                .Select(target => new PassiveCapturePlanRegion
                {
                    RegionId = string.IsNullOrWhiteSpace(target.BaseAddressHex)
                        ? FirstNonEmpty(target.SessionBRegionId, target.SessionARegionId)
                        : string.Empty,
                    RankScore = target.PriorityScore,
                    BaseAddressHex = target.BaseAddressHex,
                    Reason = target.Reason
                })
                .ToArray();
        }

        if (document.RootElement.TryGetProperty("regions", out _))
        {
            var plan = JsonSerializer.Deserialize<PassiveCapturePlanDocument>(json, SessionJson.Options)
                ?? throw new InvalidOperationException($"Could not deserialize capture plan: {planPath}.");
            return plan.Regions;
        }

        throw new InvalidOperationException($"Unsupported capture plan schema: {planPath}.");
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static ulong ParseHex(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.Parse(text, System.Globalization.NumberStyles.HexNumber);
    }

    private static void ValidateOptions(PassiveCapturePlanOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SourceSessionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputPath);
        if (string.IsNullOrWhiteSpace(options.ProcessName) && options.ProcessId is null)
        {
            throw new ArgumentException("Capture plan requires --process <name> or --pid <id>.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.TopRegions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Samples);
        ArgumentOutOfRangeException.ThrowIfNegative(options.IntervalMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxBytesPerRegion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxTotalBytes);
        ValidateStimulusLabel(options.StimulusLabel);
    }

    private static void ValidateStimulusLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        if (!PassiveCaptureService.ValidStimulusLabels.Contains(label))
        {
            throw new ArgumentException($"Unknown stimulus label: {label}.");
        }
    }
}
