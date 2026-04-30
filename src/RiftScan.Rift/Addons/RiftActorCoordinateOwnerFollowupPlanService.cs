using System.Globalization;
using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftActorCoordinateOwnerFollowupPlanService
{
    public RiftActorCoordinateOwnerFollowupPlanResult Plan(RiftActorCoordinateOwnerFollowupPlanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PacketPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.TopOffsets);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.IntervalMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxBytesPerRegion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WindowsPerRegion);

        var packetPath = Path.GetFullPath(options.PacketPath);
        var verification = new RiftActorCoordinateOwnerDiscoveryPacketVerifier().Verify(packetPath);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Actor-coordinate owner discovery packet verification failed: {issues}");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(packetPath));
        var root = document.RootElement;
        var targetBaseAddress = ParseUnsignedHexOrDecimal(verification.TargetBaseAddressHex);
        var selectedOffsets = ReadCandidateOffsets(root)
            .Distinct()
            .Take(options.TopOffsets)
            .ToArray();
        var targetAddresses = selectedOffsets
            .Select(offset => checked(targetBaseAddress + offset))
            .Distinct()
            .ToArray();
        var maxTotalBytes = checked((long)Math.Max(1, targetAddresses.Length) * options.Samples * options.MaxBytesPerRegion);
        var capturePlan = BuildCapturePlan(verification, selectedOffsets, targetAddresses);
        var capturePlanPath = string.IsNullOrWhiteSpace(options.OutputPath)
            ? null
            : Path.GetFullPath(options.OutputPath);
        if (!string.IsNullOrWhiteSpace(capturePlanPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(capturePlanPath)!);
            File.WriteAllText(capturePlanPath, JsonSerializer.Serialize(capturePlan, SessionJson.Options));
        }

        var recommendedCaptureArgs = BuildRecommendedCaptureArgs(capturePlanPath, options, targetAddresses.Length, maxTotalBytes);
        return new RiftActorCoordinateOwnerFollowupPlanResult
        {
            Success = true,
            PacketPath = packetPath,
            PacketVerificationPath = verification.Path,
            PassiveSessionId = verification.PassiveSessionId,
            MoveSessionId = verification.MoveSessionId,
            TargetBaseAddressHex = verification.TargetBaseAddressHex,
            TopOffsets = options.TopOffsets,
            SelectedOffsetCount = selectedOffsets.Length,
            TargetAddressCount = targetAddresses.Length,
            TargetAddresses = targetAddresses.Select(FormatHex).ToArray(),
            Samples = options.Samples,
            IntervalMilliseconds = options.IntervalMilliseconds,
            WindowsPerRegion = options.WindowsPerRegion,
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            CapturePlanPath = capturePlanPath,
            CapturePlan = capturePlan,
            RecommendedCaptureArgs = recommendedCaptureArgs,
            RecommendedCaptureCommandTemplate = "riftscan " + string.Join(' ', recommendedCaptureArgs.Select(QuoteIfNeeded)),
            Warnings =
            [
                "actor_coordinate_owner_followup_plan_is_capture_scaffolding_not_truth",
                "live_capture_requires_explicit_operator_window_targeting_and_read_only_process_access"
            ],
            Diagnostics =
            [
                "generated_from_verified_actor_coordinate_owner_discovery_packet",
                "capture_plan_targets_absolute_addresses_for_candidate_offsets",
                "rerun_coordinate_mirror_context_and_xref_chain_after_followup_capture"
            ]
        };
    }

    private static ComparisonNextCapturePlan BuildCapturePlan(
        RiftActorCoordinateOwnerDiscoveryPacketVerificationResult verification,
        IReadOnlyList<ulong> selectedOffsets,
        IReadOnlyList<ulong> targetAddresses)
    {
        var targets = selectedOffsets
            .Zip(targetAddresses, (offset, address) => new { Offset = offset, Address = address })
            .Select((target, index) => new ComparisonCaptureTarget
            {
                BaseAddressHex = FormatHex(target.Address),
                OffsetHex = FormatHex(target.Offset),
                DataType = "actor_coordinate_owner_candidate",
                PriorityScore = Math.Max(1, 100 - index),
                Reason = "actor_coordinate_owner_discriminator_followup"
            })
            .ToArray();

        return new ComparisonNextCapturePlan
        {
            SessionAId = verification.PassiveSessionId,
            SessionBId = verification.MoveSessionId,
            RecommendedMode = "capture_actor_coordinate_owner_discriminator_followup",
            TargetRegionPriorities = targets,
            Reason = "synchronized_coordinate_mirror_clusters_block_canonical_owner_promotion",
            ExpectedSignal = "outside_region_or_reciprocal_owner_container_evidence_selects_one_coordinate_family",
            StopCondition = "one_candidate_family_retains_addon_match_behavior_match_cross_session_xref_support_and_unique_owner_discriminator",
            Warnings =
            [
                "next_capture_plan_is_recommendation_not_truth_claim",
                "actor_coordinate_owner_not_promoted_until_mirror_clusters_are_resolved"
            ]
        };
    }

    private static IReadOnlyList<ulong> ReadCandidateOffsets(JsonElement root)
    {
        if (!root.TryGetProperty("next_recommended_action", out var nextAction) ||
            !nextAction.TryGetProperty("candidate_offsets_to_prioritize", out var offsets) ||
            offsets.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return offsets
            .EnumerateArray()
            .Where(offset => offset.ValueKind == JsonValueKind.String)
            .Select(offset => offset.GetString())
            .Where(offset => !string.IsNullOrWhiteSpace(offset))
            .Select(offset => ParseUnsignedHexOrDecimal(offset!))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildRecommendedCaptureArgs(
        string? capturePlanPath,
        RiftActorCoordinateOwnerFollowupPlanOptions options,
        int targetAddressCount,
        long maxTotalBytes)
    {
        var args = new List<string>
        {
            "capture",
            "plan",
            string.IsNullOrWhiteSpace(capturePlanPath) ? "<actor-coordinate-owner-followup-plan.json>" : capturePlanPath,
            "--pid",
            "<rift_pid>",
            "--process",
            "rift_x64",
            "--out",
            "sessions/<new-actor-coordinate-owner-followup>",
            "--top-regions",
            Math.Max(1, targetAddressCount).ToString(CultureInfo.InvariantCulture),
            "--samples",
            options.Samples.ToString(CultureInfo.InvariantCulture),
            "--interval-ms",
            options.IntervalMilliseconds.ToString(CultureInfo.InvariantCulture),
            "--windows-per-region",
            options.WindowsPerRegion.ToString(CultureInfo.InvariantCulture),
            "--max-bytes-per-region",
            options.MaxBytesPerRegion.ToString(CultureInfo.InvariantCulture),
            "--max-total-bytes",
            maxTotalBytes.ToString(CultureInfo.InvariantCulture),
            "--stimulus",
            "move_forward"
        };
        return args;
    }

    private static string QuoteIfNeeded(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

    private static string FormatHex(ulong value) => $"0x{value:X}";

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
