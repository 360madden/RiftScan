using System.Text.Json;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Capture.Passive;

public sealed class PassiveCapturePlanService(IProcessMemoryReader processMemoryReader)
{
    public PassiveCaptureResult CaptureFromPlan(PassiveCapturePlanOptions options)
    {
        ValidateOptions(options);

        var sourceSessionPath = Path.GetFullPath(options.SourceSessionPath);
        var planPath = Path.Combine(sourceSessionPath, "next_capture_plan.json");
        if (!File.Exists(planPath))
        {
            throw new InvalidOperationException($"Capture plan not found: {planPath}");
        }

        var plan = JsonSerializer.Deserialize<PassiveCapturePlanDocument>(File.ReadAllText(planPath), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize next_capture_plan.json.");
        var plannedRegions = plan.Regions
            .Where(region => !string.IsNullOrWhiteSpace(region.RegionId))
            .OrderByDescending(region => region.RankScore)
            .ThenBy(region => region.RegionId, StringComparer.OrdinalIgnoreCase)
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
            BaseAddresses = baseAddresses
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
    }
}
