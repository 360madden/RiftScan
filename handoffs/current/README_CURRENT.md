# RiftScan Current Resume Pointer

## Current source of truth

Start here for new RiftScan work:

1. `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md`
2. `handoffs/current/operator/operator-current-gate-summary.json`
3. `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`
4. `docs/helper-tooling-policy.md`

The older 2026-05-05 handoffs remain useful history, but their next-step ordering is superseded by the 2026-05-06 Operator Gate Workflow handoff and the current gate summary JSON.

## Latest verified workflow-code milestone referenced by this pointer

```text
3ae21d7 Record current-client gate pass and capture plan
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
- The no-GUI diagnostics wrapper `.\scripts\run-riftscan-operator-offline-diagnostics.cmd` runs the safe helper sweep, self-tests, Capture Plan Check, and Operator report refresh without GUI clicks.
- Operator reports include a `Current Workflow Gate` go/no-go section and `handoffs/current/operator/operator-current-gate-summary.json` for baseline/readiness/full-preflight state.
- Current-client Post-Update Baseline and Capture Readiness were rerun from the Operator flow and now PASS.
- Latest metadata-only capture plan: `plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan`.
- Latest Capture Plan Check artifacts: `handoffs/current/capture-plan-check/`.
- Future live-collection criteria are documented under `handoffs/current/live-collection-gate/`; this is a checklist only, not authorization.
- Patch Intake can require Operator, Post-Update Baseline, Capture Readiness, and Offline Workflow Check post-apply py_compile/self-test checks for future gate patches.

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
- `live_collection_allowed` remains `false`; do not resume live capture, coordinate recovery, actor/camera discovery, movement/input, `/reloadui`, offset validation, or RiftReader validation from this milestone alone.
- Treat RiftReader assumptions as unvalidated after a game update unless RiftReader-specific recovery docs and live proof say otherwise.

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

Review the latest Capture Plan Check report at `handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md`, then review the future live-collection gate checklist at `handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md`. Do not start real capture, scanner/discovery probes, movement/input, `/reloadui`, offset validation, or RiftReader validation from this plan without a separate explicit live-collection gate approval.
