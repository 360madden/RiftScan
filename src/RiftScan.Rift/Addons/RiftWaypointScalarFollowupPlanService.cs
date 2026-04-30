using System.Globalization;
using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftWaypointScalarFollowupPlanService
{
    public RiftWaypointScalarFollowupPlanResult Plan(RiftWaypointScalarFollowupPlanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ScalarMatchPath);
        if (options.TopPairs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.TopPairs), "Top pairs must be positive.");
        }

        if (options.Samples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Samples), "Samples must be positive.");
        }

        if (options.IntervalMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.IntervalMilliseconds), "Interval milliseconds must be positive.");
        }

        if (options.MaxBytesPerRegion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxBytesPerRegion), "Max bytes per region must be positive.");
        }

        var scalarMatchPath = Path.GetFullPath(options.ScalarMatchPath);
        var match = JsonSerializer.Deserialize<RiftSessionWaypointScalarMatchResult>(File.ReadAllText(scalarMatchPath), SessionJson.Options)
            ?? throw new InvalidOperationException($"Unable to read waypoint scalar match result: {scalarMatchPath}");
        var selectedPairs = match.PairCandidates
            .Take(options.TopPairs)
            .ToArray();
        var baseAddresses = selectedPairs
            .SelectMany(candidate => new[] { candidate.XSourceBaseAddressHex, candidate.ZSourceBaseAddressHex })
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(NormalizeHex)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(address => ParseUnsignedHexOrDecimal(address))
            .ToArray();
        var maxRegions = baseAddresses.Length;
        var maxTotalBytes = checked((long)Math.Max(1, maxRegions) * options.Samples * options.MaxBytesPerRegion);
        var warnings = new List<string> { "waypoint_scalar_followup_plan_is_validation_scaffolding_not_truth" };
        if (selectedPairs.Length == 0)
        {
            warnings.Add("no_pair_candidates_available_for_followup");
        }

        if (match.PairCandidateCount > selectedPairs.Length)
        {
            warnings.Add("pair_candidates_truncated_by_top_pairs");
        }

        var recommendedCaptureArgs = new List<string>
        {
            "capture",
            "passive",
            "--pid",
            "<rift_pid>",
            "--process",
            "rift_x64",
            "--out",
            "sessions/<new-waypoint-targeted-session>",
            "--samples",
            options.Samples.ToString(CultureInfo.InvariantCulture),
            "--interval-ms",
            options.IntervalMilliseconds.ToString(CultureInfo.InvariantCulture),
            "--max-regions",
            Math.Max(1, maxRegions).ToString(CultureInfo.InvariantCulture),
            "--max-bytes-per-region",
            options.MaxBytesPerRegion.ToString(CultureInfo.InvariantCulture),
            "--max-total-bytes",
            maxTotalBytes.ToString(CultureInfo.InvariantCulture)
        };
        if (baseAddresses.Length > 0)
        {
            recommendedCaptureArgs.Add("--base-addresses");
            recommendedCaptureArgs.Add(string.Join(',', baseAddresses));
        }

        recommendedCaptureArgs.Add("--stimulus");
        recommendedCaptureArgs.Add("passive_idle");

        var commandTemplate = "riftscan " + string.Join(' ', recommendedCaptureArgs.Select(QuoteIfNeeded));
        return new()
        {
            Success = true,
            ScalarMatchPath = scalarMatchPath,
            SourceSessionId = match.SessionId,
            SourceSessionPath = match.SessionPath,
            TopPairs = options.TopPairs,
            SelectedPairCandidateCount = selectedPairs.Length,
            BaseAddressCount = baseAddresses.Length,
            BaseAddresses = baseAddresses,
            Samples = options.Samples,
            IntervalMilliseconds = options.IntervalMilliseconds,
            MaxRegions = Math.Max(1, maxRegions),
            MaxBytesPerRegion = options.MaxBytesPerRegion,
            MaxTotalBytes = maxTotalBytes,
            RecommendedCaptureArgs = recommendedCaptureArgs,
            RecommendedCaptureCommandTemplate = commandTemplate,
            CandidateSummaries = selectedPairs.Select(ToSummary).ToArray(),
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Diagnostics =
            [
                "change_waypoint_before_running_recommended_capture",
                "capture_is_read_only_and_limited_to_pair_candidate_base_addresses",
                "run_match_waypoint_scalars_and_compare_waypoint_scalars_after_capture"
            ]
        };
    }

    private static RiftWaypointScalarFollowupCandidateSummary ToSummary(RiftSessionWaypointScalarPairCandidate candidate) =>
        new()
        {
            CandidateId = candidate.CandidateId,
            XSourceBaseAddressHex = NormalizeHex(candidate.XSourceBaseAddressHex),
            XSourceOffsetHex = NormalizeHex(candidate.XSourceOffsetHex),
            ZSourceBaseAddressHex = NormalizeHex(candidate.ZSourceBaseAddressHex),
            ZSourceOffsetHex = NormalizeHex(candidate.ZSourceOffsetHex),
            SupportCount = candidate.SupportCount,
            AnchorSupportCount = candidate.AnchorSupportCount,
            BestDistanceTotal = candidate.BestDistanceTotal,
            ValidationStatus = candidate.ValidationStatus
        };

    private static string QuoteIfNeeded(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

    private static string NormalizeHex(string value) => $"0x{ParseUnsignedHexOrDecimal(value):X}";

    private static ulong ParseUnsignedHexOrDecimal(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
