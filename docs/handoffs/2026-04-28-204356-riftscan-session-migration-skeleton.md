# RiftScan handoff - session migration skeleton

created_local: 2026-04-28 20:43:56 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main_before_this_handoff
latest_commit: af2c481 Add session migration command skeleton
supersedes: docs/handoffs/2026-04-28-203820-riftscan-command-and-plan-schema-contracts.md

## TL;DR

A minimal offline `riftscan migrate session` skeleton now exists. It validates a session first, supports current-schema no-op migration to `riftscan.session.v1`, rejects unsupported source/target schemas with machine-readable issues, and writes no artifacts.

## Current truth

- Branch was clean and synced with `origin/main` immediately before this handoff file was created.
- Latest pushed implementation commit: `af2c481 Add session migration command skeleton`.
- New command usage:

    riftscan migrate session <session-path> --to-schema riftscan.session.v1 [--dry-run]

- Current implementation is intentionally a schema-safety placeholder, not a mutating migrator.

## Files changed by the implementation commit

- `src/RiftScan.Core/Sessions/SessionMigrationResult.cs`
  - Adds `result_schema_version = riftscan.session_migration_result.v1`.
  - Emits success, session path/id, source/target schema, dry-run flag, status, artifacts written, and issues.
- `src/RiftScan.Core/Sessions/SessionMigrationService.cs`
  - Verifies sessions before migration decisions.
  - Supports no-op current schema only: `riftscan.session.v1` -> `riftscan.session.v1`.
  - Rejects unsupported source or target schema versions.
  - Preserves raw evidence by writing no artifacts.
- `src/RiftScan.Cli/Program.cs`
  - Wires `migrate session` command and help text.
- `tests/RiftScan.Tests/SessionMigrationServiceTests.cs`
  - Covers current-schema no-op, unsupported target schema, and CLI JSON output.
- `tests/RiftScan.Tests/CommandResultContractTests.cs`
  - Pins migration result JSON field set and schema version.

## Proven behavior

Fixture command:

    dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- migrate session tests/RiftScan.Tests/Fixtures/valid-session --to-schema riftscan.session.v1

Returned:

- `result_schema_version`: `riftscan.session_migration_result.v1`
- `success`: `true`
- `from_schema_version`: `riftscan.session.v1`
- `to_schema_version`: `riftscan.session.v1`
- `dry_run`: `true`
- `status`: `noop_current_schema`
- `artifacts_written`: `[]`

## Validation evidence

    dotnet test --configuration Release --filter "FullyQualifiedName~SessionMigrationServiceTests|FullyQualifiedName~CommandResultContractTests"
    # Passed: 8/8

    dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- migrate session tests/RiftScan.Tests/Fixtures/valid-session --to-schema riftscan.session.v1
    # success true, noop_current_schema

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 76/76

    dotnet format RiftScan.slnx --verify-no-changes
    # Passed

    git diff --check
    # Passed

    dotnet test RiftScan.slnx --configuration Release --no-build --filter "FullyQualifiedName~SessionMigrationServiceTests|FullyQualifiedName~CommandResultContractTests"
    # Passed: 8/8 after commit

    git push
    # Pushed af2c481 to origin/main

## Still not proven

- No live RIFT capture or restart validation has been run in this slice.
- Migration does not yet rewrite or upgrade older schemas; it only verifies and reports no-op/current-schema readiness.
- There is no `--apply` path yet because no real schema migration exists.

## Resume here

1. Confirm clean state: `git status --short --branch`.
2. If staying offline, add a fixture-only command-contract test that exercises CLI help/unknown migrate options.
3. If preparing future schema bumps, add a migration plan artifact format before any mutating migration.
4. If moving live, use `docs/validation/live-rift-passive-capture-restart-checklist.md`; do not claim live recovery without a verified live session artifact.

## Suggested next commit if continuing offline

Add migration CLI edge-case tests
