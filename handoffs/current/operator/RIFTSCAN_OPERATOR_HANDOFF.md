# RiftScan Operator Handoff

Created UTC: `2026-05-07T17:26:46Z`
App version: `riftscan-operator-app-v3.8.22`
Repo root: `C:\RIFT MODDING\Riftscan`

## Operator Assessment

Full live preflight gate: `PASS`
Focus preflight: `PASS`
Summary: `status=foreground_verified pid=11220 hwnd=0xA09D4 title=RIFT`

- No blocking focus/git issues detected; see Current Workflow Gate for baseline/readiness state.

## Current Workflow Gate

Summary path: `handoffs/current/operator/operator-current-gate-summary.json`

```text
metadata_capture_plan_gate: PASS
post_update_baseline: PASS
post_update_baseline_freshness: warning_non_relevant_changes
capture_readiness: PASS
capture_readiness_freshness: warning_non_relevant_changes
capture_readiness_baseline_link: match
latest_metadata_capture_plan: valid_metadata_only
latest_capture_plan: plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan
capture_plan_check: PASS
movement_test_readiness: PASS
movement_execution_gate: BLOCKED
movement_execution_allowed: False
movement_execution_expires_utc: None
full_live_preflight: PASS
focus_preflight: PASS
live_collection_allowed: false
old_offsets_trusted: false
next_action: Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.

blockers:
- None
```

```json
{
  "schema_version": "riftscan.operator_current_workflow_gate.v1",
  "created_utc": "2026-05-07T17:26:46Z",
  "metadata_capture_plan_gate": "PASS",
  "live_collection_allowed": false,
  "old_offsets_trusted": false,
  "post_update_baseline": {
    "label": "Post-Update Baseline",
    "artifact_status": "present",
    "status": "pass",
    "display_status": "PASS",
    "created_utc": "2026-05-06T04:25:10Z",
    "blockers": [],
    "paths": {
      "report": "handoffs\\current\\post-update-baseline\\POST_UPDATE_BASELINE_REPORT.md",
      "summary": "handoffs\\current\\post-update-baseline\\post-update-baseline-summary.json",
      "log": "handoffs\\current\\post-update-baseline\\post-update-baseline-log.jsonl"
    },
    "artifact_freshness": {
      "status": "warning_non_relevant_changes",
      "artifact_head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
      "current_head": "5c3ff94e4982bca4a8375493081f88faa318fec9",
      "head_matches_current": false,
      "changed_paths_since_artifact_head": [
        "docs/agent-execution-workflow.md",
        "docs/ai-workflow-packet-schema.md",
        "docs/discovery-ledger-workflow.md",
        "docs/helper-tooling-policy.md",
        "handoffs/current/README_CURRENT.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_0650_COORD_API_TRUTH_TRACE_BLOCKED.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md",
        "handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md",
        "handoffs/current/ai-workflow/ai-workflow-log.jsonl",
        "handoffs/current/ai-workflow/ai-workflow-summary.json",
        "handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md",
        "handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl",
        "handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json",
        "handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md",
        "handoffs/current/capture-plan-check/capture-plan-check-log.jsonl",
        "handoffs/current/capture-plan-check/capture-plan-check-summary.json",
        "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
        "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
        "handoffs/current/capture-readiness/capture-readiness-summary.json",
        "handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md",
        "handoffs/current/coord-api-truth/coord-api-truth-log.jsonl",
        "handoffs/current/coord-api-truth/coord-api-truth-summary.json",
        "handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md",
        "handoffs/current/discovery-ledger/candidate_ledger.jsonl",
        "handoffs/current/discovery-ledger/discovery-ledger-log.jsonl",
        "handoffs/current/discovery-ledger/discovery-ledger-summary.json",
        "handoffs/current/focus-control-local/focus-control-log.jsonl",
        "handoffs/current/focus-control-local/focus-control-summary.json",
        "handoffs/current/focus-control-local/process-command-result.json",
        "handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md",
        "handoffs/current/live-collection-gate/live-collection-gate-summary.json",
        "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
        "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl",
        "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json",
        "handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md",
        "handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl",
        "handoffs/current/movement-test-readiness/movement-test-readiness-summary.json",
        "handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md",
        "handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl",
        "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json",
        "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
        "handoffs/current/operator/operator-current-gate-summary.json",
        "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
        "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl",
        "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
        "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
        "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json",
        "plans/focus-gated-capture-plans/LATEST_CAPTURE_PLAN.txt",
        "scripts/live-test-riftscan.ps1",
        "scripts/run-riftscan-ai-workflow-packet.cmd",
        "scripts/run-riftscan-candidate-ledger-consumer.cmd",
        "scripts/run-riftscan-capture-plan-check.cmd",
        "scripts/run-riftscan-discovery-ledger.cmd",
        "scripts/run-riftscan-movement-execution-gate.cmd",
        "scripts/run-riftscan-movement-test-readiness.cmd",
        "scripts/run-riftscan-operator-offline-diagnostics.cmd",
        "tools/riftscan_ai_workflow_packet.py",
        "tools/riftscan_candidate_ledger_consumer.py",
        "tools/riftscan_capture_plan_check.py",
        "tools/riftscan_discovery_ledger.py",
        "tools/riftscan_movement_execution_gate.py",
        "tools/riftscan_movement_test_readiness.py",
        "tools/riftscan_offline_workflow_check.py",
        "tools/riftscan_operator_app.py"
      ],
      "relevant_gate_code_changed": false,
      "relevant_changed_paths": []
    }
  },
  "capture_readiness": {
    "label": "Capture Readiness",
    "artifact_status": "present",
    "status": "pass",
    "display_status": "PASS",
    "created_utc": "2026-05-06T04:25:46Z",
    "blockers": [],
    "paths": {
      "report": "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
      "summary": "handoffs/current/capture-readiness/capture-readiness-summary.json",
      "log": "handoffs/current/capture-readiness/capture-readiness-log.jsonl"
    },
    "artifact_freshness": {
      "status": "warning_non_relevant_changes",
      "artifact_head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
      "current_head": "5c3ff94e4982bca4a8375493081f88faa318fec9",
      "head_matches_current": false,
      "changed_paths_since_artifact_head": [
        "docs/agent-execution-workflow.md",
        "docs/ai-workflow-packet-schema.md",
        "docs/discovery-ledger-workflow.md",
        "docs/helper-tooling-policy.md",
        "handoffs/current/README_CURRENT.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_0650_COORD_API_TRUTH_TRACE_BLOCKED.md",
        "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md",
        "handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md",
        "handoffs/current/ai-workflow/ai-workflow-log.jsonl",
        "handoffs/current/ai-workflow/ai-workflow-summary.json",
        "handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md",
        "handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl",
        "handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json",
        "handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md",
        "handoffs/current/capture-plan-check/capture-plan-check-log.jsonl",
        "handoffs/current/capture-plan-check/capture-plan-check-summary.json",
        "handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md",
        "handoffs/current/capture-readiness/capture-readiness-log.jsonl",
        "handoffs/current/capture-readiness/capture-readiness-summary.json",
        "handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md",
        "handoffs/current/coord-api-truth/coord-api-truth-log.jsonl",
        "handoffs/current/coord-api-truth/coord-api-truth-summary.json",
        "handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md",
        "handoffs/current/discovery-ledger/candidate_ledger.jsonl",
        "handoffs/current/discovery-ledger/discovery-ledger-log.jsonl",
        "handoffs/current/discovery-ledger/discovery-ledger-summary.json",
        "handoffs/current/focus-control-local/focus-control-log.jsonl",
        "handoffs/current/focus-control-local/focus-control-summary.json",
        "handoffs/current/focus-control-local/process-command-result.json",
        "handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md",
        "handoffs/current/live-collection-gate/live-collection-gate-summary.json",
        "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
        "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl",
        "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json",
        "handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md",
        "handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl",
        "handoffs/current/movement-test-readiness/movement-test-readiness-summary.json",
        "handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md",
        "handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl",
        "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json",
        "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
        "handoffs/current/operator/operator-current-gate-summary.json",
        "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
        "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl",
        "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
        "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
        "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json",
        "plans/focus-gated-capture-plans/LATEST_CAPTURE_PLAN.txt",
        "scripts/live-test-riftscan.ps1",
        "scripts/run-riftscan-ai-workflow-packet.cmd",
        "scripts/run-riftscan-candidate-ledger-consumer.cmd",
        "scripts/run-riftscan-capture-plan-check.cmd",
        "scripts/run-riftscan-discovery-ledger.cmd",
        "scripts/run-riftscan-movement-execution-gate.cmd",
        "scripts/run-riftscan-movement-test-readiness.cmd",
        "scripts/run-riftscan-operator-offline-diagnostics.cmd",
        "tools/riftscan_ai_workflow_packet.py",
        "tools/riftscan_candidate_ledger_consumer.py",
        "tools/riftscan_capture_plan_check.py",
        "tools/riftscan_discovery_ledger.py",
        "tools/riftscan_movement_execution_gate.py",
        "tools/riftscan_movement_test_readiness.py",
        "tools/riftscan_offline_workflow_check.py",
        "tools/riftscan_operator_app.py"
      ],
      "relevant_gate_code_changed": false,
      "relevant_changed_paths": []
    }
  },
  "capture_readiness_baseline_link": {
    "status": "match",
    "current_baseline": {
      "created_utc": "2026-05-06T04:25:10Z",
      "status": "pass",
      "display_status": "PASS",
      "runtime": {
        "pid": 11220,
        "hwnd": 657876
      }
    },
    "readiness_baseline": {
      "created_utc": "2026-05-06T04:25:10Z",
      "status": "pass",
      "display_status": "PASS",
      "runtime": {
        "pid": 11220,
        "hwnd": 657876
      }
    },
    "mismatches": []
  },
  "latest_capture_plan": {
    "artifact_status": "present",
    "status": "valid_metadata_only",
    "latest_plan": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan",
    "manifest_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json",
    "handoff_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
    "schema_version": "riftscan.focus_gated_capture_plan.v1",
    "plan_status": "capture_plan_created",
    "metadata_only": true,
    "capture_started": false,
    "capture_completed": false
  },
  "capture_plan_check": {
    "label": "Capture Plan Check",
    "artifact_status": "present",
    "status": "pass",
    "display_status": "PASS",
    "created_utc": "2026-05-07T17:26:44Z",
    "blockers": [],
    "paths": {
      "report": "handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md",
      "summary": "handoffs/current/capture-plan-check/capture-plan-check-summary.json",
      "log": "handoffs/current/capture-plan-check/capture-plan-check-log.jsonl"
    }
  },
  "movement_test_readiness": {
    "label": "Movement Test Readiness",
    "artifact_status": "present",
    "status": "pass",
    "display_status": "PASS",
    "created_utc": "2026-05-07T17:26:45Z",
    "blockers": [],
    "paths": {
      "report": "handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md",
      "summary": "handoffs/current/movement-test-readiness/movement-test-readiness-summary.json",
      "log": "handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl"
    }
  },
  "movement_execution_gate": {
    "label": "Movement Execution Gate",
    "artifact_status": "present",
    "status": "blocked_movement_execution_not_allowed",
    "display_status": "BLOCKED",
    "created_utc": "2026-05-06T10:38:19Z",
    "blockers": [
      "live-test-riftscan preflight failed for move_forward.",
      "live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.",
      "live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance."
    ],
    "paths": {
      "report": "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
      "summary": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json",
      "log": "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl"
    },
    "movement_execution_allowed": false,
    "expires_utc": null
  },
  "full_live_preflight": "PASS",
  "focus_preflight": "PASS",
  "blockers": [],
  "next_action": "Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.",
  "guardrail": "No live capture, discovery, movement/input, memory scan/read, offset validation, RiftReader validation, or /reloadui until current gates pass and an explicit future live gate is added."
}
```

## Git Status

Exit code: `0`

```text
 M docs/ai-workflow-packet-schema.md
 M docs/helper-tooling-policy.md
 M handoffs/current/README_CURRENT.md
 M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md
 M handoffs/current/ai-workflow/ai-workflow-log.jsonl
 M handoffs/current/ai-workflow/ai-workflow-summary.json
 M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json
 M handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md
 M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl
 M handoffs/current/capture-plan-check/capture-plan-check-summary.json
 M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md
 M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl
 M handoffs/current/discovery-ledger/discovery-ledger-summary.json
 M handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md
 M handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl
 M handoffs/current/movement-test-readiness/movement-test-readiness-summary.json
 M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md
 M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl
 M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json
 M tools/riftscan_ai_workflow_packet.py

```

## Recent Commits

Exit code: `0`

```text
5c3ff94 Validate AI packet contract offline
c9c00cd Document AI workflow packet schema
abb5c3f Add AI workflow packet diffing
7ae032e Add offline artifact age diagnostics
a333923 Add safe candidate ledger consumer

```

## Focus Summary JSON

```json
{
  "schema_version": "riftscan.local_focus_control_summary.v1",
  "created_utc": "2026-05-06T10:38:15Z",
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
    "blockers": [],
    "created_utc": "2026-05-06T04:25:10Z",
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
      "head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
      "log_oneline_5": "40bbd1c Refresh blocked post-update baseline artifacts\n17d69f5 Refresh handoffs after offline workflow check\nb3bb14d Add offline workflow check helper\n0125e33 Refresh handoff after operator report wrapper\na2cf481 Add operator report command wrapper",
      "status_short": " M handoffs/current/focus-control-local/focus-control-log.jsonl\n M handoffs/current/focus-control-local/focus-control-summary.json\n M handoffs/current/focus-control-local/process-command-result.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md\n M handoffs/current/operator/operator-current-gate-summary.json\n M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl"
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
        "created_utc": "2026-05-06T04:25:10Z",
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
      "created_utc": "2026-05-06T04:25:10Z",
      "display_status": "PASS",
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
      "status": "pass",
      "summary_path": "handoffs/current/post-update-baseline/post-update-baseline-summary.json"
    },
    "blockers": [],
    "created_utc": "2026-05-06T04:25:46Z",
    "display_status": "PASS",
    "focus_command_result": {
      "args": [
        "cmd",
        "/c",
        "C:\\RIFT MODDING\\Riftscan\\scripts\\run-rift-focus-control.cmd"
      ],
      "returncode": 0,
      "stderr": "",
      "stdout": "Focus control handoff written to C:\\RIFT MODDING\\Riftscan\\handoffs\\current\\focus-control-local\n",
      "success": true
    },
    "git": {
      "branch": "main",
      "head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
      "log_oneline_5": "40bbd1c Refresh blocked post-update baseline artifacts\n17d69f5 Refresh handoffs after offline workflow check\nb3bb14d Add offline workflow check helper\n0125e33 Refresh handoff after operator report wrapper\na2cf481 Add operator report command wrapper",
      "status_short": " M handoffs/current/capture-readiness/capture-readiness-log.jsonl\n M handoffs/current/focus-control-local/focus-control-log.jsonl\n M handoffs/current/focus-control-local/focus-control-summary.json\n M handoffs/current/focus-control-local/process-command-result.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md\n M handoffs/current/operator/operator-current-gate-summary.json\n M handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md\n M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl\n M handoffs/current/post-update-baseline/post-update-baseline-summary.json\n"
    },
    "next_step": "Create or refresh a metadata-only focus-gated capture plan.",
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
      "capture_planning_allowed": true,
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
        "created_utc": "2026-05-06T04:25:46Z",
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
        "blockers": [],
        "created_utc": "2026-05-06T04:25:10Z",
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
          "head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
          "log_oneline_5": "40bbd1c Refresh blocked post-update baseline artifacts\n17d69f5 Refresh handoffs after offline workflow check\nb3bb14d Add offline workflow check helper\n0125e33 Refresh handoff after operator report wrapper\na2cf481 Add operator report command wrapper",
          "status_short": " M handoffs/current/focus-control-local/focus-control-log.jsonl\n M handoffs/current/focus-control-local/focus-control-summary.json\n M handoffs/current/focus-control-local/process-command-result.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md\n M handoffs/current/operator/operator-current-gate-summary.json\n M handoffs/current/post-update-baseline/post-update-baseline-log.jsonl"
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
            "created_utc": "2026-05-06T04:25:10Z",
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
    "app_version": "riftscan-offline-workflow-check-v1.0.8",
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
          "tools/riftscan_offline_workflow_check.py",
          "tools/riftscan_capture_plan_check.py",
          "tools/riftscan_movement_test_readiness.py",
          "tools/riftscan_movement_execution_gate.py",
          "tools/riftscan_discovery_ledger.py",
          "tools/riftscan_ai_workflow_packet.py",
          "tools/riftscan_candidate_ledger_consumer.py"
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
        "stdout": "{\n  \"app_version\": \"riftscan-offline-workflow-check-v1.0.8\",\n  \"case_count\": 4,\n  \"created_utc\": \"2026-05-07T17:26:34Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.offline_workflow_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"expected\": \"pass\",\n      \"failed_checks\": [],\n      \"name\": \"all pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"expected\": \"fail\",\n      \"failed_checks\": [\n        \"b\"\n      ],\n      \"name\": \"one fail\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"pass\",\n      \"errors\": [],\n      \"expected\": \"pass\",\n      \"name\": \"ai packet contract pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"errors\": [\n        \"previous_packet_diff_compared_fields missing required field(s): status, blocker_count, warning_count, current_best_stable_id, current_best_address, candidate_consumer_status, safe_candidate_count, rejected_candidate_count, artifact_stale_count, artifact_missing_count, current_best_stale_count, current_best_missing_count, discovery_ledger_contract_status, offline_workflow_status, operator_live_collection_allowed\"\n      ],\n      \"expected\": \"fail\",\n      \"name\": \"ai packet contract blocks missing fields\",\n      \"pass\": true\n    }\n  ]\n}\n"
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
        "stdout": "{\n  \"schema_version\": \"riftscan.operator_self_test.v1\",\n  \"created_utc\": \"2026-05-07T17:26:35Z\",\n  \"app_version\": \"riftscan-operator-app-v3.8.22\",\n  \"status\": \"PASS\",\n  \"case_count\": 11,\n  \"tests\": [\n    {\n      \"name\": \"all gates pass without latest capture plan\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Refresh the metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with latest metadata-only capture plan\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run Capture Plan Check and review the latest metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with capture plan check\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run Movement Test Readiness before staging any live game-world movement test.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with movement test readiness\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run the final current-window Movement Execution Gate; live movement still requires immediate PID/HWND/focus revalidation and abort controls.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with blocked movement execution gate\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with movement execution gate pass\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Movement Execution Gate is PASS; if still before expires_utc, run only the exact bounded move_forward command from that gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"baseline blocks even when preflight passes\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.\",\n      \"blockers\": [\n        \"Post-Update Baseline is not PASS for the current updated client.\",\n        \"Stable in-world state is not confirmed.\",\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"readiness blocks after baseline pass\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness and resolve any blockers before capture-plan refresh.\",\n      \"blockers\": [\n        \"Capture Readiness is not PASS.\",\n        \"Post-update baseline is not PASS for the current client.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"stale readiness baseline link blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness again against the latest Post-Update Baseline.\",\n      \"blockers\": [\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"full live preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus status is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"focus preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus preflight is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    }\n  ],\n  \"safety\": {\n    \"writes_artifacts\": false,\n    \"launches_gui\": false,\n    \"runs_focus_preflight\": false,\n    \"capture_started\": false,\n    \"movement_or_input_sent\": false,\n    \"memory_scan_or_read_started\": false,\n    \"reloadui_sent\": false\n  }\n}\n"
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
        "stdout": "{\n  \"app_version\": \"riftscan-post-update-baseline-v1.0.1\",\n  \"case_count\": 6,\n  \"created_utc\": \"2026-05-07T17:26:35Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.post_update_baseline_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass baseline\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Maintenance is not confirmed over.\",\n        \"Login is not confirmed successful.\",\n        \"Stable in-world state is not confirmed.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Maintenance is not confirmed over\",\n        \"Login is not confirmed successful\",\n        \"Stable in-world state is not confirmed\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked manual state\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Focus preflight command did not complete successfully.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"selected_window is missing or null.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"selected_window is missing or null\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked missing selected window\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"windows.json has no window entries.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"windows.json has no window entries\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked empty windows list\",\n      \"pass\": true\n    }\n  ]\n}\n"
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
        "stdout": "{\n  \"app_version\": \"riftscan-capture-readiness-v1.0.1\",\n  \"case_count\": 7,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Post-update baseline is not PASS for the current client.\",\n        \"Post-update baseline display_status is not PASS.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Post-update baseline is not PASS\",\n        \"display_status is not PASS\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked baseline status\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Safety field baseline.safety.old_offsets_trusted is not false.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"baseline.safety.old_offsets_trusted\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked unsafe baseline safety\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus status is not foreground_verified.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus status is not foreground_verified\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked current focus lost\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current RIFT PID differs from the post-update baseline; rerun Post-Update Baseline.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current RIFT PID differs\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked pid drift\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus preflight command did not complete successfully.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_capture_plan_check.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "capture_plan_check_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-capture-plan-check-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_plan_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"valid metadata-only plan\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"capture-plan capture_started is not false.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked capture_started true\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Capture-plan expected_files is missing required metadata outputs: capture-log.jsonl, focus-summary-after.json, focus-summary-before.json, operator-report.md.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked missing expected files\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Capture-plan focus/preflight source artifacts are missing: windows_json=handoffs/current/focus-control-local/windows.json.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked missing source artifact\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Operator gate live_collection_allowed is not false.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked operator live collection allowed\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_movement_test_readiness.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "movement_test_readiness_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-movement-test-readiness-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.movement_test_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"all readiness inputs pass\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Operator metadata_capture_plan_gate is not PASS.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked operator gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Capture Plan Check is not PASS.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked capture plan check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Movement live-test wrapper is missing required guard features: move_forward stimulus, pre-capture wait, ReaderBridge freshness, RiftReader anchor read, RiftScan passive capture, delta summary, movement-proof interpretation.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked wrapper missing movement support\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"RiftReader run-reader.cmd is missing; movement wrapper cannot refresh proof-grade coordinate anchors.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked missing RiftReader\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_movement_execution_gate.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "movement_execution_gate_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-movement-execution-gate-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started_by_riftscan\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.movement_execution_gate_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"blockers\": [],\n      \"expected\": \"pass\",\n      \"name\": \"pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"Movement Test Readiness is not PASS.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked readiness\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"Focus summary status is not foreground_verified.\",\n        \"Focus summary selected_window is missing.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked focus\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"live-test-riftscan preflight failed for move_forward.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked wrapper\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked skipped wrapper\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_discovery_ledger.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "discovery_ledger_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-discovery-ledger-v1.2.0\",\n  \"contract_validation_issue_count\": 0,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.discovery_ledger.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_discovery_ledger.py"
        ],
        "exit_code": 0,
        "name": "discovery_ledger_refresh",
        "status": "pass",
        "stderr": "",
        "stdout": "RIFTSCAN DISCOVERY LEDGER: handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\nSummary: handoffs/current/discovery-ledger/discovery-ledger-summary.json\nCandidate ledger: handoffs/current/discovery-ledger/candidate_ledger.jsonl\nSafety: offline artifact inventory only; no focus, capture, input, movement, memory read, RiftReader command, or /reloadui was run.\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_discovery_ledger.py",
          "--validate-existing"
        ],
        "exit_code": 0,
        "name": "discovery_ledger_validate_existing",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-discovery-ledger-v1.2.0\",\n  \"candidate_count\": 3,\n  \"created_utc\": \"2026-05-07T17:26:36Z\",\n  \"display_status\": \"PASS\",\n  \"error_count\": 0,\n  \"issues\": [],\n  \"line_count\": 3,\n  \"path\": \"handoffs/current/discovery-ledger/candidate_ledger.jsonl\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"ledger_live_movement_authorized\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"offline_only\": true,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.discovery_ledger_validation.v1\",\n  \"status\": \"PASS\",\n  \"warning_count\": 0\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_candidate_ledger_consumer.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "candidate_ledger_consumer_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-candidate-ledger-consumer-v1.1.0\",\n  \"created_utc\": \"2026-05-07T17:26:37Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.candidate_ledger_consumer.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_candidate_ledger_consumer.py",
          "--strict-exit-code"
        ],
        "exit_code": 0,
        "name": "candidate_ledger_consumer_refresh",
        "status": "pass",
        "stderr": "",
        "stdout": "RIFTSCAN CANDIDATE LEDGER CONSUMER: handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\nSummary: handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\nStatus: PASS\nSafety: offline consumer only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.\n"
      },
      {
        "args": [
          "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
          "tools/riftscan_ai_workflow_packet.py",
          "--self-test"
        ],
        "exit_code": 0,
        "name": "ai_workflow_packet_self_test",
        "status": "pass",
        "stderr": "",
        "stdout": "{\n  \"app_version\": \"riftscan-ai-workflow-packet-v1.5.0\",\n  \"created_utc\": \"2026-05-07T17:26:37Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.ai_workflow_packet.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
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
        "stdout": "{\n  \"app_version\": \"riftscan-patch-intake-v1.2.5\",\n  \"created_utc\": \"2026-05-07T17:26:42Z\",\n  \"schema_version\": \"riftscan.patch_intake_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"empty payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"wrong header\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_MANIFEST\",\n      \"expected\": \"FAIL_BAD_MANIFEST\",\n      \"issues\": [\n        \"JSONDecodeError: Expecting property name enclosed in double quotes: line 1 column 2 (char 1)\"\n      ],\n      \"name\": \"bad json\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_PAYLOAD\",\n      \"expected\": \"FAIL_MISSING_PAYLOAD\",\n      \"issues\": [\n        \"Payload block markers are missing.\"\n      ],\n      \"name\": \"missing payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_HASH_MISMATCH\",\n      \"expected\": \"FAIL_HASH_MISMATCH\",\n      \"issues\": [],\n      \"name\": \"hash mismatch\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_STALE_PATCH\",\n      \"expected\": \"FAIL_STALE_PATCH\",\n      \"issues\": [\n        \"Patch timestamp is not newer than last accepted patch.\"\n      ],\n      \"name\": \"stale timestamp\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_WRONG_REPO\",\n      \"expected\": \"FAIL_WRONG_REPO\",\n      \"issues\": [\n        \"target_repo_root does not match selected repo root.\"\n      ],\n      \"name\": \"wrong repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"valid dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"expected\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"issues\": [\n        \"No successful process/apply result exists.\"\n      ],\n      \"name\": \"commit without apply\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload without commit metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"expected\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"issues\": [\n        \"Manifest commit block is required.\"\n      ],\n      \"name\": \"commit missing metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"expected\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"issues\": [\n        \"Unsafe commit.stage_paths entry: .\"\n      ],\n      \"name\": \"unsafe commit stage path validation\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload with capture readiness checks\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_COMMITTED\",\n      \"expected\": \"PASS_COMMITTED\",\n      \"issues\": [],\n      \"name\": \"commit in temp repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"expected\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"issues\": [],\n      \"name\": \"push verify simulated\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"chunked dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"expected\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"issues\": [\n        \"chunk 1 hash mismatch\"\n      ],\n      \"name\": \"chunked bad chunk hash\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_CHUNK\",\n      \"expected\": \"FAIL_MISSING_CHUNK\",\n      \"issues\": [\n        \"missing chunks: 1\"\n      ],\n      \"name\": \"chunked missing chunk\",\n      \"pass\": true\n    }\n  ]\n}\n"
      },
      {
        "args": [],
        "errors": [],
        "exit_code": 0,
        "name": "ai_workflow_packet_contract",
        "required_field_count": 16,
        "schema_doc_path": "docs/ai-workflow-packet-schema.md",
        "status": "pass",
        "stderr": "",
        "stdout": "checked_fields=16\n",
        "summary_path": "handoffs/current/ai-workflow/ai-workflow-summary.json"
      }
    ],
    "created_utc": "2026-05-07T17:26:43Z",
    "display_status": "PASS",
    "failed_check_count": 0,
    "failed_checks": [],
    "git": {
      "command_status": {
        "head": "pass",
        "log": "pass",
        "status": "pass"
      },
      "head": "5c3ff94e4982bca4a8375493081f88faa318fec9",
      "log_oneline_5": "5c3ff94 Validate AI packet contract offline\nc9c00cd Document AI workflow packet schema\nabb5c3f Add AI workflow packet diffing\n7ae032e Add offline artifact age diagnostics\na333923 Add safe candidate ledger consumer",
      "status_short": " M docs/ai-workflow-packet-schema.md\n M docs/helper-tooling-policy.md\n M handoffs/current/README_CURRENT.md\n M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md\n M handoffs/current/ai-workflow/ai-workflow-log.jsonl\n M handoffs/current/ai-workflow/ai-workflow-summary.json\n M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M tools/riftscan_ai_workflow_packet.py\n"
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
      "riftreader_command_executed": false,
      "riftreader_validation_started": false
    },
    "schema_version": "riftscan.offline_workflow_check.v1",
    "status": "pass"
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Discovery Ledger

```json
{
  "status": "present",
  "report_path": "handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md",
  "summary_path": "handoffs/current/discovery-ledger/discovery-ledger-summary.json",
  "candidate_ledger_path": "handoffs/current/discovery-ledger/candidate_ledger.jsonl",
  "log_path": "handoffs/current/discovery-ledger/discovery-ledger-log.jsonl",
  "summary": {
    "app_version": "riftscan-discovery-ledger-v1.2.0",
    "blockers": [
      "offline ledger cannot authorize live movement or claim current window focus",
      "RiftReader pointer says fresh preflight is required before more movement",
      "older Coord API Truth artifact remains stale-trace-blocked",
      "RiftScan Movement Execution Gate artifact is blocked/stale relative to newer RiftReader proof lane"
    ],
    "candidate_count": 3,
    "candidate_ledger_contract_validation": {
      "app_version": "riftscan-discovery-ledger-v1.2.0",
      "candidate_count": 3,
      "created_utc": "2026-05-07T17:26:36Z",
      "display_status": "PASS",
      "error_count": 0,
      "issues": [],
      "line_count": 3,
      "path": "handoffs/current/discovery-ledger/candidate_ledger.jsonl",
      "safety": {
        "capture_started": false,
        "ledger_live_movement_authorized": false,
        "memory_scan_or_read_started": false,
        "movement_or_input_sent": false,
        "offline_only": true,
        "process_attach_or_memory_read_started": false,
        "reloadui_sent": false,
        "riftreader_command_executed": false,
        "runs_focus_preflight": false,
        "writes_artifacts": false
      },
      "schema_version": "riftscan.discovery_ledger_validation.v1",
      "status": "PASS",
      "warning_count": 0
    },
    "candidates": [
      {
        "axis_order": "xyz",
        "best_addon_xyz": [
          7436.57568359375,
          885.2205810546875,
          3056.736572265625
        ],
        "best_max_abs_distance": 0,
        "best_memory_xyz": [
          7436.57568359375,
          885.2205810546875,
          3056.736572265625
        ],
        "candidate_id": "rift-addon-coordinate-candidate-000001",
        "claim_level": "validated_candidate",
        "kind": "coordinate_vec3",
        "latest_validation": {
          "current_coordinate": {
            "recordedAtUtc": "2026-05-07T16:18:16.0931490Z",
            "x": 7437.462890625,
            "y": 885.2191772460938,
            "z": 3055.73779296875
          },
          "generated_at_utc": "2026-05-07T16:18:16.435533+00:00",
          "movement_allowed_at_capture_time": true,
          "movement_sent_by_readback": false,
          "no_cheat_engine": true,
          "proof_anchor_max_age_seconds": null,
          "readback_recorded_sample_count": null,
          "readback_total_region_read_failures": null,
          "stable_across_readback_samples": null,
          "status": "valid"
        },
        "ledger_live_movement_authorized": false,
        "movement_evidence": {
          "active_movement_input_resumed_by_user": true,
          "currently_requires_revalidation_before_more_movement": true,
          "latest_forward_series_completed_pulse_count": 3,
          "latest_forward_series_requested_pulse_count": 3,
          "latest_forward_series_status": "passed-python-forward-series-3x250",
          "latest_forward_smoke_status": "passed-python-orchestrator-forward250",
          "proof_gated_pulse_status": "passed",
          "requires_fresh_preflight_immediately_before_movement": true
        },
        "next_validation_step": "rerun exact current PID/HWND proof readback before any more live movement",
        "observation_support_count": 1,
        "proof_anchor_cache": {
          "canonical_coord_source_kind": "riftscan-reference-validated-candidate",
          "generated_at_utc": "2026-05-07T16:03:01.4741365+00:00",
          "max_delta_error": 0.0063136718749774445,
          "max_reference_planar_displacement": 2.4753908943841862,
          "pose_count": 3,
          "proof_method": "no-ce-riftscan-reference-multisample",
          "proof_validation_status": "validated"
        },
        "proof_level": "riftscan_candidate_plus_riftreader_no_ce_multisample_and_post_readback",
        "riftreader_pointer_matched_candidate": true,
        "riftreader_status": "valid-after-run-progress-checkpoint-proofonly",
        "riftscan_validation_status": "candidate_unverified",
        "source": "riftscan_addon_coordinate_match",
        "source_absolute_address_hex": "0x2400EA32120",
        "source_artifacts": [
          "reports/generated/codex-current-coord-region-passive-20260506-230940-addon-coordinate-matches.json",
          "C:\\RIFT MODDING\\RiftReader\\docs\\recovery\\current-proof-anchor-readback.json",
          "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\proof-anchor-currentpid-47560-readback-summary-20260507-121811.json"
        ],
        "source_base_address_hex": "0x2400E970000",
        "source_offset_hex": "0xC2120",
        "source_region_id": "region-001892",
        "source_session_id": "codex-current-coord-region-passive-20260506-230940",
        "source_session_path": "C:\\RIFT MODDING\\Riftscan\\sessions\\codex-current-coord-region-passive-20260506-230940",
        "stable_id": "coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120",
        "state": "validated_candidate_historical_checkpoint",
        "support_count": 3,
        "warnings": [
          "offline_ledger_does_not_authorize_live_movement",
          "fresh_pid_hwnd_preflight_required_before_any_input"
        ]
      },
      {
        "axis_order": "xyz",
        "best_addon_xyz": [
          7511.5297851562,
          904.47998046875,
          3040.2800292969
        ],
        "best_max_abs_distance": 5.002220859751105e-11,
        "best_memory_xyz": [
          7511.52978515625,
          904.47998046875,
          3040.280029296875
        ],
        "candidate_id": "rift-addon-coordinate-candidate-000001",
        "claim_level": "candidate",
        "kind": "coordinate_vec3",
        "ledger_live_movement_authorized": false,
        "next_validation_step": "keep as historical evidence unless explicitly replaying the stale-trace blocker",
        "proof_level": "current_api_plus_readonly_memory_candidate",
        "source": "coord_api_truth_handoff",
        "source_absolute_address_hex": "0x1DA682DF690",
        "source_artifacts": [
          "handoffs/current/coord-api-truth/coord-api-truth-summary.json"
        ],
        "source_base_address_hex": null,
        "source_offset_hex": null,
        "source_session_id": "current-api-coord-readonly-20260506-064252",
        "source_session_path": "sessions/current-api-coord-readonly-20260506-064252",
        "stable_id": "coordinate::rift-addon-coordinate-candidate-000001::0x1DA682DF690::legacy_coord_api_truth",
        "state": "historical_stale_trace_blocked",
        "superseded_by_stable_id": "coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120",
        "support_count": 8,
        "trace_anchor": {
          "blocked_reason": "old coord-trace artifact is from PID 41220 and does not match current PID 11220",
          "process_id": 11220,
          "trace_matches_process": false,
          "trace_process_id": 41220
        },
        "warnings": [
          "old_coord_trace_anchor_does_not_match_current_process",
          "not_movement_grade_truth"
        ]
      },
      {
        "candidate_like_value_count": 32,
        "claim_level": "observed",
        "final_truth_claim": false,
        "hit_count": 32,
        "kind": "coordinate_candidate_scan",
        "ledger_live_movement_authorized": false,
        "manual_confirmation_required": true,
        "next_validation_step": "do not use for current-client movement proof; keep only as historical search context",
        "process_id": 33812,
        "proof_level": "candidate_like_values_only",
        "sample_candidate_addresses": [
          "0x1F60208F250",
          "0x1F6038AD880",
          "0x1F603FABC60",
          "0x1F603FAD0F0",
          "0x1F604FC7270",
          "0x1F6052B5CA0",
          "0x1F6052BEF10",
          "0x1F6052BF4B0",
          "0x1F6094292C0",
          "0x1F60DB4D230"
        ],
        "source": "coord_recovery_probe_summary",
        "source_artifacts": [
          "handoffs/current/coord-recovery/coord-recovery-summary.json"
        ],
        "stable_id": "coordinate_scan::33812::2026-05-02T17:14:47Z",
        "state": "historical_candidate_scan_only",
        "warnings": [
          "candidate_scan_not_truth",
          "historical_process_specific"
        ]
      }
    ],
    "created_utc": "2026-05-07T17:26:36Z",
    "current_best_candidate": {
      "axis_order": "xyz",
      "best_addon_xyz": [
        7436.57568359375,
        885.2205810546875,
        3056.736572265625
      ],
      "best_max_abs_distance": 0,
      "best_memory_xyz": [
        7436.57568359375,
        885.2205810546875,
        3056.736572265625
      ],
      "candidate_id": "rift-addon-coordinate-candidate-000001",
      "claim_level": "validated_candidate",
      "kind": "coordinate_vec3",
      "latest_validation": {
        "current_coordinate": {
          "recordedAtUtc": "2026-05-07T16:18:16.0931490Z",
          "x": 7437.462890625,
          "y": 885.2191772460938,
          "z": 3055.73779296875
        },
        "generated_at_utc": "2026-05-07T16:18:16.435533+00:00",
        "movement_allowed_at_capture_time": true,
        "movement_sent_by_readback": false,
        "no_cheat_engine": true,
        "proof_anchor_max_age_seconds": null,
        "readback_recorded_sample_count": null,
        "readback_total_region_read_failures": null,
        "stable_across_readback_samples": null,
        "status": "valid"
      },
      "ledger_live_movement_authorized": false,
      "movement_evidence": {
        "active_movement_input_resumed_by_user": true,
        "currently_requires_revalidation_before_more_movement": true,
        "latest_forward_series_completed_pulse_count": 3,
        "latest_forward_series_requested_pulse_count": 3,
        "latest_forward_series_status": "passed-python-forward-series-3x250",
        "latest_forward_smoke_status": "passed-python-orchestrator-forward250",
        "proof_gated_pulse_status": "passed",
        "requires_fresh_preflight_immediately_before_movement": true
      },
      "next_validation_step": "rerun exact current PID/HWND proof readback before any more live movement",
      "observation_support_count": 1,
      "proof_anchor_cache": {
        "canonical_coord_source_kind": "riftscan-reference-validated-candidate",
        "generated_at_utc": "2026-05-07T16:03:01.4741365+00:00",
        "max_delta_error": 0.0063136718749774445,
        "max_reference_planar_displacement": 2.4753908943841862,
        "pose_count": 3,
        "proof_method": "no-ce-riftscan-reference-multisample",
        "proof_validation_status": "validated"
      },
      "proof_level": "riftscan_candidate_plus_riftreader_no_ce_multisample_and_post_readback",
      "riftreader_pointer_matched_candidate": true,
      "riftreader_status": "valid-after-run-progress-checkpoint-proofonly",
      "riftscan_validation_status": "candidate_unverified",
      "source": "riftscan_addon_coordinate_match",
      "source_absolute_address_hex": "0x2400EA32120",
      "source_artifacts": [
        "reports/generated/codex-current-coord-region-passive-20260506-230940-addon-coordinate-matches.json",
        "C:\\RIFT MODDING\\RiftReader\\docs\\recovery\\current-proof-anchor-readback.json",
        "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\proof-anchor-currentpid-47560-readback-summary-20260507-121811.json"
      ],
      "source_base_address_hex": "0x2400E970000",
      "source_offset_hex": "0xC2120",
      "source_region_id": "region-001892",
      "source_session_id": "codex-current-coord-region-passive-20260506-230940",
      "source_session_path": "C:\\RIFT MODDING\\Riftscan\\sessions\\codex-current-coord-region-passive-20260506-230940",
      "stable_id": "coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120",
      "state": "validated_candidate_historical_checkpoint",
      "support_count": 3,
      "warnings": [
        "offline_ledger_does_not_authorize_live_movement",
        "fresh_pid_hwnd_preflight_required_before_any_input"
      ]
    },
    "current_best_candidate_stable_id": "coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120",
    "inventory": {
      "latest_sessions": [
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-07T03:09:44.862971Z",
          "path": "sessions/codex-current-coord-region-passive-20260506-230940",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "codex-current-coord-region-passive-20260506-230940",
          "snapshot_count": 3,
          "status": "complete",
          "total_bytes_raw": 50528256
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T16:57:11.702384Z",
          "path": "sessions/riftreader-currentpid-47560-passive-noinput-20260506-125703",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 16,
          "session_id": "riftreader-currentpid-47560-passive-noinput-20260506-125703",
          "snapshot_count": 32,
          "status": "complete",
          "total_bytes_raw": 2097152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T13:12:20.345488Z",
          "path": "sessions/riftreader-currentpid-47560-passive-noinput-20260506-091212",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 16,
          "session_id": "riftreader-currentpid-47560-passive-noinput-20260506-091212",
          "snapshot_count": 32,
          "status": "complete",
          "total_bytes_raw": 2097152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T13:06:22.409463Z",
          "path": "sessions/riftreader-currentpid-47560-passive-noinput-20260506-090614",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 16,
          "session_id": "riftreader-currentpid-47560-passive-noinput-20260506-090614",
          "snapshot_count": 32,
          "status": "complete",
          "total_bytes_raw": 2097152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T13:05:46.714390Z",
          "path": "sessions/riftreader-currentpid-47560-passive-noinput-20260506-090539",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 16,
          "session_id": "riftreader-currentpid-47560-passive-noinput-20260506-090539",
          "snapshot_count": 32,
          "status": "complete",
          "total_bytes_raw": 2097152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T12:56:58.935273Z",
          "path": "sessions/riftreader-currentpid-47560-passive-noinput-20260506-085655",
          "process_id": 47560,
          "process_name": "rift_x64",
          "region_count": 16,
          "session_id": "riftreader-currentpid-47560-passive-noinput-20260506-085655",
          "snapshot_count": 32,
          "status": "complete",
          "total_bytes_raw": 2097152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-05-06T10:42:54.151525Z",
          "path": "sessions/current-api-coord-readonly-20260506-064252",
          "process_id": 11220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "current-api-coord-readonly-20260506-064252",
          "snapshot_count": 8,
          "status": "complete",
          "total_bytes_raw": 4096
        },
        {
          "capture_mode": null,
          "last_write_utc_inferred_from_filesystem": "2026-05-03T07:50:23.041489Z",
          "path": "sessions/focus-gated-dry-runs/20260503T075023Z_focus_gated_session_dry_run",
          "process_id": null,
          "process_name": null,
          "region_count": null,
          "session_id": "20260503T075023Z_focus_gated_session_dry_run",
          "snapshot_count": null,
          "status": "dry_run_session_created",
          "total_bytes_raw": null
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T14:58:25.809904Z",
          "path": "sessions/actor-coord-move-forward-manual-20260430-105808",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 3,
          "session_id": "actor-coord-move-forward-manual-20260430-105808",
          "snapshot_count": 240,
          "status": "complete",
          "total_bytes_raw": 983040
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T13:48:42.813102Z",
          "path": "sessions/codex-riftreader-delegate-actor-coords-20260430-094839",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 3,
          "session_id": "codex-riftreader-delegate-actor-coords-20260430-094839",
          "snapshot_count": 12,
          "status": "complete",
          "total_bytes_raw": 49152
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T08:38:30.476263Z",
          "path": "sessions/actor-coordinate-owner-combined-passive-20260430-043829",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 2,
          "session_id": "actor-coordinate-owner-combined-passive-20260430-043829",
          "snapshot_count": 16,
          "status": "complete",
          "total_bytes_raw": 884736
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T08:34:29.228166Z",
          "path": "sessions/actor-coordinate-owner-adjacent-passive-20260430-043427",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 48,
          "session_id": "actor-coordinate-owner-adjacent-passive-20260430-043427",
          "snapshot_count": 192,
          "status": "complete",
          "total_bytes_raw": 3686400
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T08:20:04.859546Z",
          "path": "sessions/actor-coordinate-owner-followup-passive-20260430-042003",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 2,
          "session_id": "actor-coordinate-owner-followup-passive-20260430-042003",
          "snapshot_count": 16,
          "status": "complete",
          "total_bytes_raw": 1048576
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T07:16:07.360392Z",
          "path": "sessions/live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-alt-z-camera-20260430-031557-camera_only_alt_z_zoom",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T07:11:42.686152Z",
          "path": "sessions/live-alt-z-camera-20260430-031132-camera_only_alt_z_zoom",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-alt-z-camera-20260430-031132-camera_only_alt_z_zoom",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T07:04:09.832107Z",
          "path": "sessions/live-shift-z-repeat-20260430-030359-camera_only_shift_z_zoom",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-shift-z-repeat-20260430-030359-camera_only_shift_z_zoom",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T07:01:59.664208Z",
          "path": "sessions/live-redo-movement-20260430-025319-camera_only_shift_z_zoom",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-redo-movement-20260430-025319-camera_only_shift_z_zoom",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T06:56:26.021229Z",
          "path": "sessions/live-redo-movement-20260430-025319-camera_only",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-redo-movement-20260430-025319-camera_only",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T06:54:01.932778Z",
          "path": "sessions/live-redo-movement-20260430-025319-move_forward",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-redo-movement-20260430-025319-move_forward",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        },
        {
          "capture_mode": "passive",
          "last_write_utc_inferred_from_filesystem": "2026-04-30T06:53:50.991365Z",
          "path": "sessions/live-redo-movement-20260430-025319-turn_right",
          "process_id": 41220,
          "process_name": "rift_x64",
          "region_count": 1,
          "session_id": "live-redo-movement-20260430-025319-turn_right",
          "snapshot_count": 80,
          "status": "complete",
          "total_bytes_raw": 7864320
        }
      ],
      "session_manifest_count": 115
    },
    "next_recommended_actions": [
      "Use the RiftReader May 7 current-proof pointer as the latest discovery status, but treat it as requiring fresh preflight before more movement.",
      "Keep the RiftScan candidate at 0x2400EA32120 as the current best coordinate candidate source.",
      "Do not promote the older 0x1DA682DF690 Coord API Truth artifact beyond historical stale-trace-blocked evidence.",
      "When the game window is available, have RiftReader rerun exact PID/HWND proof readback rather than rediscovering from scratch.",
      "If PID/HWND changed, reacquire via RiftScan-first candidate import/readback/promotion instead of CE or heuristic caches."
    ],
    "output_paths": {
      "candidate_ledger": "handoffs/current/discovery-ledger/candidate_ledger.jsonl",
      "log": "handoffs/current/discovery-ledger/discovery-ledger-log.jsonl",
      "report": "handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md",
      "summary": "handoffs/current/discovery-ledger/discovery-ledger-summary.json"
    },
    "safety": {
      "focus_preflight_started": false,
      "ledger_live_movement_authorized": false,
      "live_capture_started": false,
      "movement_or_input_sent": false,
      "offline_only": true,
      "process_attach_or_memory_read_started": false,
      "reloadui_sent": false,
      "riftreader_command_executed": false
    },
    "schema_version": "riftscan.discovery_ledger.v1",
    "scope": "offline_artifact_inventory_no_live_process_access",
    "source_artifact_status": {
      "riftreader_current_proof_pointer_exists": true,
      "riftreader_latest_handoff_exists": true,
      "riftreader_latest_proof_summary_exists": true,
      "riftscan_coord_api_truth_summary_exists": true,
      "riftscan_coord_recovery_summary_exists": true,
      "riftscan_match_file_exists": true
    },
    "source_artifacts": {
      "riftreader_current_proof_pointer": "C:\\RIFT MODDING\\RiftReader\\docs\\recovery\\current-proof-anchor-readback.json",
      "riftreader_latest_handoff": "C:\\RIFT MODDING\\RiftReader\\docs\\handoffs\\2026-05-07-122200-python-live-test-orchestrator-current-handoff.md",
      "riftreader_latest_proof_summary": "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\proof-anchor-currentpid-47560-readback-summary-20260507-123808.json",
      "riftscan_coord_api_truth_summary": "handoffs/current/coord-api-truth/coord-api-truth-summary.json",
      "riftscan_coord_recovery_summary": "handoffs/current/coord-recovery/coord-recovery-summary.json",
      "riftscan_match_file": "reports/generated/codex-current-coord-region-passive-20260506-230940-addon-coordinate-matches.json",
      "riftscan_movement_execution_gate_summary": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json"
    },
    "status": "ledger_written"
  },
  "report_exists": true,
  "candidate_ledger_exists": true,
  "log_exists": true
}
```

## Latest Capture Plan Check

```json
{
  "status": "present",
  "report_path": "handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md",
  "summary_path": "handoffs/current/capture-plan-check/capture-plan-check-summary.json",
  "log_path": "handoffs/current/capture-plan-check/capture-plan-check-log.jsonl",
  "summary": {
    "app_version": "riftscan-capture-plan-check-v1.0.0",
    "blockers": [],
    "checks": {
      "capture_completed": false,
      "capture_started": false,
      "expected_files": [
        "capture-session-manifest.json",
        "capture-log.jsonl",
        "focus-summary-before.json",
        "focus-summary-after.json",
        "operator-report.md"
      ],
      "full_live_preflight": {
        "focus_status": "foreground_verified",
        "process_id": 11220,
        "process_name": "rift_x64",
        "status": "PASS",
        "window_hwnd": 657876,
        "window_hwnd_hex": "0xA09D4",
        "window_title": "RIFT",
        "windows_count": 1
      },
      "guardrail_count": 7,
      "metadata_only": true,
      "plan_status": "capture_plan_created",
      "schema_version": "riftscan.focus_gated_capture_plan.v1",
      "source_artifacts": {
        "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
        "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
        "latest_dry_run_pointer": "sessions/focus-gated-dry-runs/LATEST_DRY_RUN.txt",
        "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
        "windows_json": "handoffs/current/focus-control-local/windows.json"
      }
    },
    "created_utc": "2026-05-07T17:26:44Z",
    "display_status": "PASS",
    "git": {
      "head": "5c3ff94e4982bca4a8375493081f88faa318fec9",
      "log_oneline_5": "5c3ff94 Validate AI packet contract offline\nc9c00cd Document AI workflow packet schema\nabb5c3f Add AI workflow packet diffing\n7ae032e Add offline artifact age diagnostics\na333923 Add safe candidate ledger consumer",
      "status_short": " M docs/ai-workflow-packet-schema.md\n M docs/helper-tooling-policy.md\n M handoffs/current/README_CURRENT.md\n M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md\n M handoffs/current/ai-workflow/ai-workflow-log.jsonl\n M handoffs/current/ai-workflow/ai-workflow-summary.json\n M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\n M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M tools/riftscan_ai_workflow_packet.py\n"
    },
    "latest_capture_plan": {
      "handoff_exists": true,
      "handoff_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
      "latest_plan": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan",
      "manifest_exists": true,
      "manifest_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json",
      "pointer_path": "plans/focus-gated-capture-plans/LATEST_CAPTURE_PLAN.txt",
      "status": "present"
    },
    "next_step": "Review/refine the latest metadata-only capture plan. Real live collection remains blocked until a separate live-collection gate is explicitly approved.",
    "operator_gate": {
      "capture_readiness_baseline_link": "match",
      "capture_readiness_display_status": "PASS",
      "live_collection_allowed": false,
      "metadata_capture_plan_gate": "PASS",
      "next_action": "Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.",
      "old_offsets_trusted": false,
      "post_update_baseline_display_status": "PASS",
      "summary_path": "handoffs/current/operator/operator-current-gate-summary.json"
    },
    "paths": {
      "log": "handoffs/current/capture-plan-check/capture-plan-check-log.jsonl",
      "report": "handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md",
      "summary": "handoffs/current/capture-plan-check/capture-plan-check-summary.json"
    },
    "safety": {
      "capture_completed": false,
      "capture_plan_review_allowed": true,
      "capture_started": false,
      "live_collection_allowed": false,
      "memory_scan_or_read_started": false,
      "metadata_only": true,
      "movement_or_input_sent": false,
      "offset_validation_started": false,
      "reloadui_sent": false,
      "riftreader_validation_started": false
    },
    "schema_version": "riftscan.capture_plan_check.v1",
    "source_artifacts": {
      "latest_capture_plan_pointer": "plans/focus-gated-capture-plans/LATEST_CAPTURE_PLAN.txt",
      "operator_gate_summary": "handoffs/current/operator/operator-current-gate-summary.json"
    },
    "status": "pass",
    "warnings": []
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Movement Test Readiness

```json
{
  "status": "present",
  "report_path": "handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md",
  "summary_path": "handoffs/current/movement-test-readiness/movement-test-readiness-summary.json",
  "log_path": "handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl",
  "summary": {
    "app_version": "riftscan-movement-test-readiness-v1.0.0",
    "blockers": [],
    "checks": {
      "authorization_docs": {
        "missing_tokens": {}
      },
      "capture_plan_check": {
        "capture_plan_review_allowed": true,
        "display_status": "PASS",
        "live_collection_allowed": false,
        "memory_scan_or_read_started": false,
        "movement_or_input_sent": false,
        "status": "pass"
      },
      "live_collection_gate": {
        "checklist_mentions_abort": true,
        "checklist_mentions_movement": true,
        "display_status": "BLOCKED",
        "live_collection_allowed": false,
        "status": "defined_not_satisfied",
        "summary_exists": true
      },
      "movement_wrapper": {
        "cmd_exists": true,
        "focus_control_exists": true,
        "missing_tokens": [],
        "ps1_readable": true,
        "riftreader_run_cmd_exists": true
      },
      "operator_gate": {
        "capture_readiness": "PASS",
        "capture_readiness_baseline_link": "match",
        "latest_capture_plan_status": "valid_metadata_only",
        "live_collection_allowed": false,
        "metadata_capture_plan_gate": "PASS",
        "old_offsets_trusted": false,
        "post_update_baseline": "PASS"
      }
    },
    "created_utc": "2026-05-07T17:26:45Z",
    "display_status": "PASS",
    "git": {
      "head": "5c3ff94e4982bca4a8375493081f88faa318fec9",
      "log_oneline_5": "5c3ff94 Validate AI packet contract offline\nc9c00cd Document AI workflow packet schema\nabb5c3f Add AI workflow packet diffing\n7ae032e Add offline artifact age diagnostics\na333923 Add safe candidate ledger consumer",
      "status_short": " M docs/ai-workflow-packet-schema.md\n M docs/helper-tooling-policy.md\n M handoffs/current/README_CURRENT.md\n M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md\n M handoffs/current/ai-workflow/ai-workflow-log.jsonl\n M handoffs/current/ai-workflow/ai-workflow-summary.json\n M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\n M handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md\n M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl\n M handoffs/current/capture-plan-check/capture-plan-check-summary.json\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M tools/riftscan_ai_workflow_packet.py\n"
    },
    "paths": {
      "log": "handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl",
      "report": "handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md",
      "summary": "handoffs/current/movement-test-readiness/movement-test-readiness-summary.json"
    },
    "readiness": {
      "movement_execution_note": "This readiness check does not send input. A future live run must verify the exact RIFT window immediately before any bounded movement stimulus.",
      "ready_for_live_game_world_movement_testing": true,
      "ready_for_movement_execution_now": false,
      "recommended_live_wrapper_capture": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100",
      "recommended_live_wrapper_preflight": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly",
      "recommended_pre_capture_wait_ms": 3000,
      "recommended_stimulus": "move_forward",
      "requires_final_live_rerun_before_execution": true
    },
    "safety": {
      "capture_started": false,
      "focus_preflight_started": false,
      "memory_scan_or_read_started": false,
      "movement_or_input_sent": false,
      "offset_validation_started": false,
      "readiness_check_only": true,
      "reloadui_sent": false,
      "riftreader_validation_started": false
    },
    "schema_version": "riftscan.movement_test_readiness.v1",
    "source_artifacts": {
      "capture_plan_check_summary": "handoffs/current/capture-plan-check/capture-plan-check-summary.json",
      "focus_control": "tools/rift_focus_control.py",
      "live_collection_gate_checklist": "handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md",
      "live_collection_gate_summary": "handoffs/current/live-collection-gate/live-collection-gate-summary.json",
      "live_test_cmd": "scripts/live-test-riftscan.cmd",
      "live_test_ps1": "scripts/live-test-riftscan.ps1",
      "operator_gate_summary": "handoffs/current/operator/operator-current-gate-summary.json",
      "riftreader_run_cmd": "C:\\RIFT MODDING\\RiftReader\\scripts\\run-reader.cmd"
    },
    "status": "pass",
    "warnings": []
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Movement Execution Gate

```json
{
  "status": "present",
  "report_path": "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
  "summary_path": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json",
  "log_path": "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl",
  "summary": {
    "app_version": "riftscan-movement-execution-gate-v1.0.0",
    "blockers": [
      "live-test-riftscan preflight failed for move_forward.",
      "live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.",
      "live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance."
    ],
    "checks": {
      "commands": {
        "focus_preflight": {
          "args": [
            "cmd",
            "/c",
            "C:\\RIFT MODDING\\Riftscan\\scripts\\run-rift-focus-control.cmd"
          ],
          "error": null,
          "returncode": 0,
          "stderr_tail": "",
          "stdout_tail": "Focus control handoff written to C:\\RIFT MODDING\\Riftscan\\handoffs\\current\\focus-control-local\n",
          "success": true
        },
        "wrapper_preflight": {
          "args": [
            "cmd",
            "/c",
            "C:\\RIFT MODDING\\Riftscan\\scripts\\live-test-riftscan.cmd",
            "-Stimulus",
            "move_forward",
            "-PreflightOnly"
          ],
          "error": null,
          "returncode": 2,
          "stderr_tail": "",
          "stdout_tail": "BLOCKED: freshness checks failed. No capture started.\nVerdict: C:\\RIFT MODDING\\Riftscan\\reports\\generated\\manual-live-test-20260506-063816\\freshness-verdict.json\n - RiftReader anchor TraceMatchesProcess is not true.\n - Source object coordinate sample does not match ReaderBridge within tolerance.\n",
          "success": false
        }
      },
      "focus": {
        "hwnd": 657876,
        "pid": 11220,
        "status": "foreground_verified",
        "title": "RIFT"
      },
      "movement_test_readiness": {
        "display_status": "PASS",
        "status": "pass"
      },
      "operator_gate": {
        "live_collection_allowed": false,
        "metadata_capture_plan_gate": "PASS",
        "movement_test_readiness": "PASS",
        "old_offsets_trusted": false
      }
    },
    "created_utc": "2026-05-06T10:38:19Z",
    "display_status": "BLOCKED",
    "expires_utc": null,
    "movement_execution_allowed": false,
    "paths": {
      "log": "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl",
      "report": "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
      "summary": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json"
    },
    "recommended_command": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100",
    "safety": {
      "capture_started": false,
      "focus_preflight_started": true,
      "memory_scan_or_read_started_by_riftscan": false,
      "movement_execution_gate_only": true,
      "movement_or_input_sent": false,
      "offset_validation_started": false,
      "reloadui_sent": false,
      "riftreader_anchor_preflight_started": true,
      "wrapper_preflight_started": true
    },
    "schema_version": "riftscan.movement_execution_gate.v1",
    "status": "blocked_movement_execution_not_allowed",
    "warnings": []
  },
  "report_exists": true,
  "log_exists": true
}
```

## Latest Coord API Truth

```json
{
  "status": "present",
  "report_path": "handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md",
  "summary_path": "handoffs/current/coord-api-truth/coord-api-truth-summary.json",
  "log_path": "handoffs/current/coord-api-truth/coord-api-truth-log.jsonl",
  "summary": {
    "addon_api_scan": {
      "observation_count": 383,
      "observation_kind_counts": {
        "current_player": 378,
        "player_loc": 1,
        "target": 3,
        "waypoint_status": 1
      },
      "truth_record_count": 4,
      "warnings": [
        "addon_api_truth_summary_is_snapshot_evidence_not_memory_truth",
        "no_focus_coordinate_truth_observed",
        "no_focus_target_coordinate_truth_observed",
        "no_player_waypoint_anchor_truth_observed",
        "no_waypoint_coordinate_truth_observed",
        "source_scan_warning:addon_api_observations_filtered_by_addon_name",
        "source_scan_warning:addon_api_observations_filtered_by_min_file_write_utc"
      ]
    },
    "coordinate_truth_level": "current_api_plus_readonly_memory_candidate",
    "created_utc": "2026-05-06T10:45:20Z",
    "current_player": {
      "api_source": "Inspect.Unit.Detail",
      "confidence_level": "addon_api_direct_savedvariables",
      "coordinate_x": 7511.5297851562,
      "coordinate_y": 904.47998046875,
      "coordinate_z": 3040.2800292969,
      "file_last_write_utc": "2026-05-06T10:34:50.511034+00:00",
      "location_name": "Sanctum of the Vigil",
      "source_addon": "ReaderBridgeExport",
      "source_file_name": "ReaderBridgeExport.lua",
      "source_mode": "DirectAPI",
      "unit_id": "u035400012FA2D207",
      "unit_name": "Atank",
      "zone_id": "z487C9102D2EA79BE"
    },
    "display_status": "PARTIAL_PASS_TRACE_BLOCKED",
    "live_collection_allowed": false,
    "movement_execution_allowed": false,
    "movement_execution_gate": {
      "blockers": [
        "live-test-riftscan preflight failed for move_forward.",
        "live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.",
        "live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance."
      ],
      "display_status": "BLOCKED",
      "movement_execution_allowed": false,
      "summary_path": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json"
    },
    "next_action": "Rebuild/refresh RiftReader coord-trace proof anchor for current PID before movement; do not use the old 0x216... trace addresses as current truth.",
    "old_offsets_trusted": false,
    "paths": {
      "addon_api_scan": "reports/generated/addon-api-observation-scan-current-coords-fresh-20260506-103803.json",
      "addon_api_truth": "reports/generated/addon-api-truth-current-coords-fresh-20260506-103803.json",
      "log": "handoffs/current/coord-api-truth/coord-api-truth-log.jsonl",
      "report": "handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md",
      "riftreader_coord_anchor": "reports/generated/riftreader-read-player-coord-anchor-20260506-104154.json",
      "riftreader_current": "reports/generated/riftreader-read-player-current-20260506-104154.json",
      "riftreader_snapshot": "reports/generated/riftreader-readerbridge-snapshot-20260506-104154.json",
      "riftscan_addon_match": "reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.json",
      "riftscan_addon_match_report": "reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.md",
      "riftscan_session": "sessions/current-api-coord-readonly-20260506-064252",
      "summary": "handoffs/current/coord-api-truth/coord-api-truth-summary.json"
    },
    "riftreader_coord_trace_anchor": {
      "blocked_reason": "old coord-trace artifact is from PID 41220 and does not match current PID 11220",
      "object_base_address": "0x216F2F26068",
      "process_id": 11220,
      "source_coord_relative_offset": 72,
      "source_file": "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\player-coord-write-trace.json",
      "source_object_address": "0x216F2F26020",
      "trace_matches_process": false,
      "trace_process_id": 41220
    },
    "riftreader_current": {
      "anchor_provenance": "heuristic",
      "coord_matches_within_tolerance": true,
      "delta_x": 0,
      "delta_y": 0,
      "delta_z": 0,
      "expected_coord_x": 7511.5297851562,
      "expected_coord_y": 904.47998046875,
      "expected_coord_z": 3040.2800292969,
      "memory_address_hex": "0x1DA682DF690",
      "memory_coord_x": 7511.53,
      "memory_coord_y": 904.48,
      "memory_coord_z": 3040.28,
      "process_id": 11220,
      "process_name": "rift_x64",
      "proof_grade": false,
      "proof_grade_blocker": "read-player-current is heuristic/current sanity evidence; coord-trace anchor does not match current process",
      "selection_source": "heuristic"
    },
    "riftscan_readonly_capture": {
      "axis_order": "xyz",
      "best_addon_x": 7511.5297851562,
      "best_addon_y": 904.47998046875,
      "best_addon_z": 3040.2800292969,
      "best_max_abs_distance": 5.002220859751105e-11,
      "best_memory_x": 7511.52978515625,
      "best_memory_y": 904.47998046875,
      "best_memory_z": 3040.280029296875,
      "bytes_captured": 4096,
      "candidate_count": 1,
      "candidate_id": "rift-addon-coordinate-candidate-000001",
      "candidate_source_absolute_address_hex": "0x1DA682DF690",
      "match_count": 8,
      "movement_or_input_sent": false,
      "samples_captured": 8,
      "session_id": "current-api-coord-readonly-20260506-064252",
      "session_path": "sessions/current-api-coord-readonly-20260506-064252",
      "stimulus": "passive_idle",
      "support_count": 8,
      "target_base_address_hex": "0x1DA682DF690",
      "warning": "addon_coordinate_matches_are_validation_evidence_not_final_truth"
    },
    "safety": {
      "coord_trace_anchor_rebuilt": false,
      "movement_or_input_sent": false,
      "offset_validation_or_trust_promoted": false,
      "read_only_memory_capture_started": true,
      "reloadui_sent": false
    },
    "schema_version": "riftscan.coord_api_truth_current.v1",
    "status": "api_and_riftscan_memory_candidate_matched_trace_anchor_blocked"
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
  "latest_plan": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan",
  "manifest_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json",
  "handoff_path": "plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
  "manifest": {
    "schema_version": "riftscan.focus_gated_capture_plan.v1",
    "created_utc": "2026-05-06T04:28:24Z",
    "app_version": "riftscan-operator-app-v3.8.17",
    "plan_id": "20260506T042824Z_focus_gated_capture_plan",
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
      "process_id": 11220,
      "process_name": "rift_x64",
      "window_hwnd": 657876,
      "window_hwnd_hex": "0xA09D4",
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
{"timestamp_utc": "2026-05-06T10:38:14Z", "event": "script_start", "script": "C:\\RIFT MODDING\\Riftscan\\tools\\rift_focus_control.py", "repo_root": "C:\\RIFT MODDING\\Riftscan", "process_name": "rift_x64", "explicit_pid": 0, "retries": 3, "settle_ms": 400}
{"timestamp_utc": "2026-05-06T10:38:14Z", "event": "powershell_start", "command": "$items = @(Get-Process -Name 'rift_x64' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); $items | ConvertTo-Json -Depth 4"}
{"timestamp_utc": "2026-05-06T10:38:15Z", "event": "powershell_finish", "success": true, "returncode": 0, "elapsed_ms": 571, "stdout_length": 210, "stderr_length": 0}
{"timestamp_utc": "2026-05-06T10:38:15Z", "event": "focus_attempt", "attempt": 1, "restore_ok": true, "set_foreground_ok": true, "foreground_hwnd": 657876, "foreground_hwnd_hex": "0xA09D4", "foreground_pid": 11220, "foreground_title": "RIFT", "verified": true}
{"timestamp_utc": "2026-05-06T10:38:15Z", "event": "script_finish", "success": true, "status": "foreground_verified"}
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
