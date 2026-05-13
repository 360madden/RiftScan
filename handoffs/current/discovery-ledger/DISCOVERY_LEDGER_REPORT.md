# RiftScan Offline Discovery Ledger

Created UTC: `2026-05-13T17:40:44Z`
App version: `riftscan-discovery-ledger-v1.2.1`

## Result

```text
status: ledger_written
scope: offline_artifact_inventory_no_live_process_access
candidate_count: 2
candidate_ledger_contract_validation: PASS
ledger_live_movement_authorized: False
```

## Current best candidate

| Field | Value |
|---|---|
| Candidate | `rift-addon-coordinate-candidate-000001` |
| State | `historical_stale_trace_blocked` |
| Claim level | `candidate` |
| Proof level | `current_api_plus_readonly_memory_candidate` |
| Address | `0x1DA682DF690` |
| Base + offset | `None` + `None` |
| Axis | `xyz` |
| Support | `8` snapshots / `None` observations |
| Best max abs distance | `5.002220859751105e-11` |
| RiftReader status | `None` |
| Next validation | `keep as historical evidence unless explicitly replaying the stale-trace blocker` |

## Candidate ledger

| State | Candidate / kind | Address | Proof level | Next validation |
|---|---|---|---|---|
| `historical_stale_trace_blocked` | `rift-addon-coordinate-candidate-000001` | `0x1DA682DF690` | `current_api_plus_readonly_memory_candidate` | `keep as historical evidence unless explicitly replaying the stale-trace blocker` |
| `historical_candidate_scan_only` | `coordinate_candidate_scan` | `-` | `candidate_like_values_only` | `do not use for current-client movement proof; keep only as historical search context` |

## Candidate ledger contract

```text
status: PASS
path: handoffs/current/discovery-ledger/candidate_ledger.jsonl
candidate_count: 2
error_count: 0
warning_count: 0
```

## Blockers / guardrails

- offline ledger cannot authorize live movement or claim current window focus
- RiftReader pointer says fresh preflight is required before more movement
- older Coord API Truth artifact remains stale-trace-blocked
- RiftScan Movement Execution Gate artifact is blocked/stale relative to newer RiftReader proof lane

## Source artifacts

```json
{
  "riftreader_current_proof_pointer": "C:\\RIFT MODDING\\RiftReader\\docs\\recovery\\current-proof-anchor-readback.json",
  "riftreader_latest_handoff": "C:\\RIFT MODDING\\RiftReader\\docs\\handoffs\\2026-05-13-001802-x64dbg-safe-reintegration-handoff.md",
  "riftreader_latest_proof_summary": "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\proof-anchor-currentpid-57656-readback-summary-20260512-071051.json",
  "riftscan_coord_api_truth_summary": "handoffs/current/coord-api-truth/coord-api-truth-summary.json",
  "riftscan_coord_recovery_summary": "handoffs/current/coord-recovery/coord-recovery-summary.json",
  "riftscan_match_file": "C:\\RIFT MODDING\\RiftReader\\scripts\\captures\\family-scan-currentpid-57656-20260512-103249\\api-family-vec3-candidates.jsonl",
  "riftscan_movement_execution_gate_summary": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json"
}
```

## Safety

```json
{
  "focus_preflight_started": false,
  "ledger_live_movement_authorized": false,
  "live_capture_started": false,
  "movement_or_input_sent": false,
  "offline_only": true,
  "process_attach_or_memory_read_started": false,
  "reloadui_sent": false,
  "riftreader_command_executed": false
}
```

## Next recommended actions

1. Use the RiftReader May 7 current-proof pointer as the latest discovery status, but treat it as requiring fresh preflight before more movement.
2. Keep the RiftScan candidate at 0x2400EA32120 as the current best coordinate candidate source.
3. Do not promote the older 0x1DA682DF690 Coord API Truth artifact beyond historical stale-trace-blocked evidence.
4. When the game window is available, have RiftReader rerun exact PID/HWND proof readback rather than rediscovering from scratch.
5. If PID/HWND changed, reacquire via RiftScan-first candidate import/readback/promotion instead of CE or heuristic caches.
