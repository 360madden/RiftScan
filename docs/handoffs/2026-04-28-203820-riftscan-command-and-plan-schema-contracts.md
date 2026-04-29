# RiftScan handoff - command and plan schema contracts

created_local: 2026-04-28 20:38:20 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 62bb351 Validate capture plan schema versions
supersedes: docs/handoffs/2026-04-28-203350-riftscan-generated-analyzer-schema-checks.md

## TL;DR

Command/result outputs and capture-plan readers now have explicit schema contracts. Analyze/report/verify command result records serialize stable esult_schema_version fields, and capture plan documents serialize/validate schema_version.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commits after the previous handoff:
  - e2e2783 Add schemas to command results
  - 62bb351 Validate capture plan schema versions
- New contract test file: 	ests/RiftScan.Tests/CommandResultContractTests.cs.

## Proven behavior

- SessionAnalysisResult serializes esult_schema_version = riftscan.session_analysis_result.v1.
- SessionReportResult serializes esult_schema_version = riftscan.session_report_result.v1.
- SessionVerificationResult serializes esult_schema_version = riftscan.session_verification_result.v1.
- PassiveCapturePlanDocument serializes schema_version = riftscan.next_capture_plan.v1 and nalyzer_version.
- PassiveCapturePlanService rejects unsupported normal capture-plan schema versions while retaining compatibility for missing/older schema fields.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~CommandResultContractTests|FullyQualifiedName~SessionAnalysisAndReportTests|FullyQualifiedName~SessionVerifierTests"
    # Passed: 14/14

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~PassiveCaptureSerializationContractTests|FullyQualifiedName~PassiveCaptureServiceTests"
    # Passed: 21/21

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 72/72

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Schema validation protects artifact shape and compatibility, not semantic truth.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next useful step is adding a migration command skeleton for future schema bumps.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add session migration command skeleton
