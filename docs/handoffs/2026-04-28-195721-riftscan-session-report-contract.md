# RiftScan handoff - session report contract

created_local: 2026-04-28 19:57:21 -04:00
repo: C:\RIFT MODDING\Riftscan
branch: main
remote_state: synced_with_origin_main
latest_commit: 5818d78 List analyzers in session reports
supersedes: docs/handoffs/2026-04-28-195252-riftscan-passive-capture-report-json.md

## TL;DR

Passive capture intervention reporting is now both human-readable and machine-readable. Session reports include elapsed interruption metrics, report.json output, pinned JSON contract fields, analyzer identities on analyzer outputs, and an analyzer/version table in report.md/report.json.

## Current truth

- Branch is clean and synced with origin/main as of this handoff.
- Latest pushed commits after the previous handoff:
  - c573fbc Pin session report JSON contract fields
  - a37f8cd Add analyzer identity to analysis outputs
  - 5818d78 List analyzers in session reports
- iftscan report session <session-path> writes eport.md and eport.json.
- CLI JSON output includes eport_json_path.

## Proven behavior

- eport.md includes elapsed_ms for interrupted captures.
- eport.md includes an Analyzers table with analyzer_id, analyzer_version, artifact_path, and entry_count.
- eport.json includes schema_version, session metadata, artifact counts, analyzers[], top_limit, markdown_report_path, and capture_interruption details.
- deltas.jsonl, 	yped_value_candidates.jsonl, and ec3_candidates.jsonl now include analyzer_id and analyzer_version, matching the existing triage/structure/cluster pattern.
- Report JSON contract fields are pinned by tests.

## Validation evidence

    dotnet test RiftScan.slnx --configuration Release --filter FullyQualifiedName~SessionAnalysisAndReportTests
    # Passed: 5/5

    dotnet test RiftScan.slnx --configuration Release --filter "FullyQualifiedName~ByteDeltaAnalyzerTests|FullyQualifiedName~TypedValueLaneAnalyzerTests|FullyQualifiedName~Vec3CandidateAnalyzerTests"
    # Passed: 4/4

    dotnet test RiftScan.slnx --configuration Release --no-build
    # Passed: 48/48

    dotnet format --verify-no-changes
    # Passed

    git diff --check
    # Passed

## Still not proven

- No live RIFT restart/crash validation has been run.
- Disposable-process proof remains process-lifecycle evidence, not a live RIFT truth claim.

## Resume here

1. Confirm clean state: git status --short --branch.
2. If staying offline, next smallest code step is adding report limitations / next recommended capture fields to report.json so the machine report mirrors the human report's final guidance.
3. If moving live, use docs/validation/live-rift-passive-capture-restart-checklist.md and preserve session artifacts.
4. Do not claim live RIFT recovery until a verified live session artifact exists.

## Suggested next commit if continuing offline

Add report limitations and next capture guidance to report JSON
