# Focus-Gated Window/Process Metadata Collector

Session ID: `20260505T003619Z_window_process_metadata_collector`
Created UTC: `2026-05-05T00:36:19Z`
Completed UTC: `2026-05-05T00:36:50Z`
Status: `window_process_metadata_collector_completed`
Duration target seconds: `30`
Sample interval ms: `500`
Samples written: `60`
Errors written: `0`

## Result

The operator app ran the first focus-gated metadata collector. It sampled OS/window/process/focus metadata only.

```text
Focus before: foreground_verified
Focus after: foreground_verified
PID: 29420
HWND: 0x4E0F42
Title: RIFT
Focus verified samples: 13
Focus lost samples: 47
```

## Files

```text
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/capture-session-manifest.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/capture-log.jsonl
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/collector-manifest.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/collector-samples.jsonl
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/collector-summary.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/collector-errors.jsonl
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/focus-summary-before.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/focus-summary-after.json
sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector/operator-report.md
```

## Guardrails

- OS/window/process metadata only.
- No process memory read.
- No movement/input sent.
- No /reloadui sent.

## Next Expected Step

Review OS/window/process metadata samples, then decide whether to add RiftReader external collector integration.
