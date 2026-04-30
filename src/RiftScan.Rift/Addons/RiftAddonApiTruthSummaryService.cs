using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Rift.Addons;

public sealed class RiftAddonApiTruthSummaryService
{
    public RiftAddonApiTruthSummaryResult Summarize(RiftAddonApiTruthSummaryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ScanPath);

        var scanPath = Path.GetFullPath(options.ScanPath);
        var scan = JsonSerializer.Deserialize<RiftAddonApiObservationScanResult>(File.ReadAllText(scanPath), SessionJson.Options)
            ?? throw new InvalidOperationException($"Unable to read addon API observation scan result: {scanPath}");
        var latestPlayer = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("current_player", StringComparison.OrdinalIgnoreCase) &&
            observation.CoordX.HasValue &&
            observation.CoordZ.HasValue);
        var latestTarget = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("target", StringComparison.OrdinalIgnoreCase) &&
            observation.CoordX.HasValue &&
            observation.CoordZ.HasValue);
        var latestFocus = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("focus", StringComparison.OrdinalIgnoreCase) &&
            observation.CoordX.HasValue &&
            observation.CoordZ.HasValue);
        var latestFocusTarget = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("focus_target", StringComparison.OrdinalIgnoreCase) &&
            observation.CoordX.HasValue &&
            observation.CoordZ.HasValue);
        var latestPlayerLoc = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("player_loc", StringComparison.OrdinalIgnoreCase) &&
            observation.LocX.HasValue &&
            observation.LocZ.HasValue);
        var latestWaypoint = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("waypoint", StringComparison.OrdinalIgnoreCase) &&
            observation.WaypointX.HasValue &&
            observation.WaypointZ.HasValue)
            ?? LatestObservation(scan.Observations, observation =>
                observation.Kind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase) &&
                observation.WaypointHasWaypoint == true &&
                observation.WaypointX.HasValue &&
                observation.WaypointZ.HasValue);
        var latestWaypointStatus = LatestObservation(scan.Observations, observation =>
            observation.Kind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase));
        var latestAnchor = scan.WaypointAnchors
            .OrderByDescending(anchor => anchor.FileLastWriteUtc)
            .ThenByDescending(anchor => anchor.Realtime ?? double.MinValue)
            .ThenBy(anchor => anchor.AnchorId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var records = new List<RiftAddonApiTruthRecord>();
        AddRecord(records, latestPlayer, "current_player");
        AddRecord(records, latestTarget, "target");
        AddRecord(records, latestFocus, "focus");
        AddRecord(records, latestFocusTarget, "focus_target");
        AddRecord(records, latestPlayerLoc, "player_loc");
        AddRecord(records, latestWaypoint, "waypoint");
        AddRecord(records, latestWaypointStatus, "waypoint_status");
        if (latestAnchor is not null)
        {
            records.Add(ToAnchorRecord(latestAnchor));
        }

        var truthRecords = records
            .Select((record, index) => record with { TruthId = $"rift-addon-api-truth-{index + 1:000000}" })
            .ToArray();
        var warnings = BuildWarnings(scan, latestPlayer, latestTarget, latestFocus, latestFocusTarget, latestPlayerLoc, latestWaypoint, latestWaypointStatus, latestAnchor);

        return new()
        {
            Success = true,
            ScanPath = scanPath,
            ScanRootPathRedacted = scan.RootPathRedacted,
            ObservationCount = scan.ObservationCount,
            WaypointAnchorCount = scan.WaypointAnchorCount,
            ObservationKindCounts = scan.Observations
                .GroupBy(observation => observation.Kind, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase),
            TruthRecordCount = truthRecords.Length,
            LatestPlayer = FindRecord(truthRecords, "current_player"),
            LatestTarget = FindRecord(truthRecords, "target"),
            LatestFocus = FindRecord(truthRecords, "focus"),
            LatestFocusTarget = FindRecord(truthRecords, "focus_target"),
            LatestPlayerLoc = FindRecord(truthRecords, "player_loc"),
            LatestWaypoint = FindRecord(truthRecords, "waypoint"),
            LatestWaypointStatus = FindRecord(truthRecords, "waypoint_status"),
            LatestPlayerWaypointAnchor = FindRecord(truthRecords, "player_waypoint_anchor"),
            TruthRecords = truthRecords,
            Warnings = warnings,
            Diagnostics =
            [
                "uses_addon_api_observation_scan_only",
                "coordinate_truth_should_label_capture_sessions_before_memory_candidate_promotion",
                "missing_target_focus_or_waypoint_records_mean_the_addon_scan_did_not_observe_that_truth_source"
            ]
        };
    }

    private static RiftAddonApiObservation? LatestObservation(
        IReadOnlyList<RiftAddonApiObservation> observations,
        Func<RiftAddonApiObservation, bool> predicate) =>
        observations
            .Where(predicate)
            .OrderByDescending(observation => observation.FileLastWriteUtc)
            .ThenByDescending(observation => observation.Realtime ?? double.MinValue)
            .ThenBy(observation => observation.SourcePathRedacted, StringComparer.OrdinalIgnoreCase)
            .ThenBy(observation => observation.LineNumber)
            .FirstOrDefault();

    private static void AddRecord(
        ICollection<RiftAddonApiTruthRecord> records,
        RiftAddonApiObservation? observation,
        string truthKind)
    {
        if (observation is not null)
        {
            records.Add(ToObservationRecord(observation, truthKind));
        }
    }

    private static RiftAddonApiTruthRecord? FindRecord(IReadOnlyList<RiftAddonApiTruthRecord> records, string kind) =>
        records.FirstOrDefault(record => record.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));

    private static RiftAddonApiTruthRecord ToObservationRecord(RiftAddonApiObservation observation, string truthKind)
    {
        var (coordinateX, coordinateY, coordinateZ) = PrimaryCoordinate(observation, truthKind);
        return new()
        {
            Kind = truthKind,
            SourceObservationId = observation.ObservationId,
            SourceAddon = observation.SourceAddon,
            SourceFileName = observation.SourceFileName,
            SourcePathRedacted = observation.SourcePathRedacted,
            FileLastWriteUtc = observation.FileLastWriteUtc,
            Realtime = observation.Realtime,
            ApiSource = observation.ApiSource,
            SourceMode = observation.SourceMode,
            CoordinateSpace = observation.CoordinateSpace,
            ConfidenceLevel = observation.ConfidenceLevel,
            UnitId = observation.UnitId,
            UnitName = observation.UnitName,
            ZoneId = observation.ZoneId,
            LocationName = observation.LocationName,
            CoordinateX = coordinateX,
            CoordinateY = coordinateY,
            CoordinateZ = coordinateZ,
            PlayerX = truthKind.Equals("current_player", StringComparison.OrdinalIgnoreCase) ? observation.CoordX : null,
            PlayerY = truthKind.Equals("current_player", StringComparison.OrdinalIgnoreCase) ? observation.CoordY : null,
            PlayerZ = truthKind.Equals("current_player", StringComparison.OrdinalIgnoreCase) ? observation.CoordZ : null,
            TargetX = truthKind.Equals("target", StringComparison.OrdinalIgnoreCase) ? observation.CoordX : null,
            TargetY = truthKind.Equals("target", StringComparison.OrdinalIgnoreCase) ? observation.CoordY : null,
            TargetZ = truthKind.Equals("target", StringComparison.OrdinalIgnoreCase) ? observation.CoordZ : null,
            WaypointX = truthKind.Equals("waypoint", StringComparison.OrdinalIgnoreCase) || truthKind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase)
                ? observation.WaypointX
                : null,
            WaypointZ = truthKind.Equals("waypoint", StringComparison.OrdinalIgnoreCase) || truthKind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase)
                ? observation.WaypointZ
                : null,
            LocX = truthKind.Equals("player_loc", StringComparison.OrdinalIgnoreCase) ? observation.LocX : null,
            LocY = truthKind.Equals("player_loc", StringComparison.OrdinalIgnoreCase) ? observation.LocY : null,
            LocZ = truthKind.Equals("player_loc", StringComparison.OrdinalIgnoreCase) ? observation.LocZ : null,
            WaypointHasWaypoint = observation.WaypointHasWaypoint,
            WaypointUpdateCount = observation.WaypointUpdateCount,
            WaypointLastUpdateAt = observation.WaypointLastUpdateAt,
            WaypointLastCommand = observation.WaypointLastCommand,
            RawText = observation.RawText,
            EvidenceSummary = observation.EvidenceSummary
        };
    }

    private static RiftAddonApiTruthRecord ToAnchorRecord(RiftAddonWaypointAnchor anchor) =>
        new()
        {
            Kind = "player_waypoint_anchor",
            SourceAnchorId = anchor.AnchorId,
            SourceAddon = anchor.SourceAddon,
            SourceFileName = anchor.SourceFileName,
            SourcePathRedacted = anchor.SourcePathRedacted,
            FileLastWriteUtc = anchor.FileLastWriteUtc,
            Realtime = anchor.Realtime,
            ApiSource = "Inspect.Unit.Detail + Inspect.Map.Waypoint.Get",
            CoordinateSpace = "world_xyz_to_map_xz",
            ConfidenceLevel = anchor.ConfidenceLevel,
            ZoneId = anchor.ZoneId,
            LocationName = anchor.LocationName,
            PlayerX = anchor.PlayerX,
            PlayerY = anchor.PlayerY,
            PlayerZ = anchor.PlayerZ,
            WaypointX = anchor.WaypointX,
            WaypointZ = anchor.WaypointZ,
            DeltaX = anchor.DeltaX,
            DeltaZ = anchor.DeltaZ,
            HorizontalDistance = anchor.HorizontalDistance,
            EvidenceSummary = anchor.EvidenceSummary
        };

    private static (double? X, double? Y, double? Z) PrimaryCoordinate(RiftAddonApiObservation observation, string truthKind)
    {
        if (truthKind.Equals("player_loc", StringComparison.OrdinalIgnoreCase))
        {
            return (observation.LocX, observation.LocY, observation.LocZ);
        }

        if (truthKind.Equals("waypoint", StringComparison.OrdinalIgnoreCase) ||
            truthKind.Equals("waypoint_status", StringComparison.OrdinalIgnoreCase))
        {
            return (observation.WaypointX, null, observation.WaypointZ);
        }

        return (observation.CoordX, observation.CoordY, observation.CoordZ);
    }

    private static IReadOnlyList<string> BuildWarnings(
        RiftAddonApiObservationScanResult scan,
        RiftAddonApiObservation? latestPlayer,
        RiftAddonApiObservation? latestTarget,
        RiftAddonApiObservation? latestFocus,
        RiftAddonApiObservation? latestFocusTarget,
        RiftAddonApiObservation? latestPlayerLoc,
        RiftAddonApiObservation? latestWaypoint,
        RiftAddonApiObservation? latestWaypointStatus,
        RiftAddonWaypointAnchor? latestAnchor)
    {
        var warnings = new List<string> { "addon_api_truth_summary_is_snapshot_evidence_not_memory_truth" };
        warnings.AddRange(scan.Warnings.Select(warning => $"source_scan_warning:{warning}"));
        if (latestPlayer is null)
        {
            warnings.Add("no_current_player_coordinate_truth_observed");
        }

        if (latestTarget is null)
        {
            warnings.Add("no_target_coordinate_truth_observed");
        }
        else if (latestPlayer is not null && CoordinatesMatch(latestPlayer, latestTarget) && UnitIdentityMatches(latestPlayer, latestTarget))
        {
            warnings.Add("target_coordinate_matches_current_player_coordinate_truth");
        }

        if (latestFocus is null)
        {
            warnings.Add("no_focus_coordinate_truth_observed");
        }

        if (latestFocusTarget is null)
        {
            warnings.Add("no_focus_target_coordinate_truth_observed");
        }

        if (latestPlayerLoc is null)
        {
            warnings.Add("no_player_loc_truth_observed");
        }

        if (latestWaypoint is null)
        {
            warnings.Add("no_waypoint_coordinate_truth_observed");
        }

        if (latestWaypointStatus is null)
        {
            warnings.Add("no_waypoint_status_truth_observed");
        }

        if (latestAnchor is null)
        {
            warnings.Add("no_player_waypoint_anchor_truth_observed");
        }

        return warnings
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool CoordinatesMatch(RiftAddonApiObservation first, RiftAddonApiObservation second) =>
        ValuesMatch(first.CoordX, second.CoordX) &&
        ValuesMatch(first.CoordY, second.CoordY) &&
        ValuesMatch(first.CoordZ, second.CoordZ);

    private static bool UnitIdentityMatches(RiftAddonApiObservation first, RiftAddonApiObservation second)
    {
        if (!string.IsNullOrWhiteSpace(first.UnitId) || !string.IsNullOrWhiteSpace(second.UnitId))
        {
            return string.Equals(first.UnitId, second.UnitId, StringComparison.OrdinalIgnoreCase);
        }

        return !string.IsNullOrWhiteSpace(first.UnitName) &&
            string.Equals(first.UnitName, second.UnitName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ValuesMatch(double? first, double? second) =>
        first.HasValue &&
        second.HasValue &&
        Math.Abs(first.Value - second.Value) <= 0.0001d;
}
