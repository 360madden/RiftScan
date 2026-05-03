# version: 0.1.2
# total_character_count: computed_by_packager
# purpose: Local-only RIFT foreground control probe. Finds the live RIFT window, restores it, requests foreground via Win32 APIs, verifies foreground PID, and writes diagnostic JSON. No mouse clicks and no keyboard input.

from __future__ import annotations

import argparse
import ctypes
import json
import os
import subprocess
import time
from ctypes import wintypes
# Version: focus-local-wndenumproc-fix-v1
# Purpose: Define the Win32 EnumWindows callback type locally because ctypes.wintypes does not expose WNDENUMPROC.
# Character count: 181
WNDENUMPROC = ctypes.WINFUNCTYPE(
    wintypes.BOOL,
    wintypes.HWND,
    wintypes.LPARAM,
)
# End patch block
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

DEFAULT_REPO_ROOT = Path(r"C:\RIFT MODDING\Riftscan")
DEFAULT_PROCESS_NAME = "rift_x64"
SW_RESTORE = 9


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding="utf-8")


def write_text(path: Path, value: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8")


def log_event(log_path: Path, event: str, **fields: Any) -> None:
    log_path.parent.mkdir(parents=True, exist_ok=True)
    entry = {"timestamp_utc": utc_now_iso(), "event": event, **fields}
    with log_path.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(entry, ensure_ascii=False) + "\n")


def display_path(repo_root: Path, path: Path) -> str:
    try:
        return str(path.resolve().relative_to(repo_root.resolve())).replace("\\", "/")
    except ValueError:
        return str(path)


def safe_get_dict(value: Any) -> dict[str, Any]:
    return value if isinstance(value, dict) else {}


def run_powershell(repo_root: Path, log_path: Path, command: str, timeout_seconds: int) -> dict[str, Any]:
    started = utc_now_iso()
    start = time.monotonic()
    args = ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command]
    log_event(log_path, "powershell_start", command=command)

    try:
        result = subprocess.run(
            args,
            cwd=str(repo_root),
            text=True,
            capture_output=True,
            shell=False,
            timeout=timeout_seconds,
        )
        elapsed_ms = int((time.monotonic() - start) * 1000)
        payload = {
            "started_utc": started,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": result.returncode == 0,
            "returncode": result.returncode,
            "args": args,
            "stdout": result.stdout,
            "stderr": result.stderr,
        }
        log_event(
            log_path,
            "powershell_finish",
            success=payload["success"],
            returncode=result.returncode,
            elapsed_ms=elapsed_ms,
            stdout_length=len(result.stdout),
            stderr_length=len(result.stderr),
        )
        return payload
    except Exception as exc:
        elapsed_ms = int((time.monotonic() - start) * 1000)
        payload = {
            "started_utc": started,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "args": args,
            "stdout": "",
            "stderr": "",
            "exception_type": type(exc).__name__,
            "exception_message": str(exc),
        }
        log_event(log_path, "powershell_exception", error_type=type(exc).__name__, error=str(exc))
        return payload


def resolve_processes(repo_root: Path, out_dir: Path, log_path: Path, process_name: str, timeout_seconds: int) -> list[dict[str, Any]]:
    ps = (
        f"$items = @(Get-Process -Name '{process_name}' -ErrorAction SilentlyContinue | "
        "Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); "
        "$items | ConvertTo-Json -Depth 4"
    )
    result = run_powershell(repo_root, log_path, ps, timeout_seconds)
    write_json(out_dir / "process-command-result.json", result)

    if not result.get("success"):
        return []

    stdout = result.get("stdout", "").strip()
    if not stdout:
        return []

    try:
        parsed = json.loads(stdout)
    except json.JSONDecodeError as exc:
        log_event(log_path, "process_json_parse_failed", error=str(exc), stdout=stdout[:500])
        return []

    if isinstance(parsed, list):
        return [item for item in parsed if isinstance(item, dict)]
    if isinstance(parsed, dict):
        return [parsed]
    return []


def load_user32() -> Any:
    if os.name != "nt":
        raise RuntimeError("This script requires Windows.")

    user32 = ctypes.WinDLL("user32", use_last_error=True)

    user32.IsWindowVisible.argtypes = [wintypes.HWND]
    user32.IsWindowVisible.restype = wintypes.BOOL

    user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
    user32.GetWindowTextLengthW.restype = ctypes.c_int

    user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
    user32.GetWindowTextW.restype = ctypes.c_int

    user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user32.GetWindowThreadProcessId.restype = wintypes.DWORD

    user32.ShowWindow.argtypes = [wintypes.HWND, ctypes.c_int]
    user32.ShowWindow.restype = wintypes.BOOL

    user32.SetForegroundWindow.argtypes = [wintypes.HWND]
    user32.SetForegroundWindow.restype = wintypes.BOOL

    user32.GetForegroundWindow.argtypes = []
    user32.GetForegroundWindow.restype = wintypes.HWND

    return user32


def get_window_pid(user32: Any, hwnd: int) -> int:
    pid = wintypes.DWORD()
    user32.GetWindowThreadProcessId(wintypes.HWND(hwnd), ctypes.byref(pid))
    return int(pid.value)


def get_window_title(user32: Any, hwnd: int) -> str:
    length = user32.GetWindowTextLengthW(wintypes.HWND(hwnd))
    if length <= 0:
        return ""
    buffer = ctypes.create_unicode_buffer(length + 1)
    user32.GetWindowTextW(wintypes.HWND(hwnd), buffer, length + 1)
    return buffer.value


def find_windows_for_pid(pid: int) -> list[dict[str, Any]]:
    user32 = load_user32()
    windows: list[dict[str, Any]] = []

    @WNDENUMPROC
    def enum_proc(hwnd: wintypes.HWND, _lparam: wintypes.LPARAM) -> bool:
        hwnd_int = int(hwnd)
        if not user32.IsWindowVisible(wintypes.HWND(hwnd_int)):
            return True

        window_pid = get_window_pid(user32, hwnd_int)
        if window_pid != pid:
            return True

        title = get_window_title(user32, hwnd_int)
        windows.append(
            {
                "hwnd": hwnd_int,
                "hwnd_hex": f"0x{hwnd_int:X}",
                "pid": window_pid,
                "title": title,
            }
        )
        return True

    if not user32.EnumWindows(enum_proc, 0):
        raise ctypes.WinError(ctypes.get_last_error())

    return windows


def request_foreground(log_path: Path, hwnd: int, target_pid: int, retries: int, settle_ms: int) -> dict[str, Any]:
    user32 = load_user32()
    attempts: list[dict[str, Any]] = []

    for attempt in range(1, retries + 1):
        restore_ok = bool(user32.ShowWindow(wintypes.HWND(hwnd), SW_RESTORE))
        foreground_ok = bool(user32.SetForegroundWindow(wintypes.HWND(hwnd)))
        time.sleep(max(settle_ms, 0) / 1000)

        foreground_hwnd = int(user32.GetForegroundWindow())
        foreground_pid = get_window_pid(user32, foreground_hwnd) if foreground_hwnd else 0
        foreground_title = get_window_title(user32, foreground_hwnd) if foreground_hwnd else ""

        result = {
            "attempt": attempt,
            "restore_ok": restore_ok,
            "set_foreground_ok": foreground_ok,
            "foreground_hwnd": foreground_hwnd,
            "foreground_hwnd_hex": f"0x{foreground_hwnd:X}" if foreground_hwnd else None,
            "foreground_pid": foreground_pid,
            "foreground_title": foreground_title,
            "verified": foreground_pid == target_pid,
        }
        attempts.append(result)
        log_event(log_path, "focus_attempt", **result)

        if result["verified"]:
            return {"success": True, "attempts": attempts}

    return {"success": False, "attempts": attempts}


def choose_process(processes: list[dict[str, Any]], pid: int) -> tuple[dict[str, Any] | None, list[str]]:
    issues: list[str] = []

    if pid > 0:
        for proc in processes:
            if int(proc.get("Id", -1)) == pid:
                return proc, issues
        return {"Id": pid, "ProcessName": None, "Path": None, "MainWindowTitle": None}, issues

    if not processes:
        issues.append("no_rift_process_found")
        return None, issues

    if len(processes) > 1:
        issues.append("multiple_rift_processes_found_pass_explicit_pid")
        return None, issues

    return processes[0], issues


def build_markdown(summary: dict[str, Any]) -> str:
    process = safe_get_dict(summary.get("process"))
    selected_window = safe_get_dict(summary.get("selected_window"))
    focus = safe_get_dict(summary.get("focus"))
    return f"""# Local RIFT Focus Control Probe

## Purpose

Local-only probe that requests foreground for the RIFT window using Win32 APIs.

## Result

- Status: `{summary.get("status")}`
- Process ID: `{process.get("Id")}`
- Selected HWND: `{selected_window.get("hwnd_hex")}`
- Focus verified: `{focus.get("success")}`

## Safety

- No mouse clicking.
- No title-bar clicking.
- No keyboard input.
- No `/reloadui`.
- No movement.

## Files

- `focus-control-summary.json`
- `process-info.json`
- `windows.json`
- `focus-result.json`
- `focus-control-log.jsonl`
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Local-only RIFT foreground control probe.")
    parser.add_argument("--repo", default=str(DEFAULT_REPO_ROOT))
    parser.add_argument("--process-name", default=DEFAULT_PROCESS_NAME)
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--timeout-seconds", type=int, default=30)
    parser.add_argument("--retries", type=int, default=3)
    parser.add_argument("--settle-ms", type=int, default=400)
    args = parser.parse_args()

    repo_root = Path(args.repo)
    out_dir = repo_root / "handoffs" / "current" / "focus-control-local"
    log_path = out_dir / "focus-control-log.jsonl"

    if out_dir.exists():
        import shutil
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    log_event(
        log_path,
        "script_start",
        script=str(Path(__file__).resolve()),
        repo_root=str(repo_root),
        process_name=args.process_name,
        explicit_pid=args.pid,
        retries=args.retries,
        settle_ms=args.settle_ms,
    )

    if os.name != "nt":
        summary = {
            "schema_version": "riftscan.local_focus_control_summary.v1",
            "created_utc": utc_now_iso(),
            "status": "unsupported_non_windows",
            "process": None,
            "selected_window": None,
            "focus": {"success": False},
        }
        write_json(out_dir / "focus-control-summary.json", summary)
        write_text(out_dir / "FOCUS_CONTROL_HANDOFF.md", build_markdown(summary))
        return 1

    processes = resolve_processes(repo_root, out_dir, log_path, args.process_name, args.timeout_seconds)
    process, issues = choose_process(processes, args.pid)

    write_json(
        out_dir / "process-info.json",
        {"processes": processes, "selected": process, "issues": issues},
    )

    if process is None or issues:
        summary = {
            "schema_version": "riftscan.local_focus_control_summary.v1",
            "created_utc": utc_now_iso(),
            "status": "process_resolution_failed",
            "process": process,
            "selected_window": None,
            "focus": {"success": False},
            "issues": issues,
        }
        write_json(out_dir / "focus-control-summary.json", summary)
        write_text(out_dir / "FOCUS_CONTROL_HANDOFF.md", build_markdown(summary))
        log_event(log_path, "script_finish", success=False, reason="process_resolution_failed", issues=issues)
        print(f"Focus control failed during process resolution. Handoff: {out_dir}")
        return 1

    pid = int(process["Id"])

    try:
        windows = find_windows_for_pid(pid)
    except Exception as exc:
        windows = []
        log_event(log_path, "window_enumeration_failed", error_type=type(exc).__name__, error=str(exc))

    write_json(out_dir / "windows.json", {"pid": pid, "windows": windows})

    titled = [window for window in windows if str(window.get("title", "")).strip()]
    selected_window = titled[0] if titled else (windows[0] if windows else None)

    if selected_window is None:
        focus_result = {"success": False, "reason": "no_visible_window_for_pid", "attempts": []}
    else:
        focus_result = request_foreground(
            log_path=log_path,
            hwnd=int(selected_window["hwnd"]),
            target_pid=pid,
            retries=args.retries,
            settle_ms=args.settle_ms,
        )

    write_json(out_dir / "focus-result.json", focus_result)

    status = "foreground_verified" if focus_result.get("success") else "foreground_not_verified"
    summary = {
        "schema_version": "riftscan.local_focus_control_summary.v1",
        "created_utc": utc_now_iso(),
        "status": status,
        "process": process,
        "selected_window": selected_window,
        "focus": focus_result,
        "notes": [
            "This local probe uses Win32 foreground APIs.",
            "It does not click the mouse.",
            "It does not send keyboard input.",
            "It does not run /reloadui.",
        ],
    }
    write_json(out_dir / "focus-control-summary.json", summary)
    write_text(out_dir / "FOCUS_CONTROL_HANDOFF.md", build_markdown(summary))

    log_event(log_path, "script_finish", success=bool(focus_result.get("success")), status=status)
    print(f"Focus control handoff written to {out_dir}")
    return 0 if focus_result.get("success") else 1


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT_MARKER



