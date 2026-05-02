# RiftScan Step 1 Baseline Handoff

## Purpose

This handoff contains the result of a single standing baseline capture.

No movement was attempted.

## Commands

Preflight:

```powershell
.\scripts\live-test-riftscan.cmd -PreflightOnly
```

Capture:

```powershell
.\scripts\live-test-riftscan.cmd -Stimulus passive_idle -PreCaptureWaitMilliseconds 0
```

## Source run

- Run directory: `reports/generated/manual-live-test-20260502-124424`
- Session directory: `None`

## Result

- Preflight success: `False`
- Capture success: `None`
- Verify success: `None`
- Analyze success: `None`
- Delta interpretation: `None`

## Diagnostics

- Timestamped log: `handoffs/current/step1-log.jsonl`

## Interpretation

For Step 1, this is a baseline capture at the player's current standing location.

For this baseline step, no primary movement delta is not automatically a failure.

## Files included

See `files-included.json`.

## Files excluded

See `files-excluded.json`.
