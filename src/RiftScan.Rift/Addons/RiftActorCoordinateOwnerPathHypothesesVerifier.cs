using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerPathHypothesesVerifier
{
    private const string ExpectedSchemaVersion = "riftscan.actor_coordinate_owner_passive_owner_path_hypotheses.v1";
    private const int MinimumPassiveSupport = 8;

    public RiftActorCoordinateOwnerPathHypothesesVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftActorCoordinateOwnerPathHypothesesVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Actor-coordinate owner path hypotheses JSON does not exist."));
            return new RiftActorCoordinateOwnerPathHypothesesVerificationResult { Path = fullPath, Issues = issues };
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
            issues.Add(Error("json_invalid", $"Invalid actor-coordinate owner path hypotheses JSON: {ex.Message}"));
            return new RiftActorCoordinateOwnerPathHypothesesVerificationResult { Path = fullPath, Issues = issues };
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("json_root_invalid", "Actor-coordinate owner path hypotheses JSON root must be an object."));
                return new RiftActorCoordinateOwnerPathHypothesesVerificationResult { Path = fullPath, Issues = issues };
            }

            var hypothesesSchemaVersion = ReadRequiredString(root, "schema_version", "schema_version_missing", "schema_version is required.", issues);
            if (!string.IsNullOrWhiteSpace(hypothesesSchemaVersion) &&
                !string.Equals(hypothesesSchemaVersion, ExpectedSchemaVersion, StringComparison.Ordinal))
            {
                issues.Add(Error("schema_version_invalid", $"schema_version must be {ExpectedSchemaVersion}."));
            }

            var sessionId = ReadRequiredString(root, "session_id", "session_id_missing", "session_id is required.", issues);
            var status = ReadRequiredString(root, "status", "status_missing", "status is required.", issues);
            if (ClaimsPromotion(status))
            {
                issues.Add(Error("unsupported_truth_promotion_claim", "Path hypotheses must not claim canonical actor-coordinate owner promotion."));
            }

            ValidateSourceArtifacts(root, fullPath, issues, out var sourceArtifactCount);
            ValidateVerificationSummary(root, issues);
            var targetRegionBaseHex = ReadAndValidateHex(root, "target_region_base_hex", "target_region_base_missing", "target_region_base_hex is required.", issues);
            var ownerCandidateRegionBaseHex = ReadAndValidateHex(root, "owner_candidate_region_base_hex", "owner_candidate_region_base_missing", "owner_candidate_region_base_hex is required.", issues);
            if (!string.IsNullOrWhiteSpace(targetRegionBaseHex) &&
                string.Equals(targetRegionBaseHex, ownerCandidateRegionBaseHex, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("owner_candidate_matches_target_region", "owner_candidate_region_base_hex must differ from target_region_base_hex."));
            }

            var edgeSummary = ValidateExactTargetEdgeSummary(root, issues);
            var externalBaseEdgeSummary = ValidateExternalBaseEdges(root, targetRegionBaseHex, ownerCandidateRegionBaseHex, edgeSummary, issues);
            var internalExactEdgeCount = ValidateInternalExactEdges(root, targetRegionBaseHex, edgeSummary, issues);
            var top1000Summary = ValidateTop1000ChainSummary(root, issues);
            ValidateNonEmptyArray(root, "interpretation", "interpretation_missing", "interpretation must contain entries.", issues);
            ValidateNonEmptyArray(root, "blocking_gaps", "blocking_gaps_missing", "blocking_gaps must contain remaining blocker entries.", issues);
            ValidateNextRecommendedAction(root, issues);

            return new RiftActorCoordinateOwnerPathHypothesesVerificationResult
            {
                Path = fullPath,
                HypothesesSchemaVersion = hypothesesSchemaVersion,
                SessionId = sessionId,
                Status = status,
                TargetRegionBaseHex = targetRegionBaseHex,
                OwnerCandidateRegionBaseHex = ownerCandidateRegionBaseHex,
                ExactPointerHitCount = edgeSummary.ExactPointerHitCount,
                UniqueExactEdgeCount = edgeSummary.UniqueExactEdgeCount,
                MissingDiscriminatingExactOffsetCount = edgeSummary.MissingDiscriminatingExactOffsetCount,
                ExternalBaseEdgeCount = externalBaseEdgeSummary.EdgeCount,
                ExternalBaseEdgeSupportMax = externalBaseEdgeSummary.SupportMax,
                InternalExactEdgeCount = internalExactEdgeCount,
                TargetOutputIsCapped = top1000Summary.TargetOutputIsCapped,
                OwnerReciprocalPairCount = top1000Summary.OwnerReciprocalPairCount,
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
        string hypothesesPath,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues,
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
            "combined_passive_findings",
            "combined_passive_findings_verification",
            "target_region_xrefs",
            "target_region_xref_chain_min8_top1000",
            "target_region_xref_chain_min8_top1000_verification",
            "owner_region_xref_chain_min8_top1000",
            "owner_region_xref_chain_min8_top1000_verification"
        };

        foreach (var key in requiredArtifactKeys)
        {
            var artifactPath = ReadRequiredString(sourceArtifacts, key, "source_artifact_missing", $"source_artifacts.{key} is required.", issues);
            if (string.IsNullOrWhiteSpace(artifactPath))
            {
                continue;
            }

            sourceArtifactCount++;
            if (!TryResolveArtifactPath(artifactPath, hypothesesPath, out var resolvedArtifactPath))
            {
                issues.Add(Error("source_artifact_file_missing", $"source_artifacts.{key} does not resolve to an existing file: {artifactPath}."));
                continue;
            }

            if (key.EndsWith("_verification", StringComparison.Ordinal))
            {
                ValidateSuccessArtifact(resolvedArtifactPath, key, issues);
            }
        }
    }

    private static void ValidateSuccessArtifact(
        string path,
        string key,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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

    private static void ValidateVerificationSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetObject(root, "verification_summary", out var verificationSummary))
        {
            issues.Add(Error("verification_summary_missing", "verification_summary object is required."));
            return;
        }

        RequireTrue(verificationSummary, "combined_findings_success", "combined_findings_success_missing", "verification_summary.combined_findings_success must be true.", issues);
        RequireTrue(verificationSummary, "target_chain_success", "target_chain_success_missing", "verification_summary.target_chain_success must be true.", issues);
        RequireTrue(verificationSummary, "owner_chain_success", "owner_chain_success_missing", "verification_summary.owner_chain_success must be true.", issues);
        if (!TryGetArray(verificationSummary, "issues", out var summaryIssues))
        {
            issues.Add(Error("verification_summary_issues_missing", "verification_summary.issues array is required."));
        }
        else if (summaryIssues.GetArrayLength() > 0)
        {
            issues.Add(Error("verification_summary_has_issues", "verification_summary.issues must be empty for verified path hypotheses."));
        }
    }

    private static ExactTargetEdgeSummary ValidateExactTargetEdgeSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetObject(root, "exact_target_edge_summary", out var edgeSummary))
        {
            issues.Add(Error("exact_target_edge_summary_missing", "exact_target_edge_summary object is required."));
            return new ExactTargetEdgeSummary(0, 0, 0, 0, 0, 0);
        }

        var exactPointerHitCount = RequirePositiveInt64(edgeSummary, "exact_pointer_hit_count", "exact_pointer_hit_count_missing", "exact_target_edge_summary.exact_pointer_hit_count is required and must be positive.", issues);
        var uniqueExactEdgeCount = RequirePositiveInt(edgeSummary, "unique_exact_edge_count", "unique_exact_edge_count_missing", "exact_target_edge_summary.unique_exact_edge_count is required and must be positive.", issues);
        var supportSum = RequirePositiveInt64(edgeSummary, "support_sum", "support_sum_missing", "exact_target_edge_summary.support_sum is required and must be positive.", issues);
        RequireTrue(edgeSummary, "exact_edge_set_is_complete_for_target_xrefs", "exact_edge_set_complete_missing", "exact_target_edge_summary.exact_edge_set_is_complete_for_target_xrefs must be true.", issues);
        var outsideExactEdgeCount = RequirePositiveInt(edgeSummary, "outside_exact_edge_count", "outside_exact_edge_count_missing", "exact_target_edge_summary.outside_exact_edge_count is required and must be positive.", issues);
        var internalExactEdgeCount = RequireNonNegativeInt(edgeSummary, "internal_exact_edge_count", "internal_exact_edge_count_missing", "exact_target_edge_summary.internal_exact_edge_count is required.", issues);

        ValidateHexArray(edgeSummary, "exact_target_offsets", "exact_target_offsets_missing", "exact_target_edge_summary.exact_target_offsets must contain hexadecimal offsets.", issues, requireNonEmpty: true);
        var discriminatingOffsetCount = ValidateHexArray(edgeSummary, "discriminating_offsets_checked", "discriminating_offsets_checked_missing", "exact_target_edge_summary.discriminating_offsets_checked must contain checked offsets.", issues, requireNonEmpty: true);
        var missingDiscriminatingOffsetCount = ValidateHexArray(edgeSummary, "missing_discriminating_exact_offsets", "missing_discriminating_exact_offsets_missing", "exact_target_edge_summary.missing_discriminating_exact_offsets must contain missing discriminating offsets.", issues, requireNonEmpty: true);

        if (supportSum != exactPointerHitCount)
        {
            issues.Add(Error("support_sum_mismatch", "exact_target_edge_summary.support_sum must equal exact_pointer_hit_count."));
        }

        if (uniqueExactEdgeCount != outsideExactEdgeCount + internalExactEdgeCount)
        {
            issues.Add(Error("unique_exact_edge_count_mismatch", "unique_exact_edge_count must equal outside_exact_edge_count plus internal_exact_edge_count."));
        }

        if (missingDiscriminatingOffsetCount != discriminatingOffsetCount)
        {
            issues.Add(Error("discriminating_offset_not_all_missing", "All discriminating_offsets_checked must remain in missing_discriminating_exact_offsets for passive owner-path hypotheses."));
        }

        if (!ArrayContainsString(edgeSummary, "missing_discriminating_exact_offsets", "0x177E0"))
        {
            issues.Add(Error("missing_0x177e0_not_recorded", "missing_discriminating_exact_offsets must include 0x177E0."));
        }

        return new ExactTargetEdgeSummary(
            exactPointerHitCount,
            uniqueExactEdgeCount,
            supportSum,
            outsideExactEdgeCount,
            internalExactEdgeCount,
            missingDiscriminatingOffsetCount);
    }

    private static ExternalBaseEdgeSummary ValidateExternalBaseEdges(
        JsonElement root,
        string targetRegionBaseHex,
        string ownerCandidateRegionBaseHex,
        ExactTargetEdgeSummary edgeSummary,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetArray(root, "external_base_edges", out var externalBaseEdges) ||
            externalBaseEdges.GetArrayLength() == 0)
        {
            issues.Add(Error("external_base_edges_missing", "external_base_edges must contain at least one edge."));
            return new ExternalBaseEdgeSummary(0, 0, 0);
        }

        var edgeCount = externalBaseEdges.GetArrayLength();
        var supportMax = 0;
        var supportSum = 0;
        foreach (var edge in externalBaseEdges.EnumerateArray())
        {
            var supportCount = ValidateExternalBaseEdge(edge, targetRegionBaseHex, ownerCandidateRegionBaseHex, issues);
            supportMax = Math.Max(supportMax, supportCount);
            supportSum += supportCount;
        }

        if (edgeCount != edgeSummary.OutsideExactEdgeCount)
        {
            issues.Add(Error("external_base_edge_count_mismatch", "external_base_edges count must equal exact_target_edge_summary.outside_exact_edge_count."));
        }

        return new ExternalBaseEdgeSummary(edgeCount, supportMax, supportSum);
    }

    private static int ValidateExternalBaseEdge(
        JsonElement edge,
        string targetRegionBaseHex,
        string ownerCandidateRegionBaseHex,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (edge.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Error("external_base_edge_invalid", "external_base_edges entries must be objects."));
            return 0;
        }

        var sourceBaseAddressHex = ReadAndValidateHex(edge, "source_base_address_hex", "external_edge_source_base_missing", "external_base_edges.source_base_address_hex is required.", issues);
        ReadAndValidateHex(edge, "source_offset_hex", "external_edge_source_offset_missing", "external_base_edges.source_offset_hex is required.", issues);
        ReadAndValidateHex(edge, "source_absolute_address_hex", "external_edge_source_absolute_missing", "external_base_edges.source_absolute_address_hex is required.", issues);
        var pointerValueHex = ReadAndValidateHex(edge, "pointer_value_hex", "external_edge_pointer_value_missing", "external_base_edges.pointer_value_hex is required.", issues);
        var targetOffsetHex = ReadAndValidateHex(edge, "target_offset_hex", "external_edge_target_offset_missing", "external_base_edges.target_offset_hex is required.", issues);

        if (!string.IsNullOrWhiteSpace(ownerCandidateRegionBaseHex) &&
            !string.Equals(sourceBaseAddressHex, ownerCandidateRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_source_not_owner_candidate", "external_base_edges.source_base_address_hex must match owner_candidate_region_base_hex."));
        }

        if (!string.IsNullOrWhiteSpace(targetRegionBaseHex) &&
            !string.Equals(pointerValueHex, targetRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_pointer_not_target_base", "external_base_edges.pointer_value_hex must match target_region_base_hex."));
        }

        if (!string.Equals(targetOffsetHex, "0x0", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("external_edge_not_base_pointer", "external_base_edges.target_offset_hex must be 0x0."));
        }

        if (!edge.TryGetProperty("source_is_target_region", out var sourceIsTargetRegion) ||
            sourceIsTargetRegion.ValueKind != JsonValueKind.False)
        {
            issues.Add(Error("external_edge_source_is_target_region", "external_base_edges.source_is_target_region must be false."));
        }

        var supportCount = RequirePositiveInt(edge, "support_count", "external_edge_support_missing", "external_base_edges.support_count is required.", issues);
        if (supportCount < MinimumPassiveSupport)
        {
            issues.Add(Error("external_edge_support_too_low", $"external_base_edges.support_count must be at least {MinimumPassiveSupport}."));
        }

        if (!TryGetArray(edge, "supporting_snapshot_ids", out var supportingSnapshotIds) ||
            supportingSnapshotIds.GetArrayLength() < supportCount)
        {
            issues.Add(Error("external_edge_supporting_snapshots_missing", "external_base_edges.supporting_snapshot_ids must contain at least support_count snapshots."));
        }

        return supportCount;
    }

    private static int ValidateInternalExactEdges(
        JsonElement root,
        string targetRegionBaseHex,
        ExactTargetEdgeSummary edgeSummary,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetArray(root, "internal_exact_edges", out var internalExactEdges) ||
            internalExactEdges.GetArrayLength() == 0)
        {
            issues.Add(Error("internal_exact_edges_missing", "internal_exact_edges must contain at least one edge."));
            return 0;
        }

        var supportSum = 0;
        foreach (var edge in internalExactEdges.EnumerateArray())
        {
            supportSum += ValidateInternalExactEdge(edge, targetRegionBaseHex, issues);
        }

        if (internalExactEdges.GetArrayLength() != edgeSummary.InternalExactEdgeCount)
        {
            issues.Add(Error("internal_exact_edge_count_mismatch", "internal_exact_edges count must equal exact_target_edge_summary.internal_exact_edge_count."));
        }

        if (supportSum + (edgeSummary.OutsideExactEdgeCount * MinimumPassiveSupport) > edgeSummary.SupportSum)
        {
            issues.Add(Error("edge_support_sum_exceeds_summary", "Internal and external edge support must not exceed exact_target_edge_summary.support_sum."));
        }

        return internalExactEdges.GetArrayLength();
    }

    private static int ValidateInternalExactEdge(
        JsonElement edge,
        string targetRegionBaseHex,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (edge.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Error("internal_exact_edge_invalid", "internal_exact_edges entries must be objects."));
            return 0;
        }

        var sourceBaseAddressHex = ReadAndValidateHex(edge, "source_base_address_hex", "internal_edge_source_base_missing", "internal_exact_edges.source_base_address_hex is required.", issues);
        ReadAndValidateHex(edge, "source_offset_hex", "internal_edge_source_offset_missing", "internal_exact_edges.source_offset_hex is required.", issues);
        ReadAndValidateHex(edge, "source_absolute_address_hex", "internal_edge_source_absolute_missing", "internal_exact_edges.source_absolute_address_hex is required.", issues);
        ReadAndValidateHex(edge, "pointer_value_hex", "internal_edge_pointer_value_missing", "internal_exact_edges.pointer_value_hex is required.", issues);
        ReadAndValidateHex(edge, "target_offset_hex", "internal_edge_target_offset_missing", "internal_exact_edges.target_offset_hex is required.", issues);

        if (!string.IsNullOrWhiteSpace(targetRegionBaseHex) &&
            !string.Equals(sourceBaseAddressHex, targetRegionBaseHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("internal_edge_source_not_target_region", "internal_exact_edges.source_base_address_hex must match target_region_base_hex."));
        }

        if (!edge.TryGetProperty("source_is_target_region", out var sourceIsTargetRegion) ||
            sourceIsTargetRegion.ValueKind != JsonValueKind.True)
        {
            issues.Add(Error("internal_edge_source_not_target_region_flag", "internal_exact_edges.source_is_target_region must be true."));
        }

        var supportCount = RequirePositiveInt(edge, "support_count", "internal_edge_support_missing", "internal_exact_edges.support_count is required.", issues);
        if (supportCount < MinimumPassiveSupport)
        {
            issues.Add(Error("internal_edge_support_too_low", $"internal_exact_edges.support_count must be at least {MinimumPassiveSupport}."));
        }

        if (!TryGetArray(edge, "supporting_snapshot_ids", out var supportingSnapshotIds) ||
            supportingSnapshotIds.GetArrayLength() < supportCount)
        {
            issues.Add(Error("internal_edge_supporting_snapshots_missing", "internal_exact_edges.supporting_snapshot_ids must contain at least support_count snapshots."));
        }

        return supportCount;
    }

    private static Top1000Summary ValidateTop1000ChainSummary(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetObject(root, "top1000_chain_summary", out var summary))
        {
            issues.Add(Error("top1000_chain_summary_missing", "top1000_chain_summary object is required."));
            return new Top1000Summary(false, 0);
        }

        var targetStableEdgeCount = RequirePositiveInt64(summary, "target_stable_edge_count", "target_stable_edge_count_missing", "top1000_chain_summary.target_stable_edge_count is required and must be positive.", issues);
        var targetTopLimit = RequirePositiveInt(summary, "target_top_limit", "target_top_limit_missing", "top1000_chain_summary.target_top_limit is required and must be positive.", issues);
        var targetOutputIsCapped = RequireBool(summary, "target_output_is_capped", "target_output_is_capped_missing", "top1000_chain_summary.target_output_is_capped is required.", issues);
        var targetReciprocalPairCount = RequireNonNegativeInt64(summary, "target_reciprocal_pair_count", "target_reciprocal_pair_count_missing", "top1000_chain_summary.target_reciprocal_pair_count is required.", issues);
        RequirePositiveInt64(summary, "owner_stable_edge_count", "owner_stable_edge_count_missing", "top1000_chain_summary.owner_stable_edge_count is required and must be positive.", issues);
        RequirePositiveInt(summary, "owner_top_limit", "owner_top_limit_missing", "top1000_chain_summary.owner_top_limit is required and must be positive.", issues);
        RequireBool(summary, "owner_output_is_capped", "owner_output_is_capped_missing", "top1000_chain_summary.owner_output_is_capped is required.", issues);
        var ownerReciprocalPairCount = RequireNonNegativeInt64(summary, "owner_reciprocal_pair_count", "owner_reciprocal_pair_count_missing", "top1000_chain_summary.owner_reciprocal_pair_count is required.", issues);

        if (targetOutputIsCapped && targetStableEdgeCount < targetTopLimit)
        {
            issues.Add(Error("target_output_capped_count_mismatch", "target_output_is_capped cannot be true when target_stable_edge_count is below target_top_limit."));
        }

        if (targetReciprocalPairCount != 0 || ownerReciprocalPairCount != 0)
        {
            issues.Add(Error("reciprocal_pair_present", "Passive owner-path hypotheses must not contain reciprocal owner/container pairs."));
        }

        return new Top1000Summary(targetOutputIsCapped, ownerReciprocalPairCount);
    }

    private static void ValidateNextRecommendedAction(
        JsonElement root,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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

    private static int ValidateHexArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues,
        bool requireNonEmpty)
    {
        if (!TryGetArray(parent, propertyName, out var array))
        {
            issues.Add(Error(code, message));
            return 0;
        }

        if (requireNonEmpty && array.GetArrayLength() == 0)
        {
            issues.Add(Error($"{propertyName}_empty", message));
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !LooksLikeHex(item.GetString() ?? string.Empty))
            {
                issues.Add(Error($"{propertyName}_invalid", $"{propertyName} values must be hexadecimal strings."));
            }
        }

        return array.GetArrayLength();
    }

    private static bool ArrayContainsString(JsonElement parent, string propertyName, string value)
    {
        if (!TryGetArray(parent, propertyName, out var array))
        {
            return false;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String &&
                string.Equals(item.GetString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateNonEmptyArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!TryGetArray(parent, propertyName, out var array) || array.GetArrayLength() == 0)
        {
            issues.Add(Error(code, message));
        }
    }

    private static void RequireTrue(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.True)
        {
            issues.Add(Error(code, message));
        }
    }

    private static bool RequireBool(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            issues.Add(Error(code, message));
            return false;
        }

        return value.GetBoolean();
    }

    private static string ReadAndValidateHex(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
    {
        var value = ReadRequiredString(parent, propertyName, code, message, issues);
        if (!LooksLikeHex(value))
        {
            issues.Add(Error($"{propertyName}_invalid", $"{propertyName} must be hexadecimal."));
        }

        return value;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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

    private static int RequireNonNegativeInt(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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

    private static long RequirePositiveInt64(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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
        ICollection<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> issues)
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

    private static bool TryResolveArtifactPath(string artifactPath, string hypothesesPath, out string resolvedArtifactPath)
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

        var hypothesesDirectory = Path.GetDirectoryName(hypothesesPath);
        if (!string.IsNullOrWhiteSpace(hypothesesDirectory))
        {
            resolvedArtifactPath = Path.Combine(hypothesesDirectory, artifactPath);
            if (File.Exists(resolvedArtifactPath))
            {
                return true;
            }

            resolvedArtifactPath = Path.Combine(hypothesesDirectory, Path.GetFileName(artifactPath));
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

    private static RiftActorCoordinateOwnerPathHypothesesVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };

    private sealed record ExactTargetEdgeSummary(
        long ExactPointerHitCount,
        int UniqueExactEdgeCount,
        long SupportSum,
        int OutsideExactEdgeCount,
        int InternalExactEdgeCount,
        int MissingDiscriminatingExactOffsetCount);

    private sealed record ExternalBaseEdgeSummary(
        int EdgeCount,
        int SupportMax,
        int SupportSum);

    private sealed record Top1000Summary(
        bool TargetOutputIsCapped,
        long OwnerReciprocalPairCount);
}

public sealed record RiftActorCoordinateOwnerPathHypothesesVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.rift_actor_coordinate_owner_path_hypotheses_verification_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("hypotheses_schema_version")]
    public string HypothesesSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("target_region_base_hex")]
    public string TargetRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("owner_candidate_region_base_hex")]
    public string OwnerCandidateRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("exact_pointer_hit_count")]
    public long ExactPointerHitCount { get; init; }

    [JsonPropertyName("unique_exact_edge_count")]
    public int UniqueExactEdgeCount { get; init; }

    [JsonPropertyName("missing_discriminating_exact_offset_count")]
    public int MissingDiscriminatingExactOffsetCount { get; init; }

    [JsonPropertyName("external_base_edge_count")]
    public int ExternalBaseEdgeCount { get; init; }

    [JsonPropertyName("external_base_edge_support_max")]
    public int ExternalBaseEdgeSupportMax { get; init; }

    [JsonPropertyName("internal_exact_edge_count")]
    public int InternalExactEdgeCount { get; init; }

    [JsonPropertyName("target_output_is_capped")]
    public bool TargetOutputIsCapped { get; init; }

    [JsonPropertyName("owner_reciprocal_pair_count")]
    public long OwnerReciprocalPairCount { get; init; }

    [JsonPropertyName("source_artifact_count")]
    public int SourceArtifactCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftActorCoordinateOwnerPathHypothesesVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftActorCoordinateOwnerPathHypothesesVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
