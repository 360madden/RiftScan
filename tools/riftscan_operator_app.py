# Version: riftscan-operator-app-v3.3
# Purpose: Windows Tkinter helper app for RiftScan operator workflow: run focus preflight, run full live preflight gate, create focus-gated session dry-run manifests, create metadata-only focus-gated capture plans, run focus-gated timed capture scaffolds, validate handoffs, write AI-ready reports, clean known junk, and safely commit/push allowlisted files, including ignored artifact paths when needed.
# Total character count: 46678

from __future__ import annotations

import datetime as dt
import json
import os
import shutil
import subprocess
import threading
import time
import tkinter as tk
from pathlib import Path
from tkinter import messagebox, scrolledtext
from typing import Any


APP_VERSION = "riftscan-operator-app-v3.3"
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
    return {
        "status": "present",
        "latest_session": session_rel,
        "manifest_path": rel(manifest_path),
        "handoff_path": rel(handoff_path),
        "manifest": manifest,
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
- The focus-gated capture scaffold may open a timed session, but records focus metadata/log structure only.
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
        "schema_version": "riftscan.focus_gated_capture_session_scaffold.v1",
        "created_utc": started_utc,
        "app_version": APP_VERSION,
        "session_id": session_id,
        "status": "capture_scaffold_running",
        "scaffold_only": True,
        "capture_started": True,
        "capture_completed": False,
        "capture_mode": "focus_metadata_only_scaffold",
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
        "capture_completed": True,
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
        "The operator app opened and closed a timed focus-gated capture scaffold. This is the first capture-session wiring layer, but it records focus metadata/log structure only.",
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
        self.title("RiftScan Operator")
        self.geometry("1100x720")
        self.minsize(900, 580)

        self.status_var = tk.StringVar(value="Ready.")
        self.focus_var = tk.StringVar(value="Focus: unknown")

        header = tk.Frame(self)
        header.pack(fill=tk.X, padx=10, pady=(10, 4))

        tk.Label(header, text="RiftScan Operator", font=("Segoe UI", 16, "bold")).pack(side=tk.LEFT)
        tk.Label(header, textvariable=self.focus_var, font=("Segoe UI", 10)).pack(side=tk.RIGHT)

        button_row = tk.Frame(self)
        button_row.pack(fill=tk.X, padx=10, pady=4)

        buttons = [
            ("Refresh Status", self.refresh_status),
            ("Run Full Live Preflight", self.run_full_live_preflight),
            ("Run Focus-Gated Session Dry Run", self.run_focus_gated_session_dry_run),
            ("Create Focus-Gated Capture Plan", self.create_focus_gated_capture_plan_clicked),
            ("Run Focus-Gated Capture Scaffold", self.run_focus_gated_capture_scaffold_clicked),
            ("Run Focus Preflight", self.run_focus_preflight),
            ("Write AI Report", self.write_report_clicked),
            ("Clean Known Junk", self.clean_junk_clicked),
            ("Commit Allowlist", self.commit_clicked),
            ("Push", self.push_clicked),
            ("Open Report", self.open_report_clicked),
        ]

        for label, command in buttons:
            tk.Button(button_row, text=label, command=command).pack(side=tk.LEFT, padx=4, pady=4)

        tk.Label(self, textvariable=self.status_var, anchor="w").pack(fill=tk.X, padx=10, pady=(2, 4))

        self.output = scrolledtext.ScrolledText(self, wrap=tk.WORD, font=("Consolas", 10))
        self.output.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))

        self.append(f"Repo root: {REPO_ROOT}\n")
        self.refresh_status()

    def append(self, text: str) -> None:
        self.output.insert(tk.END, text)
        if not text.endswith("\n"):
            self.output.insert(tk.END, "\n")
        self.output.see(tk.END)

    def set_status(self, text: str) -> None:
        self.status_var.set(text)
        self.append(text)

    def run_async(self, label: str, func: Any) -> None:
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
                self.after(0, lambda: self.status_var.set("Ready."))

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
