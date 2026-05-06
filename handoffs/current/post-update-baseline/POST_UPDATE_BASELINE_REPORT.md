# RiftScan Post-Update Baseline Report

## Result

```text
POST-UPDATE BASELINE: PASS
status: pass
```

## Blockers

- None

## Runtime

```text
focus_status: foreground_verified
selected_window_present: True
windows_entry_count: 1
pid: 11220
hwnd: 657876
title: RIFT
character_name: Atank
shard: Deepwood
zone_or_location: Sanctum of the Vigil
```

## Manual State

```text
maintenance_over: True
login_successful: True
world_loaded: True
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
head: 3add5fa4b5c094373cd69ab7e778d66a3eb8afb3
```

Git status:

```text
 M handoffs/current/focus-control-local/FOCUS_CONTROL_HANDOFF.md
 M handoffs/current/focus-control-local/focus-control-log.jsonl
 M handoffs/current/focus-control-local/focus-control-summary.json
 M handoffs/current/focus-control-local/focus-result.json
 M handoffs/current/focus-control-local/process-command-result.json
 M handoffs/current/focus-control-local/process-info.json
 M handoffs/current/focus-control-local/windows.json
?? handoffs/current/post-update-baseline/post-update-baseline-log.jsonl
```

Recent commits:

```text
3add5fa Document post-update baseline implementation
11380e2 Add post-update baseline launcher
d9a43dc Add post-update baseline tool
ea4c950 Add post-update baseline handoff
1cf6759 Add RiftScan resume handoff for transfer/operator guide
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
  "app_version": "riftscan-post-update-baseline-v1.0.0",
  "blockers": [],
  "created_utc": "2026-05-06T01:00:48Z",
  "display_status": "PASS",
  "focus_command_result": {
    "args": [
      "C:\\RIFT MODDING\\Riftscan\\scripts\\run-rift-focus-control.cmd"
    ],
    "returncode": 0,
    "stderr": "",
    "stdout": "Focus control handoff written to C:\\RIFT MODDING\\Riftscan\\handoffs\\current\\focus-control-local\n",
    "success": true
  },
  "git": {
    "branch": "main",
    "head": "3add5fa4b5c094373cd69ab7e778d66a3eb8afb3",
    "log_oneline_5": "3add5fa Document post-update baseline implementation\n11380e2 Add post-update baseline launcher\nd9a43dc Add post-update baseline tool\nea4c950 Add post-update baseline handoff\n1cf6759 Add RiftScan resume handoff for transfer/operator guide",
    "status_short": " M handoffs/current/focus-control-local/FOCUS_CONTROL_HANDOFF.md\n M handoffs/current/focus-control-local/focus-control-log.jsonl\n M handoffs/current/focus-control-local/focus-control-summary.json\n M handoffs/current/focus-control-local/focus-result.json\n M handoffs/current/focus-control-local/process-command-result.json\n M handoffs/current/focus-control-local/process-info.json\n M handoffs/current/focus-control-local/windows.json\n?? handoffs/current/post-update-baseline/post-update-baseline-log.jsonl"
  },
  "manual_state": {
    "login_successful": true,
    "maintenance_over": true,
    "world_loaded": true
  },
  "paths": {
    "log": "handoffs\\current\\post-update-baseline\\post-update-baseline-log.jsonl",
    "report": "handoffs\\current\\post-update-baseline\\POST_UPDATE_BASELINE_REPORT.md",
    "summary": "handoffs\\current\\post-update-baseline\\post-update-baseline-summary.json"
  },
  "runtime": {
    "character_name": "Atank",
    "focus_status": "foreground_verified",
    "hwnd": 657876,
    "pid": 11220,
    "selected_window_present": true,
    "shard": "Deepwood",
    "title": "RIFT",
    "windows_entry_count": 1,
    "zone_or_location": "Sanctum of the Vigil"
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
  "status": "pass"
}
```
