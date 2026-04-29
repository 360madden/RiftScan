# RiftScan changed-file inventory

Timestamp: 2026-04-29 05:45 America/New_York
Repository: `C:\RIFT MODDING\Riftscan`

Purpose: grouped review inventory for the scanner-readiness milestone. This is not a truth claim; it is a review aid for the current uncommitted working tree.

Status legend: `M` modified, `??` untracked.

## Docs and handoffs

- `M` `README.md`
- `??` `docs/capability-readiness-workflow.md`
- `??` `docs/handoffs/2026-04-29-052900-riftscan-scanner-readiness-milestone.md`
- `??` `docs/handoffs/2026-04-29-053500-riftscan-changed-file-inventory.md`
- `??` `docs/handoffs/2026-04-29-054000-riftscan-commit-ready-summary.md`
- `??` `docs/scalar-truth-corroboration.example.jsonl`
- `??` `docs/scalar-truth-run-checklist.md`
- `??` `docs/scalar-truth-workflow.md`

## Scripts

- `??` `scripts/verify-readiness-workflow.ps1`

## CLI surface

- `M` `src/RiftScan.Cli/Program.cs`

## Capture and planning

- `M` `src/RiftScan.Capture/Passive/PassiveCaptureOptions.cs`
- `M` `src/RiftScan.Capture/Passive/PassiveCapturePlanOptions.cs`
- `M` `src/RiftScan.Capture/Passive/PassiveCapturePlanService.cs`
- `M` `src/RiftScan.Capture/Passive/PassiveCaptureService.cs`

## Core/session verification

- `M` `src/RiftScan.Core/Sessions/SessionVerifier.cs`

## Analysis comparison and heuristics

- `M` `src/RiftScan.Analysis/Comparison/SessionComparisonNextCapturePlanGenerator.cs`
- `M` `src/RiftScan.Analysis/Comparison/SessionComparisonReportGenerator.cs`
- `M` `src/RiftScan.Analysis/Comparison/SessionComparisonResult.cs`
- `M` `src/RiftScan.Analysis/Comparison/SessionComparisonService.cs`
- `M` `src/RiftScan.Analysis/Comparison/ValueCandidateComparison.cs`
- `M` `src/RiftScan.Analysis/Comparison/Vec3BehaviorSummary.cs`
- `??` `src/RiftScan.Analysis/Comparison/ComparisonTruthReadinessResult.cs`
- `??` `src/RiftScan.Analysis/Comparison/ComparisonTruthReadinessService.cs`
- `??` `src/RiftScan.Analysis/Comparison/ComparisonTruthReadinessVerificationResult.cs`
- `??` `src/RiftScan.Analysis/Comparison/ComparisonTruthReadinessVerifier.cs`
- `??` `src/RiftScan.Analysis/Comparison/EntityLayoutComparison.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarBehaviorHeuristicEngine.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarBehaviorSummary.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarCandidateComparison.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarEvidenceSetReportGenerator.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarEvidenceSetResult.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarEvidenceSetService.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthCandidate.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthCandidateExporter.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthCorroborationEntry.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthCorroborationVerificationResult.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthCorroborationVerifier.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthRecoveryResult.cs`
- `??` `src/RiftScan.Analysis/Comparison/ScalarTruthRecoveryService.cs`
- `??` `src/RiftScan.Analysis/Comparison/Vec3BehaviorHeuristicEngine.cs`

## Analysis entity/scalar/reporting

- `M` `src/RiftScan.Analysis/Reports/SessionReportGenerator.cs`
- `M` `src/RiftScan.Analysis/Triage/DynamicRegionTriageAnalyzer.cs`
- `M` `src/RiftScan.Analysis/Vectors/Vec3CandidateAnalyzer.cs`
- `??` `src/RiftScan.Analysis/Entities/`
- `??` `src/RiftScan.Analysis/Reports/CapabilityStatusResult.cs`
- `??` `src/RiftScan.Analysis/Reports/CapabilityStatusService.cs`
- `??` `src/RiftScan.Analysis/Reports/CapabilityStatusVerificationResult.cs`
- `??` `src/RiftScan.Analysis/Reports/CapabilityStatusVerifier.cs`
- `??` `src/RiftScan.Analysis/Scalars/`

## Tests

- `M` `tests/RiftScan.Tests/ComparisonOutputContractTests.cs`
- `M` `tests/RiftScan.Tests/GeneratedAnalyzerArtifactSchemaTests.cs`
- `M` `tests/RiftScan.Tests/PassiveCaptureServiceTests.cs`
- `M` `tests/RiftScan.Tests/SessionAnalysisAndReportTests.cs`
- `M` `tests/RiftScan.Tests/SessionComparisonServiceTests.cs`
- `M` `tests/RiftScan.Tests/SessionVerifierTests.cs`
- `M` `tests/RiftScan.Tests/Vec3CandidateAnalyzerTests.cs`
- `??` `tests/RiftScan.Tests/EntityLayoutAnalyzerTests.cs`
- `??` `tests/RiftScan.Tests/ScalarLaneAnalyzerTests.cs`
- `??` `tests/RiftScan.Tests/ScalarTruthCorroborationVerifierTests.cs`

## Review priority

1. `src/RiftScan.Capture/Passive/*` - live read behavior and capture-plan fanout.
2. `src/RiftScan.Analysis/Comparison/*` - truth-readiness, scalar/vec3 behavior heuristics, comparison aggregation.
3. `src/RiftScan.Analysis/Entities/*` and `src/RiftScan.Analysis/Scalars/*` - new analyzers and candidate contracts.
4. `src/RiftScan.Cli/Program.cs` and `scripts/verify-readiness-workflow.ps1` - command surface and operator workflow wiring.
5. `tests/RiftScan.Tests/*` - fixture proof and regression coverage.

## Validation associated with this inventory

- Full suite last run: `229/229 passed`.
- Fixture smoke passed at `reports/generated/smoke-fixture-capability-scalar-20260429-094228`.
- `scripts/verify-readiness-workflow.ps1` smoke succeeded against `entity-layout-compare-20260429-051819.truth-readiness.json`.
- `report capability` now accepts optional `--scalar-evidence-set` and `scripts/verify-readiness-workflow.ps1` can pass `-ScalarEvidenceSetPath`.
- Format check: passed.
- `git diff --check`: passed with LF-to-CRLF warnings only.

## Commit caution

The working tree is intentionally large. Do not reset or clean generated/untracked files without reviewing docs, scripts, tests, new analyzer files, new verifier files, and scalar workflow files.
