# RiftScan handoff - analyzer artifact schema versions

created_local: 2026-04-28 20:30:53 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 260a658 Add schema versions to analyzer artifacts
supersedes: docs/handoffs/2026-04-28-202846-riftscan-generated-comparison-json-and-sources.md

## TL;DR

Analyzer-generated artifacts now carry schema versions on top-level records. This makes JSON/JSONL artifact shape explicit and easier to migrate later.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 260a658 Add schema versions to analyzer artifacts

## Proven behavior

- Added schema_version to:
  - RegionTriageEntry
  - NextCapturePlan
  - RegionDeltaEntry
  - TypedValueCandidate
  - StructureCandidate
  - Vec3Candidate
  - StructureCluster
- Added nalyzer_version to NextCapturePlan for parity with other analyzer outputs.
- Analyzer output contract tests pin the new schema fields.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~AnalyzerOutputContractTests|FullyQualifiedName~SessionAnalysisAndReportTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 16/16

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 64/64

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Schema fields improve artifact contract/migration readiness, not truth validation.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next useful step is adding generated artifact fixture tests that parse actual JSONL outputs and verify schema_version fields end-to-end.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add generated analyzer schema fixture checks
