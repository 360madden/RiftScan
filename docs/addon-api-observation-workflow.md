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

## Compact coordinate truth summary

Before chasing memory candidates, summarize the addon/API scan so the current
semantic truth is visible in one small artifact:

```powershell
riftscan rift addon-api-truth `
  reports/generated/addon-api-observation-scan.json `
  --out reports/generated/addon-api-truth-summary.json
```

The summary promotes only addon/API observation evidence, not memory truth. It
reports the latest observed `current_player`, `target`, `player_loc`,
`waypoint`, `waypoint_status`, and `player_waypoint_anchor` records when those
sources exist. Missing records are explicit warnings, so a missing target or
waypoint is treated as "not observed by the addon scan" rather than a memory
discovery failure.

## Actor/player coordinate matching

For actor-coordinate discovery, use the truth summary directly as the semantic
label source instead of first converting it to a separate coordinate JSONL file:

```powershell
riftscan rift match-addon-coords `
  sessions/<session_id> `
  --truth-summary reports/generated/addon-api-truth-summary.json `
  --truth-kind current_player `
  --region-base 0xADDR `
  --tolerance 0.25 `
  --latest-only `
  --top 100 `
  --out reports/generated/session-current-player-coordinate-matches.json `
  --report-md reports/generated/session-current-player-coordinate-matches.md
```

`current_player` is the default truth kind when `--truth-kind` is omitted.
Repeat `--truth-kind` or pass comma-separated kinds when deliberately comparing
additional coordinate labels such as `target`. Match output is validation
evidence only: synchronized mirrors can all match the same player coordinate, so
canonical actor-coordinate promotion still needs movement/cross-session
separation.

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

When the same SavedVariables snapshot contains both a `current_player`
observation and an active `waypoint`/`waypoint_status` coordinate, the scan
result also emits `waypoint_anchors`. These anchors record the player X/Z,
waypoint X/Z, delta, and distance as semantic labels for later offline memory
candidate rejection.

## Offline waypoint-anchor matching

After an addon/API scan emits `waypoint_anchors`, replay them against a stored
RiftScan session without attaching to RIFT:

```powershell
riftscan rift match-waypoint-anchors `
  sessions/<session_id> `
  --anchors reports/generated/addon-api-observation-scan.json `
  --tolerance 5 `
  --top 100 `
  --out reports/generated/session-waypoint-anchor-matches.json
```

Optional filters:

- `--region-base 0xADDR`: repeatable, bounds the offline scan to known-good
  snapshot regions.
- `--tolerance <units>`: maximum absolute X/Z coordinate and delta mismatch.
- `--top <n>`: caps emitted candidates and matches for review.

The matcher searches stored snapshot float32 vec3 triples for a player coordinate
and a waypoint coordinate whose memory delta matches the addon anchor delta. Its
output is validation evidence only; it narrows candidate regions/offsets but
does not by itself promote final coordinate truth.

If the vec3-pair matcher returns no hits, run the looser scalar matcher before
starting another live capture. It searches the same stored snapshots for
waypoint X and waypoint Z independently, then ranks non-adjacent X/Z pair
candidates:

```powershell
riftscan rift match-waypoint-scalars `
  sessions/<session_id> `
  --anchors reports/generated/addon-api-observation-scan.json `
  --tolerance 5 `
  --top 100 `
  --scalar-hits-out reports/generated/session-waypoint-scalar-hits.jsonl `
  --out reports/generated/session-waypoint-scalar-matches.json
```

Optional scalar-specific cap:

- `--max-scalar-hits-per-snapshot-axis <n>`: defaults to `64`; keeps pair
  generation bounded when a broad snapshot contains many loose scalar hits.
- `--scalar-hits-out <path>`: writes all retained scalar hits as JSONL so later
  differential comparison is not limited to the top hits embedded in the summary
  JSON.

Scalar matcher output is still validation evidence, not final truth. A strong
result is a repeated X/Z source pair across multiple snapshots or anchors.
When scalar pair candidates exist, generate the next targeted capture plan
instead of manually copying base addresses:

```powershell
riftscan rift plan-waypoint-scalar-followup `
  reports/generated/session-waypoint-scalar-matches.json `
  --top-pairs 2 `
  --out reports/generated/waypoint-scalar-followup-plan.json
```

The follow-up plan emits unique pair-candidate base addresses plus a bounded
`capture passive` command template. Change the waypoint before running that
targeted capture, then rerun `match-waypoint-scalars` and
`compare-waypoint-scalars`.

Compare scalar results across two or more labeled waypoint states before
promoting any scalar lead:

```powershell
riftscan rift compare-waypoint-scalars `
  reports/generated/session-waypoint-scalar-matches-a.json `
  reports/generated/session-waypoint-scalar-matches-b.json `
  --delta-tolerance 5 `
  --top 100 `
  --out reports/generated/waypoint-scalar-comparison.json
```

The comparer classifies emitted scalar offsets as `tracks_waypoint_candidate`,
`missing_after_waypoint_change`, `stable_despite_waypoint_change`, or
`changes_but_not_waypoint`. If an input match JSON has `scalar_hits_output_path`,
the comparer uses that retained-hit JSONL artifact instead of only the embedded
top-hit summary. It also reads each input session's snapshot index when
available; if a missing input did not capture the source base address, the
candidate is classified as `not_captured_in_missing_input` instead of being
rejected. Missing or static offsets after a deliberate waypoint change are
rejection evidence only when the relevant source region was captured.

For older scan JSON created before `waypoint_anchors` were emitted, the matcher
can derive the same anchor from saved `current_player` plus `waypoint` or active
`waypoint_status` observations.

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
