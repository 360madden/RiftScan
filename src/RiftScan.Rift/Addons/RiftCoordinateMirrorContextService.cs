using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed record RiftCoordinateMirrorContextOptions
{
    public string MotionComparisonPath { get; init; } = string.Empty;

    public string SessionPath { get; init; } = string.Empty;

    public int WindowBytes { get; init; } = 256;

    public int MaxPointerHits { get; init; } = 32;

    public int Top { get; init; } = 100;
}

public sealed class RiftCoordinateMirrorContextService
{
    private const int FloatStrideBytes = sizeof(float);
    private const int PointerStrideBytes = sizeof(ulong);
    private const int Vec3ByteLength = sizeof(float) * 3;
    private const double PlausibleCoordinateAbsMax = 100_000;

    public RiftCoordinateMirrorContextResult Analyze(RiftCoordinateMirrorContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.MotionComparisonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SessionPath);
        if (options.WindowBytes < Vec3ByteLength)
        {
            throw new ArgumentOutOfRangeException(nameof(options.WindowBytes), "Window bytes must be at least 12.");
        }

        if (options.MaxPointerHits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxPointerHits), "Maximum pointer hits must be non-negative.");
        }

        if (options.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Top), "Top must be positive.");
        }

        var motionComparisonPath = Path.GetFullPath(options.MotionComparisonPath);
        var sessionPath = Path.GetFullPath(options.SessionPath);
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before coordinate-mirror context analysis: {issues}");
        }

        var manifest = ReadSessionJson<SessionManifest>(sessionPath, "manifest.json");
        var regions = ReadSessionJson<RegionMap>(sessionPath, "regions.json").Regions;
        var snapshots = ReadSnapshotIndex(sessionPath);
        var comparison = ReadJson<RiftAddonCoordinateMotionComparisonResult>(motionComparisonPath);
        var warnings = new List<string>
        {
            "coordinate_mirror_context_is_owner_discriminator_evidence_not_final_truth"
        };
        var diagnostics = new List<string>
        {
            "uses_existing_motion_comparison_and_stored_snapshots_only",
            "no_live_process_access",
            $"window_bytes={options.WindowBytes}",
            $"max_pointer_hits={options.MaxPointerHits}"
        };

        if (!comparison.Success)
        {
            warnings.Add("input_motion_comparison_not_successful");
        }

        if (comparison.MotionClusters.Count == 0)
        {
            warnings.Add("input_motion_comparison_has_no_motion_clusters");
        }

        var clusters = comparison.MotionClusters
            .Take(options.Top)
            .ToArray();
        if (comparison.MotionClusters.Count > clusters.Length)
        {
            warnings.Add("motion_cluster_context_output_truncated_by_top_limit");
        }

        var contextWarnings = new List<string>();
        var contexts = clusters
            .Select((cluster, index) => BuildContext(
                sessionPath,
                snapshots,
                regions,
                cluster,
                options.WindowBytes,
                options.MaxPointerHits,
                $"rift-coordinate-mirror-context-{index + 1:000000}",
                contextWarnings))
            .ToArray();
        warnings.AddRange(contextWarnings);

        return new RiftCoordinateMirrorContextResult
        {
            Success = true,
            MotionComparisonPath = motionComparisonPath,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            AnalyzerSources = ["manifest.json", "regions.json", "snapshots/index.jsonl", "snapshots/*.bin", motionComparisonPath],
            WindowBytes = options.WindowBytes,
            MaxPointerHits = options.MaxPointerHits,
            TopLimit = options.Top,
            MotionClusterCount = comparison.MotionClusterCount,
            ContextCount = contexts.Length,
            Contexts = contexts,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    public void WriteMarkdown(RiftCoordinateMirrorContextResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, BuildMarkdown(result));
    }

    private static RiftCoordinateMirrorContext BuildContext(
        string sessionPath,
        IReadOnlyList<SnapshotIndexEntry> snapshots,
        IReadOnlyList<MemoryRegion> regions,
        RiftAddonCoordinateMotionCluster cluster,
        int windowBytes,
        int maxPointerHits,
        string contextId,
        ICollection<string> warnings)
    {
        var representativeOffset = ParseUnsignedHexOrDecimal(cluster.RepresentativeSourceOffsetHex);
        var sourceOffsets = cluster.SourceOffsets.Count == 0
            ? [cluster.RepresentativeSourceOffsetHex]
            : cluster.SourceOffsets;
        var parsedOffsets = sourceOffsets
            .Select(ParseUnsignedHexOrDecimal)
            .Distinct()
            .Order()
            .ToArray();
        var relativeOffsets = parsedOffsets
            .Select(offset => checked((long)offset - (long)representativeOffset))
            .ToArray();
        var memberGaps = parsedOffsets
            .Zip(parsedOffsets.Skip(1), (left, right) => checked((long)right - (long)left))
            .ToArray();
        var memberSpanBytes = parsedOffsets.Length == 0
            ? 0
            : checked((long)(parsedOffsets[^1] - parsedOffsets[0]) + Vec3ByteLength);
        var clusterSnapshots = snapshots
            .Where(snapshot =>
                string.Equals(snapshot.RegionId, cluster.RepresentativeSourceRegionId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeHex(snapshot.BaseAddressHex), NormalizeHex(cluster.RepresentativeSourceBaseAddressHex), StringComparison.OrdinalIgnoreCase))
            .OrderBy(snapshot => snapshot.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (clusterSnapshots.Length == 0)
        {
            warnings.Add("coordinate_mirror_cluster_has_no_matching_session_snapshots");
            return BuildMissingSnapshotContext(
                contextId,
                cluster,
                relativeOffsets,
                memberGaps,
                memberSpanBytes,
                "blocked_no_matching_snapshots_for_motion_cluster");
        }

        var firstSnapshot = clusterSnapshots[0];
        var firstBytes = File.ReadAllBytes(ResolveSessionPath(sessionPath, firstSnapshot.Path));
        var lastSnapshot = clusterSnapshots[^1];
        var lastBytes = clusterSnapshots.Length == 1
            ? firstBytes
            : File.ReadAllBytes(ResolveSessionPath(sessionPath, lastSnapshot.Path));
        var (windowStart, windowEnd) = BuildWindow(parsedOffsets, firstBytes.Length, windowBytes);
        var firstValues = BuildMemberValues(firstBytes, parsedOffsets, firstSnapshot);
        var lastValues = BuildMemberValues(lastBytes, parsedOffsets, lastSnapshot);
        var pointerScan = ScanPointerLikeValues(firstBytes, firstSnapshot, regions, windowStart, windowEnd, maxPointerHits);
        if (pointerScan.TotalCount > pointerScan.Hits.Count)
        {
            warnings.Add("coordinate_mirror_pointer_hit_output_truncated_by_max_pointer_hits");
        }

        var firstUniqueCount = CountUniqueReadableMemberValues(firstValues);
        var lastUniqueCount = CountUniqueReadableMemberValues(lastValues);
        var readableMemberCount = firstValues.Count(value => value.Readable);
        var floatCounts = CountFloats(firstBytes, windowStart, windowEnd);
        var discriminatorStatus = BuildDiscriminatorStatus(cluster.CandidateCount, readableMemberCount, firstUniqueCount, lastUniqueCount, pointerScan.TotalCount);

        return new RiftCoordinateMirrorContext
        {
            ContextId = contextId,
            MotionClusterId = cluster.ClusterId,
            RepresentativeSourceRegionId = cluster.RepresentativeSourceRegionId,
            RepresentativeSourceBaseAddressHex = cluster.RepresentativeSourceBaseAddressHex,
            RepresentativeSourceOffsetHex = cluster.RepresentativeSourceOffsetHex,
            RepresentativeSourceAbsoluteAddressHex = cluster.RepresentativeSourceAbsoluteAddressHex,
            AxisOrder = cluster.AxisOrder,
            CandidateCount = cluster.CandidateCount,
            SourceOffsets = sourceOffsets,
            SourceAbsoluteAddresses = cluster.SourceAbsoluteAddresses,
            MemberRelativeOffsetsBytes = relativeOffsets,
            MemberGapBytes = memberGaps,
            MemberSpanBytes = memberSpanBytes,
            LocalWindowStartOffsetHex = FormatHex((ulong)windowStart),
            LocalWindowEndOffsetHex = FormatHex((ulong)windowEnd),
            LocalWindowSizeBytes = windowEnd - windowStart,
            SnapshotsInspected = clusterSnapshots.Length,
            FirstSnapshotId = firstSnapshot.SnapshotId,
            LastSnapshotId = lastSnapshot.SnapshotId,
            FirstSnapshotFiniteFloat32Count = floatCounts.FiniteFloat32Count,
            FirstSnapshotPlausibleVec3Count = floatCounts.PlausibleVec3Count,
            FirstSnapshotReadableMemberCount = readableMemberCount,
            FirstSnapshotUniqueMemberValueCount = firstUniqueCount,
            LastSnapshotUniqueMemberValueCount = lastUniqueCount,
            FirstSnapshotMemberValues = firstValues,
            LastSnapshotMemberValues = lastValues,
            PointerLikeValueCount = pointerScan.TotalCount,
            PointerLikeHits = pointerScan.Hits,
            CanonicalDiscriminatorStatus = discriminatorStatus,
            EvidenceSummary = $"cluster={cluster.ClusterId};representative={cluster.RepresentativeSourceBaseAddressHex}+{cluster.RepresentativeSourceOffsetHex};candidate_count={cluster.CandidateCount};member_span_bytes={memberSpanBytes};window={FormatHex((ulong)windowStart)}-{FormatHex((ulong)windowEnd)};pointer_like_values={pointerScan.TotalCount};status={discriminatorStatus}"
        };
    }

    private static RiftCoordinateMirrorContext BuildMissingSnapshotContext(
        string contextId,
        RiftAddonCoordinateMotionCluster cluster,
        IReadOnlyList<long> relativeOffsets,
        IReadOnlyList<long> memberGaps,
        long memberSpanBytes,
        string status) =>
        new()
        {
            ContextId = contextId,
            MotionClusterId = cluster.ClusterId,
            RepresentativeSourceRegionId = cluster.RepresentativeSourceRegionId,
            RepresentativeSourceBaseAddressHex = cluster.RepresentativeSourceBaseAddressHex,
            RepresentativeSourceOffsetHex = cluster.RepresentativeSourceOffsetHex,
            RepresentativeSourceAbsoluteAddressHex = cluster.RepresentativeSourceAbsoluteAddressHex,
            AxisOrder = cluster.AxisOrder,
            CandidateCount = cluster.CandidateCount,
            SourceOffsets = cluster.SourceOffsets,
            SourceAbsoluteAddresses = cluster.SourceAbsoluteAddresses,
            MemberRelativeOffsetsBytes = relativeOffsets,
            MemberGapBytes = memberGaps,
            MemberSpanBytes = memberSpanBytes,
            CanonicalDiscriminatorStatus = status,
            EvidenceSummary = $"cluster={cluster.ClusterId};candidate_count={cluster.CandidateCount};status={status}"
        };

    private static (int Start, int End) BuildWindow(IReadOnlyList<ulong> offsets, int byteLength, int windowBytes)
    {
        if (byteLength <= 0)
        {
            return (0, 0);
        }

        var halfWindow = windowBytes / 2;
        var minOffset = offsets.Count == 0 ? 0 : offsets[0];
        var maxOffset = offsets.Count == 0 ? 0 : offsets[^1];
        var start = minOffset > (ulong)halfWindow
            ? minOffset - (ulong)halfWindow
            : 0;
        var end = Math.Min(
            (ulong)byteLength,
            checked(maxOffset + Vec3ByteLength + (ulong)halfWindow));
        if (start > (ulong)byteLength)
        {
            start = (ulong)byteLength;
        }

        if (end < start)
        {
            end = start;
        }

        return ((int)start, (int)end);
    }

    private static IReadOnlyList<RiftCoordinateMirrorMemberValue> BuildMemberValues(
        byte[] bytes,
        IReadOnlyList<ulong> offsets,
        SnapshotIndexEntry snapshot) =>
        offsets.Select(offset =>
        {
            var readable = offset <= int.MaxValue && (long)offset <= bytes.Length - Vec3ByteLength;
            var absoluteAddress = CheckedAdd(ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex), offset);
            if (!readable)
            {
                return new RiftCoordinateMirrorMemberValue
                {
                    SourceOffsetHex = FormatHex(offset),
                    SourceAbsoluteAddressHex = FormatHex(absoluteAddress),
                    Readable = false
                };
            }

            var index = (int)offset;
            return new RiftCoordinateMirrorMemberValue
            {
                SourceOffsetHex = FormatHex(offset),
                SourceAbsoluteAddressHex = FormatHex(absoluteAddress),
                Readable = true,
                X = ReadJsonSafeSingle(bytes, index),
                Y = ReadJsonSafeSingle(bytes, index + FloatStrideBytes),
                Z = ReadJsonSafeSingle(bytes, index + FloatStrideBytes + FloatStrideBytes)
            };
        }).ToArray();

    private static int CountUniqueReadableMemberValues(IReadOnlyList<RiftCoordinateMirrorMemberValue> values)
    {
        var uniqueValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!value.Readable || !value.X.HasValue || !value.Y.HasValue || !value.Z.HasValue)
            {
                continue;
            }

            uniqueValues.Add(string.Join(
                '|',
                value.X.GetValueOrDefault().ToString("R", CultureInfo.InvariantCulture),
                value.Y.GetValueOrDefault().ToString("R", CultureInfo.InvariantCulture),
                value.Z.GetValueOrDefault().ToString("R", CultureInfo.InvariantCulture)));
        }

        return uniqueValues.Count;
    }

    private static (int FiniteFloat32Count, int PlausibleVec3Count) CountFloats(byte[] bytes, int windowStart, int windowEnd)
    {
        var finiteFloatCount = 0;
        var plausibleVec3Count = 0;
        for (var offset = windowStart; offset <= windowEnd - FloatStrideBytes; offset += FloatStrideBytes)
        {
            if (float.IsFinite(ReadSingle(bytes, offset)))
            {
                finiteFloatCount++;
            }
        }

        for (var offset = windowStart; offset <= windowEnd - Vec3ByteLength; offset += FloatStrideBytes)
        {
            if (IsFinitePlausibleCoordinate(ReadSingle(bytes, offset)) &&
                IsFinitePlausibleCoordinate(ReadSingle(bytes, offset + FloatStrideBytes)) &&
                IsFinitePlausibleCoordinate(ReadSingle(bytes, offset + FloatStrideBytes + FloatStrideBytes)))
            {
                plausibleVec3Count++;
            }
        }

        return (finiteFloatCount, plausibleVec3Count);
    }

    private static (int TotalCount, IReadOnlyList<RiftCoordinateMirrorPointerHit> Hits) ScanPointerLikeValues(
        byte[] bytes,
        SnapshotIndexEntry snapshot,
        IReadOnlyList<MemoryRegion> regions,
        int windowStart,
        int windowEnd,
        int maxPointerHits)
    {
        var hits = new List<RiftCoordinateMirrorPointerHit>();
        var total = 0;
        var alignedStart = AlignUp(windowStart, PointerStrideBytes);
        var baseAddress = ParseUnsignedHexOrDecimal(snapshot.BaseAddressHex);
        for (var offset = alignedStart; offset <= windowEnd - PointerStrideBytes; offset += PointerStrideBytes)
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, PointerStrideBytes));
            var targetRegion = regions.FirstOrDefault(region => ContainsAddress(region, value));
            if (targetRegion is null)
            {
                continue;
            }

            total++;
            if (hits.Count >= maxPointerHits)
            {
                continue;
            }

            var sourceAbsoluteAddress = CheckedAdd(baseAddress, (ulong)offset);
            var targetBase = ParseUnsignedHexOrDecimal(targetRegion.BaseAddressHex);
            hits.Add(new RiftCoordinateMirrorPointerHit
            {
                SourceOffsetHex = FormatHex((ulong)offset),
                SourceAbsoluteAddressHex = FormatHex(sourceAbsoluteAddress),
                PointerValueHex = FormatHex(value),
                TargetRegionId = targetRegion.RegionId,
                TargetBaseAddressHex = FormatHex(targetBase),
                TargetOffsetHex = FormatHex(value - targetBase),
                SourceIsRepresentativeRegion = string.Equals(snapshot.RegionId, targetRegion.RegionId, StringComparison.OrdinalIgnoreCase)
            });
        }

        return (total, hits);
    }

    private static string BuildMarkdown(RiftCoordinateMirrorContextResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# RiftScan Coordinate Mirror Context - {result.SessionId}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Motion comparison: `{result.MotionComparisonPath}`");
        builder.AppendLine($"- Session: `{result.SessionPath}`");
        builder.AppendLine($"- Motion clusters in input: {result.MotionClusterCount}");
        builder.AppendLine($"- Contexts emitted: {result.ContextCount}");
        builder.AppendLine($"- Window bytes: {result.WindowBytes}");
        builder.AppendLine($"- Max pointer hits: {result.MaxPointerHits}");
        builder.AppendLine();
        builder.AppendLine("## Mirror contexts");
        builder.AppendLine();
        builder.AppendLine("| Context | Representative | Count | Member span | Unique first/last | Pointer-like values | Status | Offsets |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---|---|");
        foreach (var context in result.Contexts)
        {
            builder.AppendLine($"| `{Escape(context.ContextId)}` | `{Escape(context.RepresentativeSourceBaseAddressHex)}+{Escape(context.RepresentativeSourceOffsetHex)}` | {context.CandidateCount} | {context.MemberSpanBytes} | {context.FirstSnapshotUniqueMemberValueCount}/{context.LastSnapshotUniqueMemberValueCount} | {context.PointerLikeValueCount} | {Escape(context.CanonicalDiscriminatorStatus)} | {Escape(string.Join(", ", context.SourceOffsets))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Pointer-like hit samples");
        builder.AppendLine();
        builder.AppendLine("| Context | Source offset | Pointer value | Target region | Target offset |");
        builder.AppendLine("|---|---:|---:|---|---:|");
        var pointerRows = 0;
        foreach (var context in result.Contexts)
        {
            foreach (var hit in context.PointerLikeHits.Take(10))
            {
                pointerRows++;
                builder.AppendLine($"| `{Escape(context.ContextId)}` | `{Escape(hit.SourceOffsetHex)}` | `{Escape(hit.PointerValueHex)}` | `{Escape(hit.TargetRegionId)}` | `{Escape(hit.TargetOffsetHex)}` |");
            }
        }

        if (pointerRows == 0)
        {
            builder.AppendLine("| _none_ |  |  |  |  |");
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in result.Warnings)
        {
            builder.AppendLine($"- {Escape(warning)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Next recommended action");
        builder.AppendLine();
        builder.AppendLine("- Use pointer-like hits and member-span context as owner/container xref targets; this report does not promote a canonical coordinate owner by itself.");
        return builder.ToString();
    }

    private static string BuildDiscriminatorStatus(
        int candidateCount,
        int readableMemberCount,
        int firstUniqueCount,
        int lastUniqueCount,
        int pointerLikeValueCount)
    {
        if (candidateCount <= 1)
        {
            return "single_candidate_no_mirror_discriminator_needed";
        }

        if (readableMemberCount == 0)
        {
            return "blocked_cluster_offsets_not_captured";
        }

        if (pointerLikeValueCount > 0)
        {
            return "owner_container_trace_ready_pointer_context_present";
        }

        if (firstUniqueCount > 1 || lastUniqueCount > 1)
        {
            return "value_groups_differ_but_semantic_owner_unverified";
        }

        return "no_unique_local_discriminator_same_values";
    }

    private static T ReadSessionJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Coordinate mirror context input JSON does not exist: {path}");
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {path}.");
    }

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
            .ToArray();
    }

    private static bool ContainsAddress(MemoryRegion region, ulong address)
    {
        if (region.SizeBytes <= 0)
        {
            return false;
        }

        var baseAddress = ParseUnsignedHexOrDecimal(region.BaseAddressHex);
        return address >= baseAddress && address - baseAddress < (ulong)region.SizeBytes;
    }

    private static bool IsFinitePlausibleCoordinate(float value) =>
        float.IsFinite(value) && Math.Abs(value) <= PlausibleCoordinateAbsMax;

    private static float ReadSingle(byte[] bytes, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, FloatStrideBytes)));

    private static double? ReadJsonSafeSingle(byte[] bytes, int offset)
    {
        var value = ReadSingle(bytes, offset);
        return float.IsFinite(value) ? value : null;
    }

    private static int AlignUp(int value, int alignment) =>
        value % alignment == 0 ? value : checked(value + alignment - (value % alignment));

    private static ulong CheckedAdd(ulong baseAddress, ulong offset)
    {
        if (ulong.MaxValue - baseAddress < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Address calculation overflowed.");
        }

        return baseAddress + offset;
    }

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static string NormalizeHex(string value) => FormatHex(ParseUnsignedHexOrDecimal(value));

    private static string FormatHex(ulong value) => $"0x{value:X}";

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");
}

public sealed record RiftCoordinateMirrorContextResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_coordinate_mirror_context_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("motion_comparison_path")]
    public string MotionComparisonPath { get; init; } = string.Empty;

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("analyzer_id")]
    public string AnalyzerId { get; init; } = "rift_coordinate_mirror_context";

    [JsonPropertyName("analyzer_version")]
    public string AnalyzerVersion { get; init; } = "1";

    [JsonPropertyName("analyzer_sources")]
    public IReadOnlyList<string> AnalyzerSources { get; init; } = [];

    [JsonPropertyName("window_bytes")]
    public int WindowBytes { get; init; }

    [JsonPropertyName("max_pointer_hits")]
    public int MaxPointerHits { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("motion_cluster_count")]
    public int MotionClusterCount { get; init; }

    [JsonPropertyName("context_count")]
    public int ContextCount { get; init; }

    [JsonPropertyName("contexts")]
    public IReadOnlyList<RiftCoordinateMirrorContext> Contexts { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftCoordinateMirrorContext
{
    [JsonPropertyName("context_id")]
    public string ContextId { get; init; } = string.Empty;

    [JsonPropertyName("motion_cluster_id")]
    public string MotionClusterId { get; init; } = string.Empty;

    [JsonPropertyName("representative_source_region_id")]
    public string RepresentativeSourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("representative_source_base_address_hex")]
    public string RepresentativeSourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("representative_source_offset_hex")]
    public string RepresentativeSourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("representative_source_absolute_address_hex")]
    public string RepresentativeSourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("axis_order")]
    public string AxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("candidate_count")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("source_offsets")]
    public IReadOnlyList<string> SourceOffsets { get; init; } = [];

    [JsonPropertyName("source_absolute_addresses")]
    public IReadOnlyList<string> SourceAbsoluteAddresses { get; init; } = [];

    [JsonPropertyName("member_relative_offsets_bytes")]
    public IReadOnlyList<long> MemberRelativeOffsetsBytes { get; init; } = [];

    [JsonPropertyName("member_gap_bytes")]
    public IReadOnlyList<long> MemberGapBytes { get; init; } = [];

    [JsonPropertyName("member_span_bytes")]
    public long MemberSpanBytes { get; init; }

    [JsonPropertyName("local_window_start_offset_hex")]
    public string LocalWindowStartOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("local_window_end_offset_hex")]
    public string LocalWindowEndOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("local_window_size_bytes")]
    public int LocalWindowSizeBytes { get; init; }

    [JsonPropertyName("snapshots_inspected")]
    public int SnapshotsInspected { get; init; }

    [JsonPropertyName("first_snapshot_id")]
    public string FirstSnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("last_snapshot_id")]
    public string LastSnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("first_snapshot_finite_float32_count")]
    public int FirstSnapshotFiniteFloat32Count { get; init; }

    [JsonPropertyName("first_snapshot_plausible_vec3_count")]
    public int FirstSnapshotPlausibleVec3Count { get; init; }

    [JsonPropertyName("first_snapshot_readable_member_count")]
    public int FirstSnapshotReadableMemberCount { get; init; }

    [JsonPropertyName("first_snapshot_unique_member_value_count")]
    public int FirstSnapshotUniqueMemberValueCount { get; init; }

    [JsonPropertyName("last_snapshot_unique_member_value_count")]
    public int LastSnapshotUniqueMemberValueCount { get; init; }

    [JsonPropertyName("first_snapshot_member_values")]
    public IReadOnlyList<RiftCoordinateMirrorMemberValue> FirstSnapshotMemberValues { get; init; } = [];

    [JsonPropertyName("last_snapshot_member_values")]
    public IReadOnlyList<RiftCoordinateMirrorMemberValue> LastSnapshotMemberValues { get; init; } = [];

    [JsonPropertyName("pointer_like_value_count")]
    public int PointerLikeValueCount { get; init; }

    [JsonPropertyName("pointer_like_hits")]
    public IReadOnlyList<RiftCoordinateMirrorPointerHit> PointerLikeHits { get; init; } = [];

    [JsonPropertyName("canonical_discriminator_status")]
    public string CanonicalDiscriminatorStatus { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record RiftCoordinateMirrorMemberValue
{
    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("readable")]
    public bool Readable { get; init; }

    [JsonPropertyName("x")]
    public double? X { get; init; }

    [JsonPropertyName("y")]
    public double? Y { get; init; }

    [JsonPropertyName("z")]
    public double? Z { get; init; }
}

public sealed record RiftCoordinateMirrorPointerHit
{
    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("pointer_value_hex")]
    public string PointerValueHex { get; init; } = string.Empty;

    [JsonPropertyName("target_region_id")]
    public string TargetRegionId { get; init; } = string.Empty;

    [JsonPropertyName("target_base_address_hex")]
    public string TargetBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("target_offset_hex")]
    public string TargetOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_is_representative_region")]
    public bool SourceIsRepresentativeRegion { get; init; }
}
