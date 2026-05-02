# version: 0.1.0
# total_character_count: computed_by_py_compile_test
# purpose: Read-only diagnostic probe that reports whether the live RIFT window is currently foreground; it does not change focus or send input.

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
HANDOFF_ROOT = REPO_ROOT / "handoffs" / "current" / "foreground-status"
LOG_PATH = HANDOFF_ROOT / "step-log.jsonl"
DEFAULT_PROCESS_NAME = "rift_x64"


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def display_path(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT.resolve())).replace("\\", "/")
    except ValueError:
        return str(path)


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding="utf-8")


def write_text(path: Path, value: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8")


def log_event(event: str, **fields: Any) -> None:
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    entry = {"timestamp_utc": utc_now_iso(), "event": event, **fields}
    with LOG_PATH.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(entry, ensure_ascii=False) + "\n")


def reset_handoff() -> None:
    if HANDOFF_ROOT.exists():
        shutil.rmtree(HANDOFF_ROOT)
    HANDOFF_ROOT.mkdir(parents=True, exist_ok=True)
    log_event("handoff_reset", handoff_root=display_path(HANDOFF_ROOT))


def command_for_platform(args: list[str]) -> list[str]:
    if os.name == "nt":
        return ["cmd.exe", "/c", *args]
    return args


def run_command(args: list[str], timeout_seconds: int) -> dict[str, Any]:
    executed_args = command_for_platform(args)
    started_utc = utc_now_iso()
    started_monotonic = time.monotonic()
    log_event("command_start", args=args, executed_args=executed_args)
    try:
        result = subprocess.run(
            executed_args,
            cwd=str(REPO_ROOT),
            text=True,
            capture_output=True,
            shell=False,
            timeout=timeout_seconds,
        )
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": result.returncode == 0,
            "returncode": result.returncode,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": executed_args,
            "stdout": result.stdout,
            "stderr": result.stderr,
        }
        log_event("command_finish", success=payload["success"], returncode=result.returncode, elapsed_ms=elapsed_ms)
        return payload
    except Exception as exc:
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": executed_args,
            "stdout": "",
            "stderr": "",
            "exception_type": type(exc).__name__,
            "exception_message": str(exc),
        }
        log_event("command_exception", error_type=type(exc).__name__, error=str(exc), elapsed_ms=elapsed_ms)
        return payload


def foreground_status_script(process_name: str) -> str:
    return rf'''
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class ForegroundProbe {{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}}
"@
$foregroundHwnd = [ForegroundProbe]::GetForegroundWindow()
[uint32]$foregroundPid = 0
[void][ForegroundProbe]::GetWindowThreadProcessId($foregroundHwnd, [ref]$foregroundPid)
$foregroundProcess = $null
if ($foregroundPid -gt 0) {{
    $foregroundProcess = Get-Process -Id ([int]$foregroundPid) -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime
}}
$riftProcesses = @(Get-Process -Name '{process_name}' -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime)
$result = [ordered]@{{
    foreground_hwnd = ('0x{{0:X}}' -f $foregroundHwnd.ToInt64())
    foreground_pid = [int]$foregroundPid
    foreground_process = $foregroundProcess
    rift_processes = $riftProcesses
    rift_process_count = $riftProcesses.Count
    rift_foreground = $false
}}
foreach ($proc in $riftProcesses) {{
    if ($proc.Id -eq [int]$foregroundPid) {{
        $result.rift_foreground = $true
    }}
}}
$result | ConvertTo-Json -Depth 6
'''


def parse_json_stdout(command_result: dict[str, Any]) -> Any | None:
    stdout = command_result.get("stdout")
    if not isinstance(stdout, str) or not stdout.strip():
        return None
    try:
        return json.loads(stdout)
    except json.JSONDecodeError:
        return None


def build_summary(status: str, parsed: Any, command_result: dict[str, Any]) -> dict[str, Any]:
    rift_foreground = None
    foreground_pid = None
    rift_process_count = None
    if isinstance(parsed, dict):
        rift_foreground = parsed.get("rift_foreground")
        foreground_pid = parsed.get("foreground_pid")
        rift_process_count = parsed.get("rift_process_count")
    return {
        "schema_version": "riftscan.foreground_status_probe_summary.v1",
        "created_utc": utc_now_iso(),
        "status": status,
        "read_only": True,
        "changed_foreground": False,
        "sent_input": False,
        "clicked_mouse": False,
        "command_success": bool(command_result.get("success")),
        "returncode": command_result.get("returncode"),
        "foreground_pid": foreground_pid,
        "rift_process_count": rift_process_count,
        "rift_foreground": rift_foreground,
        "log_path": display_path(LOG_PATH),
        "notes": [
            "This probe is read-only.",
            "It does not call SetForegroundWindow.",
            "It does not send keyboard or mouse input.",
            "It only reports whether RIFT is already foreground.",
        ],
    }


def build_markdown(summary: dict[str, Any]) -> str:
    return f"""# RIFT Foreground Status Probe

## Purpose

Read-only diagnostic that reports whether RIFT is currently the foreground window.

## Result

- Status: `{summary.get('status')}`
- RIFT foreground: `{summary.get('rift_foreground')}`
- Foreground PID: `{summary.get('foreground_pid')}`
- RIFT process count: `{summary.get('rift_process_count')}`

## Safety

- Does not change focus.
- Does not call SetForegroundWindow.
- Does not click.
- Does not send keyboard input.
- Does not run `/reloadui`.

## Files

- `foreground-status-summary.json`
- `foreground-status.json`
- `command-result.json`
- `step-log.jsonl`
"""


def write_file_lists(max_file_bytes: int) -> None:
    included: list[dict[str, Any]] = []
    excluded: list[dict[str, Any]] = []
    for path in sorted(HANDOFF_ROOT.rglob("*")):
        if not path.is_file():
            continue
        item = {"path": display_path(path), "bytes": path.stat().st_size}
        if path.stat().st_size <= max_file_bytes and path.suffix.lower() not in {".bin", ".raw", ".dump", ".dmp"}:
            included.append(item)
        else:
            excluded.append(item)
    write_json(HANDOFF_ROOT / "files-included.json", included)
    write_json(HANDOFF_ROOT / "files-excluded.json", excluded)


def run_self_check(args: argparse.Namespace) -> int:
    reset_handoff()
    fake_result = {
        "schema_version": "riftscan.command_result.v1",
        "success": True,
        "returncode": 0,
        "stdout": json.dumps({"foreground_pid": 123, "rift_process_count": 1, "rift_foreground": True}),
        "stderr": "",
    }
    parsed = parse_json_stdout(fake_result)
    summary = build_summary("self_check_passed", parsed, fake_result)
    write_json(HANDOFF_ROOT / "foreground-status-summary.json", summary)
    write_json(HANDOFF_ROOT / "foreground-status.json", parsed)
    write_json(HANDOFF_ROOT / "command-result.json", fake_result)
    write_text(HANDOFF_ROOT / "FOREGROUND_STATUS_HANDOFF.md", build_markdown(summary))
    fake_raw = HANDOFF_ROOT / "fake.bin"
    fake_raw.write_bytes(b"excluded")
    write_file_lists(args.max_file_bytes)
    print(f"Self-check passed. Handoff written to {HANDOFF_ROOT}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Read-only RIFT foreground status probe.")
    parser.add_argument("--process-name", default=DEFAULT_PROCESS_NAME)
    parser.add_argument("--timeout-seconds", type=int, default=30)
    parser.add_argument("--max-file-bytes", type=int, default=5_000_000)
    parser.add_argument("--self-check", action="store_true")
    args = parser.parse_args()

    if args.self_check:
        return run_self_check(args)

    reset_handoff()
    log_event("script_start", script=display_path(SCRIPT_PATH), process_name=args.process_name)

    if os.name != "nt":
        command_result = {"success": False, "returncode": None, "stdout": "", "stderr": "unsupported_non_windows"}
        parsed = None
        status = "unsupported_non_windows"
    else:
        ps_script = foreground_status_script(args.process_name)
        command_result = run_command(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", ps_script], args.timeout_seconds)
        parsed = parse_json_stdout(command_result)
        if command_result.get("success") and isinstance(parsed, dict):
            status = "rift_is_foreground" if parsed.get("rift_foreground") is True else "rift_is_not_foreground"
        elif command_result.get("success"):
            status = "status_json_unparsed"
        else:
            status = "status_command_failed"

    summary = build_summary(status, parsed, command_result)
    write_json(HANDOFF_ROOT / "foreground-status-summary.json", summary)
    write_json(HANDOFF_ROOT / "foreground-status.json", parsed)
    write_json(HANDOFF_ROOT / "command-result.json", command_result)
    write_text(HANDOFF_ROOT / "FOREGROUND_STATUS_HANDOFF.md", build_markdown(summary))
    write_file_lists(args.max_file_bytes)
    log_event("script_finish", status=status, rift_foreground=summary.get("rift_foreground"))
    print(f"Foreground status handoff written to {HANDOFF_ROOT}")
    return 0 if command_result.get("success") else 1


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT_MARKER
