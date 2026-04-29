using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Xrefs;

public sealed record SessionXrefChainSummaryOptions
{
    public IReadOnlyList<string> InputPaths { get; init; } = [];

    public int MinSupport { get; init; } = 2;

    public int Top { get; init; } = 100;
}

public sealed class SessionXrefChainSummaryService
{
    public SessionXrefChainSummaryResult Summarize(SessionXrefChainSummaryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.InputPaths.Count == 0)
        {
            throw new ArgumentException("Xref-chain summary requires at least one xref JSON input path.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinSupport);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Top);

        var fullInputPaths = options.InputPaths.Select(Path.GetFullPath).ToArray();
        var reports = fullInputPaths.Select(ReadReport).ToArray();
        var warnings = new List<string>();
        var diagnostics = new List<string>
        {
            "offline_xref_json_summary_only",
            "stable_edges_grouped_by_source_absolute_and_pointer_value"
        };

        foreach (var report in reports)
        {
            if (report.Report.PointerHits.Count < report.Report.PointerHitCount)
            {
                warnings.Add($"input_pointer_hits_truncated:{report.Path}");
            }

            foreach (var warning in report.Report.Warnings.Where(warning => warning.Contains("truncated", StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add($"input_warning:{Path.GetFileName(report.Path)}:{warning}");
            }
        }

        var allEdges = reports
            .SelectMany(report => report.Report.PointerHits.Select(hit => new EdgeObservation(report.Path, report.Report, hit)))
            .GroupBy(observation => EdgeKey(observation.Hit), StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildStableEdge(group.ToArray()))
            .Where(edge => edge.SupportCount >= options.MinSupport)
            .OrderBy(edge => EdgePriority(edge))
            .ThenByDescending(edge => edge.SupportCount)
            .ThenBy(edge => ParseHexOrMax(edge.SourceAbsoluteAddressHex))
            .ToArray();
        var stableEdges = allEdges
            .Take(options.Top)
            .Select((edge, index) => edge with { EdgeId = $"xref-edge-{index + 1:000000}" })
            .ToArray();
        var reciprocalPairs = BuildReciprocalPairs(stableEdges, options.Top);

        if (stableEdges.Length == 0)
        {
            warnings.Add("no_stable_xref_edges_at_min_support");
        }

        return new SessionXrefChainSummaryResult
        {
            Success = true,
            InputPaths = fullInputPaths,
            InputCount = fullInputPaths.Length,
            MinSupport = options.MinSupport,
            TopLimit = options.Top,
            StableEdgeCount = stableEdges.Length,
            ReciprocalPairCount = reciprocalPairs.Count,
            StableEdges = stableEdges,
            ReciprocalPairs = reciprocalPairs,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    public void WriteMarkdown(SessionXrefChainSummaryResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, BuildMarkdown(result));
    }

    private static LoadedXrefReport ReadReport(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var report = JsonSerializer.Deserialize<SessionXrefAnalysisResult>(File.ReadAllText(fullPath), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize xref report: {fullPath}");
        if (!report.Success)
        {
            throw new InvalidOperationException($"Xref report was not successful: {fullPath}");
        }

        return new LoadedXrefReport(fullPath, report);
    }

    private static SessionXrefStableEdge BuildStableEdge(IReadOnlyList<EdgeObservation> observations)
    {
        var first = observations[0];
        var hit = first.Hit;
        var supportKeys = observations
            .Select(observation => $"{observation.InputPath}|{observation.Hit.SnapshotId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inputPaths = observations
            .Select(observation => observation.InputPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var targetBases = observations
            .Select(observation => observation.Report.TargetBaseAddressHex)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SessionXrefStableEdge
        {
            SourceRegionId = hit.SourceRegionId,
            SourceBaseAddressHex = hit.SourceBaseAddressHex,
            SourceOffsetHex = hit.SourceOffsetHex,
            SourceAbsoluteAddressHex = hit.SourceAbsoluteAddressHex,
            PointerValueHex = hit.PointerValueHex,
            TargetOffsetHex = hit.TargetOffsetHex,
            MatchKind = hit.MatchKind,
            SourceIsTargetRegion = hit.SourceIsTargetRegion,
            Classification = Classify(hit),
            SupportCount = supportKeys.Length,
            InputPathCount = inputPaths.Length,
            SupportingInputPaths = inputPaths,
            SupportingSnapshotIds = observations
                .Select(observation => observation.Hit.SnapshotId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            TargetBaseAddressHexes = targetBases,
            EvidenceSummary = $"support={supportKeys.Length};inputs={inputPaths.Length};{hit.SourceAbsoluteAddressHex}->{hit.PointerValueHex};{Classify(hit)}"
        };
    }

    private static IReadOnlyList<SessionXrefReciprocalPair> BuildReciprocalPairs(IReadOnlyList<SessionXrefStableEdge> edges, int top)
    {
        var pairs = new List<SessionXrefReciprocalPair>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (string.Equals(edge.SourceBaseAddressHex, edge.PointerValueHex, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var keyParts = new[] { edge.SourceBaseAddressHex, edge.PointerValueHex }.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var key = string.Join("|", keyParts);
            if (!seen.Add(key))
            {
                continue;
            }

            var firstToSecondEdges = edges
                .Where(candidate =>
                    string.Equals(candidate.SourceBaseAddressHex, keyParts[0], StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.PointerValueHex, keyParts[1], StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var secondToFirstEdges = edges
                .Where(candidate =>
                    string.Equals(candidate.SourceBaseAddressHex, keyParts[1], StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.PointerValueHex, keyParts[0], StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (firstToSecondEdges.Length == 0 || secondToFirstEdges.Length == 0)
            {
                continue;
            }

            pairs.Add(new SessionXrefReciprocalPair
            {
                FirstBaseAddressHex = keyParts[0],
                SecondBaseAddressHex = keyParts[1],
                FirstToSecondEdgeIds = firstToSecondEdges
                    .Select(candidate => candidate.EdgeId)
                    .ToArray(),
                SecondToFirstEdgeIds = secondToFirstEdges
                    .Select(candidate => candidate.EdgeId)
                    .ToArray(),
                SupportCount = Math.Min(
                    firstToSecondEdges.Max(candidate => candidate.SupportCount),
                    secondToFirstEdges.Max(candidate => candidate.SupportCount)),
                EvidenceSummary = $"{keyParts[0]}<->{keyParts[1]}"
            });
        }

        return pairs
            .OrderByDescending(pair => pair.SupportCount)
            .ThenBy(pair => pair.FirstBaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.SecondBaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select((pair, index) => pair with { PairId = $"xref-pair-{index + 1:000000}" })
            .ToArray();
    }

    private static string BuildMarkdown(SessionXrefChainSummaryResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# RiftScan Xref Chain Summary");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Inputs: `{result.InputCount}`");
        builder.AppendLine($"- Min support: `{result.MinSupport}`");
        builder.AppendLine($"- Stable edges: `{result.StableEdgeCount}`");
        builder.AppendLine($"- Reciprocal pairs: `{result.ReciprocalPairCount}`");
        builder.AppendLine();

        builder.AppendLine("## Reciprocal pairs");
        builder.AppendLine();
        if (result.ReciprocalPairs.Count == 0)
        {
            builder.AppendLine("- No reciprocal pairs found.");
        }
        else
        {
            builder.AppendLine("| Pair | First base | Second base | Support | First -> second | Second -> first |");
            builder.AppendLine("|---|---:|---:|---:|---|---|");
            foreach (var pair in result.ReciprocalPairs)
            {
                builder.AppendLine($"| `{pair.PairId}` | `{pair.FirstBaseAddressHex}` | `{pair.SecondBaseAddressHex}` | {pair.SupportCount} | `{string.Join(", ", pair.FirstToSecondEdgeIds)}` | `{string.Join(", ", pair.SecondToFirstEdgeIds)}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Stable edges");
        builder.AppendLine();
        if (result.StableEdges.Count == 0)
        {
            builder.AppendLine("- No stable edges found.");
        }
        else
        {
            builder.AppendLine("| Edge | Source absolute | Pointer value | Support | Class | Source region | Target offset |");
            builder.AppendLine("|---|---:|---:|---:|---|---|---:|");
            foreach (var edge in result.StableEdges)
            {
                builder.AppendLine($"| `{edge.EdgeId}` | `{edge.SourceAbsoluteAddressHex}` | `{edge.PointerValueHex}` | {edge.SupportCount} | `{edge.Classification}` | `{edge.SourceRegionId}` | `{edge.TargetOffsetHex}` |");
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
        builder.AppendLine("- Xref-chain summaries are pointer-graph evidence only, not semantic truth claims.");
        builder.AppendLine("- Input xref reports may be truncated when generated with low max-hit limits.");
        return builder.ToString();
    }

    private static string EdgeKey(SessionXrefPointerHit hit) =>
        string.Join("|", hit.SourceAbsoluteAddressHex, hit.PointerValueHex, hit.MatchKind);

    private static string Classify(SessionXrefPointerHit hit)
    {
        var exact = string.Equals(hit.MatchKind, "exact_target_offset_pointer", StringComparison.OrdinalIgnoreCase);
        return (hit.SourceIsTargetRegion, exact) switch
        {
            (false, true) => "outside_exact_target_pointer_edge",
            (false, false) => "outside_target_pointer_edge",
            (true, true) => "internal_exact_target_pointer_edge",
            _ => "internal_target_pointer_edge"
        };
    }

    private static int EdgePriority(SessionXrefStableEdge edge) =>
        edge.Classification switch
        {
            "outside_exact_target_pointer_edge" => 0,
            "outside_target_pointer_edge" => 1,
            "internal_exact_target_pointer_edge" => 2,
            _ => 3
        };

    private static ulong ParseHexOrMax(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ulong.MaxValue;
        }

        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : ulong.MaxValue;
    }

    private sealed record LoadedXrefReport(string Path, SessionXrefAnalysisResult Report);

    private sealed record EdgeObservation(string InputPath, SessionXrefAnalysisResult Report, SessionXrefPointerHit Hit);
}

public sealed record SessionXrefChainSummaryResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_xref_chain_summary_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("input_paths")]
    public IReadOnlyList<string> InputPaths { get; init; } = [];

    [JsonPropertyName("input_count")]
    public int InputCount { get; init; }

    [JsonPropertyName("min_support")]
    public int MinSupport { get; init; }

    [JsonPropertyName("top_limit")]
    public int TopLimit { get; init; }

    [JsonPropertyName("stable_edge_count")]
    public int StableEdgeCount { get; init; }

    [JsonPropertyName("reciprocal_pair_count")]
    public int ReciprocalPairCount { get; init; }

    [JsonPropertyName("stable_edges")]
    public IReadOnlyList<SessionXrefStableEdge> StableEdges { get; init; } = [];

    [JsonPropertyName("reciprocal_pairs")]
    public IReadOnlyList<SessionXrefReciprocalPair> ReciprocalPairs { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("markdown_report_path")]
    public string? MarkdownReportPath { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record SessionXrefStableEdge
{
    [JsonPropertyName("edge_id")]
    public string EdgeId { get; init; } = string.Empty;

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

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("input_path_count")]
    public int InputPathCount { get; init; }

    [JsonPropertyName("supporting_input_paths")]
    public IReadOnlyList<string> SupportingInputPaths { get; init; } = [];

    [JsonPropertyName("supporting_snapshot_ids")]
    public IReadOnlyList<string> SupportingSnapshotIds { get; init; } = [];

    [JsonPropertyName("target_base_address_hexes")]
    public IReadOnlyList<string> TargetBaseAddressHexes { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}

public sealed record SessionXrefReciprocalPair
{
    [JsonPropertyName("pair_id")]
    public string PairId { get; init; } = string.Empty;

    [JsonPropertyName("first_base_address_hex")]
    public string FirstBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("second_base_address_hex")]
    public string SecondBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("first_to_second_edge_ids")]
    public IReadOnlyList<string> FirstToSecondEdgeIds { get; init; } = [];

    [JsonPropertyName("second_to_first_edge_ids")]
    public IReadOnlyList<string> SecondToFirstEdgeIds { get; init; } = [];

    [JsonPropertyName("support_count")]
    public int SupportCount { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
