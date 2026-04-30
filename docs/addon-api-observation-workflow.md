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
- `waypoint`: saved `waypoint = { x = ..., z = ... }` table intended for
  future `Inspect.Map.Waypoint.Get` exports.
- `waypoint_or_route_point`: TomTom `xpos/ypos` cache; useful for discovery
  hints, but lower-confidence than direct API exports.

## Confidence tiers

- `addon_api_direct_savedvariables`: SavedVariables export declares
  `sourceMode = "DirectAPI"`.
- `addon_savedvariables_direct`: direct coordinate table from addon state, but
  no explicit DirectAPI marker was found.
- `addon_route_cache`: route or waypoint cache from addon state; useful for
  search narrowing, not sufficient for final truth promotion.

## Boundary

This workflow does not replace memory proof. It provides semantic anchors for
offline candidate rejection, capture planning, and cross-checks. Final coordinate
or waypoint claims still require replayable RiftScan artifacts.
