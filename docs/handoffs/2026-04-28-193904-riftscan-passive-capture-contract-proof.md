# RiftScan handoff - passive capture contract proof

created_local: 2026-04-28 19:39:04 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 0cd6f03 Add passive capture serialization contract tests
supersedes: docs/handoffs/2026-04-28-193034-riftscan-passive-capture-restart-proof.md

## TL;DR

Passive capture intervention handling is implemented, pushed, unit-tested, CLI-smoke-tested, disposable-process restart tested, and serialization-contract tested. The remaining unproven item is live RIFT restart/crash behavior.

## Current truth

- Branch is clean and synced with origin/main.
- Latest pushed intervention commits:
  - 35f73cb Add passive capture intervention handoff
  - 4f63281 Add handoff path and restart recovery for passive capture
  - d916ee6 Add CLI coverage for capture intervention flags
  - 766fac5 Distinguish unreadable regions during passive capture
  - 8105b3f Add passive capture read failure diagnostics
  - ce67941 Tailor passive capture handoff actions
  - ef075d8 Add passive capture intervention resume handoff
  - 5ec4576 Add passive capture process restart integration test
  - 0d9b52a Add process name normalization coverage
  - c46c62f Add passive capture result summary fields
  - 0cd6f03 Add passive capture serialization contract tests

## Proven behavior

- capture passive and capture plan support --intervention-wait-ms and --intervention-poll-ms.
- PID restart fallback works when both --pid and --process are supplied.
- Recovery refreshes regions and modules after the replacement process is found.
- Recovery retries the interrupted sample when the process instance changes.
- Dotted process names are preserved during Windows process lookup; only .exe is stripped.
- Interrupted captures write intervention_handoff.json and expose handoff_path in CLI JSON output.
- Partial interrupted sessions include intervention_handoff.json in checksums.json when snapshots exist.
- Selected-region unreadable cases are distinguished from process disappearance.
- intervention_handoff.json includes reason-specific recommended_next_action and region_read_failures[].
- PassiveCaptureResult now includes summary fields: status, samples_requested, samples_attempted, interruption_reason, and region_read_failure_count.
- Serialization contract tests pin PassiveCaptureResult optional/null behavior and CaptureInterventionHandoff read-failure fields.

## Validation evidence

Latest serialization contract proof:

    dotnet test RiftScan.slnx --configuration Release --filter FullyQualifiedName~PassiveCaptureSerializationContractTests
    # Passed: 3/3

Latest process integration proof:

    dotnet test RiftScan.slnx --configuration Release --filter FullyQualifiedName~PassiveCaptureProcessIntegrationTests
    # Passed: 2/2

Latest full suite:

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 46/46

Other checks run in this lane:

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Disposable-process proof is strong process-lifecycle evidence, but it is not a live RIFT claim.
- PassiveCaptureResult has tested fields but no explicit result_schema_version yet.

## Resume here

1. Confirm clean state: git status --short --branch
2. If staying offline, next smallest code step is adding result_schema_version or richer interrupted capture report output.
3. If moving live, run one low-pressure RIFT passive capture with --pid and --process rift_x64, force/observe process disappearance or restart, and preserve resulting session artifacts.
4. Do not claim live RIFT recovery until that live session artifact exists.

## Suggested next commit if continuing offline

Add passive capture result schema version
