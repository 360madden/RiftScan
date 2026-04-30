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

## Distinct waypoint wide-capture and targeted rejection

A third waypoint was set with both axes changed so scalar discovery could avoid
the earlier "waypoint Z equals player Z" ambiguity:

- command proof:
  `reports/generated/verified-addon-command-waypoint-test-123-77-allhits-followup-20260430-050649.json`
- anchor scan:
  `reports/generated/addon-api-observation-scan-waypoint-test-123-77-allhits-followup-20260430-050649.json`
- waypoint X/Z: `7360.6196289062`, `3128.0598144531`

A wider low-pressure passive capture was then taken:

- session: `sessions/live-waypoint-123-77-wide128-20260430-0508-passive`
- regions captured: `128`
- snapshots captured: `1024`
- verified: session verifier passed
- vec3 anchor matcher:
  `reports/generated/session-waypoint-anchor-matches-live-wide128-123-77-20260430-0508.json`
- vec3 result: `match_count = 0`, `candidate_count = 0`
- scalar matcher:
  `reports/generated/session-waypoint-scalar-matches-live-wide128-123-77-20260430-0508.json`
- scalar hits:
  `reports/generated/session-waypoint-scalar-hits-live-wide128-123-77-20260430-0508.jsonl`
- scalar result: `waypoint_x hits = 16`, `waypoint_z hits = 35`,
  `pair_candidate_count = 1`

The one scalar pair lead was:

- base: `0x21681060000`
- X offset: `0x5618`, memory `7359.212890625`
- Z offset: `0xB3DC`, memory `3125.847900390625`
- support: `8` snapshots
- validation status: `waypoint_scalar_pair_supported`

A fourth waypoint changed the anchor by X `+80` and Z `+60`:

- command proof:
  `reports/generated/verified-addon-command-waypoint-test-203-137-target-region-validation-20260430-050848.json`
- anchor scan:
  `reports/generated/addon-api-observation-scan-waypoint-test-203-137-target-region-validation-20260430-050848.json`
- waypoint X/Z: `7440.6196289062`, `3188.0598144531`

Only the lead base was captured for a cheap follow-up:

- session:
  `sessions/live-waypoint-203-137-target-21681060000-20260430-0509-passive`
- base filter: `0x21681060000`
- snapshots captured: `8`
- verified: session verifier passed
- scalar matcher:
  `reports/generated/session-waypoint-scalar-matches-target-21681060000-203-137-20260430-0509.json`
- scalar hits:
  `reports/generated/session-waypoint-scalar-hits-target-21681060000-203-137-20260430-0509.jsonl`
- result: `scalar_hit_count = 0`, `pair_candidate_count = 0`

The coverage-aware comparison output is:

- `reports/generated/waypoint-scalar-comparison-123-77-vs-203-137-target-coverage-aware-20260430-0510.json`
- `classification_counts = { missing_after_waypoint_change = 2, not_captured_in_missing_input = 7 }`
- the two true rejections are the captured lead offsets:
  `0x21681060000+0x5618` and `0x21681060000+0xB3DC`

Interpretation: the third-waypoint wide capture produced a plausible but false
scalar pair. The targeted same-process follow-up captured the lead base after a
known waypoint change and found no matching scalars, so that pair is rejected.
The comparer now avoids over-rejecting offsets from regions that a targeted
follow-up did not capture.

## Wide-256 waypoint follow-up and second targeted rejection

A fifth waypoint used a larger distinct offset to broaden the passive scan:

- command proof:
  `reports/generated/verified-addon-command-waypoint-test-317-223-wide256-20260430-052035.json`
- anchor scan:
  `reports/generated/addon-api-observation-scan-waypoint-test-317-223-wide256-20260430-052035.json`
- waypoint X/Z: `7554.6196289062`, `3274.0598144531`

The broader passive capture was still low-pressure and read-only:

- session: `sessions/live-waypoint-317-223-wide256-20260430-0521-passive`
- regions captured: `256`
- snapshots captured: `2048`
- verified: session verifier passed
- vec3 anchor matcher:
  `reports/generated/session-waypoint-anchor-matches-live-wide256-317-223-20260430-0521.json`
- vec3 result: `match_count = 0`, `candidate_count = 0`
- scalar matcher:
  `reports/generated/session-waypoint-scalar-matches-live-wide256-317-223-20260430-0521.json`
- scalar hits:
  `reports/generated/session-waypoint-scalar-hits-live-wide256-317-223-20260430-0521.jsonl`
- scalar result: `waypoint_x hits = 40`, `waypoint_z hits = 84`,
  `pair_candidate_count = 2`

The two scalar pair leads were:

1. base `0x21682420000`, X offset `0x112C`, Z offset `0xE3D4`,
   support `8`, best distance total `5.734375000075033`
2. base `0x21683F10000`, X offset `0x6094`, Z offset `0xBED8`,
   support `8`, best distance total `6.616699218775011`

Comparing the older wide-128 waypoint state against this wide-256 state
rejected overlap candidates and left only uncaptured-region candidates
unverified:

- comparison:
  `reports/generated/waypoint-scalar-comparison-123-77-vs-317-223-wide-coverage-aware-20260430-0522.json`
- `classification_counts = { missing_after_waypoint_change = 17, not_captured_in_missing_input = 11 }`
- no `tracks_waypoint_candidate` classifications

A sixth waypoint then changed the anchor by X `+186` and Z `+136` from the
wide-256 state:

- command proof:
  `reports/generated/verified-addon-command-waypoint-test-503-359-target-new-leads-20260430-052224.json`
- anchor scan:
  `reports/generated/addon-api-observation-scan-waypoint-test-503-359-target-new-leads-20260430-052224.json`
- waypoint X/Z: `7740.6196289062`, `3410.0598144531`

Only the two wide-256 lead bases were captured for targeted validation:

- session: `sessions/live-waypoint-503-359-target-new-leads-20260430-0523-passive`
- base filters: `0x21682420000`, `0x21683F10000`
- regions captured: `2`
- snapshots captured: `16`
- verified: session verifier passed
- scalar matcher:
  `reports/generated/session-waypoint-scalar-matches-target-new-leads-503-359-20260430-0523.json`
- scalar hits:
  `reports/generated/session-waypoint-scalar-hits-target-new-leads-503-359-20260430-0523.jsonl`
- result: `scalar_hit_count = 0`, `pair_candidate_count = 0`

The targeted comparison output is:

- `reports/generated/waypoint-scalar-comparison-317-223-vs-503-359-target-new-leads-20260430-0523.json`
- `classification_counts = { missing_after_waypoint_change = 4, not_captured_in_missing_input = 15 }`
- the four true rejections are the two X/Z pairs from the captured lead bases:
  - `0x21682420000+0x112C`
  - `0x21682420000+0xE3D4`
  - `0x21683F10000+0x6094`
  - `0x21683F10000+0xBED8`

Final cleanup was verified:

- proof:
  `reports/generated/verified-addon-command-waypoint-clear-after-wide256-loop-20260430-052332.json`
- final state: `waypoint_has_waypoint = false`, `waypoint_anchor_count = 0`

Interpretation: the wider search found two stronger-looking scalar pairs, but
both failed targeted validation after a deliberate waypoint change. The current
best evidence says the captured scalar pairs are environmental/noise mirrors,
not durable player waypoint storage. The next useful improvement is to generate
targeted follow-up plans automatically from scalar-pair outputs rather than
manually copying base addresses.
