# 2026-04-30 waypoint AutoHotkey backend and anchor-match check

## Summary

AutoHotkey `SendText` backend evidence showed the RIFT window was foreground to
AutoHotkey and an active addon/API waypoint anchor was available. Two offline
memory-match attempts then scanned a known coordinate region and a wider 32-region
capture, but no snapshot contained a player vec3 plus waypoint vec3 pair matching
the addon delta within tolerance.

This is validation evidence, not final waypoint-memory truth.

## Live addon/API anchor

Evidence files:

- `reports/generated/verified-addon-command-proof-wpclear-ahk-backend-arraycheck-20260430-040045.json`
- `reports/generated/addon-api-observation-scan-proof-wpclear-ahk-backend-arraycheck-20260430-040045.json`

Key observed values:

- sender backend: `autohotkey-sendtext`
- sender exit code: `0`
- reload exit code: `0`
- foreground as seen by sender: RIFT PID `41220`
- waypoint anchor count: `1`
- player X/Y/Z: `7237.6196289062`, `873.46997070312`, `3051.0598144531`
- waypoint X/Z: `7257.6196289062`, `3051.0598144531`
- addon/API delta X/Z: `20`, `0`
- anchor confidence: `api_player_to_waypoint_pair`

The wrapper command expected a clear state, so its top-level `success` was false;
however, its scan output still captured a valid active waypoint anchor and proved
the AutoHotkey backend could see RIFT as foreground.

## Captures and matcher results

Known coordinate-region capture:

- session: `sessions/live-waypoint-anchor-20260430-0401-passive`
- regions captured: `1`
- snapshots captured: `8`
- bytes captured: `524288`
- match output: `reports/generated/session-waypoint-anchor-matches-live-20260430-0401.json`
- result: `anchor_count = 1`, `anchors_used = 1`, `match_count = 0`

Wider capture:

- session: `sessions/live-waypoint-anchor-wide-20260430-0402-passive`
- regions captured: `32`
- snapshots captured: `256`
- bytes captured: `16777216`
- match output: `reports/generated/session-waypoint-anchor-matches-live-wide-20260430-0402.json`
- result: `anchor_count = 1`, `anchors_used = 1`, `match_count = 0`

Both matcher outputs included:

```text
no_snapshot_vec3_pair_matches_waypoint_anchors_within_tolerance
waypoint_anchor_matches_are_validation_evidence_not_final_truth
```

## Cleanup observation

`/rbx waypoint-clear` was sent through the AutoHotkey backend and followed by
`/reloadui`. The follow-up scan showed `waypoint_has_waypoint = false` and
`waypoint_anchor_count = 0` in:

- `reports/generated/verified-addon-command-cleanup-waypoint-clear-ahk-20260430-040214.json`
- `reports/generated/addon-api-observation-scan-cleanup-waypoint-clear-ahk-20260430-040214.json`

The wrapper marked that cleanup run unsuccessful only because
`waypoint_last_command` was blank after reload, not because a waypoint remained.

## Interpretation

The active waypoint was available through addon/API truth, but the tested memory
regions did not contain an obvious float32 player/waypoint vec3 pair with the
same X/Z delta. The next search should focus on waypoint-specific storage or
separate scalar/pair matching, rather than assuming waypoint coordinates sit next
to the already known player vec3 family.

## Next smallest action

Add an offline waypoint-scalar/loose-pair scanner that can search for waypoint
X/Z independently across captured regions, then relate those hits back to known
player coordinate candidates and addon delta evidence.

## Follow-up implementation note

The next analyzer lane is now `riftscan rift match-waypoint-scalars`. It is
offline-only and was run against this session/anchor pair:

```powershell
riftscan rift match-waypoint-scalars `
  sessions/live-waypoint-anchor-wide-20260430-0402-passive `
  --anchors reports/generated/addon-api-observation-scan-proof-wpclear-ahk-backend-arraycheck-20260430-040045.json `
  --tolerance 5 `
  --top 100 `
  --out reports/generated/session-waypoint-scalar-matches-live-wide-20260430-0402.json
```

Result:

- `scalar_hit_count = 200`
- `scalar_axis_hit_counts = { waypoint_x = 0, waypoint_z = 200 }`
- `pair_candidate_count = 0`
- key warning: `no_waypoint_x_scalar_hits_within_tolerance`

Interpretation: the wide captured region contains repeated float32 values near
the addon waypoint Z, but no matching waypoint X scalar within tolerance. This
keeps the active waypoint API proof strong while showing the current memory
capture did not include both waypoint axes.
