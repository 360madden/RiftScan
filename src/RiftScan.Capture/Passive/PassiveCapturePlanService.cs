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
        var regionIds = plan.Regions
            .Where(region => !string.IsNullOrWhiteSpace(region.RegionId))
            .OrderByDescending(region => region.RankScore)
            .ThenBy(region => region.RegionId, StringComparer.OrdinalIgnoreCase)
            .Take(options.TopRegions)
            .Select(region => region.RegionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (regionIds.Count == 0)
        {
            throw new InvalidOperationException("Capture plan contains no region IDs to follow up.");
        }

        return new PassiveCaptureService(processMemoryReader).Capture(new PassiveCaptureOptions
        {
            ProcessName = options.ProcessName,
            ProcessId = options.ProcessId,
            OutputPath = options.OutputPath,
            Samples = options.Samples,
            IntervalMilliseconds = options.IntervalMilliseconds,
            MaxRegions = regionIds.Count,
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = options.MaxTotalBytes,
            IncludeImageRegions = options.IncludeImageRegions,
            RegionIds = regionIds
        });
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
