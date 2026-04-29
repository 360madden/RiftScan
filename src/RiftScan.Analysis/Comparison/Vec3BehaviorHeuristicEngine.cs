namespace RiftScan.Analysis.Comparison;

internal static class Vec3BehaviorHeuristicEngine
{
    private const double StableDeltaMaximum = 0.01;
    private const double MeaningfulMoveDeltaMinimum = 0.01;
    private const double StrongMoveDeltaMinimum = 0.25;

    public static Vec3BehaviorHeuristicResult Score(Vec3CandidateComparison match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var passiveDelta = TryGetDeltaForLabel(match, IsPassiveLabel);
        var moveDelta = TryGetDeltaForLabel(match, IsMoveForwardLabel);
        var scoreBreakdown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var supportingReasons = new List<string>();
        var rejectionReasons = new List<string>();

        var labelScore = ScoreLabels(match, passiveDelta.HasValue, moveDelta.HasValue, supportingReasons, rejectionReasons);
        scoreBreakdown["label_contrast_score"] = labelScore;

        var behaviorScore = ScoreBehaviorContrast(passiveDelta, moveDelta, supportingReasons, rejectionReasons);
        scoreBreakdown["behavior_contrast_score"] = behaviorScore;

        var supportScore = ScoreSnapshotSupport(match, supportingReasons, rejectionReasons);
        scoreBreakdown["snapshot_support_score"] = supportScore;

        var rankScore = ScoreCandidateRank(match, supportingReasons, rejectionReasons);
        scoreBreakdown["cross_session_rank_score"] = rankScore;

        var validationScore = ScoreValidationStatus(match, supportingReasons, rejectionReasons);
        scoreBreakdown["validation_status_score"] = validationScore;

        var noisePenalty = ScoreNoisePenalty(match, passiveDelta, moveDelta, rejectionReasons);
        scoreBreakdown["noise_penalty"] = noisePenalty;

        var rawScore = labelScore + behaviorScore + supportScore + rankScore + validationScore - noisePenalty;
        var scoreTotal = Math.Round(Math.Clamp(rawScore, 0, 100), 3);
        scoreBreakdown["score_total"] = scoreTotal;

        return new Vec3BehaviorHeuristicResult(
            Classification(passiveDelta, moveDelta, scoreTotal),
            scoreTotal,
            ConfidenceLevel(scoreTotal, rejectionReasons),
            scoreBreakdown,
            supportingReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            rejectionReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            NextValidationStep(scoreTotal, rejectionReasons));
    }

    private static double ScoreLabels(
        Vec3CandidateComparison match,
        bool hasPassive,
        bool hasMove,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (hasPassive && hasMove)
        {
            supportingReasons.Add("has_passive_and_move_forward_labels");
            return 15;
        }

        if (string.IsNullOrWhiteSpace(match.SessionAStimulusLabel) || string.IsNullOrWhiteSpace(match.SessionBStimulusLabel))
        {
            rejectionReasons.Add("missing_stimulus_label");
        }
        else
        {
            rejectionReasons.Add("missing_required_passive_move_contrast");
        }

        return 0;
    }

    private static double ScoreBehaviorContrast(
        double? passiveDelta,
        double? moveDelta,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (!passiveDelta.HasValue || !moveDelta.HasValue)
        {
            rejectionReasons.Add("behavior_contrast_not_observable");
            return 0;
        }

        var score = 0.0;
        if (passiveDelta.Value <= StableDeltaMaximum)
        {
            supportingReasons.Add("passive_delta_stable");
            score += 20;
        }
        else
        {
            rejectionReasons.Add("passive_delta_changed");
        }

        if (moveDelta.Value > MeaningfulMoveDeltaMinimum)
        {
            supportingReasons.Add("move_forward_delta_changed");
            score += moveDelta.Value >= StrongMoveDeltaMinimum ? 30 : 20;
        }
        else
        {
            rejectionReasons.Add("move_forward_delta_not_observed");
        }

        return score;
    }

    private static double ScoreSnapshotSupport(
        Vec3CandidateComparison match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var minimumSupport = Math.Min(match.SessionASnapshotSupport, match.SessionBSnapshotSupport);
        if (minimumSupport >= 3)
        {
            supportingReasons.Add("three_or_more_snapshots_support_both_sessions");
            return 15;
        }

        if (minimumSupport >= 2)
        {
            supportingReasons.Add("two_snapshots_support_both_sessions");
            return 10;
        }

        rejectionReasons.Add("insufficient_snapshot_support");
        return 0;
    }

    private static double ScoreCandidateRank(
        Vec3CandidateComparison match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var minimumRank = Math.Min(match.SessionARankScore, match.SessionBRankScore);
        if (minimumRank >= 90)
        {
            supportingReasons.Add("high_rank_in_both_sessions");
            return 15;
        }

        if (minimumRank >= 75)
        {
            supportingReasons.Add("good_rank_in_both_sessions");
            return 10;
        }

        if (minimumRank >= 50)
        {
            supportingReasons.Add("medium_rank_in_both_sessions");
            return 5;
        }

        rejectionReasons.Add("low_rank_in_one_or_more_sessions");
        return 0;
    }

    private static double ScoreValidationStatus(
        Vec3CandidateComparison match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var aConsistent = string.Equals(match.SessionAValidationStatus, "behavior_consistent_candidate", StringComparison.OrdinalIgnoreCase);
        var bConsistent = string.Equals(match.SessionBValidationStatus, "behavior_consistent_candidate", StringComparison.OrdinalIgnoreCase);
        if (aConsistent && bConsistent)
        {
            supportingReasons.Add("behavior_consistent_status_in_both_sessions");
            return 10;
        }

        rejectionReasons.Add("behavior_consistent_status_missing_in_one_or_more_sessions");
        return 0;
    }

    private static double ScoreNoisePenalty(
        Vec3CandidateComparison match,
        double? passiveDelta,
        double? moveDelta,
        ICollection<string> rejectionReasons)
    {
        var penalty = 0.0;
        if (passiveDelta > StableDeltaMaximum)
        {
            penalty += 20;
        }

        if (moveDelta <= MeaningfulMoveDeltaMinimum)
        {
            penalty += 20;
        }

        if (!double.IsFinite(match.SessionAValueDeltaMagnitude) || !double.IsFinite(match.SessionBValueDeltaMagnitude))
        {
            rejectionReasons.Add("non_finite_delta_magnitude");
            penalty += 40;
        }

        return penalty;
    }

    private static double? TryGetDeltaForLabel(Vec3CandidateComparison match, Func<string, bool> labelPredicate)
    {
        if (labelPredicate(match.SessionAStimulusLabel))
        {
            return match.SessionAValueDeltaMagnitude;
        }

        if (labelPredicate(match.SessionBStimulusLabel))
        {
            return match.SessionBValueDeltaMagnitude;
        }

        return null;
    }

    private static string Classification(double? passiveDelta, double? moveDelta, double scoreTotal)
    {
        if (passiveDelta <= StableDeltaMaximum &&
            moveDelta > MeaningfulMoveDeltaMinimum &&
            scoreTotal >= 60)
        {
            return "position_like_vec3_candidate";
        }

        return "unclassified_vec3_behavior_contrast_candidate";
    }

    private static string ConfidenceLevel(double scoreTotal, IReadOnlyCollection<string> rejectionReasons)
    {
        if (rejectionReasons.Count > 0 && scoreTotal < 75)
        {
            return "weak_candidate";
        }

        if (scoreTotal >= 85)
        {
            return "strong_candidate";
        }

        return scoreTotal >= 60 ? "candidate" : "weak_candidate";
    }

    private static string NextValidationStep(double scoreTotal, IReadOnlyCollection<string> rejectionReasons)
    {
        if (scoreTotal >= 75 && rejectionReasons.Count == 0)
        {
            return "validate_against_addon_truth_or_repeat_move_forward_contrast";
        }

        return "capture_additional_labeled_contrast_before_truth_claim";
    }

    private static bool IsPassiveLabel(string label) =>
        string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase);

    private static bool IsMoveForwardLabel(string label) =>
        string.Equals(label, "move_forward", StringComparison.OrdinalIgnoreCase);
}

internal sealed record Vec3BehaviorHeuristicResult(
    string Classification,
    double ScoreTotal,
    string ConfidenceLevel,
    IReadOnlyDictionary<string, double> ScoreBreakdown,
    IReadOnlyList<string> SupportingReasons,
    IReadOnlyList<string> RejectionReasons,
    string NextValidationStep);
