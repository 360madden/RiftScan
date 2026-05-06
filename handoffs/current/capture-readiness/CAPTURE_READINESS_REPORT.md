# RiftScan Capture Readiness Report

## Result

```text
CAPTURE READINESS: BLOCKED
status: blocked_waiting_for_current_baseline
```

## Blockers

- Post-update baseline is not PASS for the current client.
- Post-update baseline display_status is not PASS.

## Gate Summary

```text
post_update_baseline_status: blocked_waiting_for_game_or_focus
post_update_baseline_display_status: BLOCKED
current_focus_status: foreground_verified
selected_window_present: True
windows_entry_count: 1
pid: 11220
hwnd: 657876
title: RIFT
```

## Safety Boundary

```text
old_offsets_trusted: false
capture_started: false
live_collection_allowed: false
capture_planning_allowed: false
movement_or_input_sent: false
memory_scan_or_read_started: false
reloadui_sent: false
```

## Output Paths

```text
report: handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md
summary: handoffs/current/capture-readiness/capture-readiness-summary.json
log: handoffs/current/capture-readiness/capture-readiness-log.jsonl
```

## Next Step

Run a fresh Post-Update Baseline after the current updated client is confirmed stable in-world.

## Git Snapshot

```text
branch: main
head: 27ecb05d766213c356d9c708a6ce735fa708f05e
```

Git status:

```text
 M docs/helper-tooling-policy.md
 M handoffs/current/README_CURRENT.md
 M tools/riftscan_operator_app.py
?? handoffs/current/capture-readiness/
?? scripts/run-riftscan-capture-readiness.cmd
?? tools/riftscan_capture_readiness.py

```

Recent commits:

```text
27ecb05 Document Python helper tooling direction
f0f0362 Wire post-update baseline into operator app
b9868ba Add next-step workflow handoff
115c31a Record post-update baseline pass
3add5fa Document post-update baseline implementation
```

## Machine-Readable Summary

```json
{
  "app_version": "riftscan-capture-readiness-v1.0.0",
  "baseline": {
    "created_utc": "2026-05-06T01:32:34Z",
    "display_status": "BLOCKED",
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
    "status": "blocked_waiting_for_game_or_focus",
    "summary_path": "handoffs/current/post-update-baseline/post-update-baseline-summary.json"
  },
  "blockers": [
    "Post-update baseline is not PASS for the current client.",
    "Post-update baseline display_status is not PASS."
  ],
  "created_utc": "2026-05-06T01:57:17Z",
  "display_status": "BLOCKED",
  "focus_command_result": {
    "args": [
      "scripts/run-rift-focus-control.cmd"
    ],
    "skipped": true,
    "success": true
  },
  "git": {
    "branch": "main",
    "head": "27ecb05d766213c356d9c708a6ce735fa708f05e",
    "log_oneline_5": "27ecb05 Document Python helper tooling direction\nf0f0362 Wire post-update baseline into operator app\nb9868ba Add next-step workflow handoff\n115c31a Record post-update baseline pass\n3add5fa Document post-update baseline implementation",
    "status_short": " M docs/helper-tooling-policy.md\n M handoffs/current/README_CURRENT.md\n M tools/riftscan_operator_app.py\n?? handoffs/current/capture-readiness/\n?? scripts/run-riftscan-capture-readiness.cmd\n?? tools/riftscan_capture_readiness.py\n"
  },
  "next_step": "Run a fresh Post-Update Baseline after the current updated client is confirmed stable in-world.",
  "paths": {
    "log": "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
    "report": "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
    "summary": "handoffs/current/capture-readiness/capture-readiness-summary.json"
  },
  "runtime": {
    "focus_status": "foreground_verified",
    "hwnd": 657876,
    "pid": 11220,
    "selected_window_present": true,
    "title": "RIFT",
    "windows_entry_count": 1
  },
  "safety": {
    "capture_planning_allowed": false,
    "capture_started": false,
    "live_collection_allowed": false,
    "memory_scan_or_read_started": false,
    "movement_or_input_sent": false,
    "old_offsets_trusted": false,
    "reloadui_sent": false
  },
  "schema_version": "riftscan.capture_readiness.v1",
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
    "post_update_baseline_summary": {
      "app_version": "riftscan-post-update-baseline-v1.0.0",
      "blockers": [
        "Maintenance is not confirmed over.",
        "Login is not confirmed successful.",
        "Stable in-world state is not confirmed."
      ],
      "created_utc": "2026-05-06T01:32:34Z",
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
        "head": "b9868bac4c85557ae0598cbfdeb5226b98315024",
        "log_oneline_5": "b9868ba Add next-step workflow handoff\n115c31a Record post-update baseline pass\n3add5fa Document post-update baseline implementation\n11380e2 Add post-update baseline launcher\nd9a43dc Add post-update baseline tool",
        "status_short": " M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md\n M handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md\n M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl\n M handoffs/current/post-update-baseline/post-update-baseline-summary.json\n M tools/riftscan_operator_app.py"
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
  "status": "blocked_waiting_for_current_baseline"
}
```
