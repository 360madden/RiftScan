# RiftScan handoff - generated comparison JSON and report sources

created_local: 2026-04-28 20:28:46 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 9f12c0d Render analyzer sources in comparison reports
supersedes: docs/handoffs/2026-04-28-202217-riftscan-comparison-contracts-and-sources.md

## TL;DR

Comparison output hardening continued: session comparison JSON now has a schema version, CLI-generated comparison JSON is parsed and checked end-to-end, and comparison markdown reports render compact analyzer source lists for candidate matches.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commits after the previous handoff:
  - 1df4547 Add comparison schema and generated JSON checks
  - 9f12c0d Render analyzer sources in comparison reports

## Proven behavior

- SessionComparisonResult serializes schema_version = riftscan.session_comparison.v1.
- CLI compare test parses generated comparison.json and verifies schema plus candidate-id/summary/source fields.
- Comparison markdown reports include a compact Sources column for vec3, structure, and typed-value matches.
- comparison.md tests verify source artifacts like snapshots/*.bin appear in report output.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~SessionComparisonServiceTests|FullyQualifiedName~ComparisonOutputContractTests"
    # Passed: 13/13

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 63/63

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Generated comparison JSON/report checks prove artifact shape, not semantic truth.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next useful step is adding schema versions to remaining generated analyzer artifacts or creating a compact fixture-generation command/test.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add analyzer artifact schema versions
