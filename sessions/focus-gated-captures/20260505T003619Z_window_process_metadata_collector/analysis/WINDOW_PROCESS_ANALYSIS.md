# Window/Process Metadata Session Analysis

Created UTC: `2026-05-05T00:37:38Z`
App version: `riftscan-operator-app-v3.8.6`
Session: `sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector`
Status: `FAIL`
Samples: `60`
Anomalies: `3`
Errors: `1`
Warnings: `2`

## Key Checks

- Focus lost count: `47`
- RIFT process dead count: `0`
- Missing sample indexes: `0`
- Duplicate sample indexes: `0`
- Artifact contract: `PASS`

## Files

```text
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/analysis/window-process-analysis.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/analysis/window-process-anomalies.jsonl
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/analysis/WINDOW_PROCESS_ANALYSIS.md
```

## Notes

This analysis is offline-only. It does not run capture, read process memory, send input, change focus, or run /reloadui.

## Anomalies

- `error` `focus_lost` — One or more samples were not focus-verified.
- `warning` `foreground_hwnd_changed` — Foreground HWND changed during session.
- `warning` `foreground_pid_changed` — Foreground PID changed during session.
