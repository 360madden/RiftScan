# Manual live testing stale-guard wrapper

This repo includes a script-only wrapper for live RiftScan captures when Codex is not available:

```powershell
.\scripts\live-test-riftscan.cmd -Stimulus move_forward
```

The wrapper does not add new scanner features. It orchestrates existing RiftReader and RiftScan commands and writes machine-readable artifacts under `reports/generated/manual-live-test-<timestamp>/`.

## What it checks before capture

- Resolves exactly one live `rift_x64` process, or uses `-ProcessId`.
- Records PID, process path, title, and process start time.
- Finds the newest `ReaderBridgeExport.lua`, or uses `-ReaderBridgeFile`.
- Blocks if the ReaderBridge export is older than `-MaxReaderBridgeAgeSeconds`.
- Runs RiftReader `--read-player-coord-anchor --json`.
- Blocks unless the source object coordinate sample matches ReaderBridge within tolerance.
- Builds RiftScan `--base-addresses` from the fresh source object and trace object.
- Uses RiftScan `--pre-capture-wait-ms` so the user can start manual movement before snapshots begin.

## Important limitation

This is a stale-data reduction wrapper, not final truth automation. It avoids most common stale-data mistakes, but final proof still depends on the generated delta summary:

- `stimulus_observed_primary_triplet_changed` means the primary triplet changed during capture.
- `stimulus_not_observed_or_no_primary_triplet_delta` means do not treat the run as movement proof.

## Examples

Preflight only:

```powershell
.\scripts\live-test-riftscan.cmd -PreflightOnly
```

Manual forward movement capture:

```powershell
.\scripts\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 10000
```

Use explicit PID and add a known owner/bridge base address:

```powershell
.\scripts\live-test-riftscan.cmd -ProcessId 32468 -Stimulus move_forward -ExtraBaseAddress 0x216BE6A0000
```

Allow an unproven capture only when intentionally collecting negative/control evidence:

```powershell
.\scripts\live-test-riftscan.cmd -Stimulus passive_idle -AllowUnproven
```
