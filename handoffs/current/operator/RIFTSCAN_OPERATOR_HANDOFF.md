# RiftScan Operator Handoff

Created UTC: `2026-05-06T03:05:02Z`
App version: `riftscan-operator-app-v3.8.14`
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
capture_readiness: BLOCKED
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
  "created_utc": "2026-05-06T03:05:02Z",
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

```

## Recent Commits

Exit code: `0`

```text
8652930 Add operator report CLI and readiness link gate
1aad239 Update current handoff verified milestone
4f17bbe Refresh blocked gate artifacts after baseline self-test
870a721 Add post-update baseline self-test
8311033 Clarify current handoff commit reference

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
