using System.Text.Json;
using RiftScan.Core.Processes;
using RiftScan.Core.Sessions;

namespace RiftScan.Capture.Passive;

public sealed class PassiveCaptureService(IProcessMemoryReader processMemoryReader)
{
    public static readonly IReadOnlySet<string> ValidStimulusLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "passive",
        "passive_idle",
        "passive_world_activity",
        "move_forward",
        "turn_left",
        "turn_right",
        "camera_only"
    };

    private const string CapturedSessionStatus = "complete";
    private const string InterruptedSessionStatus = "interrupted";
    private const string InterventionHandoffFileName = "intervention_handoff.json";
    private const int MinInterventionPollIntervalMilliseconds = 250;

    public PassiveCaptureResult Capture(PassiveCaptureOptions options)
    {
        ValidateOptions(options);

        var sessionPath = Path.GetFullPath(options.OutputPath);
        var process = ResolveProcess(options);
        var modules = processMemoryReader.GetModules(process.ProcessId);
        var candidateRegions = ResolveCandidateRegions(process.ProcessId, options);

        if (candidateRegions.Length == 0)
        {
            throw new InvalidOperationException("No readable committed memory regions matched the passive capture filter or requested region IDs/base addresses.");
        }

        PrepareSessionDirectory(sessionPath);
        Directory.CreateDirectory(Path.Combine(sessionPath, "snapshots"));

        var capturedRegions = new Dictionary<string, MemoryRegion>(StringComparer.OrdinalIgnoreCase);
        var snapshotEntries = new List<SnapshotIndexEntry>();
        long totalBytes = 0;
        bool interrupted = false;
        var interruptionReason = "intervention_wait_timed_out";
        var sampleCountAttempted = 0;
        var restoredProcess = process;

        for (var sample = 1; sample <= options.Samples; sample++)
        {
            if (totalBytes >= options.MaxTotalBytes)
            {
                break;
            }

            sampleCountAttempted = sample;
            var capturedThisSample = false;

            foreach (var region in candidateRegions)
            {
                if (totalBytes >= options.MaxTotalBytes)
                {
                    break;
                }

                var remainingBudget = options.MaxTotalBytes - totalBytes;
                var bytesToRead = (int)Math.Min(
                    (ulong)Math.Min(options.MaxBytesPerRegion, int.MaxValue),
                    Math.Min(region.SizeBytes, (ulong)remainingBudget));
                if (bytesToRead <= 0)
                {
                    continue;
                }

                byte[] bytes;
                try
                {
                    bytes = processMemoryReader.ReadMemory(restoredProcess.ProcessId, region.BaseAddress, bytesToRead);
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
                capturedThisSample = true;
            }

            if (!capturedThisSample)
            {
                var resumedProcess = ResolveProcessForWaitOrNull(options);
                if (resumedProcess is not null)
                {
                    var currentProcessChanged = resumedProcess.ProcessId != restoredProcess.ProcessId ||
                        resumedProcess.StartTimeUtc != restoredProcess.StartTimeUtc;
                    restoredProcess = resumedProcess;
                    candidateRegions = ResolveCandidateRegions(restoredProcess.ProcessId, options);
                    modules = processMemoryReader.GetModules(restoredProcess.ProcessId);
                    if (currentProcessChanged)
                    {
                        sample--;
                        continue;
                    }

                    interrupted = true;
                    interruptionReason = "selected_regions_unreadable";
                    break;
                }

                resumedProcess = WaitForProcessInterventionOrFail(options);
                if (resumedProcess is null)
                {
                    interrupted = true;
                    interruptionReason = "intervention_wait_timed_out";
                    break;
                }

                var processChanged = resumedProcess.ProcessId != restoredProcess.ProcessId ||
                    resumedProcess.StartTimeUtc != restoredProcess.StartTimeUtc;
                restoredProcess = resumedProcess;
                candidateRegions = ResolveCandidateRegions(restoredProcess.ProcessId, options);
                modules = processMemoryReader.GetModules(restoredProcess.ProcessId);
                if (processChanged)
                {
                    sample--;
                    continue;
                }
            }

            if (sample < options.Samples && options.IntervalMilliseconds > 0)
            {
                Thread.Sleep(options.IntervalMilliseconds);
            }
        }

        if (snapshotEntries.Count == 0)
        {
            if (interrupted)
            {
                WriteInterventionHandoff(
                    sessionPath,
                    restoredProcess,
                    options,
                    NoSnapshotInterruptionReason(interruptionReason),
                    0,
                    0,
                    0,
                    sampleCountAttempted);

                return new PassiveCaptureResult
                {
                    Success = false,
                    SessionPath = sessionPath,
                    SessionId = Path.GetFileName(sessionPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    ProcessId = restoredProcess.ProcessId,
                    ProcessName = restoredProcess.ProcessName,
                    RegionsCaptured = 0,
                    SnapshotsCaptured = 0,
                    BytesCaptured = 0,
                    HandoffPath = ResolveSessionPath(sessionPath, InterventionHandoffFileName),
                    ArtifactsWritten = [InterventionHandoffFileName]
                };
            }

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
            ProcessName = restoredProcess.ProcessName,
            ProcessId = restoredProcess.ProcessId,
            ProcessStartTimeUtc = restoredProcess.StartTimeUtc ?? DateTimeOffset.UtcNow,
            CaptureMode = "passive",
            SnapshotCount = snapshotEntries.Count,
            RegionCount = capturedRegions.Count,
            TotalBytesRaw = totalBytes,
            TotalBytesStored = totalBytes,
            Compression = "none",
            ChecksumAlgorithm = "SHA256",
            Status = interrupted ? InterruptedSessionStatus : CapturedSessionStatus
        };

        WriteJson(sessionPath, "manifest.json", manifest);
        WriteJson(sessionPath, "regions.json", new RegionMap { Regions = capturedRegions.Values.OrderBy(region => region.BaseAddressHex).ToArray() });
        WriteJson(sessionPath, "modules.json", new ModuleMap { Modules = modules });
        WriteSnapshotIndex(sessionPath, snapshotEntries);
        WriteStimulusIfRequested(sessionPath, manifest.SessionId, snapshotEntries, options);

        if (interrupted)
        {
            WriteInterventionHandoff(
                sessionPath,
                restoredProcess,
                options,
                interruptionReason,
                capturedRegions.Count,
                snapshotEntries.Count,
                totalBytes,
                sampleCountAttempted);
        }

        WriteChecksums(sessionPath, snapshotEntries);

        return new PassiveCaptureResult
        {
            Success = !interrupted,
            SessionPath = sessionPath,
            SessionId = manifest.SessionId,
            ProcessId = restoredProcess.ProcessId,
            ProcessName = restoredProcess.ProcessName,
            RegionsCaptured = capturedRegions.Count,
            SnapshotsCaptured = snapshotEntries.Count,
            BytesCaptured = totalBytes,
            HandoffPath = interrupted ? ResolveSessionPath(sessionPath, InterventionHandoffFileName) : null,
            ArtifactsWritten = EnumerateArtifacts(sessionPath).ToArray()
        };
    }

    private ProcessDescriptor? WaitForProcessInterventionOrFail(
        PassiveCaptureOptions options)
    {
        if (options.InterventionWaitMilliseconds <= 0)
        {
            return null;
        }

        var startedUtc = DateTimeOffset.UtcNow;
        var deadline = startedUtc + TimeSpan.FromMilliseconds(options.InterventionWaitMilliseconds);
        var pollInterval = TimeSpan.FromMilliseconds(Math.Max(MinInterventionPollIntervalMilliseconds, options.InterventionPollIntervalMilliseconds));

        while (DateTimeOffset.UtcNow < deadline)
        {
            var remainingMilliseconds = (int)Math.Min(
                pollInterval.TotalMilliseconds,
                (deadline - DateTimeOffset.UtcNow).TotalMilliseconds);
            if (remainingMilliseconds > 0)
            {
                Thread.Sleep(remainingMilliseconds);
            }

            var currentProcess = ResolveProcessForWaitOrNull(options);
            if (currentProcess is not null)
            {
                return currentProcess;
            }
        }

        return null;
    }

    private ProcessDescriptor? ResolveProcessForWaitOrNull(PassiveCaptureOptions options)
    {
        if (options.ProcessId is { } processId)
        {
            try
            {
                return processMemoryReader.GetProcessById(processId);
            }
            catch (InvalidOperationException)
            {
                // PID may have disappeared during a client restart; fall back to name if the caller supplied one.
            }
            catch (ArgumentException)
            {
                // PID may have disappeared during a client restart; fall back to name if the caller supplied one.
            }

            if (!string.IsNullOrWhiteSpace(options.ProcessName))
            {
                return ResolveProcessByNameForWaitOrNull(options.ProcessName);
            }

            return null;
        }

        if (!string.IsNullOrWhiteSpace(options.ProcessName))
        {
            return ResolveProcessByNameForWaitOrNull(options.ProcessName);
        }

        return null;
    }

    private ProcessDescriptor? ResolveProcessByNameForWaitOrNull(string processName)
    {
        try
        {
            return ResolveProcessByName(processName);
        }
        catch (InvalidOperationException)
        {
            // Process still unavailable or ambiguous; keep waiting.
        }
        catch (ArgumentException)
        {
            // Process still unavailable or ambiguous; keep waiting.
        }

        return null;
    }

    private VirtualMemoryRegion[] ResolveCandidateRegions(int processId, PassiveCaptureOptions options)
    {
        var candidateRegionsQuery = processMemoryReader
            .EnumerateRegions(processId)
            .Where(region => MemoryRegionFilter.IsDefaultCaptureCandidate(region, new MemoryRegionFilterOptions
            {
                IncludeImageRegions = options.IncludeImageRegions,
                MaxRegionBytes = (ulong)options.MaxBytesPerRegion
            }));

        if (options.RegionIds.Count > 0 || options.BaseAddresses.Count > 0)
        {
            candidateRegionsQuery = candidateRegionsQuery.Where(region =>
                options.RegionIds.Contains(region.RegionId) ||
                options.BaseAddresses.Contains(region.BaseAddress));
        }

        return candidateRegionsQuery
            .OrderBy(region => region.BaseAddress)
            .Take(options.MaxRegions)
            .ToArray();
    }

    private ProcessDescriptor ResolveProcess(PassiveCaptureOptions options)
    {
        if (options.ProcessId is { } processId)
        {
            return processMemoryReader.GetProcessById(processId);
        }

        return ResolveProcessByName(options.ProcessName!);
    }

    private ProcessDescriptor ResolveProcessByName(string processName)
    {
        var matches = processMemoryReader.FindProcessesByName(processName);
        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"No process found with name '{processName}'."),
            1 => matches[0],
            _ => throw new InvalidOperationException($"Multiple processes matched '{processName}': {string.Join(", ", matches.Select(match => match.ProcessId))}. Use --pid for an exact target.")
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.InterventionWaitMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.InterventionPollIntervalMilliseconds);
        if (!string.IsNullOrWhiteSpace(options.StimulusLabel) && !ValidStimulusLabels.Contains(options.StimulusLabel))
        {
            throw new ArgumentException($"Unknown stimulus label: {options.StimulusLabel}.");
        }
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

    private static void WriteStimulusIfRequested(
        string sessionPath,
        string sessionId,
        IReadOnlyList<SnapshotIndexEntry> snapshotEntries,
        PassiveCaptureOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.StimulusLabel))
        {
            return;
        }

        var stimulus = new StimulusEvent
        {
            SessionId = sessionId,
            StimulusId = "stimulus-000001",
            Label = options.StimulusLabel,
            StartSnapshotId = snapshotEntries.First().SnapshotId,
            EndSnapshotId = snapshotEntries.Last().SnapshotId,
            CreatedUtc = DateTimeOffset.UtcNow,
            Notes = options.StimulusNotes ?? string.Empty
        };
        var path = ResolveSessionPath(sessionPath, "stimuli.jsonl");
        File.WriteAllLines(path, [JsonSerializer.Serialize(stimulus, SessionJson.Options).ReplaceLineEndings(string.Empty)]);
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
        if (File.Exists(ResolveSessionPath(sessionPath, "stimuli.jsonl")))
        {
            paths.Add("stimuli.jsonl");
        }

        if (File.Exists(ResolveSessionPath(sessionPath, InterventionHandoffFileName)))
        {
            paths.Add(InterventionHandoffFileName);
        }

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

    private static void WriteInterventionHandoff(
        string sessionPath,
        ProcessDescriptor process,
        PassiveCaptureOptions options,
        string reason,
        int regionCount,
        int snapshotCount,
        long bytesCaptured,
        int samplesTargeted)
    {
        var handoff = new CaptureInterventionHandoff
        {
            SessionPath = sessionPath,
            SessionId = Path.GetFileName(sessionPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            ProcessName = process.ProcessName,
            ProcessId = process.ProcessId,
            ProcessStartTimeUtc = process.StartTimeUtc,
            CreatedUtc = DateTimeOffset.UtcNow,
            Reason = reason,
            RegionCount = regionCount,
            SnapshotCount = snapshotCount,
            BytesCaptured = bytesCaptured,
            SamplesTargeted = samplesTargeted,
            InterventionWaitMilliseconds = options.InterventionWaitMilliseconds,
            InterventionPollIntervalMilliseconds = options.InterventionPollIntervalMilliseconds
        };

        WriteJson(sessionPath, InterventionHandoffFileName, handoff);
    }

    private static string NoSnapshotInterruptionReason(string interruptionReason) =>
        interruptionReason switch
        {
            "selected_regions_unreadable" => "no_snapshot_data_before_selected_regions_unreadable",
            _ => "no_snapshot_data_before_intervention_timeout"
        };

    private static IEnumerable<string> EnumerateArtifacts(string sessionPath) =>
        Directory.EnumerateFiles(sessionPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(sessionPath, path).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase);

    private static string ResolveSessionPath(string sessionPath, string relativePath) =>
        Path.GetFullPath(Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
