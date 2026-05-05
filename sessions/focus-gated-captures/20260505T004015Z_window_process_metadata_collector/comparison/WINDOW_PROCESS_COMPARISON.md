# Window/Process Metadata Session Comparison

Created UTC: `2026-05-05T00:45:34Z`
App version: `riftscan-operator-app-v3.8.6`
Status: `PASS`
Previous session: `sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector`
Latest session: `sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector`
Differences: `5`
Errors: `0`
Warnings: `5`

## Key Deltas

- Sample count: `60` -> `60`
- Focus lost count: `47` -> `0`
- Artifact contract: `PASS` -> `PASS`
- Analysis status: `FAIL` -> `PASS`

## Files

```text
sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/comparison/window-process-comparison.json
sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/comparison/window-process-comparison-differences.jsonl
sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/comparison/WINDOW_PROCESS_COMPARISON.md
```

## Notes

This comparison is offline-only. It does not run capture, read process memory, send input, change focus, or run /reloadui.

## Differences

- `warning` `analysis_status` — Analysis status changed. Previous=`FAIL` Latest=`PASS`
- `warning` `analysis_anomaly_count` — Analysis anomaly count changed. Previous=`3` Latest=`0`
- `warning` `unique_foreground_hwnds` — Foreground HWND set changed. Previous=`['0x4E0F42', '0x9AD0DEC']` Latest=`['0x4E0F42']`
- `warning` `unique_foreground_pids` — Foreground PID set changed. Previous=`[29420, 61120]` Latest=`[29420]`
- `warning` `focus_lost_changed` — Focus lost count changed. Previous=`47` Latest=`0`
