# Live RIFT passive capture restart validation checklist

purpose: prove_or_reject_live_rift_restart_recovery_for_passive_capture
scope: validation_plan_only_no_live_action_by_this_document
last_updated_local: 2026-04-28

## Safety boundary

- This checklist does not authorize live input, game-window focus, clicks, keys, launcher handling, credentials, or memory writes.
- RiftScan validation remains read-only process observation plus stored artifact review.
- Do not claim live RIFT restart recovery until a session artifact from this checklist exists and verifies.

## Preconditions

- RIFT is already running and logged in by the user.
- User explicitly approves live process observation for this validation run.
- Use both PID and process name so restart fallback can be exercised.
- Choose a new empty session directory under sessions/.
- Keep sample pressure low: small sample count, modest interval, limited region count, no full dump.

## Identify target process

Use PowerShell process lookup only; do not interact with the game window:

    Get-Process -Name rift_x64 -ErrorAction Stop | Select-Object Id,ProcessName,StartTime,Path

If multiple RIFT processes exist, stop and choose the intended PID explicitly before capture.

## Capture command template

Replace <pid> and <session_id> before running:

    dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release -- capture passive --pid <pid> --process rift_x64 --out sessions/<session_id> --samples 3 --interval-ms 5000 --max-regions 4 --max-bytes-per-region 1048576 --max-total-bytes 12582912 --intervention-wait-ms 300000 --intervention-poll-ms 1000

Expected behavior without restart: capture completes, success true, status complete, no handoff_path.

## Restart/recovery observation

- Start capture and wait until at least one snapshot file exists under sessions/<session_id>/snapshots/.
- Only after user approval, cause/observe RIFT process disappearance or restart outside RiftScan.
- RiftScan should wait, find the replacement process by process name, refresh regions/modules, and continue.
- If selected regions become unreadable while process remains available, expected result is an interrupted handoff, not a restart claim.

## Required artifact checks

After capture exits, run:

    dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release -- verify session sessions/<session_id>
    dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release -- report session sessions/<session_id> --top 20

Preserve these artifacts:

- sessions/<session_id>/manifest.json
- sessions/<session_id>/regions.json
- sessions/<session_id>/modules.json
- sessions/<session_id>/checksums.json
- sessions/<session_id>/snapshots/index.jsonl
- sessions/<session_id>/intervention_handoff.json if present
- sessions/<session_id>/report.md

## Pass criteria

- verify session returns success true.
- If restart recovery was exercised, output JSON has success true and no handoff_path.
- manifest process_id should reflect the restored process if recovery happened after restart.
- report.md exists and captures status clearly.

## Non-pass but useful outcomes

- success false with intervention_handoff.json reason intervention_wait_timed_out: process did not return or name lookup failed.
- success false with selected_regions_unreadable: process remained available but followed regions were no longer readable; review region_read_failures and capture from a fresh plan.
- verifier failure: preserve artifacts and fix integrity issue before retrying.

## Completion note template

Record the result in a handoff/status note with:

- session_id
- exact command
- original PID and restored PID if applicable
- verify result
- report path
- whether this is live RIFT recovery proof or only a failed/useful observation
