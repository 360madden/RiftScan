using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class Vec3TruthPromotionVerifier
{
    public Vec3TruthPromotionVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<Vec3TruthPromotionVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Vec3 truth promotion file does not exist."));
            return new Vec3TruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        Vec3TruthPromotionResult? result;
        try
        {
            result = JsonSerializer.Deserialize<Vec3TruthPromotionResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid vec3 truth promotion JSON: {ex.Message}"));
            return new Vec3TruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Vec3 truth promotion JSON did not contain an object."));
            return new Vec3TruthPromotionVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new Vec3TruthPromotionVerificationResult
        {
            Path = fullPath,
            PromotedCandidateCount = result.PromotedCandidateCount,
            BlockedCandidateCount = result.BlockedCandidateCount,
            RecommendedManualReviewCandidateId = result.RecommendedManualReviewCandidateId,
            Issues = issues
        };
    }

    private static void ValidateResult(Vec3TruthPromotionResult result, ICollection<Vec3TruthPromotionVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.vec3_truth_promotion.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.vec3_truth_promotion.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable vec3 truth promotion packet."));
        }

        Require(result.RecoveryPath, "recovery_path_missing", "recovery_path is required.", issues);
        Require(result.CorroborationPath, "corroboration_path_missing", "corroboration_path is required.", issues);
        if (result.PromotedCandidateCount != result.PromotedCandidates.Count)
        {
            issues.Add(Error("promoted_candidate_count_mismatch", "promoted_candidate_count must match promoted_candidates length."));
        }

        if (result.BlockedCandidateCount != result.BlockedCandidates.Count)
        {
            issues.Add(Error("blocked_candidate_count_mismatch", "blocked_candidate_count must match blocked_candidates length."));
        }

        if (result.PromotedCandidateCount + result.BlockedCandidateCount > result.RecoveredCandidateCount)
        {
            issues.Add(Error("candidate_count_invalid", "promoted_candidate_count plus blocked_candidate_count must be less than or equal to recovered_candidate_count."));
        }

        if (!result.Warnings.Contains("vec3_truth_promotion_is_not_final_truth_without_manual_review", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include vec3_truth_promotion_is_not_final_truth_without_manual_review."));
        }

        if (result.PromotedCandidateCount > 0)
        {
            var recommended = result.PromotedCandidates.First().CandidateId;
            if (!string.Equals(result.RecommendedManualReviewCandidateId, recommended, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("recommended_manual_review_candidate_invalid", "recommended_manual_review_candidate_id must match the first promoted candidate."));
            }
        }

        ValidateCandidates(result.PromotedCandidates, expectedPromoted: true, issues);
        ValidateCandidates(result.BlockedCandidates, expectedPromoted: false, issues);
    }

    private static void ValidateCandidates(
        IReadOnlyList<Vec3PromotedTruthCandidate> candidates,
        bool expectedPromoted,
        ICollection<Vec3TruthPromotionVerificationIssue> issues)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            Require(candidate.CandidateId, "candidate_id_missing", "candidate_id is required.", issues, index);
            Require(candidate.SourceRecoveredCandidateId, "source_recovered_candidate_id_missing", "source_recovered_candidate_id is required.", issues, index);
            Require(candidate.BaseAddressHex, "candidate_base_address_missing", "base_address_hex is required.", issues, index);
            Require(candidate.OffsetHex, "candidate_offset_missing", "offset_hex is required.", issues, index);
            Require(candidate.XOffsetHex, "candidate_x_offset_missing", "x_offset_hex is required.", issues, index);
            Require(candidate.YOffsetHex, "candidate_y_offset_missing", "y_offset_hex is required.", issues, index);
            Require(candidate.ZOffsetHex, "candidate_z_offset_missing", "z_offset_hex is required.", issues, index);
            Require(candidate.DataType, "candidate_data_type_missing", "data_type is required.", issues, index);
            Require(candidate.Classification, "candidate_classification_missing", "classification is required.", issues, index);
            Require(candidate.PromotionStatus, "candidate_promotion_status_missing", "promotion_status is required.", issues, index);
            Require(candidate.TruthReadiness, "candidate_truth_readiness_missing", "truth_readiness is required.", issues, index);
            Require(candidate.ClaimLevel, "candidate_claim_level_missing", "claim_level is required.", issues, index);
            Require(candidate.CorroborationStatus, "candidate_corroboration_status_missing", "corroboration_status is required.", issues, index);
            Require(candidate.EvidenceSummary, "candidate_evidence_summary_missing", "evidence_summary is required.", issues, index);
            Require(candidate.NextValidationStep, "candidate_next_validation_step_missing", "next_validation_step is required.", issues, index);
            Require(candidate.Warning, "candidate_warning_missing", "warning is required.", issues, index);

            ValidateHex(candidate.BaseAddressHex, "candidate_base_address_invalid", "base_address_hex must be hexadecimal, for example 0x1000.", issues, index);
            ValidateHex(candidate.OffsetHex, "candidate_offset_invalid", "offset_hex must be hexadecimal, for example 0x4.", issues, index);
            ValidateHex(candidate.XOffsetHex, "candidate_x_offset_invalid", "x_offset_hex must be hexadecimal, for example 0x4.", issues, index);
            ValidateHex(candidate.YOffsetHex, "candidate_y_offset_invalid", "y_offset_hex must be hexadecimal, for example 0x8.", issues, index);
            ValidateHex(candidate.ZOffsetHex, "candidate_z_offset_invalid", "z_offset_hex must be hexadecimal, for example 0xC.", issues, index);

            if (!string.Equals(candidate.DataType, "vec3_float32", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_data_type_invalid", "promoted coordinate candidates must use data_type=vec3_float32.", index));
            }

            if (expectedPromoted && !string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_promotion_status_invalid", "promoted_candidates must use promotion_status=corroborated_candidate.", index));
            }

            if (expectedPromoted && !string.Equals(candidate.TruthReadiness, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_truth_readiness_invalid", "promoted_candidates must use truth_readiness=corroborated_candidate.", index));
            }

            if (expectedPromoted && !string.Equals(candidate.CorroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_corroboration_status_invalid", "promoted_candidates must use corroboration_status=corroborated.", index));
            }

            if (!expectedPromoted &&
                !string.Equals(candidate.PromotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate.PromotionStatus, "recovered_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_blocked_status_invalid", "blocked_candidates must use promotion_status=blocked_conflict or recovered_candidate.", index));
            }

            if (candidate.SupportingFileCount < 2)
            {
                issues.Add(Error("candidate_supporting_file_count_too_low", "promoted candidates require supporting_file_count >= 2.", index));
            }

            if (candidate.SupportingTruthCandidateIds.Count < 2)
            {
                issues.Add(Error("candidate_supporting_ids_too_few", "promoted candidates require at least two supporting truth candidate IDs.", index));
            }

            if (candidate.BestScoreTotal is < 0 or > 100 || double.IsNaN(candidate.BestScoreTotal) || double.IsInfinity(candidate.BestScoreTotal))
            {
                issues.Add(Error("candidate_score_invalid", "best_score_total must be finite and between 0 and 100.", index));
            }

            ValidateFinite(candidate.AddonObservedX, "addon_observed_x_invalid", "addon_observed_x must be finite when provided.", issues, index);
            ValidateFinite(candidate.AddonObservedY, "addon_observed_y_invalid", "addon_observed_y must be finite when provided.", issues, index);
            ValidateFinite(candidate.AddonObservedZ, "addon_observed_z_invalid", "addon_observed_z must be finite when provided.", issues, index);
            if (candidate.Tolerance is { } tolerance && (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0))
            {
                issues.Add(Error("tolerance_invalid", "tolerance must be finite and non-negative when provided.", index));
            }

            if (candidate.ActorYawProximityBytes is < 0)
            {
                issues.Add(Error("actor_yaw_proximity_invalid", "actor_yaw_proximity_bytes must be non-negative when provided.", index));
            }

            if (candidate.ActorYawProximityBytes.HasValue)
            {
                Require(candidate.ActorYawSourceCandidateId ?? string.Empty, "actor_yaw_source_candidate_missing", "actor_yaw_source_candidate_id is required when actor_yaw_proximity_bytes is present.", issues, index);
                Require(candidate.ActorYawBaseAddressHex ?? string.Empty, "actor_yaw_base_address_missing", "actor_yaw_base_address_hex is required when actor_yaw_proximity_bytes is present.", issues, index);
                Require(candidate.ActorYawOffsetHex ?? string.Empty, "actor_yaw_offset_missing", "actor_yaw_offset_hex is required when actor_yaw_proximity_bytes is present.", issues, index);
                ValidateHex(candidate.ActorYawBaseAddressHex ?? string.Empty, "actor_yaw_base_address_invalid", "actor_yaw_base_address_hex must be hexadecimal when provided.", issues, index);
                ValidateHex(candidate.ActorYawOffsetHex ?? string.Empty, "actor_yaw_offset_invalid", "actor_yaw_offset_hex must be hexadecimal when provided.", issues, index);
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<Vec3TruthPromotionVerificationIssue> issues,
        int? candidateIndex = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
    }

    private static void ValidateFinite(
        double? value,
        string code,
        string message,
        ICollection<Vec3TruthPromotionVerificationIssue> issues,
        int? candidateIndex = null)
    {
        if (value is { } number && (double.IsNaN(number) || double.IsInfinity(number)))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
    }

    private static void ValidateHex(
        string value,
        string code,
        string message,
        ICollection<Vec3TruthPromotionVerificationIssue> issues,
        int? candidateIndex = null)
    {
        if (!LooksLikeHex(value))
        {
            issues.Add(Error(code, message, candidateIndex));
        }
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

    private static Vec3TruthPromotionVerificationIssue Error(string code, string message, int? candidateIndex = null) =>
        new()
        {
            Code = code,
            Message = message,
            CandidateIndex = candidateIndex
        };
}
