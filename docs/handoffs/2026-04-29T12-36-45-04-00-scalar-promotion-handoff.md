# RiftScan handoff - scalar promotion milestone

Created: 2026-04-29T12:36:45-04:00
Repo: C:\RIFT MODDING\Riftscan
Branch/worktree state: uncommitted working tree with scalar recovery + scalar promotion milestone changes.

## TL;DR

The current lane is no longer just scalar evidence aggregation. The repo now has a validated offline pipeline for:

1. dual-lane scalar evidence set generation,
2. scalar truth candidate export,
3. repeated scalar truth recovery,
4. external/addon corroboration JSONL verification,
5. scalar truth promotion review,
6. capability status promotion to `corroborated_candidate`.

The latest validated smoke proof shows actor yaw and camera orientation can be recovered together from repeated scalar truth packets and then promoted together when external corroboration matches.

This is still not a final live RIFT truth claim. `corroborated_candidate` means repeated recovery plus matching external/addon corroboration, with manual review still required before declaring final recovered truth.

## User strategy directive captured

The user explicitly rejected slow micro-step cadence and directed a stronger strategy:

- Work in larger coherent milestone blocks.
- Test at block boundaries, not after every tiny edit.
- Keep pushing toward a superior scanner, not merely parity with RiftReader.
- Missing feature set is unacceptable if it blocks fastest truth discovery.
- Heuristics must be strong, evidence-backed, and machine-verifiable.
- Do not claim truth without artifacts.

This was documented in:

- `C:\RIFT MODDING\Riftscan\docs\agent-execution-workflow.md`

## Current coded capabilities added in this milestone

### Scalar truth recovery capability readiness

Implemented before this handoff:

- `riftscan verify scalar-truth-recovery <scalar-truth-recovery.json>`
- `riftscan report capability --scalar-truth-recovery <path>`
- Capability components can be promoted to `recovered_candidate`.

### Corroboration-aware scalar promotion

Implemented in the latest block:

- `riftscan compare scalar-promotion <scalar-truth-recovery.json> --corroboration <scalar_truth_corroboration.jsonl> --out <scalar-truth-promotion.json>`
- `riftscan verify scalar-truth-promotion <scalar-truth-promotion.json>`
- `riftscan report capability --scalar-truth-promotion <path>`
- Capability components can be promoted to `corroborated_candidate`.
- Conflicts are preserved as `blocked_conflict`, not hidden.

## New/modified source files

New files:

- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Comparison\ScalarTruthPromotionResult.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Comparison\ScalarTruthPromotionService.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Comparison\ScalarTruthPromotionVerificationResult.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Comparison\ScalarTruthPromotionVerifier.cs`

Modified files:

- `C:\RIFT MODDING\Riftscan\README.md`
- `C:\RIFT MODDING\Riftscan\docs\capability-readiness-workflow.md`
- `C:\RIFT MODDING\Riftscan\docs\scalar-truth-workflow.md`
- `C:\RIFT MODDING\Riftscan\scripts\smoke-fixture.ps1`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Reports\CapabilityStatusResult.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Reports\CapabilityStatusService.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Analysis\Reports\CapabilityStatusVerifier.cs`
- `C:\RIFT MODDING\Riftscan\src\RiftScan.Cli\Program.cs`
- `C:\RIFT MODDING\Riftscan\tests\RiftScan.Tests\ComparisonOutputContractTests.cs`
- `C:\RIFT MODDING\Riftscan\tests\RiftScan.Tests\SessionComparisonServiceTests.cs`

## Key CLI workflows now available

### Verify scalar truth recovery

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json
```

### Promote recovered scalar truth with corroboration

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-promotion reports/generated/<scalar-truth-recovery>.json `
  --corroboration reports/generated/<scalar-truth-corroboration>.jsonl `
  --out reports/generated/<scalar-truth-promotion>.json
```

### Verify scalar truth promotion

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json
```

### Capability report with all current evidence packets

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability `
  --truth-readiness reports/generated/<truth-readiness>.json `
  --scalar-evidence-set reports/generated/<scalar-evidence-set>.json `
  --scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json `
  --scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json `
  --json-out reports/generated/<capability-status>.json
```

## Validation completed

### Build

```powershell
dotnet build .\RiftScan.slnx --configuration Release --no-restore
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Tests

```powershell
dotnet test .\RiftScan.slnx --configuration Release --no-restore
```

Result:

```text
Passed: 254
Failed: 0
Skipped: 0
```

### Smoke

Non-preserved smoke run passed:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-fixture.ps1 `
  -OutputRoot .\reports\generated\smoke-fixture-promotion-20260429-123215
```

Preserved smoke run passed:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-fixture.ps1 `
  -OutputRoot .\reports\generated\smoke-fixture-promotion-kept-20260429-123419 `
  -KeepOutput
```

Result:

```text
Fixture smoke passed.
Output preserved: C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419
```

### Format/checks

```powershell
dotnet format .\RiftScan.slnx --verify-no-changes --no-restore
```

Passed silently.

```powershell
git diff --check
```

Passed with only LF -> CRLF warnings.

## Preserved proof artifacts

Smoke output root:

- `C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419`

Important files:

- `C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419\reports\fixture-combined-scalar-truth-recovery.json`
- `C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419\reports\fixture-combined-scalar-truth-corroboration.jsonl`
- `C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419\reports\fixture-combined-scalar-truth-promotion.json`
- `C:\RIFT MODDING\Riftscan\reports\generated\smoke-fixture-promotion-kept-20260429-123419\reports\fixture-combined-capability-status.json`

Smoke proof outcome:

- Actor yaw recovered at base `0x60000000`, offset `0x4`.
- Camera orientation recovered at base `0x60000000`, offset `0x8`.
- Both were promoted to `corroborated_candidate` when corroboration JSONL matched.
- Capability status reported:
  - `actor_yaw.evidence_readiness = corroborated_candidate`
  - `camera_orientation.evidence_readiness = corroborated_candidate`
  - `evidence_missing = []`

## Current git state at handoff creation

Uncommitted tracked changes:

- `README.md`
- `docs/capability-readiness-workflow.md`
- `docs/scalar-truth-workflow.md`
- `scripts/smoke-fixture.ps1`
- `src/RiftScan.Analysis/Reports/CapabilityStatusResult.cs`
- `src/RiftScan.Analysis/Reports/CapabilityStatusService.cs`
- `src/RiftScan.Analysis/Reports/CapabilityStatusVerifier.cs`
- `src/RiftScan.Cli/Program.cs`
- `tests/RiftScan.Tests/ComparisonOutputContractTests.cs`
- `tests/RiftScan.Tests/SessionComparisonServiceTests.cs`

Untracked new files:

- `src/RiftScan.Analysis/Comparison/ScalarTruthPromotionResult.cs`
- `src/RiftScan.Analysis/Comparison/ScalarTruthPromotionService.cs`
- `src/RiftScan.Analysis/Comparison/ScalarTruthPromotionVerificationResult.cs`
- `src/RiftScan.Analysis/Comparison/ScalarTruthPromotionVerifier.cs`
- this handoff file

## Remaining risks

- All latest proof is offline fixture proof, not live RIFT proof.
- `corroborated_candidate` is intentionally not final truth.
- There is not yet a final manual promotion review packet that says ready/blocked/needs-more-evidence in a single decision artifact.
- Promotion report is JSON only; there is no human-friendly markdown promotion report yet.

## Recommended next coherent block

Build a **manual promotion review packet** and verifier.

Goal: convert `scalar-truth-promotion.json` into a final review artifact with explicit decision states:

- `ready_for_manual_truth_review`
- `blocked_conflict`
- `needs_more_corroboration`
- `needs_repeat_capture`
- `do_not_promote`

Proposed CLI:

```powershell
riftscan review scalar-promotion <scalar-truth-promotion.json> --out reports/generated/<promotion-review>.json
riftscan verify scalar-promotion-review <promotion-review>.json
```

Acceptance criteria:

1. Review packet verifies by schema and invariant checks.
2. Conflicts cannot be hidden or silently promoted.
3. Capability status can reference review packets later if needed.
4. Smoke proves happy path and conflict path.
5. Manual-review warning remains present until live/manual final truth is explicitly confirmed.

## Resume prompt

Continue in `C:\RIFT MODDING\Riftscan`. Read the newest handoff only:

`C:\RIFT MODDING\Riftscan\docs\handoffs\2026-04-29T12-36-45-04-00-scalar-promotion-handoff.md`

Then implement the next coherent block: manual scalar promotion review packet + verifier + CLI + docs + smoke/test coverage. Keep the larger-block strategy, do not micro-step, and do not claim final truth without artifact evidence.
