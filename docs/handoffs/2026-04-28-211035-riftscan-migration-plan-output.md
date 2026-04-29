# RiftScan handoff - migration plan output

created_local: 2026-04-28 21:10:35 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
latest_commit: 2578c4b Add session migration plan output

## TL;DR

iftscan migrate session now has an explicit non-mutating plan artifact path via --plan-out. This keeps the migration lane moving toward real schema upgrades while preserving raw session evidence.

## Changed

- Added SessionMigrationPlan / SessionMigrationPlanAction JSON contract.
- Added CLI support:

    riftscan migrate session <session-path> --to-schema riftscan.session.v1 [--dry-run] [--plan-out reports/generated/migration-plan.json]

- Added direct command help for:

    riftscan migrate session --help

- Added tests for:
  - migration plan JSON field contract
  - migration plan action JSON field contract
  - service plan-out writes a machine-readable non-mutating plan
  - CLI plan-out reports the written artifact
  - CLI migrate help
  - CLI unknown migrate option machine-readable error

## Validation

- dotnet test --configuration Release --filter "FullyQualifiedName~SessionMigrationServiceTests|FullyQualifiedName~CommandResultContractTests"
  - Passed: 14/14
- dotnet test RiftScan.slnx --configuration Release --no-build
  - Passed: 82/82
- dotnet format RiftScan.slnx --verify-no-changes
  - Passed
- git diff --check
  - Passed
- git push
  - Pushed latest commit to origin/main

## Still not proven

- No live RIFT capture/restart validation in this slice.
- No real source schema upgrade exists yet.
- can_apply is intentionally false because only current-schema noop planning exists.

## Resume here

1. Add first real legacy fixture/session-schema downgrade fixture if an older schema target is known.
2. Implement one explicit old-schema-to-current migration in dry-run plan first.
3. Only after the dry-run plan is contract-tested, add an apply path that writes generated migrated artifacts without mutating raw evidence.
