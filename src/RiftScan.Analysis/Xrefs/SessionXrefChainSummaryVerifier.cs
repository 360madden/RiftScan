using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Xrefs;

public sealed record SessionXrefChainSummaryVerificationOptions
{
    public int MinSupport { get; init; } = 1;

    public IReadOnlyList<SessionXrefRequiredEdge> RequiredEdges { get; init; } = [];

    public IReadOnlyList<SessionXrefRequiredReciprocalPair> RequiredReciprocalPairs { get; init; } = [];
}

public sealed class SessionXrefChainSummaryVerifier
{
    public SessionXrefChainSummaryVerificationResult Verify(string path, SessionXrefChainSummaryVerificationOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new SessionXrefChainSummaryVerificationOptions();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinSupport);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<SessionXrefChainSummaryVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Xref-chain summary file does not exist."));
            return BuildResult(fullPath, options, stableEdgeCount: 0, reciprocalPairCount: 0, issues);
        }

        SessionXrefChainSummaryResult? summary;
        try
        {
            summary = JsonSerializer.Deserialize<SessionXrefChainSummaryResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid xref-chain summary JSON: {ex.Message}"));
            return BuildResult(fullPath, options, stableEdgeCount: 0, reciprocalPairCount: 0, issues);
        }

        if (summary is null)
        {
            issues.Add(Error("json_empty", "Xref-chain summary JSON did not contain an object."));
            return BuildResult(fullPath, options, stableEdgeCount: 0, reciprocalPairCount: 0, issues);
        }

        ValidateSummary(summary, options, issues);
        return BuildResult(fullPath, options, summary.StableEdges.Count, summary.ReciprocalPairs.Count, issues);
    }

    private static void ValidateSummary(
        SessionXrefChainSummaryResult summary,
        SessionXrefChainSummaryVerificationOptions options,
        ICollection<SessionXrefChainSummaryVerificationIssue> issues)
    {
        if (!string.Equals(summary.ResultSchemaVersion, "riftscan.session_xref_chain_summary_result.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("result_schema_version_invalid", "result_schema_version must be riftscan.session_xref_chain_summary_result.v1."));
        }

        if (!summary.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable xref-chain summary."));
        }

        if (summary.StableEdgeCount != summary.StableEdges.Count)
        {
            issues.Add(Error("stable_edge_count_mismatch", "stable_edge_count must match stable_edges length."));
        }

        if (summary.ReciprocalPairCount != summary.ReciprocalPairs.Count)
        {
            issues.Add(Error("reciprocal_pair_count_mismatch", "reciprocal_pair_count must match reciprocal_pairs length."));
        }

        ValidateEdges(summary.StableEdges, options, issues);
        ValidatePairs(summary.ReciprocalPairs, summary.StableEdges, options, issues);
    }

    private static void ValidateEdges(
        IReadOnlyList<SessionXrefStableEdge> stableEdges,
        SessionXrefChainSummaryVerificationOptions options,
        ICollection<SessionXrefChainSummaryVerificationIssue> issues)
    {
        var edgeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in stableEdges)
        {
            Require(edge.EdgeId, "edge_id_missing", "stable_edges.edge_id is required.", issues);
            Require(edge.SourceBaseAddressHex, "edge_source_base_missing", "stable_edges.source_base_address_hex is required.", issues);
            Require(edge.SourceAbsoluteAddressHex, "edge_source_absolute_missing", "stable_edges.source_absolute_address_hex is required.", issues);
            Require(edge.PointerValueHex, "edge_pointer_value_missing", "stable_edges.pointer_value_hex is required.", issues);
            Require(edge.MatchKind, "edge_match_kind_missing", "stable_edges.match_kind is required.", issues);
            Require(edge.Classification, "edge_classification_missing", "stable_edges.classification is required.", issues);

            if (!string.IsNullOrWhiteSpace(edge.EdgeId) && !edgeIds.Add(edge.EdgeId))
            {
                issues.Add(Error("edge_id_duplicate", $"Duplicate edge_id: {edge.EdgeId}."));
            }

            if (edge.SupportCount < 1)
            {
                issues.Add(Error("edge_support_count_invalid", "stable_edges.support_count must be >= 1."));
            }
        }

        foreach (var required in options.RequiredEdges)
        {
            var matchingEdge = stableEdges.FirstOrDefault(edge =>
                string.Equals(edge.SourceBaseAddressHex, required.SourceBaseAddressHex, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(edge.PointerValueHex, required.PointerValueHex, StringComparison.OrdinalIgnoreCase) &&
                edge.SupportCount >= options.MinSupport);
            if (matchingEdge is null)
            {
                issues.Add(Error(
                    "required_edge_missing",
                    $"Required edge {required.SourceBaseAddressHex}->{required.PointerValueHex} with support >= {options.MinSupport} was not found."));
            }
        }
    }

    private static void ValidatePairs(
        IReadOnlyList<SessionXrefReciprocalPair> reciprocalPairs,
        IReadOnlyList<SessionXrefStableEdge> stableEdges,
        SessionXrefChainSummaryVerificationOptions options,
        ICollection<SessionXrefChainSummaryVerificationIssue> issues)
    {
        var edgeIds = stableEdges
            .Select(edge => edge.EdgeId)
            .Where(edgeId => !string.IsNullOrWhiteSpace(edgeId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pairIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in reciprocalPairs)
        {
            Require(pair.PairId, "pair_id_missing", "reciprocal_pairs.pair_id is required.", issues);
            Require(pair.FirstBaseAddressHex, "pair_first_base_missing", "reciprocal_pairs.first_base_address_hex is required.", issues);
            Require(pair.SecondBaseAddressHex, "pair_second_base_missing", "reciprocal_pairs.second_base_address_hex is required.", issues);

            if (!string.IsNullOrWhiteSpace(pair.PairId) && !pairIds.Add(pair.PairId))
            {
                issues.Add(Error("pair_id_duplicate", $"Duplicate pair_id: {pair.PairId}."));
            }

            if (string.Equals(pair.FirstBaseAddressHex, pair.SecondBaseAddressHex, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("pair_self_reference", "reciprocal_pairs must not describe a self-base pair."));
            }

            if (pair.SupportCount < 1)
            {
                issues.Add(Error("pair_support_count_invalid", "reciprocal_pairs.support_count must be >= 1."));
            }

            if (pair.FirstToSecondEdgeIds.Count == 0 || pair.SecondToFirstEdgeIds.Count == 0)
            {
                issues.Add(Error("pair_edge_refs_missing", "reciprocal_pairs must include edge IDs in both directions."));
            }

            foreach (var edgeId in pair.FirstToSecondEdgeIds.Concat(pair.SecondToFirstEdgeIds))
            {
                if (!edgeIds.Contains(edgeId))
                {
                    issues.Add(Error("pair_edge_ref_missing", $"reciprocal_pairs references unknown edge_id: {edgeId}."));
                }
            }
        }

        foreach (var required in options.RequiredReciprocalPairs)
        {
            var matchingPair = reciprocalPairs.FirstOrDefault(pair =>
                PairMatches(pair, required.FirstBaseAddressHex, required.SecondBaseAddressHex) &&
                pair.SupportCount >= options.MinSupport);
            if (matchingPair is null)
            {
                issues.Add(Error(
                    "required_reciprocal_pair_missing",
                    $"Required reciprocal pair {required.FirstBaseAddressHex}<->{required.SecondBaseAddressHex} with support >= {options.MinSupport} was not found."));
            }
        }
    }

    private static bool PairMatches(SessionXrefReciprocalPair pair, string firstBaseAddressHex, string secondBaseAddressHex) =>
        string.Equals(pair.FirstBaseAddressHex, firstBaseAddressHex, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pair.SecondBaseAddressHex, secondBaseAddressHex, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(pair.FirstBaseAddressHex, secondBaseAddressHex, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pair.SecondBaseAddressHex, firstBaseAddressHex, StringComparison.OrdinalIgnoreCase);

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<SessionXrefChainSummaryVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message));
        }
    }

    private static SessionXrefChainSummaryVerificationResult BuildResult(
        string path,
        SessionXrefChainSummaryVerificationOptions options,
        int stableEdgeCount,
        int reciprocalPairCount,
        IReadOnlyList<SessionXrefChainSummaryVerificationIssue> issues) =>
        new()
        {
            Success = issues.Count == 0,
            Path = path,
            MinSupport = options.MinSupport,
            RequiredEdgeCount = options.RequiredEdges.Count,
            RequiredReciprocalPairCount = options.RequiredReciprocalPairs.Count,
            StableEdgeCount = stableEdgeCount,
            ReciprocalPairCount = reciprocalPairCount,
            Issues = issues
        };

    private static SessionXrefChainSummaryVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };
}

public sealed record SessionXrefRequiredEdge
{
    [JsonPropertyName("source_base_address_hex")]
    public string SourceBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("pointer_value_hex")]
    public string PointerValueHex { get; init; } = string.Empty;
}

public sealed record SessionXrefRequiredReciprocalPair
{
    [JsonPropertyName("first_base_address_hex")]
    public string FirstBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("second_base_address_hex")]
    public string SecondBaseAddressHex { get; init; } = string.Empty;
}

public sealed record SessionXrefChainSummaryVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.session_xref_chain_summary_verification_result.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("min_support")]
    public int MinSupport { get; init; }

    [JsonPropertyName("required_edge_count")]
    public int RequiredEdgeCount { get; init; }

    [JsonPropertyName("required_reciprocal_pair_count")]
    public int RequiredReciprocalPairCount { get; init; }

    [JsonPropertyName("stable_edge_count")]
    public int StableEdgeCount { get; init; }

    [JsonPropertyName("reciprocal_pair_count")]
    public int ReciprocalPairCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<SessionXrefChainSummaryVerificationIssue> Issues { get; init; } = [];
}

public sealed record SessionXrefChainSummaryVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
