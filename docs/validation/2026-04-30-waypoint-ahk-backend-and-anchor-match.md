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

## Differential waypoint follow-up

A second labeled waypoint was set through the foreground-safe AutoHotkey helper
using normal keyboard events:

```powershell
.\scripts\send-rift-slash-command-ahk.ps1 `
  -CommandText "/rbx waypoint-test 40 60" `
  -Focus `
  -OpenChatBeforeCommand `
  -TextMode sendevent
```

After `/reloadui`, the addon/API scan confirmed:

- scan: `reports/generated/addon-api-observation-scan-after-reload-waypoint-diff-40-60-20260430-0435.json`
- player X/Z: `7237.6196289062`, `3051.0598144531`
- waypoint X/Z: `7277.6196289062`, `3111.0598144531`
- delta X/Z: `40`, `60`
- waypoint anchor count: `1`

A new wide passive capture used the same low-pressure shape as the earlier
capture:

- session: `sessions/live-waypoint-anchor2-wide-20260430-0436-passive`
- regions captured: `32`
- snapshots captured: `256`
- bytes captured: `16777216`
- scalar output: `reports/generated/session-waypoint-scalar-matches-live-wide-anchor2-20260430-0436.json`
- result: `waypoint_x hits = 0`, `waypoint_z hits = 0`, `pair_candidate_count = 0`

Conclusion: the earlier `waypoint_z = 3051.059814` scalar hits are not waypoint
storage proof. They appeared because the first test waypoint reused the player's
current Z. When the waypoint Z moved by `+60`, those same capture/scanner
conditions produced zero waypoint scalar hits.

Machine-readable summary:

- `reports/generated/waypoint-differential-summary-20260430-0438.json`

The reusable comparer command for this class of follow-up is:

```powershell
riftscan rift compare-waypoint-scalars `
  reports/generated/session-waypoint-scalar-matches-live-wide-20260430-0402.json `
  reports/generated/session-waypoint-scalar-matches-live-wide-anchor2-20260430-0436.json `
  --delta-tolerance 5 `
  --out reports/generated/waypoint-scalar-comparison-20260430-0438.json
```

Comparer result:

- comparison output: `reports/generated/waypoint-scalar-comparison-20260430-0438.json`
- `comparison_count = 14`
- `classification_counts = { missing_after_waypoint_change = 14 }`

Cleanup was also verified:

- proof: `reports/generated/verified-addon-command-wrapper-proof-waypoint-clear-sendevent-openchat-20260430-20260430-043748.json`
- final state: `waypoint_has_waypoint = false`, `waypoint_anchor_count = 0`

## Retained scalar-hit export follow-up

The scalar matcher now supports a separate retained-hit JSONL artifact so
cross-waypoint comparison is not limited to the top hits embedded in the summary
JSON:

```powershell
riftscan rift match-waypoint-scalars `
  sessions/live-waypoint-anchor-wide-20260430-0402-passive `
  --anchors reports/generated/addon-api-observation-scan-proof-wpclear-ahk-backend-arraycheck-20260430-040045.json `
  --tolerance 5 `
  --top 100 `
  --scalar-hits-out reports/generated/session-waypoint-scalar-hits-live-wide-allhits-20260430-010320.jsonl `
  --out reports/generated/session-waypoint-scalar-matches-live-wide-allhits-20260430-010320.json
```

The same command was run against
`sessions/live-waypoint-anchor2-wide-20260430-0436-passive`, producing:

- `reports/generated/session-waypoint-scalar-matches-live-wide-anchor2-allhits-20260430-010320.json`
- `reports/generated/session-waypoint-scalar-hits-live-wide-anchor2-allhits-20260430-010320.jsonl`

The all-retained-hit comparison output is:

- `reports/generated/waypoint-scalar-comparison-allhits-20260430-010320.json`
- `comparison_count = 25`
- `classification_counts = { missing_after_waypoint_change = 25 }`
- comparison warnings only include
  `waypoint_scalar_comparison_is_validation_evidence_not_final_truth`

Interpretation: the top-hit truncation blind spot is removed for this replay.
All 200 retained scalar hits from the first waypoint state were available to the
comparer, and none persisted after the deliberate waypoint-Z change. The earlier
`waypoint_z` scalar lead remains rejected.
