# RiftScan handoff - CI validation

created_local: 2026-04-28 21:32:27 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
latest_commit: 29c01dc Add CI validation workflow

## TL;DR

RiftScan now has a GitHub Actions CI workflow on main and pull requests. The workflow runs restore, build, tests, format check, fixture smoke, and migration smoke on Windows.

## Changed

- Added .github/workflows/ci.yml.
- CI steps:
  - checkout
  - setup .NET 10.0.x
  - dotnet restore RiftScan.slnx
  - dotnet build RiftScan.slnx --configuration Release --no-restore
  - dotnet test RiftScan.slnx --configuration Release --no-build
  - dotnet format RiftScan.slnx --verify-no-changes --no-restore
  - scripts/smoke-fixture.ps1
  - scripts/smoke-migration.ps1

## Validation

Local validation before push:

- dotnet restore RiftScan.slnx
- dotnet build RiftScan.slnx --configuration Release --no-restore
- dotnet test RiftScan.slnx --configuration Release --no-build
  - Passed: 95/95
- dotnet format RiftScan.slnx --verify-no-changes --no-restore
- scripts/smoke-fixture.ps1
- scripts/smoke-migration.ps1
- git diff --check

Remote CI validation:

- GitHub Actions run: https://github.com/360madden/RiftScan/actions/runs/25086447666
- Result: success

## Resume here

Best next result-producing move: add a small CI badge to README or move to the next offline analyzer/reporting feature now that migration and CI are sealed.