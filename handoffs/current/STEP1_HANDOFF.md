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

- Run directory: `reports/generated/manual-live-test-20260502-122551`
- Session directory: `None`

## Result

- Preflight success: `False`
- Capture success: `None`
- Verify success: `None`
- Analyze success: `None`
- Delta interpretation: `None`

## Interpretation

For Step 1, this is a baseline capture at the player's current standing location.

Expected result:

- The session should verify.
- Analysis should complete.
- The primary coordinate triplet may stay stable.
- `stimulus_not_observed_or_no_primary_triplet_delta` is not automatically a failure for this baseline step.

## Files included

See `files-included.json`.

## Files excluded

See `files-excluded.json`.

Raw memory snapshot binaries are intentionally excluded from the handoff.
