# RiftScan Current In-Game API Coordinate Truth Report

Created UTC: `2026-05-06T10:45:20Z`

## Result

```text
COORD API TRUTH: PARTIAL PASS / TRACE ANCHOR BLOCKED
coordinate_truth_level: current_api_plus_readonly_memory_candidate
movement_execution_allowed: false
old_offsets_trusted: false
```

## Current player from in-game API

| Field | Value |
|---|---:|
| Unit | `Atank` |
| Location | `Sanctum of the Vigil` |
| Zone | `z487C9102D2EA79BE` |
| X | `7511.5297851562` |
| Y | `904.47998046875` |
| Z | `3040.2800292969` |
| Source | `ReaderBridgeExport / Inspect.Unit.Detail / DirectAPI` |
| File write UTC | `2026-05-06T10:34:50.511034+00:00` |

## RiftReader current memory sanity check

| Field | Value |
|---|---:|
| PID | `11220` |
| Memory address | `0x1DA682DF690` |
| Memory XYZ | `7511.53, 904.48, 3040.28` |
| API expected XYZ | `7511.5297851562, 904.47998046875, 3040.2800292969` |
| Coord match | `True` |
| Delta XYZ | `0, 0, 0` |
| Selection/provenance | `heuristic / heuristic` |

This is strong current sanity evidence, but not movement-grade proof because the coord-trace anchor is still stale.

## RiftScan read-only capture + addon-coordinate match

| Field | Value |
|---|---:|
| Session | `sessions/current-api-coord-readonly-20260506-064252` |
| Captured base | `0x1DA682DF690` |
| Samples | `8` |
| Stimulus | `passive_idle` |
| Candidate | `rift-addon-coordinate-candidate-000001` |
| Candidate address | `0x1DA682DF690` |
| Axis | `xyz` |
| Support snapshots | `8` |
| Best max abs distance | `5.002220859751105e-11` |
| Match count | `8` |

## Still blocked

Movement Execution Gate remains BLOCKED:

- live-test-riftscan preflight failed for move_forward.
- live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.
- live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance.

The old coord-trace source is from trace PID `41220` and does not match current PID `11220`. Do not treat `0x216F2F26020` / `0x216F2F26068` as current proof-grade addresses until RiftReader rebuilds or revalidates the trace anchor.

## Evidence paths

```text
report: handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md
summary: handoffs/current/coord-api-truth/coord-api-truth-summary.json
addon_api_truth: reports/generated/addon-api-truth-current-coords-fresh-20260506-103803.json
riftreader_current: reports/generated/riftreader-read-player-current-20260506-104154.json
riftscan_session: sessions/current-api-coord-readonly-20260506-064252
riftscan_addon_match: reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.json
movement_gate: handoffs/current/movement-execution-gate/movement-execution-gate-summary.json
```

## Safety

```text
movement_or_input_sent: false
reloadui_sent: false
read_only_memory_capture_started: true
offset_validation_or_trust_promoted: false
coord_trace_anchor_rebuilt: false
```

## Next action

Rebuild/refresh the RiftReader coord-trace proof anchor for current PID before movement. Keep using `0x1DA682DF690` only as a current read-only candidate that matched the in-game API, not as a final durable coordinate truth claim.
