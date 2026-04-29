using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionVerifier
{
    private static readonly string[] RequiredFiles =
    [
        "manifest.json",
        "regions.json",
        "modules.json",
        "snapshots/index.jsonl",
        "checksums.json"
    ];

    private static readonly (string Path, string SchemaVersion)[] GeneratedJsonLineArtifacts =
    [
        ("triage.jsonl", "riftscan.region_triage_entry.v1"),
        ("deltas.jsonl", "riftscan.region_delta_entry.v1"),
        ("typed_value_candidates.jsonl", "riftscan.typed_value_candidate.v1"),
        ("scalar_candidates.jsonl", "riftscan.scalar_candidate.v1"),
        ("structures.jsonl", "riftscan.structure_candidate.v1"),
        ("vec3_candidates.jsonl", "riftscan.vec3_candidate.v1"),
        ("clusters.jsonl", "riftscan.structure_cluster.v1"),
        ("entity_layout_candidates.jsonl", "riftscan.entity_layout_candidate.v1")
    ];

    private static readonly (string Path, string SchemaVersion)[] GeneratedJsonArtifacts =
    [
        ("next_capture_plan.json", "riftscan.next_capture_plan.v1"),
        ("report.json", "riftscan.session_report.v1")
    ];

    public SessionVerificationResult Verify(string sessionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);

        var fullSessionPath = Path.GetFullPath(sessionPath);
        var issues = new List<VerificationIssue>();
        var verifiedArtifacts = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        string? sessionId = null;

        if (!Directory.Exists(fullSessionPath))
        {
            issues.Add(Error("session_root_missing", "Session directory does not exist.", fullSessionPath));
            return CreateResult(fullSessionPath, sessionId, verifiedArtifacts, issues);
        }

        foreach (var requiredFile in RequiredFiles)
        {
            if (!File.Exists(Resolve(fullSessionPath, requiredFile)))
            {
                issues.Add(Error("required_file_missing", $"Required session artifact is missing: {requiredFile}.", requiredFile));
            }
        }

        var manifest = ReadJson<SessionManifest>(fullSessionPath, "manifest.json", issues);
        if (manifest is not null)
        {
            sessionId = manifest.SessionId;
            ValidateManifest(manifest, issues);
        }

        var regions = ReadJson<RegionMap>(fullSessionPath, "regions.json", issues);
        if (regions is not null)
        {
            ValidateRegions(regions, issues);
            if (manifest is not null && manifest.RegionCount != regions.Regions.Count)
            {
                issues.Add(Error("manifest_region_count_mismatch", $"Manifest region_count is {manifest.RegionCount}, but regions.json contains {regions.Regions.Count} regions.", "regions.json"));
            }
        }

        _ = ReadJson<ModuleMap>(fullSessionPath, "modules.json", issues);

        var snapshotEntries = ReadSnapshotIndex(fullSessionPath, issues);
        if (manifest is not null && manifest.SnapshotCount != snapshotEntries.Count)
        {
            issues.Add(Error("manifest_snapshot_count_mismatch", $"Manifest snapshot_count is {manifest.SnapshotCount}, but snapshots/index.jsonl contains {snapshotEntries.Count} entries.", "snapshots/index.jsonl"));
        }

        VerifySnapshotEntries(fullSessionPath, snapshotEntries, verifiedArtifacts, issues);

        var checksumManifest = ReadJson<ChecksumManifest>(fullSessionPath, "checksums.json", issues);
        if (checksumManifest is not null)
        {
            VerifyChecksumManifest(fullSessionPath, checksumManifest, verifiedArtifacts, issues);
        }

        VerifyGeneratedArtifacts(fullSessionPath, verifiedArtifacts, issues);

        return CreateResult(fullSessionPath, sessionId, verifiedArtifacts, issues);
    }

    private static SessionVerificationResult CreateResult(string sessionPath, string? sessionId, IEnumerable<string> artifactsVerified, IReadOnlyList<VerificationIssue> issues) =>
        new()
        {
            SessionPath = sessionPath,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId,
            ArtifactsVerified = artifactsVerified.ToArray(),
            Issues = issues
        };

    private static T? ReadJson<T>(string sessionPath, string relativePath, ICollection<VerificationIssue> issues)
    {
        var path = Resolve(sessionPath, relativePath);
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SessionJson.Options);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("json_invalid", $"Invalid JSON in {relativePath}: {ex.Message}", relativePath));
            return default;
        }
    }

    private static List<SnapshotIndexEntry> ReadSnapshotIndex(string sessionPath, ICollection<VerificationIssue> issues)
    {
        var relativePath = "snapshots/index.jsonl";
        var path = Resolve(sessionPath, relativePath);
        var entries = new List<SnapshotIndexEntry>();

        if (!File.Exists(path))
        {
            return entries;
        }

        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<SnapshotIndexEntry>(line, SessionJson.Options);
                if (entry is null)
                {
                    issues.Add(Error("snapshot_index_entry_empty", $"Snapshot index line {lineNumber} did not contain an object.", relativePath));
                    continue;
                }

                ValidateSnapshotEntry(entry, relativePath, lineNumber, issues);
                entries.Add(entry);
            }
            catch (JsonException ex)
            {
                issues.Add(Error("snapshot_index_json_invalid", $"Invalid JSONL entry at line {lineNumber}: {ex.Message}", relativePath));
            }
        }

        if (entries.Count == 0)
        {
            issues.Add(Error("snapshot_index_empty", "snapshots/index.jsonl must contain at least one snapshot entry.", relativePath));
        }

        return entries;
    }

    private static void ValidateManifest(SessionManifest manifest, ICollection<VerificationIssue> issues)
    {
        Require(manifest.SchemaVersion, "manifest_schema_version_missing", "manifest.json", issues);
        Require(manifest.SessionId, "manifest_session_id_missing", "manifest.json", issues);
        Require(manifest.ProjectVersion, "manifest_project_version_missing", "manifest.json", issues);
        Require(manifest.MachineName, "manifest_machine_name_missing", "manifest.json", issues);
        Require(manifest.OsVersion, "manifest_os_version_missing", "manifest.json", issues);
        Require(manifest.ProcessName, "manifest_process_name_missing", "manifest.json", issues);
        Require(manifest.CaptureMode, "manifest_capture_mode_missing", "manifest.json", issues);
        Require(manifest.Compression, "manifest_compression_missing", "manifest.json", issues);
        Require(manifest.ChecksumAlgorithm, "manifest_checksum_algorithm_missing", "manifest.json", issues);
        Require(manifest.Status, "manifest_status_missing", "manifest.json", issues);

        if (manifest.CreatedUtc == default)
        {
            issues.Add(Error("manifest_created_utc_missing", "manifest.json field is missing or default: created_utc.", "manifest.json"));
        }

        if (manifest.ProcessStartTimeUtc == default)
        {
            issues.Add(Error("manifest_process_start_time_utc_missing", "manifest.json field is missing or default: process_start_time_utc.", "manifest.json"));
        }

        if (manifest.ProcessId < 0)
        {
            issues.Add(Error("manifest_process_id_invalid", "manifest.json process_id must be non-negative for fixture and captured sessions.", "manifest.json"));
        }

        if (manifest.SnapshotCount < 1)
        {
            issues.Add(Error("manifest_snapshot_count_invalid", "manifest.json snapshot_count must be at least 1.", "manifest.json"));
        }

        if (manifest.RegionCount < 1)
        {
            issues.Add(Error("manifest_region_count_invalid", "manifest.json region_count must be at least 1.", "manifest.json"));
        }

        if (manifest.TotalBytesRaw < 0 || manifest.TotalBytesStored < 0)
        {
            issues.Add(Error("manifest_total_bytes_invalid", "manifest.json byte totals must be non-negative.", "manifest.json"));
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(manifest.ChecksumAlgorithm, "SHA256"))
        {
            issues.Add(Error("unsupported_checksum_algorithm", "Only SHA256 session checksums are supported by the first verifier slice.", "manifest.json"));
        }
    }

    private static void ValidateRegions(RegionMap regions, ICollection<VerificationIssue> issues)
    {
        if (regions.Regions.Count == 0)
        {
            issues.Add(Error("regions_empty", "regions.json must contain at least one region.", "regions.json"));
            return;
        }

        foreach (var region in regions.Regions)
        {
            Require(region.RegionId, "region_id_missing", "regions.json", issues);
            Require(region.BaseAddressHex, "region_base_address_missing", "regions.json", issues);
            if (region.SizeBytes <= 0)
            {
                issues.Add(Error("region_size_invalid", $"Region {region.RegionId} has non-positive size_bytes.", "regions.json"));
            }
        }
    }

    private static void ValidateSnapshotEntry(SnapshotIndexEntry entry, string relativePath, int lineNumber, ICollection<VerificationIssue> issues)
    {
        Require(entry.SnapshotId, "snapshot_id_missing", relativePath, issues);
        Require(entry.RegionId, "snapshot_region_id_missing", relativePath, issues);
        Require(entry.Path, "snapshot_path_missing", relativePath, issues);
        Require(entry.BaseAddressHex, "snapshot_base_address_missing", relativePath, issues);
        Require(entry.ChecksumSha256Hex, "snapshot_checksum_missing", relativePath, issues);

        if (entry.SizeBytes <= 0)
        {
            issues.Add(Error("snapshot_size_invalid", $"Snapshot index line {lineNumber} has non-positive size_bytes.", relativePath));
        }
    }

    private static void VerifySnapshotEntries(string sessionPath, IEnumerable<SnapshotIndexEntry> entries, ISet<string> verifiedArtifacts, ICollection<VerificationIssue> issues)
    {
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                continue;
            }

            var normalizedRelativePath = NormalizeRelativePath(entry.Path);
            var absolutePath = Resolve(sessionPath, normalizedRelativePath);
            if (!File.Exists(absolutePath))
            {
                issues.Add(Error("snapshot_file_missing", $"Snapshot file listed in index does not exist: {normalizedRelativePath}.", normalizedRelativePath));
                continue;
            }

            var fileInfo = new FileInfo(absolutePath);
            if (entry.SizeBytes >= 0 && fileInfo.Length != entry.SizeBytes)
            {
                issues.Add(Error("snapshot_size_mismatch", $"Snapshot {normalizedRelativePath} size is {fileInfo.Length}, expected {entry.SizeBytes}.", normalizedRelativePath));
            }

            var actualHash = SessionChecksum.ComputeSha256Hex(absolutePath);
            if (!HashEquals(actualHash, entry.ChecksumSha256Hex))
            {
                issues.Add(Error("snapshot_checksum_mismatch", $"Snapshot {normalizedRelativePath} SHA256 does not match snapshots/index.jsonl.", normalizedRelativePath));
                continue;
            }

            verifiedArtifacts.Add(normalizedRelativePath);
        }
    }

    private static void VerifyChecksumManifest(string sessionPath, ChecksumManifest manifest, ISet<string> verifiedArtifacts, ICollection<VerificationIssue> issues)
    {
        if (!StringComparer.OrdinalIgnoreCase.Equals(manifest.Algorithm, "SHA256"))
        {
            issues.Add(Error("checksums_algorithm_unsupported", "checksums.json algorithm must be SHA256.", "checksums.json"));
        }

        if (manifest.Entries.Count == 0)
        {
            issues.Add(Error("checksums_empty", "checksums.json must contain at least one entry.", "checksums.json"));
            return;
        }

        foreach (var entry in manifest.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                issues.Add(Error("checksum_path_missing", "A checksums.json entry has no path.", "checksums.json"));
                continue;
            }

            var normalizedRelativePath = NormalizeRelativePath(entry.Path);
            var absolutePath = Resolve(sessionPath, normalizedRelativePath);
            if (!File.Exists(absolutePath))
            {
                issues.Add(Error("checksum_file_missing", $"checksums.json references a missing file: {normalizedRelativePath}.", normalizedRelativePath));
                continue;
            }

            var fileInfo = new FileInfo(absolutePath);
            if (entry.Bytes >= 0 && fileInfo.Length != entry.Bytes)
            {
                issues.Add(Error("checksum_size_mismatch", $"{normalizedRelativePath} size is {fileInfo.Length}, expected {entry.Bytes}.", normalizedRelativePath));
            }

            var actualHash = SessionChecksum.ComputeSha256Hex(absolutePath);
            if (!HashEquals(actualHash, entry.Sha256Hex))
            {
                issues.Add(Error("checksum_mismatch", $"{normalizedRelativePath} SHA256 does not match checksums.json.", normalizedRelativePath));
                continue;
            }

            verifiedArtifacts.Add(normalizedRelativePath);
        }
    }

    private static void VerifyGeneratedArtifacts(string sessionPath, ISet<string> verifiedArtifacts, ICollection<VerificationIssue> issues)
    {
        foreach (var (relativePath, schemaVersion) in GeneratedJsonLineArtifacts)
        {
            VerifyGeneratedJsonLines(sessionPath, relativePath, schemaVersion, verifiedArtifacts, issues);
        }

        foreach (var (relativePath, schemaVersion) in GeneratedJsonArtifacts)
        {
            VerifyGeneratedJsonFile(sessionPath, relativePath, schemaVersion, verifiedArtifacts, issues);
        }
    }

    private static void VerifyGeneratedJsonLines(
        string sessionPath,
        string relativePath,
        string expectedSchemaVersion,
        ISet<string> verifiedArtifacts,
        ICollection<VerificationIssue> issues)
    {
        var absolutePath = Resolve(sessionPath, relativePath);
        if (!File.Exists(absolutePath))
        {
            return;
        }

        var hadError = false;
        var lineNumber = 0;
        foreach (var line in File.ReadLines(absolutePath))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    issues.Add(Error("generated_jsonl_entry_not_object", $"{relativePath} line {lineNumber} must be a JSON object.", relativePath));
                    hadError = true;
                    continue;
                }

                VerifyGeneratedSchema(relativePath, expectedSchemaVersion, document.RootElement, issues, ref hadError);
            }
            catch (JsonException ex)
            {
                issues.Add(Error("generated_jsonl_invalid", $"Invalid JSONL entry in {relativePath} at line {lineNumber}: {ex.Message}", relativePath));
                hadError = true;
            }
        }

        if (!hadError)
        {
            verifiedArtifacts.Add(relativePath);
        }
    }

    private static void VerifyGeneratedJsonFile(
        string sessionPath,
        string relativePath,
        string expectedSchemaVersion,
        ISet<string> verifiedArtifacts,
        ICollection<VerificationIssue> issues)
    {
        var absolutePath = Resolve(sessionPath, relativePath);
        if (!File.Exists(absolutePath))
        {
            return;
        }

        var hadError = false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Error("generated_json_not_object", $"{relativePath} must be a JSON object.", relativePath));
                return;
            }

            VerifyGeneratedSchema(relativePath, expectedSchemaVersion, document.RootElement, issues, ref hadError);
        }
        catch (JsonException ex)
        {
            issues.Add(Error("generated_json_invalid", $"Invalid JSON in generated artifact {relativePath}: {ex.Message}", relativePath));
            hadError = true;
        }

        if (!hadError)
        {
            verifiedArtifacts.Add(relativePath);
        }
    }

    private static void VerifyGeneratedSchema(
        string relativePath,
        string expectedSchemaVersion,
        JsonElement element,
        ICollection<VerificationIssue> issues,
        ref bool hadError)
    {
        if (!element.TryGetProperty("schema_version", out var schemaVersionElement) || schemaVersionElement.ValueKind != JsonValueKind.String)
        {
            issues.Add(Error("generated_schema_missing", $"Generated artifact {relativePath} is missing string field: schema_version.", relativePath));
            hadError = true;
            return;
        }

        var actualSchemaVersion = schemaVersionElement.GetString();
        if (!StringComparer.Ordinal.Equals(actualSchemaVersion, expectedSchemaVersion))
        {
            issues.Add(Error("generated_schema_mismatch", $"Generated artifact {relativePath} schema_version is {actualSchemaVersion}, expected {expectedSchemaVersion}.", relativePath));
            hadError = true;
        }
    }

    private static void Require(string value, string code, string path, ICollection<VerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, $"Required field is missing or empty: {code}.", path));
        }
    }

    private static string Resolve(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, NormalizeRelativePath(relativePath).Replace('/', Path.DirectorySeparatorChar)));

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/').TrimStart('/');

    private static bool HashEquals(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static VerificationIssue Error(string code, string message, string? path) =>
        new()
        {
            Severity = "error",
            Code = code,
            Message = message,
            Path = path
        };
}
