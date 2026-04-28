# RiftScan handoff - passive capture elapsed proof

created_local: 2026-04-28 19:46:51 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: a6d660d Add passive capture elapsed metrics
supersedes: docs/handoffs/2026-04-28-194055-riftscan-passive-capture-schema-proof.md

## TL;DR

Passive capture intervention handling is implemented, pushed, unit-tested, CLI-smoke-tested, disposable-process restart tested, serialization-contract tested, versioned, reported, and now includes elapsed_ms metrics. The remaining unproven item is live RIFT restart/crash behavior.

## Current truth

- Branch is clean and synced with origin/main.
- Latest pushed intervention/report commits:
  - 35f73cb Add passive capture intervention handoff
  - 4f63281 Add handoff path and restart recovery for passive capture
  - d916ee6 Add CLI coverage for capture intervention flags
  - 766fac5 Distinguish unreadable regions during passive capture
  - 8105b3f Add passive capture read failure diagnostics
  - ce67941 Tailor passive capture handoff actions
  - 5ec4576 Add passive capture process restart integration test
  - 0d9b52a Add process name normalization coverage
  - c46c62f Add passive capture result summary fields
  - 0cd6f03 Add passive capture serialization contract tests
  - 4b9da34 Add passive capture result schema version
  - e3266d8 Report passive capture interruptions
  - 67cf82f Document live RIFT restart validation checklist
  - a6d660d Add passive capture elapsed metrics

## Proven behavior

- capture passive and capture plan support --intervention-wait-ms and --intervention-poll-ms.
- PID restart fallback works when both --pid and --process are supplied.
- Recovery refreshes regions and modules after the replacement process is found.
- Recovery retries the interrupted sample when the process instance changes.
- Dotted process names are preserved during Windows process lookup; only .exe is stripped.
- Interrupted captures write intervention_handoff.json and expose handoff_path in CLI JSON output.
- Partial interrupted sessions include intervention_handoff.json in checksums.json when snapshots exist.
- Selected-region unreadable cases are distinguished from process disappearance.
- intervention_handoff.json includes reason-specific recommended_next_action, region_read_failures[], and elapsed_ms.
- PassiveCaptureResult includes result_schema_version = riftscan.passive_capture_result.v1.
- PassiveCaptureResult summary fields are pinned: elapsed_ms, status, samples_requested, samples_attempted, interruption_reason, and region_read_failure_count.
- Report generation includes capture interruption details when intervention_handoff.json exists.

## Validation evidence

Latest targeted elapsed-metrics proof:

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~PassiveCaptureSerializationContractTests|FullyQualifiedName~PassiveCaptureServiceTests"
    # Passed: 19/19

Latest full suite:

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 47/47

Other checks run in this lane:

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Disposable-process proof is strong process-lifecycle evidence, but it is not a live RIFT claim.

## Resume here

1. Confirm clean state: git status --short --branch
2. If staying offline, next smallest code step is rendering elapsed_ms in report.md summary/interruption sections or adding JSON report output for interruption details.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Render passive capture elapsed metrics in reports
