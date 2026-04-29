using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class Vec3TruthPromotionService
{
    public Vec3TruthPromotionResult Promote(
        string vec3TruthRecoveryPath,
        string vec3TruthCorroborationPath,
        string? actorYawRecoveryPath = null,
        int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vec3TruthRecoveryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(vec3TruthCorroborationPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var recoveryPath = Path.GetFullPath(vec3TruthRecoveryPath);
        var corroborationPath = Path.GetFullPath(vec3TruthCorroborationPath);
        var fullActorYawRecoveryPath = string.IsNullOrWhiteSpace(actorYawRecoveryPath)
            ? null
            : Path.GetFullPath(actorYawRecoveryPath);
        var recovery = ReadRecovery(recoveryPath);
        var corroboration = ReadCorroboration(corroborationPath);
        var actorYawCandidates = fullActorYawRecoveryPath is null
            ? []
            : ReadActorYawCandidates(fullActorYawRecoveryPath);
        var reviewCandidates = recovery.RecoveredCandidates
            .Select(candidate => BuildCandidate(candidate, FindCorroboration(corroboration, candidate), FindNearestActorYaw(candidate, actorYawCandidates)))
            .OrderBy(candidate => PromotionSortRank(candidate.PromotionStatus))
            .ThenBy(candidate => candidate.ActorYawProximityBytes.HasValue ? 0 : 1)
            .ThenBy(candidate => candidate.ActorYawProximityBytes ?? long.MaxValue)
            .ThenByDescending(candidate => candidate.BestScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var promoted = reviewCandidates
            .Where(candidate => string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            .Select((candidate, index) => candidate with { CandidateId = $"vec3-promoted-{index + 1:000000}" })
            .ToArray();
        var blocked = reviewCandidates
            .Where(candidate => !string.Equals(candidate.PromotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase))
            .Select((candidate, index) => candidate with { CandidateId = $"vec3-blocked-{index + 1:000000}" })
            .ToArray();

        return new Vec3TruthPromotionResult
        {
            Success = true,
            RecoveryPath = recoveryPath,
            CorroborationPath = corroborationPath,
            ActorYawRecoveryPath = fullActorYawRecoveryPath,
            RecoveredCandidateCount = recovery.RecoveredCandidateCount,
            PromotedCandidateCount = promoted.Length,
            BlockedCandidateCount = blocked.Length,
            RecommendedManualReviewCandidateId = promoted.FirstOrDefault()?.CandidateId,
            PromotedCandidates = promoted,
            BlockedCandidates = blocked,
            Warnings = BuildWarnings(promoted, blocked, fullActorYawRecoveryPath, actorYawCandidates)
        };
    }

    private static Vec3TruthRecoveryResult ReadRecovery(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Vec3 truth recovery file does not exist.", path);
        }

        var verification = new Vec3TruthRecoveryVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Vec3 truth recovery verification failed: {issues}");
        }

        return JsonSerializer.Deserialize<Vec3TruthRecoveryResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize vec3 truth recovery packet.");
    }

    private static IReadOnlyDictionary<string, Vec3TruthCorroborationEntry> ReadCorroboration(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Vec3 truth corroboration file does not exist.", path);
        }

        var verification = new Vec3TruthCorroborationVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Vec3 truth corroboration verification failed: {issues}");
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Vec3TruthCorroborationEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid vec3 truth corroboration JSONL entry in {path}."))
            .GroupBy(entry => CorroborationKey(entry.BaseAddressHex, entry.OffsetHex, entry.DataType, entry.Classification), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ScalarRecoveredTruthCandidate> ReadActorYawCandidates(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Actor yaw scalar truth recovery file does not exist.", path);
        }

        var verification = new ScalarTruthRecoveryVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Actor yaw scalar truth recovery verification failed: {issues}");
        }

        var recovery = JsonSerializer.Deserialize<ScalarTruthRecoveryResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize actor yaw scalar truth recovery packet.");
        return recovery.RecoveredCandidates
            .Where(candidate => string.Equals(candidate.Classification, "actor_yaw_angle_scalar_candidate", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.BestScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Vec3PromotedTruthCandidate BuildCandidate(
        Vec3RecoveredTruthCandidate recovered,
        Vec3TruthCorroborationEntry? corroboration,
        ActorYawProximity? actorYawProximity)
    {
        var corroborationStatus = corroboration?.CorroborationStatus ?? "uncorroborated";
        var promotionStatus = PromotionStatus(corroborationStatus);
        var supportingReasons = recovered.SupportingReasons
            .Append($"addon_coordinate_corroboration_{corroborationStatus}")
            .Append(actorYawProximity is null ? "actor_yaw_proximity_not_available" : "actor_yaw_proximity_available")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var offset = TryParseHex(recovered.OffsetHex);
        var actorYawSummary = actorYawProximity is null
            ? "actor_yaw_proximity=unavailable"
            : $"actor_yaw_proximity_bytes={actorYawProximity.ProximityBytes};actor_yaw_offset={actorYawProximity.OffsetHex};actor_yaw_candidate={actorYawProximity.SourceCandidateId}";

        return new Vec3PromotedTruthCandidate
        {
            SourceRecoveredCandidateId = recovered.CandidateId,
            BaseAddressHex = recovered.BaseAddressHex,
            OffsetHex = recovered.OffsetHex,
            XOffsetHex = OffsetHex(offset, 0),
            YOffsetHex = OffsetHex(offset, 4),
            ZOffsetHex = OffsetHex(offset, 8),
            DataType = recovered.DataType,
            Classification = recovered.Classification,
            PromotionStatus = promotionStatus,
            TruthReadiness = TruthReadiness(promotionStatus),
            ClaimLevel = ClaimLevel(promotionStatus),
            CorroborationStatus = corroborationStatus,
            CorroborationSources = string.IsNullOrWhiteSpace(corroboration?.Source) ? [] : [corroboration.Source],
            CorroborationSummary = corroboration?.EvidenceSummary ?? string.Empty,
            AddonObservedX = corroboration?.AddonObservedX,
            AddonObservedY = corroboration?.AddonObservedY,
            AddonObservedZ = corroboration?.AddonObservedZ,
            Tolerance = corroboration?.Tolerance,
            SupportingTruthCandidateIds = recovered.SupportingTruthCandidateIds,
            SupportingFileCount = recovered.SupportingFileCount,
            BestScoreTotal = recovered.BestScoreTotal,
            LabelsPresent = recovered.LabelsPresent,
            SupportingReasons = supportingReasons,
            ActorYawSourceCandidateId = actorYawProximity?.SourceCandidateId,
            ActorYawBaseAddressHex = actorYawProximity?.BaseAddressHex,
            ActorYawOffsetHex = actorYawProximity?.OffsetHex,
            ActorYawProximityBytes = actorYawProximity?.ProximityBytes,
            EvidenceSummary = $"{recovered.EvidenceSummary};corroboration_status={corroborationStatus};{actorYawSummary};component_offsets={OffsetHex(offset, 0)},{OffsetHex(offset, 4)},{OffsetHex(offset, 8)}",
            NextValidationStep = NextValidationStep(promotionStatus, actorYawProximity),
            Warning = Warning(promotionStatus)
        };
    }

    private static Vec3TruthCorroborationEntry? FindCorroboration(
        IReadOnlyDictionary<string, Vec3TruthCorroborationEntry> corroboration,
        Vec3RecoveredTruthCandidate candidate)
    {
        var exactKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, candidate.Classification);
        if (corroboration.TryGetValue(exactKey, out var exact))
        {
            return exact;
        }

        var wildcardKey = CorroborationKey(candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType, string.Empty);
        return corroboration.TryGetValue(wildcardKey, out var wildcard) ? wildcard : null;
    }

    private static ActorYawProximity? FindNearestActorYaw(
        Vec3RecoveredTruthCandidate candidate,
        IReadOnlyList<ScalarRecoveredTruthCandidate> actorYawCandidates)
    {
        var candidateOffset = TryParseHex(candidate.OffsetHex);
        if (candidateOffset is null || actorYawCandidates.Count == 0)
        {
            return null;
        }

        return actorYawCandidates
            .Where(actor => string.Equals(actor.BaseAddressHex, candidate.BaseAddressHex, StringComparison.OrdinalIgnoreCase))
            .Select(actor => new
            {
                Actor = actor,
                Offset = TryParseHex(actor.OffsetHex)
            })
            .Where(item => item.Offset is not null)
            .Select(item => new ActorYawProximity(
                item.Actor.CandidateId,
                item.Actor.BaseAddressHex,
                item.Actor.OffsetHex,
                Math.Abs(candidateOffset.Value - item.Offset!.Value)))
            .OrderBy(item => item.ProximityBytes)
            .ThenBy(item => item.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string PromotionStatus(string corroborationStatus)
    {
        if (string.Equals(corroborationStatus, "corroborated", StringComparison.OrdinalIgnoreCase))
        {
            return "corroborated_candidate";
        }

        if (string.Equals(corroborationStatus, "conflicted", StringComparison.OrdinalIgnoreCase))
        {
            return "blocked_conflict";
        }

        return "recovered_candidate";
    }

    private static int PromotionSortRank(string promotionStatus) =>
        promotionStatus.ToLowerInvariant() switch
        {
            "corroborated_candidate" => 0,
            "recovered_candidate" => 1,
            "blocked_conflict" => 2,
            _ => 3
        };

    private static string TruthReadiness(string promotionStatus) =>
        string.Equals(promotionStatus, "corroborated_candidate", StringComparison.OrdinalIgnoreCase)
            ? "corroborated_candidate"
            : promotionStatus;

    private static string ClaimLevel(string promotionStatus) =>
        string.Equals(promotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase)
            ? "blocked_conflict"
            : TruthReadiness(promotionStatus);

    private static string NextValidationStep(string promotionStatus, ActorYawProximity? actorYawProximity) =>
        promotionStatus.ToLowerInvariant() switch
        {
            "corroborated_candidate" when actorYawProximity is not null => "manual_review_addon_corroborated_coordinate_candidate_near_actor_yaw_before_final_truth_claim",
            "corroborated_candidate" => "manual_review_addon_corroborated_coordinate_candidate_before_final_truth_claim",
            "blocked_conflict" => "resolve_addon_coordinate_corroboration_conflict_before_promotion",
            _ => "add_addon_coordinate_corroboration_or_actor_yaw_proximity_evidence"
        };

    private static string Warning(string promotionStatus) =>
        promotionStatus.ToLowerInvariant() switch
        {
            "corroborated_candidate" => "corroborated_coordinate_candidate_requires_manual_review_before_final_truth_claim",
            "blocked_conflict" => "coordinate_candidate_conflicts_with_addon_coordinate_corroboration",
            _ => "recovered_coordinate_candidate_requires_addon_waypoint_review_before_final_truth_claim"
        };

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<Vec3PromotedTruthCandidate> promoted,
        IReadOnlyList<Vec3PromotedTruthCandidate> blocked,
        string? actorYawRecoveryPath,
        IReadOnlyList<ScalarRecoveredTruthCandidate> actorYawCandidates)
    {
        var warnings = new List<string> { "vec3_truth_promotion_is_not_final_truth_without_manual_review" };
        if (promoted.Count == 0)
        {
            warnings.Add("no_vec3_truth_candidates_promoted");
        }

        if (blocked.Any(candidate => string.Equals(candidate.PromotionStatus, "blocked_conflict", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("vec3_truth_promotion_contains_addon_coordinate_conflicts");
        }

        if (blocked.Any(candidate => string.Equals(candidate.PromotionStatus, "recovered_candidate", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("vec3_truth_promotion_contains_uncorroborated_recovered_candidates");
        }

        if (actorYawRecoveryPath is null)
        {
            warnings.Add("vec3_truth_promotion_has_no_actor_yaw_proximity_evidence");
        }
        else if (actorYawCandidates.Count == 0)
        {
            warnings.Add("actor_yaw_recovery_has_no_actor_yaw_candidates");
        }

        return warnings;
    }

    private static long? TryParseHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string OffsetHex(long? offset, int addend) =>
        offset is null ? string.Empty : $"0x{offset.Value + addend:X}";

    private static string CorroborationKey(string baseAddressHex, string offsetHex, string dataType, string classification) =>
        string.Join("|", baseAddressHex, offsetHex, dataType, classification);

    private sealed record ActorYawProximity(string SourceCandidateId, string BaseAddressHex, string OffsetHex, long ProximityBytes);
}
