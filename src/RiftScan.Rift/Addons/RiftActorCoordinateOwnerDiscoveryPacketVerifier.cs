using System.Text.Json;
using System.Text.Json.Serialization;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerDiscoveryPacketVerifier
{
    private const string ExpectedSchemaVersion = "riftscan.actor_coordinate_owner_discovery_packet.v2";

    public RiftActorCoordinateOwnerDiscoveryPacketVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Actor-coordinate owner discovery packet does not exist."));
            return new RiftActorCoordinateOwnerDiscoveryPacketVerificationResult { Path = fullPath, Issues = issues };
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
            issues.Add(Error("json_invalid", $"Invalid actor-coordinate owner discovery packet JSON: {ex.Message}"));
            return new RiftActorCoordinateOwnerDiscoveryPacketVerificationResult { Path = fullPath, Issues = issues };
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("json_root_invalid", "Actor-coordinate owner discovery packet JSON root must be an object."));
                return new RiftActorCoordinateOwnerDiscoveryPacketVerificationResult { Path = fullPath, Issues = issues };
            }

            var packetSchemaVersion = ReadRequiredString(root, "schema_version", "schema_version_missing", "schema_version is required.", issues);
            if (!string.IsNullOrWhiteSpace(packetSchemaVersion) &&
                !string.Equals(packetSchemaVersion, ExpectedSchemaVersion, StringComparison.Ordinal))
            {
                issues.Add(Error("schema_version_invalid", $"schema_version must be {ExpectedSchemaVersion}."));
            }

            var passiveSessionId = ReadNestedRequiredString(root, ["sessions", "passive"], "passive_session_missing", "sessions.passive is required.", issues);
            var moveSessionId = ReadNestedRequiredString(root, ["sessions", "move"], "move_session_missing", "sessions.move is required.", issues);
            var targetBaseAddressHex = ReadRequiredString(root, "target_base_address_hex", "target_base_address_missing", "target_base_address_hex is required.", issues);
            if (!LooksLikeHex(targetBaseAddressHex))
            {
                issues.Add(Error("target_base_address_invalid", "target_base_address_hex must be hexadecimal, for example 0x1000."));
            }

            ValidateSourceArtifacts(root, fullPath, issues);
            var canonicalPromotionStatus = ValidateMotionSummary(root, issues);
            ValidateMirrorContexts(root, issues);
            var (context01StableEdgeCount, crossSessionMin160StableEdgeCount, crossSessionMin320StableEdgeCount) =
                ValidateCrossSessionXrefs(root, issues);
            ValidateAssessment(root, issues);
            ValidateNextRecommendedAction(root, issues);

            return new RiftActorCoordinateOwnerDiscoveryPacketVerificationResult
            {
                Path = fullPath,
                PacketSchemaVersion = packetSchemaVersion,
                PassiveSessionId = passiveSessionId,
                MoveSessionId = moveSessionId,
                TargetBaseAddressHex = targetBaseAddressHex,
                CanonicalPromotionStatus = canonicalPromotionStatus,
                Context01StableEdgeCount = context01StableEdgeCount,
                CrossSessionMin160StableEdgeCount = crossSessionMin160StableEdgeCount,
                CrossSessionMin320StableEdgeCount = crossSessionMin320StableEdgeCount,
                Issues = issues
            };
        }
    }

    private static void ValidateSourceArtifacts(
        JsonElement root,
        string packetPath,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "source_artifacts", out var sourceArtifacts))
        {
            issues.Add(Error("source_artifacts_missing", "source_artifacts object is required."));
            return;
        }

        var requiredArtifactKeys = new[]
        {
            "motion",
            "move_context",
            "passive_context",
            "context01_chain",
            "passive_move_chain160",
            "passive_move_chain320"
        };
        foreach (var key in requiredArtifactKeys)
        {
            var artifactPath = ReadRequiredString(sourceArtifacts, key, "source_artifact_missing", $"source_artifacts.{key} is required.", issues);
            if (string.IsNullOrWhiteSpace(artifactPath))
            {
                continue;
            }

            if (!ArtifactExists(artifactPath, packetPath))
            {
                issues.Add(Error("source_artifact_file_missing", $"source_artifacts.{key} does not resolve to an existing file: {artifactPath}."));
            }
        }
    }

    private static string ValidateMotionSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "motion_summary", out var motionSummary))
        {
            issues.Add(Error("motion_summary_missing", "motion_summary object is required."));
            return string.Empty;
        }

        var canonicalPromotionStatus = ReadRequiredString(
            motionSummary,
            "canonical_promotion_status",
            "canonical_promotion_status_missing",
            "motion_summary.canonical_promotion_status is required.",
            issues);
        var motionClusterCount = ReadRequiredNonNegativeInt(
            motionSummary,
            "motion_cluster_count",
            "motion_cluster_count_missing",
            "motion_summary.motion_cluster_count is required.",
            issues);
        var synchronizedMirrorClusterCount = ReadRequiredNonNegativeInt(
            motionSummary,
            "synchronized_mirror_cluster_count",
            "synchronized_mirror_cluster_count_missing",
            "motion_summary.synchronized_mirror_cluster_count is required.",
            issues);

        if (motionClusterCount <= 0)
        {
            issues.Add(Error("motion_cluster_count_too_low", "motion_summary.motion_cluster_count must be greater than zero."));
        }

        if (synchronizedMirrorClusterCount > 0 &&
            !canonicalPromotionStatus.Contains("blocked", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("mirror_blocker_not_reflected", "canonical_promotion_status must reflect blocking while synchronized mirror clusters remain."));
        }

        return canonicalPromotionStatus;
    }

    private static void ValidateMirrorContexts(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "mirror_context_summary", out var mirrorSummary))
        {
            issues.Add(Error("mirror_context_summary_missing", "mirror_context_summary object is required."));
            return;
        }

        ValidateMirrorContextSide(mirrorSummary, "move", issues);
        ValidateMirrorContextSide(mirrorSummary, "passive", issues);
    }

    private static void ValidateMirrorContextSide(
        JsonElement mirrorSummary,
        string side,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(mirrorSummary, side, out var contextSide))
        {
            issues.Add(Error($"{side}_mirror_context_missing", $"mirror_context_summary.{side} object is required."));
            return;
        }

        var contextCount = ReadRequiredNonNegativeInt(
            contextSide,
            "context_count",
            $"{side}_context_count_missing",
            $"mirror_context_summary.{side}.context_count is required.",
            issues);
        if (contextCount <= 0)
        {
            issues.Add(Error($"{side}_context_count_too_low", $"mirror_context_summary.{side}.context_count must be greater than zero."));
        }

        if (!TryGetArray(contextSide, "contexts", out var contexts))
        {
            issues.Add(Error($"{side}_contexts_missing", $"mirror_context_summary.{side}.contexts array is required."));
            return;
        }

        if (contexts.GetArrayLength() != contextCount)
        {
            issues.Add(Error($"{side}_context_count_mismatch", $"mirror_context_summary.{side}.context_count must match contexts length."));
        }
    }

    private static (int Context01StableEdgeCount, int CrossSessionMin160StableEdgeCount, int CrossSessionMin320StableEdgeCount) ValidateCrossSessionXrefs(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "cross_session_xref_chain_summary", out var xrefs))
        {
            issues.Add(Error("cross_session_xref_summary_missing", "cross_session_xref_chain_summary object is required."));
            return (0, 0, 0);
        }

        var context01Count = ValidateXrefSummary(xrefs, "context01_min160", issues);
        var min160Count = ValidateXrefSummary(xrefs, "all_contexts_min160", issues);
        var min320Count = ValidateXrefSummary(xrefs, "all_contexts_min320", issues);
        return (context01Count, min160Count, min320Count);
    }

    private static int ValidateXrefSummary(
        JsonElement xrefs,
        string propertyName,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(xrefs, propertyName, out var summary))
        {
            issues.Add(Error($"{propertyName}_missing", $"cross_session_xref_chain_summary.{propertyName} object is required."));
            return 0;
        }

        var stableEdgeCount = ReadRequiredNonNegativeInt(
            summary,
            "stable_edge_count",
            $"{propertyName}_stable_edge_count_missing",
            $"cross_session_xref_chain_summary.{propertyName}.stable_edge_count is required.",
            issues);
        if (stableEdgeCount <= 0)
        {
            issues.Add(Error($"{propertyName}_stable_edge_count_too_low", $"cross_session_xref_chain_summary.{propertyName}.stable_edge_count must be greater than zero."));
        }

        if (!TryGetArray(summary, "stable_edges", out var stableEdges))
        {
            issues.Add(Error($"{propertyName}_stable_edges_missing", $"cross_session_xref_chain_summary.{propertyName}.stable_edges array is required."));
        }
        else if (stableEdgeCount > 0 && stableEdges.GetArrayLength() == 0)
        {
            issues.Add(Error($"{propertyName}_stable_edges_empty", $"cross_session_xref_chain_summary.{propertyName}.stable_edges must include at least one sampled edge."));
        }

        return stableEdgeCount;
    }

    private static void ValidateAssessment(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "current_assessment", out var assessment))
        {
            issues.Add(Error("current_assessment_missing", "current_assessment object is required."));
            return;
        }

        var status = ReadRequiredString(assessment, "status", "assessment_status_missing", "current_assessment.status is required.", issues);
        if (status.Contains("promoted", StringComparison.OrdinalIgnoreCase) &&
            !status.Contains("not_promoted", StringComparison.OrdinalIgnoreCase) &&
            !status.Contains("not promoted", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("unsupported_truth_promotion_claim", "Discovery packets must not claim canonical actor-coordinate truth promotion."));
        }

        ValidateNonEmptyArray(assessment, "strongest_evidence", "strongest_evidence_missing", "current_assessment.strongest_evidence must contain evidence entries.", issues);
        ValidateNonEmptyArray(assessment, "blocking_gaps", "blocking_gaps_missing", "current_assessment.blocking_gaps must contain remaining blocker entries.", issues);
    }

    private static void ValidateNextRecommendedAction(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
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

        if (!TryGetArray(nextAction, "candidate_offsets_to_prioritize", out var candidateOffsets) ||
            candidateOffsets.GetArrayLength() == 0)
        {
            issues.Add(Error("candidate_offsets_missing", "next_recommended_action.candidate_offsets_to_prioritize must contain at least one offset."));
            return;
        }

        foreach (var candidateOffset in candidateOffsets.EnumerateArray())
        {
            if (candidateOffset.ValueKind != JsonValueKind.String || !LooksLikeHex(candidateOffset.GetString() ?? string.Empty))
            {
                issues.Add(Error("candidate_offset_invalid", "candidate_offsets_to_prioritize values must be hexadecimal strings."));
            }
        }
    }

    private static void ValidateNonEmptyArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!TryGetArray(parent, propertyName, out var array) || array.GetArrayLength() == 0)
        {
            issues.Add(Error(code, message));
        }
    }

    private static string ReadNestedRequiredString(
        JsonElement root,
        IReadOnlyList<string> propertyPath,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        var current = root;
        for (var index = 0; index < propertyPath.Count; index++)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(propertyPath[index], out current))
            {
                issues.Add(Error(code, message));
                return string.Empty;
            }
        }

        if (current.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(current.GetString()))
        {
            issues.Add(Error(code, message));
            return string.Empty;
        }

        return current.GetString()!;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
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

    private static int ReadRequiredNonNegativeInt(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var parsed))
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

    private static bool ArtifactExists(string artifactPath, string packetPath)
    {
        if (Path.IsPathRooted(artifactPath))
        {
            return File.Exists(artifactPath);
        }

        if (File.Exists(Path.GetFullPath(artifactPath)))
        {
            return true;
        }

        var packetDirectory = Path.GetDirectoryName(packetPath);
        if (string.IsNullOrWhiteSpace(packetDirectory))
        {
            return false;
        }

        return File.Exists(Path.Combine(packetDirectory, artifactPath)) ||
            File.Exists(Path.Combine(packetDirectory, Path.GetFileName(artifactPath)));
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

    private static RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };
}

public sealed record RiftActorCoordinateOwnerDiscoveryPacketVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_discovery_packet_verification_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("packet_schema_version")]
    public string PacketSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("passive_session_id")]
    public string PassiveSessionId { get; init; } = string.Empty;

    [JsonPropertyName("move_session_id")]
    public string MoveSessionId { get; init; } = string.Empty;

    [JsonPropertyName("target_base_address_hex")]
    public string TargetBaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("canonical_promotion_status")]
    public string CanonicalPromotionStatus { get; init; } = string.Empty;

    [JsonPropertyName("context01_stable_edge_count")]
    public int Context01StableEdgeCount { get; init; }

    [JsonPropertyName("cross_session_min160_stable_edge_count")]
    public int CrossSessionMin160StableEdgeCount { get; init; }

    [JsonPropertyName("cross_session_min320_stable_edge_count")]
    public int CrossSessionMin320StableEdgeCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftActorCoordinateOwnerDiscoveryPacketVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
