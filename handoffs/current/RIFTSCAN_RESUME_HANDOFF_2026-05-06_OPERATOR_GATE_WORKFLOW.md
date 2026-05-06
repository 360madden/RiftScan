---
schema_version: riftscan.resume_handoff.v1
handoff_id: RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW
created_utc: 2026-05-06T02:45:00Z
repo: 360madden/RiftScan
branch: main
local_repo_root: "C:\\RIFT MODDING\\Riftscan"
latest_verified_commit: "430f0b4a73299e106a1567ca1502df6f0b8ca5d0"
latest_verified_commit_subject: "Add operator self-test patch intake check"
current_gate_artifact: "handoffs/current/operator/operator-current-gate-summary.json"
metadata_capture_plan_gate: "BLOCKED"
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
430f0b4a73299e106a1567ca1502df6f0b8ca5d0 Add operator self-test patch intake check
```

Newer generated-artifact or doc-only commits may exist. Always verify the exact current HEAD with `git log --oneline -5` before editing.

Current Operator workflow gate:

```text
metadata_capture_plan_gate: BLOCKED
post_update_baseline: BLOCKED
capture_readiness: BLOCKED
full_live_preflight: PASS
focus_preflight: PASS
live_collection_allowed: false
old_offsets_trusted: false
next_action: Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.
```

Source artifact:

```text
handoffs/current/operator/operator-current-gate-summary.json
```

## Current tool versions

```text
Operator Helper App: riftscan-operator-app-v3.8.16
Capture Readiness: riftscan-capture-readiness-v1.0.1
Patch Intake Helper: riftscan-patch-intake-v1.2.4
Post-Update Baseline: riftscan-post-update-baseline-v1.0.1
```

## What changed since the 2026-05-05 handoffs

Completed workflow milestones now pushed to GitHub:

```text
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
- CLI/CMD report-only refresh paths: `python tools\riftscan_operator_app.py --write-report` and `.\scripts\run-riftscan-operator-report.cmd`.
- `Current Workflow Gate` section in `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`.
- Machine-readable gate summary at `handoffs/current/operator/operator-current-gate-summary.json`.
- `capture_readiness_baseline_link` stale-chain check in the machine-readable gate summary.
- Artifact freshness classification for Post-Update Baseline and Capture Readiness summaries.

Capture Readiness now has:

- CLI/CMD self-test path.
- Conservative PASS/BLOCKED evaluation from Post-Update Baseline + current focus/window artifacts.
- Metadata-only safety fields; it does not capture, send input, scan/read memory, validate offsets, or run `/reloadui`.

Post-Update Baseline now has:

- CLI/CMD self-test path.
- Diagnostics tab `Post-Update Baseline Self-Test` button.
- Offline PASS/BLOCKED logic coverage without writing current handoff artifacts.

Patch Intake now supports post-apply checks for Capture Readiness patches:

```text
py_compile_operator
operator_self_test
py_compile_post_update_baseline
post_update_baseline_self_test
py_compile_capture_readiness
capture_readiness_self_test
```

## Current blockers

The current gate is intentionally BLOCKED because the latest current-client Post-Update Baseline is not PASS.

Current Post-Update Baseline blockers:

```text
Maintenance is not confirmed over.
Login is not confirmed successful.
Stable in-world state is not confirmed.
```

Current Capture Readiness blockers:

```text
Post-update baseline is not PASS for the current client.
Post-update baseline display_status is not PASS.
```

## Hard safety boundary

Do not start any of these until the current-client gates pass:

- live capture
- scanner/discovery probes
- coordinate recovery
- actor/camera signal discovery
- movement/input automation
- `/reloadui`
- offset validation
- RiftReader anchor/orientation validation

Old offsets and old live proofs remain historical after the RIFT update unless revalidated by current-client artifacts.

## Safe offline commands

Run these anytime from repo root:

```powershell
python tools\riftscan_operator_app.py --self-test
python tools\riftscan_operator_app.py --write-report
.\scripts\run-riftscan-operator-report.cmd
python tools\riftscan_post_update_baseline.py --self-test
python tools\riftscan_capture_readiness.py --self-test
python tools\riftscan_patch_intake_app.py --self-test
python -m py_compile tools\riftscan_operator_app.py tools\riftscan_capture_readiness.py tools\riftscan_post_update_baseline.py tools\riftscan_patch_intake_app.py
```

Expected self-test results:

```text
Operator gate self-test: PASS
Operator report refresh: PASS
Post-Update Baseline self-test: PASS
Capture Readiness self-test: PASS
Patch Intake self-test: PASS
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
4. Main -> Open Report
5. Main -> Post-Update Baseline only after the current updated RIFT client is truly stable in-world
6. Main -> Capture Readiness
7. Main -> Open Report and require metadata_capture_plan_gate: PASS before capture-plan refresh
```

## Current next best action

First GUI-smoke-test the three offline self-test buttons. Then, only when the current updated RIFT client is confirmed stable in-world, run `Post-Update Baseline` and require a PASS baseline before `Capture Readiness` or any downstream metadata plan refresh.

## Top 10 next recommended actions

1. GUI-click `Operator Gate Self-Test`.
2. GUI-click `Post-Update Baseline Self-Test`.
3. GUI-click `Capture Readiness Self-Test`.
4. GUI-click `Open Report` and verify `Current Workflow Gate` is visible near the top.
5. When RIFT is genuinely stable in-world, GUI-click `Post-Update Baseline`.
6. Require `POST-UPDATE BASELINE: PASS`.
7. GUI-click `Capture Readiness`.
8. Require `metadata_capture_plan_gate: PASS` in the Operator report.
9. Refresh the metadata-only capture plan only after the gate passes.
10. After current gates pass, proceed only to metadata-only collector follow-up; still do not start movement/input/offset validation.

## Ready-to-paste resume prompt

```text
Resume RiftScan from C:\RIFT MODDING\Riftscan on main. Read handoffs/current/README_CURRENT.md and handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md first. Treat older 2026-05-05 handoffs as historical/superseded for next-step ordering. Current HEAD should be 430f0b4 Add operator self-test patch intake check or newer. Do not run live capture, movement/input, /reloadui, scanner probes, offset validation, or RiftReader validation until the Operator Current Workflow Gate shows metadata_capture_plan_gate: PASS. First safe actions: run python tools\riftscan_operator_app.py --self-test, python tools\riftscan_post_update_baseline.py --self-test, python tools\riftscan_capture_readiness.py --self-test, and inspect handoffs/current/operator/operator-current-gate-summary.json.
```
