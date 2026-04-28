# RiftScan handoff - passive capture intervention resume

created_local: 2026-04-28 19:18:49 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: ce67941 Tailor passive capture handoff actions
supersedes: docs/handoffs/2026-04-28-183124-riftscan-capture-intervention-handoff.md

## TL;DR

Passive capture interruption handling is now implemented, tested, committed, and pushed. The old handoff known gaps for PID restart fallback, handoff checksums, CLI handoff path, and CLI intervention flag parsing are closed. Live RIFT restart/crash validation is still not proven.

## Current truth

- Branch is clean and synced with origin/main.
- Latest pushed intervention commits:
  - 35f73cb Add passive capture intervention handoff
  - 4f63281 Add handoff path and restart recovery for passive capture
  - d916ee6 Add CLI coverage for capture intervention flags
  - 766fac5 Distinguish unreadable regions during passive capture
  - 8105b3f Add passive capture read failure diagnostics
  - ce67941 Tailor passive capture handoff actions

## Implemented behavior

- capture passive and capture plan support --intervention-wait-ms and --intervention-poll-ms.
- If reads stop, passive capture now:
  - checks whether the target process is still available
  - waits for process return/restart when the process disappears
  - resumes with --pid + --process by falling back to process name when the old PID is gone
  - refreshes regions and modules after recovery
  - retries the interrupted sample when recovery finds a different process instance
  - stops with a specific handoff when selected regions remain unreadable while the process is still present
- Interrupted captures write intervention_handoff.json and expose handoff_path in CLI JSON output.
- Partial interrupted sessions include the handoff in checksums.json when snapshots exist.
- intervention_handoff.json now includes reason-specific recommended_next_action and region_read_failures[].

## Validation evidence

Most recent validation in this lane:

    dotnet test RiftScan.slnx --configuration Release --filter FullyQualifiedName~PassiveCaptureServiceTests
    # Passed: 16/16

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

Full suite was also run during the lane:

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 41/41

## Still not proven

- No live RIFT restart/crash validation has been run.
- No disposable-process integration test exists yet for actual PID disappearance/restart.
- Current coverage is unit/CLI smoke coverage, not live process lifecycle proof.

## Resume here

1. Confirm clean state: git status --short --branch
2. Next smallest offline code step: add a disposable-process integration test for PID disappearance/restart, or add richer interrupted-session summary metrics if integration testing is deferred.
3. Before any live RIFT claim, run a low-pressure real capture/restart validation and preserve the resulting session artifacts.

## Suggested next commit if continuing offline

Add process restart integration coverage for passive capture
