# Version: riftscan-operator-app-v3.7
# Purpose: Windows Tkinter helper app for RiftScan operator workflow: run focus preflight, run full live preflight gate, create focus-gated capture plans, run the focus-gated window/process metadata collector, analyze latest collector sessions, validate collector artifacts, write compact AI-ready reports, clean known junk, safely commit/push allowlisted files, and provide tabbed/wrapped controls with lightweight status highlighting.
# Total character count: 101692

from __future__ import annotations

import ctypes
import datetime as dt
from ctypes import wintypes
import json
import os
import re
import shutil
import subprocess
import threading
import time
import tkinter as tk
from pathlib import Path
from tkinter import messagebox, scrolledtext, ttk
from typing import Any


APP_VERSION = "riftscan-operator-app-v3.7"
REPO_ROOT = Path(__file__).resolve().parents[1]
FOCUS_SCRIPT = REPO_ROOT / "scripts" / "run-rift-focus-control.cmd"
HANDOFF_DIR = REPO_ROOT / "handoffs" / "current" / "focus-control-local"
OPERATOR_DIR = REPO_ROOT / "handoffs" / "current" / "operator"
REPORT_PATH = OPERATOR_DIR / "RIFTSCAN_OPERATOR_HANDOFF.md"
FOCUS_SUMMARY = HANDOFF_DIR / "focus-control-summary.json"
WINDOWS_JSON = HANDOFF_DIR / "windows.json"
FOCUS_LOG = HANDOFF_DIR / "focus-control-log.jsonl"
DRY_RUN_ROOT = REPO_ROOT / "sessions" / "focus-gated-dry-runs"
LATEST_DRY_RUN = DRY_RUN_ROOT / "LATEST_DRY_RUN.txt"
CAPTURE_PLAN_ROOT = REPO_ROOT / "plans" / "focus-gated-capture-plans"
LATEST_CAPTURE_PLAN = CAPTURE_PLAN_ROOT / "LATEST_CAPTURE_PLAN.txt"
CAPTURE_SESSION_ROOT = REPO_ROOT / "sessions" / "focus-gated-captures"
LATEST_CAPTURE_SESSION = CAPTURE_SESSION_ROOT / "LATEST_CAPTURE_SESSION.txt"
LOG_SCHEMA_VERSION = "riftscan.capture_log.v1"
WINDOW_PROCESS_COLLECTOR_SCHEMA_VERSION = "riftscan.window_process_metadata_collector.v1"
ANALYSIS_SCHEMA_VERSION = "riftscan.window_process_analysis.v1"
DEFAULT_WINDOW_PROCESS_SAMPLE_INTERVAL_MS = 500

ALLOWLIST = [
    "handoffs/current/focus-control-local",
    "handoffs/current/operator",
    "scripts/run-rift-focus-control.cmd",
    "scripts/riftscan-operator-app.cmd",
    "tools/rift_focus_control.py",
    "tools/riftscan_operator_app.py",
    "plans/focus-gated-capture-plans",
]

FORCE_ADD_ALLOWLIST = [
    "sessions/focus-gated-dry-runs",
    "sessions/focus-gated-captures",
]

JUNK_LITERAL = [
    "None",
    "dict[str",
    "list[dict[str",
    "str",
    "README.txt",
    "README_INSTALL.md",
    "install-riftscan-operator-app.cmd",
    "README_INSTALL_v3.md",
    "README_INSTALL_v31.md",
    "install-riftscan-operator-app-v3.cmd",
    "install-riftscan-operator-app-v31.cmd",
    "RiftScan_Operator_App_v31_Dry_Run_Commit_Hotfix.zip",
    "RIFTSCAN_apply_focus_gated_capture_plan_patch.py",
    "RIFTSCAN_apply_focus_gated_capture_scaffold_patch.py",
    "RIFTSCAN_apply_operator_v34_hardening_patch.py",
    "RIFTSCAN_apply_operator_v34_hardening_patch_v11.py",
    "RIFTSCAN_apply_operator_v35_window_process_collector_patch.py",
    "RIFTSCAN_apply_operator_v351_artifact_contract_patch.py",
    "RIFTSCAN_apply_operator_v36_ui_tabs_highlighting_patch.py",
    "RIFTSCAN_apply_operator_v36_ui_tabs_highlighting_patch_v11.py",
    "RIFTSCAN_apply_operator_v361_ui_hotfix_patch.py",
    "RIFTSCAN_apply_operator_v361_ui_hotfix_patch_v11.py",
    "RIFTSCAN_apply_operator_v362_ui_import_hotfix_patch.py",
    "RIFTSCAN_apply_operator_v37_analyze_latest_session_patch_checked.py",
    "payload",
    "rift_focus_local_simple_v2.zip",
]

JUNK_GLOBS = [
    "__pycache__",
    "tools/__pycache__",
    "scripts/__pycache__",
    "*.bak-*",
    "tools/*.bak-*",
    "scripts/*.bak-*",
    "*.repair-bak-*",
]


def utc_now() -> str:
    return dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def safe_timestamp() -> str:
    return dt.datetime.now(dt.UTC).strftime("%Y%m%dT%H%M%SZ")


def rel(path: Path) -> str:
    try:
        return path.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path)


def run_command(args: list[str], timeout: int = 90) -> tuple[int, str, str]:
    try:
        completed = subprocess.run(
            args,
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            timeout=timeout,
            shell=False,
        )
        return completed.returncode, completed.stdout, completed.stderr
    except subprocess.TimeoutExpired as exc:
        out = exc.stdout or ""
        err = exc.stderr or ""
        return 124, out, f"{err}\nTIMEOUT after {timeout} seconds"
    except Exception as exc:
        return 1, "", f"{type(exc).__name__}: {exc}"


def is_git_ignored(repo_relative_path: str) -> bool:
    code, _, _ = run_command(["git", "check-ignore", "-q", "--", repo_relative_path], timeout=30)
    return code == 0


def read_text(path: Path) -> str:
    if not path.exists():
        return f"[missing: {rel(path)}]"
    return path.read_text(encoding="utf-8", errors="replace")


def tail_text(path: Path, lines: int = 60) -> str:
    return "\n".join(read_text(path).splitlines()[-lines:])


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"_missing": rel(path)}
    try:
        return json.loads(path.read_text(encoding="utf-8", errors="replace"))
    except Exception as exc:
        return {"_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def json_block(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False)


def focus_line(summary: dict[str, Any]) -> str:
    status = summary.get("status", "unknown")
    process = summary.get("process") or {}
    selected = summary.get("selected_window") or {}
    pid = process.get("Id", "n/a")
    hwnd = selected.get("hwnd_hex", "n/a")
    title = selected.get("title", "n/a")
    return f"status={status} pid={pid} hwnd={hwnd} title={title}"


def validate_full_live_preflight(summary: dict[str, Any], windows: dict[str, Any]) -> tuple[bool, list[str]]:
    issues: list[str] = []

    if summary.get("_missing"):
        issues.append(f"{summary['_missing']} is missing.")
    if summary.get("_error"):
        issues.append(f"{summary.get('_path', rel(FOCUS_SUMMARY))} failed to parse: {summary['_error']}")
    if windows.get("_missing"):
        issues.append(f"{windows['_missing']} is missing.")
    if windows.get("_error"):
        issues.append(f"{windows.get('_path', rel(WINDOWS_JSON))} failed to parse: {windows['_error']}")

    if summary.get("status") != "foreground_verified":
        issues.append("Focus status is not foreground_verified.")
    if not summary.get("selected_window"):
        issues.append("selected_window is missing or null.")

    process = summary.get("process") or {}
    process_name = process.get("ProcessName") or process.get("Name")
    if process_name not in {"rift_x64", "rift_x64.exe"}:
        issues.append("RIFT process name is not rift_x64.")

    window_entries = windows.get("windows")
    if not isinstance(window_entries, list) or not window_entries:
        issues.append("windows.json has no window entries.")

    return not issues, issues


def run_full_live_preflight_gate() -> dict[str, Any]:
    focus_code = 1
    focus_out = ""
    focus_err = ""
    issues: list[str] = []

    if FOCUS_SCRIPT.exists():
        focus_code, focus_out, focus_err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)
    else:
        issues.append(f"Missing {rel(FOCUS_SCRIPT)}.")

    summary = load_json(FOCUS_SUMMARY)
    windows = load_json(WINDOWS_JSON)
    valid, validation_issues = validate_full_live_preflight(summary, windows)
    issues.extend(validation_issues)

    if FOCUS_SCRIPT.exists() and focus_code != 0:
        issues.append(f"Focus-control script exited with code {focus_code}.")

    git_status_code, git_status, git_status_err = run_command(["git", "status", "--short"], timeout=30)
    git_log_code, git_log, git_log_err = run_command(["git", "log", "--oneline", "-5"], timeout=30)

    if git_status_code != 0:
        issues.append("git status --short failed.")
    if git_log_code != 0:
        issues.append("git log --oneline -5 failed.")

    pass_gate = valid and focus_code == 0 and git_status_code == 0 and git_log_code == 0 and not issues

    return {
        "pass_gate": pass_gate,
        "issues": issues,
        "focus_code": focus_code,
        "focus_stdout": focus_out,
        "focus_stderr": focus_err,
        "summary": summary,
        "windows": windows,
        "git_status_code": git_status_code,
        "git_status": git_status,
        "git_status_err": git_status_err,
        "git_log_code": git_log_code,
        "git_log": git_log,
        "git_log_err": git_log_err,
    }


def latest_dry_run_summary() -> dict[str, Any]:
    if not LATEST_DRY_RUN.exists():
        return {"status": "none"}
    session_rel = LATEST_DRY_RUN.read_text(encoding="utf-8", errors="replace").strip()
    if not session_rel:
        return {"status": "none", "reason": "latest pointer is empty"}
    manifest_path = REPO_ROOT / session_rel / "manifest.json"
    manifest = load_json(manifest_path)
    return {
        "status": "present",
        "latest_session": session_rel,
        "manifest_path": rel(manifest_path),
        "manifest": manifest,
    }


def latest_capture_plan_summary() -> dict[str, Any]:
    if not LATEST_CAPTURE_PLAN.exists():
        return {"status": "none"}
    plan_rel = LATEST_CAPTURE_PLAN.read_text(encoding="utf-8", errors="replace").strip()
    if not plan_rel:
        return {"status": "none", "reason": "latest pointer is empty"}
    plan_dir = REPO_ROOT / plan_rel
    manifest_path = plan_dir / "capture-plan.json"
    handoff_path = plan_dir / "CAPTURE_PLAN_HANDOFF.md"
    manifest = load_json(manifest_path)
    return {
        "status": "present",
        "latest_plan": plan_rel,
        "manifest_path": rel(manifest_path),
        "handoff_path": rel(handoff_path),
        "manifest": manifest,
    }


def latest_capture_session_summary() -> dict[str, Any]:
    if not LATEST_CAPTURE_SESSION.exists():
        return {"status": "none"}
    session_rel = LATEST_CAPTURE_SESSION.read_text(encoding="utf-8", errors="replace").strip()
    if not session_rel:
        return {"status": "none", "reason": "latest pointer is empty"}
    session_dir = REPO_ROOT / session_rel
    manifest_path = session_dir / "capture-session-manifest.json"
    handoff_path = session_dir / "CAPTURE_SESSION_HANDOFF.md"
    manifest = load_json(manifest_path)

    if manifest.get("_missing") or manifest.get("_error"):
        return {
            "status": "present_with_manifest_problem",
            "latest_session": session_rel,
            "manifest_path": rel(manifest_path),
            "handoff_path": rel(handoff_path),
            "manifest_problem": manifest,
        }

    full_live_preflight = manifest.get("full_live_preflight") or {}
    focus_after = manifest.get("focus_after") or {}
    files = manifest.get("files") or {}

    return {
        "status": "present",
        "latest_session": session_rel,
        "manifest_path": rel(manifest_path),
        "handoff_path": rel(handoff_path),
        "summary": {
            "schema_version": manifest.get("schema_version"),
            "app_version": manifest.get("app_version"),
            "status": manifest.get("status"),
            "scaffold_only": manifest.get("scaffold_only"),
            "capture_mode": manifest.get("capture_mode"),
            "duration_target_seconds": manifest.get("duration_target_seconds"),
            "stimulus_name": manifest.get("stimulus_name"),
            "scaffold_window_started": manifest.get("scaffold_window_started"),
            "scaffold_window_completed": manifest.get("scaffold_window_completed"),
            "real_capture_started": manifest.get("real_capture_started"),
            "real_capture_completed": manifest.get("real_capture_completed"),
            "legacy_capture_started": manifest.get("capture_started"),
            "legacy_capture_completed": manifest.get("capture_completed"),
            "focus_before_status": full_live_preflight.get("focus_status"),
            "focus_after_status": focus_after.get("focus_status"),
            "process_id": full_live_preflight.get("process_id"),
            "window_hwnd_hex": full_live_preflight.get("window_hwnd_hex"),
            "window_title": full_live_preflight.get("window_title"),
            "capture_log": files.get("capture_log"),
            "collector_samples": files.get("collector_samples"),
            "collector_summary": files.get("collector_summary"),
            "memory_read_started": manifest.get("memory_read_started"),
            "input_sent": manifest.get("input_sent"),
            "reloadui_sent": manifest.get("reloadui_sent"),
            "sample_count": (manifest.get("collector_summary") or {}).get("sample_count"),
            "error_count": (manifest.get("collector_summary") or {}).get("error_count"),
            "artifact_contract_status": (manifest.get("artifact_contract") or {}).get("status"),
            "missing_artifacts": (manifest.get("artifact_contract") or {}).get("missing_artifacts"),
            "analysis_status": (latest_window_process_analysis_summary().get("summary") or {}).get("status"),
            "analysis_anomaly_count": (latest_window_process_analysis_summary().get("summary") or {}).get("anomaly_count"),
        },
    }


def append_jsonl(path: Path, event: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    line = json.dumps(event, ensure_ascii=False, sort_keys=True)
    with path.open("a", encoding="utf-8") as handle:
        handle.write(line + "\n")


def bounded_duration_seconds(value: Any, default: int = 30) -> int:
    try:
        seconds = int(value)
    except (TypeError, ValueError):
        return default
    return max(1, min(seconds, 300))


def parse_int(value: Any, default: int = 0) -> int:
    try:
        if isinstance(value, str) and value.lower().startswith("0x"):
            return int(value, 16)
        return int(value)
    except (TypeError, ValueError):
        return default


def hwnd_to_hex(hwnd: Any) -> str:
    return f"0x{parse_int(hwnd):X}"


def window_pid(hwnd: int) -> int:
    if not hwnd:
        return 0
    pid = wintypes.DWORD(0)
    try:
        ctypes.windll.user32.GetWindowThreadProcessId(wintypes.HWND(hwnd), ctypes.byref(pid))
        return int(pid.value)
    except Exception:
        return 0


def window_title(hwnd: int) -> str:
    if not hwnd:
        return ""
    try:
        user32 = ctypes.windll.user32
        length = int(user32.GetWindowTextLengthW(wintypes.HWND(hwnd)))
        buffer = ctypes.create_unicode_buffer(max(1, length + 1))
        user32.GetWindowTextW(wintypes.HWND(hwnd), buffer, len(buffer))
        return buffer.value
    except Exception:
        return ""


def rect_to_dict(rect: Any) -> dict[str, int]:
    left = int(rect.left)
    top = int(rect.top)
    right = int(rect.right)
    bottom = int(rect.bottom)
    return {
        "left": left,
        "top": top,
        "right": right,
        "bottom": bottom,
        "width": max(0, right - left),
        "height": max(0, bottom - top),
    }


def window_rect(hwnd: int) -> dict[str, Any]:
    if not hwnd:
        return {"available": False}
    rect = wintypes.RECT()
    try:
        ok = bool(ctypes.windll.user32.GetWindowRect(wintypes.HWND(hwnd), ctypes.byref(rect)))
        return {"available": ok, "rect": rect_to_dict(rect) if ok else None}
    except Exception as exc:
        return {"available": False, "error": f"{type(exc).__name__}: {exc}"}


def client_rect(hwnd: int) -> dict[str, Any]:
    if not hwnd:
        return {"available": False}
    rect = wintypes.RECT()
    try:
        ok = bool(ctypes.windll.user32.GetClientRect(wintypes.HWND(hwnd), ctypes.byref(rect)))
        return {"available": ok, "rect": rect_to_dict(rect) if ok else None}
    except Exception as exc:
        return {"available": False, "error": f"{type(exc).__name__}: {exc}"}


def is_window(hwnd: int) -> bool:
    if not hwnd:
        return False
    try:
        return bool(ctypes.windll.user32.IsWindow(wintypes.HWND(hwnd)))
    except Exception:
        return False


def is_window_visible(hwnd: int) -> bool:
    if not hwnd:
        return False
    try:
        return bool(ctypes.windll.user32.IsWindowVisible(wintypes.HWND(hwnd)))
    except Exception:
        return False


def is_process_alive(pid: Any) -> bool:
    pid_int = parse_int(pid)
    if pid_int <= 0:
        return False

    process_query_limited_information = 0x1000
    still_active = 259

    try:
        kernel32 = ctypes.windll.kernel32
        handle = kernel32.OpenProcess(process_query_limited_information, False, pid_int)
        if not handle:
            return False
        try:
            exit_code = wintypes.DWORD(0)
            ok = bool(kernel32.GetExitCodeProcess(handle, ctypes.byref(exit_code)))
            return ok and int(exit_code.value) == still_active
        finally:
            kernel32.CloseHandle(handle)
    except Exception:
        return False


def target_identity_from_summary(summary: dict[str, Any]) -> dict[str, Any]:
    process = summary.get("process") or {}
    selected = summary.get("selected_window") or {}
    hwnd = parse_int(selected.get("hwnd") or selected.get("hwnd_hex"))
    return {
        "process_name": process.get("ProcessName") or process.get("Name") or "rift_x64",
        "pid": parse_int(process.get("Id")),
        "hwnd": hwnd,
        "hwnd_hex": selected.get("hwnd_hex") or hwnd_to_hex(hwnd),
        "title": selected.get("title") or process.get("MainWindowTitle") or "RIFT",
    }


def collector_state(
    name: str,
    *,
    real_capture_started: bool,
    memory_read_started: bool = False,
    input_sent: bool = False,
    reloadui_sent: bool = False,
) -> dict[str, Any]:
    return {
        "name": name,
        "real_capture_started": real_capture_started,
        "memory_read_started": memory_read_started,
        "input_sent": input_sent,
        "reloadui_sent": reloadui_sent,
    }


def make_capture_event(
    event_index: int,
    *,
    phase: str,
    event: str,
    session_id: str,
    start_monotonic: float,
    target_identity: dict[str, Any],
    collector: dict[str, Any],
    extra: dict[str, Any] | None = None,
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "event_index": event_index,
        "utc": utc_now(),
        "monotonic_elapsed_seconds": round(max(0.0, time.monotonic() - start_monotonic), 3),
        "phase": phase,
        "event": event,
        "session_id": session_id,
        "app_version": APP_VERSION,
        "log_schema_version": LOG_SCHEMA_VERSION,
        "target_identity": target_identity,
        "collector": collector,
    }
    if extra:
        payload.update(extra)
    return payload


def collect_window_process_metadata_sample(target_identity: dict[str, Any]) -> dict[str, Any]:
    rift_hwnd = parse_int(target_identity.get("hwnd"))
    rift_pid = parse_int(target_identity.get("pid"))

    try:
        foreground_hwnd = int(ctypes.windll.user32.GetForegroundWindow())
    except Exception:
        foreground_hwnd = 0

    foreground_pid = window_pid(foreground_hwnd)
    foreground = {
        "hwnd": foreground_hwnd,
        "hwnd_hex": hwnd_to_hex(foreground_hwnd),
        "pid": foreground_pid,
        "title": window_title(foreground_hwnd),
        "is_window": is_window(foreground_hwnd),
        "is_visible": is_window_visible(foreground_hwnd),
        "window_rect": window_rect(foreground_hwnd),
        "client_rect": client_rect(foreground_hwnd),
    }

    rift_window = {
        "hwnd": rift_hwnd,
        "hwnd_hex": hwnd_to_hex(rift_hwnd),
        "pid": rift_pid,
        "process_name": target_identity.get("process_name"),
        "title": window_title(rift_hwnd) or target_identity.get("title"),
        "is_window": is_window(rift_hwnd),
        "is_visible": is_window_visible(rift_hwnd),
        "window_rect": window_rect(rift_hwnd),
        "client_rect": client_rect(rift_hwnd),
        "process_alive": is_process_alive(rift_pid),
    }

    focus_verified = (
        foreground_hwnd != 0
        and rift_hwnd != 0
        and foreground_hwnd == rift_hwnd
        and foreground_pid == rift_pid
    )

    return {
        "foreground": foreground,
        "rift_window": rift_window,
        "focus_verified": focus_verified,
        "foreground_matches_rift_hwnd": foreground_hwnd == rift_hwnd and rift_hwnd != 0,
        "foreground_matches_rift_pid": foreground_pid == rift_pid and rift_pid != 0,
    }


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json_block(value) + "\n", encoding="utf-8")


def run_focus_gated_window_process_metadata_collector(gate: dict[str, Any]) -> dict[str, Path]:
    if not gate.get("pass_gate"):
        raise RuntimeError("Cannot run window/process metadata collector because full live preflight did not pass.")

    plan_summary = latest_capture_plan_summary()
    if plan_summary.get("status") != "present":
        raise RuntimeError("Cannot run window/process metadata collector because no latest focus-gated capture plan exists.")

    plan_manifest = plan_summary.get("manifest") or {}
    if plan_manifest.get("metadata_only") is not True:
        raise RuntimeError("Latest capture plan manifest is invalid: metadata_only is not true.")

    duration_seconds = bounded_duration_seconds(plan_manifest.get("duration_target_seconds"), default=30)
    sample_interval_ms = DEFAULT_WINDOW_PROCESS_SAMPLE_INTERVAL_MS
    sample_interval_seconds = sample_interval_ms / 1000.0

    summary_before = gate.get("summary") or {}
    windows_before = gate.get("windows") or {}
    target_identity = target_identity_from_summary(summary_before)
    collector = collector_state(
        "window_process_metadata",
        real_capture_started=True,
        memory_read_started=False,
        input_sent=False,
        reloadui_sent=False,
    )

    session_id = f"{safe_timestamp()}_window_process_metadata_collector"
    session_dir = CAPTURE_SESSION_ROOT / session_id
    session_dir.mkdir(parents=True, exist_ok=False)

    manifest_path = session_dir / "capture-session-manifest.json"
    capture_log_path = session_dir / "capture-log.jsonl"
    focus_before_path = session_dir / "focus-summary-before.json"
    focus_after_path = session_dir / "focus-summary-after.json"
    operator_report_copy = session_dir / "operator-report.md"
    handoff_path = session_dir / "CAPTURE_SESSION_HANDOFF.md"

    collector_manifest_path = session_dir / "collector-manifest.json"
    collector_samples_path = session_dir / "collector-samples.jsonl"
    collector_summary_path = session_dir / "collector-summary.json"
    collector_errors_path = session_dir / "collector-errors.jsonl"

    for declared_artifact_file in (
        capture_log_path,
        collector_samples_path,
        collector_errors_path,
        operator_report_copy,
        handoff_path,
    ):
        declared_artifact_file.parent.mkdir(parents=True, exist_ok=True)
        declared_artifact_file.touch(exist_ok=True)

    write_json(focus_before_path, summary_before)

    started_utc = utc_now()
    start_monotonic = time.monotonic()
    log_index = 0
    sample_index = 0
    error_index = 0

    def log_event(phase: str, event_name: str, extra: dict[str, Any] | None = None) -> None:
        nonlocal log_index
        log_index += 1
        append_jsonl(
            capture_log_path,
            make_capture_event(
                log_index,
                phase=phase,
                event=event_name,
                session_id=session_id,
                start_monotonic=start_monotonic,
                target_identity=target_identity,
                collector=collector,
                extra=extra,
            ),
        )

    def sample_event(extra: dict[str, Any]) -> None:
        nonlocal sample_index
        sample_index += 1
        append_jsonl(
            collector_samples_path,
            make_capture_event(
                sample_index,
                phase="collector_sample",
                event="window_process_metadata_sample",
                session_id=session_id,
                start_monotonic=start_monotonic,
                target_identity=target_identity,
                collector=collector,
                extra={"sample_index": sample_index, **extra},
            ),
        )

    def error_event(extra: dict[str, Any]) -> None:
        nonlocal error_index
        error_index += 1
        append_jsonl(
            collector_errors_path,
            make_capture_event(
                error_index,
                phase="collector_error",
                event="window_process_metadata_error",
                session_id=session_id,
                start_monotonic=start_monotonic,
                target_identity=target_identity,
                collector=collector,
                extra={"error_index": error_index, **extra},
            ),
        )

    files = {
        "capture_session_manifest": rel(manifest_path),
        "capture_log": rel(capture_log_path),
        "focus_summary_before": rel(focus_before_path),
        "focus_summary_after": rel(focus_after_path),
        "operator_report": rel(operator_report_copy),
        "handoff": rel(handoff_path),
        "collector_manifest": rel(collector_manifest_path),
        "collector_samples": rel(collector_samples_path),
        "collector_summary": rel(collector_summary_path),
        "collector_errors": rel(collector_errors_path),
    }

    collector_manifest = {
        "schema_version": WINDOW_PROCESS_COLLECTOR_SCHEMA_VERSION,
        "created_utc": started_utc,
        "app_version": APP_VERSION,
        "session_id": session_id,
        "collector": collector,
        "target_identity": target_identity,
        "duration_target_seconds": duration_seconds,
        "sample_interval_ms": sample_interval_ms,
        "output_files": files,
        "guardrails": [
            "OS/window/process metadata only.",
            "No process memory read.",
            "No keyboard input sent.",
            "No mouse input sent.",
            "No /reloadui sent.",
        ],
    }
    write_json(collector_manifest_path, collector_manifest)

    manifest = {
        "schema_version": "riftscan.focus_gated_window_process_metadata_session.v1",
        "created_utc": started_utc,
        "app_version": APP_VERSION,
        "session_id": session_id,
        "status": "window_process_metadata_collector_running",
        "capture_mode": "window_process_metadata",
        "scaffold_only": False,
        "metadata_collector_started": True,
        "metadata_collector_completed": False,
        "real_capture_started": True,
        "real_capture_completed": False,
        "memory_read_started": False,
        "memory_read_completed": False,
        "input_sent": False,
        "reloadui_sent": False,
        "duration_target_seconds": duration_seconds,
        "sample_interval_ms": sample_interval_ms,
        "stimulus_name": plan_manifest.get("stimulus_name", "none_metadata_only"),
        "source_capture_plan": plan_summary,
        "full_live_preflight": {
            "status": "PASS",
            "focus_status": summary_before.get("status"),
            "process_id": target_identity.get("pid"),
            "process_name": target_identity.get("process_name"),
            "window_hwnd": target_identity.get("hwnd"),
            "window_hwnd_hex": target_identity.get("hwnd_hex"),
            "window_title": target_identity.get("title"),
            "windows_count": len(windows_before.get("windows") or []),
        },
        "files": files,
        "guardrails": [
            "Window/process metadata collector only.",
            "No process memory read.",
            "No movement/input sent.",
            "No /reloadui sent.",
        ],
    }
    write_json(manifest_path, manifest)

    log_event(
        "collector_start",
        "window_process_metadata_collector_started",
        {
            "duration_target_seconds": duration_seconds,
            "sample_interval_ms": sample_interval_ms,
            "source_capture_plan": plan_summary.get("latest_plan"),
        },
    )

    next_sample = start_monotonic
    next_heartbeat = start_monotonic + 5.0
    first_sample_utc: str | None = None
    last_sample_utc: str | None = None
    focus_verified_count = 0
    focus_lost_count = 0
    rift_process_alive_count = 0
    rift_process_dead_count = 0

    while True:
        now = time.monotonic()
        elapsed = now - start_monotonic
        if elapsed >= duration_seconds:
            break

        if now >= next_sample:
            try:
                sample = collect_window_process_metadata_sample(target_identity)
                if sample.get("focus_verified"):
                    focus_verified_count += 1
                else:
                    focus_lost_count += 1

                rift_alive = bool((sample.get("rift_window") or {}).get("process_alive"))
                if rift_alive:
                    rift_process_alive_count += 1
                else:
                    rift_process_dead_count += 1

                sample_utc = utc_now()
                first_sample_utc = first_sample_utc or sample_utc
                last_sample_utc = sample_utc

                sample_event({
                    "utc": sample_utc,
                    "data": sample,
                })
            except Exception as exc:
                error_event({
                    "error_type": type(exc).__name__,
                    "error_message": str(exc),
                })

            next_sample += sample_interval_seconds

        if now >= next_heartbeat:
            log_event(
                "collector_window",
                "window_process_metadata_collector_heartbeat",
                {
                    "elapsed_seconds": round(elapsed, 3),
                    "remaining_seconds": max(0, round(duration_seconds - elapsed, 3)),
                    "samples_written": sample_index,
                    "errors_written": error_index,
                },
            )
            next_heartbeat += 5.0

        sleep_until = min(next_sample, next_heartbeat, start_monotonic + duration_seconds)
        time.sleep(max(0.01, min(0.1, sleep_until - time.monotonic())))

    elapsed_seconds = round(time.monotonic() - start_monotonic, 3)
    log_event(
        "collector_window",
        "window_process_metadata_collector_window_elapsed",
        {
            "elapsed_seconds": elapsed_seconds,
            "samples_written": sample_index,
            "errors_written": error_index,
        },
    )

    after_code, after_out, after_err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)
    summary_after = load_json(FOCUS_SUMMARY)
    write_json(focus_after_path, summary_after)

    completed_utc = utc_now()
    final_status = "window_process_metadata_collector_completed" if after_code == 0 else "window_process_metadata_collector_completed_with_focus_after_failure"

    collector_summary = {
        "schema_version": "riftscan.window_process_metadata_collector_summary.v1",
        "created_utc": completed_utc,
        "session_id": session_id,
        "status": final_status,
        "duration_target_seconds": duration_seconds,
        "elapsed_seconds": elapsed_seconds,
        "sample_interval_ms": sample_interval_ms,
        "sample_count": sample_index,
        "error_count": error_index,
        "first_sample_utc": first_sample_utc,
        "last_sample_utc": last_sample_utc,
        "focus_verified_count": focus_verified_count,
        "focus_lost_count": focus_lost_count,
        "rift_process_alive_count": rift_process_alive_count,
        "rift_process_dead_count": rift_process_dead_count,
        "focus_after_status": summary_after.get("status"),
        "memory_read_started": False,
        "input_sent": False,
        "reloadui_sent": False,
    }
    write_json(collector_summary_path, collector_summary)

    log_event(
        "collector_complete",
        "window_process_metadata_collector_completed",
        {
            "status": final_status,
            "completed_utc": completed_utc,
            "elapsed_seconds": elapsed_seconds,
            "samples_written": sample_index,
            "errors_written": error_index,
            "focus_after_exit_code": after_code,
            "focus_after_status": summary_after.get("status"),
            "artifacts": files,
        },
    )

    manifest.update({
        "completed_utc": completed_utc,
        "status": final_status,
        "metadata_collector_completed": True,
        "real_capture_completed": True,
        "elapsed_seconds": elapsed_seconds,
        "collector_summary": collector_summary,
        "focus_after": {
            "command_exit_code": after_code,
            "stdout": after_out.strip(),
            "stderr": after_err.strip(),
            "focus_status": summary_after.get("status"),
        },
        "next_expected_step": "Review OS/window/process metadata samples, then decide whether to add RiftReader external collector integration.",
    })
    write_json(manifest_path, manifest)

    LATEST_CAPTURE_SESSION.parent.mkdir(parents=True, exist_ok=True)
    LATEST_CAPTURE_SESSION.write_text(rel(session_dir) + "\n", encoding="utf-8")

    declared_artifact_paths = {
        "capture_session_manifest": manifest_path,
        "capture_log": capture_log_path,
        "focus_summary_before": focus_before_path,
        "focus_summary_after": focus_after_path,
        "operator_report": operator_report_copy,
        "handoff": handoff_path,
        "collector_manifest": collector_manifest_path,
        "collector_samples": collector_samples_path,
        "collector_summary": collector_summary_path,
        "collector_errors": collector_errors_path,
    }
    missing_artifacts = [rel(path) for path in declared_artifact_paths.values() if not path.exists()]
    artifact_contract = {
        "schema_version": "riftscan.artifact_contract.v1",
        "status": "FAIL" if missing_artifacts else "PASS",
        "declared_count": len(declared_artifact_paths),
        "existing_count": len(declared_artifact_paths) - len(missing_artifacts),
        "missing_artifacts": missing_artifacts,
    }
    collector_summary["artifact_contract"] = artifact_contract
    manifest["artifact_contract"] = artifact_contract
    if missing_artifacts:
        warning_status = "window_process_metadata_collector_completed_with_artifact_warning"
        collector_summary["status"] = warning_status
        manifest["status"] = warning_status
        log_event(
            "collector_finalize",
            "window_process_metadata_artifact_contract_warning",
            {
                "status": warning_status,
                "missing_artifacts": missing_artifacts,
                "artifact_contract": artifact_contract,
            },
        )
    write_json(collector_summary_path, collector_summary)
    write_json(manifest_path, manifest)

    report_path = write_operator_report()
    operator_report_copy.write_text(read_text(report_path), encoding="utf-8")

    handoff_lines = [
        "# Focus-Gated Window/Process Metadata Collector",
        "",
        f"Session ID: `{session_id}`",
        f"Created UTC: `{started_utc}`",
        f"Completed UTC: `{completed_utc}`",
        f"Status: `{final_status}`",
        f"Duration target seconds: `{duration_seconds}`",
        f"Sample interval ms: `{sample_interval_ms}`",
        f"Samples written: `{sample_index}`",
        f"Errors written: `{error_index}`",
        "",
        "## Result",
        "",
        "The operator app ran the first focus-gated metadata collector. It sampled OS/window/process/focus metadata only.",
        "",
        "```text",
        f"Focus before: {summary_before.get('status')}",
        f"Focus after: {summary_after.get('status')}",
        f"PID: {target_identity.get('pid')}",
        f"HWND: {target_identity.get('hwnd_hex')}",
        f"Title: {target_identity.get('title')}",
        f"Focus verified samples: {focus_verified_count}",
        f"Focus lost samples: {focus_lost_count}",
        "```",
        "",
        "## Files",
        "",
        "```text",
        rel(manifest_path),
        rel(capture_log_path),
        rel(collector_manifest_path),
        rel(collector_samples_path),
        rel(collector_summary_path),
        rel(collector_errors_path),
        rel(focus_before_path),
        rel(focus_after_path),
        rel(operator_report_copy),
        "```",
        "",
        "## Guardrails",
        "",
        "- OS/window/process metadata only.",
        "- No process memory read.",
        "- No movement/input sent.",
        "- No /reloadui sent.",
        "",
        "## Next Expected Step",
        "",
        "Review OS/window/process metadata samples, then decide whether to add RiftReader external collector integration.",
    ]
    handoff_path.write_text("\n".join(handoff_lines) + "\n", encoding="utf-8")

    return {
        "session_dir": session_dir,
        "manifest_path": manifest_path,
        "capture_log_path": capture_log_path,
        "collector_manifest_path": collector_manifest_path,
        "collector_samples_path": collector_samples_path,
        "collector_summary_path": collector_summary_path,
        "collector_errors_path": collector_errors_path,
        "focus_before_path": focus_before_path,
        "focus_after_path": focus_after_path,
        "handoff_path": handoff_path,
        "operator_report_path": report_path,
    }


def read_jsonl(path: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    records: list[dict[str, Any]] = []
    errors: list[dict[str, Any]] = []

    if not path.exists():
        errors.append({
            "line": 0,
            "error_type": "missing_file",
            "error_message": f"{rel(path)} is missing.",
        })
        return records, errors

    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), start=1):
        line = raw_line.strip()
        if not line:
            continue

        try:
            value = json.loads(line)
        except Exception as exc:
            errors.append({
                "line": line_number,
                "error_type": type(exc).__name__,
                "error_message": str(exc),
                "raw": raw_line[:500],
            })
            continue

        if isinstance(value, dict):
            records.append(value)
        else:
            errors.append({
                "line": line_number,
                "error_type": "invalid_record_type",
                "error_message": f"Expected JSON object, got {type(value).__name__}.",
            })

    return records, errors


def latest_window_process_analysis_summary() -> dict[str, Any]:
    if not LATEST_CAPTURE_SESSION.exists():
        return {"status": "none"}

    session_rel = LATEST_CAPTURE_SESSION.read_text(encoding="utf-8", errors="replace").strip()
    if not session_rel:
        return {"status": "none", "reason": "latest capture session pointer is empty"}

    session_dir = REPO_ROOT / session_rel
    analysis_path = session_dir / "analysis" / "window-process-analysis.json"
    handoff_path = session_dir / "analysis" / "WINDOW_PROCESS_ANALYSIS.md"

    if not analysis_path.exists():
        return {
            "status": "none",
            "latest_session": session_rel,
            "reason": "analysis has not been generated for latest session",
            "expected_analysis_path": rel(analysis_path),
        }

    analysis = load_json(analysis_path)
    summary = analysis.get("summary") if isinstance(analysis, dict) else None
    return {
        "status": "present",
        "latest_session": session_rel,
        "analysis_path": rel(analysis_path),
        "handoff_path": rel(handoff_path),
        "summary": summary or analysis,
    }


def rect_signature(value: Any) -> tuple[Any, Any, Any, Any] | None:
    if not isinstance(value, dict):
        return None
    rect = value.get("rect")
    if not isinstance(rect, dict):
        return None
    return (
        rect.get("left"),
        rect.get("top"),
        rect.get("right"),
        rect.get("bottom"),
    )


def unique_values(values: list[Any]) -> list[Any]:
    result: list[Any] = []
    for value in values:
        if value not in result:
            result.append(value)
    return result


def analyze_latest_window_process_session() -> dict[str, Path]:
    if not LATEST_CAPTURE_SESSION.exists():
        raise RuntimeError(f"Missing latest capture session pointer: {rel(LATEST_CAPTURE_SESSION)}")

    session_rel = LATEST_CAPTURE_SESSION.read_text(encoding="utf-8", errors="replace").strip()
    if not session_rel:
        raise RuntimeError(f"Latest capture session pointer is empty: {rel(LATEST_CAPTURE_SESSION)}")

    session_dir = REPO_ROOT / session_rel
    if not session_dir.exists():
        raise RuntimeError(f"Latest capture session directory does not exist: {rel(session_dir)}")

    manifest_path = session_dir / "capture-session-manifest.json"
    collector_summary_path = session_dir / "collector-summary.json"
    collector_samples_path = session_dir / "collector-samples.jsonl"
    capture_log_path = session_dir / "capture-log.jsonl"

    analysis_dir = session_dir / "analysis"
    analysis_dir.mkdir(parents=True, exist_ok=True)

    analysis_path = analysis_dir / "window-process-analysis.json"
    anomalies_path = analysis_dir / "window-process-anomalies.jsonl"
    handoff_path = analysis_dir / "WINDOW_PROCESS_ANALYSIS.md"

    manifest = load_json(manifest_path)
    collector_summary = load_json(collector_summary_path)
    samples, sample_parse_errors = read_jsonl(collector_samples_path)
    capture_events, capture_log_parse_errors = read_jsonl(capture_log_path)

    anomalies: list[dict[str, Any]] = []

    def add_anomaly(kind: str, severity: str, message: str, **extra: Any) -> None:
        anomaly = {
            "kind": kind,
            "severity": severity,
            "message": message,
        }
        anomaly.update(extra)
        anomalies.append(anomaly)

    if manifest.get("_missing") or manifest.get("_error"):
        add_anomaly("manifest_problem", "error", "capture-session-manifest.json is missing or invalid.", details=manifest)

    if collector_summary.get("_missing") or collector_summary.get("_error"):
        add_anomaly("collector_summary_problem", "error", "collector-summary.json is missing or invalid.", details=collector_summary)

    if sample_parse_errors:
        add_anomaly("sample_parse_errors", "error", "collector-samples.jsonl contains parse errors.", count=len(sample_parse_errors), errors=sample_parse_errors[:10])

    if capture_log_parse_errors:
        add_anomaly("capture_log_parse_errors", "warning", "capture-log.jsonl contains parse errors.", count=len(capture_log_parse_errors), errors=capture_log_parse_errors[:10])

    sample_count = len(samples)
    expected_sample_count = collector_summary.get("sample_count")
    if isinstance(expected_sample_count, int) and expected_sample_count != sample_count:
        add_anomaly(
            "sample_count_mismatch",
            "error",
            "collector-summary sample_count does not match actual parsed sample count.",
            expected=expected_sample_count,
            actual=sample_count,
        )

    sample_indexes = [parse_int(sample.get("sample_index") or sample.get("event_index")) for sample in samples]
    nonzero_sample_indexes = [index for index in sample_indexes if index > 0]
    missing_sample_indexes: list[int] = []
    duplicate_sample_indexes: list[int] = []

    if nonzero_sample_indexes:
        min_index = min(nonzero_sample_indexes)
        max_index = max(nonzero_sample_indexes)
        expected_indexes = set(range(min_index, max_index + 1))
        actual_indexes = set(nonzero_sample_indexes)
        missing_sample_indexes = sorted(expected_indexes - actual_indexes)
        duplicate_sample_indexes = sorted(index for index in actual_indexes if nonzero_sample_indexes.count(index) > 1)

        if min_index != 1:
            add_anomaly("sample_index_start", "warning", "Sample indexes do not start at 1.", first_index=min_index)
        if missing_sample_indexes:
            add_anomaly("missing_sample_indexes", "error", "Missing sample indexes detected.", missing_sample_indexes=missing_sample_indexes[:50], count=len(missing_sample_indexes))
        if duplicate_sample_indexes:
            add_anomaly("duplicate_sample_indexes", "error", "Duplicate sample indexes detected.", duplicate_sample_indexes=duplicate_sample_indexes[:50], count=len(duplicate_sample_indexes))
    elif sample_count:
        add_anomaly("sample_index_missing", "warning", "No usable sample_index/event_index values found in sample records.")

    focus_lost_samples: list[int] = []
    rift_process_dead_samples: list[int] = []
    foreground_hwnds: list[Any] = []
    foreground_pids: list[Any] = []
    rift_hwnds: list[Any] = []
    rift_pids: list[Any] = []
    rift_titles: list[Any] = []
    rift_window_rects: list[Any] = []
    rift_client_rects: list[Any] = []
    monotonic_values: list[float] = []

    for index, sample in enumerate(samples, start=1):
        sample_index = parse_int(sample.get("sample_index") or sample.get("event_index"), default=index)
        data = sample.get("data") or {}
        foreground = data.get("foreground") or {}
        rift_window = data.get("rift_window") or {}

        if data.get("focus_verified") is not True:
            focus_lost_samples.append(sample_index)

        if rift_window.get("process_alive") is not True:
            rift_process_dead_samples.append(sample_index)

        foreground_hwnds.append(foreground.get("hwnd_hex") or foreground.get("hwnd"))
        foreground_pids.append(foreground.get("pid"))
        rift_hwnds.append(rift_window.get("hwnd_hex") or rift_window.get("hwnd"))
        rift_pids.append(rift_window.get("pid"))
        rift_titles.append(rift_window.get("title"))
        rift_window_rects.append(rect_signature(rift_window.get("window_rect")))
        rift_client_rects.append(rect_signature(rift_window.get("client_rect")))

        try:
            monotonic_values.append(float(sample.get("monotonic_elapsed_seconds")))
        except (TypeError, ValueError):
            pass

    unique_foreground_hwnds = unique_values([value for value in foreground_hwnds if value is not None])
    unique_foreground_pids = unique_values([value for value in foreground_pids if value is not None])
    unique_rift_hwnds = unique_values([value for value in rift_hwnds if value is not None])
    unique_rift_pids = unique_values([value for value in rift_pids if value is not None])
    unique_rift_titles = unique_values([value for value in rift_titles if value is not None])
    unique_rift_window_rects = unique_values([value for value in rift_window_rects if value is not None])
    unique_rift_client_rects = unique_values([value for value in rift_client_rects if value is not None])

    if focus_lost_samples:
        add_anomaly("focus_lost", "error", "One or more samples were not focus-verified.", sample_indexes=focus_lost_samples[:50], count=len(focus_lost_samples))

    if rift_process_dead_samples:
        add_anomaly("rift_process_not_alive", "error", "One or more samples reported RIFT process not alive.", sample_indexes=rift_process_dead_samples[:50], count=len(rift_process_dead_samples))

    if len(unique_foreground_hwnds) > 1:
        add_anomaly("foreground_hwnd_changed", "warning", "Foreground HWND changed during session.", values=unique_foreground_hwnds)

    if len(unique_foreground_pids) > 1:
        add_anomaly("foreground_pid_changed", "warning", "Foreground PID changed during session.", values=unique_foreground_pids)

    if len(unique_rift_hwnds) > 1:
        add_anomaly("rift_hwnd_changed", "error", "RIFT HWND changed during session.", values=unique_rift_hwnds)

    if len(unique_rift_pids) > 1:
        add_anomaly("rift_pid_changed", "error", "RIFT PID changed during session.", values=unique_rift_pids)

    if len(unique_rift_titles) > 1:
        add_anomaly("rift_window_title_changed", "warning", "RIFT window title changed during session.", values=unique_rift_titles)

    if len(unique_rift_window_rects) > 1:
        add_anomaly("rift_window_rect_changed", "warning", "RIFT window rect changed during session.", values=unique_rift_window_rects)

    if len(unique_rift_client_rects) > 1:
        add_anomaly("rift_client_rect_changed", "warning", "RIFT client rect changed during session.", values=unique_rift_client_rects)

    interval_deltas: list[float] = []
    if len(monotonic_values) >= 2:
        interval_deltas = [
            round(monotonic_values[index] - monotonic_values[index - 1], 3)
            for index in range(1, len(monotonic_values))
        ]

    sample_interval_ms = collector_summary.get("sample_interval_ms") or manifest.get("sample_interval_ms") or DEFAULT_WINDOW_PROCESS_SAMPLE_INTERVAL_MS
    expected_interval = float(sample_interval_ms) / 1000.0
    max_interval_delta = max(interval_deltas) if interval_deltas else None
    min_interval_delta = min(interval_deltas) if interval_deltas else None
    avg_interval_delta = round(sum(interval_deltas) / len(interval_deltas), 3) if interval_deltas else None
    max_abs_interval_drift = max((abs(delta - expected_interval) for delta in interval_deltas), default=0.0)

    if interval_deltas and max_abs_interval_drift > 0.25:
        add_anomaly(
            "sample_interval_drift",
            "warning",
            "Sample interval drift exceeded 250 ms from expected interval.",
            expected_interval_seconds=expected_interval,
            max_abs_interval_drift_seconds=round(max_abs_interval_drift, 3),
            min_interval_delta=min_interval_delta,
            max_interval_delta=max_interval_delta,
            avg_interval_delta=avg_interval_delta,
        )

    artifact_contract = manifest.get("artifact_contract") or collector_summary.get("artifact_contract") or {}
    if artifact_contract.get("status") != "PASS":
        add_anomaly("artifact_contract_not_pass", "error", "Artifact contract is not PASS.", artifact_contract=artifact_contract)

    summary = {
        "status": "PASS" if not any(item.get("severity") == "error" for item in anomalies) else "FAIL",
        "warning_count": sum(1 for item in anomalies if item.get("severity") == "warning"),
        "error_count": sum(1 for item in anomalies if item.get("severity") == "error"),
        "anomaly_count": len(anomalies),
        "session": session_rel,
        "sample_count": sample_count,
        "expected_sample_count": expected_sample_count,
        "focus_lost_count": len(focus_lost_samples),
        "rift_process_dead_count": len(rift_process_dead_samples),
        "unique_foreground_hwnds": unique_foreground_hwnds,
        "unique_foreground_pids": unique_foreground_pids,
        "unique_rift_hwnds": unique_rift_hwnds,
        "unique_rift_pids": unique_rift_pids,
        "unique_rift_titles": unique_rift_titles,
        "unique_rift_window_rects": unique_rift_window_rects,
        "unique_rift_client_rects": unique_rift_client_rects,
        "missing_sample_index_count": len(missing_sample_indexes),
        "duplicate_sample_index_count": len(duplicate_sample_indexes),
        "sample_interval_seconds": {
            "expected": expected_interval,
            "min": min_interval_delta,
            "max": max_interval_delta,
            "avg": avg_interval_delta,
            "max_abs_drift": round(max_abs_interval_drift, 3),
        },
        "artifact_contract_status": artifact_contract.get("status"),
    }

    analysis = {
        "schema_version": ANALYSIS_SCHEMA_VERSION,
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "session_dir": session_rel,
        "inputs": {
            "manifest": rel(manifest_path),
            "collector_summary": rel(collector_summary_path),
            "collector_samples": rel(collector_samples_path),
            "capture_log": rel(capture_log_path),
        },
        "outputs": {
            "analysis_json": rel(analysis_path),
            "anomalies_jsonl": rel(anomalies_path),
            "handoff": rel(handoff_path),
        },
        "summary": summary,
        "anomalies": anomalies,
    }

    write_json(analysis_path, analysis)

    anomalies_path.parent.mkdir(parents=True, exist_ok=True)
    with anomalies_path.open("w", encoding="utf-8") as handle:
        for anomaly in anomalies:
            handle.write(json.dumps(anomaly, ensure_ascii=False, sort_keys=True) + "\n")

    handoff_lines = [
        "# Window/Process Metadata Session Analysis",
        "",
        f"Created UTC: `{analysis['created_utc']}`",
        f"App version: `{APP_VERSION}`",
        f"Session: `{session_rel}`",
        f"Status: `{summary['status']}`",
        f"Samples: `{sample_count}`",
        f"Anomalies: `{len(anomalies)}`",
        f"Errors: `{summary['error_count']}`",
        f"Warnings: `{summary['warning_count']}`",
        "",
        "## Key Checks",
        "",
        f"- Focus lost count: `{summary['focus_lost_count']}`",
        f"- RIFT process dead count: `{summary['rift_process_dead_count']}`",
        f"- Missing sample indexes: `{summary['missing_sample_index_count']}`",
        f"- Duplicate sample indexes: `{summary['duplicate_sample_index_count']}`",
        f"- Artifact contract: `{summary['artifact_contract_status']}`",
        "",
        "## Files",
        "",
        "```text",
        rel(analysis_path),
        rel(anomalies_path),
        rel(handoff_path),
        "```",
        "",
        "## Notes",
        "",
        "This analysis is offline-only. It does not run capture, read process memory, send input, change focus, or run /reloadui.",
    ]

    if anomalies:
        handoff_lines.extend(["", "## Anomalies", ""])
        for anomaly in anomalies[:20]:
            handoff_lines.append(f"- `{anomaly.get('severity')}` `{anomaly.get('kind')}` — {anomaly.get('message')}")

    handoff_path.write_text("\n".join(handoff_lines) + "\n", encoding="utf-8")

    return {
        "session_dir": session_dir,
        "analysis_path": analysis_path,
        "anomalies_path": anomalies_path,
        "handoff_path": handoff_path,
    }


def write_operator_report() -> Path:
    OPERATOR_DIR.mkdir(parents=True, exist_ok=True)

    status_code, git_status, git_status_err = run_command(["git", "status", "--short"], timeout=30)
    log_code, git_log, git_log_err = run_command(["git", "log", "--oneline", "-5"], timeout=30)

    summary = load_json(FOCUS_SUMMARY)
    windows = load_json(WINDOWS_JSON)
    log_tail = tail_text(FOCUS_LOG, 60)
    dry_run = latest_dry_run_summary()
    capture_plan = latest_capture_plan_summary()
    capture_session = latest_capture_session_summary()
    window_process_analysis = latest_window_process_analysis_summary()

    focus_ok = summary.get("status") == "foreground_verified"
    full_ok, validation_issues = validate_full_live_preflight(summary, windows)
    issues: list[str] = [f"- {issue}" for issue in validation_issues]

    if status_code != 0:
        issues.append("- git status command failed.")
    if log_code != 0:
        issues.append("- git log command failed.")
    if not issues:
        issues.append("- No blocking operator issues detected.")

    report = f"""# RiftScan Operator Handoff

Created UTC: `{utc_now()}`
App version: `{APP_VERSION}`
Repo root: `{REPO_ROOT}`

## Operator Assessment

Full live preflight gate: `{"PASS" if full_ok and status_code == 0 and log_code == 0 else "FAIL"}`
Focus preflight: `{"PASS" if focus_ok else "FAIL"}`
Summary: `{focus_line(summary)}`

{chr(10).join(issues)}

## Git Status

Exit code: `{status_code}`

```text
{git_status or git_status_err}
```

## Recent Commits

Exit code: `{log_code}`

```text
{git_log or git_log_err}
```

## Focus Summary JSON

```json
{json_block(summary)}
```

## Windows JSON

```json
{json_block(windows)}
```

## Latest Focus-Gated Session Dry Run

```json
{json_block(dry_run)}
```

## Latest Focus-Gated Capture Plan

```json
{json_block(capture_plan)}
```

## Latest Focus-Gated Capture Session

```json
{json_block(capture_session)}
```

## Latest Window/Process Analysis

```json
{json_block(window_process_analysis)}
```

## Focus Log Tail

```jsonl
{log_tail}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
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
"""
    REPORT_PATH.write_text(report, encoding="utf-8")
    return REPORT_PATH


def create_focus_gated_session_dry_run(gate: dict[str, Any]) -> tuple[Path, Path, Path]:
    if not gate.get("pass_gate"):
        raise RuntimeError("Cannot create dry-run session because full live preflight did not pass.")

    summary = gate.get("summary") or {}
    windows = gate.get("windows") or {}
    process = summary.get("process") or {}
    selected = summary.get("selected_window") or {}

    session_id = f"{safe_timestamp()}_focus_gated_session_dry_run"
    session_dir = DRY_RUN_ROOT / session_id
    session_dir.mkdir(parents=True, exist_ok=False)

    manifest = {
        "schema_version": "riftscan.focus_gated_session_dry_run.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "session_id": session_id,
        "status": "dry_run_session_created",
        "dry_run": True,
        "full_live_preflight": {
            "status": "PASS",
            "focus_status": summary.get("status"),
            "process_id": process.get("Id"),
            "process_name": process.get("ProcessName"),
            "window_hwnd": selected.get("hwnd"),
            "window_hwnd_hex": selected.get("hwnd_hex"),
            "window_title": selected.get("title"),
            "windows_count": len(windows.get("windows") or []),
        },
        "guardrails": [
            "No live test sequence was started.",
            "No local data collection sequence was started.",
            "This session is metadata-only.",
        ],
        "source_artifacts": {
            "focus_summary": rel(FOCUS_SUMMARY),
            "windows_json": rel(WINDOWS_JSON),
            "focus_log": rel(FOCUS_LOG),
            "operator_report": rel(REPORT_PATH),
        },
        "next_expected_step": "Use this metadata-only session structure as the staging contract before wiring the first real focus-gated live-test workflow.",
    }

    manifest_path = session_dir / "manifest.json"
    manifest_path.write_text(json_block(manifest) + "\n", encoding="utf-8")

    handoff = f"""# Focus-Gated Session Dry Run

Session ID: `{session_id}`
Created UTC: `{manifest["created_utc"]}`
Status: `{manifest["status"]}`

## Result

The operator app created this metadata-only session after the full live preflight gate passed.

```text
FULL LIVE PREFLIGHT: PASS
Focus: {summary.get("status")}
PID: {process.get("Id")}
HWND: {selected.get("hwnd_hex")}
Title: {selected.get("title")}
```

## Guardrails

- No live test sequence was started.
- No local data collection sequence was started.
- This session is metadata-only.

## Manifest

```text
{rel(manifest_path)}
```

## Next Expected Step

Use this metadata-only session structure as the staging contract before wiring the first real focus-gated live-test workflow.
"""
    handoff_path = session_dir / "DRY_RUN_HANDOFF.md"
    handoff_path.write_text(handoff, encoding="utf-8")

    LATEST_DRY_RUN.parent.mkdir(parents=True, exist_ok=True)
    LATEST_DRY_RUN.write_text(rel(session_dir) + "\n", encoding="utf-8")

    return session_dir, manifest_path, handoff_path


def create_focus_gated_capture_plan(gate: dict[str, Any]) -> tuple[Path, Path, Path]:
    if not gate.get("pass_gate"):
        raise RuntimeError("Cannot create capture plan because full live preflight did not pass.")

    summary = gate.get("summary") or {}
    windows = gate.get("windows") or {}
    process = summary.get("process") or {}
    selected = summary.get("selected_window") or {}

    plan_id = f"{safe_timestamp()}_focus_gated_capture_plan"
    plan_dir = CAPTURE_PLAN_ROOT / plan_id
    plan_dir.mkdir(parents=True, exist_ok=False)

    expected_files = [
        "capture-session-manifest.json",
        "capture-log.jsonl",
        "focus-summary-before.json",
        "focus-summary-after.json",
        "operator-report.md",
    ]

    preflight_requirements = [
        "Full live preflight gate PASS",
        "Focus status == foreground_verified",
        "selected_window exists",
        "windows.json has at least one window",
        "RIFT process name == rift_x64",
        "Operator app is the controlling workflow",
        "No uncommitted tool-code changes unless intentionally testing new tool code",
    ]

    abort_conditions = [
        "Focus preflight fails",
        "RIFT process missing",
        "RIFT HWND missing",
        "Foreground HWND does not belong to RIFT",
        "Git state cannot be read",
        "Operator cancels",
        "Any planned capture file path already exists unexpectedly",
        "Any live-capture command would be required at this stage",
    ]

    manifest = {
        "schema_version": "riftscan.focus_gated_capture_plan.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "plan_id": plan_id,
        "status": "capture_plan_created",
        "metadata_only": True,
        "capture_started": False,
        "capture_completed": False,
        "capture_type": "focus_gated_manual_observation",
        "duration_target_seconds": 30,
        "stimulus_name": "none_metadata_only",
        "expected_files": expected_files,
        "preflight_requirements": preflight_requirements,
        "abort_conditions": abort_conditions,
        "operator_notes": (
            "Metadata-only plan generated by RiftScan Operator. This does not start capture. "
            "Use as staging contract before implementing real focus-gated capture."
        ),
        "full_live_preflight": {
            "status": "PASS",
            "focus_status": summary.get("status"),
            "process_id": process.get("Id"),
            "process_name": process.get("ProcessName") or process.get("Name"),
            "window_hwnd": selected.get("hwnd"),
            "window_hwnd_hex": selected.get("hwnd_hex"),
            "window_title": selected.get("title"),
            "windows_count": len(windows.get("windows") or []),
        },
        "source_artifacts": {
            "focus_summary": rel(FOCUS_SUMMARY),
            "windows_json": rel(WINDOWS_JSON),
            "focus_log": rel(FOCUS_LOG),
            "operator_report": rel(REPORT_PATH),
            "latest_dry_run_pointer": rel(LATEST_DRY_RUN),
        },
        "guardrails": [
            "Metadata only.",
            "No capture started.",
            "No live test sequence started.",
            "No local data collection sequence started.",
            "No movement/input sent.",
            "No memory scan/read started.",
            "No /reloadui sent.",
        ],
        "next_expected_step": (
            "Use this capture plan as the staging contract before implementing real focus-gated capture."
        ),
    }

    manifest_path = plan_dir / "capture-plan.json"
    manifest_path.write_text(json_block(manifest) + "\n", encoding="utf-8")

    handoff = f"""# Focus-Gated Capture Plan

Plan ID: `{plan_id}`
Created UTC: `{manifest["created_utc"]}`
Status: `{manifest["status"]}`
Metadata only: `{manifest["metadata_only"]}`

## Result

The operator app created this metadata-only capture plan after the full live preflight gate passed.

```text
FULL LIVE PREFLIGHT: PASS
Focus: {summary.get("status")}
PID: {process.get("Id")}
HWND: {selected.get("hwnd_hex")}
Title: {selected.get("title")}
```

## Planned Capture Contract

```json
{json_block({
    "capture_type": manifest["capture_type"],
    "duration_target_seconds": manifest["duration_target_seconds"],
    "stimulus_name": manifest["stimulus_name"],
    "expected_files": expected_files,
    "preflight_requirements": preflight_requirements,
    "abort_conditions": abort_conditions,
})}
```

## Guardrails

- Metadata only.
- No capture started.
- No live test sequence started.
- No local data collection sequence started.
- No movement/input sent.
- No memory scan/read started.
- No /reloadui sent.

## Manifest

```text
{rel(manifest_path)}
```

## Next Expected Step

Use this capture plan as the staging contract before implementing real focus-gated capture.
"""
    handoff_path = plan_dir / "CAPTURE_PLAN_HANDOFF.md"
    handoff_path.write_text(handoff, encoding="utf-8")

    LATEST_CAPTURE_PLAN.parent.mkdir(parents=True, exist_ok=True)
    LATEST_CAPTURE_PLAN.write_text(rel(plan_dir) + "\n", encoding="utf-8")

    return plan_dir, manifest_path, handoff_path


def run_focus_gated_capture_scaffold(gate: dict[str, Any]) -> tuple[Path, Path, Path, Path, Path, Path]:
    if not gate.get("pass_gate"):
        raise RuntimeError("Cannot run capture scaffold because full live preflight did not pass.")

    plan_summary = latest_capture_plan_summary()
    if plan_summary.get("status") != "present":
        raise RuntimeError("Cannot run capture scaffold because no latest focus-gated capture plan exists.")

    plan_manifest = plan_summary.get("manifest") or {}
    if plan_manifest.get("metadata_only") is not True:
        raise RuntimeError("Latest capture plan manifest is invalid: metadata_only is not true.")
    if plan_manifest.get("capture_started") is not False:
        raise RuntimeError("Latest capture plan manifest is invalid: capture_started is not false.")
    if plan_manifest.get("capture_completed") is not False:
        raise RuntimeError("Latest capture plan manifest is invalid: capture_completed is not false.")

    duration_seconds = bounded_duration_seconds(plan_manifest.get("duration_target_seconds"), default=30)
    summary_before = gate.get("summary") or {}
    windows_before = gate.get("windows") or {}
    process = summary_before.get("process") or {}
    selected = summary_before.get("selected_window") or {}

    session_id = f"{safe_timestamp()}_focus_gated_capture_scaffold"
    session_dir = CAPTURE_SESSION_ROOT / session_id
    session_dir.mkdir(parents=True, exist_ok=False)

    manifest_path = session_dir / "capture-session-manifest.json"
    log_path = session_dir / "capture-log.jsonl"
    focus_before_path = session_dir / "focus-summary-before.json"
    focus_after_path = session_dir / "focus-summary-after.json"
    operator_report_copy = session_dir / "operator-report.md"
    handoff_path = session_dir / "CAPTURE_SESSION_HANDOFF.md"

    focus_before_path.write_text(json_block(summary_before) + "\n", encoding="utf-8")

    started_utc = utc_now()
    append_jsonl(log_path, {
        "event": "capture_scaffold_started",
        "utc": started_utc,
        "session_id": session_id,
        "duration_target_seconds": duration_seconds,
        "focus_status": summary_before.get("status"),
        "pid": process.get("Id"),
        "hwnd_hex": selected.get("hwnd_hex"),
        "plan": plan_summary.get("latest_plan"),
        "guardrail": "focus metadata/log structure only; no movement/input/memory scan/read/reloadui",
    })

    interim_manifest = {
        "schema_version": "riftscan.focus_gated_capture_session_scaffold.v2",
        "created_utc": started_utc,
        "app_version": APP_VERSION,
        "session_id": session_id,
        "status": "capture_scaffold_running",
        "scaffold_only": True,
        "capture_mode": "focus_metadata_only_scaffold",
        "scaffold_window_started": True,
        "scaffold_window_completed": False,
        "real_capture_started": False,
        "real_capture_completed": False,
        "capture_started": False,
        "capture_completed": False,
        "capture_fields_note": (
            "Legacy capture_started/capture_completed remain false because no real collector ran. "
            "Use scaffold_window_started/scaffold_window_completed for scaffold timing."
        ),
        "duration_target_seconds": duration_seconds,
        "stimulus_name": plan_manifest.get("stimulus_name", "none_metadata_only"),
        "source_capture_plan": plan_summary,
        "full_live_preflight": {
            "status": "PASS",
            "focus_status": summary_before.get("status"),
            "process_id": process.get("Id"),
            "process_name": process.get("ProcessName") or process.get("Name"),
            "window_hwnd": selected.get("hwnd"),
            "window_hwnd_hex": selected.get("hwnd_hex"),
            "window_title": selected.get("title"),
            "windows_count": len(windows_before.get("windows") or []),
        },
        "files": {
            "capture_session_manifest": rel(manifest_path),
            "capture_log": rel(log_path),
            "focus_summary_before": rel(focus_before_path),
            "focus_summary_after": rel(focus_after_path),
            "operator_report": rel(operator_report_copy),
            "handoff": rel(handoff_path),
        },
        "guardrails": [
            "Timed capture scaffold only.",
            "Focus metadata/log structure only.",
            "No movement/input sent.",
            "No memory scan/read started.",
            "No /reloadui sent.",
        ],
    }
    manifest_path.write_text(json_block(interim_manifest) + "\n", encoding="utf-8")

    start_monotonic = time.monotonic()
    next_heartbeat = 5
    while True:
        elapsed = time.monotonic() - start_monotonic
        if elapsed >= duration_seconds:
            break
        if elapsed >= next_heartbeat:
            append_jsonl(log_path, {
                "event": "capture_scaffold_heartbeat",
                "utc": utc_now(),
                "session_id": session_id,
                "elapsed_seconds": round(elapsed, 3),
                "remaining_seconds": max(0, round(duration_seconds - elapsed, 3)),
            })
            next_heartbeat += 5
        time.sleep(0.25)

    append_jsonl(log_path, {
        "event": "capture_scaffold_window_elapsed",
        "utc": utc_now(),
        "session_id": session_id,
        "elapsed_seconds": round(time.monotonic() - start_monotonic, 3),
    })

    after_code, after_out, after_err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)
    summary_after = load_json(FOCUS_SUMMARY)
    focus_after_path.write_text(json_block(summary_after) + "\n", encoding="utf-8")

    completed_utc = utc_now()
    final_status = "capture_scaffold_completed" if after_code == 0 else "capture_scaffold_completed_with_focus_after_failure"

    append_jsonl(log_path, {
        "event": "capture_scaffold_completed",
        "utc": completed_utc,
        "session_id": session_id,
        "status": final_status,
        "focus_after_exit_code": after_code,
        "focus_after_status": summary_after.get("status"),
    })

    final_manifest = dict(interim_manifest)
    final_manifest.update({
        "completed_utc": completed_utc,
        "status": final_status,
        "scaffold_window_completed": True,
        "real_capture_started": False,
        "real_capture_completed": False,
        "capture_started": False,
        "capture_completed": False,
        "elapsed_seconds": round(time.monotonic() - start_monotonic, 3),
        "focus_after": {
            "command_exit_code": after_code,
            "stdout": after_out.strip(),
            "stderr": after_err.strip(),
            "focus_status": summary_after.get("status"),
        },
        "next_expected_step": "Review scaffold artifacts, then wire the first real collector behind this same focus gate.",
    })
    manifest_path.write_text(json_block(final_manifest) + "\n", encoding="utf-8")

    report_path = write_operator_report()
    operator_report_copy.write_text(read_text(report_path), encoding="utf-8")

    handoff_lines = [
        "# Focus-Gated Capture Session Scaffold",
        "",
        f"Session ID: `{session_id}`",
        f"Created UTC: `{started_utc}`",
        f"Completed UTC: `{completed_utc}`",
        f"Status: `{final_status}`",
        f"Duration target seconds: `{duration_seconds}`",
        "",
        "## Result",
        "",
        "The operator app opened and closed a timed focus-gated scaffold window. This is session wiring only: no real capture collector ran.",
        "",
        "```text",
        f"Focus before: {summary_before.get('status')}",
        f"Focus after: {summary_after.get('status')}",
        f"PID: {process.get('Id')}",
        f"HWND: {selected.get('hwnd_hex')}",
        f"Title: {selected.get('title')}",
        "```",
        "",
        "## Files",
        "",
        "```text",
        rel(manifest_path),
        rel(log_path),
        rel(focus_before_path),
        rel(focus_after_path),
        rel(operator_report_copy),
        "```",
        "",
        "## Guardrails",
        "",
        "- Timed capture scaffold only.",
        "- Focus metadata/log structure only.",
        "- Real capture collector did not run.",
        "- No movement/input sent.",
        "- No memory scan/read started.",
        "- No /reloadui sent.",
        "",
        "## Next Expected Step",
        "",
        "Review scaffold artifacts, then wire the first real collector behind this same focus gate.",
        "",
    ]
    handoff_path.write_text("\n".join(handoff_lines), encoding="utf-8")

    LATEST_CAPTURE_SESSION.parent.mkdir(parents=True, exist_ok=True)
    LATEST_CAPTURE_SESSION.write_text(rel(session_dir) + "\n", encoding="utf-8")

    return session_dir, manifest_path, log_path, focus_before_path, focus_after_path, handoff_path


def clean_known_junk() -> list[str]:
    removed: list[str] = []

    for name in JUNK_LITERAL:
        path = REPO_ROOT / name
        if path.exists():
            if path.is_dir():
                shutil.rmtree(path)
            else:
                path.unlink()
            removed.append(rel(path))

    for pattern in JUNK_GLOBS:
        for path in REPO_ROOT.glob(pattern):
            if path.exists():
                if path.is_dir():
                    shutil.rmtree(path)
                else:
                    path.unlink()
                removed.append(rel(path))

    return removed


class RiftScanOperatorApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title(f"RiftScan Operator — {APP_VERSION}")
        self.geometry("1100x720")
        self.minsize(900, 580)

        self.status_var = tk.StringVar(value="Ready.")
        self.focus_var = tk.StringVar(value="Focus: unknown")
        self.busy = False

        header = tk.Frame(self)
        header.pack(fill=tk.X, padx=10, pady=(10, 4))

        tk.Label(header, text="RiftScan Operator", font=("Segoe UI", 16, "bold")).pack(side=tk.LEFT)
        tk.Label(header, text=APP_VERSION, font=("Segoe UI", 9), foreground="gray").pack(side=tk.LEFT, padx=(10, 0))
        tk.Label(header, textvariable=self.focus_var, font=("Segoe UI", 10)).pack(side=tk.RIGHT)

        button_tabs = ttk.Notebook(self)
        button_tabs.pack(fill=tk.X, padx=10, pady=4)

        main_tab = tk.Frame(button_tabs)
        planning_tab = tk.Frame(button_tabs)
        diagnostics_tab = tk.Frame(button_tabs)
        legacy_tab = tk.Frame(button_tabs)
        git_tab = tk.Frame(button_tabs)

        button_tabs.add(main_tab, text="Main")
        button_tabs.add(planning_tab, text="Planning")
        button_tabs.add(diagnostics_tab, text="Diagnostics")
        button_tabs.add(legacy_tab, text="Legacy")
        button_tabs.add(git_tab, text="Git / Maintenance")

        self.add_button_grid(
            main_tab,
            [
                ("Refresh Status", self.refresh_status),
                ("Run Window/Process Metadata Collector", self.run_window_process_metadata_collector),
                ("Analyze Latest Session", self.analyze_latest_session_clicked),
                ("Open Report", self.open_report_clicked),
            ],
            columns=3,
        )

        self.add_button_grid(
            planning_tab,
            [
                ("Create Focus-Gated Capture Plan", self.create_focus_gated_capture_plan_clicked),
            ],
            columns=3,
        )

        self.add_button_grid(
            diagnostics_tab,
            [
                ("Run Full Live Preflight", self.run_full_live_preflight),
                ("Run Focus Preflight", self.run_focus_preflight),
            ],
            columns=3,
        )

        self.add_button_grid(
            legacy_tab,
            [
                ("Run Focus-Gated Session Dry Run", self.run_focus_gated_session_dry_run),
                ("Write AI Report", self.write_report_clicked),
            ],
            columns=3,
        )

        self.add_button_grid(
            git_tab,
            [
                ("Clean Known Junk", self.clean_junk_clicked),
                ("Commit Allowlist", self.commit_clicked),
                ("Push", self.push_clicked),
            ],
            columns=3,
        )

        tk.Label(self, textvariable=self.status_var, anchor="w").pack(fill=tk.X, padx=10, pady=(2, 4))

        self.output = scrolledtext.ScrolledText(self, wrap=tk.WORD, font=("Consolas", 10))
        self.output.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))
        self.configure_output_tags()

        self.append(f"Repo root: {REPO_ROOT}\n")
        self.refresh_status()

    def add_button_grid(self, parent: tk.Widget, buttons: list[tuple[str, Any]], columns: int = 3) -> None:
        for index, (label, command) in enumerate(buttons):
            row = index // columns
            column = index % columns
            tk.Button(parent, text=label, command=command).grid(
                row=row,
                column=column,
                padx=4,
                pady=4,
                sticky="ew",
            )

        for column in range(columns):
            parent.grid_columnconfigure(column, weight=1)

    def configure_output_tags(self) -> None:
        self.output.tag_configure("pass", foreground="#2e7d32", font=("Consolas", 10, "bold"))
        self.output.tag_configure("fail", foreground="#c62828", font=("Consolas", 10, "bold"))
        self.output.tag_configure("warning", foreground="#b26a00", font=("Consolas", 10, "bold"))
        self.output.tag_configure("running", foreground="#1565c0", font=("Consolas", 10, "bold"))
        self.output.tag_configure("section", foreground="#4e342e", font=("Consolas", 10, "bold"))
        self.output.tag_configure("path", foreground="#00695c")
        self.output.tag_configure("muted", foreground="#616161")

    def classify_output_line(self, line: str) -> str | None:
        stripped = line.strip()
        lower = stripped.lower()

        if not stripped:
            return None

        if stripped.startswith("===") or stripped.startswith("# "):
            return "section"

        if lower.startswith("running:") or "in progress" in lower:
            return "running"

        if stripped.startswith("??") or stripped.startswith(" M ") or stripped.startswith("MM "):
            return "warning"

        if (
            "errors: 0" in lower
            or '"error_count": 0' in lower
            or "error_count: 0" in lower
            or "missing_artifacts\": []" in lower
            or "missing_artifacts: []" in lower
            or "artifact contract: pass" in lower
            or "artifact_contract_status\": \"pass\"" in lower
        ):
            return "pass"

        fail_tokens = [
            "fail",
            "error:",
            "blocked",
            "traceback",
            "exception",
            "missing_artifacts",
            "artifact contract: fail",
            "artifact_contract_status\": \"fail\"",
            "exit=1",
            "exit code: 1",
            "completed_with_artifact_warning",
            "focus lost samples: 1",
            "focus lost samples: 2",
            "focus lost samples: 3",
            "focus lost samples: 4",
            "focus lost samples: 5",
            "focus_lost_count\": 1",
            "focus_lost_count\": 2",
            "focus_lost_count\": 3",
            "focus_lost_count\": 4",
            "focus_lost_count\": 5",
        ]
        if any(token in lower for token in fail_tokens):
            return "fail"

        warning_tokens = [
            "warning",
            "dirty",
            "untracked",
            "completed_with",
            "artifact warning",
            "skipped",
        ]
        if any(token in lower for token in warning_tokens):
            return "warning"

        pass_tokens = [
            "pass",
            "ok",
            "success",
            "foreground_verified",
            "exit=0",
            "exit code: `0`",
            "collector completed",
            "samples: 60",
            "focus verified samples:",
            "git push exit=0",
            "git commit exit=0",
        ]
        if any(token in lower for token in pass_tokens):
            return "pass"

        if (
            "sessions/" in stripped
            or "handoffs/" in stripped
            or "plans/" in stripped
            or "tools/" in stripped
            or "scripts/" in stripped
            or "C:\\" in stripped
        ):
            return "path"

        if re.search(r"\\b[0-9a-f]{7,40}\\b", stripped, flags=re.IGNORECASE):
            return "muted"

        return None

    def append(self, text: str) -> None:
        if not text.endswith("\n"):
            text += "\n"

        for line in text.splitlines(keepends=True):
            start = self.output.index(tk.END)
            self.output.insert(tk.END, line)
            end = self.output.index(tk.END)
            tag = self.classify_output_line(line.rstrip("\r\n"))
            if tag:
                self.output.tag_add(tag, start, end)

        self.output.see(tk.END)

    def set_status(self, text: str) -> None:
        self.status_var.set(text)
        self.append(text)

    def run_async(self, label: str, func: Any) -> None:
        if self.busy:
            self.append(f"\nBLOCKED: another Operator action is already running; skipped {label}.")
            return

        self.busy = True

        def finish_ready() -> None:
            self.status_var.set("Ready.")
            self.busy = False

        def worker() -> None:
            self.after(0, lambda: self.set_status(f"Running: {label}"))
            try:
                result = func()
                if result is not None:
                    self.after(0, lambda: self.append(str(result)))
            except Exception as exc:
                self.after(0, lambda: messagebox.showerror("RiftScan Operator", f"{type(exc).__name__}: {exc}"))
                self.after(0, lambda: self.append(f"ERROR: {type(exc).__name__}: {exc}"))
            finally:
                self.after(0, finish_ready)

        threading.Thread(target=worker, daemon=True).start()

    def refresh_status(self) -> None:
        summary = load_json(FOCUS_SUMMARY)
        line = focus_line(summary)
        self.focus_var.set(f"Focus: {line}")

        code, status, err = run_command(["git", "status", "--short"], timeout=30)
        log_code, log, log_err = run_command(["git", "log", "--oneline", "-3"], timeout=30)

        self.append("\n=== STATUS ===")
        self.append(line)
        self.append("\nGit status:")
        self.append(status if code == 0 else err)
        self.append("\nRecent commits:")
        self.append(log if log_code == 0 else log_err)

    def run_focus_preflight(self) -> None:
        def task() -> str:
            if not FOCUS_SCRIPT.exists():
                return f"ERROR: missing {rel(FOCUS_SCRIPT)}"

            code, out, err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)
            summary = load_json(FOCUS_SUMMARY)
            report_path = write_operator_report()
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            lines = [
                "\n=== FOCUS PREFLIGHT ===",
                f"Exit code: {code}",
                out.strip(),
                err.strip(),
                focus_line(summary),
                f"Operator report: {rel(report_path)}",
            ]
            return "\n".join(line for line in lines if line)

        self.run_async("focus preflight", task)

    def run_full_live_preflight(self) -> None:
        def task() -> str:
            gate = run_full_live_preflight_gate()
            report_path = write_operator_report()
            summary = gate["summary"]
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            process = summary.get("process") or {}
            selected = summary.get("selected_window") or {}
            pid = process.get("Id", "n/a")
            hwnd = selected.get("hwnd_hex", "n/a")
            title = selected.get("title", "n/a")

            lines = ["\n=== FULL LIVE PREFLIGHT ==="]
            if gate["pass_gate"]:
                lines.extend(
                    [
                        "FULL LIVE PREFLIGHT: PASS",
                        f"Focus: {summary.get('status', 'unknown')}",
                        f"PID: {pid}",
                        f"HWND: {hwnd}",
                        f"Title: {title}",
                        f"Operator report: {rel(report_path)}",
                    ]
                )
            else:
                lines.append("FULL LIVE PREFLIGHT: FAIL")
                lines.extend(f"- {issue}" for issue in gate["issues"])
                lines.append(f"Operator report: {rel(report_path)}")

            lines.extend(
                [
                    "",
                    "Focus-control stdout:",
                    gate["focus_stdout"].strip() or "[empty]",
                    "",
                    "Focus-control stderr:",
                    gate["focus_stderr"].strip() or "[empty]",
                    "",
                    "git status --short:",
                    gate["git_status"].strip()
                    if gate["git_status_code"] == 0 and gate["git_status"].strip()
                    else ("[clean]" if gate["git_status_code"] == 0 else gate["git_status_err"].strip()),
                    "",
                    "git log --oneline -5:",
                    gate["git_log"].strip() if gate["git_log_code"] == 0 else gate["git_log_err"].strip(),
                ]
            )
            return "\n".join(lines)

        self.run_async("full live preflight", task)

    def run_focus_gated_session_dry_run(self) -> None:
        def task() -> str:
            gate = run_full_live_preflight_gate()
            summary = gate["summary"]
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            report_path = write_operator_report()

            if not gate["pass_gate"]:
                lines = [
                    "\n=== FOCUS-GATED SESSION DRY RUN ===",
                    "FOCUS-GATED SESSION DRY RUN: FAIL",
                    "Dry-run session was not created because the full live preflight gate failed.",
                ]
                lines.extend(f"- {issue}" for issue in gate["issues"])
                lines.append(f"Operator report: {rel(report_path)}")
                return "\n".join(lines)

            session_dir, manifest_path, handoff_path = create_focus_gated_session_dry_run(gate)
            report_path = write_operator_report()

            selected = (summary.get("selected_window") or {})
            process = (summary.get("process") or {})

            lines = [
                "\n=== FOCUS-GATED SESSION DRY RUN ===",
                "FOCUS-GATED SESSION DRY RUN: PASS",
                "Created dry-run session metadata only.",
                f"Session: {rel(session_dir)}",
                f"Manifest: {rel(manifest_path)}",
                f"Handoff: {rel(handoff_path)}",
                f"Focus: {summary.get('status', 'unknown')}",
                f"PID: {process.get('Id', 'n/a')}",
                f"HWND: {selected.get('hwnd_hex', 'n/a')}",
                f"Title: {selected.get('title', 'n/a')}",
                f"Operator report: {rel(report_path)}",
                "",
                "No live test sequence or local data collection sequence was started.",
            ]
            return "\n".join(lines)

        self.run_async("focus-gated session dry run", task)


    def create_focus_gated_capture_plan_clicked(self) -> None:
        def task() -> str:
            gate = run_full_live_preflight_gate()
            summary = gate["summary"]
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            report_path = write_operator_report()

            if not gate["pass_gate"]:
                lines = [
                    "\n=== FOCUS-GATED CAPTURE PLAN ===",
                    "FOCUS-GATED CAPTURE PLAN: FAIL",
                    "Capture plan was not created because the full live preflight gate failed.",
                ]
                lines.extend(f"- {issue}" for issue in gate["issues"])
                lines.append(f"Operator report: {rel(report_path)}")
                return "\n".join(lines)

            plan_dir, manifest_path, handoff_path = create_focus_gated_capture_plan(gate)
            report_path = write_operator_report()

            selected = summary.get("selected_window") or {}
            process = summary.get("process") or {}

            lines = [
                "\n=== FOCUS-GATED CAPTURE PLAN ===",
                "FOCUS-GATED CAPTURE PLAN: PASS",
                "Created metadata-only capture plan.",
                f"Plan: {rel(plan_dir)}",
                f"Manifest: {rel(manifest_path)}",
                f"Handoff: {rel(handoff_path)}",
                f"Focus: {summary.get('status', 'unknown')}",
                f"PID: {process.get('Id', 'n/a')}",
                f"HWND: {selected.get('hwnd_hex', 'n/a')}",
                f"Title: {selected.get('title', 'n/a')}",
                f"Operator report: {rel(report_path)}",
                "",
                "No capture, movement, input, memory scan, or /reloadui was run.",
            ]
            return "\n".join(lines)

        self.run_async("focus-gated capture plan", task)


    def run_focus_gated_capture_scaffold_clicked(self) -> None:
        def task() -> str:
            gate = run_full_live_preflight_gate()
            summary = gate["summary"]
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            report_path = write_operator_report()

            if not gate["pass_gate"]:
                lines = [
                    "\n=== FOCUS-GATED CAPTURE SCAFFOLD ===",
                    "FOCUS-GATED CAPTURE SCAFFOLD: FAIL",
                    "Capture scaffold was not created because the full live preflight gate failed.",
                ]
                lines.extend(f"- {issue}" for issue in gate["issues"])
                lines.append(f"Operator report: {rel(report_path)}")
                return "\n".join(lines)

            try:
                session_dir, manifest_path, log_path, focus_before_path, focus_after_path, handoff_path = run_focus_gated_capture_scaffold(gate)
            except Exception as exc:
                report_path = write_operator_report()
                return "\n".join([
                    "\n=== FOCUS-GATED CAPTURE SCAFFOLD ===",
                    "FOCUS-GATED CAPTURE SCAFFOLD: FAIL",
                    f"- {type(exc).__name__}: {exc}",
                    f"Operator report: {rel(report_path)}",
                ])

            report_path = write_operator_report()
            summary_after = load_json(FOCUS_SUMMARY)
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary_after)}"))

            lines = [
                "\n=== FOCUS-GATED CAPTURE SCAFFOLD ===",
                "FOCUS-GATED CAPTURE SCAFFOLD: PASS",
                "Created timed focus-gated capture scaffold.",
                f"Session: {rel(session_dir)}",
                f"Manifest: {rel(manifest_path)}",
                f"Log: {rel(log_path)}",
                f"Focus before: {rel(focus_before_path)}",
                f"Focus after: {rel(focus_after_path)}",
                f"Handoff: {rel(handoff_path)}",
                f"Operator report: {rel(report_path)}",
                "",
                "This opened and closed a timed capture window, but recorded focus metadata/log structure only.",
                "No movement, input, memory scan/read, or /reloadui was run.",
            ]
            return "\n".join(lines)

        self.run_async("focus-gated capture scaffold", task)



    def run_window_process_metadata_collector(self) -> None:
        def task() -> str:
            gate = run_full_live_preflight_gate()
            summary = gate["summary"]
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            report_path = write_operator_report()

            if not gate["pass_gate"]:
                lines = [
                    "\n=== WINDOW/PROCESS METADATA COLLECTOR ===",
                    "WINDOW/PROCESS METADATA COLLECTOR: FAIL",
                    "Collector did not run because the full live preflight gate failed.",
                ]
                lines.extend(f"- {issue}" for issue in gate["issues"])
                lines.append(f"Operator report: {rel(report_path)}")
                return "\n".join(lines)

            artifacts = run_focus_gated_window_process_metadata_collector(gate)
            summary_path = artifacts["collector_summary_path"]
            collector_summary = load_json(summary_path)

            selected = summary.get("selected_window") or {}
            process = summary.get("process") or {}

            lines = [
                "\n=== WINDOW/PROCESS METADATA COLLECTOR ===",
                "WINDOW/PROCESS METADATA COLLECTOR: PASS",
                "Collected OS/window/process/focus metadata only.",
                f"Session: {rel(artifacts['session_dir'])}",
                f"Manifest: {rel(artifacts['manifest_path'])}",
                f"Capture log: {rel(artifacts['capture_log_path'])}",
                f"Collector samples: {rel(artifacts['collector_samples_path'])}",
                f"Collector summary: {rel(summary_path)}",
                f"Collector errors: {rel(artifacts['collector_errors_path'])}",
                f"Handoff: {rel(artifacts['handoff_path'])}",
                f"Focus: {summary.get('status', 'unknown')}",
                f"PID: {process.get('Id', 'n/a')}",
                f"HWND: {selected.get('hwnd_hex', 'n/a')}",
                f"Title: {selected.get('title', 'n/a')}",
                f"Samples: {collector_summary.get('sample_count', 'n/a')}",
                f"Errors: {collector_summary.get('error_count', 'n/a')}",
                f"Artifact contract: {(collector_summary.get('artifact_contract') or {}).get('status', 'n/a')}",
                f"Operator report: {rel(artifacts['operator_report_path'])}",
                "",
                "No process memory read, movement, input, or /reloadui was run.",
            ]
            return "\n".join(lines)

        self.run_async("window/process metadata collector", task)


    def analyze_latest_session_clicked(self) -> None:
        def task() -> str:
            artifacts = analyze_latest_window_process_session()
            analysis = load_json(artifacts["analysis_path"])
            summary = analysis.get("summary") or {}
            report_path = write_operator_report()

            lines = [
                "\n=== ANALYZE LATEST SESSION ===",
                f"ANALYZE LATEST SESSION: {summary.get('status', 'UNKNOWN')}",
                f"Session: {rel(artifacts['session_dir'])}",
                f"Analysis: {rel(artifacts['analysis_path'])}",
                f"Anomalies: {rel(artifacts['anomalies_path'])}",
                f"Handoff: {rel(artifacts['handoff_path'])}",
                f"Samples: {summary.get('sample_count', 'n/a')}",
                f"Anomalies: {summary.get('anomaly_count', 'n/a')}",
                f"Errors: {summary.get('error_count', 'n/a')}",
                f"Warnings: {summary.get('warning_count', 'n/a')}",
                f"Focus lost: {summary.get('focus_lost_count', 'n/a')}",
                f"Artifact contract: {summary.get('artifact_contract_status', 'n/a')}",
                f"Operator report: {rel(report_path)}",
                "",
                "Offline analysis only. No capture, memory read, input, focus change, or /reloadui was run.",
            ]
            return "\n".join(lines)

        self.run_async("analyze latest session", task)

    def write_report_clicked(self) -> None:
        path = write_operator_report()
        self.append(f"\nWrote operator report: {rel(path)}")

    def clean_junk_clicked(self) -> None:
        if not messagebox.askyesno("Clean known junk", "Remove known junk files/cache directories?"):
            return
        removed = clean_known_junk()
        if removed:
            self.append("\nRemoved:")
            for item in removed:
                self.append(f"- {item}")
        else:
            self.append("\nNo known junk found.")
        self.refresh_status()

    def commit_clicked(self) -> None:
        if not messagebox.askyesno(
            "Commit allowlisted files",
            "Stage only allowlisted focus/operator/session paths and commit? Push remains separate.",
        ):
            return

        def task() -> str:
            write_operator_report()
            allowlisted_existing = [path for path in ALLOWLIST if (REPO_ROOT / path).exists()]
            force_existing = [path for path in FORCE_ADD_ALLOWLIST if (REPO_ROOT / path).exists()]
            regular_existing: list[str] = []

            for path in allowlisted_existing:
                if path in force_existing:
                    continue
                if is_git_ignored(path):
                    force_existing.append(path)
                else:
                    regular_existing.append(path)

            force_existing = list(dict.fromkeys(force_existing))

            if not regular_existing and not force_existing:
                return "No allowlisted paths exist to stage."

            add_outputs: list[str] = []

            if regular_existing:
                add_code, add_out, add_err = run_command(["git", "add", "--", *regular_existing], timeout=60)
                add_outputs.extend([add_out.strip(), add_err.strip()])
                if add_code != 0:
                    return f"git add failed:\n{add_out}\n{add_err}"

            if force_existing:
                force_code, force_out, force_err = run_command(["git", "add", "-f", "--", *force_existing], timeout=60)
                add_outputs.extend([force_out.strip(), force_err.strip()])
                if force_code != 0:
                    return f"git add -f failed for ignored allowlist paths:\n{force_out}\n{force_err}"

            diff_code, _, _ = run_command(["git", "diff", "--cached", "--quiet"], timeout=30)
            if diff_code == 0:
                return "No staged changes after allowlisted git add."

            commit_code, commit_out, commit_err = run_command(
                ["git", "commit", "-m", "Update RiftScan operator handoff"],
                timeout=90,
            )
            self.after(0, self.refresh_status)
            return f"git commit exit={commit_code}\n{commit_out}\n{commit_err}"

        self.run_async("commit allowlisted files", task)

    def push_clicked(self) -> None:
        if not messagebox.askyesno("Push", "Run git push for the current branch?"):
            return

        def task() -> str:
            code, out, err = run_command(["git", "push"], timeout=120)
            self.after(0, self.refresh_status)
            return f"git push exit={code}\n{out}\n{err}"

        self.run_async("push", task)

    def open_report_clicked(self) -> None:
        if not REPORT_PATH.exists():
            write_operator_report()
        try:
            os.startfile(str(REPORT_PATH))  # type: ignore[attr-defined]
        except Exception as exc:
            messagebox.showerror("Open report failed", f"{type(exc).__name__}: {exc}")


def main() -> int:
    app = RiftScanOperatorApp()
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# End of script
