# RiftScan handoff - candidate confidence defaults

created_local: 2026-04-28 20:05:10 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 9491c4e Add candidate confidence defaults
supersedes: docs/handoffs/2026-04-28-200306-riftscan-analyzer-output-contracts.md

## TL;DR

Candidate outputs now carry basic alidation_status and confidence_level fields where they were missing. This moves value, structure, and vec3 candidate JSON closer to the project candidate contract without changing live capture behavior.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 9491c4e Add candidate confidence defaults

## Proven behavior

- TypedValueCandidate serializes alidation_status and confidence_level.
- StructureCandidate serializes alidation_status and confidence_level.
- Vec3Candidate serializes confidence_level alongside its existing alidation_status.
- Typed value, structure, and vec3 analyzers assign confidence from score thresholds:
  - high: score >= 75
  - medium: score >= 50
  - low: score < 50
- Analyzer output contract tests now pin those fields.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~AnalyzerOutputContractTests|FullyQualifiedName~TypedValueLaneAnalyzerTests|FullyQualifiedName~StructureAnalyzerTests|FullyQualifiedName~Vec3CandidateAnalyzerTests|FullyQualifiedName~KnownSystemRegionClassifierTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 16/16

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 54/54

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Confidence is still a simple score-threshold label, not cross-session validation.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding xplanation_short to candidate outputs so each candidate has a compact machine-readable reason.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate explanation_short fields
