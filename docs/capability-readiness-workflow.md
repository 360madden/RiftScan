# RiftScan capability and truth-readiness workflow

This workflow answers two separate questions:

1. **What is coded in?** Use `report capability`.
2. **What evidence is still missing before truth claims?** Use `compare sessions --truth-readiness`, then verify it.

The output is machine-readable JSON. It is candidate/readiness evidence only; it is not recovered truth.

## 1. Generate comparison outputs with truth readiness

Run this after two analyzed sessions have overlapping evidence.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare sessions sessions/<session_a> sessions/<session_b> --top 50 `
  --out reports/generated/<compare>.json `
  --report-md reports/generated/<compare>.md `
  --next-plan reports/generated/<compare>.next-capture-plan.json `
  --truth-readiness reports/generated/<compare>.truth-readiness.json
```

The truth-readiness packet reports the current state of:

- `entity_layout`
- `position`
- `actor_yaw`
- `camera_orientation`

Expected readiness values include:

- `missing`
- `candidate`
- `candidate_needs_labeled_contrast`
- `candidate_needs_camera_only_separation`
- `strong_candidate`

## 2. Verify the truth-readiness packet

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify comparison-readiness reports/generated/<compare>.truth-readiness.json
```

The verifier checks schema, required components, confidence-score range, blocking gaps, required next capture fields, and the warning that readiness is candidate evidence rather than truth.

A usable packet returns:

```json
{
  "schema_version": "riftscan.comparison_truth_readiness_verification.v1",
  "success": true,
  "issues": []
}
```

## 3. Generate the capability/status matrix

Use the verified truth-readiness packet to bind coded capability status to live evidence readiness.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability `
  --truth-readiness reports/generated/<compare>.truth-readiness.json `
  --truth-readiness reports/generated/<additional-compare>.truth-readiness.json `
  --scalar-evidence-set reports/generated/<scalar-evidence-set>.json `
  --scalar-evidence-set reports/generated/<additional-scalar-evidence-set>.json `
  --json-out reports/generated/<capability-status>.json
```

This reports:

- coded capability surfaces and primary commands
- output artifacts each surface emits
- truth component readiness
- evidence gaps
- top next recommended actions

## 4. Verify the capability/status matrix

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify capability-status reports/generated/<capability-status>.json
```

The verifier checks the required scanner capabilities, truth components when present, next recommended actions, and the warning that capability status is not recovered truth.

## One-command verifier helper

After a truth-readiness packet exists, this script runs the three verification/status steps together:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-readiness-workflow.ps1 `
  -TruthReadinessPath reports/generated/<compare>.truth-readiness.json `
  -ScalarEvidenceSetPath reports/generated/<scalar-evidence-set>.json `
  -CapabilityStatusPath reports/generated/<capability-status>.json
```

It performs:

1. `verify comparison-readiness`
2. `verify scalar-evidence-set` when `-ScalarEvidenceSetPath` is supplied
3. `report capability --truth-readiness ... --scalar-evidence-set ... --json-out ...`
4. `verify capability-status`

`report capability` accepts repeated `--truth-readiness` and `--scalar-evidence-set` inputs when entity-layout, position, actor-yaw, and camera-orientation evidence were produced as separate replayable packets.

## Current interpretation rules

- `entity_layout:strong_candidate` means layout evidence exists across sessions; it still needs behavior validation before player/actor/camera truth claims.
- `position:candidate_needs_labeled_contrast` means vec3 candidates match, but passive-vs-move-forward evidence is still missing.
- `actor_yaw:missing` means run labeled turn evidence, ideally both `turn_left` and `turn_right`.
- `actor_yaw:candidate_needs_camera_only_separation` means turn-responsive scalar evidence exists, but it cannot be promoted until camera-only separation is shown.
- `camera_orientation:missing` means run a labeled `camera_only` capture.

## Validation after workflow changes

```powershell
dotnet test RiftScan.slnx --configuration Release
dotnet format RiftScan.slnx --verify-no-changes --no-restore
git diff --check
```
