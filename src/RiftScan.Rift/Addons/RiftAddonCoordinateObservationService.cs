using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftAddonCoordinateObservationService
{
    private const string NumberPattern = @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?";
    private static readonly Regex CoordTableRegex = new(@"coord\s*=\s*\{(?<body>.*?)\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex CoordXyzRegex = new(
        $@"coordX\s*=\s*(?<x>{NumberPattern})\s*,?\s*coordY\s*=\s*(?<y>{NumberPattern})\s*,?\s*coordZ\s*=\s*(?<z>{NumberPattern})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ZoneRegex = new(@"(?:zone|zoneId)\s*=\s*""(?<zone>[^""]+)""", RegexOptions.IgnoreCase);

    public RiftAddonCoordinateScanResult Scan(string path, int maxFiles = 5000, string? jsonlOutputPath = null) =>
        Scan(new RiftAddonCoordinateScanOptions
        {
            Path = path,
            MaxFiles = maxFiles,
            JsonlOutputPath = jsonlOutputPath
        });

    public RiftAddonCoordinateScanResult Scan(RiftAddonCoordinateScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxFiles);

        var fullPath = Path.GetFullPath(options.Path);
        var warnings = new List<string>();
        var files = EnumerateLuaFiles(fullPath, options.MaxFiles, warnings).ToArray();
        var observations = new List<RiftAddonCoordinateObservation>();
        foreach (var file in files)
        {
            ScanFile(fullPath, file, observations, warnings);
        }

        var ordered = observations
            .OrderByDescending(observation => observation.FileLastWriteUtc)
            .ThenBy(observation => observation.SourcePathRedacted, StringComparer.OrdinalIgnoreCase)
            .ThenBy(observation => observation.LineNumber)
            .Select((observation, index) => observation with { ObservationId = $"rift-addon-coord-{index + 1:000000}" })
            .ToArray();
        var filtered = ApplyFilters(ordered, options, warnings)
            .Select((observation, index) => observation with { ObservationId = $"rift-addon-coord-{index + 1:000000}" })
            .ToArray();
        if (ordered.Length > 0 && filtered.Length == 0)
        {
            warnings.Add("no_addon_coordinate_observations_after_filters");
        }

        var fullJsonlOutputPath = WriteJsonLines(options.JsonlOutputPath, filtered);
        return new RiftAddonCoordinateScanResult
        {
            RootPathRedacted = RedactPath(fullPath),
            JsonlOutputPath = fullJsonlOutputPath,
            FilesScanned = files.Length,
            ObservationCount = filtered.Length,
            Observations = filtered,
            Warnings = warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static IEnumerable<RiftAddonCoordinateObservation> ApplyFilters(
        IEnumerable<RiftAddonCoordinateObservation> observations,
        RiftAddonCoordinateScanOptions options,
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
            filtered = filtered.Where(observation => includeSet.Contains(observation.AddonName));
            warnings.Add("addon_coordinate_observations_filtered_by_addon_name");
        }

        if (options.MinFileLastWriteUtc.HasValue)
        {
            filtered = filtered.Where(observation => observation.FileLastWriteUtc >= options.MinFileLastWriteUtc.Value);
            warnings.Add("addon_coordinate_observations_filtered_by_min_file_write_utc");
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
            throw new DirectoryNotFoundException($"Addon coordinate path does not exist: {fullPath}");
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

    private static void ScanFile(string rootPath, string file, ICollection<RiftAddonCoordinateObservation> observations, ICollection<string> warnings)
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
                observations.Add(BuildObservation(rootPath, file, text, match.Index, "coord_table_xyz", x, y, z));
            }
        }

        foreach (Match match in CoordXyzRegex.Matches(text))
        {
            observations.Add(BuildObservation(
                rootPath,
                file,
                text,
                match.Index,
                "coordX_coordY_coordZ",
                ParseNumber(match.Groups["x"].Value),
                ParseNumber(match.Groups["y"].Value),
                ParseNumber(match.Groups["z"].Value)));
        }
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

    private static RiftAddonCoordinateObservation BuildObservation(
        string rootPath,
        string file,
        string text,
        int matchIndex,
        string sourcePattern,
        double x,
        double y,
        double z)
    {
        var zoneId = ExtractZone(text, matchIndex);
        var lastWriteUtc = File.GetLastWriteTimeUtc(file);
        return new()
        {
            SourceFileName = Path.GetFileName(file),
            SourcePathRedacted = RedactPath(Path.GetRelativePath(rootPath, file)),
            AddonName = Path.GetFileNameWithoutExtension(file),
            SourcePattern = sourcePattern,
            LineNumber = LineNumber(text, matchIndex),
            FileLastWriteUtc = new DateTimeOffset(lastWriteUtc, TimeSpan.Zero),
            CoordX = x,
            CoordY = y,
            CoordZ = z,
            ZoneId = zoneId,
            EvidenceSummary = $"source={Path.GetFileName(file)};pattern={sourcePattern};x={x:F6};y={y:F6};z={z:F6};zone={zoneId}"
        };
    }

    private static string ExtractZone(string text, int matchIndex)
    {
        var afterLength = Math.Min(text.Length - matchIndex, 1200);
        var afterWindow = text.Substring(matchIndex, afterLength);
        var match = ZoneRegex.Match(afterWindow);
        if (match.Success)
        {
            return match.Groups["zone"].Value;
        }

        var beforeStart = Math.Max(0, matchIndex - 1200);
        var beforeLength = matchIndex - beforeStart;
        var beforeWindow = text.Substring(beforeStart, beforeLength);
        match = ZoneRegex.Match(beforeWindow);
        return match.Success ? match.Groups["zone"].Value : string.Empty;
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

    private static string? WriteJsonLines(string? outputPath, IReadOnlyList<RiftAddonCoordinateObservation> observations)
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
