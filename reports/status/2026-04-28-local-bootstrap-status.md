# RiftScan Local Bootstrap Status - 2026-04-28

## TL;DR

`360madden/RiftScan` is cloned locally at `C:\RIFT MODDING\Riftscan` on `main`, tracking `origin/main`. The first validated .NET 10 foundation slice now exists: solution/projects, core session schema models, fixture-backed `verify session`, and unit tests.

## Current repository state

- Local path: `C:\RIFT MODDING\Riftscan`
- GitHub remote: `https://github.com/360madden/RiftScan.git`
- Branch: `main`
- Latest upstream commits inspected before local work:
  - `526f41b Add project foundation files`
  - `979069a Add RiftScan AGENTS contract`
  - `27d2b8f Initial commit`

## Foundation slice created locally

- Solution: `RiftScan.slnx`
- Projects:
  - `src/RiftScan.Core`
  - `src/RiftScan.Capture`
  - `src/RiftScan.Analysis`
  - `src/RiftScan.Rift`
  - `src/RiftScan.Cli`
  - `tests/RiftScan.Tests`
- Core schema/verification:
  - `SessionManifest`
  - `RegionMap`
  - `ModuleMap`
  - `SnapshotIndexEntry`
  - `ChecksumManifest`
  - `SessionVerifier`
  - `SessionVerificationResult`
- Fixture session:
  - `tests/RiftScan.Tests/Fixtures/valid-session`
- CLI path:
  - `riftscan verify session <session-path>`

## AGENTS.md operating rules carried forward

- Project target: standalone read-only behavioral memory discovery engine for RIFT MMO memory analysis.
- Core boundary: external read-only observation and offline analysis only.
- Do not add game input, launcher automation, memory writes, injection, or one-off scanner logic to core.
- First implementation order now has items 1-4 represented by a minimal validated slice:
  1. create solution and projects
  2. define session schema models
  3. implement verify command for schema/checksums
  4. add fixture session tests not requiring RIFT

## Validation run

- `dotnet restore RiftScan.slnx` - passed
- `dotnet build RiftScan.slnx --configuration Release --no-restore` - passed, 0 warnings, 0 errors
- `dotnet test RiftScan.slnx --configuration Release --no-build` - passed, 5 tests
- `dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- verify session tests/RiftScan.Tests/Fixtures/valid-session` - passed, `success: true`
- `dotnet format RiftScan.slnx --verify-no-changes` - passed

## Smallest next implementation step

Add the next fixture-only vertical slice before live capture: region filtering and snapshot index roundtrip tests, then expand `verify session` only where those tests require it.
