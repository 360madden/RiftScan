# RiftScan Future Live-Collection Gate Checklist

Created UTC: `2026-05-06T05:05:00Z`
Status: `DEFINED_NOT_SATISFIED`

## Purpose

Separate "ready to review a metadata capture plan" from "allowed to run any real live collection."

This checklist is a future approval gate only. It does not authorize live capture by itself.

## Required PASS inputs

Before any real collection command exists or runs, require all of these fresh current-client artifacts:

1. `Post-Update Baseline: PASS`
   - Report: `handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md`
   - Summary: `handoffs/current/post-update-baseline/post-update-baseline-summary.json`
2. `Capture Readiness: PASS`
   - Report: `handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md`
   - Summary: `handoffs/current/capture-readiness/capture-readiness-summary.json`
3. `Capture Plan Check: PASS`
   - Report: `handoffs/current/capture-plan-check/CAPTURE_PLAN_CHECK_REPORT.md`
   - Summary: `handoffs/current/capture-plan-check/capture-plan-check-summary.json`
4. `Movement Test Readiness: PASS` before any movement-labeled test is staged:
   - Report: `handoffs/current/movement-test-readiness/MOVEMENT_TEST_READINESS_REPORT.md`
   - Summary: `handoffs/current/movement-test-readiness/movement-test-readiness-summary.json`
5. `Movement Execution Gate: PASS` immediately before any movement/input command:
   - Report: `handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md`
   - Summary: `handoffs/current/movement-execution-gate/movement-execution-gate-summary.json`
   - Must show `movement_execution_allowed=true`
   - Must still be before `expires_utc`
6. Operator report refreshed after the checks:
   - `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`
   - `handoffs/current/operator/operator-current-gate-summary.json`
7. Explicit operator approval in the current session for the exact next live-collection slice.

## Still forbidden in this future gate unless separately approved

- movement/input
- `/reloadui`
- offset trust or offset validation
- RiftReader anchor/orientation validation
- scanner/discovery expansion beyond the exact approved slice
- service/listener/polling/scheduled task behavior
- hidden commit/push
- `git add .`

## First allowed live slice shape after approval

If this gate is explicitly approved later, the first live slice should be:

- read-only
- passive only
- short timeout
- no stimulus
- no movement/input
- no `/reloadui`
- append-only artifacts
- strict abort on focus drift
- explicit report/summary/log output

## First movement-labeled live slice shape after passive proof

Movement-labeled testing remains a later step after passive live proof. When allowed, it should be:

- exact target PID/HWND verified immediately before input
- one short `move_forward` stimulus only
- bounded duration
- no turn/camera/reloadui mixed into the same run
- abort if focus drifts
- use `scripts/live-test-riftscan.cmd` with explicit `-Stimulus move_forward`
- require delta-summary proof: `stimulus_observed_primary_triplet_changed`

## Abort conditions

Abort immediately if any of these occur:

- Post-Update Baseline is not fresh PASS.
- Capture Readiness is not fresh PASS.
- Capture Plan Check is not PASS.
- Movement Test Readiness is not PASS before a movement-labeled run.
- Movement Execution Gate is not PASS immediately before a movement/input command.
- Movement Execution Gate is PASS but `expires_utc` has passed.
- Operator approval is absent or ambiguous.
- Focus is not foreground-verified for the target RIFT window.
- PID/HWND changes relative to the fresh baseline/readiness artifacts.
- Any command would send movement/input or `/reloadui`.
- Any command would validate or trust old offsets.
- Any artifact path would overwrite an existing raw/live artifact unexpectedly.

## Current decision

```text
live_collection_allowed: false
live_collection_gate_status: DEFINED_NOT_SATISFIED
next_action: Review this checklist only; do not run live collection from this milestone.
```
