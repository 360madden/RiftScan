# Focus-Gated Capture Plan

Plan ID: `20260505T003511Z_focus_gated_capture_plan`
Created UTC: `2026-05-05T00:35:11Z`
Status: `capture_plan_created`
Metadata only: `True`

## Result

The operator app created this metadata-only capture plan after the full live preflight gate passed.

```text
FULL LIVE PREFLIGHT: PASS
Focus: foreground_verified
PID: 29420
HWND: 0x4E0F42
Title: RIFT
```

## Planned Capture Contract

```json
{
  "capture_type": "focus_gated_manual_observation",
  "duration_target_seconds": 30,
  "stimulus_name": "none_metadata_only",
  "expected_files": [
    "capture-session-manifest.json",
    "capture-log.jsonl",
    "focus-summary-before.json",
    "focus-summary-after.json",
    "operator-report.md"
  ],
  "preflight_requirements": [
    "Full live preflight gate PASS",
    "Focus status == foreground_verified",
    "selected_window exists",
    "windows.json has at least one window",
    "RIFT process name == rift_x64",
    "Operator app is the controlling workflow",
    "No uncommitted tool-code changes unless intentionally testing new tool code"
  ],
  "abort_conditions": [
    "Focus preflight fails",
    "RIFT process missing",
    "RIFT HWND missing",
    "Foreground HWND does not belong to RIFT",
    "Git state cannot be read",
    "Operator cancels",
    "Any planned capture file path already exists unexpectedly",
    "Any live-capture command would be required at this stage"
  ]
}
```

## Guardrails

- Metadata only.
- No capture started.
- No live test sequence started.
- No local data collection sequence started.
- No movement/input sent.
- No memory scan/read started.
- No /reloadui sent.

## Manifest

```text
plans/focus-gated-capture-plans/20260505T003511Z_focus_gated_capture_plan/capture-plan.json
```

## Next Expected Step

Use this capture plan as the staging contract before implementing real focus-gated capture.
