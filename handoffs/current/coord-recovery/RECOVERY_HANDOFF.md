# RiftScan Fresh Coordinate Recovery Probe

## Purpose

This handoff contains a fresh coordinate candidate scan for RIFT after prior coordinate anchors/offsets became suspect.

This does not claim recovered coordinate truth.

## Scope

- RiftScan repository only.
- RiftReader is called as an external command-line tool.
- RiftReader files are not modified.
- No movement automation.
- No foreground automation.
- No old anchor is required.

## Result

- Status: `coordinate_candidates_observed`
- Process ID: `33812`
- Command success: `True`
- Return code: `0`
- Hit count: `32`
- Candidate-like value count: `32`

## Files

- `coord-recovery-summary.json`
- `riftreader-scan-readerbridge-player-coords.json`
- `command-result.json`
- `process-info.json`
- `step-log.jsonl`
- `files-included.json`
- `files-excluded.json`

## Interpretation

If hits are present, they are fresh coordinate candidates only. The next phase must validate candidates with capture and movement contrast before promoting any new anchor.
