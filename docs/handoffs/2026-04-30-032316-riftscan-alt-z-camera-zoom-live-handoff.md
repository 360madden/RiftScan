# RiftScan Alt+Z camera zoom live-test handoff

Created local: 2026-04-30 03:23:16 -0400  
Created UTC: 2026-04-30T07:23:16.784057+00:00  
Repository: `C:\RIFT MODDING\Riftscan`  
Branch: `main`  
Reason: preserve the live Alt+Z camera-zoom retry state, invalidated failed attempts, usable artifacts, and next safest actions.

## TL;DR

- The previous mouse camera attempt is invalid: the drag moved the RIFT window rather than the camera.
- The previous `Shift+Z` camera-zoom attempts are invalid: operator corrected the camera zoom keybind to `Alt+Z`.
- New rule for live stimulus: **do not send gameplay stimulus unless the keybind is confirmed by the operator in-chat or decoded from a small targeted game config/keybind source.**
- Current camera zoom confirmation source: operator correction in chat on 2026-04-30: `Alt+Z`.
- A guarded keyboard-only `Alt+Z` live capture succeeded against the verified RIFT window.
- New usable Alt+Z camera-only/zoom session: `sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom`.
- Top camera zoom scalar candidate from this run: `0x975E1D8000+0x17384`, `camera_zoom_angle_scalar_candidate`, score `80`, `camera_only_delta=-0.237074`.
- This is **candidate evidence only**, not final/recovered truth.

## Current repo state when handoff was written

```text
main...origin/main
```

No tracked code changes were present before creating this handoff. Generated sessions/reports are preserved under ignored artifact paths. This handoff itself is a new tracked-doc candidate if you choose to commit it.

## Live target verified for this run

Latest verified target during live test:

```text
process_name: rift_x64
process_id: 41220
window_title: RIFT
hwnd_hex: 0xBD0D94
hwnd_decimal: 12389780
process_start_time_local: 2026-04-28T14:06:20.4022660-04:00
```

Re-verify PID/HWND before any future live input; both can drift.

## Keybind confirmation status

`Alt+Z` was used only after the operator corrected the keybind in chat.

Artifact:

- `reports/generated/live-alt-z-camera-20260430-031557-keybind-confirmation.json`

Targeted config lookup note:

- `C:\Program Files (x86)\Glyph\Games\RIFT\Live\codex_keys.dat` exists, but it is binary/opaque and was **not** used as semantic confirmation.
- Do not do broad recursive text scans through addon/zips/binaries for keybinds. If needed, search only small candidate config/keybind files by name/extension and exclude AddOns, zips, DLLs, EXEs, and large files.

## Invalidated attempts

### 1. Mouse camera-only attempt

Invalid reason: left-mouse drag moved the whole RIFT window instead of the camera. Root cause was unsafe cursor-origin assumption; the drag did not start from a verified client-area center.

Invalidation artifacts already present:

- `reports/generated/live-redo-movement-20260430-025319-camera_only-invalidated.json`
- `reports/generated/live-redo-movement-20260430-025319-camera_only-invalidated.md`

Do not use `sessions/live-redo-movement-20260430-025319-camera_only` for camera-only, camera-yaw, or actor/camera separation claims.

### 2. Shift+Z attempts

Invalid reason: operator corrected camera zoom keybind to `Alt+Z`, not `Shift+Z`.

Invalidation artifacts:

- `reports/generated/live-camera-zoom-shift-z-invalidated-20260430-030847.json`
- `reports/generated/live-camera-zoom-shift-z-invalidated-20260430-030847.md`

Invalidated Shift+Z artifacts include:

- `sessions/live-redo-movement-20260430-025319-camera_only_shift_z_zoom`
- `reports/generated/live-redo-movement-20260430-025319-shift-z-full-scalar-evidence-set.json`
- `reports/generated/live-redo-movement-20260430-025319-shift-z-full-scalar-evidence-set.md`
- `reports/generated/live-redo-movement-20260430-025319-shift-z-full-scalar-truth.jsonl`
- `sessions/live-shift-z-repeat-20260430-030359-camera_only_shift_z_zoom`
- `reports/generated/live-shift-z-repeat-20260430-030359-with-base-turns-scalar-evidence-set.json`
- `reports/generated/live-shift-z-repeat-20260430-030359-with-base-turns-scalar-evidence-set.md`
- `reports/generated/live-shift-z-repeat-20260430-030359-with-base-turns-scalar-truth.jsonl`

Allowed reuse: historical failed-attempt audit only.  
Forbidden reuse: camera-only truth, camera zoom truth, actor/camera signal-separation claim.

### 3. First Alt+Z retry at 20260430-031132

Invalid reason: AutoHotkey guard script had bad generated JSON-string quoting and hung before proving input. The raw session exists but cannot be trusted as a proven Alt+Z camera stimulus.

Invalidation artifacts:

- `reports/generated/live-alt-z-camera-20260430-031132-invalidated.json`
- `reports/generated/live-alt-z-camera-20260430-031132-invalidated.md`

Do not use `sessions/live-alt-z-camera-20260430-031132-camera_only_alt_z_zoom` for camera evidence.

## Valid Alt+Z camera zoom capture

Session:

- `sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom`

Manifest facts:

```json
{
  "session_id": "live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom",
  "process_name": "rift_x64",
  "process_id": 41220,
  "capture_mode": "passive",
  "snapshot_count": 80,
  "region_count": 1,
  "total_bytes_raw": 7864320,
  "total_bytes_stored": 7864320,
  "checksum_algorithm": "SHA256",
  "status": "complete"
}
```

Stimulus guard:

- `reports/generated/live-alt-z-camera-20260430-031557-stimulus.json`

Guard facts:

```json
{
  "status": "ok",
  "stimulus": "Alt+Z camera zoom keyboard-only",
  "target_pid": 41220,
  "target_hwnd_hex": "0xBD0D94",
  "sent_count": 12,
  "press_count": 12,
  "interval_ms": 350,
  "mouse_input_used": false,
  "active_hwnd_before_decimal": 12389780,
  "active_hwnd_after_decimal": 12389780,
  "pre_window_rect": { "x": 23, "y": 19, "w": 1023, "h": 691 },
  "post_window_rect": { "x": 23, "y": 19, "w": 1023, "h": 691 },
  "window_moved": false
}
```

Important caveat: the wrapper initially reported capture timeout because stdout was redirected and waited without concurrent drain. The session manifest/checksums subsequently verified successfully, so the session artifacts are usable.

Run summary:

- `reports/generated/live-alt-z-camera-20260430-031557-capture-run-summary.json`

Human/machine summary:

- `reports/generated/live-alt-z-camera-20260430-031557-summary.md`
- `reports/generated/live-alt-z-camera-20260430-031557-summary.json`

## Validation commands already run

```powershell
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe verify session sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe analyze session sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom --all
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe compare scalar-set sessions/live-redo-movement-20260430-025319-passive_idle sessions/live-redo-movement-20260430-025319-turn_left sessions/live-redo-movement-20260430-025319-turn_right sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom --top 100 --out reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-evidence-set.json --report-md reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-evidence-set.md --truth-out reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-truth.jsonl
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe verify scalar-evidence-set reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-evidence-set.json
```

Results:

- Session verification: passed.
- Session analysis: passed.
- Scalar evidence set verification: passed.
- Evidence set warning remains correct: `scalar_evidence_is_candidate_evidence_not_truth_claim`.

## Scalar evidence outputs

Evidence set:

- `reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-evidence-set.json`
- `reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-evidence-set.md`
- `reports/generated/live-alt-z-camera-20260430-031557-with-base-turns-scalar-truth.jsonl`

Session mix used:

- `sessions/live-redo-movement-20260430-025319-passive_idle`
- `sessions/live-redo-movement-20260430-025319-turn_left`
- `sessions/live-redo-movement-20260430-025319-turn_right`
- `sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom`

Aggregate facts:

```text
session_count: 4
scalar_candidate_key_count: 2030
ranked_candidate_count: 100
```

Top camera/zoom candidate:

```text
address: 0x975E1D8000+0x17384
classification: camera_zoom_angle_scalar_candidate
score_total: 80
confidence_level: strong_candidate
truth_readiness: candidate
value_family: angle_radians_neg_pi_to_pi
camera_only_signed_delta: -0.237074
camera_turn_separation: camera_only_changes_turn_stable
evidence_summary: labels=camera_only,turn_left,turn_right;passive_stable=False;left_delta=0.000000;right_delta=0.000000;camera_delta=-0.237074;camera_turn=camera_only_changes_turn_stable;camera_stimulus=zoom
next_validation_step: add_yaw_or_pitch_camera_only_capture_for_orientation
```

Top actor-yaw separation candidates from same evidence set:

```text
0x975E1D8000+0x1512C actor_yaw_angle_scalar_candidate score=100 left_delta=0.076981 right_delta=-0.076981 camera_delta=0
0x975E1D8000+0x1513C actor_yaw_angle_scalar_candidate score=100 left_delta=-0.274844 right_delta=0.274844 camera_delta=0
0x975E1D8000+0x1518C actor_yaw_angle_scalar_candidate score=100 left_delta=0.173096 right_delta=-0.173096 camera_delta=0
```

Interpretation:

- The Alt+Z zoom lane helps separate actor-yaw candidates from camera zoom; these actor-yaw candidates changed under turn-left/turn-right and stayed stable under Alt+Z.
- `0x17384` is a camera-zoom candidate, not final camera orientation truth.
- Do not promote any scalar as final truth without repeat/corroboration.

## Live-input safety rules to preserve

1. Re-verify exact `rift_x64` PID/HWND/title before live input.
2. Confirm keybind before stimulus. User correction in current turn is acceptable; otherwise decode a targeted config/keybind source first.
3. Prefer keyboard-only stimuli over mouse for camera tests unless a client-area coordinate is explicitly verified.
4. If mouse is ever needed, compute and record the client-area center; never use the current cursor position.
5. Guard every live input with foreground HWND checks and pre/post window rectangle checks.
6. Preserve failed raw sessions and write invalidation artifacts instead of deleting them.
7. Keep live input outside scanner core; helpers are scaffolding only.

## Next smallest action

Repeat the guarded `Alt+Z` capture once more and compare it against `live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom`. Promote `0x17384` only if the signal reproduces.

Suggested command shape after re-verifying PID/HWND:

```powershell
# Re-run the guarded keyboard-only Alt+Z helper flow, then:
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe verify session <new-alt-z-session>
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe analyze session <new-alt-z-session> --all
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe compare scalar-set sessions/live-redo-movement-20260430-025319-passive_idle sessions/live-redo-movement-20260430-025319-turn_left sessions/live-redo-movement-20260430-025319-turn_right sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom <new-alt-z-session> --top 100 --out reports/generated/<repeat-alt-z-prefix>-scalar-evidence-set.json --report-md reports/generated/<repeat-alt-z-prefix>-scalar-evidence-set.md --truth-out reports/generated/<repeat-alt-z-prefix>-scalar-truth.jsonl
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe verify scalar-evidence-set reports/generated/<repeat-alt-z-prefix>-scalar-evidence-set.json
```

## Optional top 5 next best recommended actions

1. Repeat `Alt+Z` once with the same guard to confirm `0x17384` reproducibility.
2. Convert the temporary guarded AHK flow into a reusable script so future key stimuli cannot silently miss guard logging.
3. Create a small `reports/generated/live-keybind-confirmations-YYYYMMDD.json` artifact for operator-confirmed `W`, `A`, `D`, `Alt+Z`, `Tab`, and any camera-yaw/pitch keys.
4. If a true keyboard camera-yaw/pitch key exists, test that separately; zoom is useful but not orientation.
5. Keep all current camera evidence at candidate level until repeated and cross-session corroborated.

## Resume prompt for next conversation

```text
Resume RiftScan from C:\RIFT MODDING\Riftscan. Read the newest handoff docs/handoffs/2026-04-30-032316-riftscan-alt-z-camera-zoom-live-handoff.md first. Continue from the valid guarded Alt+Z session sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom. Do not use the invalid mouse, Shift+Z, or 031132 Alt+Z artifacts for camera truth. Re-verify rift_x64 PID/HWND/title before live input. Do not send gameplay stimulus unless keybind is confirmed by operator or decoded from a targeted config source. Next smallest action: repeat guarded keyboard-only Alt+Z capture and compare scalar evidence to confirm or reject 0x975E1D8000+0x17384 as reproducible camera zoom candidate.
```
