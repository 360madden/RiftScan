using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthRecoveryVerifier
{
    public ScalarTruthRecoveryVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ScalarTruthRecoveryVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Scalar truth recovery file does not exist."));
            return new ScalarTruthRecoveryVerificationResult { Path = fullPath, Issues = issues };
        }

        ScalarTruthRecoveryResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ScalarTruthRecoveryResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid scalar truth recovery JSON: {ex.Message}"));
            return new ScalarTruthRecoveryVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Scalar truth recovery JSON did not contain an object."));
            return new ScalarTruthRecoveryVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new ScalarTruthRecoveryVerificationResult
        {
            Path = fullPath,
            TruthCandidatePathCount = result.TruthCandidatePaths.Count,
            InputCandidateCount = result.InputCandidateCount,
            RecoveredCandidateCount = result.RecoveredCandidateCount,
            Issues = issues
        };
    }

    private static void ValidateResult(ScalarTruthRecoveryResult result, ICollection<ScalarTruthRecoveryVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.scalar_truth_recovery.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.scalar_truth_recovery.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable scalar truth recovery packet."));
        }

        if (result.TruthCandidatePaths.Count < 2)
        {
            issues.Add(Error("truth_candidate_path_count_too_low", "truth_candidate_paths must include at least two files."));
        }

        if (result.InputCandidateCount < result.RecoveredCandidateCount)
        {
            issues.Add(Error("input_candidate_count_invalid", "input_candidate_count must be greater than or equal to recovered_candidate_count."));
        }

        if (result.RecoveredCandidateCount != result.RecoveredCandidates.Count)
        {
            issues.Add(Error("recovered_candidate_count_mismatch", "recovered_candidate_count must match recovered_candidates length."));
        }

        if (!result.Warnings.Contains("scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include scalar_recovery_is_repeated_candidate_evidence_not_unconditional_truth."));
        }

        ValidateCandidates(result.RecoveredCandidates, issues);
    }

    private static void ValidateCandidates(
        IReadOnlyList<ScalarRecoveredTruthCandidate> candidates,
        ICollection<ScalarTruthRecoveryVerificationIssue> issues)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            Require(candidate.CandidateId, "candidate_id_missing", "recovered_candidates.candidate_id is required.", issues, index);
            Require(candidate.BaseAddressHex, "candidate_base_address_missing", "recovered_candidates.base_address_hex is required.", issues, index);
            Require(candidate.OffsetHex, "candidate_offset_missing", "recovered_candidates.offset_hex is required.", issues, index);
            Require(candidate.DataType, "candidate_data_type_missing", "recovered_candidates.data_type is required.", issues, index);
            Require(candidate.Classification, "candidate_classification_missing", "recovered_candidates.classification is required.", issues, index);
            Require(candidate.TruthReadiness, "candidate_truth_readiness_missing", "recovered_candidates.truth_readiness is required.", issues, index);
            Require(candidate.ClaimLevel, "candidate_claim_level_missing", "recovered_candidates.claim_level is required.", issues, index);
            Require(candidate.EvidenceSummary, "candidate_evidence_summary_missing", "recovered_candidates.evidence_summary is required.", issues, index);
            Require(candidate.Warning, "candidate_warning_missing", "recovered_candidates.warning is required.", issues, index);

            if (!LooksLikeHex(candidate.BaseAddressHex))
            {
                issues.Add(Error("candidate_base_address_invalid", "recovered_candidates.base_address_hex must be hexadecimal, for example 0x1000.", index));
            }

            if (!LooksLikeHex(candidate.OffsetHex))
            {
                issues.Add(Error("candidate_offset_invalid", "recovered_candidates.offset_hex must be hexadecimal, for example 0x4.", index));
            }

            if (!string.Equals(candidate.TruthReadiness, "recovered_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_truth_readiness_invalid", "recovered candidates must use truth_readiness=recovered_candidate.", index));
            }

            if (!string.Equals(candidate.ClaimLevel, "recovered_candidate", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_claim_level_invalid", "recovered candidates must use claim_level=recovered_candidate.", index));
            }

            if (candidate.SupportingFileCount < 2)
            {
                issues.Add(Error("candidate_supporting_file_count_too_low", "recovered candidates require supporting_file_count >= 2.", index));
            }

            if (candidate.SupportingTruthCandidateIds.Count < 2)
            {
                issues.Add(Error("candidate_supporting_ids_too_few", "recovered candidates require at least two supporting truth candidate IDs.", index));
            }

            if (candidate.BestScoreTotal is < 0 or > 100 || double.IsNaN(candidate.BestScoreTotal) || double.IsInfinity(candidate.BestScoreTotal))
            {
                issues.Add(Error("candidate_score_invalid", "recovered_candidates.best_score_total must be finite and between 0 and 100.", index));
            }

            if (!candidate.SupportingReasons.Contains("repeated_truth_candidate_match", StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(Error("candidate_repeated_match_reason_missing", "recovered candidates must include repeated_truth_candidate_match support.", index));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<ScalarTruthRecoveryVerificationIssue> issues,
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

    private static ScalarTruthRecoveryVerificationIssue Error(string code, string message, int? candidateIndex = null) =>
        new()
        {
            Code = code,
            Message = message,
            CandidateIndex = candidateIndex
        };
}
