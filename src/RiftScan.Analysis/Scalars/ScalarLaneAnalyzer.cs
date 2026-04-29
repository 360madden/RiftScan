using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Scalars;

public sealed class ScalarLaneAnalyzer
{
    private const double TwoPi = Math.PI * 2.0;
    private const float MaxPlausibleMagnitude = 1_000_000f;
    private const float MinMeaningfulMagnitude = 0.0001f;

    public IReadOnlyList<ScalarCandidate> AnalyzeSession(string sessionPath, int top = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var verification = new SessionVerifier().Verify(fullSessionPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Session verification failed before scalar analysis: {issues}");
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json");
        var stimulusLabel = ReadStimulusLabel(fullSessionPath);
        var allCandidates = ReadSnapshotIndex(fullSessionPath)
            .GroupBy(entry => entry.RegionId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => AnalyzeRegion(fullSessionPath, manifest.SessionId, stimulusLabel, group.ToArray()))
            .ToArray();
        var candidates = SelectBalancedCandidates(allCandidates, top)
            .Select((candidate, index) => candidate with { CandidateId = $"scalar-{index + 1:000000}" })
            .ToArray();

        WriteJsonLines(fullSessionPath, "scalar_candidates.jsonl", candidates);
        return candidates;
    }

    private static IEnumerable<ScalarCandidate> AnalyzeRegion(
        string sessionPath,
        string sessionId,
        string stimulusLabel,
        IReadOnlyList<SnapshotIndexEntry> snapshots)
    {
        if (snapshots.Count < 2)
        {
            yield break;
        }

        var snapshotBytes = snapshots
            .Select(snapshot => File.ReadAllBytes(ResolveSessionPath(sessionPath, snapshot.Path)))
            .ToArray();
        var maxOffset = snapshotBytes.Min(bytes => bytes.Length) - sizeof(float);
        if (maxOffset < 0)
        {
            yield break;
        }

        for (var offset = 0; offset <= maxOffset; offset += sizeof(float))
        {
            var values = snapshotBytes
                .Select(bytes => BitConverter.ToSingle(bytes, offset))
                .ToArray();
            if (!LooksLikeFloatLane(values))
            {
                continue;
            }

            yield return BuildCandidate(sessionId, snapshots[0], stimulusLabel, values, offset);
        }
    }

    private static ScalarCandidate BuildCandidate(
        string sessionId,
        SnapshotIndexEntry firstSnapshot,
        string stimulusLabel,
        IReadOnlyList<float> values,
        int offset)
    {
        var preview = values.Select(value => value.ToString("G9", CultureInfo.InvariantCulture)).ToArray();
        var distinctValueCount = preview.Distinct(StringComparer.Ordinal).Count();
        var changedSampleCount = CountAdjacentChanges(preview);
        var comparedPairCount = Math.Max(1, preview.Length - 1);
        var supportRatio = 1.0;
        var changeRatio = changedSampleCount / (double)comparedPairCount;
        var stabilityRatio = 1.0 - changeRatio;
        var valueFamily = ClassifyValueFamily(values);
        var valueDeltaMagnitude = Math.Abs((double)values[^1] - values[0]);
        if (!double.IsFinite(valueDeltaMagnitude))
        {
            valueDeltaMagnitude = 0;
        }

        var circularDeltaMagnitude = CalculateCircularDeltaMagnitude(values, valueFamily);
        var signedCircularDelta = CalculateSignedCircularDelta(values, valueFamily);
        var directionConsistencyRatio = CalculateDirectionConsistencyRatio(values, valueFamily);
        var dominantDirection = DominantDirection(signedCircularDelta);
        var retentionBucket = RetentionBucket(valueFamily, changedSampleCount);
        var finiteSupportScore = supportRatio * 30.0;
        var typeScore = 25.0;
        var stabilityScore = stabilityRatio * 25.0;
        var changeScore = changeRatio * 20.0;
        var angleShapeScore = ScoreValueFamily(valueFamily);
        var rankScore = Math.Round(Math.Clamp(finiteSupportScore + typeScore + stabilityScore + changeScore + angleShapeScore, 0, 100), 3);
        var diagnostics = new List<string>
        {
            "finite_float32_scalar_lane",
            valueFamily,
            retentionBucket,
            changedSampleCount == 0 ? "stable_scalar_lane_included" : "changing_scalar_lane_included",
            "candidate_not_truth_claim"
        };
        var analyzerSources = new List<string> { "snapshots/index.jsonl", "snapshots/*.bin" };
        if (!string.IsNullOrWhiteSpace(stimulusLabel))
        {
            analyzerSources.Add("stimuli.jsonl");
        }

        var absoluteAddress = ParseHex(firstSnapshot.BaseAddressHex) + (ulong)offset;
        return new ScalarCandidate
        {
            SessionId = sessionId,
            RegionId = firstSnapshot.RegionId,
            BaseAddressHex = firstSnapshot.BaseAddressHex,
            OffsetHex = $"0x{offset:X}",
            AbsoluteAddressHex = $"0x{absoluteAddress:X}",
            DataType = "float32",
            SampleCount = preview.Length,
            DistinctValueCount = distinctValueCount,
            ChangedSampleCount = changedSampleCount,
            ValueDeltaMagnitude = Math.Round(valueDeltaMagnitude, 6),
            CircularDeltaMagnitude = Math.Round(circularDeltaMagnitude, 6),
            SignedCircularDelta = Math.Round(signedCircularDelta, 6),
            DominantDirection = dominantDirection,
            ValueFamily = valueFamily,
            DirectionConsistencyRatio = Math.Round(directionConsistencyRatio, 6),
            RetentionBucket = retentionBucket,
            StimulusLabel = stimulusLabel,
            RankScore = rankScore,
            ScoreBreakdown = BuildScoreBreakdown(finiteSupportScore, typeScore, stabilityScore, changeScore, angleShapeScore, rankScore),
            FeatureVector = BuildFeatureVector(preview.Length, distinctValueCount, changedSampleCount, changeRatio, stabilityRatio, valueDeltaMagnitude, circularDeltaMagnitude, signedCircularDelta, directionConsistencyRatio),
            AnalyzerSources = analyzerSources,
            ValueSequenceSummary = $"samples={preview.Length};distinct={distinctValueCount};changed_pairs={changedSampleCount};delta={valueDeltaMagnitude:G9};circular_delta={circularDeltaMagnitude:G9};signed_circular_delta={signedCircularDelta:G9};direction={dominantDirection};family={valueFamily};preview={string.Join("|", preview.Take(3))}",
            ConfidenceLevel = ToConfidenceLevel(rankScore),
            ExplanationShort = changedSampleCount == 0
                ? $"stable_float32_scalar_lane_{preview.Length}_samples"
                : $"changing_float32_scalar_lane_{changedSampleCount}_of_{comparedPairCount}_pairs",
            Recommendation = Recommend(stimulusLabel, changedSampleCount),
            ValuePreview = preview.Take(8).ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static IReadOnlyList<ScalarCandidate> SelectBalancedCandidates(IReadOnlyList<ScalarCandidate> candidates, int top)
    {
        var sortedGroups = candidates
            .GroupBy(candidate => string.Join("|", candidate.RegionId, candidate.RetentionBucket), StringComparer.OrdinalIgnoreCase)
            .Select(group => new CandidateGroup(
                BucketPriority(group.First().RetentionBucket),
                group.First().RegionId,
                group.First().RetentionBucket,
                group
                    .OrderByDescending(candidate => candidate.RankScore)
                    .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(group => group.Priority)
            .ThenBy(group => group.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Bucket, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selected = new List<ScalarCandidate>(Math.Min(top, candidates.Count));

        while (selected.Count < top)
        {
            var selectedInPass = false;
            foreach (var group in sortedGroups)
            {
                if (group.NextIndex >= group.Candidates.Count)
                {
                    continue;
                }

                selected.Add(group.Candidates[group.NextIndex]);
                group.NextIndex++;
                selectedInPass = true;
                if (selected.Count == top)
                {
                    break;
                }
            }

            if (!selectedInPass)
            {
                break;
            }
        }

        return selected
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenBy(candidate => BucketPriority(candidate.RetentionBucket))
            .ThenBy(candidate => candidate.RegionId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OffsetHex, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, double> BuildScoreBreakdown(
        double finiteSupportScore,
        double typeScore,
        double stabilityScore,
        double changeScore,
        double angleShapeScore,
        double scoreTotal) =>
        new Dictionary<string, double>
        {
            ["finite_support_score"] = Math.Round(finiteSupportScore, 3),
            ["type_score"] = Math.Round(typeScore, 3),
            ["stability_score"] = Math.Round(stabilityScore, 3),
            ["change_score"] = Math.Round(changeScore, 3),
            ["angle_shape_score"] = Math.Round(angleShapeScore, 3),
            ["score_total"] = Math.Round(scoreTotal, 3)
        };

    private static IReadOnlyDictionary<string, double> BuildFeatureVector(
        int sampleCount,
        int distinctValueCount,
        int changedSampleCount,
        double changeRatio,
        double stabilityRatio,
        double valueDeltaMagnitude,
        double circularDeltaMagnitude,
        double signedCircularDelta,
        double directionConsistencyRatio) =>
        new Dictionary<string, double>
        {
            ["sample_count"] = sampleCount,
            ["distinct_value_count"] = distinctValueCount,
            ["changed_sample_count"] = changedSampleCount,
            ["change_ratio"] = Math.Round(changeRatio, 6),
            ["stability_ratio"] = Math.Round(stabilityRatio, 6),
            ["value_delta_magnitude"] = Math.Round(valueDeltaMagnitude, 6),
            ["circular_delta_magnitude"] = Math.Round(circularDeltaMagnitude, 6),
            ["signed_circular_delta"] = Math.Round(signedCircularDelta, 6),
            ["direction_consistency_ratio"] = Math.Round(directionConsistencyRatio, 6)
        };

    private static string ClassifyValueFamily(IReadOnlyList<float> values)
    {
        if (values.All(value => value >= -0.001f && value <= (float)(TwoPi + 0.001)))
        {
            return "angle_radians_0_to_2pi";
        }

        if (values.All(value => value >= (float)(-Math.PI - 0.001) && value <= (float)(Math.PI + 0.001)))
        {
            return "angle_radians_neg_pi_to_pi";
        }

        if (values.All(value => value >= -0.001f && value <= 360.001f))
        {
            return "angle_degrees_0_to_360";
        }

        if (values.All(value => value >= -180.001f && value <= 180.001f))
        {
            return "angle_degrees_neg_180_to_180";
        }

        if (values.All(value => value >= -1.001f && value <= 1.001f))
        {
            return "normalized_unit_scalar";
        }

        return "generic_float32_scalar";
    }

    private static double ScoreValueFamily(string valueFamily) =>
        valueFamily switch
        {
            "angle_radians_0_to_2pi" or "angle_radians_neg_pi_to_pi" => 15.0,
            "angle_degrees_0_to_360" or "angle_degrees_neg_180_to_180" => 12.0,
            "normalized_unit_scalar" => 3.0,
            _ => 0.0
        };

    private static string RetentionBucket(string valueFamily, int changedSampleCount)
    {
        var motion = changedSampleCount > 0 ? "changing" : "stable";
        if (valueFamily.StartsWith("angle_", StringComparison.OrdinalIgnoreCase))
        {
            return $"{motion}_angle";
        }

        if (string.Equals(valueFamily, "normalized_unit_scalar", StringComparison.OrdinalIgnoreCase))
        {
            return $"{motion}_normalized";
        }

        return $"{motion}_generic";
    }

    private static int BucketPriority(string retentionBucket) =>
        retentionBucket switch
        {
            "changing_angle" => 0,
            "stable_angle" => 1,
            "changing_normalized" => 2,
            "stable_normalized" => 3,
            "changing_generic" => 4,
            "stable_generic" => 5,
            _ => 99
        };

    private static double CalculateCircularDeltaMagnitude(IReadOnlyList<float> values, string valueFamily)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var period = PeriodFor(valueFamily);
        if (period <= 0)
        {
            return Math.Abs((double)values[^1] - values[0]);
        }

        var total = 0.0;
        for (var index = 1; index < values.Count; index++)
        {
            total += Math.Abs(CircularDifference(values[index - 1], values[index], period));
        }

        return double.IsFinite(total) ? total : 0;
    }

    private static double CalculateDirectionConsistencyRatio(IReadOnlyList<float> values, string valueFamily)
    {
        if (values.Count < 3)
        {
            return 1.0;
        }

        var period = PeriodFor(valueFamily);
        var positive = 0;
        var negative = 0;
        for (var index = 1; index < values.Count; index++)
        {
            var delta = period > 0
                ? CircularDifference(values[index - 1], values[index], period)
                : values[index] - values[index - 1];
            if (Math.Abs(delta) < MinMeaningfulMagnitude)
            {
                continue;
            }

            if (delta > 0)
            {
                positive++;
            }
            else
            {
                negative++;
            }
        }

        var directionalChanges = positive + negative;
        return directionalChanges == 0 ? 1.0 : Math.Max(positive, negative) / (double)directionalChanges;
    }

    private static double CalculateSignedCircularDelta(IReadOnlyList<float> values, string valueFamily)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var period = PeriodFor(valueFamily);
        var total = 0.0;
        for (var index = 1; index < values.Count; index++)
        {
            total += period > 0
                ? CircularDifference(values[index - 1], values[index], period)
                : values[index] - values[index - 1];
        }

        return double.IsFinite(total) ? total : 0;
    }

    private static string DominantDirection(double signedCircularDelta)
    {
        if (Math.Abs(signedCircularDelta) < MinMeaningfulMagnitude)
        {
            return "stable";
        }

        return signedCircularDelta > 0 ? "positive" : "negative";
    }

    private static double PeriodFor(string valueFamily) =>
        valueFamily switch
        {
            "angle_radians_0_to_2pi" or "angle_radians_neg_pi_to_pi" => TwoPi,
            "angle_degrees_0_to_360" or "angle_degrees_neg_180_to_180" => 360.0,
            _ => 0.0
        };

    private static double CircularDifference(double previous, double current, double period)
    {
        var delta = current - previous;
        if (delta > period / 2.0)
        {
            delta -= period;
        }
        else if (delta < -period / 2.0)
        {
            delta += period;
        }

        return delta;
    }

    private static bool LooksLikeFloatLane(IReadOnlyList<float> values) =>
        values.All(value => float.IsFinite(value) && MathF.Abs(value) <= MaxPlausibleMagnitude) &&
        values.Any(value => MathF.Abs(value) >= MinMeaningfulMagnitude);

    private static string ToConfidenceLevel(double rankScore)
    {
        if (rankScore >= 75.0)
        {
            return "high";
        }

        return rankScore >= 50.0 ? "medium" : "low";
    }

    private static string Recommend(string stimulusLabel, int changedSampleCount)
    {
        if (changedSampleCount == 0)
        {
            return IsPassiveLabel(stimulusLabel)
                ? "passive_stable_scalar_baseline_for_behavior_contrast"
                : "stable_scalar_followup";
        }

        return IsTurnOrCameraLabel(stimulusLabel)
            ? "labeled_scalar_behavior_followup"
            : "changing_scalar_followup";
    }

    private static int CountAdjacentChanges(IReadOnlyList<string> values)
    {
        var changes = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (!string.Equals(values[index - 1], values[index], StringComparison.Ordinal))
            {
                changes++;
            }
        }

        return changes;
    }

    private static bool IsPassiveLabel(string label) =>
        string.Equals(label, "passive", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "passive_idle", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurnOrCameraLabel(string label) =>
        string.Equals(label, "turn_left", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "turn_right", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(label, "camera_only", StringComparison.OrdinalIgnoreCase);

    private static T ReadJson<T>(string sessionPath, string relativePath)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Could not deserialize {relativePath}.");
    }

    private static IReadOnlyList<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options) ?? throw new InvalidOperationException("Invalid snapshot index entry."))
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

    private static void WriteJsonLines<T>(string sessionPath, string relativePath, IEnumerable<T> entries) =>
        File.WriteAllLines(ResolveSessionPath(sessionPath, relativePath), entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

    private static ulong ParseHex(string value)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed class CandidateGroup(int priority, string regionId, string bucket, IReadOnlyList<ScalarCandidate> candidates)
    {
        public int Priority { get; } = priority;

        public string RegionId { get; } = regionId;

        public string Bucket { get; } = bucket;

        public IReadOnlyList<ScalarCandidate> Candidates { get; } = candidates;

        public int NextIndex { get; set; }
    }
}
