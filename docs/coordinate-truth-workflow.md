# RiftScan coordinate truth workflow

This workflow is for player/world coordinate discovery from vec3 candidates. It stays read-only in RiftScan: memory capture finds candidates, while addon waypoint/player-coordinate telemetry can corroborate them.

## Evidence sources

Use both sources when available:

1. RiftScan passive-vs-move contrast:
   - `passive_idle` should keep the vec3 stable.
   - `move_forward` should change the vec3.
   - `riftscan compare sessions ... --vec3-truth-out` exports machine-readable coordinate truth candidates.
2. Addon waypoint/player-coordinate truth:
   - Use RIFT addon data from `Inspect.Unit.Detail("player")` / `coordX`, `coordY`, `coordZ`.
   - Existing local addon sources that expose this lane include `ReaderBridge`, `ReaderBridgeExport`, `Leader`, `ChromaLink`, and `BarCode`.
   - Treat addon data as validator/corroborator, not as a replacement for memory discovery.

## Capture and analyze

Capture comparable regions for a passive baseline and a move-forward stimulus:

```powershell
riftscan capture passive --pid <rift_pid> --process rift_x64 `
  --out sessions/<passive_id> --samples 50 --interval-ms 100 `
  --stimulus passive_idle

riftscan capture passive --pid <rift_pid> --process rift_x64 `
  --out sessions/<move_id> --samples 50 --interval-ms 100 `
  --stimulus move_forward

riftscan verify session sessions/<passive_id>
riftscan verify session sessions/<move_id>
riftscan analyze session sessions/<passive_id> --all
riftscan analyze session sessions/<move_id> --all
```

## Export coordinate truth candidates

```powershell
riftscan compare sessions `
  sessions/<passive_id> `
  sessions/<move_id> `
  --top 100 `
  --out reports/generated/<run>-comparison.json `
  --report-md reports/generated/<run>-comparison.md `
  --truth-readiness reports/generated/<run>-truth-readiness.json `
  --vec3-truth-out reports/generated/<run>-vec3-truth-candidates.jsonl
```

The exported candidates are still candidate evidence. They should include:

- `classification=position_like_vec3_candidate`
- `truth_readiness=strong_candidate`
- `passive_stable=true`
- `move_forward_changed=true`
- `external_truth_source_hint=addon_waypoint_or_player_coord_truth`
- `session_a_value_sequence_summary` / `session_b_value_sequence_summary` previews for addon coordinate review

If addon waypoint/player-coordinate review is available, pass a corroboration JSONL too:

```powershell
riftscan verify vec3-corroboration reports/generated/<run>-vec3-truth-corroboration.jsonl

riftscan compare sessions `
  sessions/<passive_id> `
  sessions/<move_id> `
  --top 100 `
  --out reports/generated/<run>-comparison.json `
  --truth-readiness reports/generated/<run>-truth-readiness.json `
  --vec3-truth-out reports/generated/<run>-vec3-truth-candidates.jsonl `
  --vec3-corroboration reports/generated/<run>-vec3-truth-corroboration.jsonl
```

Corroboration entries should be created only from real addon/waypoint evidence, for example `Inspect.Unit.Detail("player").coordX/Y/Z`, ReaderBridge export fields, Leader dump `coordX/Y/Z`, or ChromaLink/BarCode player-position lanes. See `docs/vec3-truth-corroboration.example.jsonl`.

## Repeat recovery

After a second independent passive-vs-move run:

```powershell
riftscan compare vec3-truth `
  reports/generated/<run-a>-vec3-truth-candidates.jsonl `
  reports/generated/<run-b>-vec3-truth-candidates.jsonl `
  --out reports/generated/<combined>-vec3-truth-recovery.json

riftscan verify vec3-truth-recovery reports/generated/<combined>-vec3-truth-recovery.json
```

Recovered candidates are stronger than one-run candidates, but still need addon waypoint/player-coordinate corroboration before a final coordinate truth claim.

## Promotion review packet

After recovery and addon corroboration, create a review packet that ranks repeated vec3 candidates. If actor-yaw recovery is available, pass it so duplicate coordinate copies near the actor-yaw field are sorted first:

```powershell
riftscan compare vec3-promotion `
  reports/generated/<combined>-vec3-truth-recovery.json `
  --corroboration reports/generated/<run>-vec3-truth-corroboration.jsonl `
  --actor-yaw-recovery reports/generated/<scalar>-scalar-truth-recovery.json `
  --out reports/generated/<combined>-vec3-truth-promotion.json

riftscan verify vec3-truth-promotion reports/generated/<combined>-vec3-truth-promotion.json
```

The output is a manual-review promotion packet. A `corroborated_candidate` is still not final coordinate truth until reviewed against the session evidence and current addon export timing.

## Live promoted-coordinate verification

For the top promoted candidate, optionally compare the live process value at `base_address_hex + offset_hex` against the freshest addon SavedVariables coordinate observation:

```powershell
riftscan rift verify-promoted-coordinate `
  --promotion reports/generated/<combined>-vec3-truth-promotion.json `
  --pid <rift_pid> `
  --savedvariables "C:\Users\<user>\OneDrive\Documents\RIFT\Interface\Saved" `
  --candidate-id vec3-promoted-000001 `
  --out reports/generated/<run>-rift-promoted-coordinate-live.json `
  --tolerance 5

riftscan verify rift-promoted-coordinate-live reports/generated/<run>-rift-promoted-coordinate-live.json
```

This performs one read-only 12-byte `vec3_float32` read and scans addon SavedVariables. It is validation evidence only; it must not be promoted to final truth without review.

Feed the verified live packet into the capability/status report so the position component is marked as live-validated candidate evidence:

```powershell
riftscan report capability `
  --rift-promoted-coordinate-live reports/generated/<run>-rift-promoted-coordinate-live.json `
  --json-out reports/generated/<run>-capability-status.json

riftscan verify capability-status reports/generated/<run>-capability-status.json
```

This advances `position` to `live_validated_candidate` only when the packet reports a successful live memory/addon match within tolerance. It remains candidate-validation evidence, not final coordinate truth.

## Addon waypoint/player-coordinate use

When addon telemetry is available, record the addon coordinate source, observed player/world coordinates, and timing beside the RiftScan session artifacts. Use it to confirm the memory candidate's vec3 values and axis order.

Do not add addon/window control to RiftScan core. Keep addon/reader usage at the operator or adapter-validation layer.

### SavedVariables scan helper

RiftScan's RIFT adapter can scan addon SavedVariables for coordinate observations without adding addon logic to scanner core:

```powershell
riftscan rift addon-coords `
  "C:\Users\<user>\OneDrive\Documents\RIFT\Interface\Saved" `
  --jsonl-out reports/generated/addon-coordinate-observations.jsonl `
  --json-out reports/generated/addon-coordinate-scan.json
```

The scan emits `riftscan.rift_addon_coordinate_observation.v1` JSONL and redacts account-like path segments. It recognizes:

- `coord = { x = ..., y = ..., z = ... }`
- `coordX = ...`, `coordY = ...`, `coordZ = ...`

### Candidate corroboration helper

After exporting vec3 truth candidates and addon observations:

```powershell
riftscan rift addon-corroboration `
  --candidates reports/generated/<run>-vec3-truth-candidates.jsonl `
  --observations reports/generated/addon-coordinate-observations.jsonl `
  --out reports/generated/<run>-vec3-truth-corroboration.jsonl `
  --json-out reports/generated/<run>-addon-coordinate-corroboration.json `
  --tolerance 5

riftscan verify vec3-corroboration reports/generated/<run>-vec3-truth-corroboration.jsonl
```

This matches addon coordinates to candidate preview vectors within tolerance. A match is corroboration evidence, not final coordinate truth by itself.
