using RiftScan.Core.Processes;

namespace RiftScan.Capture.Passive;

public sealed class PassiveCaptureDryRunService(IProcessMemoryReader processMemoryReader)
{
    public PassiveCaptureDryRunResult Inspect(PassiveCaptureDryRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var process = ResolveProcess(options);
        var allRegions = processMemoryReader.EnumerateRegions(process.ProcessId)
            .OrderBy(region => region.BaseAddress)
            .ToArray();

        var skipReasonsByRegionId = allRegions.ToDictionary(
            region => region.RegionId,
            region => GetSkipReasons(region, options).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        var selectedRegionIds = new Dictionary<string, (int Order, int EstimatedReadBytes)>(StringComparer.OrdinalIgnoreCase);
        long estimatedBytesPerSample = 0;

        var candidateRegions = allRegions.Where(region => skipReasonsByRegionId[region.RegionId].Length == 0);
        if (options.RegionIds.Count > 0 || options.BaseAddresses.Count > 0)
        {
            candidateRegions = candidateRegions.OrderBy(region => region.BaseAddress);
        }
        else
        {
            candidateRegions = PassiveCaptureRegionPriority.OrderForDefaultCapture(candidateRegions, options.MaxBytesPerRegion);
        }

        foreach (var region in candidateRegions)
        {
            if (selectedRegionIds.Count >= options.MaxRegions || estimatedBytesPerSample >= options.MaxTotalBytes)
            {
                break;
            }

            var remainingBudget = options.MaxTotalBytes - estimatedBytesPerSample;
            var estimatedReadBytes = (int)Math.Min(
                (ulong)Math.Min(options.MaxBytesPerRegion, int.MaxValue),
                Math.Min(region.SizeBytes, (ulong)remainingBudget));

            if (estimatedReadBytes <= 0)
            {
                break;
            }

            var order = selectedRegionIds.Count + 1;
            selectedRegionIds.Add(region.RegionId, (order, estimatedReadBytes));
            estimatedBytesPerSample += estimatedReadBytes;
        }

        var regionRows = new List<PassiveCaptureDryRunRegion>();
        foreach (var region in allRegions)
        {
            var skipReasons = skipReasonsByRegionId[region.RegionId];
            var selected = selectedRegionIds.TryGetValue(region.RegionId, out var selection);
            var selectedOrder = selected ? selection.Order : (int?)null;
            var estimatedReadBytes = selected ? selection.EstimatedReadBytes : 0;
            var reason = ResolveReason(skipReasons, selected, selectedRegionIds.Count, options);
            var displaySkipReasons = ResolveDisplaySkipReasons(skipReasons, selected, selectedRegionIds.Count, options);

            regionRows.Add(new PassiveCaptureDryRunRegion
            {
                RegionId = region.RegionId,
                BaseAddressHex = region.BaseAddressHex,
                SizeBytes = region.SizeBytes,
                State = region.StateName,
                Protection = region.ProtectName,
                Type = region.TypeName,
                Selected = selected,
                SelectedOrder = selectedOrder,
                EstimatedReadBytes = estimatedReadBytes,
                Reason = reason,
                SkipReasons = displaySkipReasons
            });
        }

        var reportedRegions = LimitReportedRegions(regionRows, options.RegionOutputLimit);
        var candidateCount = skipReasonsByRegionId.Values.Count(skipReasons => skipReasons.Length == 0);
        var warnings = BuildWarnings(regionRows, selectedRegionIds.Count, reportedRegions.Count != regionRows.Count);
        return new PassiveCaptureDryRunResult
        {
            Success = selectedRegionIds.Count > 0,
            ProcessId = process.ProcessId,
            ProcessName = process.ProcessName,
            ProcessStartTimeUtc = process.StartTimeUtc,
            MainModulePath = process.MainModulePath,
            Samples = options.Samples,
            MaxRegions = options.MaxRegions,
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = options.MaxTotalBytes,
            IncludeImageRegions = options.IncludeImageRegions,
            RegionOutputLimit = options.RegionOutputLimit,
            TotalRegionCount = allRegions.Length,
            ReportedRegionCount = reportedRegions.Count,
            RegionOutputTruncated = reportedRegions.Count != regionRows.Count,
            CandidateRegionCount = candidateCount,
            SelectedRegionCount = selectedRegionIds.Count,
            SkippedRegionCount = regionRows.Count - selectedRegionIds.Count,
            EstimatedBytesPerSample = estimatedBytesPerSample,
            EstimatedTotalBytes = estimatedBytesPerSample * options.Samples,
            Regions = reportedRegions,
            Warnings = warnings
        };
    }

    private static string ResolveReason(
        IReadOnlyList<string> skipReasons,
        bool selected,
        int selectedCount,
        PassiveCaptureDryRunOptions options)
    {
        if (selected)
        {
            return "selected_for_passive_capture";
        }

        if (skipReasons.Count > 0)
        {
            return "not_selected";
        }

        return selectedCount >= options.MaxRegions
            ? "skipped_after_max_regions"
            : "skipped_after_max_total_bytes";
    }

    private static IReadOnlyList<string> ResolveDisplaySkipReasons(
        IReadOnlyList<string> skipReasons,
        bool selected,
        int selectedCount,
        PassiveCaptureDryRunOptions options)
    {
        if (selected || skipReasons.Count > 0)
        {
            return skipReasons;
        }

        return selectedCount >= options.MaxRegions
            ? ["max_regions_budget_exhausted"]
            : ["max_total_bytes_budget_exhausted"];
    }

    private static IReadOnlyList<PassiveCaptureDryRunRegion> LimitReportedRegions(
        IReadOnlyList<PassiveCaptureDryRunRegion> regions,
        int regionOutputLimit)
    {
        if (regionOutputLimit <= 0 || regions.Count <= regionOutputLimit)
        {
            return regions;
        }

        var reported = regions.Take(regionOutputLimit).ToList();
        foreach (var selected in regions.Where(region => region.Selected))
        {
            if (!reported.Any(region => string.Equals(region.RegionId, selected.RegionId, StringComparison.OrdinalIgnoreCase)))
            {
                reported.Add(selected);
            }
        }

        return reported;
    }

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<PassiveCaptureDryRunRegion> regions, int selectedCount, bool outputTruncated)
    {
        var warnings = new List<string>
        {
            "dry_run_only_no_process_memory_was_read"
        };

        if (outputTruncated)
        {
            warnings.Add("region_output_truncated_use_all_regions_for_full_inventory");
        }

        if (selectedCount == 0)
        {
            warnings.Add("no_regions_would_be_selected");
        }

        if (regions.Any(region => region.SkipReasons.Contains("image_region_excluded", StringComparer.OrdinalIgnoreCase)))
        {
            warnings.Add("image_regions_excluded_use_include_image_regions_if_needed");
        }

        if (regions.Any(region => region.Selected && region.SizeBytes > (ulong)region.EstimatedReadBytes))
        {
            warnings.Add("some_selected_regions_read_capped_to_max_bytes_per_region");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<string> GetSkipReasons(VirtualMemoryRegion region, PassiveCaptureDryRunOptions options)
    {
        if (options.RegionIds.Count > 0 || options.BaseAddresses.Count > 0)
        {
            var requested = options.RegionIds.Contains(region.RegionId) || options.BaseAddresses.Contains(region.BaseAddress);
            if (!requested)
            {
                yield return "not_requested_by_region_filter";
            }
        }

        if (region.SizeBytes == 0)
        {
            yield return "zero_size_region";
        }

        if (region.State != MemoryRegionConstants.MemCommit)
        {
            yield return "not_mem_commit";
        }

        if (!options.IncludeImageRegions && region.Type == MemoryRegionConstants.MemImage)
        {
            yield return "image_region_excluded";
        }

        if ((region.Protect & MemoryRegionConstants.PageGuard) != 0)
        {
            yield return "guard_page";
        }

        var baseProtect = region.Protect & 0xFF;
        if (baseProtect is not (MemoryRegionConstants.PageReadOnly
            or MemoryRegionConstants.PageReadWrite
            or MemoryRegionConstants.PageWriteCopy
            or MemoryRegionConstants.PageExecuteRead
            or MemoryRegionConstants.PageExecuteReadWrite
            or MemoryRegionConstants.PageExecuteWriteCopy))
        {
            yield return "unreadable_protection";
        }
    }

    private ProcessDescriptor ResolveProcess(PassiveCaptureDryRunOptions options)
    {
        if (options.ProcessId is { } processId)
        {
            return processMemoryReader.GetProcessById(processId);
        }

        var processName = options.ProcessName ?? throw new ArgumentException("Dry-run requires --process <name> or --pid <id>.");
        var matches = processMemoryReader.FindProcessesByName(processName);
        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"No process found with name '{processName}'."),
            1 => matches[0],
            _ => throw new InvalidOperationException($"Multiple processes matched '{processName}': {string.Join(", ", matches.Select(match => match.ProcessId))}. Use --pid for an exact target.")
        };
    }

    private static void ValidateOptions(PassiveCaptureDryRunOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProcessName) && options.ProcessId is null)
        {
            throw new ArgumentException("Dry-run requires --process <name> or --pid <id>.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxRegions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxBytesPerRegion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxTotalBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(options.RegionOutputLimit);
    }
}
