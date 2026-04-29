using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Xrefs;

public sealed record SessionXrefAnalysisOptions
{
    public string SessionPath { get; init; } = string.Empty;

    public ulong TargetBaseAddress { get; init; }

    public ulong? TargetSizeBytes { get; init; }

    public IReadOnlyList<ulong> TargetOffsets { get; init; } = [];

    public IReadOnlyList<ulong> PatternOffsets { get; init; } = [];

    public int PatternLengthBytes { get; init; } = 12;

    public int MaxHits { get; init; } = 500;
}

public sealed class SessionXrefAnalysisService
{
    private const int PointerWidthBytes = sizeof(ulong);
    private const int PointerStrideBytes = sizeof(ulong);
    private const int PatternStrideBytes = sizeof(float);

    public SessionXrefAnalysisResult Analyze(SessionXrefAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.PatternLengthBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaxHits);

        var sessionPath = Path.GetFullPath(options.SessionPath);
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before xref analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(sessionPath, "manifest.json");
        var regions = ReadJson<RegionMap>(sessionPath, "regions.json");
        var snapshots = ReadSnapshotIndex(sessionPath);
        var targetRegionIds = regions.Regions
            .Where(region => ParseUnsignedHexOrDecimal(region.BaseAddressHex) == options.TargetBaseAddress)
            .Select(region => region.RegionId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(regionId => regionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var targetOffsets = options.TargetOffsets
            .Distinct()
            .OrderBy(offset => offset)
            .ToArray();
        var warnings = new List<string>();
        var diagnostics = new List<string>
        {
            "session_verified_before_xref_analysis",
            "offline_snapshot_scan_only",
            $"pointer_width_bytes={PointerWidthBytes}",
            $"pointer_scan_stride_bytes={PointerStrideBytes}",
            $"pattern_scan_stride_bytes={PatternStrideBytes}"
        };

        var targetSize = ResolveTargetSize(options.TargetBaseAddress, options.TargetSizeBytes, targetOffsets, regions, snapshots, warnings);
        var targetEndExclusive = CheckedEndAddress(options.TargetBaseAddress, targetSize);
        var exactTargets = targetOffsets
            .Select(offset => CheckedEndAddress(options.TargetBaseAddress, offset))
            .ToHashSet();

        var snapshotBytes = snapshots
            .Select(snapshot => new LoadedSnapshot(snapshot, File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path))))
            .ToArray();

        var pointerSummary = ScanPointers(snapshotBytes, options.TargetBaseAddress, targetEndExclusive, exactTargets, targetRegionIds, options.MaxHits);
        if (pointerSummary.PointerHitCount > pointerSummary.PointerHits.Count)
        {
            warnings.Add("pointer_hits_truncated_by_max_hits");
        }

        var patternDefinitions = BuildPatternDefinitions(
            snapshotBytes,
            options.TargetBaseAddress,
            options.PatternOffsets,
            options.PatternLengthBytes,
            warnings);
        var patternSummary = ScanPatterns(snapshotBytes, patternDefinitions, options.TargetBaseAddress, targetRegionIds, options.MaxHits);
        if (patternSummary.PatternHitCount > patternSummary.PatternHits.Count)
        {
            warnings.Add("pattern_hits_truncated_by_max_hits");
        }

        if (targetRegionIds.Length == 0)
        {
            warnings.Add("target_base_not_present_in_regions_json");
        }

        if (targetOffsets.Length == 0)
        {
            warnings.Add("no_exact_target_offsets_requested");
        }

        if (options.PatternOffsets.Count > 0 && patternDefinitions.Count == 0)
        {
            warnings.Add("pattern_offsets_requested_but_no_patterns_built");
        }

        if (pointerSummary.PointerHitCount == 0)
        {
            warnings.Add("no_pointer_xrefs_found");
        }

        return new SessionXrefAnalysisResult
        {
            Success = true,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            AnalyzerSources = ["manifest.json", "regions.json", "snapshots/index.jsonl", "snapshots/*.bin"],
            TargetBaseAddressHex = FormatHex(options.TargetBaseAddress),
            TargetSizeBytes = ToLong(targetSize, nameof(options.TargetSizeBytes)),
            TargetRegionIds = targetRegionIds,
            TargetOffsets = targetOffsets
                .Select(offset => new SessionXrefTargetOffset
                {
                    OffsetHex = FormatHex(offset),
                    AbsoluteAddressHex = FormatHex(CheckedEndAddress(options.TargetBaseAddress, offset))
                })
                .ToArray(),
            SnapshotCount = snapshots.Count,
            RegionCount = regions.Regions.Count,
            RegionsScanned = snapshotBytes
                .Select(snapshot => snapshot.Entry.RegionId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            BytesScanned = snapshotBytes.Sum(snapshot => (long)snapshot.Bytes.Length),
            PointerHitCount = pointerSummary.PointerHitCount,
            ExactTargetPointerCount = pointerSummary.ExactTargetPointerCount,
            OutsideTargetRegionPointerCount = pointerSummary.OutsideTargetRegionPointerCount,
            OutsideExactTargetPointerCount = pointerSummary.OutsideExactTargetPointerCount,
            PointerHits = pointerSummary.PointerHits,
            PatternDefinitionCount = patternDefinitions.Count,
            PatternHitCount = patternSummary.PatternHitCount,
            OutsideTargetRegionPatternHitCount = patternSummary.OutsideTargetRegionPatternHitCount,
            PatternHits = patternSummary.PatternHits,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    public void WriteMarkdown(SessionXrefAnalysisResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, BuildMarkdown(result));
    }

    private static PointerScanSummary ScanPointers(
        IReadOnlyList<LoadedSnapshot> snapshots,
        ulong targetBaseAddress,
        ulong targetEndExclusive,
        IReadOnlySet<ulong> exactTargets,
        IReadOnlyCollection<string> targetRegionIds,
        int maxHits)
    {
        var priorityHits = new List<SessionXrefPointerHit>();
        var otherHits = new List<SessionXrefPointerHit>();
        var pointerHitCount = 0;
        var exactTargetPointerCount = 0;
        var outsideTargetRegionPointerCount = 0;
        var outsideExactTargetPointerCount = 0;

        foreach (var snapshot in snapshots)
        {
            var sourceBaseAddress = ParseUnsignedHexOrDecimal(snapshot.Entry.BaseAddressHex);
            var sourceIsTargetRegion = IsTargetRegion(snapshot.Entry, sourceBaseAddress, targetBaseAddress, targetRegionIds);
            for (var offset = 0; offset + PointerWidthBytes <= snapshot.Bytes.Length; offset += PointerStrideBytes)
            {
                var pointerValue = BinaryPrimitives.ReadUInt64LittleEndian(snapshot.Bytes.AsSpan(offset, PointerWidthBytes));
                var pointsIntoTarget = pointerValue >= targetBaseAddress && pointerValue < targetEndExclusive;
                var exactTarget = exactTargets.Contains(pointerValue);
                if (!pointsIntoTarget && !exactTarget)
                {
                    continue;
                }

                pointerHitCount++;
                if (exactTarget)
                {
                    exactTargetPointerCount++;
                }

                if (!sourceIsTargetRegion)
                {
                    outsideTargetRegionPointerCount++;
                    if (exactTarget)
                    {
                        outsideExactTargetPointerCount++;
                    }
                }

                if (maxHits <= 0)
                {
                    continue;
                }

                var sourceAbsoluteAddress = CheckedEndAddress(sourceBaseAddress, (ulong)offset);
                var hit = new SessionXrefPointerHit
                {
                    SnapshotId = snapshot.Entry.SnapshotId,
                    SourceRegionId = snapshot.Entry.RegionId,
                    SourceBaseAddressHex = snapshot.Entry.BaseAddressHex,
                    SourceOffsetHex = FormatHex((ulong)offset),
                    SourceAbsoluteAddressHex = FormatHex(sourceAbsoluteAddress),
                    PointerValueHex = FormatHex(pointerValue),
                    TargetOffsetHex = pointerValue >= targetBaseAddress
                        ? FormatHex(pointerValue - targetBaseAddress)
                        : string.Empty,
                    MatchKind = exactTarget ? "exact_target_offset_pointer" : "pointer_into_target_region",
                    SourceIsTargetRegion = sourceIsTargetRegion
                };
                if (exactTarget || !sourceIsTargetRegion)
                {
                    priorityHits.Add(hit);
                }
                else if (otherHits.Count < maxHits)
                {
                    otherHits.Add(hit);
                }
            }
        }

        var orderedHits = OrderPointerHits(priorityHits)
            .Concat(OrderPointerHits(otherHits))
            .Take(maxHits)
            .ToArray();

        return new PointerScanSummary(
            pointerHitCount,
            exactTargetPointerCount,
            outsideTargetRegionPointerCount,
            outsideExactTargetPointerCount,
            orderedHits);
    }

    private static IReadOnlyList<PatternDefinition> BuildPatternDefinitions(
        IReadOnlyList<LoadedSnapshot> snapshots,
        ulong targetBaseAddress,
        IReadOnlyList<ulong> patternOffsets,
        int patternLengthBytes,
        ICollection<string> warnings)
    {
        if (patternOffsets.Count == 0)
        {
            return [];
        }

        var targetSnapshot = snapshots
            .Where(snapshot => ParseUnsignedHexOrDecimal(snapshot.Entry.BaseAddressHex) == targetBaseAddress)
            .OrderBy(snapshot => snapshot.Entry.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(snapshot => snapshot.Entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (targetSnapshot is null)
        {
            warnings.Add("pattern_source_target_snapshot_missing");
            return [];
        }

        var definitions = new List<PatternDefinition>();
        foreach (var offset in patternOffsets.Distinct().OrderBy(offset => offset))
        {
            if (offset > int.MaxValue ||
                offset + (ulong)patternLengthBytes > (ulong)targetSnapshot.Bytes.Length)
            {
                warnings.Add($"pattern_offset_out_of_range:{FormatHex(offset)}");
                continue;
            }

            var patternBytes = new byte[patternLengthBytes];
            Array.Copy(targetSnapshot.Bytes, (int)offset, patternBytes, 0, patternLengthBytes);
            definitions.Add(new PatternDefinition(
                $"pattern-{definitions.Count + 1:000000}",
                targetSnapshot.Entry.SnapshotId,
                FormatHex(offset),
                patternLengthBytes,
                patternBytes));
        }

        if (definitions.Count > 0)
        {
            warnings.Add("pattern_bytes_derived_from_first_target_snapshot");
        }

        return definitions;
    }

    private static PatternScanSummary ScanPatterns(
        IReadOnlyList<LoadedSnapshot> snapshots,
        IReadOnlyList<PatternDefinition> patterns,
        ulong targetBaseAddress,
        IReadOnlyCollection<string> targetRegionIds,
        int maxHits)
    {
        if (patterns.Count == 0)
        {
            return new PatternScanSummary(0, 0, []);
        }

        var priorityHits = new List<SessionXrefPatternHit>();
        var otherHits = new List<SessionXrefPatternHit>();
        var patternHitCount = 0;
        var outsideTargetRegionPatternHitCount = 0;

        foreach (var snapshot in snapshots)
        {
            var sourceBaseAddress = ParseUnsignedHexOrDecimal(snapshot.Entry.BaseAddressHex);
            var sourceIsTargetRegion = IsTargetRegion(snapshot.Entry, sourceBaseAddress, targetBaseAddress, targetRegionIds);

            foreach (var pattern in patterns)
            {
                for (var offset = 0; offset + pattern.PatternLengthBytes <= snapshot.Bytes.Length; offset += PatternStrideBytes)
                {
                    if (!snapshot.Bytes.AsSpan(offset, pattern.PatternLengthBytes).SequenceEqual(pattern.Bytes))
                    {
                        continue;
                    }

                    patternHitCount++;
                    if (!sourceIsTargetRegion)
                    {
                        outsideTargetRegionPatternHitCount++;
                    }

                    if (maxHits <= 0)
                    {
                        continue;
                    }

                    var sourceAbsoluteAddress = CheckedEndAddress(sourceBaseAddress, (ulong)offset);
                    var hit = new SessionXrefPatternHit
                    {
                        PatternId = pattern.PatternId,
                        PatternSourceSnapshotId = pattern.PatternSourceSnapshotId,
                        PatternSourceOffsetHex = pattern.PatternSourceOffsetHex,
                        PatternLengthBytes = pattern.PatternLengthBytes,
                        SnapshotId = snapshot.Entry.SnapshotId,
                        SourceRegionId = snapshot.Entry.RegionId,
                        SourceBaseAddressHex = snapshot.Entry.BaseAddressHex,
                        SourceOffsetHex = FormatHex((ulong)offset),
                        SourceAbsoluteAddressHex = FormatHex(sourceAbsoluteAddress),
                        MatchKind = "exact_byte_pattern",
                        SourceIsTargetRegion = sourceIsTargetRegion
                    };
                    if (!sourceIsTargetRegion)
                    {
                        priorityHits.Add(hit);
                    }
                    else if (otherHits.Count < maxHits)
                    {
                        otherHits.Add(hit);
                    }
                }
            }
        }

        var orderedHits = OrderPatternHits(priorityHits)
            .Concat(OrderPatternHits(otherHits))
            .Take(maxHits)
            .ToArray();

        return new PatternScanSummary(patternHitCount, outsideTargetRegionPatternHitCount, orderedHits);
    }

    private static bool IsTargetRegion(
        SnapshotIndexEntry snapshot,
        ulong sourceBaseAddress,
        ulong targetBaseAddress,
        IReadOnlyCollection<string> targetRegionIds) =>
        sourceBaseAddress == targetBaseAddress ||
        targetRegionIds.Contains(snapshot.RegionId, StringComparer.OrdinalIgnoreCase);

    private static IOrderedEnumerable<SessionXrefPointerHit> OrderPointerHits(IEnumerable<SessionXrefPointerHit> hits) =>
        hits
            .OrderBy(hit => hit.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.SourceBaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => ParseUnsignedHexOrDecimal(hit.SourceOffsetHex));

    private static IOrderedEnumerable<SessionXrefPatternHit> OrderPatternHits(IEnumerable<SessionXrefPatternHit> hits) =>
        hits
            .OrderBy(hit => hit.PatternId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.SourceBaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => ParseUnsignedHexOrDecimal(hit.SourceOffsetHex));

    private static ulong ResolveTargetSize(
        ulong targetBaseAddress,
        ulong? requestedTargetSize,
        IReadOnlyList<ulong> targetOffsets,
        RegionMap regions,
        IReadOnlyList<SnapshotIndexEntry> snapshots,
        ICollection<string> warnings)
    {
        if (requestedTargetSize is > 0)
        {
            return requestedTargetSize.Value;
        }

        var regionSizes = regions.Regions
            .Where(region => ParseUnsignedHexOrDecimal(region.BaseAddressHex) == targetBaseAddress)
            .Select(region => (ulong)Math.Max(0, region.SizeBytes));
        var snapshotSizes = snapshots
            .Where(snapshot => ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex) == targetBaseAddress)
            .Select(snapshot => (ulong)Math.Max(0, snapshot.SizeBytes));
        var capturedSize = regionSizes.Concat(snapshotSizes).DefaultIfEmpty(0UL).Max();
        if (capturedSize > 0)
        {
            return capturedSize;
        }

        if (targetOffsets.Count > 0)
        {
            warnings.Add("target_size_inferred_from_max_target_offset");
            return checked(targetOffsets.Max() + PointerWidthBytes);
        }

        throw new ArgumentException("xref analysis requires --target-size when --target-base is not present in regions.json and no --target-offsets were provided.");
    }

    private static string BuildMarkdown(SessionXrefAnalysisResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# RiftScan Session Xref Report - {result.SessionId}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Session path: `{result.SessionPath}`");
        builder.AppendLine($"- Target base: `{result.TargetBaseAddressHex}`");
        builder.AppendLine($"- Target size bytes: `{result.TargetSizeBytes}`");
        builder.AppendLine($"- Target regions: `{string.Join(", ", result.TargetRegionIds.DefaultIfEmpty("none"))}`");
        builder.AppendLine($"- Snapshots scanned: `{result.SnapshotCount}`");
        builder.AppendLine($"- Regions scanned: `{result.RegionsScanned}`");
        builder.AppendLine($"- Bytes scanned: `{result.BytesScanned}`");
        builder.AppendLine($"- Pointer hits: `{result.PointerHitCount}`");
        builder.AppendLine($"- Exact target pointer hits: `{result.ExactTargetPointerCount}`");
        builder.AppendLine($"- Outside-target pointer hits: `{result.OutsideTargetRegionPointerCount}`");
        builder.AppendLine($"- Pattern hits: `{result.PatternHitCount}`");
        builder.AppendLine($"- Outside-target pattern hits: `{result.OutsideTargetRegionPatternHitCount}`");
        builder.AppendLine();

        builder.AppendLine("## Target offsets");
        builder.AppendLine();
        if (result.TargetOffsets.Count == 0)
        {
            builder.AppendLine("- No exact target offsets requested.");
        }
        else
        {
            builder.AppendLine("| Offset | Absolute address |");
            builder.AppendLine("|---|---|");
            foreach (var targetOffset in result.TargetOffsets)
            {
                builder.AppendLine($"| `{targetOffset.OffsetHex}` | `{targetOffset.AbsoluteAddressHex}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Pointer hits");
        builder.AppendLine();
        if (result.PointerHits.Count == 0)
        {
            builder.AppendLine("- No retained pointer hits.");
        }
        else
        {
            builder.AppendLine("| Snapshot | Source region | Source offset | Pointer value | Target offset | Match | Source is target |");
            builder.AppendLine("|---|---|---:|---:|---:|---|---|");
            foreach (var hit in result.PointerHits.Take(100))
            {
                builder.AppendLine($"| `{hit.SnapshotId}` | `{hit.SourceRegionId}` | `{hit.SourceOffsetHex}` | `{hit.PointerValueHex}` | `{hit.TargetOffsetHex}` | `{hit.MatchKind}` | `{hit.SourceIsTargetRegion}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Pattern hits");
        builder.AppendLine();
        if (result.PatternHits.Count == 0)
        {
            builder.AppendLine("- No retained pattern hits.");
        }
        else
        {
            builder.AppendLine("| Pattern | Pattern offset | Snapshot | Source region | Source offset | Length | Source is target |");
            builder.AppendLine("|---|---:|---|---|---:|---:|---|");
            foreach (var hit in result.PatternHits.Take(100))
            {
                builder.AppendLine($"| `{hit.PatternId}` | `{hit.PatternSourceOffsetHex}` | `{hit.SnapshotId}` | `{hit.SourceRegionId}` | `{hit.SourceOffsetHex}` | `{hit.PatternLengthBytes}` | `{hit.SourceIsTargetRegion}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in result.Warnings.DefaultIfEmpty("none"))
        {
            builder.AppendLine($"- `{warning}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Limitations");
        builder.AppendLine();
        builder.AppendLine("- Offline xrefs are candidate evidence only; pointer-shaped values do not prove semantic ownership by themselves.");
        builder.AppendLine("- Exact byte pattern hits prove repeated bytes in captured snapshots, not final vector, camera, or actor truth.");
        builder.AppendLine("- Use this report to choose the next smallest capture or owner-chain validation step.");
        return builder.ToString();
    }

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
            .ToArray();
    }

    private static ulong CheckedEndAddress(ulong baseAddress, ulong offset)
    {
        if (ulong.MaxValue - baseAddress < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Address calculation overflowed.");
        }

        return baseAddress + offset;
    }

    private static long ToLong(ulong value, string name) =>
        value > long.MaxValue
            ? throw new ArgumentOutOfRangeException(name, "Value exceeds Int64.MaxValue for JSON size field.")
            : (long)value;

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static string FormatHex(ulong value) => $"0x{value:X}";

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed record LoadedSnapshot(SnapshotIndexEntry Entry, byte[] Bytes);

    private sealed record PatternDefinition(
        string PatternId,
        string PatternSourceSnapshotId,
        string PatternSourceOffsetHex,
        int PatternLengthBytes,
        byte[] Bytes);

    private sealed record PointerScanSummary(
        int PointerHitCount,
        int ExactTargetPointerCount,
        int OutsideTargetRegionPointerCount,
        int OutsideExactTargetPointerCount,
        IReadOnlyList<SessionXrefPointerHit> PointerHits);

    private sealed record PatternScanSummary(
        int PatternHitCount,
        int OutsideTargetRegionPatternHitCount,
        IReadOnlyList<SessionXrefPatternHit> PatternHits);
}

public sealed record SessionXrefAnalysisResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_xref_analysis_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "session_xref_analysis";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("target_base_address_hex")]
    public string TargetBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("target_size_bytes")]
    public long TargetSizeBytes { get; init; }

    [JsonPropertyName("target_region_ids")]
    public IReadOnlyList<string> TargetRegionIds { get; init; } = [];

    [JsonPropertyName("target_offsets")]
    public IReadOnlyList<SessionXrefTargetOffset> TargetOffsets { get; init; } = [];

    [JsonPropertyName("snapshot_count")]
    public int SnapshotCount { get; init; }

    [JsonPropertyName("region_count")]
    public int RegionCount { get; init; }

    [JsonPropertyName("regions_scanned")]
    public int RegionsScanned { get; init; }

    [JsonPropertyName("bytes_scanned")]
    public long BytesScanned { get; init; }

    [JsonPropertyName("pointer_hit_count")]
    public int PointerHitCount { get; init; }

    [JsonPropertyName("exact_target_pointer_count")]
    public int ExactTargetPointerCount { get; init; }

    [JsonPropertyName("outside_target_region_pointer_count")]
    public int OutsideTargetRegionPointerCount { get; init; }

    [JsonPropertyName("outside_exact_target_pointer_count")]
    public int OutsideExactTargetPointerCount { get; init; }

    [JsonPropertyName("pointer_hits")]
    public IReadOnlyList<SessionXrefPointerHit> PointerHits { get; init; } = [];

    [JsonPropertyName("pattern_definition_count")]
    public int PatternDefinitionCount { get; init; }

    [JsonPropertyName("pattern_hit_count")]
    public int PatternHitCount { get; init; }

    [JsonPropertyName("outside_target_region_pattern_hit_count")]
    public int OutsideTargetRegionPatternHitCount { get; init; }

    [JsonPropertyName("pattern_hits")]
    public IReadOnlyList<SessionXrefPatternHit> PatternHits { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record SessionXrefTargetOffset
{
    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("absolute_address_hex")]
    public string AbsoluteAddressHex { get; init; } = string.Empty;
}

public sealed record SessionXrefPointerHit
{
    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("source_region_id")]
    public string SourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("pointer_value_hex")]
    public string PointerValueHex { get; init; } = string.Empty;

    [JsonPropertyName("target_offset_hex")]
    public string TargetOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("match_kind")]
    public string MatchKind { get; init; } = string.Empty;

    [JsonPropertyName("source_is_target_region")]
    public bool SourceIsTargetRegion { get; init; }
}

public sealed record SessionXrefPatternHit
{
    [JsonPropertyName("pattern_id")]
    public string PatternId { get; init; } = string.Empty;

    [JsonPropertyName("pattern_source_snapshot_id")]
    public string PatternSourceSnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("pattern_source_offset_hex")]
    public string PatternSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("pattern_length_bytes")]
    public int PatternLengthBytes { get; init; }

    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("source_region_id")]
    public string SourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("match_kind")]
    public string MatchKind { get; init; } = string.Empty;

    [JsonPropertyName("source_is_target_region")]
    public bool SourceIsTargetRegion { get; init; }
}
