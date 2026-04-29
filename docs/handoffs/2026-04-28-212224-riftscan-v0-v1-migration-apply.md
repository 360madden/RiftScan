# RiftScan handoff - v0 to v1 migration apply

created_local: 2026-04-28 21:22:24 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
latest_commit: cfd1dd6 Apply v0 to v1 session migrations to output directory

## TL;DR

RiftScan now has the first real session migration path: riftscan.session.v0 to riftscan.session.v1. Dry-run emits a planned upgrade; apply writes a separate migrated output directory, rewrites only generated copies, recalculates checksums, and verifies the migrated output. Source sessions are not mutated.

## Changed

- Added dry-run support for v0 -> v1 migration plans.
- Added CLI/service support for apply output:
  - riftscan migrate session <session> --to-schema riftscan.session.v1 --apply --out <new-session-path>
- Apply is non-destructive:
  - copies source session to output directory
  - rewrites output manifest schema_version to riftscan.session.v1
  - recalculates output manifest checksum in checksums.json
  - verifies migrated output before returning success
- Apply still rejects current/unsupported schemas unless specifically supported.
- Output directory must be missing or empty.

## Validation

- dotnet test --configuration Release --filter "FullyQualifiedName~SessionMigrationServiceTests"
  - Passed: 17/17
- dotnet test RiftScan.slnx --configuration Release --no-build
  - Passed: 92/92
- dotnet format RiftScan.slnx --verify-no-changes
  - Passed
- git diff --check
  - Passed
- git push
  - Pushed cfd1dd6 to origin/main

## Latest commits

- cfd1dd6 Apply v0 to v1 session migrations to output directory
- aa789bb Plan v0 to v1 session migrations
- 949f8b7 Test blocked migration plans for unsupported sources

## Still not proven

- No live RIFT capture/restart validation in this slice.
- Migration only supports v0 -> v1 where v0 is structurally compatible except schema_version.
- No permanent checked-in v0 fixture yet; tests generate v0 fixture dynamically from valid-session and checksum it.

## Resume here

Best next result-producing move: add a permanent valid-session-v0 fixture so migration compatibility is pinned as a durable artifact, then add CLI sample docs around dry-run and apply.