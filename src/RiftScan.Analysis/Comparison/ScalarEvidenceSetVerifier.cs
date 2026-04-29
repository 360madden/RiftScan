using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarEvidenceSetVerifier
{
    private static readonly HashSet<string> AllowedTruthReadiness = new(StringComparer.OrdinalIgnoreCase)
    {
        "insufficient",
        "candidate",
        "strong_candidate",
        "validated_candidate"
    };

    private static readonly HashSet<string> AllowedConfidence = new(StringComparer.OrdinalIgnoreCase)
    {
        "weak_candidate",
        "candidate",
        "strong_candidate",
        "validated_candidate"
    };

    public ScalarEvidenceSetVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ScalarEvidenceSetVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Scalar evidence set file does not exist."));
            return new ScalarEvidenceSetVerificationResult { Path = fullPath, Issues = issues };
        }

        ScalarEvidenceSetResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ScalarEvidenceSetResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid scalar evidence set JSON: {ex.Message}"));
            return new ScalarEvidenceSetVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Scalar evidence set JSON did not contain an object."));
            return new ScalarEvidenceSetVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new ScalarEvidenceSetVerificationResult
        {
            Path = fullPath,
            SessionCount = result.SessionCount,
            RankedCandidateCount = result.RankedCandidateCount,
            RejectedSummaryCount = result.RejectedCandidateSummaries.Count,
            Issues = issues
        };
    }

    private static void ValidateResult(ScalarEvidenceSetResult result, ICollection<ScalarEvidenceSetVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.scalar_evidence_set.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.scalar_evidence_set.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable scalar evidence set."));
        }

        if (result.SessionCount < 2)
        {
            issues.Add(Error("session_count_too_low", "session_count must be at least 2."));
        }

        if (result.SessionPaths.Count != result.SessionCount)
        {
            issues.Add(Error("session_paths_count_mismatch", "session_paths length must match session_count."));
        }

        if (result.SessionSummaries.Count != result.SessionCount)
        {
            issues.Add(Error("session_summaries_count_mismatch", "session_summaries length must match session_count."));
        }

        if (result.RankedCandidateCount != result.RankedCandidates.Count)
        {
            issues.Add(Error("ranked_candidate_count_mismatch", "ranked_candidate_count must match ranked_candidates length."));
        }

        if (!result.Warnings.Contains("scalar_evidence_is_candidate_evidence_not_truth_claim", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include scalar_evidence_is_candidate_evidence_not_truth_claim."));
        }

        ValidateSessionSummaries(result.SessionSummaries, issues);
        ValidateCandidates(result.RankedCandidates, issues);
        ValidateRejectedSummaries(result.RejectedCandidateSummaries, issues);
    }

    private static void ValidateSessionSummaries(
        IReadOnlyList<ScalarEvidenceSessionSummary> summaries,
        ICollection<ScalarEvidenceSetVerificationIssue> issues)
    {
        foreach (var summary in summaries)
        {
            Require(summary.SessionId, "session_id_missing", "session_summaries.session_id is required.", issues);
            Require(summary.SessionPath, "session_path_missing", "session_summaries.session_path is required.", issues);
            if (summary.ScalarCandidateCount < 0)
            {
                issues.Add(Error("session_scalar_candidate_count_invalid", "session_summaries.scalar_candidate_count must not be negative."));
            }
        }
    }

    private static void ValidateCandidates(
        IReadOnlyList<ScalarEvidenceAggregateCandidate> candidates,
        ICollection<ScalarEvidenceSetVerificationIssue> issues)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            Require(candidate.Classification, "candidate_classification_missing", "ranked_candidates.classification is required.", issues, index);
            Require(candidate.BaseAddressHex, "candidate_base_address_missing", "ranked_candidates.base_address_hex is required.", issues, index);
            Require(candidate.OffsetHex, "candidate_offset_missing", "ranked_candidates.offset_hex is required.", issues, index);
            Require(candidate.DataType, "candidate_data_type_missing", "ranked_candidates.data_type is required.", issues, index);
            Require(candidate.TruthReadiness, "candidate_truth_readiness_missing", "ranked_candidates.truth_readiness is required.", issues, index);
            Require(candidate.ConfidenceLevel, "candidate_confidence_missing", "ranked_candidates.confidence_level is required.", issues, index);
            Require(candidate.EvidenceSummary, "candidate_evidence_summary_missing", "ranked_candidates.evidence_summary is required.", issues, index);
            Require(candidate.NextValidationStep, "candidate_next_validation_step_missing", "ranked_candidates.next_validation_step is required.", issues, index);

            if (!LooksLikeHex(candidate.BaseAddressHex))
            {
                issues.Add(Error("candidate_base_address_invalid", "ranked_candidates.base_address_hex must be hexadecimal, for example 0x1000.", index));
            }

            if (!LooksLikeHex(candidate.OffsetHex))
            {
                issues.Add(Error("candidate_offset_invalid", "ranked_candidates.offset_hex must be hexadecimal, for example 0x4.", index));
            }

            if (candidate.ScoreTotal is < 0 or > 100 || double.IsNaN(candidate.ScoreTotal) || double.IsInfinity(candidate.ScoreTotal))
            {
                issues.Add(Error("candidate_score_invalid", "ranked_candidates.score_total must be finite and between 0 and 100.", index));
            }

            if (!string.IsNullOrWhiteSpace(candidate.TruthReadiness) && !AllowedTruthReadiness.Contains(candidate.TruthReadiness))
            {
                issues.Add(Error("candidate_truth_readiness_invalid", "ranked_candidates.truth_readiness is not an allowed readiness value.", index));
            }

            if (!string.IsNullOrWhiteSpace(candidate.ConfidenceLevel) && !AllowedConfidence.Contains(candidate.ConfidenceLevel))
            {
                issues.Add(Error("candidate_confidence_invalid", "ranked_candidates.confidence_level is not an allowed confidence value.", index));
            }

            ValidateValidatedCandidate(candidate, issues, index);
        }
    }

    private static void ValidateValidatedCandidate(
        ScalarEvidenceAggregateCandidate candidate,
        ICollection<ScalarEvidenceSetVerificationIssue> issues,
        int index)
    {
        if (!string.Equals(candidate.TruthReadiness, "validated_candidate", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!candidate.PassiveStable)
        {
            issues.Add(Error("validated_candidate_missing_passive_stability", "validated scalar candidates require passive_stable=true.", index));
        }

        if (candidate.Classification.Contains("actor_yaw", StringComparison.OrdinalIgnoreCase))
        {
            if (!candidate.OppositeTurnPolarity)
            {
                issues.Add(Error("validated_actor_yaw_missing_opposite_turn_polarity", "validated actor_yaw candidates require opposite_turn_polarity=true.", index));
            }

            if (!string.Equals(candidate.CameraTurnSeparation, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("validated_actor_yaw_missing_camera_separation", "validated actor_yaw candidates require turn_changes_camera_only_stable separation.", index));
            }
        }

        if (candidate.Classification.Contains("camera_orientation", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate.CameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("validated_camera_missing_turn_separation", "validated camera_orientation candidates require camera_only_changes_turn_stable separation.", index));
        }
    }

    private static void ValidateRejectedSummaries(
        IReadOnlyList<ScalarEvidenceRejectedSummary> summaries,
        ICollection<ScalarEvidenceSetVerificationIssue> issues)
    {
        foreach (var summary in summaries)
        {
            Require(summary.Reason, "rejected_reason_missing", "rejected_candidate_summaries.reason is required.", issues);
            if (summary.Count < 0)
            {
                issues.Add(Error("rejected_count_invalid", "rejected_candidate_summaries.count must not be negative."));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<ScalarEvidenceSetVerificationIssue> issues,
        int? candidateIndex = null)
    {
        if (string.IsNullOrWhiteSpace(value))
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

    private static ScalarEvidenceSetVerificationIssue Error(string code, string message, int? candidateIndex = null) =>
        new()
        {
            Code = code,
            Message = message,
            CandidateIndex = candidateIndex
        };
}
