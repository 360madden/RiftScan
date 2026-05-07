# RiftScan Movement Test Readiness Report

## Result

```text
MOVEMENT TEST READINESS: PASS
status: pass
ready_for_live_game_world_movement_testing: true
ready_for_movement_execution_now: false
```

## Blockers

- None

## Warnings

- None

## Meaning

`PASS` means the repository control-plane is ready to proceed to a separately gated live game-world movement test plan.
It does **not** mean this check sent movement/input or started capture.

## Recommended Future Live Movement Shape

```text
preflight: .\scripts\live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly
capture: .\scripts\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100
stimulus: move_forward
pre_capture_wait_ms: 3000
```

## Safety Boundary

```text
readiness_check_only: true
focus_preflight_started: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
reloadui_sent: false
requires_final_live_rerun_before_execution: true
```

## Output Paths

```text
report: handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md
summary: handoffs/current/movement-test-readiness/movement-test-readiness-summary.json
log: handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl
```

## Git Snapshot

```text
head: 1eca1bef210c45f42d232348c732bcb59c3981d5
```

Git status:

```text
 M docs/agent-execution-workflow.md
 M docs/helper-tooling-policy.md
 M handoffs/current/README_CURRENT.md
 M handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md
 M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl
 M handoffs/current/capture-plan-check/capture-plan-check-summary.json
 M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md
 M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl
 M handoffs/current/discovery-ledger/discovery-ledger-summary.json
 M handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl
 M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md
 M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl
 M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json
 M scripts/run-riftscan-operator-offline-diagnostics.cmd
 M tools/riftscan_offline_workflow_check.py
?? handoffs/current/ai-workflow/
?? scripts/run-riftscan-ai-workflow-packet.cmd
?? tools/riftscan_ai_workflow_packet.py

```

Recent commits:

```text
1eca1be Surface discovery ledger validation
676b428 Validate offline discovery ledger contract
9ca69dc Wire discovery ledger into offline diagnostics
7db4d85 Add offline discovery ledger
8ebf266 Record current API coordinate candidate
```

## Machine-Readable Summary

```json
{
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
  "created_utc": "2026-05-07T16:43:10Z",
  "display_status": "PASS",
  "git": {
    "head": "1eca1bef210c45f42d232348c732bcb59c3981d5",
    "log_oneline_5": "1eca1be Surface discovery ledger validation\n676b428 Validate offline discovery ledger contract\n9ca69dc Wire discovery ledger into offline diagnostics\n7db4d85 Add offline discovery ledger\n8ebf266 Record current API coordinate candidate",
    "status_short": " M docs/agent-execution-workflow.md\n M docs/helper-tooling-policy.md\n M handoffs/current/README_CURRENT.md\n M handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md\n M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl\n M handoffs/current/capture-plan-check/capture-plan-check-summary.json\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/movement-test-readiness/movement-test-readiness-log.jsonl\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M scripts/run-riftscan-operator-offline-diagnostics.cmd\n M tools/riftscan_offline_workflow_check.py\n?? handoffs/current/ai-workflow/\n?? scripts/run-riftscan-ai-workflow-packet.cmd\n?? tools/riftscan_ai_workflow_packet.py\n"
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
}
```
