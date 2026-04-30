using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerCombinedPassiveFindingsVerifier
{
    private const string ExpectedSchemaVersion = "riftscan.actor_coordinate_owner_combined_passive_findings.v1";
    private const int MinimumCombinedPassiveSupport = 8;

    public RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Actor-coordinate owner combined passive findings JSON does not exist."));
            return new RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult { Path = fullPath, Issues = issues };
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(fullPath), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid actor-coordinate owner combined passive findings JSON: {ex.Message}"));
            return new RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult { Path = fullPath, Issues = issues };
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("json_root_invalid", "Actor-coordinate owner combined passive findings JSON root must be an object."));
                return new RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult { Path = fullPath, Issues = issues };
            }

            var findingsSchemaVersion = ReadRequiredString(root, "schema_version", "schema_version_missing", "schema_version is required.", issues);
            if (!string.IsNullOrWhiteSpace(findingsSchemaVersion) &&
                !string.Equals(findingsSchemaVersion, ExpectedSchemaVersion, StringComparison.Ordinal))
            {
                issues.Add(Error("schema_version_invalid", $"schema_version must be {ExpectedSchemaVersion}."));
            }

            var sessionId = ReadRequiredString(root, "session_id", "session_id_missing", "session_id is required.", issues);
            var status = ReadRequiredString(root, "status", "status_missing", "status is required.", issues);
            if (ClaimsPromotion(status))
            {
                issues.Add(Error("unsupported_truth_promotion_claim", "Combined passive findings must not claim canonical actor-coordinate owner promotion."));
            }

            ValidateSourceArtifacts(root, fullPath, issues, out var sourceArtifactCount);
            var captureSummary = ValidateCaptureSummary(root, issues);
            var targetXrefs = ValidateTargetRegionXrefSummary(root, captureSummary, issues, out var externalExactEdgeCount, out var externalExactEdgeSupportMax);
            var ownerXrefs = ValidateOwnerRegionXrefSummary(root, issues);
            ValidateEvidenceArrays(root, issues);
            ValidateNextRecommendedAction(root, issues);

            return new RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult
            {
                Path = fullPath,
                FindingsSchemaVersion = findingsSchemaVersion,
                SessionId = sessionId,
                Status = status,
                TargetRegionBaseHex = captureSummary.TargetRegionBaseHex,
                OwnerCandidateRegionBaseHex = captureSummary.OwnerCandidateRegionBaseHex,
                ExternalExactEdgeCount = externalExactEdgeCount,
                ExternalExactEdgeSupportMax = externalExactEdgeSupportMax,
                TargetRegionPointerHitCount = targetXrefs.PointerHitCount,
                TargetRegionExactTargetPointerCount = targetXrefs.ExactTargetPointerCount,
                TargetRegionOutsideExactTargetPointerCount = targetXrefs.OutsideExactTargetPointerCount,
                TargetRegionStableEdgeCount = targetXrefs.StableEdgeCount,
                OwnerRegionPointerHitCount = ownerXrefs.PointerHitCount,
                OwnerRegionExactTargetPointerCount = ownerXrefs.ExactTargetPointerCount,
                OwnerRegionOutsideTargetRegionPointerCount = ownerXrefs.OutsideTargetRegionPointerCount,
                OwnerRegionOutsideExactTargetPointerCount = ownerXrefs.OutsideExactTargetPointerCount,
                OwnerRegionStableEdgeCount = ownerXrefs.StableEdgeCount,
                ReciprocalPairCount = targetXrefs.ReciprocalPairCount + ownerXrefs.ReciprocalPairCount,
                SourceArtifactCount = sourceArtifactCount,
                Issues = issues
            };
        }
    }

    private static bool ClaimsPromotion(string status) =>
        status.Contains("promoted", StringComparison.OrdinalIgnoreCase) &&
        !status.Contains("not_promoted", StringComparison.OrdinalIgnoreCase) &&
        !status.Contains("not promoted", StringComparison.OrdinalIgnoreCase);

    private static void ValidateSourceArtifacts(
        JsonElement root,
        string findingsPath,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues,
        out int sourceArtifactCount)
    {
        sourceArtifactCount = 0;
        if (!TryGetObject(root, "source_artifacts", out var sourceArtifacts))
        {
            issues.Add(Error("source_artifacts_missing", "source_artifacts object is required."));
            return;
        }

        var requiredArtifactKeys = new[]
        {
            "preflight_inventory",
            "capture_result",
            "verify_session",
            "target_region_xrefs",
            "target_region_xref_chain_min8",
            "target_region_xref_chain_min8_verification",
            "owner_region_xrefs",
            "owner_region_xref_chain_min8"
        };

        foreach (var key in requiredArtifactKeys)
        {
            var artifactPath = ReadRequiredString(sourceArtifacts, key, "source_artifact_missing", $"source_artifacts.{key} is required.", issues);
            if (string.IsNullOrWhiteSpace(artifactPath))
            {
                continue;
            }

            sourceArtifactCount++;
            if (!TryResolveArtifactPath(artifactPath, findingsPath, out var resolvedArtifactPath))
            {
                issues.Add(Error("source_artifact_file_missing", $"source_artifacts.{key} does not resolve to an existing file: {artifactPath}."));
                continue;
            }

            if (key is "capture_result" or "verify_session" or "target_region_xref_chain_min8_verification")
            {
                ValidateSuccessArtifact(resolvedArtifactPath, key, issues);
            }
        }
    }

    private static void ValidateSuccessArtifact(
        string path,
        string key,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("success", out var success) ||
                success.ValueKind != JsonValueKind.True)
            {
                issues.Add(Error("source_artifact_not_success", $"source_artifacts.{key} must point to a JSON result with success true."));
            }
        }
        catch (JsonException ex)
        {
            issues.Add(Error("source_artifact_json_invalid", $"source_artifacts.{key} is not valid JSON: {ex.Message}"));
        }
        catch (IOException ex)
        {
            issues.Add(Error("source_artifact_read_failed", $"source_artifacts.{key} could not be read: {ex.Message}"));
        }
    }

    private static CaptureSummaryCounts ValidateCaptureSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!TryGetObject(root, "capture_summary", out var captureSummary))
        {
            issues.Add(Error("capture_summary_missing", "capture_summary object is required."));
            return new CaptureSummaryCounts(string.Empty, string.Empty);
        }

        ReadRequiredString(captureSummary, "capture_mode", "capture_mode_missing", "capture_summary.capture_mode is required.", issues);
        RequirePositiveInt(captureSummary, "regions_captured", "regions_captured_missing", "capture_summary.regions_captured is required and must be positive.", issues);
        var targetRegionBaseHex = ReadRequiredString(captureSummary, "target_region_base_hex", "target_region_base_missing", "capture_summary.target_region_base_hex is required.", issues);
        var ownerCandidateRegionBaseHex = ReadRequiredString(captureSummary, "owner_candidate_region_base_hex", "owner_candidate_region_base_missing", "capture_summary.owner_candidate_region_base_hex is required.", issues);
        if (!LooksLikeHex(targetRegionBaseHex))
        {
            issues.Add(Error("target_region_base_invalid", "capture_summary.target_region_base_hex must be hexadecimal."));
        }

        if (!LooksLikeHex(ownerCandidateRegionBaseHex))
        {
            issues.Add(Error("owner_candidate_region_base_invalid", "capture_summary.owner_candidate_region_base_hex must be hexadecimal."));
        }

        if (!string.IsNullOrWhiteSpace(targetRegionBaseHex) &&
            string.Equals(targetRegionBaseHex, ownerCandidateRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("owner_candidate_matches_target_region", "capture_summary.owner_candidate_region_base_hex must differ from target_region_base_hex."));
        }

        RequirePositiveInt(captureSummary, "samples", "samples_missing", "capture_summary.samples is required and must be positive.", issues);
        RequirePositiveInt(captureSummary, "snapshots", "snapshots_missing", "capture_summary.snapshots is required and must be positive.", issues);
        RequirePositiveInt64(captureSummary, "bytes_captured", "bytes_captured_missing", "capture_summary.bytes_captured is required and must be positive.", issues);
        if (!captureSummary.TryGetProperty("verified", out var verified) || verified.ValueKind != JsonValueKind.True)
        {
            issues.Add(Error("capture_not_verified", "capture_summary.verified must be true."));
        }

        return new CaptureSummaryCounts(targetRegionBaseHex, ownerCandidateRegionBaseHex);
    }

    private static XrefSummaryCounts ValidateTargetRegionXrefSummary(
        JsonElement root,
        CaptureSummaryCounts captureSummary,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues,
        out int externalExactEdgeCount,
        out int externalExactEdgeSupportMax)
    {
        externalExactEdgeCount = 0;
        externalExactEdgeSupportMax = 0;
        if (!TryGetObject(root, "target_region_xref_summary", out var xrefSummary))
        {
            issues.Add(Error("target_region_xref_summary_missing", "target_region_xref_summary object is required."));
            return new XrefSummaryCounts(0, 0, 0, 0, 0, 0);
        }

        var counts = ReadXrefSummaryCounts(xrefSummary, "target_region_xref_summary", requireOutsideExactTargetPointers: true, issues);
        if (!TryGetArray(xrefSummary, "external_exact_edges", out var externalEdges) || externalEdges.GetArrayLength() == 0)
        {
            issues.Add(Error("external_exact_edges_missing", "target_region_xref_summary.external_exact_edges must contain at least one external edge."));
            return counts;
        }

        externalExactEdgeCount = externalEdges.GetArrayLength();
        foreach (var edge in externalEdges.EnumerateArray())
        {
            var supportCount = ValidateExternalExactEdge(edge, captureSummary, issues);
            externalExactEdgeSupportMax = Math.Max(externalExactEdgeSupportMax, supportCount);
        }

        return counts;
    }

    private static XrefSummaryCounts ValidateOwnerRegionXrefSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!TryGetObject(root, "owner_region_xref_summary", out var xrefSummary))
        {
            issues.Add(Error("owner_region_xref_summary_missing", "owner_region_xref_summary object is required."));
            return new XrefSummaryCounts(0, 0, 0, 0, 0, 0);
        }

        return ReadXrefSummaryCounts(xrefSummary, "owner_region_xref_summary", requireOutsideExactTargetPointers: false, issues);
    }

    private static XrefSummaryCounts ReadXrefSummaryCounts(
        JsonElement xrefSummary,
        string summaryName,
        bool requireOutsideExactTargetPointers,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        var pointerHitCount = RequirePositiveInt64(xrefSummary, "pointer_hit_count", $"{summaryName}_pointer_hit_count_missing", $"{summaryName}.pointer_hit_count is required and must be positive.", issues);
        var exactTargetPointerCount = RequirePositiveInt64(xrefSummary, "exact_target_pointer_count", $"{summaryName}_exact_target_pointer_count_missing", $"{summaryName}.exact_target_pointer_count is required and must be positive.", issues);
        var outsideTargetRegionPointerCount = RequireNonNegativeInt64(xrefSummary, "outside_target_region_pointer_count", $"{summaryName}_outside_target_region_pointer_count_missing", $"{summaryName}.outside_target_region_pointer_count is required.", issues);
        var outsideExactTargetPointerCount = requireOutsideExactTargetPointers
            ? RequirePositiveInt64(xrefSummary, "outside_exact_target_pointer_count", $"{summaryName}_outside_exact_target_pointer_count_missing", $"{summaryName}.outside_exact_target_pointer_count is required and must be positive.", issues)
            : RequireNonNegativeInt64(xrefSummary, "outside_exact_target_pointer_count", $"{summaryName}_outside_exact_target_pointer_count_missing", $"{summaryName}.outside_exact_target_pointer_count is required.", issues);
        var stableEdgeCount = RequirePositiveInt64(xrefSummary, "stable_edge_count_min_support_8", $"{summaryName}_stable_edge_count_missing", $"{summaryName}.stable_edge_count_min_support_8 is required and must be positive.", issues);
        var reciprocalPairCount = RequireNonNegativeInt64(xrefSummary, "reciprocal_pair_count", $"{summaryName}_reciprocal_pair_count_missing", $"{summaryName}.reciprocal_pair_count is required.", issues);

        if (exactTargetPointerCount > pointerHitCount)
        {
            issues.Add(Error($"{summaryName}_exact_target_count_exceeds_pointer_hits", $"{summaryName}.exact_target_pointer_count must not exceed pointer_hit_count."));
        }

        if (outsideExactTargetPointerCount > exactTargetPointerCount)
        {
            issues.Add(Error($"{summaryName}_outside_exact_count_exceeds_exact_count", $"{summaryName}.outside_exact_target_pointer_count must not exceed exact_target_pointer_count."));
        }

        if (outsideExactTargetPointerCount > outsideTargetRegionPointerCount && outsideTargetRegionPointerCount > 0)
        {
            issues.Add(Error($"{summaryName}_outside_exact_count_exceeds_outside_count", $"{summaryName}.outside_exact_target_pointer_count must not exceed outside_target_region_pointer_count."));
        }

        return new XrefSummaryCounts(
            pointerHitCount,
            exactTargetPointerCount,
            outsideTargetRegionPointerCount,
            outsideExactTargetPointerCount,
            stableEdgeCount,
            reciprocalPairCount);
    }

    private static int ValidateExternalExactEdge(
        JsonElement edge,
        CaptureSummaryCounts captureSummary,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (edge.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Error("external_edge_invalid", "target_region_xref_summary.external_exact_edges entries must be objects."));
            return 0;
        }

        var sourceBaseAddressHex = ReadRequiredString(edge, "source_base_address_hex", "external_edge_source_base_missing", "external exact edge source_base_address_hex is required.", issues);
        var pointerValueHex = ReadRequiredString(edge, "pointer_value_hex", "external_edge_pointer_value_missing", "external exact edge pointer_value_hex is required.", issues);
        var targetOffsetHex = ReadRequiredString(edge, "target_offset_hex", "external_edge_target_offset_missing", "external exact edge target_offset_hex is required.", issues);
        foreach (var propertyName in new[] { "source_base_address_hex", "source_offset_hex", "pointer_value_hex", "target_offset_hex" })
        {
            var hexValue = ReadRequiredString(edge, propertyName, "external_edge_hex_missing", $"external exact edge {propertyName} is required.", issues);
            if (!LooksLikeHex(hexValue))
            {
                issues.Add(Error("external_edge_hex_invalid", $"external exact edge {propertyName} must be hexadecimal."));
            }
        }

        if (!string.IsNullOrWhiteSpace(captureSummary.OwnerCandidateRegionBaseHex) &&
            !string.Equals(sourceBaseAddressHex, captureSummary.OwnerCandidateRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_source_not_owner_candidate", "External exact edge source_base_address_hex must match capture_summary.owner_candidate_region_base_hex."));
        }

        if (!string.IsNullOrWhiteSpace(captureSummary.TargetRegionBaseHex) &&
            !string.Equals(pointerValueHex, captureSummary.TargetRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_target_not_target_region_base", "External exact edge pointer_value_hex must match capture_summary.target_region_base_hex."));
        }

        if (!string.Equals(targetOffsetHex, "0x0", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_not_base_pointer", "Combined passive external exact edge must point at target offset 0x0."));
        }

        var supportCount = RequirePositiveInt(edge, "support_count", "external_edge_support_missing", "external exact edge support_count is required.", issues);
        if (supportCount < MinimumCombinedPassiveSupport)
        {
            issues.Add(Error("external_edge_support_too_low", $"External exact edge support_count must be at least {MinimumCombinedPassiveSupport}."));
        }

        var matchKind = ReadRequiredString(edge, "match_kind", "external_edge_match_kind_missing", "external exact edge match_kind is required.", issues);
        if (!string.Equals(matchKind, "exact_target_offset_pointer", StringComparison.Ordinal))
        {
            issues.Add(Error("external_edge_match_kind_invalid", "External exact edges must have match_kind exact_target_offset_pointer."));
        }

        if (!edge.TryGetProperty("source_is_target_region", out var sourceIsTargetRegion) ||
            sourceIsTargetRegion.ValueKind != JsonValueKind.False)
        {
            issues.Add(Error("external_edge_source_is_target_region", "External exact edges must have source_is_target_region false."));
        }

        return supportCount;
    }

    private static void ValidateEvidenceArrays(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        ValidateNonEmptyArray(root, "strongest_new_evidence", "strongest_new_evidence_missing", "strongest_new_evidence must contain evidence entries.", issues);
        ValidateNonEmptyArray(root, "blocking_gaps", "blocking_gaps_missing", "blocking_gaps must contain remaining blocker entries.", issues);
    }

    private static void ValidateNextRecommendedAction(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!TryGetObject(root, "next_recommended_action", out var nextAction))
        {
            issues.Add(Error("next_recommended_action_missing", "next_recommended_action object is required."));
            return;
        }

        ReadRequiredString(nextAction, "mode", "next_action_mode_missing", "next_recommended_action.mode is required.", issues);
        ReadRequiredString(nextAction, "reason", "next_action_reason_missing", "next_recommended_action.reason is required.", issues);
        ReadRequiredString(nextAction, "expected_signal", "next_action_expected_signal_missing", "next_recommended_action.expected_signal is required.", issues);
        ReadRequiredString(nextAction, "stop_condition", "next_action_stop_condition_missing", "next_recommended_action.stop_condition is required.", issues);
    }

    private static void ValidateNonEmptyArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!TryGetArray(parent, propertyName, out var array) || array.GetArrayLength() == 0)
        {
            issues.Add(Error(code, message));
        }
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            issues.Add(Error(code, message));
            return string.Empty;
        }

        return value.GetString()!;
    }

    private static int RequirePositiveInt(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var parsed))
        {
            issues.Add(Error(code, message));
            return 0;
        }

        if (parsed <= 0)
        {
            issues.Add(Error($"{propertyName}_too_low", message));
        }

        return parsed;
    }

    private static long RequirePositiveInt64(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        var parsed = RequireNonNegativeInt64(parent, propertyName, code, message, issues);
        if (parsed <= 0)
        {
            issues.Add(Error($"{propertyName}_too_low", message));
        }

        return parsed;
    }

    private static long RequireNonNegativeInt64(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt64(out var parsed))
        {
            issues.Add(Error(code, message));
            return 0;
        }

        if (parsed < 0)
        {
            issues.Add(Error($"{propertyName}_negative", $"{propertyName} must not be negative."));
        }

        return parsed;
    }

    private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value) =>
        parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object;

    private static bool TryGetArray(JsonElement parent, string propertyName, out JsonElement value) =>
        parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array;

    private static bool TryResolveArtifactPath(string artifactPath, string findingsPath, out string resolvedArtifactPath)
    {
        if (Path.IsPathRooted(artifactPath))
        {
            resolvedArtifactPath = artifactPath;
            return File.Exists(resolvedArtifactPath);
        }

        resolvedArtifactPath = Path.GetFullPath(artifactPath);
        if (File.Exists(resolvedArtifactPath))
        {
            return true;
        }

        var findingsDirectory = Path.GetDirectoryName(findingsPath);
        if (!string.IsNullOrWhiteSpace(findingsDirectory))
        {
            resolvedArtifactPath = Path.Combine(findingsDirectory, artifactPath);
            if (File.Exists(resolvedArtifactPath))
            {
                return true;
            }

            resolvedArtifactPath = Path.Combine(findingsDirectory, Path.GetFileName(artifactPath));
            if (File.Exists(resolvedArtifactPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return text.Length > 0 && text.All(Uri.IsHexDigit);
    }

    private static RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };

    private sealed record CaptureSummaryCounts(
        string TargetRegionBaseHex,
        string OwnerCandidateRegionBaseHex);

    private sealed record XrefSummaryCounts(
        long PointerHitCount,
        long ExactTargetPointerCount,
        long OutsideTargetRegionPointerCount,
        long OutsideExactTargetPointerCount,
        long StableEdgeCount,
        long ReciprocalPairCount);
}

public sealed record RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_combined_passive_findings_verification_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("findings_schema_version")]
    public string FindingsSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("target_region_base_hex")]
    public string TargetRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("owner_candidate_region_base_hex")]
    public string OwnerCandidateRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("external_exact_edge_count")]
    public int ExternalExactEdgeCount { get; init; }

    [JsonPropertyName("external_exact_edge_support_max")]
    public int ExternalExactEdgeSupportMax { get; init; }

    [JsonPropertyName("target_region_pointer_hit_count")]
    public long TargetRegionPointerHitCount { get; init; }

    [JsonPropertyName("target_region_exact_target_pointer_count")]
    public long TargetRegionExactTargetPointerCount { get; init; }

    [JsonPropertyName("target_region_outside_exact_target_pointer_count")]
    public long TargetRegionOutsideExactTargetPointerCount { get; init; }

    [JsonPropertyName("target_region_stable_edge_count")]
    public long TargetRegionStableEdgeCount { get; init; }

    [JsonPropertyName("owner_region_pointer_hit_count")]
    public long OwnerRegionPointerHitCount { get; init; }

    [JsonPropertyName("owner_region_exact_target_pointer_count")]
    public long OwnerRegionExactTargetPointerCount { get; init; }

    [JsonPropertyName("owner_region_outside_target_region_pointer_count")]
    public long OwnerRegionOutsideTargetRegionPointerCount { get; init; }

    [JsonPropertyName("owner_region_outside_exact_target_pointer_count")]
    public long OwnerRegionOutsideExactTargetPointerCount { get; init; }

    [JsonPropertyName("owner_region_stable_edge_count")]
    public long OwnerRegionStableEdgeCount { get; init; }

    [JsonPropertyName("reciprocal_pair_count")]
    public long ReciprocalPairCount { get; init; }

    [JsonPropertyName("source_artifact_count")]
    public int SourceArtifactCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftActorCoordinateOwnerCombinedPassiveFindingsVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
