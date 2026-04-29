using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Validation;

public sealed class RiftPromotedCoordinateLiveVerificationVerifier
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "live_memory_and_addon_coordinate_matched_candidate",
        "live_memory_addon_coordinate_mismatch",
        "verification_incomplete"
    };

    public RiftPromotedCoordinateLiveVerificationVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftPromotedCoordinateLiveVerificationVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "RIFT promoted coordinate live verification file does not exist.", fullPath));
            return new RiftPromotedCoordinateLiveVerificationVerificationResult { Path = fullPath, Issues = issues };
        }

        RiftPromotedCoordinateLiveVerificationResult? result;
        try
        {
            result = JsonSerializer.Deserialize<RiftPromotedCoordinateLiveVerificationResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid RIFT promoted coordinate live verification JSON: {ex.Message}", fullPath));
            return new RiftPromotedCoordinateLiveVerificationVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "RIFT promoted coordinate live verification JSON did not contain an object.", fullPath));
            return new RiftPromotedCoordinateLiveVerificationVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new RiftPromotedCoordinateLiveVerificationVerificationResult
        {
            Path = fullPath,
            ValidationStatus = result.ValidationStatus,
            CandidateId = result.CandidateId,
            MaxAbsDistance = result.MaxAbsDistance,
            Tolerance = result.Tolerance,
            Issues = issues
        };
    }

    private static void ValidateResult(
        RiftPromotedCoordinateLiveVerificationResult result,
        ICollection<RiftPromotedCoordinateLiveVerificationVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.rift_promoted_coordinate_live_verification.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.rift_promoted_coordinate_live_verification.v1."));
        }

        Require(result.PromotionPath, "promotion_path_missing", "promotion_path is required.", issues);
        Require(result.SavedVariablesPathRedacted, "savedvariables_path_redacted_missing", "savedvariables_path_redacted is required.", issues);
        Require(result.CandidateId, "candidate_id_missing", "candidate_id is required.", issues);
        Require(result.SourceRecoveredCandidateId, "source_recovered_candidate_id_missing", "source_recovered_candidate_id is required.", issues);
        Require(result.BaseAddressHex, "base_address_missing", "base_address_hex is required.", issues);
        Require(result.OffsetHex, "offset_missing", "offset_hex is required.", issues);
        Require(result.AbsoluteAddressHex, "absolute_address_missing", "absolute_address_hex is required.", issues);
        Require(result.XOffsetHex, "x_offset_missing", "x_offset_hex is required.", issues);
        Require(result.YOffsetHex, "y_offset_missing", "y_offset_hex is required.", issues);
        Require(result.ZOffsetHex, "z_offset_missing", "z_offset_hex is required.", issues);
        Require(result.ValidationStatus, "validation_status_missing", "validation_status is required.", issues);
        Require(result.ClaimLevel, "claim_level_missing", "claim_level is required.", issues);
        Require(result.EvidenceSummary, "evidence_summary_missing", "evidence_summary is required.", issues);

        ValidateHex(result.BaseAddressHex, "base_address_invalid", "base_address_hex must be hexadecimal, for example 0x1000.", issues);
        ValidateHex(result.OffsetHex, "offset_invalid", "offset_hex must be hexadecimal, for example 0x4.", issues);
        ValidateHex(result.AbsoluteAddressHex, "absolute_address_invalid", "absolute_address_hex must be hexadecimal, for example 0x1004.", issues);
        ValidateHex(result.XOffsetHex, "x_offset_invalid", "x_offset_hex must be hexadecimal, for example 0x4.", issues);
        ValidateHex(result.YOffsetHex, "y_offset_invalid", "y_offset_hex must be hexadecimal, for example 0x8.", issues);
        ValidateHex(result.ZOffsetHex, "z_offset_invalid", "z_offset_hex must be hexadecimal, for example 0xC.", issues);

        if (result.ProcessId <= 0)
        {
            issues.Add(Error("process_id_invalid", "process_id must be positive."));
        }

        if (!AllowedStatuses.Contains(result.ValidationStatus))
        {
            issues.Add(Error("validation_status_invalid", "validation_status is not recognized."));
        }

        if (result.Tolerance < 0 || double.IsNaN(result.Tolerance) || double.IsInfinity(result.Tolerance))
        {
            issues.Add(Error("tolerance_invalid", "tolerance must be finite and non-negative."));
        }

        ValidateFinite(result.MemoryX, "memory_x_invalid", "memory_x must be finite when provided.", issues);
        ValidateFinite(result.MemoryY, "memory_y_invalid", "memory_y must be finite when provided.", issues);
        ValidateFinite(result.MemoryZ, "memory_z_invalid", "memory_z must be finite when provided.", issues);
        ValidateFinite(result.AddonObservedX, "addon_observed_x_invalid", "addon_observed_x must be finite when provided.", issues);
        ValidateFinite(result.AddonObservedY, "addon_observed_y_invalid", "addon_observed_y must be finite when provided.", issues);
        ValidateFinite(result.AddonObservedZ, "addon_observed_z_invalid", "addon_observed_z must be finite when provided.", issues);
        ValidateFinite(result.MaxAbsDistance, "max_abs_distance_invalid", "max_abs_distance must be finite when provided.", issues);

        if (!result.Warnings.Contains("rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review."));
        }

        if (string.Equals(result.ValidationStatus, "live_memory_and_addon_coordinate_matched_candidate", StringComparison.OrdinalIgnoreCase))
        {
            if (result.MaxAbsDistance is null || result.MaxAbsDistance > result.Tolerance)
            {
                issues.Add(Error("matched_status_distance_invalid", "matched validation requires max_abs_distance <= tolerance."));
            }

            if (result.MemoryX is null || result.MemoryY is null || result.MemoryZ is null ||
                result.AddonObservedX is null || result.AddonObservedY is null || result.AddonObservedZ is null)
            {
                issues.Add(Error("matched_status_values_missing", "matched validation requires memory and addon coordinate values."));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<RiftPromotedCoordinateLiveVerificationVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message));
        }
    }

    private static void ValidateFinite(
        double? value,
        string code,
        string message,
        ICollection<RiftPromotedCoordinateLiveVerificationVerificationIssue> issues)
    {
        if (value is { } number && (double.IsNaN(number) || double.IsInfinity(number)))
        {
            issues.Add(Error(code, message));
        }
    }

    private static void ValidateHex(
        string value,
        string code,
        string message,
        ICollection<RiftPromotedCoordinateLiveVerificationVerificationIssue> issues)
    {
        if (!LooksLikeHex(value))
        {
            issues.Add(Error(code, message));
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

    private static RiftPromotedCoordinateLiveVerificationVerificationIssue Error(string code, string message, string? path = null) =>
        new()
        {
            Code = code,
            Message = message,
            Path = path
        };
}
