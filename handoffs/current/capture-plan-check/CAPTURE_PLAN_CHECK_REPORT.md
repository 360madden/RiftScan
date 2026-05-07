# RiftScan Capture Plan Check Report

## Result

```text
CAPTURE PLAN CHECK: PASS
status: pass
```

## Blockers

- None

## Warnings

- None

## Latest Capture Plan

```text
latest_plan: plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan
manifest: plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json
handoff: plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md
manifest_exists: True
handoff_exists: True
```

## Operator Gate

```text
summary_path: handoffs/current/operator/operator-current-gate-summary.json
metadata_capture_plan_gate: PASS
post_update_baseline: PASS
capture_readiness: PASS
capture_readiness_baseline_link: match
live_collection_allowed: False
old_offsets_trusted: False
next_action: Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.
```

## PASS-but-not-live Meaning

```text
metadata_capture_plan_gate: PASS means metadata capture-plan review/refinement is allowed.
live_collection_allowed: false means real capture, scanner/discovery probes, movement/input, /reloadui, offset validation, and RiftReader validation are still blocked.
```

## Safety Boundary

```text
metadata_only: true
capture_started: false
capture_completed: false
live_collection_allowed: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
reloadui_sent: false
```

## Output Paths

```text
report: handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md
summary: handoffs/current/capture-plan-check/capture-plan-check-summary.json
log: handoffs/current/capture-plan-check/capture-plan-check-log.jsonl
```

## Next Step

Review/refine the latest metadata-only capture plan. Real live collection remains blocked until a separate live-collection gate is explicitly approved.

## Git Snapshot

```text
head: 60a8e5b1066a4358a91ff0df77eb8604e93f965f
```

Git status:

```text
 M docs/ai-workflow-packet-schema.md
 M docs/helper-tooling-policy.md
 M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md
 M handoffs/current/ai-workflow/ai-workflow-log.jsonl
 M handoffs/current/ai-workflow/ai-workflow-summary.json
 M handoffs/current/ai-workflow/history/index.jsonl
 M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json
 M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl
 M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md
 M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl
 M handoffs/current/discovery-ledger/discovery-ledger-summary.json
 M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md
 M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl
 M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json
 M tools/riftscan_ai_workflow_packet.py
 M tools/riftscan_offline_workflow_check.py
?? handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md
?? handoffs/current/ai-workflow/ai-workflow-history-index-summary.json
?? handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.md
?? handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.json

```

Recent commits:

```text
60a8e5b Add AI packet history index view
aad6f11 Validate full AI packet history index
237d9f2 Index AI workflow packet history
d0d0e92 Validate AI packet archive offline
6bef855 Archive AI workflow packet history
```

## Machine-Readable Summary

```json
{
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
  "created_utc": "2026-05-07T18:11:21Z",
  "display_status": "PASS",
  "git": {
    "head": "60a8e5b1066a4358a91ff0df77eb8604e93f965f",
    "log_oneline_5": "60a8e5b Add AI packet history index view\naad6f11 Validate full AI packet history index\n237d9f2 Index AI workflow packet history\nd0d0e92 Validate AI packet archive offline\n6bef855 Archive AI workflow packet history",
    "status_short": " M docs/ai-workflow-packet-schema.md\n M docs/helper-tooling-policy.md\n M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md\n M handoffs/current/ai-workflow/ai-workflow-log.jsonl\n M handoffs/current/ai-workflow/ai-workflow-summary.json\n M handoffs/current/ai-workflow/history/index.jsonl\n M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\n M handoffs/current/capture-plan-check/capture-plan-check-log.jsonl\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M tools/riftscan_ai_workflow_packet.py\n M tools/riftscan_offline_workflow_check.py\n?? handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md\n?? handoffs/current/ai-workflow/ai-workflow-history-index-summary.json\n?? handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.md\n?? handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.json\n"
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
}
```
