# RiftScan Current Resume Pointer

## Current source of truth

Start here for new RiftScan work:

1. `handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md`
2. `handoffs/current/ai-workflow/ai-workflow-summary.json`
3. `handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md`
4. `handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json`
5. `handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md`
6. `handoffs/current/discovery-ledger/discovery-ledger-summary.json`
7. `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md`
8. `handoffs/current/operator/operator-current-gate-summary.json`
9. `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`
10. `docs/helper-tooling-policy.md`
11. `docs/discovery-ledger-workflow.md`

For autonomous offline AI workflow while RiftReader owns the game window, prefer the AI Workflow Packet first, then the Candidate Ledger Consumer, then the offline discovery ledger. The packet summarizes current safe context; the consumer exposes an offline-only safe candidate view; the ledger reconciles ignored/generated RiftScan evidence with RiftReader's tracked current-proof pointer without running focus, capture, input, movement, memory reads, RiftReader commands, offset validation, or `/reloadui`.

The older 2026-05-05 handoffs remain useful history, but their next-step ordering is superseded by the 2026-05-06 Operator Gate Workflow handoff, the current gate summary JSON, and the newer offline discovery ledger.

## Latest verified workflow-code milestone referenced by this pointer

```text
8ebf266 Record current API coordinate candidate
```

A newer doc-only handoff-pointer commit may exist; check `git log --oneline -5` for the exact current HEAD before editing.

## Completed since the 2026-05-05 workflow handoff

- `Post-Update Baseline` is wired into the Operator GUI.
- `Post-Update Baseline` has an offline self-test: `python tools/riftscan_post_update_baseline.py --self-test`.
- Python-first helper tooling direction is documented.
- `Capture Readiness` is the active metadata-only gate between a fresh PASS baseline and any capture-plan refresh.
- `Capture Readiness` has an offline self-test: `python tools/riftscan_capture_readiness.py --self-test`.
- Operator Diagnostics has `Post-Update Baseline Self-Test` and `Capture Readiness Self-Test` buttons for offline GUI wiring checks.
- Operator has an offline gate self-test: `python tools/riftscan_operator_app.py --self-test`.
- Operator has report-only CLI/CMD refresh paths: `python tools/riftscan_operator_app.py --write-report` and `.\scripts\run-riftscan-operator-report.cmd`.
- Offline Workflow Check is a Python-first helper sweep with a thin CMD wrapper: `python tools/riftscan_offline_workflow_check.py --self-test` and `.\scripts\run-riftscan-offline-workflow-check.cmd`.
- Operator Diagnostics has an `Offline Workflow Check` button for offline helper validation without capture/input/memory-read behavior.
- Capture Plan Check is a Python-first metadata validator with a thin CMD wrapper: `python tools/riftscan_capture_plan_check.py --self-test` and `.\scripts\run-riftscan-capture-plan-check.cmd --strict-exit-code`.
- Operator Diagnostics has a `Capture Plan Check` button that validates existing metadata artifacts only.
- Movement Test Readiness is a Python-first metadata/readiness validator with a thin CMD wrapper: `python tools/riftscan_movement_test_readiness.py --self-test` and `.\scripts\run-riftscan-movement-test-readiness.cmd --strict-exit-code`.
- Operator Diagnostics has a `Movement Test Readiness` button that validates movement-live-test prerequisites without running focus preflight, capture, input, movement, RiftReader validation, memory reads, offsets, or `/reloadui`.
- Movement Execution Gate is a Python-first final no-input current-window gate with a thin CMD wrapper: `python tools/riftscan_movement_execution_gate.py --self-test` and `.\scripts\run-riftscan-movement-execution-gate.cmd --strict-exit-code`.
- Operator Diagnostics has a `Movement Execution Gate` button. It may run focus preflight and the live wrapper preflight/RiftReader anchor check, but it must not start capture, send movement/input, validate offsets, or run `/reloadui`.
- The no-GUI diagnostics wrapper `.\scripts\run-riftscan-operator-offline-diagnostics.cmd` runs the safe helper sweep, self-tests, Capture Plan Check, Movement Test Readiness, Operator report refresh, and AI Workflow Packet refresh without GUI clicks.
- Operator reports include a `Current Workflow Gate` go/no-go section and `handoffs/current/operator/operator-current-gate-summary.json` for baseline/readiness/full-preflight state.
- Current-client Post-Update Baseline and Capture Readiness were rerun from the Operator flow and now PASS.
- Latest metadata-only capture plan: `plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan`.
- Latest Capture Plan Check artifacts: `handoffs/current/capture-plan-check/`.
- Latest Movement Test Readiness artifacts: `handoffs/current/movement-test-readiness/`.
- Latest Movement Execution Gate artifacts: `handoffs/current/movement-execution-gate/`.
- Future live-collection criteria are documented under `handoffs/current/live-collection-gate/`; this is a checklist only, not authorization.
- Patch Intake can require Operator, Post-Update Baseline, Capture Readiness, and Offline Workflow Check post-apply py_compile/self-test checks for future gate patches.
- Offline Discovery Ledger is a Python-first, read-only artifact inventory: `python tools/riftscan_discovery_ledger.py --self-test`, `python tools/riftscan_discovery_ledger.py`, `python tools/riftscan_discovery_ledger.py --validate-existing`, and `.\scripts\run-riftscan-discovery-ledger.cmd`.
- Latest Offline Discovery Ledger artifacts: `handoffs/current/discovery-ledger/`.
- The ledger contract is documented in `docs/discovery-ledger-workflow.md`.
- Ledger generation embeds candidate-ledger contract validation in the summary/report, and Offline Workflow Check includes Discovery Ledger self-test + refresh + candidate-ledger contract validation; `.\scripts\run-riftscan-offline-workflow-check.cmd` updates and checks the ledger without touching the game window.
- Candidate Ledger Consumer is a safe offline-only downstream view with source-artifact age diagnostics: `python tools/riftscan_candidate_ledger_consumer.py --self-test`, `python tools/riftscan_candidate_ledger_consumer.py`, and `.\scripts\run-riftscan-candidate-ledger-consumer.cmd --max-artifact-age-hours 24 --strict-exit-code`.
- Latest Candidate Ledger Consumer artifacts: `handoffs/current/candidate-ledger-consumer/`.
- AI Workflow Packet is a compact offline agent resume packet with previous-packet diffing: `python tools/riftscan_ai_workflow_packet.py --self-test`, `python tools/riftscan_ai_workflow_packet.py`, and `.\scripts\run-riftscan-ai-workflow-packet.cmd --strict-exit-code`.
- Latest AI Workflow Packet artifacts: `handoffs/current/ai-workflow/`.

## Current direction

```text
C#/.NET = product/core engine
Python = helper apps, operator workflows, gates, reports, patch intake
CMD = thin launchers for easy operation
PowerShell = rare tiny Windows bridge only
```

Do not rewrite the C# core into Python. Use Python for workflow/control-plane tooling that writes deterministic Markdown, JSON, and JSONL artifacts.

## Safety state

- RIFT updated recently; the current-client Operator gates now PASS for metadata capture-plan refresh only.
- `metadata_capture_plan_gate: PASS`, `post_update_baseline: PASS`, `capture_readiness: PASS`, and `capture_readiness_baseline_link: match`.
- `latest_metadata_capture_plan: valid_metadata_only` when the current plan is present and still metadata-only.
- `Capture Plan Check: PASS` means the latest plan is valid for review/refinement only.
- `Movement Test Readiness: PASS` means the repo control-plane is ready to stage a separately gated movement test; it does not send input or authorize movement execution by itself.
- `Movement Execution Gate: PASS` is short-lived and applies only to the exact bounded command printed in that gate before `expires_utc`.
- Current Movement Execution Gate artifacts are older/blocking evidence from RiftScan's local gate lane; the Offline Discovery Ledger may point at newer RiftReader proof-pointer evidence, but it still does not authorize movement.
- `live_collection_allowed` remains `false` from RiftScan alone; do not resume live capture, coordinate recovery, actor/camera discovery, movement/input, `/reloadui`, offset validation, or RiftReader validation from this milestone alone.
- Treat RiftReader proof-pointer facts as artifact evidence that still requires fresh exact PID/HWND preflight before any future live input.

## Milestone publishing rule

For meaningful workflow milestones:

1. inspect first
2. patch surgically
3. run relevant validation
4. `git status --short`
5. stage explicit paths only, never `git add .`
6. commit
7. push
8. verify `main...origin/main` is `0 0`

## Current next recommended action

Review `handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md`, then `handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md`, `handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md`, and `docs/discovery-ledger-workflow.md`. If RiftReader still owns the game window, keep RiftScan work offline: update/validate packet, consumer, and ledger artifacts from stored evidence only and do not start focus preflight, live capture, scanner/discovery probes, process attach/memory reads, movement/input, `/reloadui`, offset validation, or RiftReader validation from RiftScan.
