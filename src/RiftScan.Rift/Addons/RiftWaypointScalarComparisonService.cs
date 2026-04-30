using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftWaypointScalarComparisonService
{
    private const string WaypointXAxis = "waypoint_x";
    private const string WaypointZAxis = "waypoint_z";

    public RiftWaypointScalarComparisonResult Compare(RiftWaypointScalarComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.InputPaths.Count < 2)
        {
            throw new ArgumentException("At least two waypoint scalar match JSON files are required.", nameof(options.InputPaths));
        }

        if (double.IsNaN(options.DeltaTolerance) || double.IsInfinity(options.DeltaTolerance) || options.DeltaTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DeltaTolerance), "Delta tolerance must be finite and non-negative.");
        }

        if (options.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Top), "Top must be positive.");
        }

        var inputPaths = options.InputPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToArray();
        if (inputPaths.Length < 2)
        {
            throw new ArgumentException("At least two non-empty waypoint scalar match JSON files are required.", nameof(options.InputPaths));
        }

        var warnings = new List<string> { "waypoint_scalar_comparison_is_validation_evidence_not_final_truth" };
        var diagnostics = new List<string>
        {
            "offline_scalar_result_comparison_only",
            "uses_scalar_hits_output_path_when_present_else_embedded_scalar_hits",
            "classification_requires_replayable_capture_context"
        };
        var inputs = inputPaths.Select((path, index) => ReadInput(path, index + 1, warnings)).ToArray();
        var comparisons = BuildComparisons(inputs, options.DeltaTolerance).ToArray();
        var ranked = comparisons
            .OrderBy(candidate => ClassificationRank(candidate.Classification))
            .ThenByDescending(candidate => candidate.SupportCount)
            .ThenBy(candidate => candidate.DeltaError ?? double.MaxValue)
            .ThenBy(candidate => candidate.Axis, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.SourceBaseAddressHex))
            .ThenBy(candidate => ParseUnsignedHexOrDecimal(candidate.SourceOffsetHex))
            .ToArray();
        var topComparisons = ranked
            .Take(options.Top)
            .Select((candidate, index) => candidate with { CandidateId = $"rift-waypoint-scalar-comparison-{index + 1:000000}" })
            .ToArray();

        if (ranked.Length > topComparisons.Length)
        {
            warnings.Add("comparison_output_truncated_by_top_limit");
        }

        if (inputs.Any(input => input.ExpectedScalarHitCount > input.ScalarHits.Count))
        {
            warnings.Add("one_or_more_inputs_have_truncated_scalar_hit_outputs");
        }

        if (topComparisons.Length == 0)
        {
            warnings.Add("no_emitted_scalar_hits_available_for_comparison");
        }

        var classificationCounts = ranked
            .GroupBy(candidate => candidate.Classification, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new()
        {
            Success = true,
            InputPaths = inputPaths,
            AnalyzerSources = inputPaths
                .Concat(inputs.Select(input => input.Result.AnchorPath).Where(path => !string.IsNullOrWhiteSpace(path)))
                .Concat(inputs.Select(input => input.ScalarHitsSourcePath).OfType<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            DeltaTolerance = options.DeltaTolerance,
            TopLimit = options.Top,
            InputCount = inputs.Length,
            InputSummaries = inputs.Select(input => input.Summary).ToArray(),
            ComparisonCount = ranked.Length,
            ClassificationCounts = classificationCounts,
            Comparisons = topComparisons,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static ComparisonInput ReadInput(string path, int inputIndex, ICollection<string> warnings)
    {
        var result = JsonSerializer.Deserialize<RiftSessionWaypointScalarMatchResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException($"Unable to read waypoint scalar match result: {path}");
        var (scalarHits, scalarHitsSourcePath) = ReadScalarHitsForComparison(path, result);
        var anchorPath = Path.GetFullPath(result.AnchorPath);
        var anchors = LoadAnchors(anchorPath)
            .Select((anchor, index) => string.IsNullOrWhiteSpace(anchor.AnchorId)
                ? anchor with { AnchorId = $"rift-addon-waypoint-anchor-{index + 1:000000}" }
                : anchor)
            .Where(IsValidAnchor)
            .ToArray();
        var primaryAnchor = anchors.FirstOrDefault();
        var expectedScalarHitCount = result.RetainedScalarHitCount > 0 ? result.RetainedScalarHitCount : result.ScalarHitCount;
        if (expectedScalarHitCount > scalarHits.Count)
        {
            warnings.Add($"input_{inputIndex}_scalar_hits_truncated");
        }

        if (result.RetainedScalarHitCount > 0 && result.ScalarHitCount > result.RetainedScalarHitCount)
        {
            warnings.Add($"input_{inputIndex}_scalar_hits_limited_by_snapshot_axis_retention");
        }

        if (primaryAnchor is null)
        {
            warnings.Add($"input_{inputIndex}_has_no_valid_waypoint_anchor");
        }

        var summary = new RiftWaypointScalarComparisonInputSummary
        {
            InputIndex = inputIndex,
            InputPath = path,
            SessionId = result.SessionId,
            SessionPath = result.SessionPath,
            AnchorPath = anchorPath,
            PrimaryAnchorId = primaryAnchor?.AnchorId ?? string.Empty,
            PrimaryWaypointX = primaryAnchor?.WaypointX,
            PrimaryWaypointZ = primaryAnchor?.WaypointZ,
            PrimaryDeltaX = primaryAnchor?.DeltaX,
            PrimaryDeltaZ = primaryAnchor?.DeltaZ,
            ScalarHitCount = result.ScalarHitCount,
            EmittedScalarHitCount = result.ScalarHits.Count,
            ComparisonScalarHitCount = scalarHits.Count,
            ScalarHitsOutputPath = scalarHitsSourcePath,
            ScalarAxisHitCounts = result.ScalarAxisHitCounts,
            PairCandidateCount = result.PairCandidateCount,
            Warnings = result.Warnings
        };

        return new(inputIndex, path, result, anchors, summary, scalarHits, scalarHitsSourcePath, expectedScalarHitCount);
    }

    private static (IReadOnlyList<RiftSessionWaypointScalarHit> Hits, string? SourcePath) ReadScalarHitsForComparison(
        string inputPath,
        RiftSessionWaypointScalarMatchResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ScalarHitsOutputPath))
        {
            return (result.ScalarHits, null);
        }

        var fullPath = ResolveRelatedPath(inputPath, result.ScalarHitsOutputPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Waypoint scalar hits output path from match result does not exist.", fullPath);
        }

        var hits = File.ReadLines(fullPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<RiftSessionWaypointScalarHit>(line, SessionJson.Options)
                ?? throw new InvalidOperationException($"Unable to read waypoint scalar hit JSONL entry from {fullPath}."))
            .ToArray();
        return (hits, fullPath);
    }

    private static string ResolveRelatedPath(string inputPath, string outputPath)
    {
        if (Path.IsPathFullyQualified(outputPath))
        {
            return Path.GetFullPath(outputPath);
        }

        var inputDirectory = Path.GetDirectoryName(inputPath);
        return Path.GetFullPath(Path.Combine(inputDirectory ?? Environment.CurrentDirectory, outputPath));
    }

    private static IReadOnlyList<RiftAddonWaypointAnchor> LoadAnchors(string anchorPath)
    {
        var scanResult = JsonSerializer.Deserialize<RiftAddonApiObservationScanResult>(File.ReadAllText(anchorPath), SessionJson.Options)
            ?? throw new InvalidOperationException($"Unable to read waypoint anchor scan result: {anchorPath}");
        return scanResult.WaypointAnchors.Count > 0
            ? scanResult.WaypointAnchors
            : RiftAddonApiObservationService.BuildWaypointAnchors(scanResult.Observations);
    }

    private static IEnumerable<RiftWaypointScalarComparisonCandidate> BuildComparisons(IReadOnlyList<ComparisonInput> inputs, double deltaTolerance)
    {
        var observationsByKey = new Dictionary<string, Dictionary<int, ScalarObservation>>(StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            var bestHits = input.ScalarHits
                .GroupBy(BuildScalarKey, StringComparer.Ordinal)
                .Select(group => group
                    .OrderBy(hit => hit.AbsDistance)
                    .ThenBy(hit => hit.SnapshotId, StringComparer.OrdinalIgnoreCase)
                    .First());
            foreach (var hit in bestHits)
            {
                var key = BuildScalarKey(hit);
                if (!observationsByKey.TryGetValue(key, out var observationsByInput))
                {
                    observationsByInput = new Dictionary<int, ScalarObservation>();
                    observationsByKey.Add(key, observationsByInput);
                }

                observationsByInput[input.InputIndex] = new ScalarObservation(input.InputIndex, hit);
            }
        }

        var allInputIndexes = inputs.Select(input => input.InputIndex).ToArray();
        foreach (var (_, observationsByInput) in observationsByKey)
        {
            var observations = observationsByInput.Values.OrderBy(observation => observation.InputIndex).ToArray();
            if (observations.Length == 0)
            {
                continue;
            }

            var baseline = observations[0];
            var latest = observations[^1];
            var presentInputIndexes = observations.Select(observation => observation.InputIndex).ToArray();
            var missingInputIndexes = allInputIndexes.Except(presentInputIndexes).ToArray();
            var sourceRegionIds = observations
                .Select(observation => observation.Hit.SourceRegionId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var observedDelta = observations.Length >= 2
                ? latest.Hit.MemoryValue - baseline.Hit.MemoryValue
                : (double?)null;
            var waypointDelta = observations.Length >= 2
                ? latest.Hit.AnchorValue - baseline.Hit.AnchorValue
                : InferWaypointDelta(inputs, baseline.Hit.Axis)
                    ?? (missingInputIndexes.Length > 0 ? MissingInputWaypointDelta(inputs, observations, baseline.Hit.Axis) : null);
            var deltaError = observedDelta.HasValue && waypointDelta.HasValue
                ? Math.Abs(observedDelta.Value - waypointDelta.Value)
                : (double?)null;
            var classification = Classify(observations, missingInputIndexes, observedDelta, waypointDelta, deltaError, deltaTolerance);
            var validationStatus = classification switch
            {
                "tracks_waypoint_candidate" => "candidate_supported_by_waypoint_delta",
                "missing_after_waypoint_change" => "candidate_rejected_missing_after_waypoint_change",
                "stable_despite_waypoint_change" => "candidate_rejected_static_against_waypoint_delta",
                "changes_but_not_waypoint" => "candidate_rejected_delta_mismatch",
                _ => "candidate_unverified"
            };

            yield return new RiftWaypointScalarComparisonCandidate
            {
                Axis = baseline.Hit.Axis,
                SourceBaseAddressHex = baseline.Hit.SourceBaseAddressHex,
                SourceOffsetHex = baseline.Hit.SourceOffsetHex,
                SourceRegionIds = sourceRegionIds,
                PresentInputIndexes = presentInputIndexes,
                MissingInputIndexes = missingInputIndexes,
                SupportCount = observations.Length,
                BaselineMemoryValue = baseline.Hit.MemoryValue,
                LatestMemoryValue = latest.Hit.MemoryValue,
                BaselineAnchorValue = baseline.Hit.AnchorValue,
                LatestAnchorValue = latest.Hit.AnchorValue,
                ObservedDelta = observedDelta,
                WaypointDelta = waypointDelta,
                DeltaError = deltaError,
                BestAbsDistance = observations.Min(observation => observation.Hit.AbsDistance),
                Classification = classification,
                ValidationStatus = validationStatus,
                EvidenceSummary = BuildEvidenceSummary(baseline.Hit, observations.Length, missingInputIndexes, observedDelta, waypointDelta, deltaError, classification)
            };
        }
    }

    private static double? InferWaypointDelta(IReadOnlyList<ComparisonInput> inputs, string axis)
    {
        var first = inputs.FirstOrDefault(input => AxisAnchorValue(input.Summary, axis).HasValue);
        var last = inputs.LastOrDefault(input => AxisAnchorValue(input.Summary, axis).HasValue);
        if (first is null || last is null)
        {
            return null;
        }

        return AxisAnchorValue(last.Summary, axis)!.Value - AxisAnchorValue(first.Summary, axis)!.Value;
    }

    private static double? MissingInputWaypointDelta(IReadOnlyList<ComparisonInput> inputs, IReadOnlyList<ScalarObservation> observations, string axis)
    {
        var baselineInput = inputs.FirstOrDefault(input => input.InputIndex == observations[0].InputIndex);
        var latestInput = inputs.LastOrDefault(input => AxisAnchorValue(input.Summary, axis).HasValue);
        if (baselineInput is null || latestInput is null)
        {
            return null;
        }

        var baseline = AxisAnchorValue(baselineInput.Summary, axis);
        var latest = AxisAnchorValue(latestInput.Summary, axis);
        return baseline.HasValue && latest.HasValue ? latest.Value - baseline.Value : null;
    }

    private static double? AxisAnchorValue(RiftWaypointScalarComparisonInputSummary summary, string axis) =>
        string.Equals(axis, WaypointXAxis, StringComparison.OrdinalIgnoreCase)
            ? summary.PrimaryWaypointX
            : string.Equals(axis, WaypointZAxis, StringComparison.OrdinalIgnoreCase)
                ? summary.PrimaryWaypointZ
                : null;

    private static string Classify(
        IReadOnlyList<ScalarObservation> observations,
        IReadOnlyList<int> missingInputIndexes,
        double? observedDelta,
        double? waypointDelta,
        double? deltaError,
        double deltaTolerance)
    {
        if (missingInputIndexes.Count > 0)
        {
            return waypointDelta.HasValue && Math.Abs(waypointDelta.Value) > deltaTolerance
                ? "missing_after_waypoint_change"
                : "missing_in_one_or_more_inputs";
        }

        if (!observedDelta.HasValue || !waypointDelta.HasValue || !deltaError.HasValue)
        {
            return "insufficient_delta_evidence";
        }

        if (deltaError.Value <= deltaTolerance)
        {
            return "tracks_waypoint_candidate";
        }

        if (Math.Abs(waypointDelta.Value) > deltaTolerance && Math.Abs(observedDelta.Value) <= deltaTolerance)
        {
            return "stable_despite_waypoint_change";
        }

        return "changes_but_not_waypoint";
    }

    private static string BuildEvidenceSummary(
        RiftSessionWaypointScalarHit baseline,
        int supportCount,
        IReadOnlyList<int> missingInputIndexes,
        double? observedDelta,
        double? waypointDelta,
        double? deltaError,
        string classification)
    {
        var missing = missingInputIndexes.Count == 0 ? "none" : string.Join(",", missingInputIndexes);
        var observed = observedDelta.HasValue ? observedDelta.Value.ToString("F6", CultureInfo.InvariantCulture) : "null";
        var waypoint = waypointDelta.HasValue ? waypointDelta.Value.ToString("F6", CultureInfo.InvariantCulture) : "null";
        var error = deltaError.HasValue ? deltaError.Value.ToString("F6", CultureInfo.InvariantCulture) : "null";
        return $"axis={baseline.Axis};source={baseline.SourceBaseAddressHex}+{baseline.SourceOffsetHex};support={supportCount};missing={missing};observed_delta={observed};waypoint_delta={waypoint};delta_error={error};classification={classification}";
    }

    private static int ClassificationRank(string classification) =>
        classification switch
        {
            "tracks_waypoint_candidate" => 0,
            "changes_but_not_waypoint" => 1,
            "stable_despite_waypoint_change" => 2,
            "missing_after_waypoint_change" => 3,
            "missing_in_one_or_more_inputs" => 4,
            _ => 5
        };

    private static string BuildScalarKey(RiftSessionWaypointScalarHit hit) =>
        string.Join('|', hit.Axis.ToLowerInvariant(), hit.SourceBaseAddressHex.ToUpperInvariant(), hit.SourceOffsetHex.ToUpperInvariant());

    private static bool IsValidAnchor(RiftAddonWaypointAnchor anchor) =>
        double.IsFinite(anchor.WaypointX) &&
        double.IsFinite(anchor.WaypointZ) &&
        !string.IsNullOrWhiteSpace(anchor.AnchorId);

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private sealed record ComparisonInput(
        int InputIndex,
        string InputPath,
        RiftSessionWaypointScalarMatchResult Result,
        IReadOnlyList<RiftAddonWaypointAnchor> Anchors,
        RiftWaypointScalarComparisonInputSummary Summary,
        IReadOnlyList<RiftSessionWaypointScalarHit> ScalarHits,
        string? ScalarHitsSourcePath,
        int ExpectedScalarHitCount);

    private sealed record ScalarObservation(int InputIndex, RiftSessionWaypointScalarHit Hit);
}
