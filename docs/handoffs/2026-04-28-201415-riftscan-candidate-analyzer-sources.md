# RiftScan handoff - candidate analyzer sources

created_local: 2026-04-28 20:14:15 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 0bb6c5b Add candidate analyzer sources
supersedes: docs/handoffs/2026-04-28-201212-riftscan-candidate-feature-vectors.md

## TL;DR

Candidate outputs now include nalyzer_sources so typed value, structure, and vec3 candidates name the stored artifacts they depend on.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 0bb6c5b Add candidate analyzer sources

## Proven behavior

- TypedValueCandidate.analyzer_sources includes deltas.jsonl, snapshots/index.jsonl, and snapshots/*.bin.
- StructureCandidate.analyzer_sources includes snapshots/index.jsonl and snapshots/*.bin.
- Vec3Candidate.analyzer_sources includes structures.jsonl, snapshot artifacts, and stimuli.jsonl when a stimulus label is present.
- Analyzer output contract tests pin nalyzer_sources on typed value, structure, and vec3 candidate JSON.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~AnalyzerOutputContractTests|FullyQualifiedName~TypedValueLaneAnalyzerTests|FullyQualifiedName~StructureAnalyzerTests|FullyQualifiedName~Vec3CandidateAnalyzerTests|FullyQualifiedName~SessionComparisonServiceTests"
    # Passed: 14/14

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 54/54

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Analyzer sources list artifact dependencies; it does not validate semantic truth.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is adding alue_sequence_summary to candidate outputs for compact review of observed candidate values.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add candidate value_sequence_summary fields
