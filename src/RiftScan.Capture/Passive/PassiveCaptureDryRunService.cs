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

        var regionRows = new List<PassiveCaptureDryRunRegion>();
        var selectedCount = 0;
        long estimatedBytesPerSample = 0;

        foreach (var region in allRegions)
        {
            var skipReasons = GetSkipReasons(region, options).ToArray();
            var remainingBudget = options.MaxTotalBytes - estimatedBytesPerSample;
            var canFitBudget = remainingBudget > 0;
            var selected = skipReasons.Length == 0 && selectedCount < options.MaxRegions && canFitBudget;
            int estimatedReadBytes = 0;
            int? selectedOrder = null;
            string reason;

            if (selected)
            {
                estimatedReadBytes = (int)Math.Min(
                    (ulong)Math.Min(options.MaxBytesPerRegion, int.MaxValue),
                    Math.Min(region.SizeBytes, (ulong)remainingBudget));
                selected = estimatedReadBytes > 0;
            }

            if (selected)
            {
                selectedCount++;
                selectedOrder = selectedCount;
                estimatedBytesPerSample += estimatedReadBytes;
                reason = "selected_for_passive_capture";
            }
            else if (skipReasons.Length == 0 && selectedCount >= options.MaxRegions)
            {
                reason = "skipped_after_max_regions";
                skipReasons = ["max_regions_budget_exhausted"];
            }
            else if (skipReasons.Length == 0 && !canFitBudget)
            {
                reason = "skipped_after_max_total_bytes";
                skipReasons = ["max_total_bytes_budget_exhausted"];
            }
            else
            {
                reason = "not_selected";
            }

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
                SkipReasons = skipReasons
            });
        }

        var reportedRegions = LimitReportedRegions(regionRows, options.RegionOutputLimit);
        var candidateCount = regionRows.Count(region => region.SkipReasons.Count == 0 || region.Selected);
        var warnings = BuildWarnings(regionRows, selectedCount, reportedRegions.Count != regionRows.Count);
        return new PassiveCaptureDryRunResult
        {
            Success = selectedCount > 0,
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
            SelectedRegionCount = selectedCount,
            SkippedRegionCount = regionRows.Count - selectedCount,
            EstimatedBytesPerSample = estimatedBytesPerSample,
            EstimatedTotalBytes = estimatedBytesPerSample * options.Samples,
            Regions = reportedRegions,
            Warnings = warnings
        };
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

        if (regions.Any(region => region.SkipReasons.Contains("region_size_exceeds_max_bytes_per_region", StringComparer.OrdinalIgnoreCase)))
        {
            warnings.Add("some_regions_excluded_by_max_bytes_per_region");
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
        else if (region.SizeBytes > (ulong)options.MaxBytesPerRegion)
        {
            yield return "region_size_exceeds_max_bytes_per_region";
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
