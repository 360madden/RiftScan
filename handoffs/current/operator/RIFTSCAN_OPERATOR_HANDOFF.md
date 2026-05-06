# RiftScan Operator Handoff

Created UTC: `2026-05-06T03:37:16Z`
App version: `riftscan-operator-app-v3.8.17`
Repo root: `C:\RIFT MODDING\Riftscan`

## Operator Assessment

Full live preflight gate: `PASS`
Focus preflight: `PASS`
Summary: `status=foreground_verified pid=11220 hwnd=0xA09D4 title=RIFT`

- No blocking focus/git issues detected; see Current Workflow Gate for baseline/readiness state.

## Current Workflow Gate

Summary path: `handoffs/current/operator/operator-current-gate-summary.json`

```text
metadata_capture_plan_gate: BLOCKED
post_update_baseline: BLOCKED
post_update_baseline_freshness: warning_non_relevant_changes
capture_readiness: BLOCKED
capture_readiness_freshness: warning_non_relevant_changes
capture_readiness_baseline_link: match
full_live_preflight: PASS
focus_preflight: PASS
live_collection_allowed: false
old_offsets_trusted: false
next_action: Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.

blockers:
- Post-Update Baseline is not PASS for the current updated client.
- Maintenance is not confirmed over.
- Login is not confirmed successful.
- Stable in-world state is not confirmed.
- Capture Readiness is not PASS.
- Post-update baseline is not PASS for the current client.
- Post-update baseline display_status is not PASS.
```

```json
{
  "schema_version": "riftscan.operator_current_workflow_gate.v1",
  "created_utc": "2026-05-06T03:37:16Z",
  "metadata_capture_plan_gate": "BLOCKED",
  "live_collection_allowed": false,
  "old_offsets_trusted": false,
  "post_update_baseline": {
    "label": "Post-Update Baseline",
    "artifact_status": "present",
    "status": "blocked_waiting_for_game_or_focus",
    "display_status": "BLOCKED",
    "created_utc": "2026-05-06T02:53:38Z",
    "blockers": [
      "Maintenance is not confirmed over.",
      "Login is not confirmed successful.",
      "Stable in-world state is not confirmed."
    ],
    "paths": {
      "report": "handoffs\\current\\post-update-baseline\\POST_UPDATE_BASELINE_REPORT.md",
      "summary": "handoffs\\current\\post-update-baseline\\post-update-baseline-summary.json",
      "log": "handoffs\\current\\post-update-baseline\\post-update-baseline-log.jsonl"
    },
    "artifact_freshness": {
      "status": "warning_non_relevant_changes",
      "artifact_head": "870a7219add3ed3ecc62ef2dd7e3c3566e3c307d",
      "current_head": "b3bb14df4fbce6e43cba4dece49be072684bd5ff",
      "head_matches_current": false,
      "changed_paths_since_artifact_head": [
        ".gitignore",
        "docs/helper-tooling-policy.md",
        "handoffs/current/README_CURRENT.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_TRANSFER_OPERATOR_GUIDE.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md",
        "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
        "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
        "handoffs/current/capture-readiness/capture-readiness-summary.json",
        "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
        "handoffs/current/operator/operator-current-gate-summary.json",
        "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
        "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl",
        "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
        "scripts/run-riftscan-offline-workflow-check.cmd",
        "scripts/run-riftscan-operator-report.cmd",
        "tools/riftscan_offline_workflow_check.py",
        "tools/riftscan_operator_app.py",
        "tools/riftscan_patch_intake_app.py"
      ],
      "relevant_gate_code_changed": false,
      "relevant_changed_paths": []
    }
  },
  "capture_readiness": {
    "label": "Capture Readiness",
    "artifact_status": "present",
    "status": "blocked_waiting_for_current_baseline",
    "display_status": "BLOCKED",
    "created_utc": "2026-05-06T02:53:39Z",
    "blockers": [
      "Post-update baseline is not PASS for the current client.",
      "Post-update baseline display_status is not PASS."
    ],
    "paths": {
      "report": "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
      "summary": "handoffs/current/capture-readiness/capture-readiness-summary.json",
      "log": "handoffs/current/capture-readiness/capture-readiness-log.jsonl"
    },
    "artifact_freshness": {
      "status": "warning_non_relevant_changes",
      "artifact_head": "870a7219add3ed3ecc62ef2dd7e3c3566e3c307d",
      "current_head": "b3bb14df4fbce6e43cba4dece49be072684bd5ff",
      "head_matches_current": false,
      "changed_paths_since_artifact_head": [
        ".gitignore",
        "docs/helper-tooling-policy.md",
        "handoffs/current/README_CURRENT.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_TRANSFER_OPERATOR_GUIDE.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md",
        "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
        "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
        "handoffs/current/capture-readiness/capture-readiness-summary.json",
        "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
        "handoffs/current/operator/operator-current-gate-summary.json",
        "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
        "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl",
        "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
        "scripts/run-riftscan-offline-workflow-check.cmd",
        "scripts/run-riftscan-operator-report.cmd",
        "tools/riftscan_offline_workflow_check.py",
        "tools/riftscan_operator_app.py",
        "tools/riftscan_patch_intake_app.py"
      ],
      "relevant_gate_code_changed": false,
      "relevant_changed_paths": []
    }
  },
  "capture_readiness_baseline_link": {
    "status": "match",
    "current_baseline": {
      "created_utc": "2026-05-06T02:53:38Z",
      "status": "blocked_waiting_for_game_or_focus",
      "display_status": "BLOCKED",
      "runtime": {
        "pid": 11220,
        "hwnd": 657876
      }
    },
    "readiness_baseline": {
      "created_utc": "2026-05-06T02:53:38Z",
      "status": "blocked_waiting_for_game_or_focus",
      "display_status": "BLOCKED",
      "runtime": {
        "pid": 11220,
        "hwnd": 657876
      }
    },
    "mismatches": []
  },
  "full_live_preflight": "PASS",
  "focus_preflight": "PASS",
  "blockers": [
    "Post-Update Baseline is not PASS for the current updated client.",
    "Maintenance is not confirmed over.",
    "Login is not confirmed successful.",
    "Stable in-world state is not confirmed.",
    "Capture Readiness is not PASS.",
    "Post-update baseline is not PASS for the current client.",
    "Post-update baseline display_status is not PASS."
  ],
  "next_action": "Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.",
  "guardrail": "No live capture, discovery, movement/input, memory scan/read, offset validation, RiftReader validation, or /reloadui until current gates pass and an explicit future live gate is added."
}
```

## Git Status

Exit code: `0`

```text
 M handoffs/current/README_CURRENT.md
 M handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md
 M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
 M handoffs/current/operator/operator-current-gate-summary.json
?? handoffs/current/offline-workflow-check/

```

## Recent Commits

Exit code: `0`

```text
b3bb14d Add offline workflow check helper
0125e33 Refresh handoff after operator report wrapper
a2cf481 Add operator report command wrapper
a312fe1 Refresh current handoff after operator intake check
430f0b4 Add operator self-test patch intake check

```

## Focus Summary JSON

```json
{
  "schema_version": "riftscan.local_focus_control_summary.v1",
  "created_utc": "2026-05-06T01:00:48Z",
  "status": "foreground_verified",
  "process": {
    "Id": 11220,
    "ProcessName": "rift_x64",
    "Path": "C:\\Program Files (x86)\\Glyph\\Games\\RIFT\\Live\\rift_x64.exe",
    "MainWindowTitle": "RIFT",
    "StartTime": "/Date(1778027723258)/"
  },
  "selected_window": {
    "hwnd": 657876,
    "hwnd_hex": "0xA09D4",
    "pid": 11220,
    "title": "RIFT"
  },
  "focus": {
    "success": true,
    "attempts": [
      {
        "attempt": 1,
        "restore_ok": true,
        "set_foreground_ok": true,
        "foreground_hwnd": 657876,
        "foreground_hwnd_hex": "0xA09D4",
        "foreground_pid": 11220,
        "foreground_title": "RIFT",
        "verified": true
      }
    ]
  },
  "notes": [
    "This local probe uses Win32 foreground APIs.",
    "It does not click the mouse.",
    "It does not send keyboard input.",
    "It does not run /reloadui."
  ]
}
```

## Windows JSON

```json
{
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
```

## Latest Post-Update Baseline

```json
{
  "status": "present",
  "report_path": "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
  "summary_path": "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
  "log_path": "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl",
  "summary": {
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
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Capture Readiness

```json
{
  "status": "present",
  "report_path": "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
  "summary_path": "handoffs/current/capture-readiness/capture-readiness-summary.json",
  "log_path": "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
  "summary": {
    "app_version": "riftscan-capture-readiness-v1.0.1",
    "baseline": {
      "created_utc": "2026-05-06T02:53:38Z",
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
    "created_utc": "2026-05-06T02:53:39Z",
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
      "head": "870a7219add3ed3ecc62ef2dd7e3c3566e3c307d",
      "log_oneline_5": "870a721 Add post-update baseline self-test\n8311033 Clarify current handoff commit reference\nb4062da Refresh current RiftScan handoff pointer\na666c77 Add operator gate self-test\nb07990f Add operator current workflow gate summary",
      "status_short": " M handoffs/current/capture-readiness/capture-readiness-log.jsonl\n M handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md\n M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl\n M handoffs/current/post-update-baseline/post-update-baseline-summary.json\n"
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
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Offline Workflow Check

```json
{
  "status": "present",
  "report_path": "handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md",
  "summary_path": "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json",
  "log_path": "handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl",
  "summary": {
    "app_version": "riftscan-offline-workflow-check-v1.0.0",
    "checks": [
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "-m",
          "py_compile",
          "tools/riftscan_operator_app.py",
          "tools/riftscan_post_update_baseline.py",
          "tools/riftscan_capture_readiness.py",
          "tools/riftscan_patch_intake_app.py",
          "tools/riftscan_offline_workflow_check.py"
        ],
        "exit_code": 0,
        "name": "py_compile_helpers",
        "status": "pass",
        "stderr": "",
        "stdout": ""
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_offline_workflow_check.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "offline_workflow_check_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-offline-workflow-check-v1.0.0\",\n  \"case_count\": 2,\n  \"created_utc\": \"2026-05-06T03:36:50Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.offline_workflow_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"expected\": \"pass\",\n      \"failed_checks\": [],\n      \"name\": \"all pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"expected\": \"fail\",\n      \"failed_checks\": [\n        \"b\"\n      ],\n      \"name\": \"one fail\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_operator_app.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "operator_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"schema_version\": \"riftscan.operator_self_test.v1\",\n  \"created_utc\": \"2026-05-06T03:36:50Z\",\n  \"app_version\": \"riftscan-operator-app-v3.8.17\",\n  \"status\": \"PASS\",\n  \"case_count\": 6,\n  \"tests\": [\n    {\n      \"name\": \"all gates pass\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Refresh the metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"baseline blocks even when preflight passes\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.\",\n      \"blockers\": [\n        \"Post-Update Baseline is not PASS for the current updated client.\",\n        \"Stable in-world state is not confirmed.\",\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"readiness blocks after baseline pass\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness and resolve any blockers before capture-plan refresh.\",\n      \"blockers\": [\n        \"Capture Readiness is not PASS.\",\n        \"Post-update baseline is not PASS for the current client.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"stale readiness baseline link blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness again against the latest Post-Update Baseline.\",\n      \"blockers\": [\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"full live preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus status is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"focus preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus preflight is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    }\n  ],\n  \"safety\": {\n    \"writes_artifacts\": false,\n    \"launches_gui\": false,\n    \"runs_focus_preflight\": false,\n    \"capture_started\": false,\n    \"movement_or_input_sent\": false,\n    \"memory_scan_or_read_started\": false,\n    \"reloadui_sent\": false\n  }\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_post_update_baseline.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "post_update_baseline_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-post-update-baseline-v1.0.1\",\n  \"case_count\": 6,\n  \"created_utc\": \"2026-05-06T03:36:50Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.post_update_baseline_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass baseline\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Maintenance is not confirmed over.\",\n        \"Login is not confirmed successful.\",\n        \"Stable in-world state is not confirmed.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Maintenance is not confirmed over\",\n        \"Login is not confirmed successful\",\n        \"Stable in-world state is not confirmed\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked manual state\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Focus preflight command did not complete successfully.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"selected_window is missing or null.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"selected_window is missing or null\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked missing selected window\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"windows.json has no window entries.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"windows.json has no window entries\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked empty windows list\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_capture_readiness.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "capture_readiness_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-capture-readiness-v1.0.1\",\n  \"case_count\": 7,\n  \"created_utc\": \"2026-05-06T03:36:50Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Post-update baseline is not PASS for the current client.\",\n        \"Post-update baseline display_status is not PASS.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Post-update baseline is not PASS\",\n        \"display_status is not PASS\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked baseline status\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Safety field baseline.safety.old_offsets_trusted is not false.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"baseline.safety.old_offsets_trusted\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked unsafe baseline safety\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus status is not foreground_verified.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus status is not foreground_verified\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked current focus lost\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current RIFT PID differs from the post-update baseline; rerun Post-Update Baseline.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current RIFT PID differs\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked pid drift\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus preflight command did not complete successfully.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_patch_intake_app.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "patch_intake_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-patch-intake-v1.2.5\",\n  \"created_utc\": \"2026-05-06T03:36:54Z\",\n  \"schema_version\": \"riftscan.patch_intake_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"empty payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"wrong header\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_MANIFEST\",\n      \"expected\": \"FAIL_BAD_MANIFEST\",\n      \"issues\": [\n        \"JSONDecodeError: Expecting property name enclosed in double quotes: line 1 column 2 (char 1)\"\n      ],\n      \"name\": \"bad json\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_PAYLOAD\",\n      \"expected\": \"FAIL_MISSING_PAYLOAD\",\n      \"issues\": [\n        \"Payload block markers are missing.\"\n      ],\n      \"name\": \"missing payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_HASH_MISMATCH\",\n      \"expected\": \"FAIL_HASH_MISMATCH\",\n      \"issues\": [],\n      \"name\": \"hash mismatch\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_STALE_PATCH\",\n      \"expected\": \"FAIL_STALE_PATCH\",\n      \"issues\": [\n        \"Patch timestamp is not newer than last accepted patch.\"\n      ],\n      \"name\": \"stale timestamp\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_WRONG_REPO\",\n      \"expected\": \"FAIL_WRONG_REPO\",\n      \"issues\": [\n        \"target_repo_root does not match selected repo root.\"\n      ],\n      \"name\": \"wrong repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"valid dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"expected\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"issues\": [\n        \"No successful process/apply result exists.\"\n      ],\n      \"name\": \"commit without apply\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload without commit metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"expected\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"issues\": [\n        \"Manifest commit block is required.\"\n      ],\n      \"name\": \"commit missing metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"expected\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"issues\": [\n        \"Unsafe commit.stage_paths entry: .\"\n      ],\n      \"name\": \"unsafe commit stage path validation\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload with capture readiness checks\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_COMMITTED\",\n      \"expected\": \"PASS_COMMITTED\",\n      \"issues\": [],\n      \"name\": \"commit in temp repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"expected\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"issues\": [],\n      \"name\": \"push verify simulated\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"chunked dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"expected\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"issues\": [\n        \"chunk 1 hash mismatch\"\n      ],\n      \"name\": \"chunked bad chunk hash\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_CHUNK\",\n      \"expected\": \"FAIL_MISSING_CHUNK\",\n      \"issues\": [\n        \"missing chunks: 1\"\n      ],\n      \"name\": \"chunked missing chunk\",\n      \"pass\": true\n    }\n  ]\n}\n"
      }
    ],
    "created_utc": "2026-05-06T03:36:54Z",
    "display_status": "PASS",
    "failed_check_count": 0,
    "failed_checks": [],
    "git": {
      "command_status": {
        "head": "pass",
        "log": "pass",
        "status": "pass"
      },
      "head": "b3bb14df4fbce6e43cba4dece49be072684bd5ff",
      "log_oneline_5": "b3bb14d Add offline workflow check helper\n0125e33 Refresh handoff after operator report wrapper\na2cf481 Add operator report command wrapper\na312fe1 Refresh current handoff after operator intake check\n430f0b4 Add operator self-test patch intake check",
      "status_short": " M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md\n M handoffs/current/operator/operator-current-gate-summary.json\n?? handoffs/current/offline-workflow-check/\n"
    },
    "paths": {
      "log": "handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl",
      "report": "handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md",
      "summary": "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json"
    },
    "safety": {
      "capture_started": false,
      "focus_preflight_started": false,
      "memory_scan_or_read_started": false,
      "movement_or_input_sent": false,
      "offline_only": true,
      "offset_validation_started": false,
      "reloadui_sent": false,
      "riftreader_validation_started": false
    },
    "schema_version": "riftscan.offline_workflow_check.v1",
    "status": "pass"
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Focus-Gated Session Dry Run

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-dry-runs/20260503T075023Z_focus_gated_session_dry_run",
  "manifest_path": "sessions/focus-gated-dry-runs/20260503T075023Z_focus_gated_session_dry_run/manifest.json",
  "manifest": {
    "schema_version": "riftscan.focus_gated_session_dry_run.v1",
    "created_utc": "2026-05-03T07:50:23Z",
    "app_version": "riftscan-operator-app-v3",
    "session_id": "20260503T075023Z_focus_gated_session_dry_run",
    "status": "dry_run_session_created",
    "dry_run": true,
    "full_live_preflight": {
      "status": "PASS",
      "focus_status": "foreground_verified",
      "process_id": 29420,
      "process_name": "rift_x64",
      "window_hwnd": 5115714,
      "window_hwnd_hex": "0x4E0F42",
      "window_title": "RIFT",
      "windows_count": 1
    },
    "guardrails": [
      "No live test sequence was started.",
      "No local data collection sequence was started.",
      "This session is metadata-only."
    ],
    "source_artifacts": {
      "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
      "windows_json": "handoffs/current/focus-control-local/windows.json",
      "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
      "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md"
    },
    "next_expected_step": "Use this metadata-only session structure as the staging contract before wiring the first real focus-gated live-test workflow."
  }
}
```

## Latest Focus-Gated Capture Plan

```json
{
  "status": "present",
  "latest_plan": "plans/focus-gated-capture-plans/20260505T003511Z_focus_gated_capture_plan",
  "manifest_path": "plans/focus-gated-capture-plans/20260505T003511Z_focus_gated_capture_plan/capture-plan.json",
  "handoff_path": "plans/focus-gated-capture-plans/20260505T003511Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
  "manifest": {
    "schema_version": "riftscan.focus_gated_capture_plan.v1",
    "created_utc": "2026-05-05T00:35:11Z",
    "app_version": "riftscan-operator-app-v3.8.6",
    "plan_id": "20260505T003511Z_focus_gated_capture_plan",
    "status": "capture_plan_created",
    "metadata_only": true,
    "capture_started": false,
    "capture_completed": false,
    "capture_type": "focus_gated_manual_observation",
    "duration_target_seconds": 30,
    "stimulus_name": "none_metadata_only",
    "expected_files": [
      "capture-session-manifest.json",
      "capture-log.jsonl",
      "focus-summary-before.json",
      "focus-summary-after.json",
      "operator-report.md"
    ],
    "preflight_requirements": [
      "Full live preflight gate PASS",
      "Focus status == foreground_verified",
      "selected_window exists",
      "windows.json has at least one window",
      "RIFT process name == rift_x64",
      "Operator app is the controlling workflow",
      "No uncommitted tool-code changes unless intentionally testing new tool code"
    ],
    "abort_conditions": [
      "Focus preflight fails",
      "RIFT process missing",
      "RIFT HWND missing",
      "Foreground HWND does not belong to RIFT",
      "Git state cannot be read",
      "Operator cancels",
      "Any planned capture file path already exists unexpectedly",
      "Any live-capture command would be required at this stage"
    ],
    "operator_notes": "Metadata-only plan generated by RiftScan Operator. This does not start capture. Use as staging contract before implementing real focus-gated capture.",
    "full_live_preflight": {
      "status": "PASS",
      "focus_status": "foreground_verified",
      "process_id": 29420,
      "process_name": "rift_x64",
      "window_hwnd": 5115714,
      "window_hwnd_hex": "0x4E0F42",
      "window_title": "RIFT",
      "windows_count": 1
    },
    "source_artifacts": {
      "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
      "windows_json": "handoffs/current/focus-control-local/windows.json",
      "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
      "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
      "latest_dry_run_pointer": "sessions/focus-gated-dry-runs/LATEST_DRY_RUN.txt"
    },
    "guardrails": [
      "Metadata only.",
      "No capture started.",
      "No live test sequence started.",
      "No local data collection sequence started.",
      "No movement/input sent.",
      "No memory scan/read started.",
      "No /reloadui sent."
    ],
    "next_expected_step": "Use this capture plan as the staging contract before implementing real focus-gated capture."
  }
}
```

## Latest Focus-Gated Capture Session

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector",
  "manifest_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/capture-session-manifest.json",
  "handoff_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/CAPTURE_SESSION_HANDOFF.md",
  "summary": {
    "schema_version": "riftscan.focus_gated_window_process_metadata_session.v1",
    "app_version": "riftscan-operator-app-v3.8.6",
    "status": "window_process_metadata_collector_completed",
    "scaffold_only": false,
    "capture_mode": "window_process_metadata",
    "duration_target_seconds": 30,
    "stimulus_name": "none_metadata_only",
    "scaffold_window_started": null,
    "scaffold_window_completed": null,
    "real_capture_started": true,
    "real_capture_completed": true,
    "legacy_capture_started": null,
    "legacy_capture_completed": null,
    "focus_before_status": "foreground_verified",
    "focus_after_status": "foreground_verified",
    "process_id": 29420,
    "window_hwnd_hex": "0x4E0F42",
    "window_title": "RIFT",
    "capture_log": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/capture-log.jsonl",
    "collector_samples": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/collector-samples.jsonl",
    "collector_summary": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/collector-summary.json",
    "memory_read_started": false,
    "input_sent": false,
    "reloadui_sent": false,
    "sample_count": 60,
    "error_count": 0,
    "artifact_contract_status": "PASS",
    "missing_artifacts": [],
    "analysis_status": "PASS",
    "analysis_anomaly_count": 0,
    "comparison_status": "PASS",
    "comparison_difference_count": 5
  }
}
```

## Latest Window/Process Analysis

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector",
  "analysis_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/analysis/window-process-analysis.json",
  "handoff_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/analysis/WINDOW_PROCESS_ANALYSIS.md",
  "summary": {
    "status": "PASS",
    "warning_count": 0,
    "error_count": 0,
    "anomaly_count": 0,
    "session": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector",
    "sample_count": 60,
    "expected_sample_count": 60,
    "focus_lost_count": 0,
    "rift_process_dead_count": 0,
    "unique_foreground_hwnds": [
      "0x4E0F42"
    ],
    "unique_foreground_pids": [
      29420
    ],
    "unique_rift_hwnds": [
      "0x4E0F42"
    ],
    "unique_rift_pids": [
      29420
    ],
    "unique_rift_titles": [
      "RIFT"
    ],
    "unique_rift_window_rects": [
      [
        4,
        11,
        659,
        409
      ]
    ],
    "unique_rift_client_rects": [
      [
        0,
        0,
        639,
        359
      ]
    ],
    "missing_sample_index_count": 0,
    "duplicate_sample_index_count": 0,
    "sample_interval_seconds": {
      "expected": 0.5,
      "min": 0.495,
      "max": 0.501,
      "avg": 0.5,
      "max_abs_drift": 0.005
    },
    "artifact_contract_status": "PASS"
  }
}
```

## Latest Window/Process Comparison

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector",
  "comparison_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/comparison/window-process-comparison.json",
  "handoff_path": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector/comparison/WINDOW_PROCESS_COMPARISON.md",
  "summary": {
    "status": "PASS",
    "difference_count": 5,
    "warning_count": 5,
    "error_count": 0,
    "previous_session": "sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector",
    "latest_session": "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector",
    "previous_analysis_status": "FAIL",
    "latest_analysis_status": "PASS",
    "previous_sample_count": 60,
    "latest_sample_count": 60,
    "previous_focus_lost_count": 47,
    "latest_focus_lost_count": 0,
    "previous_artifact_contract_status": "PASS",
    "latest_artifact_contract_status": "PASS"
  }
}
```

## Focus Log Tail

```jsonl
{"timestamp_utc": "2026-05-06T01:00:48Z", "event": "script_start", "script": "C:\\RIFT MODDING\\Riftscan\\tools\\rift_focus_control.py", "repo_root": "C:\\RIFT MODDING\\Riftscan", "process_name": "rift_x64", "explicit_pid": 0, "retries": 3, "settle_ms": 400}
{"timestamp_utc": "2026-05-06T01:00:48Z", "event": "powershell_start", "command": "$items = @(Get-Process -Name 'rift_x64' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); $items | ConvertTo-Json -Depth 4"}
{"timestamp_utc": "2026-05-06T01:00:48Z", "event": "powershell_finish", "success": true, "returncode": 0, "elapsed_ms": 217, "stdout_length": 210, "stderr_length": 0}
{"timestamp_utc": "2026-05-06T01:00:48Z", "event": "focus_attempt", "attempt": 1, "restore_ok": true, "set_foreground_ok": true, "foreground_hwnd": 657876, "foreground_hwnd_hex": "0xA09D4", "foreground_pid": 11220, "foreground_title": "RIFT", "verified": true}
{"timestamp_utc": "2026-05-06T01:00:48Z", "event": "script_finish", "success": true, "status": "foreground_verified"}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
```

## Guardrails

- The full live preflight is conservative: focus + validation + report only.
- The focus-gated session dry run creates session metadata only.
- The focus-gated capture plan is metadata only.
- The focus-gated capture scaffold may open a timed scaffold window, but records focus metadata/log structure only.
- Real capture collector did not run.
- No movement/input sent.
- No memory scan/read started.
- No `/reloadui` sent.
- The helper stages only explicit allowlisted paths; ignored allowlisted artifact paths are force-added explicitly when needed.
- The helper never runs `git add .`.
- Known junk cleanup uses literal paths/globs from the helper configuration.
