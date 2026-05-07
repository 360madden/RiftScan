# AI workflow packet schema

This document defines the offline AI workflow packet written to `handoffs/current/ai-workflow/`.

The packet is a resume-and-triage surface for offline agents. It must remain safe to generate while RiftReader or the operator owns the game window.

## Safety boundary

Packet generation may read repo files, generated JSON/JSONL/Markdown artifacts, and local git metadata.

Packet generation must not start focus preflight, live capture, scanner probes against a live process, process attach, process memory reads, movement/input, RiftReader commands, offset validation, or `/reloadui`.

## Output files

| Path | Purpose |
|---|---|
| `handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md` | Compact human resume packet. |
| `handoffs/current/ai-workflow/ai-workflow-summary.json` | Machine-readable packet. |
| `handoffs/current/ai-workflow/ai-workflow-log.jsonl` | Append-only helper log. |
| `handoffs/current/ai-workflow/history/index.jsonl` | Append-only index of archived prior packet summaries/reports. |

## Required summary fields

| Field | Meaning |
|---|---|
| `schema_version` | Packet schema identifier. Current value is `riftscan.ai_workflow_packet.v1`. |
| `created_utc` | Packet creation timestamp. This is refresh metadata and is not treated as truth change by previous-packet diffing. |
| `app_version` | Helper implementation version. |
| `status` | `PASS` when no blockers exist, otherwise `BLOCKED`. |
| `mode` | Must be `offline_ai_workflow`. |
| `blockers` | Hard blockers for offline continuation. |
| `warnings` | Non-blocking caveats for the next agent. |
| `recommended_first_files` | Ordered files an offline agent should inspect first. |
| `current_best_candidate` | Compact current-best candidate view copied from the safe ledger consumer or discovery ledger. |
| `candidate_ledger_consumer` | Safe downstream candidate summary and artifact-age diagnostics. |
| `discovery_ledger` | Discovery ledger status and candidate-ledger contract validation summary. |
| `offline_workflow_check` | Offline workflow check status. |
| `operator_gate` | Operator summary fields needed to preserve live-work boundaries. |
| `git` | Local git snapshot from packet generation time. |
| `previous_packet_diff` | Selected-field comparison against the previous packet summary. |
| `previous_packet_diff_compared_fields` | Machine-readable contract for fields compared by `previous_packet_diff`. |
| `previous_packet_archive` | Archive result for the packet summary/report that existed before the current overwrite. |
| `packet_history_index` | Append result for the packet history index row associated with the latest archive operation. |
| `top_next_actions` | Ordered offline-only next actions. |
| `ai_resume_prompt` | Ready-to-paste resume prompt for the next offline agent. |
| `source_artifacts` | Paths used to build the packet. |
| `paths` | Packet output paths. |
| `safety` | Explicit false flags for live/focus/capture/input/memory-read actions. |

## Previous packet diff

`previous_packet_diff` tells an offline agent whether meaningful workflow/truth fields changed since the prior packet.

It intentionally ignores normal refresh noise such as `created_utc`, JSONL row growth, report timestamps, full git status text, and regenerated Markdown wording that does not alter selected packet fields.

Use `python tools/riftscan_ai_workflow_packet.py --print-diff` for a compact terminal view of the current diff after refreshing the packet artifacts.

Use `python tools/riftscan_ai_workflow_packet.py --show-existing-diff` when you need a read-only terminal view of the saved packet diff without refreshing artifacts or appending logs.

Use `python tools/riftscan_ai_workflow_packet.py --show-history-index` to inspect saved history-index rows without refreshing artifacts, or `python tools/riftscan_ai_workflow_packet.py --verify-history-index` to validate them with a nonzero exit on failure.

## Previous packet archive

Before overwriting `ai-workflow-summary.json`, packet generation copies the prior summary into `handoffs/current/ai-workflow/history/`. If the prior Markdown report exists, it is copied there too.

The current packet records this under `previous_packet_archive` with `status`, `history_dir`, `history_index`, source packet metadata, and `artifacts` paths. The archive is append-only for normal packet generation; do not delete historical packet files just to reduce noise.

When `status` is `ARCHIVED`, packet generation appends one JSON object to `handoffs/current/ai-workflow/history/index.jsonl` with schema `riftscan.ai_workflow_packet_history_index.v1`. Each row records `indexed_utc`, `archive_stem`, `source_created_utc`, `source_app_version`, and `artifacts`.

Offline Workflow Check validates `previous_packet_archive.status`, verifies archived summary/report files exist when the status is `ARCHIVED`, confirms archived summaries are valid JSON, validates every `history_index` JSONL row, and confirms the index contains the archived summary/report pair.

### Status values

| Status | Meaning |
|---|---|
| `NO_PREVIOUS_PACKET` | No readable previous `ai-workflow-summary.json` was loaded. |
| `UNCHANGED` | All selected fields match the previous packet. |
| `CHANGED` | One or more selected fields changed. Inspect `changes`. |

### Required diff fields

| Field | Meaning |
|---|---|
| `schema_version` | Diff schema identifier. Current value is `riftscan.ai_workflow_packet_diff.v1`. |
| `status` | One of the status values above. |
| `previous_packet_available` | `true` when the previous packet was loaded and parsed. |
| `previous_created_utc` | Timestamp from the previous packet, when available. |
| `previous_app_version` | Helper version from the previous packet, when available. |
| `current_created_utc` | Timestamp from the current packet. |
| `current_app_version` | Helper version from the current packet. |
| `change_count` | Number of selected fields whose values changed. |
| `changes` | Array of selected-field changes with `field`, `before`, and `after`. |

### Compared field contract

`previous_packet_diff_compared_fields` is the machine-readable source of truth. The current helper compares these fields:

| Field | Packet path |
|---|---|
| `app_version` | `app_version` |
| `status` | `status` |
| `blocker_count` | `blocker_count` |
| `warning_count` | `warning_count` |
| `current_best_stable_id` | `current_best_candidate.stable_id` |
| `current_best_address` | `current_best_candidate.source_absolute_address_hex` |
| `candidate_consumer_status` | `candidate_ledger_consumer.status` |
| `safe_candidate_count` | `candidate_ledger_consumer.safe_candidate_count` |
| `rejected_candidate_count` | `candidate_ledger_consumer.rejected_candidate_count` |
| `artifact_stale_count` | `candidate_ledger_consumer.artifact_age.stale_count` |
| `artifact_missing_count` | `candidate_ledger_consumer.artifact_age.missing_count` |
| `current_best_stale_count` | `candidate_ledger_consumer.artifact_age.current_best_stale_count` |
| `current_best_missing_count` | `candidate_ledger_consumer.artifact_age.current_best_missing_count` |
| `discovery_ledger_contract_status` | `discovery_ledger.candidate_ledger_contract_validation.status` |
| `offline_workflow_status` | `offline_workflow_check.status` |
| `operator_live_collection_allowed` | `operator_gate.live_collection_allowed` |

If this table changes, update `PACKET_DIFF_FIELDS` in `tools/riftscan_ai_workflow_packet.py`, rerun the helper self-test, refresh the packet, and rerun offline validation.

Offline Workflow Check also validates that `ai-workflow-summary.json` exposes `previous_packet_diff`, exposes `previous_packet_diff_compared_fields`, and that this schema document covers every required compared field.

## Interpretation rule

- `UNCHANGED` means the latest packet did not alter selected workflow/truth fields; continue the next planned offline slice instead of re-reading logs for timestamp-only churn.
- `CHANGED` means inspect the changed field list before promoting any conclusion.
- `NO_PREVIOUS_PACKET` means treat the packet as a baseline only.
