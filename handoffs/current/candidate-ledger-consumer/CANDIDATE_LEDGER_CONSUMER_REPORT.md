# RiftScan Candidate Ledger Consumer Report

Created UTC: `2026-05-07T16:51:20Z`
App version: `riftscan-candidate-ledger-consumer-v1.0.0`

## Result

```text
status: PASS
mode: offline_candidate_ledger_consumer
contract_validation: PASS
safe_candidate_count: 3
rejected_candidate_count: 0
live_action_authorized: False
```

## Current best offline candidate

| Field | Value |
|---|---|
| Stable ID | `coordinate::rift-addon-coordinate-candidate-000001::0x2400EA32120` |
| State | `validated_candidate_historical_checkpoint` |
| Address | `0x2400EA32120` |
| Base + offset | `0x2400E970000` + `0xC2120` |
| Live use authorized | `False` |
| Next validation | `rerun exact current PID/HWND proof readback before any more live movement` |

## Safe candidates

| State | Candidate / kind | Address | Consumer status |
|---|---|---|---|
| `validated_candidate_historical_checkpoint` | `rift-addon-coordinate-candidate-000001` | `0x2400EA32120` | `available_offline_only` |
| `historical_stale_trace_blocked` | `rift-addon-coordinate-candidate-000001` | `0x1DA682DF690` | `available_offline_only` |
| `historical_candidate_scan_only` | `coordinate_candidate_scan` | `-` | `available_offline_only` |

## Blockers

- None.

## Forbidden downstream uses

- movement
- input
- live_capture
- process_attach
- memory_read
- offset_validation
- riftreader_command
- reloadui

## Safety

```json
{
  "focus_preflight_started": false,
  "live_action_authorized": false,
  "live_capture_started": false,
  "memory_scan_or_read_started": false,
  "movement_or_input_sent": false,
  "offline_only": true,
  "offset_validation_started": false,
  "process_attach_or_memory_read_started": false,
  "reloadui_sent": false,
  "riftreader_command_executed": false
}
```
