# Addon API observation workflow

RiftScan should use addon/API exports as high-signal semantic context, while
keeping memory recovery as the final proof source.

## Command

```powershell
riftscan rift addon-api-observations `
  "C:\Users\<user>\OneDrive\Documents\RIFT\Interface\Saved" `
  --addon-name ReaderBridgeExport `
  --addon-name AutoFish `
  --jsonl-out reports/generated/addon-api-observations.jsonl `
  --json-out reports/generated/addon-api-observation-scan.json
```

Optional filters:

- `--addon-name <name>`: repeatable, comma-separated values are accepted.
- `--min-file-write-utc <timestamp>`: keeps only fresh SavedVariables files.
- `--max-files <n>`: bounds recursive SavedVariables scans.

## Current observation kinds

- `current_player`: addon-saved `Inspect.Unit.Detail`-style player `coord`
  table with `x/y/z`.
- `target`, `nearby_unit`, `party_member`: classified from nearby saved table
  context when present.
- `waypoint`: saved `waypoint = { x = ..., z = ... }` table exported from
  `Inspect.Map.Waypoint.Get`.
- `waypoint_status`: saved `waypointStatus = { ... }` diagnostic table with
  API availability, active-waypoint state, update count, and last command
  metadata.
- `player_loc`: captured `/loc` output for the player's in-game
  waypoint/location coordinate space.

## Waypoint policy

Prefer `Inspect.Map.Waypoint.Get("player")` over `/loc` for waypoint discovery.
It does not require foreground window focus or uninterrupted keyboard control.
If no `waypoint` table is present, inspect the addon-side `waypointStatus`
diagnostic before assuming failure: `apiAvailable = true` with
`hasWaypoint = false` means the API is reachable but there is no active player
waypoint to report.

## Confidence tiers

- `addon_api_direct_savedvariables`: SavedVariables export declares
  `sourceMode = "DirectAPI"`.
- `addon_savedvariables_direct`: direct coordinate table from addon state, but
  no explicit DirectAPI marker was found.

## Excluded sources

TomTom SavedVariables are intentionally ignored for RiftScan truth discovery
until the addon is developed enough to serve as a reliable observation source.
Use direct addon/API exports such as `Inspect.Unit.Detail`,
`Inspect.Map.Waypoint.Get`, or captured `/loc` output instead.

## `/loc` policy

Treat `/loc` as a high-value player-visible semantic observation source. It is
not memory truth by itself, but it is stronger than addon route caches because
it reflects the game's own in-client location/waypoint presentation. Capture it
as `player_loc` with `coordinate_space = "game_loc_xz"` and retain the raw text
when available.

If an addon cannot read the built-in slash output directly, it may emit a
clearly labeled `/loc` equivalent from `Inspect.Unit.Detail.coordX/coordZ`.
Those observations keep `kind = "player_loc"` but use
`confidence_level = "loc_equivalent_from_api"` and preserve the explicit source
in `api_source`. Promote only live slash-captured `/loc` output as
`confidence_level = "ingame_loc_output"`.

## Live waypoint proof

A live validation on 2026-04-29 EDT confirmed the preferred waypoint lane:
`ReaderBridgeExport` set a temporary waypoint through `Command.Map.Waypoint.Set`,
read it back through `Inspect.Map.Waypoint.Get("player")`, persisted it to
SavedVariables, and RiftScan parsed it as `kind = "waypoint"`.

Evidence summary:

- player X/Z: `7237.6196289062`, `3051.0598144531`
- waypoint X/Z: `7257.6196289062`, `3051.0598144531`
- parser output: `observation_count = 5`
- waypoint observation: `rift-addon-api-obs-000004`
- waypoint-status observation: `rift-addon-api-obs-000005`
- proof doc: `docs/validation/2026-04-29-readerbridgeexport-waypoint-api-proof.md`

The test used addon/API helpers only. It did not add game input/window-control
logic to RiftScan core.

## Boundary

This workflow does not replace memory proof. It provides semantic anchors for
offline candidate rejection, capture planning, and cross-checks. Final coordinate
or waypoint claims still require replayable RiftScan artifacts.
