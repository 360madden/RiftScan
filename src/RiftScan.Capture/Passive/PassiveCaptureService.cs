using System.Text.Json;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Capture.Passive;

public sealed class PassiveCaptureService(IProcessMemoryReader processMemoryReader)
{
    public PassiveCaptureResult Capture(PassiveCaptureOptions options)
    {
        ValidateOptions(options);

        var sessionPath = Path.GetFullPath(options.OutputPath);
        var process = ResolveProcess(options);
        var modules = processMemoryReader.GetModules(process.ProcessId);
        var candidateRegionsQuery = processMemoryReader
            .EnumerateRegions(process.ProcessId)
            .Where(region => MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions
            {
                IncludeImageRegions = options.IncludeImageRegions,
                MaxRegionBytes = (ulong)options.MaxBytesPerRegion
            }));

        if (options.RegionIds.Count > 0)
        {
            candidateRegionsQuery = candidateRegionsQuery.Where(region => options.RegionIds.Contains(region.RegionId));
        }

        var candidateRegions = candidateRegionsQuery
            .OrderBy(region => region.BaseAddress)
            .Take(options.MaxRegions)
            .ToArray();

        if (candidateRegions.Length == 0)
        {
            throw new InvalidOperationException("No readable committed memory regions matched the passive capture filter or requested region IDs.");
        }

        PrepareSessionDirectory(sessionPath);
        Directory.CreateDirectory(Path.Combine(sessionPath, "snapshots"));

        var capturedRegions = new Dictionary<string, MemoryRegion>(StringComparer.OrdinalIgnoreCase);
        var snapshotEntries = new List<SnapshotIndexEntry>();
        long totalBytes = 0;

        for (var sample = 1; sample <= options.Samples; sample++)
        {
            foreach (var region in candidateRegions)
            {
                if (totalBytes >= options.MaxTotalBytes)
                {
                    break;
                }

                var remainingBudget = options.MaxTotalBytes - totalBytes;
                var bytesToRead = (int)Math.Min(Math.Min((ulong)options.MaxBytesPerRegion, region.SizeBytes), (ulong)remainingBudget);
                if (bytesToRead <= 0)
                {
                    continue;
                }

                byte[] bytes;
                try
                {
                    bytes = processMemoryReader.ReadMemory(process.ProcessId, region.BaseAddress, bytesToRead);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                if (bytes.Length == 0)
                {
                    continue;
                }

                var regionId = capturedRegions.TryGetValue(region.RegionId, out var existingRegion)
                    ? existingRegion.RegionId
                    : region.RegionId;
                capturedRegions.TryAdd(regionId, ToSessionRegion(region, regionId));

                var snapshotId = $"snapshot-{snapshotEntries.Count + 1:000000}";
                var snapshotRelativePath = $"snapshots/{regionId}-sample-{sample:000000}.bin";
                var snapshotPath = ResolveSessionPath(sessionPath, snapshotRelativePath);
                File.WriteAllBytes(snapshotPath, bytes);

                snapshotEntries.Add(new SnapshotIndexEntry
                {
                    SnapshotId = snapshotId,
                    RegionId = regionId,
                    Path = snapshotRelativePath,
                    BaseAddressHex = region.BaseAddressHex,
                    SizeBytes = bytes.Length,
                    ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(snapshotPath)
                });
                totalBytes += bytes.Length;
            }

            if (sample < options.Samples && options.IntervalMilliseconds > 0)
            {
                Thread.Sleep(options.IntervalMilliseconds);
            }
        }

        if (snapshotEntries.Count == 0)
        {
            throw new InvalidOperationException("Passive capture did not read any memory snapshots from the selected process.");
        }

        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = Path.GetFileName(sessionPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            ProjectVersion = "0.1.0",
            CreatedUtc = DateTimeOffset.UtcNow,
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.VersionString,
            ProcessName = process.ProcessName,
            ProcessId = process.ProcessId,
            ProcessStartTimeUtc = process.StartTimeUtc ?? DateTimeOffset.UtcNow,
            CaptureMode = "passive",
            SnapshotCount = snapshotEntries.Count,
            RegionCount = capturedRegions.Count,
            TotalBytesRaw = totalBytes,
            TotalBytesStored = totalBytes,
            Compression = "none",
            ChecksumAlgorithm = "SHA256",
            Status = "complete"
        };

        WriteJson(sessionPath, "manifest.json", manifest);
        WriteJson(sessionPath, "regions.json", new RegionMap { Regions = capturedRegions.Values.OrderBy(region => region.BaseAddressHex).ToArray() });
        WriteJson(sessionPath, "modules.json", new ModuleMap { Modules = modules });
        WriteSnapshotIndex(sessionPath, snapshotEntries);
        WriteChecksums(sessionPath, snapshotEntries);

        return new PassiveCaptureResult
        {
            Success = true,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            ProcessId = process.ProcessId,
            ProcessName = process.ProcessName,
            RegionsCaptured = capturedRegions.Count,
            SnapshotsCaptured = snapshotEntries.Count,
            BytesCaptured = totalBytes,
            ArtifactsWritten = EnumerateArtifacts(sessionPath).ToArray()
        };
    }

    private ProcessDescriptor ResolveProcess(PassiveCaptureOptions options)
    {
        if (options.ProcessId is { } processId)
        {
            return processMemoryReader.GetProcessById(processId);
        }

        var matches = processMemoryReader.FindProcessesByName(options.ProcessName!);
        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"No process found with name '{options.ProcessName}'."),
            1 => matches[0],
            _ => throw new InvalidOperationException($"Multiple processes matched '{options.ProcessName}': {string.Join(", ", matches.Select(match => match.ProcessId))}. Use --pid for an exact target.")
        };
    }

    private static void ValidateOptions(PassiveCaptureOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProcessName) && options.ProcessId is null)
        {
            throw new ArgumentException("Passive capture requires --process <name> or --pid <id>.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Samples);
        ArgumentOutOfRangeException.ThrowIfNegative(options.IntervalMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxRegions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxBytesPerRegion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxTotalBytes);
    }

    private static void PrepareSessionDirectory(string sessionPath)
    {
        if (Directory.Exists(sessionPath) && Directory.EnumerateFileSystemEntries(sessionPath).Any())
        {
            throw new InvalidOperationException($"Output session directory already exists and is not empty: {sessionPath}");
        }

        Directory.CreateDirectory(sessionPath);
    }

    private static MemoryRegion ToSessionRegion(VirtualMemoryRegion region, string regionId) =>
        new()
        {
            RegionId = regionId,
            BaseAddressHex = region.BaseAddressHex,
            SizeBytes = checked((long)Math.Min(region.SizeBytes, long.MaxValue)),
            Protection = region.ProtectName,
            State = region.StateName,
            Type = region.TypeName
        };

    private static void WriteJson<T>(string sessionPath, string relativePath, T payload)
    {
        var path = ResolveSessionPath(sessionPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, SessionJson.Options));
    }

    private static void WriteSnapshotIndex(string sessionPath, IEnumerable<SnapshotIndexEntry> entries)
    {
        var path = ResolveSessionPath(sessionPath, "snapshots/index.jsonl");
        File.WriteAllLines(path, entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
    }

    private static void WriteChecksums(string sessionPath, IEnumerable<SnapshotIndexEntry> snapshotEntries)
    {
        var paths = new List<string>
        {
            "manifest.json",
            "regions.json",
            "modules.json",
            "snapshots/index.jsonl"
        };
        paths.AddRange(snapshotEntries.Select(entry => entry.Path));

        var entries = paths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path =>
            {
                var absolutePath = ResolveSessionPath(sessionPath, path);
                return new ChecksumEntry
                {
                    Path = path.Replace('\\', '/'),
                    Sha256Hex = SessionChecksum.ComputeSha256Hex(absolutePath),
                    Bytes = new FileInfo(absolutePath).Length
                };
            })
            .ToArray();

        WriteJson(sessionPath, "checksums.json", new ChecksumManifest
        {
            Algorithm = "SHA256",
            Entries = entries
        });
    }

    private static IEnumerable<string> EnumerateArtifacts(string sessionPath) =>
        Directory.EnumerateFiles(sessionPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(sessionPath, path).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase);

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
