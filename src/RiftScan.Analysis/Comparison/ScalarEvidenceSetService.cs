using System.Text.Json;
using RiftScan.Analysis.Scalars;
using RiftScan.Analysis.Triage;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarEvidenceSetService
{
    private const double MinMeaningfulBehaviorDelta = 0.0001;

    public ScalarEvidenceSetResult Aggregate(IReadOnlyList<string> sessionPaths, int top = 100)
    {
        ArgumentNullException.ThrowIfNull(sessionPaths);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);
        if (sessionPaths.Count < 2)
        {
            throw new ArgumentException("Scalar evidence aggregation requires at least two sessions.", nameof(sessionPaths));
        }

        var sessions = sessionPaths
            .Select(path => LoadSession(Path.GetFullPath(path), top))
            .ToArray();
        var labels = sessions
            .Select(session => session.Summary.StimulusLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var groups = sessions
            .SelectMany(session => session.Candidates.Select(candidate => new SessionScalarCandidate(session, candidate)))
            .GroupBy(entry => ScalarKey(entry.Candidate), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allAggregates = groups
            .Select(group => BuildAggregate(group.ToArray()))
            .ToArray();
        var candidates = allAggregates
            .Where(candidate => candidate.ScoreTotal >= 50)
            .OrderByDescending(candidate => candidate.ScoreTotal)
            .ThenBy(candidate => candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
        var rejectedSummaries = BuildRejectedSummaries(allAggregates.Where(candidate => candidate.ScoreTotal < 50).ToArray());

        return new ScalarEvidenceSetResult
        {
            Success = true,
            SessionPaths = sessions.Select(session => session.Summary.SessionPath).ToArray(),
            SessionCount = sessions.Length,
            ScalarCandidateKeyCount = groups.Length,
            RankedCandidateCount = candidates.Length,
            SessionSummaries = sessions.Select(session => session.Summary).ToArray(),
            RankedCandidates = candidates,
            RejectedCandidateSummaries = rejectedSummaries,
            Warnings = BuildWarnings(labels, candidates)
        };
    }

    private static LoadedScalarSession LoadSession(string sessionPath, int top)
    {
        EnsureAnalyzed(sessionPath, top);
        var manifest = ReadJson<SessionManifest>(sessionPath, "manifest.json");
        var label = ReadStimulusLabel(sessionPath);
        var candidates = ReadJsonLines<ScalarCandidate>(sessionPath, "scalar_candidates.jsonl");
        return new LoadedScalarSession(
            new ScalarEvidenceSessionSummary
            {
                SessionId = manifest.SessionId,
                SessionPath = sessionPath,
                StimulusLabel = label,
                ScalarCandidateCount = candidates.Count
            },
            candidates);
    }

    private static void EnsureAnalyzed(string sessionPath, int top)
    {
        var verification = new SessionVerifier().Verify(sessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before scalar evidence aggregation: {issues}");
        }

        if (!File.Exists(ResolveSessionPath(sessionPath, "scalar_candidates.jsonl")))
        {
            _ = new DynamicRegionTriageAnalyzer().AnalyzeSession(sessionPath, top);
        }
    }

    private static ScalarEvidenceAggregateCandidate BuildAggregate(IReadOnlyList<SessionScalarCandidate> entries)
    {
        var first = entries[0].Candidate;
        var labels = entries
            .Select(entry => entry.Session.Summary.StimulusLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var passiveEntries = entries.Where(entry => IsPassive(entry.Session.Summary.StimulusLabel)).ToArray();
        var leftEntries = entries.Where(entry => IsTurnLeft(entry.Session.Summary.StimulusLabel)).ToArray();
        var rightEntries = entries.Where(entry => IsTurnRight(entry.Session.Summary.StimulusLabel)).ToArray();
        var cameraEntries = entries.Where(entry => IsCameraOnly(entry.Session.Summary.StimulusLabel)).ToArray();

        var passiveStable = passiveEntries.Length > 0 && passiveEntries.All(entry => entry.Candidate.ChangedSampleCount == 0);
        var passiveChanged = passiveEntries.Any(entry => entry.Candidate.ChangedSampleCount > 0);
        var leftChanged = leftEntries.Any(entry => HasDirectedBehaviorChange(entry.Candidate));
        var rightChanged = rightEntries.Any(entry => HasDirectedBehaviorChange(entry.Candidate));
        var cameraChanged = cameraEntries.Any(entry => HasDirectedBehaviorChange(entry.Candidate));
        var leftSignedDelta = SumSignedDelta(leftEntries);
        var rightSignedDelta = SumSignedDelta(rightEntries);
        var cameraSignedDelta = SumSignedDelta(cameraEntries);
        var oppositePolarity = leftChanged && rightChanged && Math.Sign(leftSignedDelta) != 0 && Math.Sign(leftSignedDelta) == -Math.Sign(rightSignedDelta);
        var turnChanged = leftChanged || rightChanged;
        var cameraTurnSeparation = ClassifyCameraTurnSeparation(cameraChanged, turnChanged);

        var supportingReasons = new List<string>();
        var rejectionReasons = new List<string>();
        var scoreBreakdown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var labelScore = ScoreLabels(labels, supportingReasons, rejectionReasons);
        scoreBreakdown["label_coverage_score"] = labelScore;
        var angleScore = ScoreAngleFamily(first.ValueFamily, supportingReasons, rejectionReasons);
        scoreBreakdown["angle_family_score"] = angleScore;
        var passiveScore = ScorePassive(passiveStable, passiveChanged, supportingReasons, rejectionReasons);
        scoreBreakdown["passive_baseline_score"] = passiveScore;
        var turnScore = ScoreTurns(leftChanged, rightChanged, oppositePolarity, cameraTurnSeparation, supportingReasons, rejectionReasons);
        scoreBreakdown["turn_polarity_score"] = turnScore;
        var cameraScore = ScoreCameraSeparation(cameraTurnSeparation, supportingReasons, rejectionReasons);
        scoreBreakdown["camera_turn_separation_score"] = cameraScore;
        var rawScore = labelScore + angleScore + passiveScore + turnScore + cameraScore;
        var scoreTotal = Math.Round(Math.Clamp(rawScore, 0, 100), 3);
        scoreBreakdown["score_total"] = scoreTotal;

        return new ScalarEvidenceAggregateCandidate
        {
            Classification = Classify(scoreTotal, oppositePolarity, cameraTurnSeparation, first.ValueFamily),
            BaseAddressHex = first.BaseAddressHex,
            OffsetHex = first.OffsetHex,
            DataType = first.DataType,
            ValueFamily = first.ValueFamily,
            ScoreTotal = scoreTotal,
            ConfidenceLevel = Confidence(scoreTotal, rejectionReasons),
            TruthReadiness = TruthReadiness(scoreTotal, passiveStable, oppositePolarity, cameraTurnSeparation, rejectionReasons),
            ScoreBreakdown = scoreBreakdown,
            LabelsPresent = labels,
            SessionsPresent = entries.Select(entry => entry.Session.Summary.SessionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            PassiveStable = passiveStable,
            TurnLeftChanged = leftChanged,
            TurnRightChanged = rightChanged,
            CameraOnlyChanged = cameraChanged,
            OppositeTurnPolarity = oppositePolarity,
            CameraTurnSeparation = cameraTurnSeparation,
            TurnLeftSignedDelta = Math.Round(leftSignedDelta, 6),
            TurnRightSignedDelta = Math.Round(rightSignedDelta, 6),
            CameraOnlySignedDelta = Math.Round(cameraSignedDelta, 6),
            SupportingReasons = supportingReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            RejectionReasons = rejectionReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            EvidenceSummary = $"labels={string.Join(",", labels)};passive_stable={passiveStable};left_delta={leftSignedDelta:F6};right_delta={rightSignedDelta:F6};camera_delta={cameraSignedDelta:F6};camera_turn={cameraTurnSeparation}",
            NextValidationStep = NextValidation(scoreTotal, passiveStable, cameraTurnSeparation, oppositePolarity)
        };
    }

    private static double ScoreLabels(IReadOnlyList<string> labels, ICollection<string> supportingReasons, ICollection<string> rejectionReasons)
    {
        var score = 0.0;
        if (labels.Any(IsPassive))
        {
            supportingReasons.Add("passive_label_present");
            score += 10;
        }

        if (labels.Any(IsTurnLeft) && labels.Any(IsTurnRight))
        {
            supportingReasons.Add("opposite_turn_labels_present");
            score += 20;
        }
        else if (labels.Any(IsTurnLabel))
        {
            supportingReasons.Add("single_turn_label_present");
            score += 10;
        }

        if (labels.Any(IsCameraOnly))
        {
            supportingReasons.Add("camera_only_label_present");
            score += 15;
        }

        if (score == 0)
        {
            rejectionReasons.Add("no_behavior_labels_present");
        }

        return score;
    }

    private static double ScoreAngleFamily(string valueFamily, ICollection<string> supportingReasons, ICollection<string> rejectionReasons)
    {
        if (valueFamily.StartsWith("angle_radians_", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("radian_angle_value_family");
            return 15;
        }

        if (valueFamily.StartsWith("angle_degrees_", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("degree_angle_value_family");
            return 12;
        }

        rejectionReasons.Add("not_angle_value_family");
        return 0;
    }

    private static double ScorePassive(bool passiveStable, bool passiveChanged, ICollection<string> supportingReasons, ICollection<string> rejectionReasons)
    {
        if (passiveStable)
        {
            supportingReasons.Add("passive_baseline_stable");
            return 10;
        }

        if (passiveChanged)
        {
            rejectionReasons.Add("passive_baseline_changed");
            return -15;
        }

        return 0;
    }

    private static double ScoreTurns(
        bool leftChanged,
        bool rightChanged,
        bool oppositePolarity,
        string cameraTurnSeparation,
        ICollection<string> supportingReasons,
        ICollection<string> rejectionReasons)
    {
        if (oppositePolarity)
        {
            supportingReasons.Add("left_right_opposite_turn_polarity");
            return 25;
        }

        if (leftChanged || rightChanged)
        {
            supportingReasons.Add("turn_labeled_scalar_change");
            return 10;
        }

        if (string.Equals(cameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("turn_sessions_stable_for_camera_only");
            return 10;
        }

        rejectionReasons.Add("no_turn_labeled_scalar_change");
        return 0;
    }

    private static double ScoreCameraSeparation(string relationship, ICollection<string> supportingReasons, ICollection<string> rejectionReasons)
    {
        if (string.Equals(relationship, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("actor_turn_separated_from_camera_only");
            return 20;
        }

        if (string.Equals(relationship, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            supportingReasons.Add("camera_only_separated_from_actor_turn");
            return 20;
        }

        if (string.Equals(relationship, "camera_and_turn_both_change", StringComparison.OrdinalIgnoreCase))
        {
            rejectionReasons.Add("camera_and_turn_both_change");
            return -10;
        }

        return 0;
    }

    private static string Classify(double scoreTotal, bool oppositePolarity, string cameraTurnSeparation, string valueFamily)
    {
        if (scoreTotal < 50)
        {
            return "unclassified_scalar_evidence_candidate";
        }

        var suffix = IsAngleFamily(valueFamily) ? "angle_scalar_candidate" : "scalar_candidate";
        if (string.Equals(cameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            return $"camera_orientation_{suffix}";
        }

        if (oppositePolarity || string.Equals(cameraTurnSeparation, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase))
        {
            return $"actor_yaw_{suffix}";
        }

        if (string.Equals(cameraTurnSeparation, "camera_and_turn_both_change", StringComparison.OrdinalIgnoreCase))
        {
            return $"mixed_camera_actor_{suffix}";
        }

        return $"behavior_responsive_{suffix}";
    }

    private static string Confidence(double scoreTotal, IReadOnlyList<string> rejectionReasons)
    {
        if (scoreTotal >= 80 && rejectionReasons.Count == 0)
        {
            return "validated_candidate";
        }

        if (scoreTotal >= 65)
        {
            return "strong_candidate";
        }

        return scoreTotal >= 50 ? "candidate" : "weak_candidate";
    }

    private static string TruthReadiness(
        double scoreTotal,
        bool passiveStable,
        bool oppositePolarity,
        string cameraTurnSeparation,
        IReadOnlyList<string> rejectionReasons)
    {
        if (scoreTotal < 50)
        {
            return "insufficient";
        }

        if (scoreTotal >= 80 &&
            passiveStable &&
            oppositePolarity &&
            IsSeparatedCameraTurnRelationship(cameraTurnSeparation) &&
            rejectionReasons.Count == 0)
        {
            return "validated_candidate";
        }

        if (scoreTotal >= 80 &&
            passiveStable &&
            string.Equals(cameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase) &&
            rejectionReasons.Count == 0)
        {
            return "validated_candidate";
        }

        if (scoreTotal >= 65 && rejectionReasons.Count == 0)
        {
            return "strong_candidate";
        }

        return "candidate";
    }

    private static string NextValidation(double scoreTotal, bool passiveStable, string cameraTurnSeparation, bool oppositePolarity)
    {
        if (string.Equals(cameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase))
        {
            return scoreTotal >= 80 && passiveStable
                ? "repeat_camera_only_capture_or_validate_against_camera_truth"
                : "repeat_passive_baseline_and_camera_only_capture";
        }

        if (scoreTotal >= 80 && oppositePolarity && !string.IsNullOrWhiteSpace(cameraTurnSeparation))
        {
            return "repeat_labeled_capture_or_validate_against_addon_truth";
        }

        if (!oppositePolarity)
        {
            return "add_or_repeat_opposite_turn_sessions";
        }

        return "add_camera_only_session_for_actor_camera_split";
    }

    private static bool IsSeparatedCameraTurnRelationship(string cameraTurnSeparation) =>
        string.Equals(cameraTurnSeparation, "turn_changes_camera_only_stable", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(cameraTurnSeparation, "camera_only_changes_turn_stable", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<string> labels, IReadOnlyList<ScalarEvidenceAggregateCandidate> candidates)
    {
        var warnings = new List<string> { "scalar_evidence_is_candidate_evidence_not_truth_claim" };
        if (!labels.Any(IsPassive))
        {
            warnings.Add("missing_passive_baseline_session");
        }

        if (!labels.Any(IsTurnLeft) || !labels.Any(IsTurnRight))
        {
            warnings.Add("missing_opposite_turn_pair");
        }

        if (!labels.Any(IsCameraOnly))
        {
            warnings.Add("missing_camera_only_session");
        }

        if (candidates.Count == 0)
        {
            warnings.Add("no_ranked_scalar_evidence_candidates");
        }

        return warnings;
    }

    private static IReadOnlyList<ScalarEvidenceRejectedSummary> BuildRejectedSummaries(IReadOnlyList<ScalarEvidenceAggregateCandidate> rejectedCandidates)
    {
        var reasons = rejectedCandidates
            .SelectMany(candidate => PrimaryRejectionReasons(candidate).Select(reason => (Reason: reason, Candidate: candidate)))
            .GroupBy(entry => entry.Reason, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ScalarEvidenceRejectedSummary
            {
                Reason = group.Key,
                Count = group.Count(),
                ExampleCandidates = group
                    .OrderByDescending(entry => entry.Candidate.ScoreTotal)
                    .ThenBy(entry => entry.Candidate.BaseAddressHex, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .Select(entry => new ScalarEvidenceRejectedExample
                    {
                        BaseAddressHex = entry.Candidate.BaseAddressHex,
                        OffsetHex = entry.Candidate.OffsetHex,
                        ValueFamily = entry.Candidate.ValueFamily,
                        ScoreTotal = entry.Candidate.ScoreTotal,
                        EvidenceSummary = entry.Candidate.EvidenceSummary
                    })
                    .ToArray()
            })
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Reason, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return reasons;
    }

    private static IReadOnlyList<string> PrimaryRejectionReasons(ScalarEvidenceAggregateCandidate candidate)
    {
        var reasons = new List<string>();
        if (candidate.RejectionReasons.Count > 0)
        {
            reasons.AddRange(candidate.RejectionReasons);
        }

        if (!candidate.LabelsPresent.Any(IsPassive))
        {
            reasons.Add("missing_passive_baseline_for_candidate");
        }

        if (!candidate.LabelsPresent.Any(IsTurnLeft) || !candidate.LabelsPresent.Any(IsTurnRight))
        {
            reasons.Add("missing_opposite_turn_pair_for_candidate");
        }

        if (!candidate.LabelsPresent.Any(IsCameraOnly))
        {
            reasons.Add("missing_camera_only_for_candidate");
        }

        if (!IsAngleFamily(candidate.ValueFamily))
        {
            reasons.Add("not_angle_value_family");
        }

        if (string.Equals(candidate.CameraTurnSeparation, "camera_and_turn_both_change", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("camera_and_turn_both_change");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("score_below_candidate_threshold");
        }

        return reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string ClassifyCameraTurnSeparation(bool cameraChanged, bool turnChanged) =>
        (cameraChanged, turnChanged) switch
        {
            (true, false) => "camera_only_changes_turn_stable",
            (false, true) => "turn_changes_camera_only_stable",
            (true, true) => "camera_and_turn_both_change",
            _ => "camera_and_turn_both_stable"
        };

    private static bool HasDirectedBehaviorChange(ScalarCandidate candidate) =>
        candidate.ChangedSampleCount > 0 &&
        (Math.Abs(candidate.SignedCircularDelta) >= MinMeaningfulBehaviorDelta ||
         candidate.ValueDeltaMagnitude >= MinMeaningfulBehaviorDelta);

    private static double SumSignedDelta(IEnumerable<SessionScalarCandidate> entries) =>
        entries.Sum(entry => entry.Candidate.SignedCircularDelta);

    private static string ScalarKey(ScalarCandidate candidate) =>
        string.Join("|", candidate.BaseAddressHex, candidate.OffsetHex, candidate.DataType);

    private static bool IsAngleFamily(string valueFamily) =>
        valueFamily.StartsWith("angle_radians_", StringComparison.OrdinalIgnoreCase) ||
        valueFamily.StartsWith("angle_degrees_", StringComparison.OrdinalIgnoreCase);

    private static bool IsPassive(string label) =>
        string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurnLabel(string label) =>
        IsTurnLeft(label) || IsTurnRight(label);

    private static bool IsTurnLeft(string label) =>
        string.Equals(label, "turn_left", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurnRight(string label) =>
        string.Equals(label, "turn_right", StringComparison.OrdinalIgnoreCase);

    private static bool IsCameraOnly(string label) =>
        string.Equals(label, "camera_only", StringComparison.OrdinalIgnoreCase);

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<T> ReadJsonLines<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid JSONL entry in {relativePath}."))
            .ToArray();
    }

    private static string ReadStimulusLabel(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "stimuli.jsonl");
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var stimulus = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<StimulusEvent>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid stimulus entry."))
            .FirstOrDefault();
        return stimulus?.Label ?? string.Empty;
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed record LoadedScalarSession(ScalarEvidenceSessionSummary Summary, IReadOnlyList<ScalarCandidate> Candidates);

    private sealed record SessionScalarCandidate(LoadedScalarSession Session, ScalarCandidate Candidate);
}
