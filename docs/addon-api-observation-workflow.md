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

## Boundary

This workflow does not replace memory proof. It provides semantic anchors for
offline candidate rejection, capture planning, and cross-checks. Final coordinate
or waypoint claims still require replayable RiftScan artifacts.
