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

    public double MirrorEpsilon { get; init; } = 0.001;

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

        if (double.IsNaN(options.MirrorEpsilon) || double.IsInfinity(options.MirrorEpsilon) || options.MirrorEpsilon <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MirrorEpsilon), "Mirror epsilon must be finite and positive.");
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
        var staleAddonObservations =
            SameNonEmptyPath(pre.ObservationPath, post.ObservationPath) ||
            SameNonEmptyPath(pre.TruthSummaryPath, post.TruthSummaryPath) ||
            SameObservationTimestamp(pre.LatestObservationUtc, post.LatestObservationUtc);
        if (staleAddonObservations)
        {
            warnings.Add("addon_observations_may_be_stale_or_identical_between_pre_and_post");
        }

        var preByKey = pre.Candidates.ToDictionary(BuildKey, StringComparer.Ordinal);
        var postByKey = post.Candidates.ToDictionary(BuildKey, StringComparer.Ordinal);
        var commonKeys = preByKey.Keys.Intersect(postByKey.Keys, StringComparer.Ordinal).ToArray();
        var allDeltas = commonKeys
            .Select(key => BuildDelta(preByKey[key], postByKey[key], options.MinDeltaDistance))
            .OrderByDescending(delta => string.Equals(delta.Classification, "moved_with_player_candidate", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(delta => delta.DeltaDistance)
            .ThenBy(delta => ParseUnsignedHexOrDecimal(delta.SourceBaseAddressHex))
            .ThenBy(delta => ParseUnsignedHexOrDecimal(delta.SourceOffsetHex))
            .ThenBy(delta => delta.AxisOrder, StringComparer.Ordinal)
            .Select((delta, index) => delta with { DeltaId = $"rift-addon-coordinate-motion-delta-{index + 1:000000}" })
            .ToArray();
        var candidateDeltas = allDeltas
            .Take(options.Top)
            .ToArray();

        var movedCount = allDeltas.Count(delta => string.Equals(delta.Classification, "moved_with_player_candidate", StringComparison.OrdinalIgnoreCase));
        var clusters = BuildClusters(allDeltas, options.MirrorEpsilon)
            .OrderByDescending(cluster => string.Equals(cluster.Classification, "synchronized_coordinate_mirror_cluster", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(cluster => cluster.CandidateCount)
            .ThenByDescending(cluster => cluster.DeltaDistance)
            .ThenBy(cluster => ParseUnsignedHexOrDecimal(cluster.RepresentativeSourceBaseAddressHex))
            .ThenBy(cluster => ParseUnsignedHexOrDecimal(cluster.RepresentativeSourceOffsetHex))
            .ThenBy(cluster => cluster.AxisOrder, StringComparer.Ordinal)
            .Select((cluster, index) => cluster with { ClusterId = $"rift-addon-coordinate-motion-cluster-{index + 1:000000}" })
            .ToArray();
        var synchronizedMirrorClusterCount = clusters.Count(cluster => string.Equals(cluster.Classification, "synchronized_coordinate_mirror_cluster", StringComparison.OrdinalIgnoreCase));
        if (synchronizedMirrorClusterCount > 0)
        {
            warnings.Add("synchronized_coordinate_mirror_clusters_detected");
        }

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
            MirrorEpsilon = options.MirrorEpsilon,
            TopLimit = options.Top,
            PreCandidateCount = pre.CandidateCount,
            PostCandidateCount = post.CandidateCount,
            CommonCandidateCount = commonKeys.Length,
            MovedCandidateCount = movedCount,
            CandidateDeltas = candidateDeltas,
            MotionClusterCount = clusters.Length,
            SynchronizedMirrorClusterCount = synchronizedMirrorClusterCount,
            CanonicalPromotionStatus = BuildCanonicalPromotionStatus(movedCount, synchronizedMirrorClusterCount, staleAddonObservations),
            MotionClusters = clusters.Take(options.Top).ToArray(),
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
        builder.AppendLine($"- Motion clusters: {result.MotionClusterCount}");
        builder.AppendLine($"- Synchronized mirror clusters: {result.SynchronizedMirrorClusterCount}");
        builder.AppendLine($"- Canonical promotion status: `{Escape(result.CanonicalPromotionStatus)}`");
        builder.AppendLine($"- Minimum delta distance: {result.MinDeltaDistance.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"- Mirror epsilon: {result.MirrorEpsilon.ToString(CultureInfo.InvariantCulture)}");
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
        builder.AppendLine("## Motion clusters");
        builder.AppendLine();
        builder.AppendLine("| Cluster | Representative | Count | Delta X | Delta Y | Delta Z | Distance | Classification | Promotion status | Offsets |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|---|---|");
        foreach (var cluster in result.MotionClusters)
        {
            builder.AppendLine($"| `{Escape(cluster.ClusterId)}` | `{Escape(cluster.RepresentativeSourceBaseAddressHex)}+{Escape(cluster.RepresentativeSourceOffsetHex)}` | {cluster.CandidateCount} | {cluster.DeltaX:F6} | {cluster.DeltaY:F6} | {cluster.DeltaZ:F6} | {cluster.DeltaDistance:F6} | {Escape(cluster.Classification)} | {Escape(cluster.PromotionStatus)} | {Escape(string.Join(", ", cluster.SourceOffsets))} |");
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

    private static IReadOnlyList<RiftAddonCoordinateMotionCluster> BuildClusters(
        IReadOnlyList<RiftAddonCoordinateMotionDelta> deltas,
        double mirrorEpsilon)
    {
        var groups = deltas.GroupBy(delta => BuildMirrorKey(delta, mirrorEpsilon), StringComparer.Ordinal);
        var clusters = new List<RiftAddonCoordinateMotionCluster>();
        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(delta => ParseUnsignedHexOrDecimal(delta.SourceBaseAddressHex))
                .ThenBy(delta => ParseUnsignedHexOrDecimal(delta.SourceOffsetHex))
                .ThenBy(delta => delta.AxisOrder, StringComparer.Ordinal)
                .ToArray();
            var representative = ordered[0];
            var moved = string.Equals(representative.Classification, "moved_with_player_candidate", StringComparison.OrdinalIgnoreCase);
            var classification = moved && ordered.Length > 1
                ? "synchronized_coordinate_mirror_cluster"
                : moved
                    ? "single_moved_coordinate_candidate"
                    : "stable_or_below_threshold_cluster";
            var promotionStatus = classification switch
            {
                "synchronized_coordinate_mirror_cluster" => "blocked_by_synchronized_mirror_cluster",
                "single_moved_coordinate_candidate" => "requires_fresh_addon_observation_and_cross_session_validation",
                _ => "not_promotable_below_motion_threshold"
            };

            clusters.Add(new RiftAddonCoordinateMotionCluster
            {
                RepresentativeSourceRegionId = representative.SourceRegionId,
                RepresentativeSourceBaseAddressHex = representative.SourceBaseAddressHex,
                RepresentativeSourceOffsetHex = representative.SourceOffsetHex,
                RepresentativeSourceAbsoluteAddressHex = representative.SourceAbsoluteAddressHex,
                AxisOrder = representative.AxisOrder,
                CandidateCount = ordered.Length,
                SourceOffsets = ordered.Select(delta => delta.SourceOffsetHex).ToArray(),
                SourceAbsoluteAddresses = ordered.Select(delta => delta.SourceAbsoluteAddressHex).ToArray(),
                PreMemoryX = representative.PreMemoryX,
                PreMemoryY = representative.PreMemoryY,
                PreMemoryZ = representative.PreMemoryZ,
                PostMemoryX = representative.PostMemoryX,
                PostMemoryY = representative.PostMemoryY,
                PostMemoryZ = representative.PostMemoryZ,
                DeltaX = representative.DeltaX,
                DeltaY = representative.DeltaY,
                DeltaZ = representative.DeltaZ,
                DeltaDistance = representative.DeltaDistance,
                Classification = classification,
                PromotionStatus = promotionStatus,
                EvidenceSummary = $"representative={representative.SourceBaseAddressHex}+{representative.SourceOffsetHex};candidate_count={ordered.Length};delta={representative.DeltaX:F6}|{representative.DeltaY:F6}|{representative.DeltaZ:F6};classification={classification};promotion_status={promotionStatus}"
            });
        }

        return clusters;
    }

    private static string BuildCanonicalPromotionStatus(int movedCount, int synchronizedMirrorClusterCount, bool staleAddonObservations)
    {
        if (movedCount == 0)
        {
            return "blocked_no_moved_candidates";
        }

        if (synchronizedMirrorClusterCount > 0)
        {
            return "blocked_by_synchronized_mirror_clusters";
        }

        if (staleAddonObservations)
        {
            return "blocked_by_stale_addon_observations";
        }

        return "requires_cross_session_validation_not_final_truth";
    }

    private static string BuildMirrorKey(RiftAddonCoordinateMotionDelta delta, double mirrorEpsilon) =>
        string.Join(
            '|',
            delta.SourceRegionId,
            delta.SourceBaseAddressHex,
            delta.AxisOrder,
            Quantize(delta.PreMemoryX, mirrorEpsilon),
            Quantize(delta.PreMemoryY, mirrorEpsilon),
            Quantize(delta.PreMemoryZ, mirrorEpsilon),
            Quantize(delta.PostMemoryX, mirrorEpsilon),
            Quantize(delta.PostMemoryY, mirrorEpsilon),
            Quantize(delta.PostMemoryZ, mirrorEpsilon),
            Quantize(delta.DeltaX, mirrorEpsilon),
            Quantize(delta.DeltaY, mirrorEpsilon),
            Quantize(delta.DeltaZ, mirrorEpsilon));

    private static long Quantize(double value, double epsilon) =>
        checked((long)Math.Round(value / epsilon, MidpointRounding.AwayFromZero));

    private static string BuildKey(RiftSessionAddonCoordinateCandidate candidate) =>
        string.Join('|', candidate.SourceRegionId, candidate.SourceBaseAddressHex, candidate.SourceOffsetHex, candidate.AxisOrder);

    private static bool SameNonEmptyPath(string first, string second) =>
        !string.IsNullOrWhiteSpace(first) &&
        !string.IsNullOrWhiteSpace(second) &&
        first.Equals(second, StringComparison.OrdinalIgnoreCase);

    private static bool SameObservationTimestamp(DateTimeOffset? first, DateTimeOffset? second) =>
        first.HasValue &&
        second.HasValue &&
        first.Value == second.Value;

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

    [JsonPropertyName("mirror_epsilon")]
    public double MirrorEpsilon { get; init; }

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

    [JsonPropertyName("motion_cluster_count")]
    public int MotionClusterCount { get; init; }

    [JsonPropertyName("synchronized_mirror_cluster_count")]
    public int SynchronizedMirrorClusterCount { get; init; }

    [JsonPropertyName("canonical_promotion_status")]
    public string CanonicalPromotionStatus { get; init; } = string.Empty;

    [JsonPropertyName("motion_clusters")]
    public IReadOnlyList<RiftAddonCoordinateMotionCluster> MotionClusters { get; init; } = [];

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

public sealed record RiftAddonCoordinateMotionCluster
{
    [JsonPropertyName("cluster_id")]
    public string ClusterId { get; init; } = string.Empty;

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

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("promotion_status")]
    public string PromotionStatus { get; init; } = string.Empty;

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
