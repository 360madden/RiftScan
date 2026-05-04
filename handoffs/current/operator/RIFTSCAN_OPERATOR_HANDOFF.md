# RiftScan Operator Handoff

Created UTC: `2026-05-04T09:20:31Z`
App version: `riftscan-operator-app-v3.8.2`
Repo root: `C:\RIFT MODDING\Riftscan`

## Operator Assessment

Full live preflight gate: `PASS`
Focus preflight: `PASS`
Summary: `status=foreground_verified pid=29420 hwnd=0x4E0F42 title=RIFT`

- No blocking operator issues detected.

## Git Status

Exit code: `0`

```text
 M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
 M handoffs/current/patch-runner/patch-runner-log.jsonl
 M handoffs/current/patch-runner/patch-runner-output.txt
 M handoffs/current/patch-runner/patch-runner-summary.json
 M patches/apply-latest.ps1
?? handoffs/current/patch-runner/PATCH_RUNNER_ALPHA2_VALIDATOR_HOTFIX.md
?? handoffs/current/patch-runner/patch-runner-alpha2-validator-hotfix-summary.json

```

## Recent Commits

Exit code: `0`

```text
e71035b Update RiftScan operator handoff
c631f36 Update RiftScan operator handoff
80ee201 Update RiftScan operator handoff
1d692fc Validate patch runner alpha2 locally
a18cbc9 Add patch runner alpha2 JSON handoff

```

## Focus Summary JSON

```json
{
  "schema_version": "riftscan.local_focus_control_summary.v1",
  "created_utc": "2026-05-03T09:17:15Z",
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
  "latest_session": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector",
  "manifest_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/capture-session-manifest.json",
  "handoff_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/CAPTURE_SESSION_HANDOFF.md",
  "summary": {
    "schema_version": "riftscan.focus_gated_window_process_metadata_session.v1",
    "app_version": "riftscan-operator-app-v3.6.2",
    "status": "window_process_metadata_collector_completed",
    "scaffold_only": false,
    "capture_mode": "window_process_metadata",
    "duration_target_seconds": 30,
    "stimulus_name": "none_metadata_only",
    "scaffold_window_started": null,
    "scaffold_window_completed": null,
    "real_capture_started": true,
    "real_capture_completed": true,
    "legacy_capture_started": null,
    "legacy_capture_completed": null,
    "focus_before_status": "foreground_verified",
    "focus_after_status": "foreground_verified",
    "process_id": 29420,
    "window_hwnd_hex": "0x4E0F42",
    "window_title": "RIFT",
    "capture_log": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/capture-log.jsonl",
    "collector_samples": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/collector-samples.jsonl",
    "collector_summary": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/collector-summary.json",
    "memory_read_started": false,
    "input_sent": false,
    "reloadui_sent": false,
    "sample_count": 60,
    "error_count": 0,
    "artifact_contract_status": "PASS",
    "missing_artifacts": [],
    "analysis_status": "PASS",
    "analysis_anomaly_count": 0,
    "comparison_status": "PASS",
    "comparison_difference_count": 1
  }
}
```

## Latest Window/Process Analysis

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector",
  "analysis_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/analysis/window-process-analysis.json",
  "handoff_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/analysis/WINDOW_PROCESS_ANALYSIS.md",
  "summary": {
    "status": "PASS",
    "warning_count": 0,
    "error_count": 0,
    "anomaly_count": 0,
    "session": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector",
    "sample_count": 60,
    "expected_sample_count": 60,
    "focus_lost_count": 0,
    "rift_process_dead_count": 0,
    "unique_foreground_hwnds": [
      "0x4E0F42"
    ],
    "unique_foreground_pids": [
      29420
    ],
    "unique_rift_hwnds": [
      "0x4E0F42"
    ],
    "unique_rift_pids": [
      29420
    ],
    "unique_rift_titles": [
      "RIFT"
    ],
    "unique_rift_window_rects": [
      [
        2,
        10,
        657,
        408
      ]
    ],
    "unique_rift_client_rects": [
      [
        0,
        0,
        639,
        359
      ]
    ],
    "missing_sample_index_count": 0,
    "duplicate_sample_index_count": 0,
    "sample_interval_seconds": {
      "expected": 0.5,
      "min": 0.497,
      "max": 0.501,
      "avg": 0.5,
      "max_abs_drift": 0.003
    },
    "artifact_contract_status": "PASS"
  }
}
```

## Latest Window/Process Comparison

```json
{
  "status": "present",
  "latest_session": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector",
  "comparison_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/comparison/window-process-comparison.json",
  "handoff_path": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector/comparison/WINDOW_PROCESS_COMPARISON.md",
  "summary": {
    "status": "PASS",
    "difference_count": 1,
    "warning_count": 1,
    "error_count": 0,
    "previous_session": "sessions/focus-gated-captures/20260503T085611Z_window_process_metadata_collector",
    "latest_session": "sessions/focus-gated-captures/20260503T091644Z_window_process_metadata_collector",
    "previous_analysis_status": null,
    "latest_analysis_status": "PASS",
    "previous_sample_count": 60,
    "latest_sample_count": 60,
    "previous_focus_lost_count": 0,
    "latest_focus_lost_count": 0,
    "previous_artifact_contract_status": "PASS",
    "latest_artifact_contract_status": "PASS"
  }
}
```

## Focus Log Tail

```jsonl
{"timestamp_utc": "2026-05-03T09:17:14Z", "event": "script_start", "script": "C:\\RIFT MODDING\\Riftscan\\tools\\rift_focus_control.py", "repo_root": "C:\\RIFT MODDING\\Riftscan", "process_name": "rift_x64", "explicit_pid": 0, "retries": 3, "settle_ms": 400}
{"timestamp_utc": "2026-05-03T09:17:14Z", "event": "powershell_start", "command": "$items = @(Get-Process -Name 'rift_x64' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); $items | ConvertTo-Json -Depth 4"}
{"timestamp_utc": "2026-05-03T09:17:14Z", "event": "powershell_finish", "success": true, "returncode": 0, "elapsed_ms": 422, "stdout_length": 210, "stderr_length": 0}
{"timestamp_utc": "2026-05-03T09:17:15Z", "event": "focus_attempt", "attempt": 1, "restore_ok": true, "set_foreground_ok": true, "foreground_hwnd": 5115714, "foreground_hwnd_hex": "0x4E0F42", "foreground_pid": 29420, "foreground_title": "RIFT", "verified": true}
{"timestamp_utc": "2026-05-03T09:17:15Z", "event": "script_finish", "success": true, "status": "foreground_verified"}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
```

## Latest Patch Inbox Discovery

```json
{
  "schema_version": "riftscan.patch_inbox_discovery_result.v1",
  "created_utc": "2026-05-04T17:29:48Z",
  "app_version": "riftscan-operator-app-v3.8.5",
  "status": "pass",
  "display_status": "PASS",
  "discovery_only": true,
  "inbox_path": ".riftscan/inbox/patch-packages",
  "result_path": "handoffs/current/repo-bridge/patch-inbox-discovery-result.json",
  "candidate_count": 1,
  "valid_shape_count": 1,
  "invalid_shape_count": 0,
  "issues": [],
  "candidates": [
    {
      "path": ".riftscan/inbox/patch-packages/patch-inbox-discovery-v385/RIFTSCAN_PATCH_PACKAGE.json",
      "parse_status": "parsed",
      "valid_shape": true,
      "issues": [],
      "summary": {
        "schema_version": "riftscan.patch_package.v1",
        "package_id": "patch-inbox-discovery-v385"
      }
    }
  ],
  "guardrails": [
    "Discovery only.",
    "No package download.",
    "No package extraction.",
    "No staging.",
    "No dry-run apply.",
    "No real apply.",
    "No service/listener/polling.",
    "No auto-commit.",
    "No auto-push.",
    "No git add ."
  ]
}
```

## Guardrails

- The full live preflight is conservative: focus + validation + report only.
- The focus-gated session dry run creates session metadata only.
- The focus-gated capture plan is metadata only.
- The focus-gated capture scaffold may open a timed scaffold window, but records focus metadata/log structure only.
- Real capture collector did not run.
- No movement/input sent.
- No memory scan/read started.
- No `/reloadui` sent.
- The helper stages only explicit allowlisted paths; ignored allowlisted artifact paths are force-added explicitly when needed.
- The helper never runs `git add .`.
- Known junk cleanup uses literal paths/globs from the helper configuration.
