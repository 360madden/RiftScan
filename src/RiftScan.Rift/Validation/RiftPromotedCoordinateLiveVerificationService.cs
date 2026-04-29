using System.Globalization;
using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Rift.Validation;

public sealed class RiftPromotedCoordinateLiveVerificationService
{
    private const int Vec3FloatByteCount = 12;
    private readonly IProcessMemoryReader processMemoryReader;

    public RiftPromotedCoordinateLiveVerificationService(IProcessMemoryReader processMemoryReader)
    {
        this.processMemoryReader = processMemoryReader;
    }

    public RiftPromotedCoordinateLiveVerificationResult Verify(
        string promotionPath,
        string savedVariablesPath,
        int? processId,
        string? processName,
        string? candidateId = null,
        double tolerance = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promotionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(savedVariablesPath);
        if (processId is null && string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Live coordinate verification requires --pid <id> or --process <name>.");
        }

        if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be finite and non-negative.");
        }

        var fullPromotionPath = Path.GetFullPath(promotionPath);
        var fullSavedVariablesPath = Path.GetFullPath(savedVariablesPath);
        var promotion = ReadPromotion(fullPromotionPath);
        var candidate = ResolveCandidate(promotion, candidateId);
        var process = ResolveProcess(processId, processName);
        var readUtc = DateTimeOffset.UtcNow;
        var baseAddress = ParseHex(candidate.BaseAddressHex, "base_address_hex");
        var offset = ParseHex(candidate.OffsetHex, "offset_hex");
        var absoluteAddress = checked(baseAddress + offset);
        var issues = new List<RiftPromotedCoordinateLiveVerificationIssue>();
        var warnings = new List<string>
        {
            "rift_promoted_coordinate_live_verification_is_not_final_truth_without_manual_review",
            "addon_latest_observation_selected_for_live_verification"
        };

        var memory = TryReadVec3(process.ProcessId, absoluteAddress, issues);
        var scan = new RiftAddonCoordinateObservationService().Scan(fullSavedVariablesPath);
        warnings.AddRange(scan.Warnings.Select(warning => $"addon_scan:{warning}"));
        var observation = SelectObservation(scan.Observations, candidate, memory, warnings);
        if (scan.Observations.Count == 0)
        {
            issues.Add(Error("no_addon_coordinate_observations", "SavedVariables scan did not find addon coordinate observations.", fullSavedVariablesPath));
        }

        var maxAbsDistance = memory is not null && observation is not null
            ? MaxAbsDistance(memory.Value, observation)
            : (double?)null;
        var validationStatus = ResolveValidationStatus(memory, observation, maxAbsDistance, tolerance, issues);
        if (string.Equals(validationStatus, "live_memory_addon_coordinate_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("live_memory_addon_coordinate_mismatch");
        }

        return new RiftPromotedCoordinateLiveVerificationResult
        {
            PromotionPath = fullPromotionPath,
            SavedVariablesPathRedacted = scan.RootPathRedacted,
            ProcessId = process.ProcessId,
            ProcessName = process.ProcessName,
            ProcessStartTimeUtc = process.StartTimeUtc,
            ReadUtc = readUtc,
            CandidateId = candidate.CandidateId,
            SourceRecoveredCandidateId = candidate.SourceRecoveredCandidateId,
            BaseAddressHex = candidate.BaseAddressHex,
            OffsetHex = candidate.OffsetHex,
            AbsoluteAddressHex = $"0x{absoluteAddress:X}",
            XOffsetHex = candidate.XOffsetHex,
            YOffsetHex = candidate.YOffsetHex,
            ZOffsetHex = candidate.ZOffsetHex,
            ReadByteCount = memory is null ? 0 : Vec3FloatByteCount,
            MemoryX = memory?.X,
            MemoryY = memory?.Y,
            MemoryZ = memory?.Z,
            AddonObservationId = observation?.ObservationId ?? string.Empty,
            AddonSource = observation is null ? string.Empty : $"{observation.AddonName}:{observation.SourcePattern}",
            AddonFileLastWriteUtc = observation?.FileLastWriteUtc,
            AddonObservedX = observation?.CoordX,
            AddonObservedY = observation?.CoordY,
            AddonObservedZ = observation?.CoordZ,
            AddonObservationCount = scan.ObservationCount,
            MaxAbsDistance = maxAbsDistance,
            Tolerance = tolerance,
            ValidationStatus = validationStatus,
            EvidenceSummary = BuildEvidenceSummary(candidate, memory, observation, maxAbsDistance, tolerance, validationStatus),
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Issues = issues
        };
    }

    private static Vec3TruthPromotionResult ReadPromotion(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Vec3 truth promotion file does not exist.", path);
        }

        var verification = new Vec3TruthPromotionVerifier().Verify(path);
        if (!verification.Success)
        {
            var issues = string.Join("; ", verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Vec3 truth promotion verification failed: {issues}");
        }

        return JsonSerializer.Deserialize<Vec3TruthPromotionResult>(File.ReadAllText(path), SessionJson.Options)
            ?? throw new InvalidOperationException("Could not deserialize vec3 truth promotion packet.");
    }

    private static Vec3PromotedTruthCandidate ResolveCandidate(Vec3TruthPromotionResult promotion, string? candidateId)
    {
        var targetCandidateId = string.IsNullOrWhiteSpace(candidateId)
            ? promotion.RecommendedManualReviewCandidateId
            : candidateId;
        if (string.IsNullOrWhiteSpace(targetCandidateId))
        {
            throw new InvalidOperationException("Vec3 truth promotion packet does not name a recommended manual-review candidate.");
        }

        var candidate = promotion.PromotedCandidates
            .FirstOrDefault(item => string.Equals(item.CandidateId, targetCandidateId, StringComparison.OrdinalIgnoreCase));
        return candidate ?? throw new InvalidOperationException($"Promoted coordinate candidate '{targetCandidateId}' was not found in promoted_candidates.");
    }

    private ProcessDescriptor ResolveProcess(int? processId, string? processName)
    {
        if (processId is { } id)
        {
            return processMemoryReader.GetProcessById(id);
        }

        var matches = processMemoryReader.FindProcessesByName(processName!);
        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"No process found with name '{processName}'."),
            1 => matches[0],
            _ => throw new InvalidOperationException($"Multiple processes matched '{processName}': {string.Join(", ", matches.Select(match => match.ProcessId))}. Use --pid for an exact target.")
        };
    }

    private Vec3? TryReadVec3(
        int processId,
        ulong absoluteAddress,
        ICollection<RiftPromotedCoordinateLiveVerificationIssue> issues)
    {
        byte[] bytes;
        try
        {
            bytes = processMemoryReader.ReadMemory(processId, absoluteAddress, Vec3FloatByteCount);
        }
        catch (InvalidOperationException ex)
        {
            issues.Add(Error("memory_read_failed", ex.Message, $"0x{absoluteAddress:X}"));
            return null;
        }

        if (bytes.Length < Vec3FloatByteCount)
        {
            issues.Add(Error("memory_read_too_short", $"Expected {Vec3FloatByteCount} bytes, but read {bytes.Length}.", $"0x{absoluteAddress:X}"));
            return null;
        }

        var x = BitConverter.ToSingle(bytes, 0);
        var y = BitConverter.ToSingle(bytes, 4);
        var z = BitConverter.ToSingle(bytes, 8);
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
        {
            issues.Add(Error("memory_coordinate_not_finite", "Live memory coordinate components must be finite float32 values.", $"0x{absoluteAddress:X}"));
            return null;
        }

        return new Vec3(x, y, z);
    }

    private static RiftAddonCoordinateObservation? SelectObservation(
        IReadOnlyList<RiftAddonCoordinateObservation> observations,
        Vec3PromotedTruthCandidate candidate,
        Vec3? memory,
        ICollection<string> warnings)
    {
        var sourceFiltered = FilterByPromotionSources(observations, candidate.CorroborationSources).ToArray();
        if (sourceFiltered.Length > 0)
        {
            warnings.Add("addon_observation_filtered_to_promotion_corroboration_source");
            return SelectLatestObservation(sourceFiltered, memory);
        }

        if (candidate.CorroborationSources.Count > 0)
        {
            warnings.Add("promotion_corroboration_source_not_found_in_addon_scan");
        }

        return SelectLatestObservation(observations, memory);
    }

    private static IEnumerable<RiftAddonCoordinateObservation> FilterByPromotionSources(
        IReadOnlyList<RiftAddonCoordinateObservation> observations,
        IReadOnlyList<string> sources)
    {
        foreach (var source in sources)
        {
            var parts = source.Split(':', 2);
            var addonName = parts[0];
            var sourcePattern = parts.Length > 1 ? parts[1] : string.Empty;
            foreach (var observation in observations)
            {
                if (string.Equals(observation.AddonName, addonName, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(sourcePattern) || string.Equals(observation.SourcePattern, sourcePattern, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return observation;
                }
            }
        }
    }

    private static RiftAddonCoordinateObservation? SelectLatestObservation(
        IReadOnlyList<RiftAddonCoordinateObservation> observations,
        Vec3? memory)
    {
        if (observations.Count == 0)
        {
            return null;
        }

        var latest = observations.Max(observation => observation.FileLastWriteUtc);
        var latestObservations = observations
            .Where(observation => observation.FileLastWriteUtc == latest)
            .ToArray();
        return memory is null
            ? latestObservations
                .OrderBy(observation => observation.SourcePathRedacted, StringComparer.OrdinalIgnoreCase)
                .ThenBy(observation => observation.LineNumber)
                .First()
            : latestObservations
                .OrderBy(observation => MaxAbsDistance(memory.Value, observation))
                .ThenBy(observation => observation.SourcePathRedacted, StringComparer.OrdinalIgnoreCase)
                .ThenBy(observation => observation.LineNumber)
                .First();
    }

    private static string ResolveValidationStatus(
        Vec3? memory,
        RiftAddonCoordinateObservation? observation,
        double? maxAbsDistance,
        double tolerance,
        IReadOnlyList<RiftPromotedCoordinateLiveVerificationIssue> issues)
    {
        if (issues.Any(issue => string.Equals(issue.Severity, "error", StringComparison.OrdinalIgnoreCase)) ||
            memory is null ||
            observation is null ||
            maxAbsDistance is null)
        {
            return "verification_incomplete";
        }

        return maxAbsDistance <= tolerance
            ? "live_memory_and_addon_coordinate_matched_candidate"
            : "live_memory_addon_coordinate_mismatch";
    }

    private static string BuildEvidenceSummary(
        Vec3PromotedTruthCandidate candidate,
        Vec3? memory,
        RiftAddonCoordinateObservation? observation,
        double? maxAbsDistance,
        double tolerance,
        string validationStatus)
    {
        var memoryText = memory is null
            ? "memory=unread"
            : $"memory={memory.Value.X:F6}|{memory.Value.Y:F6}|{memory.Value.Z:F6}";
        var addonText = observation is null
            ? "addon=missing"
            : $"addon={observation.CoordX:F6}|{observation.CoordY:F6}|{observation.CoordZ:F6};addon_observation={observation.ObservationId};addon_source={observation.AddonName}:{observation.SourcePattern}";
        var distanceText = maxAbsDistance is null
            ? "max_abs_distance=unavailable"
            : $"max_abs_distance={maxAbsDistance.Value:F6}";
        return $"candidate={candidate.CandidateId};source_recovered={candidate.SourceRecoveredCandidateId};{memoryText};{addonText};{distanceText};tolerance={tolerance:F6};validation_status={validationStatus}";
    }

    private static double MaxAbsDistance(Vec3 memory, RiftAddonCoordinateObservation observation) =>
        Math.Max(
            Math.Abs(memory.X - observation.CoordX),
            Math.Max(Math.Abs(memory.Y - observation.CoordY), Math.Abs(memory.Z - observation.CoordZ)));

    private static ulong ParseHex(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"{fieldName} must be hexadecimal: {value}");
    }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static RiftPromotedCoordinateLiveVerificationIssue Error(string code, string message, string? path = null) =>
        new()
        {
            Code = code,
            Message = message,
            Path = path
        };

    private readonly record struct Vec3(double X, double Y, double Z);
}
