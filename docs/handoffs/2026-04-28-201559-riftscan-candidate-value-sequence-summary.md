# RiftScan handoff - candidate value sequence summaries

created_local: 2026-04-28 20:15:59 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 6bd6dcc Add candidate value sequence summaries
supersedes: docs/handoffs/2026-04-28-201415-riftscan-candidate-analyzer-sources.md

## TL;DR

Candidate outputs now include compact alue_sequence_summary fields for typed value, structure, and vec3 candidates. This makes quick candidate review possible without loading full previews first.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commit after the previous handoff:
  - 6bd6dcc Add candidate value sequence summaries

## Proven behavior

- TypedValueCandidate.value_sequence_summary includes sample count, distinct count, changed-pair count, and a short preview.
- StructureCandidate.value_sequence_summary includes snapshot support and a short float-triplet preview.
- Vec3Candidate.value_sequence_summary includes sample count, delta magnitude, and a short preview.
- Analyzer output contract tests pin alue_sequence_summary on typed value, structure, and vec3 candidate JSON.

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
- Value summaries are compact review aids, not semantic truth validation.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest useful step is surfacing these candidate contract fields in comparison outputs where candidate IDs or summaries are missing.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Surface candidate ids and summaries in comparisons
