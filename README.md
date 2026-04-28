# RiftScan

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

## Validation

```powershell
dotnet build RiftScan.slnx --configuration Release --no-restore
dotnet test RiftScan.slnx --configuration Release --no-build
dotnet format RiftScan.slnx --verify-no-changes
git diff --check
```
