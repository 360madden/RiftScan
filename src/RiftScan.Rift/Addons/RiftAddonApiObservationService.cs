using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftAddonApiObservationService
{
    private const string NumberPattern = @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?";
    private static readonly Regex CoordTableRegex = new(@"coord\s*=\s*\{(?<body>.*?)\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex CoordXyzRegex = new(
        $@"coordX\s*=\s*(?<x>{NumberPattern})\s*,?\s*coordY\s*=\s*(?<y>{NumberPattern})\s*,?\s*coordZ\s*=\s*(?<z>{NumberPattern})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex WaypointTableRegex = new(@"waypoint\s*=\s*\{(?<body>.*?)\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex WaypointStatusStartRegex = new(@"\bwaypointStatus\s*=\s*\{", RegexOptions.IgnoreCase);
    private static readonly Regex LocTableRegex = new(@"\bloc\s*=\s*\{(?<body>.*?)\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex LocXzRegex = new(
        $@"locX\s*=\s*(?<x>{NumberPattern})\s*,?(?:\s*locY\s*=\s*(?<y>{NumberPattern})\s*,?)?\s*locZ\s*=\s*(?<z>{NumberPattern})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ContextKeyRegex = new(@"\b(?<key>player|target|nearbyUnits|nearbyUnit|party|member|unit|waypoint)\s*=\s*\{", RegexOptions.IgnoreCase);

    public RiftAddonApiObservationScanResult Scan(RiftAddonApiObservationScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxFiles);

        var fullPath = Path.GetFullPath(options.Path);
        var warnings = new List<string>();
        var files = EnumerateLuaFiles(fullPath, options.MaxFiles, warnings).ToArray();
        var observations = new List<RiftAddonApiObservation>();
        foreach (var file in files)
        {
            ScanFile(fullPath, file, observations, warnings);
        }

        var ordered = observations
            .OrderByDescending(observation => observation.FileLastWriteUtc)
            .ThenBy(observation => observation.SourcePathRedacted, StringComparer.OrdinalIgnoreCase)
            .ThenBy(observation => observation.LineNumber)
            .Select((observation, index) => observation with { ObservationId = $"rift-addon-api-obs-{index + 1:000000}" })
            .ToArray();
        var filtered = ApplyFilters(ordered, options, warnings)
            .Select((observation, index) => observation with { ObservationId = $"rift-addon-api-obs-{index + 1:000000}" })
            .ToArray();
        if (ordered.Length > 0 && filtered.Length == 0)
        {
            warnings.Add("no_addon_api_observations_after_filters");
        }

        var waypointAnchors = BuildWaypointAnchors(filtered);
        var fullJsonlOutputPath = WriteJsonLines(options.JsonlOutputPath, filtered);
        return new RiftAddonApiObservationScanResult
        {
            RootPathRedacted = RedactPath(fullPath),
            JsonlOutputPath = fullJsonlOutputPath,
            FilesScanned = files.Length,
            ObservationCount = filtered.Length,
            Observations = filtered,
            WaypointAnchorCount = waypointAnchors.Length,
            WaypointAnchors = waypointAnchors,
            Warnings = warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static RiftAddonWaypointAnchor[] BuildWaypointAnchors(IReadOnlyList<RiftAddonApiObservation> observations) =>
        observations
            .GroupBy(
                observation => $"{observation.SourceAddon}\n{observation.SourcePathRedacted}",
                observation => observation,
                StringComparer.OrdinalIgnoreCase)
            .Select(BuildWaypointAnchor)
            .OfType<RiftAddonWaypointAnchor>()
            .Select((anchor, index) => anchor with { AnchorId = $"rift-addon-waypoint-anchor-{index + 1:000000}" })
            .ToArray();

    private static RiftAddonWaypointAnchor? BuildWaypointAnchor(IEnumerable<RiftAddonApiObservation> group)
    {
        var observations = group.ToArray();
        var player = observations.FirstOrDefault(observation =>
            observation.Kind.Equals("current_player", StringComparison.OrdinalIgnoreCase) &&
            observation.CoordX.HasValue &&
            observation.CoordZ.HasValue);
        if (player is null)
        {
            return null;
        }

        var waypoint = observations.FirstOrDefault(observation =>
            observation.Kind.Equals("waypoint", StringComparison.OrdinalIgnoreCase) &&
            observation.WaypointX.HasValue &&
            observation.WaypointZ.HasValue);
        var status = observations.FirstOrDefault(observation =>
            observation.Kind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase));
        var waypointSource = waypoint ?? (status?.WaypointHasWaypoint == true &&
            status.WaypointX.HasValue &&
            status.WaypointZ.HasValue
                ? status
                : null);
        if (waypointSource is null)
        {
            return null;
        }

        var playerX = player.CoordX!.Value;
        var playerZ = player.CoordZ!.Value;
        var waypointX = waypointSource.WaypointX!.Value;
        var waypointZ = waypointSource.WaypointZ!.Value;
        var deltaX = waypointX - playerX;
        var deltaZ = waypointZ - playerZ;
        var distance = Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        return new()
        {
            SourceAddon = player.SourceAddon,
            SourceFileName = player.SourceFileName,
            SourcePathRedacted = player.SourcePathRedacted,
            FileLastWriteUtc = player.FileLastWriteUtc >= waypointSource.FileLastWriteUtc
                ? player.FileLastWriteUtc
                : waypointSource.FileLastWriteUtc,
            Realtime = waypointSource.Realtime ?? player.Realtime,
            PlayerObservationId = player.ObservationId,
            WaypointObservationId = waypoint?.ObservationId ?? string.Empty,
            WaypointStatusObservationId = status?.ObservationId ?? string.Empty,
            PlayerX = playerX,
            PlayerY = player.CoordY,
            PlayerZ = playerZ,
            WaypointX = waypointX,
            WaypointZ = waypointZ,
            DeltaX = deltaX,
            DeltaZ = deltaZ,
            HorizontalDistance = distance,
            ZoneId = player.ZoneId,
            LocationName = player.LocationName,
            ConfidenceLevel = waypoint is null
                ? "api_player_to_waypoint_status_pair"
                : "api_player_to_waypoint_pair",
            EvidenceSummary = $"addon={player.SourceAddon};player_obs={player.ObservationId};waypoint_obs={waypoint?.ObservationId ?? "none"};status_obs={status?.ObservationId ?? "none"};player_x={playerX:F6};player_z={playerZ:F6};waypoint_x={waypointX:F6};waypoint_z={waypointZ:F6};delta_x={deltaX:F6};delta_z={deltaZ:F6};distance={distance:F6}"
        };
    }

    private static IEnumerable<RiftAddonApiObservation> ApplyFilters(
        IEnumerable<RiftAddonApiObservation> observations,
        RiftAddonApiObservationScanOptions options,
        ICollection<string> warnings)
    {
        var filtered = observations;
        var includeAddonNames = options.IncludeAddonNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .SelectMany(name => name.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (includeAddonNames.Length > 0)
        {
            var includeSet = includeAddonNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(observation => includeSet.Contains(observation.SourceAddon));
            warnings.Add("addon_api_observations_filtered_by_addon_name");
        }

        if (options.MinFileLastWriteUtc.HasValue)
        {
            filtered = filtered.Where(observation => observation.FileLastWriteUtc >= options.MinFileLastWriteUtc.Value);
            warnings.Add("addon_api_observations_filtered_by_min_file_write_utc");
        }

        return filtered;
    }

    private static IEnumerable<string> EnumerateLuaFiles(string fullPath, int maxFiles, ICollection<string> warnings)
    {
        if (File.Exists(fullPath))
        {
            yield return fullPath;
            yield break;
        }

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Addon API observation path does not exist: {fullPath}");
        }

        var count = 0;
        foreach (var file in Directory.EnumerateFiles(fullPath, "*.lua", SearchOption.AllDirectories))
        {
            if (count >= maxFiles)
            {
                warnings.Add("max_files_limit_reached");
                yield break;
            }

            count++;
            yield return file;
        }
    }

    private static void ScanFile(string rootPath, string file, ICollection<RiftAddonApiObservation> observations, ICollection<string> warnings)
    {
        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (IOException ex)
        {
            warnings.Add($"file_read_failed:{Path.GetFileName(file)}:{ex.GetType().Name}");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            warnings.Add($"file_read_failed:{Path.GetFileName(file)}:{ex.GetType().Name}");
            return;
        }

        foreach (Match match in CoordTableRegex.Matches(text))
        {
            if (TryReadCoordinateTable(match.Groups["body"].Value, out var x, out var y, out var z))
            {
                observations.Add(BuildWorldCoordinateObservation(rootPath, file, text, match.Index, "coord_table_xyz", x, y, z));
            }
        }

        foreach (Match match in CoordXyzRegex.Matches(text))
        {
            observations.Add(BuildWorldCoordinateObservation(
                rootPath,
                file,
                text,
                match.Index,
                "coordX_coordY_coordZ",
                ParseNumber(match.Groups["x"].Value),
                ParseNumber(match.Groups["y"].Value),
                ParseNumber(match.Groups["z"].Value)));
        }

        foreach (Match match in WaypointTableRegex.Matches(text))
        {
            var body = match.Groups["body"].Value;
            if (TryReadNamedNumber(body, "x", out var x) && TryReadNamedNumber(body, "z", out var z))
            {
                observations.Add(BuildWaypointObservation(rootPath, file, text, match.Index, "waypoint_table_xz", x, z, "addon_savedvariables_direct"));
            }
        }

        foreach (Match match in WaypointStatusStartRegex.Matches(text))
        {
            observations.Add(BuildWaypointStatusObservation(rootPath, file, text, match.Index));
        }

        foreach (Match match in LocTableRegex.Matches(text))
        {
            var body = match.Groups["body"].Value;
            if (TryReadNamedNumber(body, "x", out var x) && TryReadNamedNumber(body, "z", out var z))
            {
                var hasY = TryReadNamedNumber(body, "y", out var y);
                var rawText = ExtractString(body, "raw", "text", "line") ?? string.Empty;
                var apiSource = ExtractString(body, "apiSource", "source") ?? "/loc";
                var confidence = apiSource.Equals("/loc", StringComparison.OrdinalIgnoreCase)
                    ? "ingame_loc_output"
                    : "loc_equivalent_from_api";
                observations.Add(BuildLocObservation(rootPath, file, text, match.Index, "loc_table_xz", x, hasY ? y : null, z, rawText, apiSource, confidence));
            }
        }

        foreach (Match match in LocXzRegex.Matches(text))
        {
            observations.Add(BuildLocObservation(
                rootPath,
                file,
                text,
                match.Index,
                "locX_locZ",
                ParseNumber(match.Groups["x"].Value),
                match.Groups["y"].Success ? ParseNumber(match.Groups["y"].Value) : null,
                ParseNumber(match.Groups["z"].Value),
                ExtractNearbyString(text, match.Index, "locRaw", "locText") ?? string.Empty,
                "/loc",
                "ingame_loc_output"));
        }
    }

    private static RiftAddonApiObservation BuildWorldCoordinateObservation(
        string rootPath,
        string file,
        string text,
        int matchIndex,
        string sourcePattern,
        double x,
        double y,
        double z)
    {
        var sourceAddon = Path.GetFileNameWithoutExtension(file);
        var contextKey = FindLastContextKey(text, matchIndex);
        var kind = ClassifyWorldCoordinateKind(contextKey, sourceAddon);
        var nearText = Window(text, matchIndex, before: 1800, after: 1800);
        var sourceMode = ExtractString(nearText, "sourceMode", "mode") ?? ExtractString(text, "sourceMode") ?? string.Empty;
        var confidence = sourceMode.Equals("DirectAPI", StringComparison.OrdinalIgnoreCase)
            ? "addon_api_direct_savedvariables"
            : "addon_savedvariables_direct";
        var apiSource = kind is "current_player" or "target" or "nearby_unit" or "party_member"
            ? "Inspect.Unit.Detail"
            : "addon_coordinate_savedvariables";
        var zoneId = ExtractNearbyString(text, matchIndex, "zone", "zoneId") ?? ExtractString(text, "zone", "zoneId") ?? string.Empty;
        var locationName = ExtractNearbyString(text, matchIndex, "locationName", "location") ?? string.Empty;
        var unitId = ExtractNearbyString(text, matchIndex, "id", "playerUnit") ?? string.Empty;
        var unitName = ExtractNearbyString(text, matchIndex, "name") ?? string.Empty;
        var realtime = ExtractNumber(nearText, "generatedAtRealtime", "capturedAt", "realtime")
            ?? ExtractNumber(text, "generatedAtRealtime", "capturedAt", "realtime");

        return new()
        {
            Kind = kind,
            SourceAddon = sourceAddon,
            SourceFileName = Path.GetFileName(file),
            SourcePathRedacted = RedactPath(Path.GetRelativePath(rootPath, file)),
            SourcePattern = sourcePattern,
            LineNumber = LineNumber(text, matchIndex),
            FileLastWriteUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero),
            Realtime = realtime,
            ApiSource = apiSource,
            SourceMode = sourceMode,
            UnitId = unitId,
            UnitName = unitName,
            ZoneId = zoneId,
            LocationName = locationName,
            CoordinateSpace = "world_xyz",
            ConfidenceLevel = confidence,
            CoordX = x,
            CoordY = y,
            CoordZ = z,
            EvidenceSummary = $"kind={kind};addon={sourceAddon};pattern={sourcePattern};space=world_xyz;x={x:F6};y={y:F6};z={z:F6};zone={zoneId};location={locationName}"
        };
    }

    private static RiftAddonApiObservation BuildWaypointObservation(
        string rootPath,
        string file,
        string text,
        int matchIndex,
        string sourcePattern,
        double x,
        double z,
        string confidence)
    {
        var sourceAddon = Path.GetFileNameWithoutExtension(file);
        var nearText = Window(text, matchIndex, before: 1600, after: 1600);
        var sourceMode = ExtractString(nearText, "sourceMode", "mode") ?? ExtractString(text, "sourceMode") ?? string.Empty;
        var realtime = ExtractNumber(nearText, "generatedAtRealtime", "capturedAt", "realtime")
            ?? ExtractNumber(text, "generatedAtRealtime", "capturedAt", "realtime");

        return new()
        {
            Kind = "waypoint",
            SourceAddon = sourceAddon,
            SourceFileName = Path.GetFileName(file),
            SourcePathRedacted = RedactPath(Path.GetRelativePath(rootPath, file)),
            SourcePattern = sourcePattern,
            LineNumber = LineNumber(text, matchIndex),
            FileLastWriteUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero),
            Realtime = realtime,
            ApiSource = "Inspect.Map.Waypoint.Get",
            SourceMode = sourceMode,
            CoordinateSpace = "map_xz",
            ConfidenceLevel = confidence,
            WaypointX = x,
            WaypointZ = z,
            EvidenceSummary = $"kind=waypoint;addon={sourceAddon};pattern={sourcePattern};space=map_xz;x={x:F6};z={z:F6}"
        };
    }

    private static RiftAddonApiObservation BuildWaypointStatusObservation(
        string rootPath,
        string file,
        string text,
        int matchIndex)
    {
        var sourceAddon = Path.GetFileNameWithoutExtension(file);
        var statusText = Window(text, matchIndex, before: 0, after: 1800);
        var nearText = Window(text, matchIndex, before: 1600, after: 1800);
        var sourceMode = ExtractString(nearText, "sourceMode", "mode") ?? ExtractString(text, "sourceMode") ?? string.Empty;
        var realtime = ExtractNumber(nearText, "generatedAtRealtime", "capturedAt", "realtime")
            ?? ExtractNumber(text, "generatedAtRealtime", "capturedAt", "realtime");
        var apiAvailable = ExtractBoolean(statusText, "apiAvailable");
        var setApiAvailable = ExtractBoolean(statusText, "setApiAvailable");
        var clearApiAvailable = ExtractBoolean(statusText, "clearApiAvailable");
        var hasWaypoint = ExtractBoolean(statusText, "hasWaypoint");
        var updateCount = ExtractNumber(statusText, "updateCount");
        var lastUpdateAt = ExtractNumber(statusText, "lastUpdateAt");
        var lastCommand = ExtractString(statusText, "lastCommand") ?? string.Empty;
        var hasX = TryReadNamedNumber(statusText, "x", out var x);
        var hasZ = TryReadNamedNumber(statusText, "z", out var z);
        var apiSource = ExtractString(statusText, "source") ?? "Inspect.Map.Waypoint.Get";
        var unit = ExtractString(statusText, "unit") ?? string.Empty;
        var confidence = apiAvailable == false
            ? "addon_api_status_unavailable"
            : "addon_api_status";
        var apiAvailableText = apiAvailable.HasValue ? apiAvailable.Value.ToString().ToLowerInvariant() : "unknown";
        var hasWaypointText = hasWaypoint.HasValue ? hasWaypoint.Value.ToString().ToLowerInvariant() : "unknown";
        var updateCountText = updateCount?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        var xText = hasX ? x.ToString("F6", CultureInfo.InvariantCulture) : "null";
        var zText = hasZ ? z.ToString("F6", CultureInfo.InvariantCulture) : "null";

        return new()
        {
            Kind = "waypoint_status",
            SourceAddon = sourceAddon,
            SourceFileName = Path.GetFileName(file),
            SourcePathRedacted = RedactPath(Path.GetRelativePath(rootPath, file)),
            SourcePattern = "waypoint_status_table",
            LineNumber = LineNumber(text, matchIndex),
            FileLastWriteUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero),
            Realtime = realtime,
            ApiSource = apiSource,
            SourceMode = sourceMode,
            UnitId = unit,
            CoordinateSpace = "map_xz_status",
            ConfidenceLevel = confidence,
            WaypointX = hasX ? x : null,
            WaypointZ = hasZ ? z : null,
            WaypointApiAvailable = apiAvailable,
            WaypointSetApiAvailable = setApiAvailable,
            WaypointClearApiAvailable = clearApiAvailable,
            WaypointHasWaypoint = hasWaypoint,
            WaypointUpdateCount = updateCount.HasValue ? Convert.ToInt32(updateCount.Value) : null,
            WaypointLastUpdateAt = lastUpdateAt,
            WaypointLastCommand = lastCommand,
            EvidenceSummary = $"kind=waypoint_status;addon={sourceAddon};api_available={apiAvailableText};has_waypoint={hasWaypointText};update_count={updateCountText};x={xText};z={zText}"
        };
    }

    private static RiftAddonApiObservation BuildLocObservation(
        string rootPath,
        string file,
        string text,
        int matchIndex,
        string sourcePattern,
        double x,
        double? y,
        double z,
        string rawText,
        string apiSource,
        string confidence)
    {
        var sourceAddon = Path.GetFileNameWithoutExtension(file);
        var nearText = Window(text, matchIndex, before: 1600, after: 1600);
        var sourceMode = ExtractString(nearText, "sourceMode", "mode") ?? ExtractString(text, "sourceMode") ?? string.Empty;
        var zoneId = ExtractNearbyString(text, matchIndex, "zone", "zoneId") ?? ExtractString(text, "zone", "zoneId") ?? string.Empty;
        var locationName = ExtractNearbyString(text, matchIndex, "locationName", "location") ?? string.Empty;
        var realtime = ExtractNumber(nearText, "generatedAtRealtime", "capturedAt", "realtime")
            ?? ExtractNumber(text, "generatedAtRealtime", "capturedAt", "realtime");

        return new()
        {
            Kind = "player_loc",
            SourceAddon = sourceAddon,
            SourceFileName = Path.GetFileName(file),
            SourcePathRedacted = RedactPath(Path.GetRelativePath(rootPath, file)),
            SourcePattern = sourcePattern,
            LineNumber = LineNumber(text, matchIndex),
            FileLastWriteUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero),
            Realtime = realtime,
            ApiSource = apiSource,
            SourceMode = sourceMode,
            ZoneId = zoneId,
            LocationName = locationName,
            CoordinateSpace = "game_loc_xz",
            ConfidenceLevel = confidence,
            LocX = x,
            LocY = y,
            LocZ = z,
            RawText = rawText,
            EvidenceSummary = $"kind=player_loc;addon={sourceAddon};pattern={sourcePattern};space=game_loc_xz;x={x:F6};z={z:F6};zone={zoneId};location={locationName}"
        };
    }

    private static string ClassifyWorldCoordinateKind(string contextKey, string sourceAddon)
    {
        if (contextKey.Equals("player", StringComparison.OrdinalIgnoreCase))
        {
            return "current_player";
        }

        if (contextKey.Equals("target", StringComparison.OrdinalIgnoreCase))
        {
            return "target";
        }

        if (contextKey.Equals("party", StringComparison.OrdinalIgnoreCase) || contextKey.Equals("member", StringComparison.OrdinalIgnoreCase))
        {
            return "party_member";
        }

        if (contextKey.Equals("nearbyUnit", StringComparison.OrdinalIgnoreCase) ||
            contextKey.Equals("nearbyUnits", StringComparison.OrdinalIgnoreCase) ||
            contextKey.Equals("unit", StringComparison.OrdinalIgnoreCase))
        {
            return "nearby_unit";
        }

        return sourceAddon.Equals("ReaderBridgeExport", StringComparison.OrdinalIgnoreCase) ||
            sourceAddon.Equals("AutoFish", StringComparison.OrdinalIgnoreCase)
                ? "current_player"
                : "coordinate_observation";
    }

    private static string FindLastContextKey(string text, int matchIndex)
    {
        var beforeStart = Math.Max(0, matchIndex - 1600);
        var beforeWindow = text.Substring(beforeStart, matchIndex - beforeStart);
        var key = string.Empty;
        foreach (Match match in ContextKeyRegex.Matches(beforeWindow))
        {
            key = match.Groups["key"].Value;
        }

        return key;
    }

    private static bool TryReadCoordinateTable(string body, out double x, out double y, out double z)
    {
        x = 0;
        y = 0;
        z = 0;
        return TryReadNamedNumber(body, "x", out x) &&
            TryReadNamedNumber(body, "y", out y) &&
            TryReadNamedNumber(body, "z", out z);
    }

    private static bool TryReadNamedNumber(string body, string name, out double value)
    {
        var match = Regex.Match(body, $@"\b{name}\s*=\s*(?<value>{NumberPattern})", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            value = 0;
            return false;
        }

        value = ParseNumber(match.Groups["value"].Value);
        return true;
    }

    private static string? ExtractString(string text, params string[] names)
    {
        foreach (var name in names)
        {
            var match = Regex.Match(text, $@"\b{name}\s*=\s*""(?<value>[^""]*)""", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups["value"].Value;
            }
        }

        return null;
    }

    private static bool? ExtractBoolean(string text, params string[] names)
    {
        foreach (var name in names)
        {
            var match = Regex.Match(text, $@"\b{name}\s*=\s*(?<value>true|false)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return bool.Parse(match.Groups["value"].Value);
            }
        }

        return null;
    }

    private static string? ExtractNearbyString(string text, int index, params string[] names)
    {
        var afterWindow = text[index..Math.Min(text.Length, index + 1800)];
        var afterValue = ExtractString(afterWindow, names);
        if (!string.IsNullOrEmpty(afterValue))
        {
            return afterValue;
        }

        var beforeStart = Math.Max(0, index - 1800);
        var beforeWindow = text[beforeStart..index];
        return ExtractLastString(beforeWindow, names);
    }

    private static string? ExtractLastString(string text, params string[] names)
    {
        foreach (var name in names)
        {
            Match? last = null;
            foreach (Match match in Regex.Matches(text, $@"\b{name}\s*=\s*""(?<value>[^""]*)""", RegexOptions.IgnoreCase))
            {
                last = match;
            }

            if (last is not null)
            {
                return last.Groups["value"].Value;
            }
        }

        return null;
    }

    private static double? ExtractNumber(string text, params string[] names)
    {
        foreach (var name in names)
        {
            var match = Regex.Match(text, $@"\b{name}\s*=\s*(?<value>{NumberPattern})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return ParseNumber(match.Groups["value"].Value);
            }
        }

        return null;
    }

    private static string Window(string text, int index, int before, int after)
    {
        var start = Math.Max(0, index - before);
        var end = Math.Min(text.Length, index + after);
        return text[start..end];
    }

    private static int LineNumber(string text, int index)
    {
        var line = 1;
        var max = Math.Min(index, text.Length);
        for (var i = 0; i < max; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static double ParseNumber(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string? WriteJsonLines(string? outputPath, IReadOnlyList<RiftAddonApiObservation> observations)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllLines(fullOutputPath, observations.Select(observation => JsonSerializer.Serialize(observation, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        return fullOutputPath;
    }

    private static string RedactPath(string path)
    {
        var prefix = Path.GetPathRoot(path) ?? string.Empty;
        var body = prefix.Length > 0 && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..]
            : path;
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var segments = body.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Contains('@', StringComparison.Ordinal) ? "<account>" : segment);
        return prefix + string.Join(Path.DirectorySeparatorChar, segments);
    }
}
