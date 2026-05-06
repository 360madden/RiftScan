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
4. Operator report refreshed after the checks:
   - `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`
   - `handoffs/current/operator/operator-current-gate-summary.json`
5. Explicit operator approval in the current session for the exact next live-collection slice.

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

## Abort conditions

Abort immediately if any of these occur:

- Post-Update Baseline is not fresh PASS.
- Capture Readiness is not fresh PASS.
- Capture Plan Check is not PASS.
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
