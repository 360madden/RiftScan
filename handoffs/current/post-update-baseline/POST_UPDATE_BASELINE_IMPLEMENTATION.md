# RiftScan Post-Update Baseline Implementation

## Purpose

This file documents the offline implementation added while the RIFT client was down after the May 5, 2026 update/maintenance window.

## Added Files

```text
tools/riftscan_post_update_baseline.py
scripts/run-riftscan-post-update-baseline.cmd
```

## Behavior

The baseline tool writes:

```text
handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md
handoffs/current/post-update-baseline/post-update-baseline-summary.json
handoffs/current/post-update-baseline/post-update-baseline-log.jsonl
```

It records:

```text
git branch/head/status/log
focus-control summary
windows.json summary
manual maintenance/login/world-loaded flags
old_offsets_trusted=false
live_capture_allowed=false
capture_started=false
movement_or_input_sent=false
memory_scan_or_read_started=false
reloadui_sent=false
```

## Safety Boundary

This implementation does **not**:

```text
capture memory
scan memory
run coordinate recovery
send keyboard input
send mouse input
move the character
issue /reloadui
auto-commit
auto-push
```

## Recommended Use After Maintenance

```powershell
cd "C:\RIFT MODDING\Riftscan"
git pull --ff-only
.\scripts\run-riftscan-post-update-baseline.cmd --assume-in-world --character-name Atank --shard Deepwood --zone-or-location "Sanctum of the Vigil"
```

If the game is still down, run without `--assume-in-world`; the report should be `BLOCKED`, which is valid and expected.

## Next Integration Step

Wire this tool into the Operator GUI as a button named:

```text
Post-Update Baseline
```

The GUI button should call the script or equivalent internal function and display the same PASS/BLOCKED blocker list.
