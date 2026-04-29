# RiftScan

[![ci](https://github.com/360madden/RiftScan/actions/workflows/ci.yml/badge.svg)](https://github.com/360madden/RiftScan/actions/workflows/ci.yml)

Read-only RIFT memory discovery pipeline: capture once, analyze offline, compare sessions, then generate focused follow-up capture plans.

## Current working loop

Use PowerShell from the repository root.

```powershell
dotnet build RiftScan.slnx --configuration Release

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture passive --pid <rift_pid> --out sessions/<passive_id> --samples 3 --interval-ms 100 --stimulus passive_idle

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<passive_id> --top 100

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare sessions sessions/<baseline_id> sessions/<passive_id> --top 10 `
  --out reports/generated/<compare>.json `
  --report-md reports/generated/<compare>.md `
  --next-plan reports/generated/<next-plan>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<followup_id> --samples 3 --interval-ms 100 --stimulus move_forward
```

## Guardrails

- Capture is external read-only observation only.
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
session inventory sessions/<session_id> `
  --json-out reports/generated/<session_id>-inventory.json
```

Use prune dry-run to list generated artifacts that could be cleaned later. It does not delete files.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  session prune sessions/<session_id> --dry-run `
  --json-out reports/generated/<session_id>-prune-inventory.json
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
