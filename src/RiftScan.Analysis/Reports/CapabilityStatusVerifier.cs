using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Reports;

public sealed class CapabilityStatusVerifier
{
    private static readonly HashSet<string> RequiredCapabilities = new(StringComparer.OrdinalIgnoreCase)
    {
        "read_only_process_inventory",
        "passive_capture",
        "windowed_plan_capture",
        "session_integrity_verify",
        "offline_dynamic_region_triage",
        "cluster_structure_detection",
        "entity_layout_detection",
        "vec3_behavior_heuristics",
        "scalar_lane_analysis",
        "scalar_behavior_heuristics",
        "scalar_evidence_set_aggregation",
        "scalar_evidence_set_verify",
        "scalar_truth_export_and_recovery",
        "external_corroboration_hook",
        "scalar_truth_promotion_review",
        "comparison_truth_readiness_export",
        "comparison_truth_readiness_verify"
    };

    private static readonly HashSet<string> RequiredTruthComponents = new(StringComparer.OrdinalIgnoreCase)
    {
        "entity_layout",
        "position",
        "actor_yaw",
        "camera_orientation"
    };

    public CapabilityStatusVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<CapabilityStatusVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Capability status file does not exist."));
            return new CapabilityStatusVerificationResult { Path = fullPath, Issues = issues };
        }

        CapabilityStatusResult? result;
        try
        {
            result = JsonSerializer.Deserialize<CapabilityStatusResult>(File.ReadAllText(fullPath), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid capability status JSON: {ex.Message}"));
            return new CapabilityStatusVerificationResult { Path = fullPath, Issues = issues };
        }

        if (result is null)
        {
            issues.Add(Error("json_empty", "Capability status JSON did not contain an object."));
            return new CapabilityStatusVerificationResult { Path = fullPath, Issues = issues };
        }

        ValidateResult(result, issues);
        return new CapabilityStatusVerificationResult
        {
            Path = fullPath,
            CapabilityCount = result.CapabilityCount,
            Issues = issues
        };
    }

    private static void ValidateResult(CapabilityStatusResult result, ICollection<CapabilityStatusVerificationIssue> issues)
    {
        if (!string.Equals(result.SchemaVersion, "riftscan.capability_status.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.capability_status.v1."));
        }

        if (!result.Success)
        {
            issues.Add(Error("result_not_successful", "success must be true for a usable capability status packet."));
        }

        if (result.CapabilityCount != result.Capabilities.Count)
        {
            issues.Add(Error("capability_count_mismatch", "capability_count must match capabilities length."));
        }

        if (!result.Warnings.Contains("capability_status_reports_coded_surfaces_and_evidence_readiness_not_recovered_truth", StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(Error("truth_claim_warning_missing", "warnings must include capability_status_reports_coded_surfaces_and_evidence_readiness_not_recovered_truth."));
        }

        ValidateCapabilities(result.Capabilities, issues);
        ValidateTruthComponents(result.TruthComponents, issues);

        if (result.NextRecommendedActions.Count == 0)
        {
            issues.Add(Error("next_recommended_actions_missing", "next_recommended_actions must include at least one action."));
        }
    }

    private static void ValidateCapabilities(
        IReadOnlyList<CapabilityStatusEntry> capabilities,
        ICollection<CapabilityStatusVerificationIssue> issues)
    {
        var names = capabilities
            .Select(capability => capability.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var required in RequiredCapabilities)
        {
            if (!names.Contains(required))
            {
                issues.Add(Error("required_capability_missing", $"Required capability is missing: {required}."));
            }
        }

        foreach (var capability in capabilities)
        {
            Require(capability.Name, "capability_name_missing", "capability.name is required.", issues);
            Require(capability.Status, "capability_status_missing", $"capability.status is required for {capability.Name}.", issues);
            Require(capability.PrimaryCommand, "capability_primary_command_missing", $"capability.primary_command is required for {capability.Name}.", issues);
            Require(capability.EvidenceSurface, "capability_evidence_surface_missing", $"capability.evidence_surface is required for {capability.Name}.", issues);
            if (capability.OutputArtifacts.Count == 0)
            {
                issues.Add(Error("capability_output_artifacts_missing", $"capability.output_artifacts must not be empty for {capability.Name}."));
            }
        }
    }

    private static void ValidateTruthComponents(
        IReadOnlyList<CapabilityTruthComponentStatus> truthComponents,
        ICollection<CapabilityStatusVerificationIssue> issues)
    {
        if (truthComponents.Count == 0)
        {
            return;
        }

        var names = truthComponents
            .Select(component => component.Component)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var required in RequiredTruthComponents)
        {
            if (!names.Contains(required))
            {
                issues.Add(Error("required_truth_component_missing", $"Required truth component is missing: {required}."));
            }
        }

        foreach (var component in truthComponents)
        {
            Require(component.Component, "truth_component_missing", "truth_components.component is required.", issues);
            Require(component.CodeStatus, "truth_component_code_status_missing", $"truth_components.code_status is required for {component.Component}.", issues);
            Require(component.EvidenceReadiness, "truth_component_evidence_readiness_missing", $"truth_components.evidence_readiness is required for {component.Component}.", issues);
            Require(component.NextAction, "truth_component_next_action_missing", $"truth_components.next_action is required for {component.Component}.", issues);
            if (component.EvidenceCount < 0)
            {
                issues.Add(Error("truth_component_evidence_count_invalid", $"truth_components.evidence_count must not be negative for {component.Component}."));
            }
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        ICollection<CapabilityStatusVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message));
        }
    }

    private static CapabilityStatusVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message
        };
}
