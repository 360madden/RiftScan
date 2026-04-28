# RiftScan handoff - capture intervention safety

created_local: 2026-04-28 18:31:24 -04:00
repo: C:\RIFT MODDING\Riftscan
branch_state: uncommitted_worktree_changes_present
scope: passive capture crash/intervention handling

## TL;DR

RiftScan now has an in-progress, validated implementation for passive capture interruption handling: if capture reads stop, it waits for user/game recovery, resumes if the process returns, or writes `intervention_handoff.json` and stops after the configured timeout. Build, tests, format, and diff-check passed before this handoff was written.

## Current uncommitted work

Files modified:

- `src/RiftScan.Capture/Passive/PassiveCaptureOptions.cs`
- `src/RiftScan.Capture/Passive/PassiveCapturePlanOptions.cs`
- `src/RiftScan.Capture/Passive/PassiveCapturePlanService.cs`
- `src/RiftScan.Capture/Passive/PassiveCaptureService.cs`
- `src/RiftScan.Cli/Program.cs`
- `tests/RiftScan.Tests/PassiveCaptureServiceTests.cs`

Files added:

- `src/RiftScan.Capture/Passive/CaptureInterventionHandoff.cs`
- `docs/handoffs/2026-04-28-183124-riftscan-capture-intervention-handoff.md`

## Implemented behavior

- Added capture options:
  - `InterventionWaitMilliseconds`, default `1200000` (20 minutes)
  - `InterventionPollIntervalMilliseconds`, default `2000`
- Added CLI flags:
  - `--intervention-wait-ms`
  - `--intervention-poll-ms`
- `capture plan` now forwards these values into passive capture.
- Passive capture now:
  - treats a sample with zero successful reads as a likely interruption/crash condition
  - waits/polls for process availability
  - resumes capture if the process becomes available again
  - stops with `Success = false` if timeout is reached
  - writes `intervention_handoff.json` on timeout
  - marks partial sessions as manifest `status = "interrupted"`
- No live RIFT interaction or input control was added.

## Validation already run

Commands run from `C:\RIFT MODDING\Riftscan`:

```powershell
dotnet build RiftScan.slnx --configuration Release
dotnet test RiftScan.slnx --configuration Release
dotnet format --verify-no-changes
git diff --check
```

Results:

- Build: passed
- Tests: passed, 36/36
- Format check: passed
- `git diff --check`: passed

Note: an earlier `dotnet test --no-build` failed because tests were changed after the previous build. Full `dotnet test RiftScan.slnx --configuration Release` was then run and passed.

## Important behavior notes

- Current wait/resume logic works best when capture is started by process name (`--process rift_x64`).
- If capture is started by PID only and the game restarts under a new PID, auto-resolve by name cannot happen unless name fallback is added.
- If every selected memory region becomes unreadable for a reason other than process crash, the same intervention wait path is triggered. This is intentional for now, but future diagnostics should distinguish process absence vs region-read exhaustion.

## Known risks / gaps

1. `--pid` restart recovery is limited because the restarted process usually has a different PID.
2. Handoff artifact is not yet included in `checksums.json` for partial interrupted sessions with snapshots.
3. CLI result currently reports artifacts, but does not emit a dedicated `handoff_path` field.
4. No end-to-end CLI test covers `--intervention-wait-ms` parsing yet.
5. No live RIFT crash/restart test has been run.

## Resume steps

1. Inspect current worktree:
   ```powershell
   git status --short
   ```
2. Re-run validation if anything changed:
   ```powershell
   dotnet test RiftScan.slnx --configuration Release
   dotnet format --verify-no-changes
   git diff --check
   ```
3. Next smallest code improvement:
   - add optional process-name fallback for PID captures when both `--pid` and `--process` are supplied, so a restarted game can be found under a new PID.
4. Then add a CLI parsing test or direct command-level smoke test for the intervention flags.
5. Commit only after user explicitly asks or after confirming desired commit scope.

## Suggested commit message when approved

```text
Add passive capture intervention timeout handoff
```

## Stop condition

Do not claim live crash recovery is proven until a real RIFT process disappearance/restart path has been tested or an equivalent controlled process integration test exists.
