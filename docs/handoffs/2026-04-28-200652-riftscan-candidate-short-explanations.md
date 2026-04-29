# RiftScan handoff - candidate short explanations

created_local: 2026-04-28 20:06:52 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: e99240b Add candidate short explanations
supersedes: docs/handoffs/2026-04-28-200510-riftscan-candidate-confidence-defaults.md

## TL;DR

Candidate outputs now include compact xplanation_short fields for typed value, structure, and vec3 candidates. These are pinned by analyzer output contract tests.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - e99240b Add candidate short explanations

## Proven behavior

- TypedValueCandidate.explanation_short summarizes lane type and adjacent-pair changes.
- StructureCandidate.explanation_short summarizes finite triplet support across snapshots.
- Vec3Candidate.explanation_short mirrors the behavior recommendation for compact machine reuse.
- Analyzer output contract tests pin xplanation_short on typed value, structure, and vec3 candidate JSON.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~AnalyzerOutputContractTests|FullyQualifiedName~TypedValueLaneAnalyzerTests|FullyQualifiedName~StructureAnalyzerTests|FullyQualifiedName~Vec3CandidateAnalyzerTests|FullyQualifiedName~SessionAnalysisAndReportTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 19/19

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 54/54

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Candidate explanations are compact diagnostics, not recovered truth claims.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding score_breakdown defaults to candidate outputs so scoring is explainable without parsing prose.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate score_breakdown fields
