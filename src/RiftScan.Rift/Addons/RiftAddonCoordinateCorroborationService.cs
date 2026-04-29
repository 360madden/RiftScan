using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftAddonCoordinateCorroborationService
{
    private static readonly Regex PreviewRegex = new(@"preview=(?<x>[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\|(?<y>[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\|(?<z>[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)", RegexOptions.IgnoreCase);

    public RiftAddonCoordinateCorroborationResult Build(
        string truthCandidatePath,
        string observationPath,
        string outputPath,
        double tolerance = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(truthCandidatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(observationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be finite and non-negative.");
        }

        var candidates = ReadJsonLines<Vec3TruthCandidate>(Path.GetFullPath(truthCandidatePath));
        var observations = ReadJsonLines<RiftAddonCoordinateObservation>(Path.GetFullPath(observationPath));
        var entries = BuildEntries(candidates, observations, tolerance).ToArray();
        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllLines(fullOutputPath, entries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));

        return new RiftAddonCoordinateCorroborationResult
        {
            TruthCandidatePath = Path.GetFullPath(truthCandidatePath),
            ObservationPath = Path.GetFullPath(observationPath),
            OutputPath = fullOutputPath,
            Tolerance = tolerance,
            CandidateCount = candidates.Count,
            ObservationCount = observations.Count,
            CorroborationEntryCount = entries.Length,
            Warnings = BuildWarnings(entries)
        };
    }

    private static IEnumerable<Vec3TruthCorroborationEntry> BuildEntries(
        IReadOnlyList<Vec3TruthCandidate> candidates,
        IReadOnlyList<RiftAddonCoordinateObservation> observations,
        double tolerance)
    {
        foreach (var candidate in candidates)
        {
            var candidateVectors = CandidateVectors(candidate).ToArray();
            if (candidateVectors.Length == 0)
            {
                continue;
            }

            var match = observations
                .SelectMany(observation => candidateVectors.Select(vector => new CandidateObservationMatch(candidate, observation, vector, MaxAbsDistance(vector, observation))))
                .Where(item => item.Distance <= tolerance)
                .OrderBy(item => item.Distance)
                .ThenByDescending(item => item.Observation.FileLastWriteUtc)
                .FirstOrDefault();

            if (match is null)
            {
                continue;
            }

            yield return new Vec3TruthCorroborationEntry
            {
                BaseAddressHex = candidate.BaseAddressHex,
                OffsetHex = candidate.OffsetHex,
                DataType = candidate.DataType,
                Classification = candidate.Classification,
                CorroborationStatus = "corroborated",
                Source = $"{match.Observation.AddonName}:{match.Observation.SourcePattern}",
                AddonSourceType = "rift_addon_savedvariables_coordinate_observation",
                AddonObservedX = match.Observation.CoordX,
                AddonObservedY = match.Observation.CoordY,
                AddonObservedZ = match.Observation.CoordZ,
                AxisOrder = "x_y_z",
                Tolerance = tolerance,
                EvidenceSummary = $"candidate={candidate.CandidateId};observation={match.Observation.ObservationId};max_abs_distance={match.Distance:F6};candidate_preview={match.Vector.X:F6}|{match.Vector.Y:F6}|{match.Vector.Z:F6};addon={match.Observation.CoordX:F6}|{match.Observation.CoordY:F6}|{match.Observation.CoordZ:F6}",
                Notes = "addon coordinate corroborates candidate preview within tolerance; still candidate evidence until reviewed with session timing"
            };
        }
    }

    private static IEnumerable<Vec3> CandidateVectors(Vec3TruthCandidate candidate)
    {
        if (TryParsePreview(candidate.SessionAValueSequenceSummary, out var sessionA))
        {
            yield return sessionA;
        }

        if (TryParsePreview(candidate.SessionBValueSequenceSummary, out var sessionB) && !sessionB.Equals(sessionA))
        {
            yield return sessionB;
        }
    }

    private static bool TryParsePreview(string summary, out Vec3 vector)
    {
        var match = PreviewRegex.Match(summary ?? string.Empty);
        if (!match.Success)
        {
            vector = default;
            return false;
        }

        vector = new Vec3(
            ParseNumber(match.Groups["x"].Value),
            ParseNumber(match.Groups["y"].Value),
            ParseNumber(match.Groups["z"].Value));
        return true;
    }

    private static double MaxAbsDistance(Vec3 candidate, RiftAddonCoordinateObservation observation) =>
        Math.Max(
            Math.Abs(candidate.X - observation.CoordX),
            Math.Max(Math.Abs(candidate.Y - observation.CoordY), Math.Abs(candidate.Z - observation.CoordZ)));

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<Vec3TruthCorroborationEntry> entries)
    {
        var warnings = new List<string> { "addon_coordinate_corroboration_is_candidate_evidence_not_final_truth" };
        if (entries.Count == 0)
        {
            warnings.Add("no_candidate_previews_matched_addon_coordinates_within_tolerance");
        }

        return warnings;
    }

    private static IReadOnlyList<T> ReadJsonLines<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("JSONL input file does not exist.", path);
        }

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, SessionJson.Options) ?? throw new InvalidOperationException($"Invalid JSONL entry in {path}."))
            .ToArray();
    }

    private static double ParseNumber(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private readonly record struct Vec3(double X, double Y, double Z);

    private sealed record CandidateObservationMatch(
        Vec3TruthCandidate Candidate,
        RiftAddonCoordinateObservation Observation,
        Vec3 Vector,
        double Distance);
}
