# RiftScan Offline AI Workflow Packet

Created UTC: `2026-05-07T16:51:31Z`
App version: `riftscan-ai-workflow-packet-v1.1.0`

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

## Blockers

- None for offline AI workflow.

## Warnings

- None.

## Recommended first files

- `handoffs/current/README_CURRENT.md`
- `handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md`
- `handoffs/current/ai-workflow/ai-workflow-summary.json`
- `handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md`
- `handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json`
- `handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md`
- `handoffs/current/discovery-ledger/discovery-ledger-summary.json`
- `docs/discovery-ledger-workflow.md`
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
10. The next useful offline slice is stale-artifact age warning or packet diffing; do not pivot into live testing without explicit authorization.

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
