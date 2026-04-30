using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerFollowupFindingsVerifier
{
    private const string ExpectedSchemaVersion = "riftscan.actor_coordinate_owner_followup_findings.v1";

    public RiftActorCoordinateOwnerFollowupFindingsVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Actor-coordinate owner follow-up findings JSON does not exist."));
            return new RiftActorCoordinateOwnerFollowupFindingsVerificationResult { Path = fullPath, Issues = issues };
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
            issues.Add(Error("json_invalid", $"Invalid actor-coordinate owner follow-up findings JSON: {ex.Message}"));
            return new RiftActorCoordinateOwnerFollowupFindingsVerificationResult { Path = fullPath, Issues = issues };
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("json_root_invalid", "Actor-coordinate owner follow-up findings JSON root must be an object."));
                return new RiftActorCoordinateOwnerFollowupFindingsVerificationResult { Path = fullPath, Issues = issues };
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
                issues.Add(Error("unsupported_truth_promotion_claim", "Follow-up findings must not claim canonical actor-coordinate owner promotion."));
            }

            ValidateSourceArtifacts(root, fullPath, issues, out var sourceArtifactCount);
            ValidateCaptureSummary(root, issues);
            var xrefSummary = ValidateXrefSummary(root, issues);
            ValidateEvidenceArrays(root, xrefSummary, issues, out var stableExactTargetEdgeCount);
            ValidateNextRecommendedAction(root, issues);

            return new RiftActorCoordinateOwnerFollowupFindingsVerificationResult
            {
                Path = fullPath,
                FindingsSchemaVersion = findingsSchemaVersion,
                SessionId = sessionId,
                Status = status,
                PointerHitCount = xrefSummary.PointerHitCount,
                ExactTargetPointerCount = xrefSummary.ExactTargetPointerCount,
                OutsideExactTargetPointerCount = xrefSummary.OutsideExactTargetPointerCount,
                StableExactTargetEdgeCount = stableExactTargetEdgeCount,
                ReciprocalPairCount = xrefSummary.ReciprocalPairCount,
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
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues,
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
            "xrefs_complete",
            "xref_chain_min2",
            "xref_chain_min2_verification"
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

            if (key is "capture_result" or "verify_session" or "xref_chain_min2_verification")
            {
                ValidateSuccessArtifact(resolvedArtifactPath, key, issues);
            }
        }
    }

    private static void ValidateSuccessArtifact(
        string path,
        string key,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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
    }

    private static void ValidateCaptureSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
    {
        if (!TryGetObject(root, "capture_summary", out var captureSummary))
        {
            issues.Add(Error("capture_summary_missing", "capture_summary object is required."));
            return;
        }

        ReadRequiredString(captureSummary, "capture_mode", "capture_mode_missing", "capture_summary.capture_mode is required.", issues);
        var targetRegionBaseHex = ReadRequiredString(captureSummary, "target_region_base_hex", "target_region_base_missing", "capture_summary.target_region_base_hex is required.", issues);
        if (!LooksLikeHex(targetRegionBaseHex))
        {
            issues.Add(Error("target_region_base_invalid", "capture_summary.target_region_base_hex must be hexadecimal."));
        }

        RequirePositiveInt(captureSummary, "windows_captured", "windows_captured_missing", "capture_summary.windows_captured is required and must be positive.", issues);
        RequirePositiveInt(captureSummary, "samples", "samples_missing", "capture_summary.samples is required and must be positive.", issues);
        RequirePositiveInt(captureSummary, "snapshots", "snapshots_missing", "capture_summary.snapshots is required and must be positive.", issues);
        RequirePositiveLong(captureSummary, "bytes_captured", "bytes_captured_missing", "capture_summary.bytes_captured is required and must be positive.", issues);
        if (!captureSummary.TryGetProperty("verified", out var verified) || verified.ValueKind != JsonValueKind.True)
        {
            issues.Add(Error("capture_not_verified", "capture_summary.verified must be true."));
        }
    }

    private static XrefSummaryCounts ValidateXrefSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
    {
        if (!TryGetObject(root, "xref_summary", out var xrefSummary))
        {
            issues.Add(Error("xref_summary_missing", "xref_summary object is required."));
            return new XrefSummaryCounts(0, 0, 0, 0, 0);
        }

        var pointerHitCount = RequirePositiveInt64(xrefSummary, "pointer_hit_count", "pointer_hit_count_missing", "xref_summary.pointer_hit_count is required and must be positive.", issues);
        var exactTargetPointerCount = RequirePositiveInt64(xrefSummary, "exact_target_pointer_count", "exact_target_pointer_count_missing", "xref_summary.exact_target_pointer_count is required and must be positive.", issues);
        var outsideExactTargetPointerCount = RequirePositiveInt64(xrefSummary, "outside_exact_target_pointer_count", "outside_exact_target_pointer_count_missing", "xref_summary.outside_exact_target_pointer_count is required and must be positive.", issues);
        var stableEdgeCount = RequirePositiveInt64(xrefSummary, "stable_edge_count_min_support_2", "stable_edge_count_missing", "xref_summary.stable_edge_count_min_support_2 is required and must be positive.", issues);
        var reciprocalPairCount = RequireNonNegativeInt64(xrefSummary, "reciprocal_pair_count", "reciprocal_pair_count_missing", "xref_summary.reciprocal_pair_count is required.", issues);

        if (exactTargetPointerCount > pointerHitCount)
        {
            issues.Add(Error("exact_target_count_exceeds_pointer_hits", "xref_summary.exact_target_pointer_count must not exceed pointer_hit_count."));
        }

        if (outsideExactTargetPointerCount > exactTargetPointerCount)
        {
            issues.Add(Error("outside_exact_count_exceeds_exact_count", "xref_summary.outside_exact_target_pointer_count must not exceed exact_target_pointer_count."));
        }

        return new XrefSummaryCounts(
            pointerHitCount,
            exactTargetPointerCount,
            outsideExactTargetPointerCount,
            stableEdgeCount,
            reciprocalPairCount);
    }

    private static void ValidateEvidenceArrays(
        JsonElement root,
        XrefSummaryCounts xrefSummary,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues,
        out int stableExactTargetEdgeCount)
    {
        stableExactTargetEdgeCount = 0;
        ValidateNonEmptyArray(root, "strongest_new_evidence", "strongest_new_evidence_missing", "strongest_new_evidence must contain evidence entries.", issues);
        ValidateNonEmptyArray(root, "blocking_gaps", "blocking_gaps_missing", "blocking_gaps must contain remaining blocker entries.", issues);

        if (!TryGetArray(root, "exact_target_edges_from_followup_window", out var exactEdges) || exactEdges.GetArrayLength() == 0)
        {
            issues.Add(Error("exact_target_edges_missing", "exact_target_edges_from_followup_window must contain at least one edge."));
        }
        else if (xrefSummary.OutsideExactTargetPointerCount > 0 && exactEdges.GetArrayLength() < xrefSummary.OutsideExactTargetPointerCount)
        {
            issues.Add(Error("exact_target_edge_count_mismatch", "exact_target_edges_from_followup_window must include the outside exact target edge evidence summarized by xref_summary."));
        }

        if (!TryGetArray(root, "stable_exact_target_edges_min_support_2", out var stableEdges) || stableEdges.GetArrayLength() == 0)
        {
            issues.Add(Error("stable_exact_edges_missing", "stable_exact_target_edges_min_support_2 must contain at least one stable edge."));
            return;
        }

        stableExactTargetEdgeCount = stableEdges.GetArrayLength();
        foreach (var edge in stableEdges.EnumerateArray())
        {
            ValidateStableExactEdge(edge, issues);
        }
    }

    private static void ValidateStableExactEdge(
        JsonElement edge,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
    {
        if (edge.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Error("stable_edge_invalid", "stable exact edge entries must be objects."));
            return;
        }

        foreach (var propertyName in new[] { "source_base_address_hex", "source_offset_hex", "pointer_value_hex", "target_offset_hex" })
        {
            var hexValue = ReadRequiredString(edge, propertyName, "stable_edge_hex_missing", $"stable_exact_target_edges_min_support_2.{propertyName} is required.", issues);
            if (!LooksLikeHex(hexValue))
            {
                issues.Add(Error("stable_edge_hex_invalid", $"stable_exact_target_edges_min_support_2.{propertyName} must be hexadecimal."));
            }
        }

        var supportCount = RequirePositiveInt(edge, "support_count", "stable_edge_support_missing", "stable_exact_target_edges_min_support_2.support_count is required.", issues);
        if (supportCount < 2)
        {
            issues.Add(Error("stable_edge_support_too_low", "stable_exact_target_edges_min_support_2.support_count must be at least 2."));
        }

        var matchKind = ReadRequiredString(edge, "match_kind", "stable_edge_match_kind_missing", "stable_exact_target_edges_min_support_2.match_kind is required.", issues);
        if (!string.Equals(matchKind, "exact_target_offset_pointer", StringComparison.Ordinal))
        {
            issues.Add(Error("stable_edge_match_kind_invalid", "stable exact edges must have match_kind exact_target_offset_pointer."));
        }

        if (!edge.TryGetProperty("source_is_target_region", out var sourceIsTargetRegion) ||
            sourceIsTargetRegion.ValueKind != JsonValueKind.False)
        {
            issues.Add(Error("stable_edge_not_followup_window", "stable exact edges must have source_is_target_region false."));
        }
    }

    private static void ValidateNextRecommendedAction(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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

    private static long RequirePositiveLong(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues) =>
        RequirePositiveInt64(parent, propertyName, code, message, issues);

    private static long RequirePositiveInt64(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> issues)
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

    private static RiftActorCoordinateOwnerFollowupFindingsVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };

    private sealed record XrefSummaryCounts(
        long PointerHitCount,
        long ExactTargetPointerCount,
        long OutsideExactTargetPointerCount,
        long StableEdgeCount,
        long ReciprocalPairCount);
}

public sealed record RiftActorCoordinateOwnerFollowupFindingsVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_followup_findings_verification_result.v1";

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

    [JsonPropertyName("pointer_hit_count")]
    public long PointerHitCount { get; init; }

    [JsonPropertyName("exact_target_pointer_count")]
    public long ExactTargetPointerCount { get; init; }

    [JsonPropertyName("outside_exact_target_pointer_count")]
    public long OutsideExactTargetPointerCount { get; init; }

    [JsonPropertyName("stable_exact_target_edge_count")]
    public int StableExactTargetEdgeCount { get; init; }

    [JsonPropertyName("reciprocal_pair_count")]
    public long ReciprocalPairCount { get; init; }

    [JsonPropertyName("source_artifact_count")]
    public int SourceArtifactCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftActorCoordinateOwnerFollowupFindingsVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftActorCoordinateOwnerFollowupFindingsVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
