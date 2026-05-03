# RiftScan Operator Handoff

Created UTC: `2026-05-03T07:25:24Z`
App version: `riftscan-operator-app-v2`
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

```

## Recent Commits

Exit code: `0`

```text
5f5d335 Add full live preflight to operator app
dbf13dd Update RiftScan operator handoff
b7201b7 Add RiftScan operator helper app launcher
5b6a3c7 Promote RIFT focus control probe to tracked tool
6bf58a5 Update local RIFT focus control handoff with verified foreground

```

## Focus Summary JSON

```json
{
  "schema_version": "riftscan.local_focus_control_summary.v1",
  "created_utc": "2026-05-03T07:21:23Z",
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

## Focus Log Tail

```jsonl
{"timestamp_utc": "2026-05-03T07:21:19Z", "event": "script_start", "script": "C:\\RIFT MODDING\\Riftscan\\tools\\rift_focus_control.py", "repo_root": "C:\\RIFT MODDING\\Riftscan", "process_name": "rift_x64", "explicit_pid": 0, "retries": 3, "settle_ms": 400}
{"timestamp_utc": "2026-05-03T07:21:19Z", "event": "powershell_start", "command": "$items = @(Get-Process -Name 'rift_x64' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); $items | ConvertTo-Json -Depth 4"}
{"timestamp_utc": "2026-05-03T07:21:22Z", "event": "powershell_finish", "success": true, "returncode": 0, "elapsed_ms": 3660, "stdout_length": 210, "stderr_length": 0}
{"timestamp_utc": "2026-05-03T07:21:23Z", "event": "focus_attempt", "attempt": 1, "restore_ok": true, "set_foreground_ok": true, "foreground_hwnd": 5115714, "foreground_hwnd_hex": "0x4E0F42", "foreground_pid": 29420, "foreground_title": "RIFT", "verified": true}
{"timestamp_utc": "2026-05-03T07:21:23Z", "event": "script_finish", "success": true, "status": "foreground_verified"}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
```

## Guardrails

- The full live preflight is conservative: focus + validation + report only.
- The full live preflight does not run movement, input, capture, memory scans, or `/reloadui`.
- The helper stages only explicit allowlisted paths.
- The helper never runs `git add .`.
- Known junk cleanup uses literal paths/globs from the helper configuration.
