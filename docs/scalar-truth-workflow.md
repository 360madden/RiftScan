# RiftScan scalar truth workflow

This workflow is for actor yaw / camera yaw scalar discovery. It is read-only, replayable, and artifact-backed. It does not turn candidate evidence into a final truth claim without labeled behavior, optional corroboration, and repeat recovery.

For live runs, copy or fill out `docs/scalar-truth-run-checklist.md` so the process ID, session IDs, artifacts, and promotion decision are preserved.

## Required capture set

Capture the same planned regions/windows across these stimulus labels:

1. `passive_idle`
2. `turn_left`
3. `turn_right`
4. `camera_only`

Use `capture plan` from a prior `next_capture_plan.json` when available so all labels read comparable regions.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<passive_id> --samples 3 --interval-ms 100 `
  --windows-per-region 3 --stimulus passive_idle

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<turn_left_id> --samples 3 --interval-ms 100 `
  --windows-per-region 3 --stimulus turn_left

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<turn_right_id> --samples 3 --interval-ms 100 `
  --windows-per-region 3 --stimulus turn_right

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  capture plan reports/generated/<next-plan>.json --pid <rift_pid> `
  --out sessions/<camera_only_id> --samples 3 --interval-ms 100 `
  --windows-per-region 3 --stimulus camera_only
```

## Analyze and report sessions

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<passive_id> --all

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<turn_left_id> --all

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<turn_right_id> --all

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  analyze session sessions/<camera_only_id> --all
```

Each analyzed session writes `scalar_candidates.jsonl` with value family, circular deltas, direction, and retention bucket fields.

## Aggregate scalar evidence

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-set `
  sessions/<passive_id> `
  sessions/<turn_left_id> `
  sessions/<turn_right_id> `
  sessions/<camera_only_id> `
  --top 100 `
  --out reports/generated/<scalar-evidence-set>.json `
  --report-md reports/generated/<scalar-evidence-set>.md `
  --truth-out reports/generated/<scalar-truth-candidates>.jsonl
```

Expected strong actor-yaw evidence:

- passive baseline is stable
- `turn_left` and `turn_right` both change
- signed circular deltas have opposite polarity
- `camera_only` is stable

Expected strong camera-yaw evidence:

- passive baseline is stable
- `camera_only` changes
- turn sessions are stable

One scalar evidence set can contain both actor-yaw and camera-orientation candidates when the same four labeled sessions include both lanes. Keep them together when possible; split evidence sets are only needed when the actor and camera lanes were captured in different region/window batches.

If no candidate ranks, inspect `rejected_candidate_summaries` in the scalar evidence set JSON/report.

Verify the scalar evidence set before feeding it into downstream status or truth export:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-evidence-set reports/generated/<scalar-evidence-set>.json
```

You can also feed the generated scalar evidence set into capability status so actor-yaw and camera-readiness gaps reflect the strongest available scalar evidence:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability `
  --truth-readiness reports/generated/<truth-readiness>.json `
  --scalar-evidence-set reports/generated/<scalar-evidence-set>.json `
  --json-out reports/generated/<capability-status>.json
```

If actor and camera candidates live in separate scalar evidence sets, repeat `--scalar-evidence-set` for each file; the capability report merges the strongest readiness per component.

## Optional external/addon corroboration

Create a corroboration JSONL file using:

- `docs/scalar-truth-corroboration.example.jsonl`

Validate it first:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-corroboration reports/generated/<scalar-truth-corroboration>.jsonl
```

Then apply it during truth-candidate export:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-set `
  sessions/<passive_id> `
  sessions/<turn_left_id> `
  sessions/<turn_right_id> `
  sessions/<camera_only_id> `
  --top 100 `
  --out reports/generated/<scalar-evidence-set>.json `
  --report-md reports/generated/<scalar-evidence-set>.md `
  --truth-out reports/generated/<scalar-truth-candidates>.jsonl `
  --corroboration reports/generated/<scalar-truth-corroboration>.jsonl
```

Corroboration can mark candidates as `corroborated`, `uncorroborated`, or `conflicted`. Conflicted candidates are preserved with conflict status instead of being hidden.

## Repeat recovery

Repeat the capture set independently, export a second truth-candidate JSONL, then compare both truth exports:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-truth `
  reports/generated/<scalar-truth-candidates-run-1>.jsonl `
  reports/generated/<scalar-truth-candidates-run-2>.jsonl `
  --out reports/generated/<scalar-truth-recovery>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json
```

Recovered scalar candidates require matching address, offset, data type, and classification across at least two truth-candidate files. They are still marked as reviewed recovery evidence, not unconditional final truth.

For combined actor/camera scalar evidence, the same repeat recovery command can recover both candidates together when both truth-candidate JSONL files contain the dual-lane packet. Expect one recovered `actor_yaw_angle_scalar_candidate` and one recovered `camera_orientation_angle_scalar_candidate` when the repeated capture set preserves the same base/offset lanes.

## Promotion review from recovery plus corroboration

After repeat recovery, combine the recovery packet with an external/addon corroboration JSONL. This creates a scalar truth promotion packet; it does not create final truth by itself.

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-promotion `
  reports/generated/<scalar-truth-recovery>.json `
  --corroboration reports/generated/<scalar-truth-corroboration>.jsonl `
  --out reports/generated/<scalar-truth-promotion>.json

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json
```

Then convert the verified promotion packet into the manual review packet and its markdown companion:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  review scalar-promotion reports/generated/<scalar-truth-promotion>.json `
  --out reports/generated/<scalar-promotion-review>.json `
  --report-md reports/generated/<scalar-promotion-review>.md

dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-promotion-review reports/generated/<scalar-promotion-review>.json
```

Promotion statuses:

- `corroborated_candidate`: recovered candidate matched external/addon corroboration.
- `recovered_candidate`: repeated recovery exists, but matching corroboration was absent or uncorroborated.
- `blocked_conflict`: external/addon corroboration conflicts with the recovered candidate.

Review decision states:

- `ready_for_manual_truth_review`: repeated recovery and corroboration are aligned, but a human/manual truth review is still required.
- `blocked_conflict`: a conflict is preserved and blocks promotion.
- `needs_more_corroboration`: recovery exists but corroboration is missing or uncorroborated.
- `needs_repeat_capture`: repeat evidence is insufficient for review.
- `do_not_promote`: the source state is unsafe or unsupported.

You can feed verified recovery into capability status. Recovery readiness takes precedence over one-run scalar evidence readiness:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  report capability `
  --truth-readiness reports/generated/<truth-readiness>.json `
  --scalar-evidence-set reports/generated/<scalar-evidence-set>.json `
  --scalar-truth-recovery reports/generated/<scalar-truth-recovery>.json `
  --scalar-truth-promotion reports/generated/<scalar-truth-promotion>.json `
  --json-out reports/generated/<capability-status>.json
```

## Readiness levels

- `insufficient`: not enough evidence to rank.
- `candidate`: behavior signal exists but required coverage is incomplete or weak.
- `strong_candidate`: high score without rejection reasons.
- `validated_candidate`: behavior-validated candidate with passive stability, opposite-turn polarity, and camera/turn separation.
- `recovered_candidate`: repeated truth-candidate export matched across independent runs.
- `corroborated_candidate`: repeated recovery plus matching external/addon corroboration; still requires manual review before final truth.
- `blocked_conflict`: external/addon corroboration conflicts with the recovered candidate.
- `ready_for_manual_truth_review`: scalar promotion review is ready for human decision, but `final_truth_claim` remains false until explicitly confirmed.

## Guardrails

- All artifacts are candidate evidence unless the claim level explicitly says otherwise.
- Scalar promotion review packets must preserve conflicts and keep `manual_confirmation_required=true` / `final_truth_claim=false`.
- Addon/external corroboration validates candidates; it does not replace memory discovery.
- Do not use launcher/input/window control inside RiftScan core.
- Preserve raw sessions and generated reports so claims remain replayable.
