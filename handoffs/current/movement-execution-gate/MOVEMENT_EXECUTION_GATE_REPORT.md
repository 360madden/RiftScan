# RiftScan Movement Execution Gate Report

## Result

```text
MOVEMENT EXECUTION GATE: BLOCKED
status: blocked_movement_execution_not_allowed
movement_execution_allowed: false
expires_utc: None
```

## Blockers

- live-test-riftscan preflight failed for move_forward.
- live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.
- live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance.

## Warnings

- None

## Recommended Exact Movement Command

Only use this command while `movement_execution_allowed=true` and before `expires_utc`:

```powershell
.\scripts\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100
```

## Safety Boundary

```text
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started_by_riftscan: false
offset_validation_started: false
reloadui_sent: false
```

Note: wrapper preflight may invoke RiftReader anchor validation but does not start RiftScan capture or send movement/input.

## Wrapper Preflight Output

stdout:

```text
BLOCKED: freshness checks failed. No capture started.
Verdict: C:\RIFT MODDING\Riftscan\reports\generated\manual-live-test-20260506-063816\freshness-verdict.json
 - RiftReader anchor TraceMatchesProcess is not true.
 - Source object coordinate sample does not match ReaderBridge within tolerance.

```

stderr:

```text

```

## Output Paths

```text
report: handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md
summary: handoffs/current/movement-execution-gate/movement-execution-gate-summary.json
log: handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl
```

## Machine-Readable Summary

```json
{
  "app_version": "riftscan-movement-execution-gate-v1.0.0",
  "blockers": [
    "live-test-riftscan preflight failed for move_forward.",
    "live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.",
    "live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance."
  ],
  "checks": {
    "commands": {
      "focus_preflight": {
        "args": [
          "cmd",
          "/c",
          "C:\\RIFT MODDING\\Riftscan\\scripts\\run-rift-focus-control.cmd"
        ],
        "error": null,
        "returncode": 0,
        "stderr_tail": "",
        "stdout_tail": "Focus control handoff written to C:\\RIFT MODDING\\Riftscan\\handoffs\\current\\focus-control-local\n",
        "success": true
      },
      "wrapper_preflight": {
        "args": [
          "cmd",
          "/c",
          "C:\\RIFT MODDING\\Riftscan\\scripts\\live-test-riftscan.cmd",
          "-Stimulus",
          "move_forward",
          "-PreflightOnly"
        ],
        "error": null,
        "returncode": 2,
        "stderr_tail": "",
        "stdout_tail": "BLOCKED: freshness checks failed. No capture started.\nVerdict: C:\\RIFT MODDING\\Riftscan\\reports\\generated\\manual-live-test-20260506-063816\\freshness-verdict.json\n - RiftReader anchor TraceMatchesProcess is not true.\n - Source object coordinate sample does not match ReaderBridge within tolerance.\n",
        "success": false
      }
    },
    "focus": {
      "hwnd": 657876,
      "pid": 11220,
      "status": "foreground_verified",
      "title": "RIFT"
    },
    "movement_test_readiness": {
      "display_status": "PASS",
      "status": "pass"
    },
    "operator_gate": {
      "live_collection_allowed": false,
      "metadata_capture_plan_gate": "PASS",
      "movement_test_readiness": "PASS",
      "old_offsets_trusted": false
    }
  },
  "created_utc": "2026-05-06T10:38:19Z",
  "display_status": "BLOCKED",
  "expires_utc": null,
  "movement_execution_allowed": false,
  "paths": {
    "log": "handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl",
    "report": "handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md",
    "summary": "handoffs/current/movement-execution-gate/movement-execution-gate-summary.json"
  },
  "recommended_command": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100",
  "safety": {
    "capture_started": false,
    "focus_preflight_started": true,
    "memory_scan_or_read_started_by_riftscan": false,
    "movement_execution_gate_only": true,
    "movement_or_input_sent": false,
    "offset_validation_started": false,
    "reloadui_sent": false,
    "riftreader_anchor_preflight_started": true,
    "wrapper_preflight_started": true
  },
  "schema_version": "riftscan.movement_execution_gate.v1",
  "status": "blocked_movement_execution_not_allowed",
  "warnings": []
}
```
