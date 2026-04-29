# RiftScan handoff - comparison candidate summaries

created_local: 2026-04-28 20:18:46 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: bb0bfa4 Surface candidate summaries in comparisons
supersedes: docs/handoffs/2026-04-28-201559-riftscan-candidate-value-sequence-summary.md

## TL;DR

Comparison outputs now surface candidate traceability: structure comparisons include candidate ids, and structure/value/vec3 comparisons include value sequence summaries. The markdown comparison report also shows candidate ids/summaries and includes a typed-value match table.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - bb0bfa4 Surface candidate summaries in comparisons

## Proven behavior

- StructureCandidateComparison includes session_a_candidate_id, session_b_candidate_id, and both value sequence summaries.
- ValueCandidateComparison includes both value sequence summaries.
- Vec3CandidateComparison includes both value sequence summaries.
- Comparison markdown reports include candidate ids and summaries for vec3 and structure matches.
- Comparison markdown reports now include a Top typed value matches table.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter FullyQualifiedName~SessionComparisonServiceTests
    # Passed: 4/4

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 54/54

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Comparison matches remain candidate evidence, not recovered truth claims.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding nalyzer_sources to comparison result records or pinning comparison JSON field contracts.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Pin comparison JSON contract fields
