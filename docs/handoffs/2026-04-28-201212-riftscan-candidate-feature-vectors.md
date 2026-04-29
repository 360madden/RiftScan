# RiftScan handoff - candidate feature vectors

created_local: 2026-04-28 20:12:12 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 3ad9253 Add candidate feature vectors
supersedes: docs/handoffs/2026-04-28-200948-riftscan-candidate-score-breakdowns.md

## TL;DR

Candidate outputs now include machine-readable eature_vector fields for typed value, structure, and vec3 candidates. This preserves the ranking/comparison inputs next to the candidate records.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 3ad9253 Add candidate feature vectors

## Proven behavior

- TypedValueCandidate.feature_vector includes sample count, distinct count, changed count, change ratio, distinct ratio, and type bonus.
- StructureCandidate.feature_vector includes snapshot support, snapshot count, support ratio, component count, and nonzero component count.
- Vec3Candidate.feature_vector includes snapshot support, sample value count, value delta magnitude, and behavior score.
- Analyzer output contract tests pin eature_vector on typed value, structure, and vec3 candidate JSON.

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
- Feature vectors expose current analyzer inputs; they are not cross-session validation by themselves.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding nalyzer_sources to candidate outputs so each candidate names its upstream artifact dependencies.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate analyzer_sources fields
