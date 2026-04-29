# RiftScan handoff - candidate score breakdowns

created_local: 2026-04-28 20:09:48 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 11a6e82 Add candidate score breakdowns
supersedes: docs/handoffs/2026-04-28-200652-riftscan-candidate-short-explanations.md

## TL;DR

Candidate outputs now include machine-readable score_breakdown fields for typed value, structure, and vec3 candidates. This makes score components inspectable without parsing prose or reverse-engineering analyzer math.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 11a6e82 Add candidate score breakdowns

## Proven behavior

- TypedValueCandidate.score_breakdown includes change ratio score, distinct ratio score, type bonus, pre-cap score, score cap, and score total.
- StructureCandidate.score_breakdown includes snapshot support ratio, support score, and score total.
- Vec3Candidate.score_breakdown includes source structure score, behavior score, pre-cap score, score cap, and score total.
- Analyzer output contract tests pin score_breakdown on typed value, structure, and vec3 candidate JSON.

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
- Score breakdowns explain current analyzer math; they are not cross-session truth validation.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding eature_vector fields to candidate outputs for reusable ranking/comparison inputs.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate feature_vector fields
