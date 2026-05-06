---
schema_version: riftscan.resume_handoff.v1
handoff_id: RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW
created_utc: 2026-05-06T02:45:00Z
repo: 360madden/RiftScan
branch: main
local_repo_root: "C:\\RIFT MODDING\\Riftscan"
latest_verified_commit: "b7bd739d3da8d9d5ec1c0e02241590e6dde682af"
latest_verified_commit_subject: "Add movement test readiness gate"
current_gate_artifact: "handoffs/current/operator/operator-current-gate-summary.json"
metadata_capture_plan_gate: "PASS"
live_collection_allowed_now: false
old_offsets_trusted: false
supersedes:
  - "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_NEXT_TOP_10_WORKFLOW.md"
  - "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_TRANSFER_OPERATOR_GUIDE.md"
  - "handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_POST_UPDATE_BASELINE.md"
---

# RiftScan Resume Handoff — Operator Gate Workflow Current Truth

## TL;DR

Use this handoff and `handoffs/current/README_CURRENT.md` as the current resume entry point.

The older 2026-05-05 handoffs are useful historical context, but their next-step lists are superseded. In particular, `Wire Post-Update Baseline into the Operator GUI` is complete.

Latest verified workflow-code commit:

```text
b7bd739d3da8d9d5ec1c0e02241590e6dde682af Add movement test readiness gate
```

Newer generated-artifact or doc-only commits may exist. Always verify the exact current HEAD with `git log --oneline -5` before editing.

Current Operator workflow gate:

```text
metadata_capture_plan_gate: PASS
post_update_baseline: PASS
capture_readiness: PASS
full_live_preflight: PASS
focus_preflight: PASS
live_collection_allowed: false
old_offsets_trusted: false
next_action: Run/refresh Movement Execution Gate; if BLOCKED, resolve the listed live-wrapper freshness blockers before any movement.
latest_metadata_capture_plan: valid_metadata_only
capture_plan_check: PASS
movement_test_readiness: PASS
movement_execution_gate: BLOCKED (stale ReaderBridgeExport.lua; TraceMatchesProcess not true; source object sample mismatch)
```

Source artifacts:

```text
handoffs/current/operator/operator-current-gate-summary.json
plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/capture-plan.json
plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md
```

## Current tool versions

```text
Operator Helper App: riftscan-operator-app-v3.8.20
Capture Readiness: riftscan-capture-readiness-v1.0.1
Patch Intake Helper: riftscan-patch-intake-v1.2.5
Post-Update Baseline: riftscan-post-update-baseline-v1.0.1
Offline Workflow Check: riftscan-offline-workflow-check-v1.0.3
Capture Plan Check: riftscan-capture-plan-check-v1.0.0
Movement Test Readiness: riftscan-movement-test-readiness-v1.0.0
Movement Execution Gate: riftscan-movement-execution-gate-v1.0.0
```

## What changed since the 2026-05-05 handoffs

Completed workflow milestones now pushed to GitHub:

```text
b3bb14d Add offline workflow check helper
a2cf481 Add operator report command wrapper
a312fe1 Refresh current handoff after operator intake check
430f0b4 Add operator self-test patch intake check
e581464 Update current handoff for patch intake checks
5d237bc Add post-update baseline patch intake checks
cb1c058 Refresh operator gate freshness artifacts
ede75ba Classify gate artifact freshness
ba6583b Refresh operator gate after readiness link check
8652930 Add operator report CLI and readiness link gate
4f17bbe Refresh blocked gate artifacts after baseline self-test
870a721 Add post-update baseline self-test
8311033 Clarify current handoff commit reference
b4062da Refresh current RiftScan handoff pointer
a666c77 Add operator gate self-test
b07990f Add operator current workflow gate summary
c95301d Add operator capture readiness self-test button
97923b1 Add capture readiness self-test
56dd777 Add capture readiness gate
27ecb05 Document Python helper tooling direction
f0f0362 Wire post-update baseline into operator app
b9868ba Add next-step workflow handoff
```

The Operator now has:

- Main tab `Post-Update Baseline` button.
- Main tab `Capture Readiness` button.
- Diagnostics tab `Post-Update Baseline Self-Test` button.
- Diagnostics tab `Capture Readiness Self-Test` button.
- Diagnostics tab `Operator Gate Self-Test` button.
- Diagnostics tab `Offline Workflow Check` button.
- Diagnostics tab `Capture Plan Check` button.
- Diagnostics tab `Movement Test Readiness` button.
- CLI/CMD report-only refresh paths: `python tools\riftscan_operator_app.py --write-report` and `.\scripts\run-riftscan-operator-report.cmd`.
- `Current Workflow Gate` section in `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`.
- Machine-readable gate summary at `handoffs/current/operator/operator-current-gate-summary.json`.
- `capture_readiness_baseline_link` stale-chain check in the machine-readable gate summary.
- Artifact freshness classification for Post-Update Baseline and Capture Readiness summaries.
- Latest Offline Workflow Check summary/reference in the Operator report.
- Latest Capture Plan Check summary/reference in the Operator report.
- Latest Movement Test Readiness summary/reference in the Operator report.
- Next-action logic that recognizes when `metadata_capture_plan_gate: PASS` already has a valid metadata-only capture plan and advances to Capture Plan Check/review instead of asking to refresh the plan again.
- No-GUI diagnostics wrapper: `.\scripts\run-riftscan-operator-offline-diagnostics.cmd`.

Offline Workflow Check now has:

- Python-first implementation with a thin CMD wrapper.
- Markdown, JSON, and JSONL artifacts under `handoffs/current/offline-workflow-check/`.
- Conservative offline safety fields; it does not run focus preflight, live capture, input, movement, memory scan/read, offset validation, RiftReader validation, or `/reloadui`.
- Helper py_compile/self-test sweep for Operator, Post-Update Baseline, Capture Readiness, Patch Intake, and itself.

Capture Plan Check now has:

- Python-first implementation with a thin CMD wrapper.
- Markdown, JSON, and JSONL artifacts under `handoffs/current/capture-plan-check/`.
- Validation that the latest capture plan is metadata-only, has `capture_started=false` and `capture_completed=false`, includes expected metadata output filenames, references existing focus/preflight artifacts, and sees `live_collection_allowed=false`.
- Conservative PASS/BLOCKED behavior; it does not run focus preflight, capture, input, movement, memory scan/read, offset validation, RiftReader validation, or `/reloadui`.

The future live-collection gate is documented but not satisfied:

- Checklist path: `handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md`.
- Summary path: `handoffs/current/live-collection-gate/live-collection-gate-summary.json`.
- It requires fresh Post-Update Baseline PASS, Capture Readiness PASS, Capture Plan Check PASS, Movement Test Readiness PASS, Movement Execution Gate PASS immediately before movement/input, explicit operator approval, no `/reloadui`, no offset trust, exact PID/HWND revalidation, and clear abort conditions.

Movement Test Readiness now has:

- Python-first implementation with a thin CMD wrapper.
- Markdown, JSON, and JSONL artifacts under `handoffs/current/movement-test-readiness/`.
- Validation that the existing `scripts/live-test-riftscan.cmd` / `.ps1` movement wrapper is present and still contains guard features for `move_forward`, `-PreflightOnly`, ReaderBridge freshness, RiftReader anchor reads, RiftScan passive capture, and delta-summary proof.
- Conservative PASS/BLOCKED behavior; it does not run focus preflight, capture, input, movement, RiftReader validation, memory scan/read, offset validation, or `/reloadui`.

Movement Execution Gate now has:

- Python-first implementation with a thin CMD wrapper.
- Markdown, JSON, and JSONL artifacts under `handoffs/current/movement-execution-gate/`.
- Final no-input current-window validation before a future bounded `move_forward` command.
- It may run focus preflight and `scripts/live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly`, which can invoke RiftReader anchor checks.
- Conservative PASS/BLOCKED behavior; it must not start capture, send movement/input, validate offsets, or run `/reloadui`.

Capture Readiness now has:

- CLI/CMD self-test path.
- Conservative PASS/BLOCKED evaluation from Post-Update Baseline + current focus/window artifacts.
- Metadata-only safety fields; it does not capture, send input, scan/read memory, validate offsets, or run `/reloadui`.

Post-Update Baseline now has:

- CLI/CMD self-test path.
- Diagnostics tab `Post-Update Baseline Self-Test` button.
- Offline PASS/BLOCKED logic coverage without writing current handoff artifacts.

Patch Intake now supports post-apply checks for Capture Readiness and Offline Workflow Check patches; future plan-gate patches should include Capture Plan Check too:

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

## Current-client gate pass and capture plan

Current-client Operator actions 1-10 were executed on 2026-05-06:

```text
Offline Workflow Check: PASS
Operator Gate Self-Test: PASS
Post-Update Baseline Self-Test: PASS
Capture Readiness Self-Test: PASS
Open Report: PASS
Post-Update Baseline: PASS
Capture Readiness: PASS
metadata_capture_plan_gate: PASS
metadata-only capture plan: plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan
Capture Plan Check: PASS
Movement Test Readiness: PASS
Movement Execution Gate: BLOCKED until current ReaderBridge/RiftReader anchor freshness passes
live_collection_allowed: false
```

The capture plan is metadata-only. It records `capture_started=false`, `capture_completed=false`, no movement/input, no memory scan/read, no `/reloadui`, and no offset/RiftReader validation.

## Current blockers

There are no current metadata capture-plan blockers. `metadata_capture_plan_gate` is PASS.

Important: `live_collection_allowed` remains false. The PASS gate, Capture Plan Check, and Movement Test Readiness authorize planning/readiness only, not real capture/discovery/movement/input/offset work. The Movement Execution Gate is the final no-input current-window check; if it is BLOCKED, do not send movement/input.

Current Movement Execution Gate blockers:

- `ReaderBridgeExport.lua` is stale by file time.
- RiftReader anchor `TraceMatchesProcess` is not true.
- Source object coordinate sample does not match ReaderBridge within tolerance.

## Hard safety boundary

Do not start any of these from this milestone without a separate explicit future live-collection gate:

- live capture
- scanner/discovery probes
- coordinate recovery
- actor/camera signal discovery
- movement/input automation
- `/reloadui`
- offset validation
- RiftReader anchor/orientation validation

Old offsets and old live proofs remain historical after the RIFT update unless revalidated by current-client artifacts. The current PASS baseline/readiness gates do not validate RiftReader anchors, offsets, orientation, actor/camera signals, or discovery candidates.

## Safe offline commands

Run these anytime from repo root:

```powershell
python tools\riftscan_operator_app.py --self-test
python tools\riftscan_operator_app.py --write-report
.\scripts\run-riftscan-operator-report.cmd
python tools\riftscan_post_update_baseline.py --self-test
python tools\riftscan_capture_readiness.py --self-test
python tools\riftscan_patch_intake_app.py --self-test
python tools\riftscan_offline_workflow_check.py --self-test
.\scripts\run-riftscan-offline-workflow-check.cmd
python tools\riftscan_capture_plan_check.py --self-test
.\scripts\run-riftscan-capture-plan-check.cmd --strict-exit-code
python tools\riftscan_movement_test_readiness.py --self-test
.\scripts\run-riftscan-movement-test-readiness.cmd --strict-exit-code
python tools\riftscan_movement_execution_gate.py --self-test
.\scripts\run-riftscan-operator-offline-diagnostics.cmd
python -m py_compile tools\riftscan_operator_app.py tools\riftscan_capture_readiness.py tools\riftscan_post_update_baseline.py tools\riftscan_patch_intake_app.py tools\riftscan_offline_workflow_check.py tools\riftscan_capture_plan_check.py tools\riftscan_movement_test_readiness.py tools\riftscan_movement_execution_gate.py
```

Expected self-test results:

```text
Operator gate self-test: PASS
Operator report refresh: PASS
Post-Update Baseline self-test: PASS
Capture Readiness self-test: PASS
Patch Intake self-test: PASS
Offline Workflow Check self-test: PASS
Offline Workflow Check full sweep: PASS
Capture Plan Check self-test: PASS
Capture Plan Check: PASS
Movement Test Readiness self-test: PASS
Movement Test Readiness: PASS
Movement Execution Gate self-test: PASS
Movement Execution Gate: PASS or BLOCKED with exact blockers and no movement/input/capture
Operator offline diagnostics: PASS
```

## GUI smoke-test order

When safe to open GUI:

```powershell
cd "C:\RIFT MODDING\Riftscan"
.\scripts\riftscan-operator-app.cmd
```

Recommended button order:

```text
1. Diagnostics -> Operator Gate Self-Test
2. Diagnostics -> Post-Update Baseline Self-Test
3. Diagnostics -> Capture Readiness Self-Test
4. Diagnostics -> Offline Workflow Check
5. Diagnostics -> Capture Plan Check
6. Diagnostics -> Movement Test Readiness
7. Diagnostics -> Movement Execution Gate
8. Main -> Open Report
9. Main -> Post-Update Baseline only after the current updated RIFT client is truly stable in-world
10. Main -> Capture Readiness
11. Main -> Open Report and require metadata_capture_plan_gate: PASS plus Capture Plan Check PASS plus Movement Test Readiness PASS plus Movement Execution Gate PASS before any future bounded movement command
```

## Current next best action

Review the Movement Execution Gate report and resolve only the listed no-input freshness blockers. Actual movement/input must still use the exact command from a PASS gate before `expires_utc`.

## Top 10 next recommended actions

1. Review `handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md`.
2. If BLOCKED, resolve ReaderBridge/RiftReader anchor freshness blockers without sending movement/input.
3. Rerun `.\scripts\run-riftscan-movement-execution-gate.cmd --strict-exit-code`.
4. Keep the first execution path bounded to one short `move_forward` stimulus only.
5. Require `scripts/live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly` to pass immediately before any movement capture.
6. Keep `/reloadui`, turn/camera mixing, offset validation, and RiftReader anchor/orientation promotion out of the first movement run.
7. Preserve current PASS artifacts; do not overwrite them with exploratory runs unless intentionally refreshing.
8. Run a passive no-stimulus live proof before movement if current-client live collection has not been freshly exercised.
9. Add Patch Intake post-apply checks for Movement Execution Gate py_compile/self-test if future patches touch this gate.
10. Commit and push every coherent milestone promptly with explicit staging only.

## Ready-to-paste resume prompt

```text
Resume RiftScan from C:\RIFT MODDING\Riftscan on main. Read handoffs/current/README_CURRENT.md and handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md first. Treat older 2026-05-05 handoffs as historical/superseded for next-step ordering. Current HEAD should be b7bd739 Add movement test readiness gate or newer. Current Operator gate should show metadata_capture_plan_gate: PASS, post_update_baseline: PASS, capture_readiness: PASS, capture_readiness_baseline_link: match, latest_metadata_capture_plan: valid_metadata_only, capture_plan_check: PASS, movement_test_readiness: PASS, and live_collection_allowed: false. Latest metadata-only capture plan should be plans/focus-gated-capture-plans/20260506T042824Z_focus_gated_capture_plan. Capture Plan Check artifacts should be under handoffs/current/capture-plan-check/. Movement Test Readiness artifacts should be under handoffs/current/movement-test-readiness/. Movement Execution Gate artifacts should be under handoffs/current/movement-execution-gate/ and may be BLOCKED by current ReaderBridge/RiftReader anchor freshness. Do not run live capture, movement/input, /reloadui, scanner probes, offset validation, or RiftReader validation unless Movement Execution Gate is PASS and not expired. First safe actions: inspect handoffs/current/operator/operator-current-gate-summary.json, handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md, and handoffs/current/live-collection-gate/LIVE_COLLECTION_GATE_CHECKLIST.md.
```
