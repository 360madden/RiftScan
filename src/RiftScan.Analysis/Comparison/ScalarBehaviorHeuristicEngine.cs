namespace RiftScan.Analysis.Comparison;

internal static class ScalarBehaviorHeuristicEngine
{
    public static ScalarBehaviorHeuristicResult Score(ValueCandidateComparison match) =>
        Score(ScalarBehaviorEvidence.FromValueMatch(match));

    public static ScalarBehaviorHeuristicResult Score(ScalarBehaviorEvidence match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var scoreBreakdown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var supportingReasons = new List<string>();
        var rejectionReasons = new List<string>();

        var labelScore = ScoreLabels(match, supportingReasons, rejectionReasons);
        scoreBreakdown["label_signal_score"] = labelScore;

        var typeScore = ScoreDataType(match, supportingReasons, rejectionReasons);
        scoreBreakdown["data_type_score"] = typeScore;

        var behaviorScore = ScoreBehavior(match, supportingReasons, rejectionReasons);
        scoreBreakdown["behavior_change_score"] = behaviorScore;

        var angleShapeScore = ScoreAngleShape(match, supportingReasons, rejectionReasons);
        scoreBreakdown["angle_shape_score"] = angleShapeScore;

        var turnPolarityScore = ScoreTurnPolarity(match, supportingReasons, rejectionReasons);
        scoreBreakdown["turn_polarity_score"] = turnPolarityScore;

        var cameraSeparationScore = ScoreCameraSeparation(match, supportingReasons, rejectionReasons);
        scoreBreakdown["camera_turn_separation_score"] = cameraSeparationScore;

        var rankScore = ScoreRank(match, supportingReasons, rejectionReasons);
        scoreBreakdown["cross_session_rank_score"] = rankScore;

        var noisePenalty = ScoreNoisePenalty(match, rejectionReasons);
        scoreBreakdown["noise_penalty"] = noisePenalty;

        var rawScore = labelScore + typeScore + behaviorScore + angleShapeScore + turnPolarityScore + cameraSeparationScore + rankScore - noisePenalty;
        var scoreTotal = Math.Round(Math.Clamp(rawScore, 0, 100), 3);
        scoreBreakdown["score_total"] = scoreTotal;

        return new ScalarBehaviorHeuristicResult(
            Classification(match, scoreTotal),
            scoreTotal,
            ConfidenceLevel(scoreTotal, rejectionReasons),
            scoreBreakdown,
            supportingReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            rejectionReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            NextValidationStep(match, scoreTotal));
    }

    private static double ScoreLabels(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var hasTurnLabel = HasTurnLabel(match);
        var hasCameraOnlyLabel = HasCameraOnlyLabel(match);
        if (hasTurnLabel)
        {
            supportingReasons.Add("turn_stimulus_label_present");
        }

        if (hasCameraOnlyLabel)
        {
            supportingReasons.Add("camera_only_stimulus_label_present");
        }

        if (hasTurnLabel && hasCameraOnlyLabel)
        {
            return 25;
        }

        if (hasTurnLabel || hasCameraOnlyLabel)
        {
            return 20;
        }

        rejectionReasons.Add("no_turn_or_camera_stimulus_label");
        return 0;
    }

    private static double ScoreDataType(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (string.Equals(match.DataType, "float32", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("float32_scalar_lane");
            return 25;
        }

        if (string.Equals(match.DataType, "int32", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("int32_scalar_lane");
            return 10;
        }

        rejectionReasons.Add("raw_uint32_lane_is_weak_for_orientation");
        return 0;
    }

    private static double ScoreBehavior(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var score = 0.0;
        if (ChangedUnderTurnOrCamera(match))
        {
            supportingReasons.Add("scalar_changed_under_turn_or_camera_label");
            score += 25;
        }
        else
        {
            rejectionReasons.Add("no_scalar_change_under_turn_or_camera_label");
        }

        if (PassiveChanged(match))
        {
            rejectionReasons.Add("passive_session_also_changed_scalar");
            score -= 10;
        }

        return Math.Max(0, score);
    }

    private static double ScoreRank(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        var minimumRank = Math.Min(match.SessionARankScore, match.SessionBRankScore);
        if (minimumRank >= 75)
        {
            supportingReasons.Add("high_rank_typed_value_match");
            return 20;
        }

        if (minimumRank >= 50)
        {
            supportingReasons.Add("medium_rank_typed_value_match");
            return 10;
        }

        rejectionReasons.Add("low_rank_typed_value_match");
        return 0;
    }

    private static double ScoreAngleShape(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (IsRadiansFamily(match.ValueFamily))
        {
            supportingReasons.Add("angle_radian_value_range");
            return HasDirectionalTurnOrCameraMovement(match, supportingReasons) ? 15 : 10;
        }

        if (IsDegreesFamily(match.ValueFamily))
        {
            supportingReasons.Add("angle_degree_value_range");
            return HasDirectionalTurnOrCameraMovement(match, supportingReasons) ? 12 : 8;
        }

        if (string.Equals(match.ValueFamily, "normalized_unit_scalar", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("normalized_unit_scalar_value_range");
            return 3;
        }

        rejectionReasons.Add("no_angle_value_range_evidence");
        return 0;
    }

    private static double ScoreNoisePenalty(ScalarBehaviorEvidence match, ICollection<string> rejectionReasons)
    {
        var penalty = 0.0;
        if (string.Equals(match.DataType, "uint32", StringComparison.OrdinalIgnoreCase))
        {
            penalty += 25;
        }

        if (PassiveChanged(match))
        {
            penalty += 15;
        }

        if (penalty > 0)
        {
            rejectionReasons.Add("scalar_noise_penalty_applied");
        }

        return penalty;
    }

    private static double ScoreTurnPolarity(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (!HasOppositeTurnLabels(match))
        {
            return 0;
        }

        if (string.Equals(match.TurnPolarityRelationship, "opposite_signed_turn_directions", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("opposite_turn_polarity_supported");
            return 15;
        }

        if (string.Equals(match.TurnPolarityRelationship, "same_signed_turn_directions", StringComparison.OrdinalIgnoreCase))
        {
            rejectionReasons.Add("opposite_turns_have_same_scalar_direction");
            return -20;
        }

        rejectionReasons.Add("opposite_turn_polarity_unresolved");
        return -5;
    }

    private static double ScoreCameraSeparation(
        ScalarBehaviorEvidence match,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (!HasCameraOnlyLabel(match) || !HasTurnLabel(match))
        {
            return 0;
        }

        if (string.Equals(match.CameraTurnSeparationRelationship, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("camera_only_separated_from_actor_turn");
            return 15;
        }

        if (string.Equals(match.CameraTurnSeparationRelationship, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("actor_turn_separated_from_camera_only");
            return 15;
        }

        if (string.Equals(match.CameraTurnSeparationRelationship, "camera_and_turn_both_change", StringComparison.OrdinalIgnoreCase))
        {
            rejectionReasons.Add("camera_and_turn_both_change_same_scalar");
            return -10;
        }

        rejectionReasons.Add("camera_turn_separation_unresolved");
        return -5;
    }

    private static string Classification(ScalarBehaviorEvidence match, double scoreTotal)
    {
        if (scoreTotal < 60)
        {
            return "unclassified_scalar_behavior_candidate";
        }

        if (HasCameraOnlyLabel(match))
        {
            if (string.Equals(match.CameraTurnSeparationRelationship, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase))
            {
                return HasAngleFamily(match) ? "actor_yaw_angle_scalar_candidate" : "actor_yaw_scalar_candidate";
            }

            if (string.Equals(match.CameraTurnSeparationRelationship, "camera_and_turn_both_change", StringComparison.OrdinalIgnoreCase))
            {
                return HasAngleFamily(match) ? "mixed_camera_actor_angle_scalar_candidate" : "mixed_camera_actor_scalar_candidate";
            }

            return HasAngleFamily(match) ? "camera_orientation_angle_scalar_candidate" : "camera_orientation_scalar_candidate";
        }

        if (HasTurnLabel(match))
        {
            return HasAngleFamily(match) ? "turn_responsive_angle_scalar_candidate" : "turn_responsive_scalar_candidate";
        }

        return "unclassified_scalar_behavior_candidate";
    }

    private static string ConfidenceLevel(double scoreTotal, IReadOnlyCollection<string> rejectionReasons)
    {
        if (scoreTotal >= 80 && rejectionReasons.Count == 0)
        {
            return "strong_candidate";
        }

        return scoreTotal >= 60 ? "candidate" : "weak_candidate";
    }

    private static string NextValidationStep(ScalarBehaviorEvidence match, double scoreTotal)
    {
        if (scoreTotal >= 60 && HasTurnLabel(match))
        {
            return "compare_against_opposite_turn_and_camera_only_sessions";
        }

        if (scoreTotal >= 60 && HasCameraOnlyLabel(match))
        {
            return "compare_against_turn_session_to_separate_actor_from_camera";
        }

        return "capture_additional_labeled_turn_and_camera_sessions";
    }

    private static bool ChangedUnderTurnOrCamera(ScalarBehaviorEvidence match) =>
        (IsTurnLabel(match.SessionAStimulusLabel) && match.SessionAChangedSampleCount > 0) ||
        (IsTurnLabel(match.SessionBStimulusLabel) && match.SessionBChangedSampleCount > 0) ||
        (IsCameraOnlyLabel(match.SessionAStimulusLabel) && match.SessionAChangedSampleCount > 0) ||
        (IsCameraOnlyLabel(match.SessionBStimulusLabel) && match.SessionBChangedSampleCount > 0);

    private static bool PassiveChanged(ScalarBehaviorEvidence match) =>
        (IsPassiveLabel(match.SessionAStimulusLabel) && match.SessionAChangedSampleCount > 0) ||
        (IsPassiveLabel(match.SessionBStimulusLabel) && match.SessionBChangedSampleCount > 0);

    private static bool HasTurnLabel(ScalarBehaviorEvidence match) =>
        IsTurnLabel(match.SessionAStimulusLabel) || IsTurnLabel(match.SessionBStimulusLabel);

    private static bool HasCameraOnlyLabel(ScalarBehaviorEvidence match) =>
        IsCameraOnlyLabel(match.SessionAStimulusLabel) || IsCameraOnlyLabel(match.SessionBStimulusLabel);

    private static bool HasOppositeTurnLabels(ScalarBehaviorEvidence match) =>
        (IsTurnLeftLabel(match.SessionAStimulusLabel) && IsTurnRightLabel(match.SessionBStimulusLabel)) ||
        (IsTurnRightLabel(match.SessionAStimulusLabel) && IsTurnLeftLabel(match.SessionBStimulusLabel));

    private static bool HasDirectionalTurnOrCameraMovement(ScalarBehaviorEvidence match, ICollection<string> supportingReasons)
    {
        var hasMovement =
            (IsTurnLabel(match.SessionAStimulusLabel) || IsCameraOnlyLabel(match.SessionAStimulusLabel)) &&
            match.SessionACircularDeltaMagnitude > 0 &&
            match.SessionADirectionConsistencyRatio >= 0.5;
        hasMovement |=
            (IsTurnLabel(match.SessionBStimulusLabel) || IsCameraOnlyLabel(match.SessionBStimulusLabel)) &&
            match.SessionBCircularDeltaMagnitude > 0 &&
            match.SessionBDirectionConsistencyRatio >= 0.5;
        if (hasMovement)
        {
            supportingReasons.Add("directional_circular_delta_under_turn_or_camera");
        }

        return hasMovement;
    }

    private static bool HasAngleFamily(ScalarBehaviorEvidence match) =>
        IsRadiansFamily(match.ValueFamily) || IsDegreesFamily(match.ValueFamily);

    private static bool IsRadiansFamily(string valueFamily) =>
        valueFamily.StartsWith("angle_radians_", StringComparison.OrdinalIgnoreCase);

    private static bool IsDegreesFamily(string valueFamily) =>
        valueFamily.StartsWith("angle_degrees_", StringComparison.OrdinalIgnoreCase);

    private static bool IsPassiveLabel(string label) =>
        string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurnLabel(string label) =>
        IsTurnLeftLabel(label) || IsTurnRightLabel(label);

    private static bool IsTurnLeftLabel(string label) =>
        string.Equals(label, "turn_left", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurnRightLabel(string label) =>
        string.Equals(label, "turn_right", StringComparison.OrdinalIgnoreCase);

    private static bool IsCameraOnlyLabel(string label) =>
        string.Equals(label, "camera_only", StringComparison.OrdinalIgnoreCase);
}

internal sealed record ScalarBehaviorHeuristicResult(
    string Classification,
    double ScoreTotal,
    string ConfidenceLevel,
    IReadOnlyDictionary<string, double> ScoreBreakdown,
    IReadOnlyList<string> SupportingReasons,
    IReadOnlyList<string> RejectionReasons,
    string NextValidationStep);

internal sealed record ScalarBehaviorEvidence
{
    public string BaseAddressHex { get; init; } = string.Empty;

    public string OffsetHex { get; init; } = string.Empty;

    public string DataType { get; init; } = string.Empty;

    public string SessionACandidateId { get; init; } = string.Empty;

    public string SessionBCandidateId { get; init; } = string.Empty;

    public double SessionARankScore { get; init; }

    public double SessionBRankScore { get; init; }

    public int SessionAChangedSampleCount { get; init; }

    public int SessionBChangedSampleCount { get; init; }

    public double SessionAValueDeltaMagnitude { get; init; }

    public double SessionBValueDeltaMagnitude { get; init; }

    public double SessionACircularDeltaMagnitude { get; init; }

    public double SessionBCircularDeltaMagnitude { get; init; }

    public double SessionASignedCircularDelta { get; init; }

    public double SessionBSignedCircularDelta { get; init; }

    public string SessionADominantDirection { get; init; } = string.Empty;

    public string SessionBDominantDirection { get; init; } = string.Empty;

    public string TurnPolarityRelationship { get; init; } = string.Empty;

    public string CameraTurnSeparationRelationship { get; init; } = string.Empty;

    public string ValueFamily { get; init; } = string.Empty;

    public double SessionADirectionConsistencyRatio { get; init; }

    public double SessionBDirectionConsistencyRatio { get; init; }

    public string SessionAStimulusLabel { get; init; } = string.Empty;

    public string SessionBStimulusLabel { get; init; } = string.Empty;

    public static ScalarBehaviorEvidence FromValueMatch(ValueCandidateComparison match) =>
        new()
        {
            BaseAddressHex = match.BaseAddressHex,
            OffsetHex = match.OffsetHex,
            DataType = match.DataType,
            SessionACandidateId = match.SessionACandidateId,
            SessionBCandidateId = match.SessionBCandidateId,
            SessionARankScore = match.SessionARankScore,
            SessionBRankScore = match.SessionBRankScore,
            SessionAChangedSampleCount = match.SessionAChangedSampleCount,
            SessionBChangedSampleCount = match.SessionBChangedSampleCount,
            SessionAStimulusLabel = match.SessionAStimulusLabel,
            SessionBStimulusLabel = match.SessionBStimulusLabel
        };

    public static ScalarBehaviorEvidence FromScalarMatch(ScalarCandidateComparison match) =>
        new()
        {
            BaseAddressHex = match.BaseAddressHex,
            OffsetHex = match.OffsetHex,
            DataType = match.DataType,
            SessionACandidateId = match.SessionACandidateId,
            SessionBCandidateId = match.SessionBCandidateId,
            SessionARankScore = match.SessionARankScore,
            SessionBRankScore = match.SessionBRankScore,
            SessionAChangedSampleCount = match.SessionAChangedSampleCount,
            SessionBChangedSampleCount = match.SessionBChangedSampleCount,
            SessionAValueDeltaMagnitude = match.SessionAValueDeltaMagnitude,
            SessionBValueDeltaMagnitude = match.SessionBValueDeltaMagnitude,
            SessionACircularDeltaMagnitude = match.SessionACircularDeltaMagnitude,
            SessionBCircularDeltaMagnitude = match.SessionBCircularDeltaMagnitude,
            SessionASignedCircularDelta = match.SessionASignedCircularDelta,
            SessionBSignedCircularDelta = match.SessionBSignedCircularDelta,
            SessionADominantDirection = match.SessionADominantDirection,
            SessionBDominantDirection = match.SessionBDominantDirection,
            TurnPolarityRelationship = match.TurnPolarityRelationship,
            CameraTurnSeparationRelationship = match.CameraTurnSeparationRelationship,
            ValueFamily = match.ValueFamily,
            SessionADirectionConsistencyRatio = match.SessionADirectionConsistencyRatio,
            SessionBDirectionConsistencyRatio = match.SessionBDirectionConsistencyRatio,
            SessionAStimulusLabel = match.SessionAStimulusLabel,
            SessionBStimulusLabel = match.SessionBStimulusLabel
        };
}
