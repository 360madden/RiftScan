using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ComparisonTruthReadinessVerifier
{
    private static readonly HashSet<string> AllowedReadiness = new(StringComparer.OrdinalIgnoreCase)
    {
        "missing",
        "candidate",
        "candidate_needs_labeled_contrast",
        "candidate_needs_camera_only_separation",
        "strong_candidate"
    };

    public ComparisonTruthReadinessVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ComparisonTruthReadinessVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Comparison truth-readiness file does not exist."));
            return new ComparisonTruthReadinessVerificationResult { Path = fullPath, Issues = issues };
        }

        ComparisonTruthReadinessResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ComparisonTruthReadinessResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid comparison truth-readiness JSON: {ex.Message}"));
            return new ComparisonTruthReadinessVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Comparison truth-readiness JSON did not contain an object."));
            return new ComparisonTruthReadinessVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new ComparisonTruthReadinessVerificationResult { Path = fullPath, Issues = issues };
    }

    private static void ValidateResult(
        ComparisonTruthReadinessResult result,
        ICollection<ComparisonTruthReadinessVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.comparison_truth_readiness.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.comparison_truth_readiness.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable readiness packet."));
        }

        Require(result.SessionAId, "session_a_id_missing", "session_a_id is required.", issues);
        Require(result.SessionBId, "session_b_id_missing", "session_b_id is required.", issues);
        ValidateStatus("entity_layout", result.EntityLayout, issues);
        ValidateStatus("position", result.Position, issues);
        ValidateStatus("actor_yaw", result.ActorYaw, issues);
        ValidateStatus("camera_orientation", result.CameraOrientation, issues);
        ValidateNextCapture(result.NextRequiredCapture, issues);

        if (!result.Warnings.Contains("truth_readiness_is_candidate_evidence_not_truth_claim", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include truth_readiness_is_candidate_evidence_not_truth_claim."));
        }

        if (result.EntityLayout.Readiness == "missing" &&
            result.Position.Readiness == "missing" &&
            result.ActorYaw.Readiness == "missing" &&
            result.CameraOrientation.Readiness == "missing")
        {
            issues.Add(Error("no_readiness_evidence", "At least one readiness component must contain candidate evidence."));
        }
    }

    private static void ValidateStatus(
        string expectedComponent,
        ComparisonTruthReadinessStatus status,
        ICollection<ComparisonTruthReadinessVerificationIssue> issues)
    {
        if (!string.Equals(status.Component, expectedComponent, StringComparison.Ordinal))
        {
            issues.Add(Error($"{expectedComponent}_component_invalid", $"{expectedComponent}.component must be {expectedComponent}."));
        }

        Require(status.Readiness, $"{expectedComponent}_readiness_missing", $"{expectedComponent}.readiness is required.", issues);
        if (!string.IsNullOrWhiteSpace(status.Readiness) && !AllowedReadiness.Contains(status.Readiness))
        {
            issues.Add(Error($"{expectedComponent}_readiness_invalid", $"{expectedComponent}.readiness is not an allowed comparison readiness value."));
        }

        if (status.ConfidenceScore < 0 || status.ConfidenceScore > 100)
        {
            issues.Add(Error($"{expectedComponent}_confidence_score_invalid", $"{expectedComponent}.confidence_score must be between 0 and 100."));
        }

        if (status.EvidenceCount < 0)
        {
            issues.Add(Error($"{expectedComponent}_evidence_count_invalid", $"{expectedComponent}.evidence_count must not be negative."));
        }

        if (string.Equals(status.Readiness, "missing", StringComparison.OrdinalIgnoreCase) && status.BlockingGaps.Count == 0)
        {
            issues.Add(Error($"{expectedComponent}_blocking_gap_missing", $"{expectedComponent} missing readiness must include at least one blocking gap."));
        }

        if (!string.Equals(status.Readiness, "missing", StringComparison.OrdinalIgnoreCase) && status.EvidenceCount == 0)
        {
            issues.Add(Error($"{expectedComponent}_evidence_count_missing", $"{expectedComponent} non-missing readiness must include evidence_count > 0."));
        }

        Require(status.PrimaryReason, $"{expectedComponent}_primary_reason_missing", $"{expectedComponent}.primary_reason is required.", issues);
        Require(status.NextAction, $"{expectedComponent}_next_action_missing", $"{expectedComponent}.next_action is required.", issues);
    }

    private static void ValidateNextCapture(
        ComparisonTruthReadinessCaptureRequirement nextCapture,
        ICollection<ComparisonTruthReadinessVerificationIssue> issues)
    {
        Require(nextCapture.Mode, "next_required_capture_mode_missing", "next_required_capture.mode is required.", issues);
        Require(nextCapture.Reason, "next_required_capture_reason_missing", "next_required_capture.reason is required.", issues);
        Require(nextCapture.ExpectedSignal, "next_required_capture_expected_signal_missing", "next_required_capture.expected_signal is required.", issues);
        Require(nextCapture.StopCondition, "next_required_capture_stop_condition_missing", "next_required_capture.stop_condition is required.", issues);
        if (nextCapture.TargetCount < 0)
        {
            issues.Add(Error("next_required_capture_target_count_invalid", "next_required_capture.target_count must not be negative."));
        }

        foreach (var target in nextCapture.TargetPreview)
        {
            if (!LooksLikeHex(target.BaseAddressHex))
            {
                issues.Add(Error("target_base_address_invalid", "target_preview.base_address_hex must be hexadecimal, for example 0x1000."));
            }

            if (!LooksLikeHex(target.OffsetHex))
            {
                issues.Add(Error("target_offset_invalid", "target_preview.offset_hex must be hexadecimal, for example 0x4."));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<ComparisonTruthReadinessVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
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

    private static ComparisonTruthReadinessVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message
        };
}
