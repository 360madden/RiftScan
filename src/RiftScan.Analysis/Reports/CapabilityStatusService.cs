using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Reports;

public sealed class CapabilityStatusService
{
    public CapabilityStatusResult Build(string? truthReadinessPath = null, string? scalarEvidenceSetPath = null)
    {
        var truthReadinessPaths = string.IsNullOrWhiteSpace(truthReadinessPath)
            ? Array.Empty<string>()
            : [truthReadinessPath];
        var scalarEvidenceSetPaths = string.IsNullOrWhiteSpace(scalarEvidenceSetPath)
            ? Array.Empty<string>()
            : [scalarEvidenceSetPath];
        return Build(truthReadinessPaths, scalarEvidenceSetPaths, [], []);
    }

    public CapabilityStatusResult Build(string? truthReadinessPath, IReadOnlyList<string> scalarEvidenceSetPaths)
    {
        var truthReadinessPaths = string.IsNullOrWhiteSpace(truthReadinessPath)
            ? Array.Empty<string>()
            : [truthReadinessPath];
        return Build(truthReadinessPaths, scalarEvidenceSetPaths, [], []);
    }

    public CapabilityStatusResult Build(
        IReadOnlyList<string> truthReadinessPaths,
        IReadOnlyList<string> scalarEvidenceSetPaths) =>
        Build(truthReadinessPaths, scalarEvidenceSetPaths, [], []);

    public CapabilityStatusResult Build(
        IReadOnlyList<string> truthReadinessPaths,
        IReadOnlyList<string> scalarEvidenceSetPaths,
        IReadOnlyList<string> scalarTruthRecoveryPaths) =>
        Build(truthReadinessPaths, scalarEvidenceSetPaths, scalarTruthRecoveryPaths, []);

    public CapabilityStatusResult Build(
        IReadOnlyList<string> truthReadinessPaths,
        IReadOnlyList<string> scalarEvidenceSetPaths,
        IReadOnlyList<string> scalarTruthRecoveryPaths,
        IReadOnlyList<string> scalarTruthPromotionPaths)
    {
        var capabilities = BuildCapabilities();
        var warnings = new List<string>
        {
            "capability_status_reports_coded_surfaces_and_evidence_readiness_not_recovered_truth"
        };
        IReadOnlyList<CapabilityTruthComponentStatus> truthComponents = [];
        IReadOnlyList<string> evidenceMissing = [
            "truth_readiness_packet_not_supplied"
        ];
        IReadOnlyList<string> nextActions = [
            "generate_or_supply_comparison_truth_readiness_packet"
        ];
        var fullReadinessPaths = new List<string>();
        var nextRequiredCaptureModes = new List<string>();
        var fullScalarEvidenceSetPaths = new List<string>();
        var scalarEvidenceSets = new List<ScalarEvidenceSetResult>();
        var fullScalarTruthRecoveryPaths = new List<string>();
        var fullScalarTruthPromotionPaths = new List<string>();

        foreach (var truthReadinessPath in truthReadinessPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullReadinessPath = Path.GetFullPath(truthReadinessPath);
            fullReadinessPaths.Add(fullReadinessPath);
            var readiness = ReadReadiness(fullReadinessPath);
            if (!string.IsNullOrWhiteSpace(readiness.NextRequiredCapture.Mode))
            {
                nextRequiredCaptureModes.Add(readiness.NextRequiredCapture.Mode);
            }

            truthComponents = MergeTruthReadinessComponents(truthComponents, readiness);
            evidenceMissing = truthComponents
                .Where(component => !IsStrongOrValidated(component.EvidenceReadiness))
                .Select(component => $"{component.Component}:{component.EvidenceReadiness}")
                .ToArray();
            nextActions = BuildNextActions(nextRequiredCaptureModes, truthComponents, scalarEvidenceSets);
            warnings = warnings
                .Concat(readiness.Warnings)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        foreach (var scalarEvidenceSetPath in scalarEvidenceSetPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullScalarEvidenceSetPath = Path.GetFullPath(scalarEvidenceSetPath);
            fullScalarEvidenceSetPaths.Add(fullScalarEvidenceSetPath);
            var scalarEvidenceSet = ReadScalarEvidenceSet(fullScalarEvidenceSetPath);
            scalarEvidenceSets.Add(scalarEvidenceSet);
            truthComponents = MergeScalarEvidenceSetComponents(truthComponents, scalarEvidenceSet);
            evidenceMissing = truthComponents
                .Where(component => !IsStrongOrValidated(component.EvidenceReadiness))
                .Select(component => $"{component.Component}:{component.EvidenceReadiness}")
                .ToArray();
            nextActions = BuildNextActions(nextRequiredCaptureModes, truthComponents, scalarEvidenceSets);
            warnings = warnings
                .Concat(scalarEvidenceSet.Warnings)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        foreach (var scalarTruthRecoveryPath in scalarTruthRecoveryPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullScalarTruthRecoveryPath = Path.GetFullPath(scalarTruthRecoveryPath);
            fullScalarTruthRecoveryPaths.Add(fullScalarTruthRecoveryPath);
            var scalarTruthRecovery = ReadScalarTruthRecovery(fullScalarTruthRecoveryPath);
            truthComponents = MergeScalarTruthRecoveryComponents(truthComponents, scalarTruthRecovery);
            evidenceMissing = truthComponents
                .Where(component => !IsStrongOrValidated(component.EvidenceReadiness))
                .Select(component => $"{component.Component}:{component.EvidenceReadiness}")
                .ToArray();
            nextActions = BuildNextActions(nextRequiredCaptureModes, truthComponents, scalarEvidenceSets);
            warnings = warnings
                .Concat(scalarTruthRecovery.Warnings)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        foreach (var scalarTruthPromotionPath in scalarTruthPromotionPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullScalarTruthPromotionPath = Path.GetFullPath(scalarTruthPromotionPath);
            fullScalarTruthPromotionPaths.Add(fullScalarTruthPromotionPath);
            var scalarTruthPromotion = ReadScalarTruthPromotion(fullScalarTruthPromotionPath);
            truthComponents = MergeScalarTruthPromotionComponents(truthComponents, scalarTruthPromotion);
            evidenceMissing = truthComponents
                .Where(component => !IsStrongOrValidated(component.EvidenceReadiness))
                .Select(component => $"{component.Component}:{component.EvidenceReadiness}")
                .ToArray();
            nextActions = BuildNextActions(nextRequiredCaptureModes, truthComponents, scalarEvidenceSets);
            warnings = warnings
                .Concat(scalarTruthPromotion.Warnings)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new CapabilityStatusResult
        {
            Capabilities = capabilities,
            TruthReadinessPath = fullReadinessPaths.Count == 0 ? null : fullReadinessPaths[0],
            TruthReadinessPaths = fullReadinessPaths,
            ScalarEvidenceSetPath = fullScalarEvidenceSetPaths.Count == 0 ? null : fullScalarEvidenceSetPaths[0],
            ScalarEvidenceSetPaths = fullScalarEvidenceSetPaths,
            ScalarTruthRecoveryPath = fullScalarTruthRecoveryPaths.Count == 0 ? null : fullScalarTruthRecoveryPaths[0],
            ScalarTruthRecoveryPaths = fullScalarTruthRecoveryPaths,
            ScalarTruthPromotionPath = fullScalarTruthPromotionPaths.Count == 0 ? null : fullScalarTruthPromotionPaths[0],
            ScalarTruthPromotionPaths = fullScalarTruthPromotionPaths,
            TruthComponents = truthComponents,
            EvidenceMissing = evidenceMissing,
            NextRecommendedActions = nextActions,
            Warnings = NormalizeWarnings(warnings, truthComponents)
        };
    }

    private static IReadOnlyList<CapabilityStatusEntry> BuildCapabilities() =>
    [
        Capability("read_only_process_inventory", "riftscan process inventory", "process inventory JSON", ["process-inventory.json"], ""),
        Capability("passive_capture", "riftscan capture passive", "immutable session artifacts", ["manifest.json", "regions.json", "snapshots/index.jsonl", "snapshots/*.bin", "checksums.json"], ""),
        Capability("windowed_plan_capture", "riftscan capture plan", "targeted follow-up sessions from next-capture plans", ["manifest.json", "snapshots/*.bin"], ""),
        Capability("session_integrity_verify", "riftscan verify session", "schema and checksum verification result", ["checksums.json", "verification JSON"], ""),
        Capability("offline_dynamic_region_triage", "riftscan analyze session", "ranked dynamic region JSONL", ["triage.jsonl"], ""),
        Capability("cluster_structure_detection", "riftscan analyze session", "cluster and structure JSONL", ["clusters.jsonl", "structures.jsonl"], ""),
        Capability("entity_layout_detection", "riftscan analyze session", "entity layout candidate JSONL", ["entity_layout_candidates.jsonl"], "requires cross-session or labeled behavior before truth claim"),
        Capability("vec3_behavior_heuristics", "riftscan compare sessions", "passive-vs-move vec3 contrast summary", ["vec3_behavior_summary"], "requires labeled move-forward evidence"),
        Capability("scalar_lane_analysis", "riftscan analyze session", "stable scalar lane JSONL", ["scalar_candidates.jsonl"], "requires labeled turn/camera evidence for yaw/camera claims"),
        Capability("scalar_behavior_heuristics", "riftscan compare sessions", "turn/camera scalar behavior summary", ["scalar_behavior_summary"], "requires opposing turn and camera-only separation evidence"),
        Capability("scalar_evidence_set_aggregation", "riftscan compare scalar-set", "multi-session scalar evidence set", ["scalar-evidence-set.json"], "requires complete passive/turn-left/turn-right/camera-only set"),
        Capability("scalar_evidence_set_verify", "riftscan verify scalar-evidence-set", "scalar evidence set invariant check", ["scalar_evidence_set_verification.v1"], ""),
        Capability("scalar_truth_export_and_recovery", "riftscan compare scalar-set --truth-out; riftscan compare scalar-truth", "truth-candidate and repeat recovery packets", ["scalar_truth_candidates.jsonl", "scalar-truth-recovery.json"], "candidate evidence until externally corroborated or repeated"),
        Capability("external_corroboration_hook", "riftscan verify scalar-corroboration", "addon/external corroboration JSONL verification", ["scalar_truth_corroboration.jsonl"], ""),
        Capability("scalar_truth_promotion_review", "riftscan compare scalar-promotion; riftscan review scalar-promotion", "recovered-plus-corroborated scalar promotion and manual review packets", ["scalar-truth-promotion.json", "scalar-promotion-review.json", "scalar-promotion-review.md"], "manual confirmation still required before final truth claim"),
        Capability("comparison_truth_readiness_export", "riftscan compare sessions --truth-readiness", "comparison readiness packet", ["truth-readiness.json"], "readiness is not recovered truth"),
        Capability("comparison_truth_readiness_verify", "riftscan verify comparison-readiness", "readiness packet invariant check", ["comparison_truth_readiness_verification.v1"], "")
    ];

    private static CapabilityStatusEntry Capability(
        string name,
        string command,
        string evidenceSurface,
        IReadOnlyList<string> artifacts,
        string remainingGap) =>
        new()
        {
            Name = name,
            PrimaryCommand = command,
            EvidenceSurface = evidenceSurface,
            OutputArtifacts = artifacts,
            RemainingGap = remainingGap
        };

    private static ComparisonTruthReadinessResult ReadReadiness(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Truth-readiness file does not exist: {path}");
        }

        var verification = new ComparisonTruthReadinessVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Truth-readiness verification failed: {issues}");
        }

        return JsonSerializer.Deserialize<ComparisonTruthReadinessResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize truth-readiness packet.");
    }

    private static ScalarEvidenceSetResult ReadScalarEvidenceSet(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Scalar evidence set file does not exist: {path}");
        }

        var verification = new ScalarEvidenceSetVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar evidence set verification failed: {issues}");
        }

        try
        {
            return JsonSerializer.Deserialize<ScalarEvidenceSetResult>(File.ReadAllText(path), SessionJson.Options)
                ?? throw new InvalidOperationException("Could not deserialize scalar evidence set packet.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid scalar evidence set JSON: {ex.Message}", ex);
        }
    }

    private static ScalarTruthRecoveryResult ReadScalarTruthRecovery(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Scalar truth recovery file does not exist: {path}");
        }

        var verification = new ScalarTruthRecoveryVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar truth recovery verification failed: {issues}");
        }

        try
        {
            return JsonSerializer.Deserialize<ScalarTruthRecoveryResult>(File.ReadAllText(path), SessionJson.Options)
                ?? throw new InvalidOperationException("Could not deserialize scalar truth recovery packet.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid scalar truth recovery JSON: {ex.Message}", ex);
        }
    }

    private static ScalarTruthPromotionResult ReadScalarTruthPromotion(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Scalar truth promotion file does not exist: {path}");
        }

        var verification = new ScalarTruthPromotionVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Scalar truth promotion verification failed: {issues}");
        }

        try
        {
            return JsonSerializer.Deserialize<ScalarTruthPromotionResult>(File.ReadAllText(path), SessionJson.Options)
                ?? throw new InvalidOperationException("Could not deserialize scalar truth promotion packet.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid scalar truth promotion JSON: {ex.Message}", ex);
        }
    }

    private static IReadOnlyList<CapabilityTruthComponentStatus> BuildTruthComponents(ComparisonTruthReadinessResult readiness) =>
    [
        TruthComponent(readiness.EntityLayout),
        TruthComponent(readiness.Position),
        TruthComponent(readiness.ActorYaw),
        TruthComponent(readiness.CameraOrientation)
    ];

    private static CapabilityTruthComponentStatus TruthComponent(ComparisonTruthReadinessStatus status) =>
        new()
        {
            Component = status.Component,
            CodeStatus = "coded",
            EvidenceReadiness = status.Readiness,
            EvidenceCount = status.EvidenceCount,
            NextAction = status.NextAction
        };

    private static IReadOnlyList<CapabilityTruthComponentStatus> MergeTruthReadinessComponents(
        IReadOnlyList<CapabilityTruthComponentStatus> existingComponents,
        ComparisonTruthReadinessResult readiness)
    {
        var components = EnsureTruthComponents(existingComponents).ToDictionary(component => component.Component, StringComparer.OrdinalIgnoreCase);
        foreach (var component in BuildTruthComponents(readiness))
        {
            MergeComponent(components, component);
        }

        return OrderedTruthComponents(components);
    }

    private static IReadOnlyList<CapabilityTruthComponentStatus> MergeScalarEvidenceSetComponents(
        IReadOnlyList<CapabilityTruthComponentStatus> existingComponents,
        ScalarEvidenceSetResult scalarEvidenceSet)
    {
        var components = EnsureTruthComponents(existingComponents).ToDictionary(component => component.Component, StringComparer.OrdinalIgnoreCase);
        MergeComponent(components, BuildScalarComponent("actor_yaw", scalarEvidenceSet));
        MergeComponent(components, BuildScalarComponent("camera_orientation", scalarEvidenceSet));
        return OrderedTruthComponents(components);
    }

    private static IReadOnlyList<CapabilityTruthComponentStatus> MergeScalarTruthRecoveryComponents(
        IReadOnlyList<CapabilityTruthComponentStatus> existingComponents,
        ScalarTruthRecoveryResult scalarTruthRecovery)
    {
        var components = EnsureTruthComponents(existingComponents).ToDictionary(component => component.Component, StringComparer.OrdinalIgnoreCase);
        MergeComponent(components, BuildRecoveredScalarComponent("actor_yaw", scalarTruthRecovery));
        MergeComponent(components, BuildRecoveredScalarComponent("camera_orientation", scalarTruthRecovery));
        return OrderedTruthComponents(components);
    }

    private static IReadOnlyList<CapabilityTruthComponentStatus> MergeScalarTruthPromotionComponents(
        IReadOnlyList<CapabilityTruthComponentStatus> existingComponents,
        ScalarTruthPromotionResult scalarTruthPromotion)
    {
        var components = EnsureTruthComponents(existingComponents).ToDictionary(component => component.Component, StringComparer.OrdinalIgnoreCase);
        MergeComponent(components, BuildPromotedScalarComponent("actor_yaw", scalarTruthPromotion));
        MergeComponent(components, BuildPromotedScalarComponent("camera_orientation", scalarTruthPromotion));
        return OrderedTruthComponents(components);
    }

    private static IReadOnlyList<CapabilityTruthComponentStatus> OrderedTruthComponents(
        IReadOnlyDictionary<string, CapabilityTruthComponentStatus> components) =>
        new[] { "entity_layout", "position", "actor_yaw", "camera_orientation" }
            .Select(component => components[component])
            .ToArray();

    private static IReadOnlyList<CapabilityTruthComponentStatus> EnsureTruthComponents(IReadOnlyList<CapabilityTruthComponentStatus> components)
    {
        if (components.Count > 0)
        {
            return components;
        }

        return
        [
            UnevaluatedComponent("entity_layout", "generate_comparison_truth_readiness_from_session_comparison"),
            UnevaluatedComponent("position", "generate_comparison_truth_readiness_from_passive_vs_move_comparison"),
            UnevaluatedComponent("actor_yaw", "supply_scalar_evidence_set_with_turn_and_camera_sessions"),
            UnevaluatedComponent("camera_orientation", "supply_scalar_evidence_set_with_camera_only_session")
        ];
    }

    private static CapabilityTruthComponentStatus UnevaluatedComponent(string component, string nextAction) =>
        new()
        {
            Component = component,
            CodeStatus = "coded",
            EvidenceReadiness = "not_evaluated",
            EvidenceCount = 0,
            NextAction = nextAction
        };

    private static CapabilityTruthComponentStatus BuildScalarComponent(string component, ScalarEvidenceSetResult scalarEvidenceSet)
    {
        var candidates = scalarEvidenceSet.RankedCandidates
            .Where(candidate => candidate.Classification.Contains(component, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => ReadinessRank(candidate.TruthReadiness))
            .ThenByDescending(candidate => candidate.ScoreTotal)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new CapabilityTruthComponentStatus
            {
                Component = component,
                CodeStatus = "coded",
                EvidenceReadiness = "missing",
                EvidenceCount = 0,
                NextAction = component.Equals("actor_yaw", StringComparison.OrdinalIgnoreCase)
                    ? "capture_passive_turn_left_turn_right_and_camera_only_scalar_set"
                    : "capture_camera_only_and_turn_stable_scalar_set"
            };
        }

        var best = candidates[0];
        return new CapabilityTruthComponentStatus
        {
            Component = component,
            CodeStatus = "coded",
            EvidenceReadiness = best.TruthReadiness,
            EvidenceCount = candidates.Length,
            NextAction = string.IsNullOrWhiteSpace(best.NextValidationStep)
                ? "repeat_labeled_capture_or_validate_against_addon_truth"
                : best.NextValidationStep
        };
    }

    private static CapabilityTruthComponentStatus BuildRecoveredScalarComponent(string component, ScalarTruthRecoveryResult scalarTruthRecovery)
    {
        var candidates = scalarTruthRecovery.RecoveredCandidates
            .Where(candidate => candidate.Classification.Contains(component, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => ReadinessRank(candidate.TruthReadiness))
            .ThenByDescending(candidate => candidate.BestScoreTotal)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new CapabilityTruthComponentStatus
            {
                Component = component,
                CodeStatus = "coded",
                EvidenceReadiness = "missing",
                EvidenceCount = 0,
                NextAction = component.Equals("actor_yaw", StringComparison.OrdinalIgnoreCase)
                    ? "repeat_scalar_truth_capture_for_actor_yaw_recovery"
                    : "repeat_scalar_truth_capture_for_camera_orientation_recovery"
            };
        }

        var best = candidates[0];
        return new CapabilityTruthComponentStatus
        {
            Component = component,
            CodeStatus = "coded",
            EvidenceReadiness = best.TruthReadiness,
            EvidenceCount = candidates.Length,
            NextAction = "review_recovered_candidate_then_external_corroborate_before_final_truth_claim"
        };
    }

    private static CapabilityTruthComponentStatus BuildPromotedScalarComponent(string component, ScalarTruthPromotionResult scalarTruthPromotion)
    {
        var promoted = scalarTruthPromotion.PromotedCandidates
            .Where(candidate => candidate.Classification.Contains(component, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => ReadinessRank(candidate.TruthReadiness))
            .ThenByDescending(candidate => candidate.BestScoreTotal)
            .ToArray();
        if (promoted.Length > 0)
        {
            var best = promoted[0];
            return new CapabilityTruthComponentStatus
            {
                Component = component,
                CodeStatus = "coded",
                EvidenceReadiness = best.TruthReadiness,
                EvidenceCount = promoted.Length,
                NextAction = best.NextValidationStep
            };
        }

        var blocked = scalarTruthPromotion.BlockedCandidates
            .Where(candidate => candidate.Classification.Contains(component, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => ReadinessRank(candidate.TruthReadiness))
            .ThenByDescending(candidate => candidate.BestScoreTotal)
            .ToArray();
        if (blocked.Length > 0)
        {
            var best = blocked[0];
            return new CapabilityTruthComponentStatus
            {
                Component = component,
                CodeStatus = "coded",
                EvidenceReadiness = best.TruthReadiness,
                EvidenceCount = blocked.Length,
                NextAction = best.NextValidationStep
            };
        }

        return new CapabilityTruthComponentStatus
        {
            Component = component,
            CodeStatus = "coded",
            EvidenceReadiness = "missing",
            EvidenceCount = 0,
            NextAction = component.Equals("actor_yaw", StringComparison.OrdinalIgnoreCase)
                ? "promote_recovered_actor_yaw_with_external_corroboration"
                : "promote_recovered_camera_orientation_with_external_corroboration"
        };
    }

    private static void MergeComponent(
        IDictionary<string, CapabilityTruthComponentStatus> components,
        CapabilityTruthComponentStatus candidate)
    {
        if (!components.TryGetValue(candidate.Component, out var existing) ||
            ReadinessRank(candidate.EvidenceReadiness) > ReadinessRank(existing.EvidenceReadiness) ||
            (ReadinessRank(candidate.EvidenceReadiness) == ReadinessRank(existing.EvidenceReadiness) &&
                candidate.EvidenceCount > existing.EvidenceCount))
        {
            components[candidate.Component] = candidate;
        }
    }

    private static IReadOnlyList<string> BuildNextActions(
        IReadOnlyList<string> nextRequiredCaptureModes,
        IReadOnlyList<CapabilityTruthComponentStatus> truthComponents,
        IReadOnlyList<ScalarEvidenceSetResult> scalarEvidenceSets)
    {
        var actions = new List<string>();
        var hasReadinessGap = truthComponents.Any(component => !IsStrongOrValidated(component.EvidenceReadiness));
        if (hasReadinessGap)
        {
            actions.AddRange(nextRequiredCaptureModes.Where(mode => !string.IsNullOrWhiteSpace(mode)));
        }

        actions.AddRange(truthComponents
            .Where(component => !IsStrongOrValidated(component.EvidenceReadiness))
            .Select(component => component.NextAction)
            .Where(action => !string.IsNullOrWhiteSpace(action)));

        actions.AddRange(scalarEvidenceSets
            .SelectMany(scalarEvidenceSet => scalarEvidenceSet.RankedCandidates)
            .Where(candidate => !IsStrongOrValidated(candidate.TruthReadiness))
            .OrderByDescending(candidate => candidate.ScoreTotal)
            .Select(candidate => candidate.NextValidationStep));

        var normalized = NormalizeNextActions(actions, truthComponents)
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return normalized.Length == 0
            ? ["repeat_or_external_corroborate_validated_candidates"]
            : normalized;
    }

    private static bool IsStrongOrValidated(string readiness) =>
        string.Equals(readiness, "strong_candidate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(readiness, "validated_candidate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(readiness, "recovered_candidate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(readiness, "corroborated_candidate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(readiness, "corroborated", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> NormalizeWarnings(
        IReadOnlyList<string> warnings,
        IReadOnlyList<CapabilityTruthComponentStatus> truthComponents)
    {
        if (truthComponents.Count == 0)
        {
            return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var readinessByComponent = truthComponents.ToDictionary(
            component => component.Component,
            component => component.EvidenceReadiness,
            StringComparer.OrdinalIgnoreCase);

        return warnings
            .Where(warning => !IsStaleReadinessWarning(warning, readinessByComponent))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> NormalizeNextActions(
        IEnumerable<string> actions,
        IReadOnlyList<CapabilityTruthComponentStatus> truthComponents)
    {
        var readinessByComponent = truthComponents.ToDictionary(
            component => component.Component,
            component => component.EvidenceReadiness,
            StringComparer.OrdinalIgnoreCase);

        return actions.Where(action => !IsStaleNextAction(action, readinessByComponent));
    }

    private static bool IsStaleNextAction(
        string action,
        IReadOnlyDictionary<string, string> readinessByComponent)
    {
        if (IsComponentStrongOrValidated("actor_yaw", readinessByComponent) &&
            (action.Contains("actor_yaw", StringComparison.OrdinalIgnoreCase) ||
                (action.Contains("turn", StringComparison.OrdinalIgnoreCase) &&
                    !action.Contains("camera", StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        if (IsComponentStrongOrValidated("camera_orientation", readinessByComponent) &&
            (action.Contains("camera_orientation", StringComparison.OrdinalIgnoreCase) ||
                (action.Contains("camera", StringComparison.OrdinalIgnoreCase) &&
                    !action.Contains("turn", StringComparison.OrdinalIgnoreCase) &&
                    !action.Contains("actor_yaw", StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        return false;
    }

    private static bool IsComponentStrongOrValidated(
        string component,
        IReadOnlyDictionary<string, string> readinessByComponent) =>
        readinessByComponent.TryGetValue(component, out var readiness) && IsStrongOrValidated(readiness);

    private static bool IsStaleReadinessWarning(
        string warning,
        IReadOnlyDictionary<string, string> readinessByComponent)
    {
        const string Suffix = "_not_strongly_ready";
        if (!warning.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var component = warning[..^Suffix.Length];
        return readinessByComponent.TryGetValue(component, out var readiness) && IsStrongOrValidated(readiness);
    }

    private static int ReadinessRank(string readiness) =>
        readiness.ToLowerInvariant() switch
        {
            "corroborated" => 5,
            "corroborated_candidate" => 5,
            "blocked_conflict" => 5,
            "recovered_candidate" => 4,
            "validated_candidate" => 3,
            "strong_candidate" => 2,
            "candidate" => 1,
            _ => 0
        };
}
