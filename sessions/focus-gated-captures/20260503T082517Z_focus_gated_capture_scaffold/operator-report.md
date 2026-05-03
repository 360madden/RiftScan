# RiftScan Operator Handoff

Created UTC: `2026-05-03T08:25:49Z`
App version: `riftscan-operator-app-v3.3`
Repo root: `C:\RIFT MODDING\Riftscan`

## Operator Assessment

Full live preflight gate: `PASS`
Focus preflight: `PASS`
Summary: `status=foreground_verified pid=29420 hwnd=0x4E0F42 title=RIFT`

- No blocking operator issues detected.

## Git Status

Exit code: `0`

```text
 M handoffs/current/focus-control-local/focus-control-log.jsonl
 M handoffs/current/focus-control-local/focus-control-summary.json
 M handoffs/current/focus-control-local/process-command-result.json
 M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
 M tools/riftscan_operator_app.py
?? tools/riftscan_operator_app.py.bak-20260503T082323Z

```

## Recent Commits

Exit code: `0`

```text
fe02a33 Update RiftScan operator handoff
45487bf Update RiftScan operator handoff
640eb62 Update RiftScan operator handoff
5f5d335 Add full live preflight to operator app
dbf13dd Update RiftScan operator handoff

```

## Focus Summary JSON

```json
{
  "schema_version": "riftscan.local_focus_control_summary.v1",
  "created_utc": "2026-05-03T08:25:48Z",
  "status": "foreground_verified",
  "process": {
    "Id": 29420,
    "ProcessName": "rift_x64",
    "Path": "C:\\Program Files (x86)\\Glyph\\Games\\RIFT\\Live\\rift_x64.exe",
    "MainWindowTitle": "RIFT",
    "StartTime": "/Date(1777764916054)/"
  },
  "selected_window": {
    "hwnd": 5115714,
    "hwnd_hex": "0x4E0F42",
    "pid": 29420,
    "title": "RIFT"
  },
  "focus": {
    "success": true,
    "attempts": [
      {
        "attempt": 1,
        "restore_ok": true,
        "set_foreground_ok": true,
        "foreground_hwnd": 5115714,
        "foreground_hwnd_hex": "0x4E0F42",
        "foreground_pid": 29420,
        "foreground_title": "RIFT",
        "verified": true
      }
    ]
  },
  "notes": [
    "This local probe uses Win32 foreground APIs.",
    "It does not click the mouse.",
    "It does not send keyboard input.",
    "It does not run /reloadui."
  ]
}
```

## Windows JSON

```json
{
  "pid": 29420,
  "windows": [
    {
      "hwnd": 5115714,
      "hwnd_hex": "0x4E0F42",
      "pid": 29420,
      "title": "RIFT"
    }
  ]
}
```

## Latest Focus-Gated Session Dry Run

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-dry-runs/20260503T075023Z_focus_gated_session_dry_run",
  "manifest_path": "sessions/focus-gated-dry-runs/20260503T075023Z_focus_gated_session_dry_run/manifest.json",
  "manifest": {
    "schema_version": "riftscan.focus_gated_session_dry_run.v1",
    "created_utc": "2026-05-03T07:50:23Z",
    "app_version": "riftscan-operator-app-v3",
    "session_id": "20260503T075023Z_focus_gated_session_dry_run",
    "status": "dry_run_session_created",
    "dry_run": true,
    "full_live_preflight": {
      "status": "PASS",
      "focus_status": "foreground_verified",
      "process_id": 29420,
      "process_name": "rift_x64",
      "window_hwnd": 5115714,
      "window_hwnd_hex": "0x4E0F42",
      "window_title": "RIFT",
      "windows_count": 1
    },
    "guardrails": [
      "No live test sequence was started.",
      "No local data collection sequence was started.",
      "This session is metadata-only."
    ],
    "source_artifacts": {
      "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
      "windows_json": "handoffs/current/focus-control-local/windows.json",
      "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
      "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md"
    },
    "next_expected_step": "Use this metadata-only session structure as the staging contract before wiring the first real focus-gated live-test workflow."
  }
}
```

## Latest Focus-Gated Capture Plan

```json
{
  "status": "present",
  "latest_plan": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan",
  "manifest_path": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan/capture-plan.json",
  "handoff_path": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
  "manifest": {
    "schema_version": "riftscan.focus_gated_capture_plan.v1",
    "created_utc": "2026-05-03T08:16:41Z",
    "app_version": "riftscan-operator-app-v3.2",
    "plan_id": "20260503T081641Z_focus_gated_capture_plan",
    "status": "capture_plan_created",
    "metadata_only": true,
    "capture_started": false,
    "capture_completed": false,
    "capture_type": "focus_gated_manual_observation",
    "duration_target_seconds": 30,
    "stimulus_name": "none_metadata_only",
    "expected_files": [
      "capture-session-manifest.json",
      "capture-log.jsonl",
      "focus-summary-before.json",
      "focus-summary-after.json",
      "operator-report.md"
    ],
    "preflight_requirements": [
      "Full live preflight gate PASS",
      "Focus status == foreground_verified",
      "selected_window exists",
      "windows.json has at least one window",
      "RIFT process name == rift_x64",
      "Operator app is the controlling workflow",
      "No uncommitted tool-code changes unless intentionally testing new tool code"
    ],
    "abort_conditions": [
      "Focus preflight fails",
      "RIFT process missing",
      "RIFT HWND missing",
      "Foreground HWND does not belong to RIFT",
      "Git state cannot be read",
      "Operator cancels",
      "Any planned capture file path already exists unexpectedly",
      "Any live-capture command would be required at this stage"
    ],
    "operator_notes": "Metadata-only plan generated by RiftScan Operator. This does not start capture. Use as staging contract before implementing real focus-gated capture.",
    "full_live_preflight": {
      "status": "PASS",
      "focus_status": "foreground_verified",
      "process_id": 29420,
      "process_name": "rift_x64",
      "window_hwnd": 5115714,
      "window_hwnd_hex": "0x4E0F42",
      "window_title": "RIFT",
      "windows_count": 1
    },
    "source_artifacts": {
      "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
      "windows_json": "handoffs/current/focus-control-local/windows.json",
      "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
      "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
      "latest_dry_run_pointer": "sessions/focus-gated-dry-runs/LATEST_DRY_RUN.txt"
    },
    "guardrails": [
      "Metadata only.",
      "No capture started.",
      "No live test sequence started.",
      "No local data collection sequence started.",
      "No movement/input sent.",
      "No memory scan/read started.",
      "No /reloadui sent."
    ],
    "next_expected_step": "Use this capture plan as the staging contract before implementing real focus-gated capture."
  }
}
```

## Latest Focus-Gated Capture Session

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold",
  "manifest_path": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/capture-session-manifest.json",
  "handoff_path": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/CAPTURE_SESSION_HANDOFF.md",
  "manifest": {
    "schema_version": "riftscan.focus_gated_capture_session_scaffold.v1",
    "created_utc": "2026-05-03T08:24:21Z",
    "app_version": "riftscan-operator-app-v3.3",
    "session_id": "20260503T082421Z_focus_gated_capture_scaffold",
    "status": "capture_scaffold_completed",
    "scaffold_only": true,
    "capture_started": true,
    "capture_completed": true,
    "capture_mode": "focus_metadata_only_scaffold",
    "duration_target_seconds": 30,
    "stimulus_name": "none_metadata_only",
    "source_capture_plan": {
      "status": "present",
      "latest_plan": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan",
      "manifest_path": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan/capture-plan.json",
      "handoff_path": "plans/focus-gated-capture-plans/20260503T081641Z_focus_gated_capture_plan/CAPTURE_PLAN_HANDOFF.md",
      "manifest": {
        "schema_version": "riftscan.focus_gated_capture_plan.v1",
        "created_utc": "2026-05-03T08:16:41Z",
        "app_version": "riftscan-operator-app-v3.2",
        "plan_id": "20260503T081641Z_focus_gated_capture_plan",
        "status": "capture_plan_created",
        "metadata_only": true,
        "capture_started": false,
        "capture_completed": false,
        "capture_type": "focus_gated_manual_observation",
        "duration_target_seconds": 30,
        "stimulus_name": "none_metadata_only",
        "expected_files": [
          "capture-session-manifest.json",
          "capture-log.jsonl",
          "focus-summary-before.json",
          "focus-summary-after.json",
          "operator-report.md"
        ],
        "preflight_requirements": [
          "Full live preflight gate PASS",
          "Focus status == foreground_verified",
          "selected_window exists",
          "windows.json has at least one window",
          "RIFT process name == rift_x64",
          "Operator app is the controlling workflow",
          "No uncommitted tool-code changes unless intentionally testing new tool code"
        ],
        "abort_conditions": [
          "Focus preflight fails",
          "RIFT process missing",
          "RIFT HWND missing",
          "Foreground HWND does not belong to RIFT",
          "Git state cannot be read",
          "Operator cancels",
          "Any planned capture file path already exists unexpectedly",
          "Any live-capture command would be required at this stage"
        ],
        "operator_notes": "Metadata-only plan generated by RiftScan Operator. This does not start capture. Use as staging contract before implementing real focus-gated capture.",
        "full_live_preflight": {
          "status": "PASS",
          "focus_status": "foreground_verified",
          "process_id": 29420,
          "process_name": "rift_x64",
          "window_hwnd": 5115714,
          "window_hwnd_hex": "0x4E0F42",
          "window_title": "RIFT",
          "windows_count": 1
        },
        "source_artifacts": {
          "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
          "windows_json": "handoffs/current/focus-control-local/windows.json",
          "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
          "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
          "latest_dry_run_pointer": "sessions/focus-gated-dry-runs/LATEST_DRY_RUN.txt"
        },
        "guardrails": [
          "Metadata only.",
          "No capture started.",
          "No live test sequence started.",
          "No local data collection sequence started.",
          "No movement/input sent.",
          "No memory scan/read started.",
          "No /reloadui sent."
        ],
        "next_expected_step": "Use this capture plan as the staging contract before implementing real focus-gated capture."
      }
    },
    "full_live_preflight": {
      "status": "PASS",
      "focus_status": "foreground_verified",
      "process_id": 29420,
      "process_name": "rift_x64",
      "window_hwnd": 5115714,
      "window_hwnd_hex": "0x4E0F42",
      "window_title": "RIFT",
      "windows_count": 1
    },
    "files": {
      "capture_session_manifest": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/capture-session-manifest.json",
      "capture_log": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/capture-log.jsonl",
      "focus_summary_before": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/focus-summary-before.json",
      "focus_summary_after": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/focus-summary-after.json",
      "operator_report": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/operator-report.md",
      "handoff": "sessions/focus-gated-captures/20260503T082421Z_focus_gated_capture_scaffold/CAPTURE_SESSION_HANDOFF.md"
    },
    "guardrails": [
      "Timed capture scaffold only.",
      "Focus metadata/log structure only.",
      "No movement/input sent.",
      "No memory scan/read started.",
      "No /reloadui sent."
    ],
    "completed_utc": "2026-05-03T08:24:52Z",
    "elapsed_seconds": 31.132,
    "focus_after": {
      "command_exit_code": 0,
      "stdout": "Focus control handoff written to C:\\RIFT MODDING\\Riftscan\\handoffs\\current\\focus-control-local",
      "stderr": "",
      "focus_status": "foreground_verified"
    },
    "next_expected_step": "Review scaffold artifacts, then wire the first real collector behind this same focus gate."
  }
}
```

## Focus Log Tail

```jsonl
{"timestamp_utc": "2026-05-03T08:25:48Z", "event": "script_start", "script": "C:\\RIFT MODDING\\Riftscan\\tools\\rift_focus_control.py", "repo_root": "C:\\RIFT MODDING\\Riftscan", "process_name": "rift_x64", "explicit_pid": 0, "retries": 3, "settle_ms": 400}
{"timestamp_utc": "2026-05-03T08:25:48Z", "event": "powershell_start", "command": "$items = @(Get-Process -Name 'rift_x64' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); $items | ConvertTo-Json -Depth 4"}
{"timestamp_utc": "2026-05-03T08:25:48Z", "event": "powershell_finish", "success": true, "returncode": 0, "elapsed_ms": 389, "stdout_length": 210, "stderr_length": 0}
{"timestamp_utc": "2026-05-03T08:25:48Z", "event": "focus_attempt", "attempt": 1, "restore_ok": true, "set_foreground_ok": true, "foreground_hwnd": 5115714, "foreground_hwnd_hex": "0x4E0F42", "foreground_pid": 29420, "foreground_title": "RIFT", "verified": true}
{"timestamp_utc": "2026-05-03T08:25:48Z", "event": "script_finish", "success": true, "status": "foreground_verified"}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
```

## Guardrails

- The full live preflight is conservative: focus + validation + report only.
- The focus-gated session dry run creates session metadata only.
- The focus-gated capture plan is metadata only.
- The focus-gated capture scaffold may open a timed session, but records focus metadata/log structure only.
- No movement/input sent.
- No memory scan/read started.
- No `/reloadui` sent.
- The helper stages only explicit allowlisted paths; ignored allowlisted artifact paths are force-added explicitly when needed.
- The helper never runs `git add .`.
- Known junk cleanup uses literal paths/globs from the helper configuration.
