# RiftScan

[![ci](https://github.com/360madden/RiftScan/actions/workflows/ci.yml/badge.svg)](https://github.com/360madden/RiftScan/actions/workflows/ci.yml)

Read-only RIFT memory discovery pipeline: capture once, analyze offline, compare sessions, then generate focused follow-up capture plans.

## Current working loop

Use PowerShell from the repository root.

```powershell
dotnet build RiftScan.slnx --configuration Release

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  process inventory --pid <rift_pid> `
  --max-regions 8 --max-bytes-per-region 65536 --max-total-bytes 524288 `
  --json-out reports/generated/<process-inventory>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture passive --dry-run --pid <rift_pid> --out sessions/<passive_id> `
  --samples 3 --max-regions 8 --max-bytes-per-region 65536 --max-total-bytes 1572864 `
  --json-out reports/generated/<capture-dry-run>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture passive --pid <rift_pid> --out sessions/<passive_id> `
  --samples 3 --interval-ms 100 --max-regions 8 --max-bytes-per-region 65536 --max-total-bytes 1572864 `
  --stimulus passive_idle

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<passive_id> --top 100

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare sessions sessions/<baseline_id> sessions/<passive_id> --top 10 `
  --out reports/generated/<compare>.json `
  --report-md reports/generated/<compare>.md `
  --next-plan reports/generated/<next-plan>.json `
  --truth-readiness reports/generated/<compare>.truth-readiness.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify comparison-readiness reports/generated/<compare>.truth-readiness.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability `
  --truth-readiness reports/generated/<compare>.truth-readiness.json `
  --truth-readiness reports/generated/<additional-compare>.truth-readiness.json `
  --scalar-evidence-set reports/generated/<scalar-evidence-set>.json `
  --scalar-evidence-set reports/generated/<additional-scalar-evidence-set>.json `
  --scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json `
  --scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json `
  --json-out reports/generated/<capability-status>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify capability-status reports/generated/<capability-status>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-evidence-set reports/generated/<scalar-evidence-set>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-promotion reports/generated/<scalar-truth-recovery>.json `
  --corroboration reports/generated/<scalar-truth-corroboration>.jsonl `
  --out reports/generated/<scalar-truth-promotion>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  review scalar-promotion reports/generated/<scalar-truth-promotion>.json `
  --out reports/generated/<scalar-promotion-review>.json `
  --report-md reports/generated/<scalar-promotion-review>.md

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-promotion-review reports/generated/<scalar-promotion-review>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<followup_id> --samples 3 --interval-ms 100 `
  --windows-per-region 3 --stimulus move_forward
```

## Guardrails

- Capture is external read-only observation only.
- `process inventory` and `capture passive --dry-run` enumerate and plan reads without calling `ReadProcessMemory`.
- Default live capture prioritizes writable private/mapped regions and reads a capped prefix from large regions; `--max-bytes-per-region` is a read cap, not a reason to skip heap-sized regions.
- Plan follow-up can inspect multiple deterministic windows inside each selected large region with `--windows-per-region <n>` or explicit `--window-offsets 0,0x10000`.
- Analysis, reports, comparisons, and next plans replay from stored artifacts.
- Comparison output is candidate evidence, not recovered truth.
- Use explicit stimulus labels before behavior claims.
- Do not use RIFT input/window control inside RiftScan core.

## Session migration

Migration is offline and artifact-based. Dry-run planning is safe and writes a machine-readable plan without changing the source session.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  migrate session tests/RiftScan.Tests/Fixtures/valid-session-v0 `
  --to-schema riftscan.session.v1 `
  --plan-out reports/generated/v0-to-v1-migration-plan.json
```

Apply mode is only supported for `riftscan.session.v0` to `riftscan.session.v1`. It writes a separate migrated session directory, recalculates generated checksums, verifies the migrated output, and does not mutate the source session.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  migrate session tests/RiftScan.Tests/Fixtures/valid-session-v0 `
  --to-schema riftscan.session.v1 `
  --apply `
  --out sessions/<migrated_id>
```

Migration apply refuses missing `--out`, non-empty output directories, unsupported source schemas, and unsupported target schemas.

## Session cleanup inventory

Use summary to inspect a session's manifest fields and generated artifacts without running analyzers.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  session summary sessions/<session_id> `
  --json-out reports/generated/<session_id>-summary.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  session inventory sessions/<session_id> `
  --json-out reports/generated/<session_id>-inventory.json
```

Use prune dry-run to list generated artifacts that could be cleaned later. It does not delete files.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  session prune sessions/<session_id> --dry-run `
  --json-out reports/generated/<session_id>-prune-inventory.json
```

## CI smoke artifacts

CI uploads commit-scoped smoke proof artifacts with SHA256 manifests. See [docs/ci-artifacts.md](docs/ci-artifacts.md) for download and verification steps.

## Scalar truth workflow

Actor-yaw / camera-yaw scalar discovery now has a replayable workflow for passive, turn-left, turn-right, camera-only, optional corroboration, truth-candidate export, and repeat recovery. See [docs/scalar-truth-workflow.md](docs/scalar-truth-workflow.md).

## Capability and readiness workflow

The scanner can now emit and verify a machine-readable capability/status matrix that separates coded capability from missing evidence. Use it to answer "what is coded in?" without treating candidates as recovered truth. See [docs/capability-readiness-workflow.md](docs/capability-readiness-workflow.md).

Shortcut:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-readiness-workflow.ps1 `
  -TruthReadinessPath reports/generated/<compare>.truth-readiness.json `
  -ScalarEvidenceSetPath reports/generated/<scalar-evidence-set>.json `
  -CapabilityStatusPath reports/generated/<capability-status>.json
```

## Validation

`scripts/smoke-fixture.ps1` is fixture-only and does not attach to RIFT. Live capture and plan-follow-up need an explicit process ID; fake-process plan coverage runs in unit tests.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-fixture.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-migration.ps1
dotnet build RiftScan.slnx --configuration Release --no-restore
dotnet test RiftScan.slnx --configuration Release --no-build
dotnet format RiftScan.slnx --verify-no-changes
git diff --check
```
