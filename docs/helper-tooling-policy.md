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
- Capture Plan Check and other metadata-plan validators
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

tools/riftscan_capture_plan_check.py
scripts/run-riftscan-capture-plan-check.cmd

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

Capture plan validation:

```text
python tools/riftscan_capture_plan_check.py --self-test
.\scripts\run-riftscan-capture-plan-check.cmd --strict-exit-code
```

No-GUI Operator diagnostics:

```text
.\scripts\run-riftscan-operator-offline-diagnostics.cmd
```

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
```

The Operator Diagnostics tab may expose offline self-tests, Offline Workflow Check, and Capture Plan Check when the action does not touch live RIFT state, capture, input, memory scan/read, offsets, RiftReader validation, or `/reloadui`. Capture Plan Check reads existing metadata artifacts only.

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

## Next preferred extension

The next helper workflow should keep the same Python-first pattern. Prefer capture-plan review/checking and a formal future live-collection gate first. Do not start live capture or discovery until Post-Update Baseline, Capture Readiness, Capture Plan Check, and explicit operator approval all pass under that separate future gate.
