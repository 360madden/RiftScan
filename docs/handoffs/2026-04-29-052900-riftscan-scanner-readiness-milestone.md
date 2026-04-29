# RiftScan scanner readiness milestone handoff

Timestamp: 2026-04-29 05:29 America/New_York
Repository: `C:\RIFT MODDING\Riftscan`

## TL;DR

RiftScan now has a broad scanner feature set for read-only capture, offline replay analysis, entity-layout detection, vec3/scalar heuristic scoring, scalar truth export/recovery, external corroboration hooks, truth-readiness export/verification, and capability-status export/verification.

Current artifact truth says the scanner is **coded broadly**, but evidence is still incomplete for final truth recovery:

- `entity_layout`: `strong_candidate`
- `position`: `candidate_needs_labeled_contrast`
- `actor_yaw`: `missing`
- `camera_orientation`: `missing`

No recovered-truth claim should be made yet. The next proof step is a labeled `move_forward` capture against the stable entity-layout/vec3 targets.

## Latest verified artifacts

Comparison and readiness artifacts:

- `C:\RIFT MODDING\Riftscan\reports\generated\entity-layout-compare-20260429-051819.json`
- `C:\RIFT MODDING\Riftscan\reports\generated\entity-layout-compare-20260429-051819.md`
- `C:\RIFT MODDING\Riftscan\reports\generated\entity-layout-compare-20260429-051819.next-capture-plan.json`
- `C:\RIFT MODDING\Riftscan\reports\generated\entity-layout-compare-20260429-051819.truth-readiness.json`

Capability status artifact:

- `C:\RIFT MODDING\Riftscan\reports\generated\capability-status-20260429-052612.json`

Latest source sessions compared:

- `C:\RIFT MODDING\Riftscan\sessions\live-recursive-window-followup-20260429-033104`
- `C:\RIFT MODDING\Riftscan\sessions\live-plan-passive-fanout-20260429-045457`

## What was built in this milestone

### Capture/follow-up

- Windowed follow-up capture with deterministic windows per selected region.
- Fixed plan base-address fanout so requested addresses inside a live region can expand to multiple windows.
- CLI supports `capture plan ... --windows-per-region` and `--window-offsets`.

### Offline analysis

- Non-finite vec3 delta handling fixed.
- Scalar lane analyzer added and wired into analysis, report, and verification paths.
- Entity layout analyzer added and wired into analysis, report, verification, and session comparison.

### Heuristics and truth-readiness

- Vec3 behavior heuristic engine for passive-vs-move contrast.
- Scalar behavior heuristic engine for turn/camera angle-like candidates.
- Opposite-turn polarity handling.
- Camera-only separation handling.
- Multi-session scalar evidence aggregation.
- Scalar truth candidate export.
- Scalar truth repeat recovery.
- External corroboration JSONL hook and verifier.
- Comparison truth-readiness export:
  - `riftscan compare sessions ... --truth-readiness <json>`
- Comparison truth-readiness verifier:
  - `riftscan verify comparison-readiness <json>`
- Capability/status matrix:
  - `riftscan report capability --truth-readiness <json> --json-out <json>`
- Capability/status verifier:
  - `riftscan verify capability-status <json>`
- One-command readiness workflow helper:
  - `scripts/verify-readiness-workflow.ps1`

### Docs

- `C:\RIFT MODDING\Riftscan\docs\scalar-truth-workflow.md`
- `C:\RIFT MODDING\Riftscan\docs\scalar-truth-run-checklist.md`
- `C:\RIFT MODDING\Riftscan\docs\scalar-truth-corroboration.example.jsonl`
- `C:\RIFT MODDING\Riftscan\docs\capability-readiness-workflow.md`
- `C:\RIFT MODDING\Riftscan\scripts\verify-readiness-workflow.ps1`
- `C:\RIFT MODDING\Riftscan\README.md` updated with the reproducible loop.

## Current capability status from latest packet

From `capability-status-20260429-052612.json`:

- `capability_count`: `15`
- `entity_layout`: coded, `strong_candidate`, evidence count `2`
- `position`: coded, `candidate_needs_labeled_contrast`, evidence count `34`
- `actor_yaw`: coded, `missing`, evidence count `0`
- `camera_orientation`: coded, `missing`, evidence count `0`

Note: current code now adds `scalar_evidence_set_verify`, so newly generated capability packets report one additional coded capability.

Evidence gaps:

- `position:candidate_needs_labeled_contrast`
- `actor_yaw:missing`
- `camera_orientation:missing`

Recommended next actions from packet:

- `capture_labeled_move_forward`
- `capture_contrasting_stimulus_session`
- `capture_labeled_turn_session`
- `capture_labeled_camera_only_session`

## Validation already run

Latest validation pass:

```powershell
dotnet test .\RiftScan.slnx --configuration Release
dotnet format .\RiftScan.slnx --verify-no-changes --no-restore
git diff --check
```

Results:

- Full test suite: `229/229 passed`
- Format check: passed
- Diff check: passed, with LF-to-CRLF warnings only

Additional focused validation run during milestone:

- targeted comparison/contract tests passed after each slice
- readiness verifier succeeded on `entity-layout-compare-20260429-051819.truth-readiness.json`
- capability verifier succeeded on `capability-status-20260429-052612.json`
- fixture smoke succeeded at `reports/generated/smoke-fixture-capability-scalar-20260429-094228`
- `scripts/verify-readiness-workflow.ps1` smoke succeeded against `entity-layout-compare-20260429-051819.truth-readiness.json`
- capability status can now optionally merge `--scalar-evidence-set` evidence into actor-yaw/camera readiness.

## Exact next live-safe proof command

This command performs read-only capture from the current next-capture plan. It does not send RIFT input. The user/operator must supply the labeled movement stimulus manually during the capture window.

```powershell
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan .\reports\generated\entity-layout-compare-20260429-051819.next-capture-plan.json `
  --pid <rift_pid> `
  --out .\sessions\live-move-forward-followup-<timestamp> `
  --samples 5 --interval-ms 100 `
  --windows-per-region 3 `
  --stimulus move_forward `
  --intervention-wait-ms 1200000 `
  --intervention-poll-ms 2000
```

After capture:

```powershell
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- analyze session .\sessions\live-move-forward-followup-<timestamp> --top 100

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- verify session .\sessions\live-move-forward-followup-<timestamp>

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare sessions .\sessions\live-plan-passive-fanout-20260429-045457 .\sessions\live-move-forward-followup-<timestamp> --top 50 `
  --out .\reports\generated\passive-vs-move-forward-<timestamp>.json `
  --report-md .\reports\generated\passive-vs-move-forward-<timestamp>.md `
  --next-plan .\reports\generated\passive-vs-move-forward-<timestamp>.next-capture-plan.json `
  --truth-readiness .\reports\generated\passive-vs-move-forward-<timestamp>.truth-readiness.json

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- verify comparison-readiness .\reports\generated\passive-vs-move-forward-<timestamp>.truth-readiness.json

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability --truth-readiness .\reports\generated\passive-vs-move-forward-<timestamp>.truth-readiness.json `
  --scalar-evidence-set .\reports\generated\scalar-evidence-set-<timestamp>.json `
  --json-out .\reports\generated\capability-status-<timestamp>.json

dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release --no-build -- verify capability-status .\reports\generated\capability-status-<timestamp>.json
```

## Important boundaries

- Do not claim recovered truth from readiness/capability packets.
- Do not add input/window control into RiftScan core.
- Keep live capture read-only and low pressure.
- Keep expensive analysis offline and replayable from stored artifacts.
- Preserve raw sessions and generated proof artifacts.

## Working tree note

The working tree is intentionally large and uncommitted at this point. Do not reset or clean without reviewing. Current work includes source, tests, docs, scalar workflow files, entity layout files, capability/readiness files, and generated proof artifacts.

