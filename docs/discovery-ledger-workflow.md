# RiftScan Discovery Ledger Workflow

## Purpose

The discovery ledger is the offline source-of-truth inventory for coordinate discovery status while live RIFT access is unavailable or owned by another tool.

It reconciles stored RiftScan artifacts with stored RiftReader proof-pointer artifacts. It never starts focus preflight, live capture, process attach, memory reads, movement/input, RiftReader commands, offset validation, or `/reloadui`.

## Commands

```text
python tools/riftscan_discovery_ledger.py --self-test
python tools/riftscan_discovery_ledger.py
python tools/riftscan_discovery_ledger.py --validate-existing
.\scripts\run-riftscan-discovery-ledger.cmd
```

`--validate-existing` validates `handoffs/current/discovery-ledger/candidate_ledger.jsonl` without writing artifacts.

Normal ledger generation writes `candidate_ledger.jsonl`, validates it immediately, then embeds the validation result in both `discovery-ledger-summary.json` and `DISCOVERY_LEDGER_REPORT.md`.

`.\scripts\run-riftscan-offline-workflow-check.cmd` refreshes the ledger and then validates the generated candidate ledger contract.

## Artifact contract

The current output directory is:

```text
handoffs/current/discovery-ledger/
```

Required outputs:

- `DISCOVERY_LEDGER_REPORT.md`
- `discovery-ledger-summary.json`
- `candidate_ledger.jsonl`
- `discovery-ledger-log.jsonl`

`candidate_ledger.jsonl` is one JSON object per line. It is intended for machine reuse and must remain append/replay friendly.

## Common candidate ledger fields

Every row must include:

```text
stable_id
kind
state
claim_level
proof_level
source
ledger_live_movement_authorized
next_validation_step
source_artifacts
warnings
```

`ledger_live_movement_authorized` must always be `false`.

`source_artifacts` must contain at least one artifact path. `warnings` must contain at least one guardrail note.

Allowed `claim_level` values:

- `observed`
- `candidate`
- `validated_candidate`

Allowed `state` values:

- `candidate`
- `validated_candidate_historical_checkpoint`
- `historical_stale_trace_blocked`
- `historical_candidate_scan_only`

## Coordinate row fields

Rows with `kind=coordinate_vec3` must include:

```text
candidate_id
source_absolute_address_hex
axis_order
support_count
best_max_abs_distance
best_memory_xyz
best_addon_xyz
```

`best_memory_xyz` and `best_addon_xyz` must each contain three values.

Rows with `state=validated_candidate_historical_checkpoint` must also include non-empty:

```text
source_base_address_hex
source_offset_hex
source_absolute_address_hex
```

Validated checkpoint rows must preserve:

- `riftreader_pointer_matched_candidate=true`
- `latest_validation.no_cheat_engine=true`
- `latest_validation.movement_sent_by_readback=false`
- a `next_validation_step` that still requires proof readback before live input
- the warning `offline_ledger_does_not_authorize_live_movement`

Important: `latest_validation.movement_allowed_at_capture_time=true` is historical proof evidence only. It is not current movement authorization.

## Historical states

`historical_stale_trace_blocked` rows preserve old evidence that failed current-process matching. They must keep `trace_anchor.trace_matches_process=false`.

`historical_candidate_scan_only` rows are observed search context only. They must use `kind=coordinate_candidate_scan`, keep `claim_level=observed`, and keep `final_truth_claim=false`.

## Consumption rules

- Do not use the ledger alone to send movement.
- Do not promote a coordinate row from the ledger alone to current live truth.
- Before future live input, rerun exact PID/HWND proof readback in RiftReader.
- If PID/HWND changed, reacquire through the current RiftScan/RiftReader proof lane instead of reusing stale traces.
- Keep stale and failed rows as evidence unless an explicit prune workflow with dry-run and manifest record exists.
