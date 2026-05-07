# RiftScan Helper Tooling Policy

## Purpose

Keep RiftScan development fast and safe by separating the core engine from the operator workflow layer.

```text
C#/.NET = RiftScan product/core engine
Python = helper apps, operator workflows, gates, reports, patch intake
CMD = thin launchers for double-click or short terminal use
PowerShell = rare tiny Windows bridge only
```

## Direction

Use Python for all nontrivial helper/operator workflow logic.

This applies to:

- Operator Helper App
- Patch Intake Helper
- Post-Update Baseline tool
- Capture Readiness gate
- Offline Workflow Check runners
- Offline Discovery Ledger and candidate-ledger contract validation
- Candidate Ledger Consumer and offline-only downstream candidate views
- AI Workflow Packet and offline agent resume packets
- Capture Plan Check and other metadata-plan validators
- Movement Test Readiness and other future live-test readiness validators
- Movement Execution Gate and other final no-input live-adjacent gate validators
- no-GUI Operator diagnostics wrappers
- report and handoff writers
- local validation runners
- patch/package intake tooling
- small workflow GUIs

Do **not** rewrite the C#/.NET core into Python.

This does not replace:

- `src/RiftScan.Core`
- `src/RiftScan.Capture`
- `src/RiftScan.Analysis`
- `src/RiftScan.Rift`
- `src/RiftScan.Cli`
- deterministic unit tests around the product engine

## Why

Python is the preferred control-plane language because it is easier to:

- patch surgically without disturbing core scanner/analyzer code
- write Markdown, JSON, and JSONL artifacts deterministically
- expose stable `--help`, `--self-test`, and exit-code behavior
- stop on failures instead of printing misleading success messages
- build Tkinter GUI helpers that are easy for the operator to click
- test offline with fixtures and `python -m py_compile`

CMD wrappers stay intentionally thin so the operator can run tools easily without copying long command blocks.

PowerShell is allowed only when it is the smallest safe bridge to Windows-specific behavior, such as a short process/window probe. Long multi-step PowerShell workflow scripts are discouraged.

## Required pattern for new helper workflows

Every nontrivial helper workflow should prefer:

```text
1. Python implementation in tools/
2. Thin .cmd launcher in scripts/
3. Markdown report under handoffs/current/<workflow>/ or reports/
4. JSON summary beside the report
5. JSONL execution log when execution occurs
6. Stable status/display_status/blockers/paths/safety fields
7. Explicit exit codes
8. Offline validation path when possible
```

Recommended common fields:

```text
schema_version
created_utc
app_version
status
display_status
blockers
paths
safety
source_artifacts
```

## Git and milestone rule

Major workflow milestones should be committed and pushed promptly so GitHub stays current.

Rules:

- never use `git add .`
- stage explicit paths only
- do not auto-commit or auto-push from helpers unless explicitly requested and gated
- after a meaningful milestone, run relevant validation, commit, push, then verify branch parity
- preserve generated handoff/report artifacts when they are evidence for the milestone

## Hard boundaries

Helper tooling must not add:

- hidden auto-commit
- hidden auto-push
- force push
- service install
- listener
- polling/watchers
- scheduled task
- raw shell execution from manifests
- live capture expansion without an explicit gate
- movement/input/reloadui behavior in metadata-only workflows

## Current approved examples

```text
tools/riftscan_operator_app.py
scripts/riftscan-operator-app.cmd
scripts/run-riftscan-operator-report.cmd

tools/riftscan_patch_intake_app.py
scripts/riftscan-patch-intake.cmd

tools/riftscan_post_update_baseline.py
scripts/run-riftscan-post-update-baseline.cmd

tools/riftscan_capture_readiness.py
scripts/run-riftscan-capture-readiness.cmd

tools/riftscan_offline_workflow_check.py
scripts/run-riftscan-offline-workflow-check.cmd

tools/riftscan_discovery_ledger.py
scripts/run-riftscan-discovery-ledger.cmd

tools/riftscan_candidate_ledger_consumer.py
scripts/run-riftscan-candidate-ledger-consumer.cmd

tools/riftscan_ai_workflow_packet.py
scripts/run-riftscan-ai-workflow-packet.cmd

tools/riftscan_capture_plan_check.py
scripts/run-riftscan-capture-plan-check.cmd

tools/riftscan_movement_test_readiness.py
scripts/run-riftscan-movement-test-readiness.cmd

tools/riftscan_movement_execution_gate.py
scripts/run-riftscan-movement-execution-gate.cmd

scripts/run-riftscan-operator-offline-diagnostics.cmd
```

Post-Update Baseline offline validation:

```text
python tools/riftscan_post_update_baseline.py --self-test
```

Capture Readiness offline validation:

```text
python tools/riftscan_capture_readiness.py --self-test
```

Operator gate offline validation:

```text
python tools/riftscan_operator_app.py --self-test
```

Offline workflow check validation and full helper sweep:

```text
python tools/riftscan_offline_workflow_check.py --self-test
.\scripts\run-riftscan-offline-workflow-check.cmd
```

Offline discovery ledger validation:

```text
python tools/riftscan_discovery_ledger.py --self-test
python tools/riftscan_discovery_ledger.py --validate-existing
.\scripts\run-riftscan-discovery-ledger.cmd
```

Candidate ledger consumer validation:

```text
python tools/riftscan_candidate_ledger_consumer.py --self-test
.\scripts\run-riftscan-candidate-ledger-consumer.cmd --strict-exit-code
.\scripts\run-riftscan-candidate-ledger-consumer.cmd --max-artifact-age-hours 24 --strict-exit-code
```

AI workflow packet validation:

```text
python tools/riftscan_ai_workflow_packet.py --self-test
.\scripts\run-riftscan-ai-workflow-packet.cmd --strict-exit-code
```

The packet should include a previous-packet diff so offline agents can distinguish real workflow/truth changes from timestamp-only refreshes. The packet and diff contracts are documented in `docs/ai-workflow-packet-schema.md`, and Offline Workflow Check validates that the current packet exposes the required diff fields.

For quick terminal triage without opening the Markdown report:

```text
python tools/riftscan_ai_workflow_packet.py --print-diff
python tools/riftscan_ai_workflow_packet.py --show-existing-diff
python tools/riftscan_ai_workflow_packet.py --show-history-index
python tools/riftscan_ai_workflow_packet.py --verify-history-index
```

`--print-diff` refreshes packet artifacts first; `--show-existing-diff` is read-only and prints the saved summary diff without appending logs.
`--show-history-index` and `--verify-history-index` are also read-only and do not refresh or archive the current packet.

Each packet refresh archives the prior `ai-workflow-summary.json` and previous Markdown packet under `handoffs/current/ai-workflow/history/` before overwriting current files.
It also appends `handoffs/current/ai-workflow/history/index.jsonl` so offline agents can enumerate prior packet checkpoints without guessing filenames.
Offline Workflow Check validates that archive metadata, archived paths, every history-index row, and archived summary JSON remain usable.

Capture plan validation:

```text
python tools/riftscan_capture_plan_check.py --self-test
.\scripts\run-riftscan-capture-plan-check.cmd --strict-exit-code
```

Movement test readiness validation:

```text
python tools/riftscan_movement_test_readiness.py --self-test
.\scripts\run-riftscan-movement-test-readiness.cmd --strict-exit-code
```

Movement execution gate validation:

```text
python tools/riftscan_movement_execution_gate.py --self-test
.\scripts\run-riftscan-movement-execution-gate.cmd --strict-exit-code
```

The self-test is offline. The CMD gate is live-adjacent but no-input: it may run focus preflight and `scripts/live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly`, which can invoke RiftReader anchor checks. It must not start capture, send movement/input, validate offsets, or run `/reloadui`.

No-GUI Operator diagnostics:

```text
.\scripts\run-riftscan-operator-offline-diagnostics.cmd
```

This wrapper should refresh the Operator report and the offline AI Workflow Packet after the helper sweep passes.

Operator report refresh without launching the GUI:

```text
python tools/riftscan_operator_app.py --write-report
.\scripts\run-riftscan-operator-report.cmd
```

Patch Intake post-apply checks can also require:

```text
py_compile_operator
operator_self_test
py_compile_post_update_baseline
post_update_baseline_self_test
py_compile_capture_readiness
capture_readiness_self_test
py_compile_offline_workflow_check
offline_workflow_check_self_test
py_compile_capture_plan_check
capture_plan_check_self_test
py_compile_movement_test_readiness
movement_test_readiness_self_test
py_compile_movement_execution_gate
movement_execution_gate_self_test
```

The Operator Diagnostics tab may expose offline self-tests, Offline Workflow Check, Capture Plan Check, and Movement Test Readiness when the action does not touch live RIFT state, capture, input, memory scan/read, offsets, RiftReader validation, or `/reloadui`. Capture Plan Check and Movement Test Readiness read existing metadata artifacts only.

The Operator Diagnostics tab may also expose `Movement Execution Gate`. Treat it differently from the offline diagnostics: it is a no-input final gate that revalidates current focus/PID/HWND and the live wrapper preflight immediately before any bounded movement command. A BLOCKED result is the expected safe state when ReaderBridge/RiftReader freshness does not prove the current client.

Operator handoffs should include a compact current workflow gate section that reports whether Post-Update Baseline, Capture Readiness, and required preflight checks allow the next metadata-only action.

Gate summaries should distinguish harmless doc/handoff-only artifact HEAD drift from relevant helper-code drift that requires rerunning the affected gate.

## PASS-but-not-live state

`metadata_capture_plan_gate: PASS` means the current updated client has enough current metadata proof for capture-plan review/refinement.

It does **not** mean live collection is allowed. Until a future live-collection gate explicitly passes:

```text
live_collection_allowed: false
old_offsets_trusted: false
```

Still blocked:

- real memory capture
- scanner/discovery probes
- movement/input
- `/reloadui`
- offset validation
- RiftReader anchor/orientation validation

`MOVEMENT TEST READINESS: PASS` means the repo control-plane is ready to stage a separately gated movement live test. It does not mean movement was sent, capture was started, or old offsets are trusted.

`MOVEMENT EXECUTION GATE: PASS` is narrower and short-lived. It means the exact bounded movement command printed by that gate may be run only before its `expires_utc`. `MOVEMENT EXECUTION GATE: BLOCKED` means do not send movement/input; resolve the listed current-window/live-wrapper blockers first.

## Next preferred extension

The next helper workflow should keep the same Python-first pattern. Prefer current-window gate repair and proof-quality stale-data blockers before any movement. Do not start live capture or discovery until Post-Update Baseline, Capture Readiness, Capture Plan Check, Movement Test Readiness, Movement Execution Gate, and explicit operator approval all pass under that separate future gate.
