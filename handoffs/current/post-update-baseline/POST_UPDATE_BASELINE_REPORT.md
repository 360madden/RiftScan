# RiftScan Post-Update Baseline Report

## Result

```text
POST-UPDATE BASELINE: BLOCKED
status: blocked_waiting_for_game_or_focus
```

## Blockers

- Maintenance is not confirmed over.
- Login is not confirmed successful.
- Stable in-world state is not confirmed.

## Runtime

```text
focus_status: foreground_verified
selected_window_present: True
windows_entry_count: 1
pid: 11220
hwnd: 657876
title: RIFT
character_name: None
shard: None
zone_or_location: None
```

## Manual State

```text
maintenance_over: False
login_successful: False
world_loaded: False
```

## Safety Boundary

```text
old_offsets_trusted: false
live_capture_allowed: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
reloadui_sent: false
```

## Git Snapshot

```text
branch: main
head: 870a7219add3ed3ecc62ef2dd7e3c3566e3c307d
```

Git status:

```text
 M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl
```

Recent commits:

```text
870a721 Add post-update baseline self-test
8311033 Clarify current handoff commit reference
b4062da Refresh current RiftScan handoff pointer
a666c77 Add operator gate self-test
b07990f Add operator current workflow gate summary
```

## Output Paths

```text
report: handoffs\current\post-update-baseline\POST_UPDATE_BASELINE_REPORT.md
summary: handoffs\current\post-update-baseline\post-update-baseline-summary.json
log: handoffs\current\post-update-baseline\post-update-baseline-log.jsonl
```

## Machine-Readable Summary

```json
{
  "app_version": "riftscan-post-update-baseline-v1.0.1",
  "blockers": [
    "Maintenance is not confirmed over.",
    "Login is not confirmed successful.",
    "Stable in-world state is not confirmed."
  ],
  "created_utc": "2026-05-06T02:53:38Z",
  "display_status": "BLOCKED",
  "focus_command_result": {
    "args": [
      "scripts\\run-rift-focus-control.cmd"
    ],
    "skipped": true,
    "success": true
  },
  "git": {
    "branch": "main",
    "head": "870a7219add3ed3ecc62ef2dd7e3c3566e3c307d",
    "log_oneline_5": "870a721 Add post-update baseline self-test\n8311033 Clarify current handoff commit reference\nb4062da Refresh current RiftScan handoff pointer\na666c77 Add operator gate self-test\nb07990f Add operator current workflow gate summary",
    "status_short": " M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl"
  },
  "manual_state": {
    "login_successful": false,
    "maintenance_over": false,
    "world_loaded": false
  },
  "paths": {
    "log": "handoffs\\current\\post-update-baseline\\post-update-baseline-log.jsonl",
    "report": "handoffs\\current\\post-update-baseline\\POST_UPDATE_BASELINE_REPORT.md",
    "summary": "handoffs\\current\\post-update-baseline\\post-update-baseline-summary.json"
  },
  "runtime": {
    "character_name": null,
    "focus_status": "foreground_verified",
    "hwnd": 657876,
    "pid": 11220,
    "selected_window_present": true,
    "shard": null,
    "title": "RIFT",
    "windows_entry_count": 1,
    "zone_or_location": null
  },
  "safety": {
    "capture_started": false,
    "live_capture_allowed": false,
    "memory_scan_or_read_started": false,
    "movement_or_input_sent": false,
    "old_offsets_trusted": false,
    "reloadui_sent": false
  },
  "schema_version": "riftscan.post_update_baseline.v1",
  "source_artifacts": {
    "focus_summary": {
      "created_utc": "2026-05-06T01:00:48Z",
      "focus": {
        "attempts": [
          {
            "attempt": 1,
            "foreground_hwnd": 657876,
            "foreground_hwnd_hex": "0xA09D4",
            "foreground_pid": 11220,
            "foreground_title": "RIFT",
            "restore_ok": true,
            "set_foreground_ok": true,
            "verified": true
          }
        ],
        "success": true
      },
      "notes": [
        "This local probe uses Win32 foreground APIs.",
        "It does not click the mouse.",
        "It does not send keyboard input.",
        "It does not run /reloadui."
      ],
      "process": {
        "Id": 11220,
        "MainWindowTitle": "RIFT",
        "Path": "C:\\Program Files (x86)\\Glyph\\Games\\RIFT\\Live\\rift_x64.exe",
        "ProcessName": "rift_x64",
        "StartTime": "/Date(1778027723258)/"
      },
      "schema_version": "riftscan.local_focus_control_summary.v1",
      "selected_window": {
        "hwnd": 657876,
        "hwnd_hex": "0xA09D4",
        "pid": 11220,
        "title": "RIFT"
      },
      "status": "foreground_verified"
    },
    "windows": {
      "pid": 11220,
      "windows": [
        {
          "hwnd": 657876,
          "hwnd_hex": "0xA09D4",
          "pid": 11220,
          "title": "RIFT"
        }
      ]
    }
  },
  "status": "blocked_waiting_for_game_or_focus"
}
```
