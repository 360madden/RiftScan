# RiftScan Offline AI Workflow Packet

Created UTC: `2026-05-07T17:56:23Z`
App version: `riftscan-ai-workflow-packet-v1.8.0`

## Result

```text
status: PASS
mode: offline_ai_workflow
live_action_authorized: False
discovery_ledger_contract: PASS
candidate_ledger_consumer: PASS
```

## Current best offline candidate

| Field | Value |
|---|---|
| Stable ID | `coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120` |
| State | `validated_candidate_historical_checkpoint` |
| Claim level | `validated_candidate` |
| Address | `0x2400EA32120` |
| Base + offset | `0x2400E970000` + `0xC2120` |
| Live use authorized | `False` |
| Next validation | `rerun exact current PID/HWND proof readback before any more live movement` |

## Candidate ledger consumer

```text
status: PASS
safe_candidate_count: 3
rejected_candidate_count: 0
artifact_stale_count: 2
artifact_missing_count: 0
current_best_stale_count: 0
current_best_missing_count: 0
```

## Blockers

- None for offline AI workflow.

## Previous packet diff

```text
status: UNCHANGED
previous_created_utc: 2026-05-07T17:52:03Z
previous_app_version: riftscan-ai-workflow-packet-v1.8.0
change_count: 0
```

Compared fields are listed in JSON at `previous_packet_diff_compared_fields` and documented in `docs/ai-workflow-packet-schema.md`.

## Previous packet archive

```json
{
  "archive_stem": "2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0",
  "artifacts": {
    "report": "handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0.md",
    "summary": "handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0.json"
  },
  "history_dir": "handoffs/current/ai-workflow/history",
  "history_index": "handoffs/current/ai-workflow/history/index.jsonl",
  "source_app_version": "riftscan-ai-workflow-packet-v1.8.0",
  "source_created_utc": "2026-05-07T17:52:03Z",
  "status": "ARCHIVED"
}
```

## Packet history index

```json
{
  "entry": {
    "archive_stem": "2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0",
    "artifacts": {
      "report": "handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0.md",
      "summary": "handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T17-52-03Z-riftscan-ai-workflow-packet-v1-8-0.json"
    },
    "indexed_utc": "2026-05-07T17:56:23Z",
    "schema_version": "riftscan.ai_workflow_packet_history_index.v1",
    "source_app_version": "riftscan-ai-workflow-packet-v1.8.0",
    "source_created_utc": "2026-05-07T17:52:03Z"
  },
  "path": "handoffs/current/ai-workflow/history/index.jsonl",
  "status": "APPENDED"
}
```

## Warnings

- Candidate Ledger Consumer: Historical/non-current candidate rows include 2 stale source artifact(s); keep them historical.

## Recommended first files

- `handoffs/current/README_CURRENT.md`
- `handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md`
- `handoffs/current/ai-workflow/ai-workflow-summary.json`
- `handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md`
- `handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json`
- `handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md`
- `handoffs/current/discovery-ledger/discovery-ledger-summary.json`
- `docs/discovery-ledger-workflow.md`
- `docs/ai-workflow-packet-schema.md`
- `AGENTS.md`
- `docs/agent-execution-workflow.md`

## Top next actions

1. Keep work offline while RiftReader or the operator owns the game window.
2. Start from this AI Workflow Packet, then inspect the Candidate Ledger Consumer and Discovery Ledger reports.
3. Use only stored artifacts, docs, schema checks, report generation, and deterministic tests.
4. Do not start focus preflight, live capture, process attach, memory reads, movement/input, RiftReader commands, offset validation, or /reloadui.
5. Treat the current best coordinate candidate as offline evidence only until a fresh exact PID/HWND proof readback is explicitly authorized.
6. If adding a helper, follow the Python tool + thin CMD wrapper + Markdown/JSON/JSONL artifact pattern.
7. Run py_compile, helper self-tests, Offline Workflow Check, JSON/JSONL validation, dotnet build/test/format, and git diff checks at the milestone boundary.
8. Commit and push coherent offline workflow milestones only after validation passes.
9. If any artifact conflicts, prefer the newest PASS machine-readable artifact and preserve older artifacts as historical evidence.
10. Review Previous packet diff before deciding whether a new offline slice changed truth or only refreshed timestamps/logs.

## AI resume prompt

```text
Resume RiftScan in offline AI workflow mode. Read handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md, then the Candidate Ledger Consumer and Discovery Ledger artifacts. Do not touch live RIFT, focus preflight, capture, process attach/memory reads, movement/input, RiftReader commands, offset validation, or /reloadui unless the user explicitly authorizes live work.
```

## Safety

```json
{
  "focus_preflight_started": false,
  "live_action_authorized": false,
  "live_capture_started": false,
  "memory_scan_or_read_started": false,
  "movement_or_input_sent": false,
  "offline_only": true,
  "offset_validation_started": false,
  "process_attach_or_memory_read_started": false,
  "reloadui_sent": false,
  "riftreader_command_executed": false
}
```

## Source artifacts

```json
{
  "agent_workflow_doc": "docs/agent-execution-workflow.md",
  "agents_contract": "AGENTS.md",
  "ai_workflow_packet_schema_doc": "docs/ai-workflow-packet-schema.md",
  "candidate_ledger_consumer_report": "handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md",
  "candidate_ledger_consumer_summary": "handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json",
  "discovery_ledger_doc": "docs/discovery-ledger-workflow.md",
  "discovery_ledger_report": "handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md",
  "discovery_ledger_summary": "handoffs/current/discovery-ledger/discovery-ledger-summary.json",
  "offline_workflow_summary": "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json",
  "operator_summary": "handoffs/current/operator/operator-current-gate-summary.json",
  "readme_current": "handoffs/current/README_CURRENT.md"
}
```
