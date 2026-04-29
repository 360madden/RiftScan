# RiftScan handoff - analyzer output contracts

created_local: 2026-04-28 20:03:06 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: e911ea5 Pin analyzer output JSON contracts
supersedes: docs/handoffs/2026-04-28-195916-riftscan-report-guidance.md

## TL;DR

Analyzer output JSON contracts are now explicitly pinned by tests. Structure candidates now carry candidate_id, and report.md includes that candidate id in the Structure candidates table.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - e911ea5 Pin analyzer output JSON contracts
- New contract test file: 	ests/RiftScan.Tests/AnalyzerOutputContractTests.cs.

## Proven behavior

- Exact top-level JSON property sets are pinned for:
  - RegionTriageEntry
  - RegionDeltaEntry
  - TypedValueCandidate
  - StructureCandidate
  - Vec3Candidate
  - StructureCluster
- StructureCandidate now serializes candidate_id.
- FloatTripletStructureAnalyzer assigns deterministic ids like structure-000001 after final ranking.
- eport.md shows structure candidate ids for traceability.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~AnalyzerOutputContractTests|FullyQualifiedName~StructureAnalyzerTests|FullyQualifiedName~SessionAnalysisAndReportTests|FullyQualifiedName~Vec3CandidateAnalyzerTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 18/18

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 54/54

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Analyzer contract tests pin field names, but not deeper semantic scoring correctness.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding candidate alidation_status / confidence_level defaults to structure/value outputs so candidate records move closer to the AGENTS candidate contract.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate validation status defaults
