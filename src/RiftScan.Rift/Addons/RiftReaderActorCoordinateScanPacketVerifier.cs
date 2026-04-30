using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiftScan.Rift.Addons;

public sealed class RiftReaderActorCoordinateScanPacketVerifier
{
    private const string ExpectedSchemaVersion = "riftscan-riftreader-delegate-actor-coordinate-scan-v1";
    private const double CoordinateDeltaTolerance = 0.25d;

    public RiftReaderActorCoordinateScanPacketVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<RiftReaderActorCoordinateScanPacketVerificationIssue>();
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "RiftReader actor-coordinate scan packet does not exist."));
            return new RiftReaderActorCoordinateScanPacketVerificationResult { Path = fullPath, Issues = issues };
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(fullPath), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid RiftReader actor-coordinate scan packet JSON: {ex.Message}"));
            return new RiftReaderActorCoordinateScanPacketVerificationResult { Path = fullPath, Issues = issues };
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("json_root_invalid", "RiftReader actor-coordinate scan packet JSON root must be an object."));
                return new RiftReaderActorCoordinateScanPacketVerificationResult { Path = fullPath, Issues = issues };
            }

            var packetSchemaVersion = ReadRequiredString(root, "schema_version", "schema_version_missing", "schema_version is required.", issues);
            if (!string.IsNullOrWhiteSpace(packetSchemaVersion) &&
                !string.Equals(packetSchemaVersion, ExpectedSchemaVersion, StringComparison.Ordinal))
            {
                issues.Add(Error("schema_version_invalid", $"schema_version must be {ExpectedSchemaVersion}."));
            }

            ValidateGeneratedUtc(root, issues);
            ValidateProcess(root, issues, out var processId, out var processName);
            ValidateRepoArtifacts(root, fullPath, issues, out var riftReaderArtifactCount);
            ValidateRiftScanPacketReference(root, fullPath, issues);

            var (sourceObjectAddressHex, coordRegionAddressHex, sourceCoordRelativeOffsetHex) =
                ValidateResolvedLiveCoordAnchor(root, issues);
            ValidateRejectedTraceAnchor(root, issues, out var traceObjectAddressHex);
            var bridgeRegionBaseHex = ValidateOwnerPointerEvidence(root, issues);
            ValidateActorCoordinateLayoutEvidence(root, sourceObjectAddressHex, sourceCoordRelativeOffsetHex, issues);
            ValidateNextCaptureRecommendation(root, sourceObjectAddressHex, bridgeRegionBaseHex, issues);

            return new RiftReaderActorCoordinateScanPacketVerificationResult
            {
                Path = fullPath,
                PacketSchemaVersion = packetSchemaVersion,
                ProcessId = processId,
                ProcessName = processName,
                SourceObjectAddressHex = sourceObjectAddressHex,
                CoordRegionAddressHex = coordRegionAddressHex,
                SourceCoordRelativeOffsetHex = sourceCoordRelativeOffsetHex,
                BridgeRegionBaseHex = bridgeRegionBaseHex,
                TraceObjectAddressHex = traceObjectAddressHex,
                RiftReaderArtifactCount = riftReaderArtifactCount,
                Issues = issues
            };
        }
    }

    private static void ValidateGeneratedUtc(JsonElement root, ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        var generatedUtc = ReadRequiredString(root, "generated_utc", "generated_utc_missing", "generated_utc is required.", issues);
        if (!string.IsNullOrWhiteSpace(generatedUtc) &&
            (!DateTimeOffset.TryParse(generatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ||
             parsed.Offset != TimeSpan.Zero))
        {
            issues.Add(Error("generated_utc_invalid", "generated_utc must be a valid UTC timestamp."));
        }
    }

    private static void ValidateProcess(
        JsonElement root,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues,
        out int processId,
        out string processName)
    {
        processId = 0;
        processName = string.Empty;

        if (!TryGetObject(root, "process", out var process))
        {
            issues.Add(Error("process_missing", "process object is required."));
            return;
        }

        processId = ReadRequiredPositiveInt(process, "pid", "process_pid_missing", "process.pid is required and must be positive.", issues);
        processName = ReadRequiredString(process, "name", "process_name_missing", "process.name is required.", issues);
        if (!string.IsNullOrWhiteSpace(processName) &&
            !string.Equals(processName, "rift_x64", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(processName, "rift_x64.exe", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("process_name_unexpected", "process.name should identify the RIFT x64 process."));
        }
    }

    private static void ValidateRepoArtifacts(
        JsonElement root,
        string packetPath,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues,
        out int artifactCount)
    {
        artifactCount = 0;
        if (!TryGetObject(root, "riftreader", out var riftReader))
        {
            issues.Add(Error("riftreader_missing", "riftreader object is required."));
            return;
        }

        var repo = ReadRequiredString(riftReader, "repo", "riftreader_repo_missing", "riftreader.repo is required.", issues);
        if (!string.IsNullOrWhiteSpace(repo) && !Directory.Exists(repo))
        {
            issues.Add(Error("riftreader_repo_missing_on_disk", $"riftreader.repo does not resolve to an existing directory: {repo}."));
        }

        var artifactDirectory = ReadRequiredString(riftReader, "artifact_directory", "riftreader_artifact_directory_missing", "riftreader.artifact_directory is required.", issues);
        if (!string.IsNullOrWhiteSpace(artifactDirectory) && !Directory.Exists(artifactDirectory))
        {
            issues.Add(Error("riftreader_artifact_directory_missing_on_disk", $"riftreader.artifact_directory does not resolve to an existing directory: {artifactDirectory}."));
        }

        if (!TryGetArray(riftReader, "artifact_files", out var artifacts) || artifacts.GetArrayLength() == 0)
        {
            issues.Add(Error("riftreader_artifact_files_missing", "riftreader.artifact_files must contain artifact paths."));
            return;
        }

        foreach (var artifact in artifacts.EnumerateArray())
        {
            if (artifact.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(artifact.GetString()))
            {
                issues.Add(Error("riftreader_artifact_file_invalid", "riftreader.artifact_files entries must be non-empty strings."));
                continue;
            }

            artifactCount++;
            var artifactPath = artifact.GetString()!;
            if (!TryResolveArtifactPath(artifactPath, packetPath, out _))
            {
                issues.Add(Error("riftreader_artifact_file_missing_on_disk", $"riftreader artifact file does not resolve to an existing file: {artifactPath}."));
            }
        }
    }

    private static void ValidateRiftScanPacketReference(
        JsonElement root,
        string packetPath,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "riftscan", out var riftScan))
        {
            issues.Add(Error("riftscan_missing", "riftscan object is required."));
            return;
        }

        var repo = ReadRequiredString(riftScan, "repo", "riftscan_repo_missing", "riftscan.repo is required.", issues);
        if (!string.IsNullOrWhiteSpace(repo) && !Directory.Exists(repo))
        {
            issues.Add(Error("riftscan_repo_missing_on_disk", $"riftscan.repo does not resolve to an existing directory: {repo}."));
        }

        var referencedPacketPath = ReadRequiredString(riftScan, "packet_path", "riftscan_packet_path_missing", "riftscan.packet_path is required.", issues);
        if (!string.IsNullOrWhiteSpace(referencedPacketPath) &&
            !Path.GetFullPath(referencedPacketPath).Equals(packetPath, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("riftscan_packet_path_mismatch", "riftscan.packet_path must point to the verified packet path."));
        }
    }

    private static (string SourceObjectAddressHex, string CoordRegionAddressHex, string SourceCoordRelativeOffsetHex) ValidateResolvedLiveCoordAnchor(
        JsonElement root,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "resolved_live_coord_anchor", out var anchor))
        {
            issues.Add(Error("resolved_live_coord_anchor_missing", "resolved_live_coord_anchor object is required."));
            return (string.Empty, string.Empty, string.Empty);
        }

        var status = ReadRequiredString(anchor, "status", "anchor_status_missing", "resolved_live_coord_anchor.status is required.", issues);
        if (!string.Equals(status, "validated", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("anchor_status_not_validated", "resolved_live_coord_anchor.status must be validated."));
        }

        var sourceKind = ReadRequiredString(anchor, "canonical_coord_source_kind", "canonical_coord_source_kind_missing", "resolved_live_coord_anchor.canonical_coord_source_kind is required.", issues);
        if (!sourceKind.Contains("coord", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("canonical_coord_source_kind_unexpected", "canonical_coord_source_kind must describe a coordinate source."));
        }

        var matchSource = ReadRequiredString(anchor, "match_source", "match_source_missing", "resolved_live_coord_anchor.match_source is required.", issues);
        if (!matchSource.Contains("readerbridge", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("match_source_unexpected", "resolved_live_coord_anchor.match_source must record ReaderBridge validation."));
        }

        var objectBaseAddressHex = ReadRequiredHex(anchor, "object_base_address", "object_base_address_missing", "resolved_live_coord_anchor.object_base_address is required.", issues);
        var sourceObjectAddressHex = ReadRequiredHex(anchor, "source_object_address", "source_object_address_missing", "resolved_live_coord_anchor.source_object_address is required.", issues);
        if (!string.IsNullOrWhiteSpace(objectBaseAddressHex) &&
            !string.IsNullOrWhiteSpace(sourceObjectAddressHex) &&
            !string.Equals(objectBaseAddressHex, sourceObjectAddressHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("object_source_address_mismatch", "object_base_address and source_object_address must match for this proof anchor."));
        }

        var coordRegionAddressHex = ReadRequiredHex(anchor, "coord_region_address", "coord_region_address_missing", "resolved_live_coord_anchor.coord_region_address is required.", issues);
        var sourceCoordRelativeOffsetHex = ReadRequiredHex(anchor, "source_coord_relative_offset_hex", "source_coord_relative_offset_missing", "resolved_live_coord_anchor.source_coord_relative_offset_hex is required.", issues);
        if (TryParseHex(sourceObjectAddressHex, out var sourceObject) &&
            TryParseHex(coordRegionAddressHex, out var coordRegion) &&
            TryParseHex(sourceCoordRelativeOffsetHex, out var relativeOffset) &&
            sourceObject + relativeOffset != coordRegion)
        {
            issues.Add(Error("coord_region_offset_mismatch", "coord_region_address must equal source_object_address plus source_coord_relative_offset_hex."));
        }

        ValidateCoordOffsets(anchor, issues);
        ValidateCoordSample(anchor, issues);
        ValidateReaderBridgeMatch(anchor, issues);

        return (sourceObjectAddressHex, coordRegionAddressHex, sourceCoordRelativeOffsetHex);
    }

    private static void ValidateCoordOffsets(JsonElement anchor, ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(anchor, "coord_offsets", out var offsets))
        {
            issues.Add(Error("coord_offsets_missing", "resolved_live_coord_anchor.coord_offsets object is required."));
            return;
        }

        foreach (var propertyName in new[] { "x", "y", "z" })
        {
            ReadRequiredHex(offsets, propertyName, $"coord_offset_{propertyName}_missing", $"coord_offsets.{propertyName} is required.", issues);
        }
    }

    private static void ValidateCoordSample(JsonElement anchor, ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(anchor, "memory_sample", out var sample))
        {
            issues.Add(Error("memory_sample_missing", "resolved_live_coord_anchor.memory_sample object is required."));
            return;
        }

        ReadRequiredHex(sample, "AddressHex", "memory_sample_address_missing", "memory_sample.AddressHex is required.", issues);
        ReadRequiredNumber(sample, "CoordX", "memory_sample_coord_x_missing", "memory_sample.CoordX is required.", issues);
        ReadRequiredNumber(sample, "CoordY", "memory_sample_coord_y_missing", "memory_sample.CoordY is required.", issues);
        ReadRequiredNumber(sample, "CoordZ", "memory_sample_coord_z_missing", "memory_sample.CoordZ is required.", issues);
    }

    private static void ValidateReaderBridgeMatch(JsonElement anchor, ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(anchor, "match", out var match))
        {
            issues.Add(Error("match_missing", "resolved_live_coord_anchor.match object is required."));
            return;
        }

        if (!match.TryGetProperty("CoordMatchesWithinTolerance", out var withinTolerance) ||
            withinTolerance.ValueKind != JsonValueKind.True)
        {
            issues.Add(Error("coord_match_not_within_tolerance", "resolved_live_coord_anchor.match.CoordMatchesWithinTolerance must be true."));
        }

        foreach (var propertyName in new[] { "DeltaX", "DeltaY", "DeltaZ" })
        {
            var delta = ReadRequiredNumber(match, propertyName, $"{propertyName.ToLowerInvariant()}_missing", $"match.{propertyName} is required.", issues);
            if (Math.Abs(delta) > CoordinateDeltaTolerance)
            {
                issues.Add(Error("coord_delta_too_large", $"match.{propertyName} must be within {CoordinateDeltaTolerance}."));
            }
        }
    }

    private static void ValidateRejectedTraceAnchor(
        JsonElement root,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues,
        out string traceObjectAddressHex)
    {
        traceObjectAddressHex = string.Empty;
        if (!TryGetObject(root, "rejected_trace_anchor", out var trace))
        {
            issues.Add(Error("rejected_trace_anchor_missing", "rejected_trace_anchor object is required."));
            return;
        }

        var status = ReadRequiredString(trace, "status", "rejected_trace_status_missing", "rejected_trace_anchor.status is required.", issues);
        if (status.Contains("coord_source", StringComparison.OrdinalIgnoreCase) &&
            !status.Contains("not", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("trace_anchor_claims_coord_source", "rejected_trace_anchor.status must not claim coordinate-source truth."));
        }

        traceObjectAddressHex = ReadRequiredHex(trace, "trace_object_base_address", "trace_object_base_address_missing", "rejected_trace_anchor.trace_object_base_address is required.", issues);
        ReadRequiredHex(trace, "trace_target_address", "trace_target_address_missing", "rejected_trace_anchor.trace_target_address is required.", issues);
        ReadRequiredString(trace, "reason", "rejected_trace_reason_missing", "rejected_trace_anchor.reason is required.", issues);

        if (!TryGetObject(trace, "trace_match", out var traceMatch))
        {
            issues.Add(Error("trace_match_missing", "rejected_trace_anchor.trace_match object is required."));
            return;
        }

        if (!traceMatch.TryGetProperty("CoordMatchesWithinTolerance", out var coordMatches) ||
            coordMatches.ValueKind != JsonValueKind.False)
        {
            issues.Add(Error("trace_coord_match_not_rejected", "rejected_trace_anchor.trace_match.CoordMatchesWithinTolerance must be false."));
        }
    }

    private static string ValidateOwnerPointerEvidence(
        JsonElement root,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "owner_pointer_evidence", out var evidence))
        {
            issues.Add(Error("owner_pointer_evidence_missing", "owner_pointer_evidence object is required."));
            return string.Empty;
        }

        ReadRequiredPositiveInt(evidence, "object_base_pointer_hit_count", "object_base_pointer_hit_count_missing", "owner_pointer_evidence.object_base_pointer_hit_count is required and must be positive.", issues);
        ReadRequiredNonNegativeInt(evidence, "coord_region_pointer_hit_count", "coord_region_pointer_hit_count_missing", "owner_pointer_evidence.coord_region_pointer_hit_count is required.", issues);
        ReadRequiredPositiveInt(evidence, "trace_object_pointer_hit_count", "trace_object_pointer_hit_count_missing", "owner_pointer_evidence.trace_object_pointer_hit_count is required and must be positive.", issues);

        if (!TryGetObject(evidence, "strongest_owner_table_candidate", out var candidate))
        {
            issues.Add(Error("strongest_owner_table_candidate_missing", "owner_pointer_evidence.strongest_owner_table_candidate object is required."));
            return string.Empty;
        }

        var bridgeRegionBaseHex = ReadRequiredHex(candidate, "region_base", "bridge_region_base_missing", "strongest_owner_table_candidate.region_base is required.", issues);
        ReadRequiredHex(candidate, "object_pointer_address", "object_pointer_address_missing", "strongest_owner_table_candidate.object_pointer_address is required.", issues);
        ReadRequiredHex(candidate, "trace_object_pointer_address", "trace_object_pointer_address_missing", "strongest_owner_table_candidate.trace_object_pointer_address is required.", issues);
        ReadRequiredHex(candidate, "instruction_pointer_address", "instruction_pointer_address_missing", "strongest_owner_table_candidate.instruction_pointer_address is required.", issues);
        ReadRequiredHex(candidate, "instruction_address", "instruction_address_missing", "strongest_owner_table_candidate.instruction_address is required.", issues);
        ValidateNonEmptyArray(candidate, "reasons", "bridge_reasons_missing", "strongest_owner_table_candidate.reasons must contain evidence entries.", issues);
        ValidateNonEmptyHexArray(candidate, "old_family_addresses_observed", "old_family_addresses_missing", "strongest_owner_table_candidate.old_family_addresses_observed must contain provenance addresses.", issues);

        var confidence = ReadRequiredString(candidate, "confidence", "bridge_confidence_missing", "strongest_owner_table_candidate.confidence is required.", issues);
        if (!confidence.Contains("not_yet_promoted", StringComparison.OrdinalIgnoreCase) &&
            !confidence.Contains("not yet promoted", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("bridge_confidence_overclaims_owner_truth", "strongest_owner_table_candidate.confidence must keep stable actor-owner promotion blocked until movement capture exists."));
        }

        return bridgeRegionBaseHex;
    }

    private static void ValidateActorCoordinateLayoutEvidence(
        JsonElement root,
        string sourceObjectAddressHex,
        string sourceCoordRelativeOffsetHex,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "actor_coordinate_layout_evidence", out var layout))
        {
            issues.Add(Error("actor_coordinate_layout_evidence_missing", "actor_coordinate_layout_evidence object is required."));
            return;
        }

        var sourceObjectBase = ReadRequiredHex(layout, "source_object_base", "layout_source_object_base_missing", "actor_coordinate_layout_evidence.source_object_base is required.", issues);
        if (!string.IsNullOrWhiteSpace(sourceObjectAddressHex) &&
            !string.Equals(sourceObjectAddressHex, sourceObjectBase, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("layout_source_object_mismatch", "actor_coordinate_layout_evidence.source_object_base must match the validated source object."));
        }

        ValidateNonEmptyHexArray(layout, "coord_triplet_offsets_hex", "coord_triplet_offsets_missing", "actor_coordinate_layout_evidence.coord_triplet_offsets_hex must contain offsets.", issues);
        var primaryOffset = ReadRequiredHex(layout, "primary_triplet_offset_hex", "primary_triplet_offset_missing", "actor_coordinate_layout_evidence.primary_triplet_offset_hex is required.", issues);
        if (!string.IsNullOrWhiteSpace(sourceCoordRelativeOffsetHex) &&
            !string.Equals(primaryOffset, sourceCoordRelativeOffsetHex, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("primary_triplet_offset_mismatch", "primary_triplet_offset_hex must match the validated source coordinate offset."));
        }

        ReadRequiredString(layout, "neighbor_triplet_note", "neighbor_triplet_note_missing", "actor_coordinate_layout_evidence.neighbor_triplet_note is required.", issues);
    }

    private static void ValidateNextCaptureRecommendation(
        JsonElement root,
        string sourceObjectAddressHex,
        string bridgeRegionBaseHex,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetObject(root, "next_capture_recommendation", out var next))
        {
            issues.Add(Error("next_capture_recommendation_missing", "next_capture_recommendation object is required."));
            return;
        }

        var mode = ReadRequiredString(next, "mode", "next_capture_mode_missing", "next_capture_recommendation.mode is required.", issues);
        if (!mode.Contains("movement", StringComparison.OrdinalIgnoreCase) &&
            !mode.Contains("moved", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("next_capture_mode_not_movement_labeled", "next_capture_recommendation.mode should request movement-labeled follow-up capture."));
        }

        ValidateNonEmptyArray(next, "use_riftreader_for", "use_riftreader_for_missing", "next_capture_recommendation.use_riftreader_for must contain RiftReader responsibilities.", issues);
        ValidateNonEmptyArray(next, "use_riftscan_for", "use_riftscan_for_missing", "next_capture_recommendation.use_riftscan_for must contain RiftScan responsibilities.", issues);
        ValidateNonEmptyArray(next, "avoid", "avoid_missing", "next_capture_recommendation.avoid must contain avoided slow paths.", issues);
        ReadRequiredString(next, "stop_condition", "next_capture_stop_condition_missing", "next_capture_recommendation.stop_condition is required.", issues);

        if (!TryGetArray(next, "target_addresses", out var targetAddresses) || targetAddresses.GetArrayLength() == 0)
        {
            issues.Add(Error("next_capture_target_addresses_missing", "next_capture_recommendation.target_addresses must contain target addresses."));
            return;
        }

        ValidateHexArray(targetAddresses, "next_capture_target_address_invalid", "next_capture_recommendation.target_addresses entries must be hexadecimal.", issues);
        if (!string.IsNullOrWhiteSpace(sourceObjectAddressHex) &&
            !ArrayContainsString(targetAddresses, sourceObjectAddressHex))
        {
            issues.Add(Error("next_capture_missing_source_object", "next_capture_recommendation.target_addresses must include the validated source object."));
        }

        if (!string.IsNullOrWhiteSpace(bridgeRegionBaseHex) &&
            !ArrayContainsString(targetAddresses, bridgeRegionBaseHex))
        {
            issues.Add(Error("next_capture_missing_bridge_region", "next_capture_recommendation.target_addresses must include the bridge region."));
        }
    }

    private static void ValidateNonEmptyArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetArray(parent, propertyName, out var array) || array.GetArrayLength() == 0)
        {
            issues.Add(Error(code, message));
        }
    }

    private static void ValidateNonEmptyHexArray(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!TryGetArray(parent, propertyName, out var array) || array.GetArrayLength() == 0)
        {
            issues.Add(Error(code, message));
            return;
        }

        ValidateHexArray(array, $"{propertyName}_invalid", $"{propertyName} entries must be hexadecimal.", issues);
    }

    private static void ValidateHexArray(
        JsonElement array,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !LooksLikeHex(item.GetString() ?? string.Empty))
            {
                issues.Add(Error(code, message));
            }
        }
    }

    private static string ReadRequiredHex(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        var value = ReadRequiredString(parent, propertyName, code, message, issues);
        if (!LooksLikeHex(value))
        {
            issues.Add(Error($"{propertyName}_invalid_hex", $"{propertyName} must be hexadecimal."));
        }

        return value;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            issues.Add(Error(code, message));
            return string.Empty;
        }

        return value.GetString()!;
    }

    private static int ReadRequiredPositiveInt(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        var parsed = ReadRequiredNonNegativeInt(parent, propertyName, code, message, issues);
        if (parsed <= 0)
        {
            issues.Add(Error($"{propertyName}_too_low", message));
        }

        return parsed;
    }

    private static int ReadRequiredNonNegativeInt(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var parsed))
        {
            issues.Add(Error(code, message));
            return 0;
        }

        if (parsed < 0)
        {
            issues.Add(Error($"{propertyName}_negative", $"{propertyName} must not be negative."));
        }

        return parsed;
    }

    private static double ReadRequiredNumber(
        JsonElement parent,
        string propertyName,
        string code,
        string message,
        ICollection<RiftReaderActorCoordinateScanPacketVerificationIssue> issues)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out var parsed) ||
            double.IsNaN(parsed) ||
            double.IsInfinity(parsed))
        {
            issues.Add(Error(code, message));
            return 0;
        }

        return parsed;
    }

    private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value) =>
        parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object;

    private static bool TryGetArray(JsonElement parent, string propertyName, out JsonElement value) =>
        parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array;

    private static bool TryResolveArtifactPath(string artifactPath, string packetPath, out string resolvedArtifactPath)
    {
        if (Path.IsPathRooted(artifactPath))
        {
            resolvedArtifactPath = artifactPath;
            return File.Exists(resolvedArtifactPath);
        }

        resolvedArtifactPath = Path.GetFullPath(artifactPath);
        if (File.Exists(resolvedArtifactPath))
        {
            return true;
        }

        var packetDirectory = Path.GetDirectoryName(packetPath);
        if (!string.IsNullOrWhiteSpace(packetDirectory))
        {
            resolvedArtifactPath = Path.Combine(packetDirectory, artifactPath);
            if (File.Exists(resolvedArtifactPath))
            {
                return true;
            }

            resolvedArtifactPath = Path.Combine(packetDirectory, Path.GetFileName(artifactPath));
            if (File.Exists(resolvedArtifactPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return text.Length > 0 && text.All(Uri.IsHexDigit);
    }

    private static bool TryParseHex(string value, out ulong parsed)
    {
        parsed = 0;
        if (!LooksLikeHex(value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
    }

    private static bool ArrayContainsString(JsonElement array, string expected) =>
        array.EnumerateArray().Any(item =>
            item.ValueKind == JsonValueKind.String &&
            string.Equals(item.GetString(), expected, StringComparison.OrdinalIgnoreCase));

    private static RiftReaderActorCoordinateScanPacketVerificationIssue Error(string code, string message) =>
        new()
        {
            Code = code,
            Message = message,
            Severity = "error"
        };
}

public sealed record RiftReaderActorCoordinateScanPacketVerificationResult
{
    [JsonPropertyName("result_schema_version")]
    public string ResultSchemaVersion { get; init; } = "riftscan.riftreader_actor_coordinate_scan_packet_verification_result.v1";

    [JsonPropertyName("success")]
    public bool Success => Issues.Count == 0;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("packet_schema_version")]
    public string PacketSchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("process_id")]
    public int ProcessId { get; init; }

    [JsonPropertyName("process_name")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("source_object_address_hex")]
    public string SourceObjectAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("coord_region_address_hex")]
    public string CoordRegionAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("source_coord_relative_offset_hex")]
    public string SourceCoordRelativeOffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("bridge_region_base_hex")]
    public string BridgeRegionBaseHex { get; init; } = string.Empty;

    [JsonPropertyName("trace_object_address_hex")]
    public string TraceObjectAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("riftreader_artifact_count")]
    public int RiftReaderArtifactCount { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<RiftReaderActorCoordinateScanPacketVerificationIssue> Issues { get; init; } = [];
}

public sealed record RiftReaderActorCoordinateScanPacketVerificationIssue
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;
}
