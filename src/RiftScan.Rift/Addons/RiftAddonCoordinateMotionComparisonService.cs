using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed record RiftAddonCoordinateMotionComparisonOptions
{
    public string PreMatchPath { get; init; } = string.Empty;

    public string PostMatchPath { get; init; } = string.Empty;

    public double MinDeltaDistance { get; init; } = 1;

    public int Top { get; init; } = 100;
}

public sealed class RiftAddonCoordinateMotionComparisonService
{
    public RiftAddonCoordinateMotionComparisonResult Compare(RiftAddonCoordinateMotionComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PreMatchPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PostMatchPath);
        if (double.IsNaN(options.MinDeltaDistance) || double.IsInfinity(options.MinDeltaDistance) || options.MinDeltaDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinDeltaDistance), "Minimum delta distance must be finite and non-negative.");
        }

        if (options.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Top), "Top must be positive.");
        }

        var prePath = Path.GetFullPath(options.PreMatchPath);
        var postPath = Path.GetFullPath(options.PostMatchPath);
        var pre = ReadJson<RiftSessionAddonCoordinateMatchResult>(prePath);
        var post = ReadJson<RiftSessionAddonCoordinateMatchResult>(postPath);
        var warnings = new List<string> { "motion_comparison_is_behavior_evidence_not_final_truth" };
        var diagnostics = new List<string>
        {
            "compares_existing_addon_coordinate_match_results_only",
            "no_live_process_access"
        };

        ValidateInput(pre, prePath, warnings);
        ValidateInput(post, postPath, warnings);
        if (pre.ObservationPath.Equals(post.ObservationPath, StringComparison.OrdinalIgnoreCase) || pre.LatestObservationUtc == post.LatestObservationUtc)
        {
            warnings.Add("addon_observations_may_be_stale_or_identical_between_pre_and_post");
        }

        var preByKey = pre.Candidates.ToDictionary(BuildKey, StringComparer.Ordinal);
        var postByKey = post.Candidates.ToDictionary(BuildKey, StringComparer.Ordinal);
        var commonKeys = preByKey.Keys.Intersect(postByKey.Keys, StringComparer.Ordinal).ToArray();
        var candidateDeltas = commonKeys
            .Select(key => BuildDelta(preByKey[key], postByKey[key], options.MinDeltaDistance))
            .OrderByDescending(delta => string.Equals(delta.Classification, "moved_with_player_candidate", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(delta => delta.DeltaDistance)
            .ThenBy(delta => ParseUnsignedHexOrDecimal(delta.SourceBaseAddressHex))
            .ThenBy(delta => ParseUnsignedHexOrDecimal(delta.SourceOffsetHex))
            .ThenBy(delta => delta.AxisOrder, StringComparer.Ordinal)
            .Take(options.Top)
            .Select((delta, index) => delta with { DeltaId = $"rift-addon-coordinate-motion-delta-{index + 1:000000}" })
            .ToArray();

        var movedCount = candidateDeltas.Count(delta => string.Equals(delta.Classification, "moved_with_player_candidate", StringComparison.OrdinalIgnoreCase));
        if (commonKeys.Length > candidateDeltas.Length)
        {
            warnings.Add("motion_delta_output_truncated_by_top_limit");
        }

        if (commonKeys.Length == 0)
        {
            warnings.Add("no_common_candidates_between_pre_and_post_match_results");
        }

        if (movedCount == 0)
        {
            warnings.Add("no_common_candidates_met_min_delta_distance");
        }

        return new RiftAddonCoordinateMotionComparisonResult
        {
            Success = true,
            PreMatchPath = prePath,
            PostMatchPath = postPath,
            PreSessionId = pre.SessionId,
            PostSessionId = post.SessionId,
            MinDeltaDistance = options.MinDeltaDistance,
            TopLimit = options.Top,
            PreCandidateCount = pre.CandidateCount,
            PostCandidateCount = post.CandidateCount,
            CommonCandidateCount = commonKeys.Length,
            MovedCandidateCount = movedCount,
            CandidateDeltas = candidateDeltas,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    public void WriteMarkdown(RiftAddonCoordinateMotionComparisonResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, BuildMarkdown(result));
    }

    private static void ValidateInput(RiftSessionAddonCoordinateMatchResult result, string path, ICollection<string> warnings)
    {
        if (!result.Success)
        {
            warnings.Add($"input_match_result_not_success:{Path.GetFileName(path)}");
        }

        if (result.Candidates.Count == 0)
        {
            warnings.Add($"input_match_result_has_no_candidates:{Path.GetFileName(path)}");
        }
    }

    private static RiftAddonCoordinateMotionDelta BuildDelta(
        RiftSessionAddonCoordinateCandidate pre,
        RiftSessionAddonCoordinateCandidate post,
        double minDeltaDistance)
    {
        var deltaX = post.BestMemoryX - pre.BestMemoryX;
        var deltaY = post.BestMemoryY - pre.BestMemoryY;
        var deltaZ = post.BestMemoryZ - pre.BestMemoryZ;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        var classification = distance >= minDeltaDistance
            ? "moved_with_player_candidate"
            : "stable_or_below_threshold_candidate";

        return new RiftAddonCoordinateMotionDelta
        {
            SourceRegionId = pre.SourceRegionId,
            SourceBaseAddressHex = pre.SourceBaseAddressHex,
            SourceOffsetHex = pre.SourceOffsetHex,
            SourceAbsoluteAddressHex = pre.SourceAbsoluteAddressHex,
            AxisOrder = pre.AxisOrder,
            PreMemoryX = pre.BestMemoryX,
            PreMemoryY = pre.BestMemoryY,
            PreMemoryZ = pre.BestMemoryZ,
            PostMemoryX = post.BestMemoryX,
            PostMemoryY = post.BestMemoryY,
            PostMemoryZ = post.BestMemoryZ,
            DeltaX = deltaX,
            DeltaY = deltaY,
            DeltaZ = deltaZ,
            DeltaDistance = distance,
            PreBestMaxAbsDistanceToAddon = pre.BestMaxAbsDistance,
            PostBestMaxAbsDistanceToAddon = post.BestMaxAbsDistance,
            PreSupportCount = pre.SupportCount,
            PostSupportCount = post.SupportCount,
            Classification = classification,
            EvidenceSummary = $"offset={pre.SourceOffsetHex};axis={pre.AxisOrder};delta={deltaX:F6}|{deltaY:F6}|{deltaZ:F6};distance={distance:F6};classification={classification}"
        };
    }

    private static string BuildMarkdown(RiftAddonCoordinateMotionComparisonResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# RiftScan Addon Coordinate Motion Comparison - {result.PreSessionId} to {result.PostSessionId}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Pre match: `{result.PreMatchPath}`");
        builder.AppendLine($"- Post match: `{result.PostMatchPath}`");
        builder.AppendLine($"- Common candidates: {result.CommonCandidateCount}");
        builder.AppendLine($"- Moved candidates: {result.MovedCandidateCount}");
        builder.AppendLine($"- Minimum delta distance: {result.MinDeltaDistance.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine();
        builder.AppendLine("## Candidate deltas");
        builder.AppendLine();
        builder.AppendLine("| Offset | Axis | Delta X | Delta Y | Delta Z | Distance | Classification |");
        builder.AppendLine("|---:|---|---:|---:|---:|---:|---|");
        foreach (var delta in result.CandidateDeltas)
        {
            builder.AppendLine($"| `{Escape(delta.SourceOffsetHex)}` | `{Escape(delta.AxisOrder)}` | {delta.DeltaX:F6} | {delta.DeltaY:F6} | {delta.DeltaZ:F6} | {delta.DeltaDistance:F6} | {Escape(delta.Classification)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in result.Warnings)
        {
            builder.AppendLine($"- {Escape(warning)}");
        }

        return builder.ToString();
    }

    private static string BuildKey(RiftSessionAddonCoordinateCandidate candidate) =>
        string.Join('|', candidate.SourceRegionId, candidate.SourceBaseAddressHex, candidate.SourceOffsetHex, candidate.AxisOrder);

    private static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Motion comparison input JSON does not exist.", path);
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {path}.");
    }

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");
}

public sealed record RiftAddonCoordinateMotionComparisonResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_addon_coordinate_motion_comparison_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("pre_match_path")]
    public string PreMatchPath { get; init; } = string.Empty;

    [JsonPropertyName("post_match_path")]
    public string PostMatchPath { get; init; } = string.Empty;

    [JsonPropertyName("pre_session_id")]
    public string PreSessionId { get; init; } = string.Empty;

    [JsonPropertyName("post_session_id")]
    public string PostSessionId { get; init; } = string.Empty;

    [JsonPropertyName("min_delta_distance")]
    public double MinDeltaDistance { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("pre_candidate_count")]
    public int PreCandidateCount { get; init; }

    [JsonPropertyName("post_candidate_count")]
    public int PostCandidateCount { get; init; }

    [JsonPropertyName("common_candidate_count")]
    public int CommonCandidateCount { get; init; }

    [JsonPropertyName("moved_candidate_count")]
    public int MovedCandidateCount { get; init; }

    [JsonPropertyName("candidate_deltas")]
    public IReadOnlyList<RiftAddonCoordinateMotionDelta> CandidateDeltas { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record RiftAddonCoordinateMotionDelta
{
    [JsonPropertyName("delta_id")]
    public string DeltaId { get; init; } = string.Empty;

    [JsonPropertyName("source_region_id")]
    public string SourceRegionId { get; init; } = string.Empty;

    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_offset_hex")]
    public string SourceOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("source_absolute_address_hex")]
    public string SourceAbsoluteAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("axis_order")]
    public string AxisOrder { get; init; } = string.Empty;

    [JsonPropertyName("pre_memory_x")]
    public double PreMemoryX { get; init; }

    [JsonPropertyName("pre_memory_y")]
    public double PreMemoryY { get; init; }

    [JsonPropertyName("pre_memory_z")]
    public double PreMemoryZ { get; init; }

    [JsonPropertyName("post_memory_x")]
    public double PostMemoryX { get; init; }

    [JsonPropertyName("post_memory_y")]
    public double PostMemoryY { get; init; }

    [JsonPropertyName("post_memory_z")]
    public double PostMemoryZ { get; init; }

    [JsonPropertyName("delta_x")]
    public double DeltaX { get; init; }

    [JsonPropertyName("delta_y")]
    public double DeltaY { get; init; }

    [JsonPropertyName("delta_z")]
    public double DeltaZ { get; init; }

    [JsonPropertyName("delta_distance")]
    public double DeltaDistance { get; init; }

    [JsonPropertyName("pre_best_max_abs_distance_to_addon")]
    public double PreBestMaxAbsDistanceToAddon { get; init; }

    [JsonPropertyName("post_best_max_abs_distance_to_addon")]
    public double PostBestMaxAbsDistanceToAddon { get; init; }

    [JsonPropertyName("pre_support_count")]
    public int PreSupportCount { get; init; }

    [JsonPropertyName("post_support_count")]
    public int PostSupportCount { get; init; }

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
