# RiftScan commit-ready summary

Timestamp: 2026-04-29 05:40 America/New_York
Repository: `C:\RIFT MODDING\Riftscan`

## Commit status

This milestone is ready for human review and commit, but no commit has been made by this handoff.

## Suggested commit message

```text
Build scanner readiness and truth-evidence workflow

- add windowed capture-plan fanout for targeted read-only followups
- add scalar lane analysis and vec3/scalar behavior heuristics
- add entity-layout candidates and cross-session layout comparison
- add scalar evidence aggregation, truth export, recovery, and corroboration checks
- add comparison truth-readiness export and verifier
- add capability status export and verifier
- document reproducible readiness/capability workflows and milestone handoffs
- add helper script to verify readiness workflow end to end
```

## Review order

1. Capture fanout and live-read behavior:
   - `src/RiftScan.Capture/Passive/PassiveCaptureService.cs`
   - `src/RiftScan.Capture/Passive/PassiveCapturePlanService.cs`
   - `src/RiftScan.Capture/Passive/PassiveCaptureOptions.cs`
   - `src/RiftScan.Capture/Passive/PassiveCapturePlanOptions.cs`
2. Offline analyzers and artifact verification:
   - `src/RiftScan.Analysis/Entities/`
   - `src/RiftScan.Analysis/Scalars/`
   - `src/RiftScan.Analysis/Triage/DynamicRegionTriageAnalyzer.cs`
   - `src/RiftScan.Core/Sessions/SessionVerifier.cs`
3. Comparison and heuristics:
   - `src/RiftScan.Analysis/Comparison/`
4. Reporting and capability/readiness status:
   - `src/RiftScan.Analysis/Reports/`
   - `src/RiftScan.Cli/Program.cs`
5. Operator helper script:
   - `scripts/verify-readiness-workflow.ps1`
6. Tests and docs:
   - `tests/RiftScan.Tests/`
   - `README.md`
   - `docs/`

## Validation evidence

Latest completed validation:

```powershell
dotnet test .\RiftScan.slnx --configuration Release
dotnet format .\RiftScan.slnx --verify-no-changes --no-restore
git diff --check
```

Results:

- Full test suite: `229/229 passed`
- Format check: passed
- Diff check: passed with LF-to-CRLF warnings only
- Fixture smoke: passed at `reports/generated/smoke-fixture-capability-scalar-20260429-094228`

Additional artifact verification:

```powershell
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- verify comparison-readiness .\reports\generated\entity-layout-compare-20260429-051819.truth-readiness.json

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- verify capability-status .\reports\generated\capability-status-20260429-052612.json
```

Results:

- comparison readiness verification: `success=true`, `issues=[]`
- capability status verification: `success=true`, `issues=[]`; current code includes the new `scalar_evidence_set_verify` capability.
- readiness workflow helper smoke: `success=true`
- capability status now accepts optional `--scalar-evidence-set` so the multi-session scalar evidence set can update actor-yaw/camera readiness.

## Current evidence truth

From verified readiness/capability packets:

- `entity_layout`: `strong_candidate`
- `position`: `candidate_needs_labeled_contrast`
- `actor_yaw`: `missing`
- `camera_orientation`: `missing`

Do not claim recovered truth yet.

## Next proof action after commit

Run the read-only move-forward followup from:

- `reports/generated/entity-layout-compare-20260429-051819.next-capture-plan.json`

Then regenerate:

- comparison JSON/MD
- next-capture plan
- truth-readiness JSON
- capability-status JSON
- both verifier outputs

## Related handoffs

- `docs/handoffs/2026-04-29-052900-riftscan-scanner-readiness-milestone.md`
- `docs/handoffs/2026-04-29-053500-riftscan-changed-file-inventory.md`

## Safety notes

- Do not reset or clean the working tree before reviewing untracked files.
- Do not add input/window control to RiftScan core.
- Live capture remains external read-only observation only.
- Readiness/capability packets are evidence status, not recovered truth.

