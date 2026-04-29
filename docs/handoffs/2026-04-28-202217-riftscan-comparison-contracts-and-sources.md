# RiftScan handoff - comparison contracts and analyzer sources

created_local: 2026-04-28 20:22:17 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: ca0e7e4 Add analyzer sources to comparisons
supersedes: docs/handoffs/2026-04-28-201846-riftscan-comparison-candidate-summaries.md

## TL;DR

Comparison JSON contracts are pinned, and candidate comparison records now carry analyzer source lists from their matched candidates. This makes comparison output drift visible and preserves artifact dependency traceability across sessions.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commits after the previous handoff:
  - fa366b2 Pin comparison JSON contracts
  - ca0e7e4 Add analyzer sources to comparisons
- New contract test file: 	ests/RiftScan.Tests/ComparisonOutputContractTests.cs.

## Proven behavior

- JSON field sets are pinned for:
  - SessionComparisonResult
  - RegionComparison
  - ClusterComparison
  - StructureCandidateComparison
  - Vec3CandidateComparison
  - ValueCandidateComparison
  - Vec3BehaviorSummary
  - ComparisonNextCapturePlan
  - ComparisonCaptureTarget
- Structure, vec3, and value candidate comparison records now include session A/B analyzer source lists.
- Comparison service tests verify source propagation from candidate artifacts.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~ComparisonOutputContractTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 13/13

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 63/63

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Comparison source propagation improves traceability, not semantic truth validation.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding comparison report rendering for analyzer sources only where it helps review, or adding end-to-end generated comparison JSON fixture tests.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add generated comparison JSON fixture coverage
