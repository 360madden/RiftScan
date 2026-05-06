#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-capture-readiness-v1.0.0
# Total character count: 13293
# Purpose: Write a conservative metadata-only capture-readiness report after a current-client post-update baseline.
# Safety boundary: Records readiness only. No memory capture, input, movement, scanning, coordinate recovery, offset validation, or /reloadui.

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-capture-readiness-v1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
FOCUS_CMD = REPO_ROOT / "scripts" / "run-rift-focus-control.cmd"
FOCUS_DIR = REPO_ROOT / "handoffs" / "current" / "focus-control-local"
FOCUS_SUMMARY = FOCUS_DIR / "focus-control-summary.json"
WINDOWS_JSON = FOCUS_DIR / "windows.json"
BASELINE_DIR = REPO_ROOT / "handoffs" / "current" / "post-update-baseline"
BASELINE_SUMMARY = BASELINE_DIR / "post-update-baseline-summary.json"
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "capture-readiness"
REPORT = OUT_DIR / "CAPTURE_READINESS_REPORT.md"
SUMMARY = OUT_DIR / "capture-readiness-summary.json"
LOG = OUT_DIR / "capture-readiness-log.jsonl"


def utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path)


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, data: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(data, encoding="utf-8", newline="\n")


def log(event: str, **fields: Any) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with LOG.open("a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps({"created_utc": utc(), "event": event, **fields}, sort_keys=True) + "\n")


def run(args: list[str], timeout: int) -> dict[str, Any]:
    log("command_start", args=args)
    try:
        p = subprocess.run(args, cwd=REPO_ROOT, text=True, capture_output=True, timeout=timeout, check=False)
        result = {
            "success": p.returncode == 0,
            "returncode": p.returncode,
            "args": args,
            "stdout": p.stdout,
            "stderr": p.stderr,
        }
        log("command_finish", args=args, returncode=p.returncode, success=p.returncode == 0)
        return result
    except subprocess.TimeoutExpired as exc:
        result = {
            "success": False,
            "returncode": None,
            "args": args,
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or "",
            "error": f"timed out after {timeout} seconds",
        }
        log("command_timeout", args=args)
        return result


def read_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
        log("json_read", path=rel(path))
        return data
    except Exception as exc:
        log("json_read_failed", path=rel(path), error=f"{type(exc).__name__}: {exc}")
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def dig(data: Any, *keys: str) -> Any:
    current = data
    for key in keys:
        if not isinstance(current, dict):
            return None
        current = current.get(key)
    return current


def selected_window(focus: dict[str, Any]) -> dict[str, Any]:
    selected = focus.get("selected_window")
    return selected if isinstance(selected, dict) else {}


def windows_list(windows: dict[str, Any]) -> list[Any]:
    value = windows.get("windows")
    return value if isinstance(value, list) else []


def current_pid(focus: dict[str, Any], selected: dict[str, Any]) -> Any:
    return dig(focus, "process", "Id") or selected.get("pid")


def current_hwnd(selected: dict[str, Any]) -> Any:
    return selected.get("hwnd") or selected.get("hwnd_hex")


def baseline_runtime_field(baseline: dict[str, Any], key: str) -> Any:
    return dig(baseline, "runtime", key)


def git_snapshot(timeout: int) -> dict[str, Any]:
    branch = run(["git", "rev-parse", "--abbrev-ref", "HEAD"], timeout)
    head = run(["git", "rev-parse", "HEAD"], timeout)
    status = run(["git", "status", "--short"], timeout)
    log5 = run(["git", "log", "--oneline", "-5"], timeout)
    return {
        "branch": branch.get("stdout", "").strip() if branch.get("success") else None,
        "head": head.get("stdout", "").strip() if head.get("success") else None,
        "status_short": status.get("stdout", "") if status.get("success") else status.get("stderr", ""),
        "log_oneline_5": log5.get("stdout", "").strip() if log5.get("success") else log5.get("stderr", ""),
    }


def false_or_block(value: Any, field: str, blockers: list[str]) -> None:
    if value is not False:
        blockers.append(f"Safety field {field} is not false.")


def build_report(data: dict[str, Any]) -> str:
    blockers = data["blockers"] or ["None"]
    blocker_text = "\n".join(f"- {b}" for b in blockers)
    return f"""# RiftScan Capture Readiness Report

## Result

```text
CAPTURE READINESS: {data["display_status"]}
status: {data["status"]}
```

## Blockers

{blocker_text}

## Gate Summary

```text
post_update_baseline_status: {data["baseline"].get("status")}
post_update_baseline_display_status: {data["baseline"].get("display_status")}
current_focus_status: {data["runtime"].get("focus_status")}
selected_window_present: {data["runtime"].get("selected_window_present")}
windows_entry_count: {data["runtime"].get("windows_entry_count")}
pid: {data["runtime"].get("pid")}
hwnd: {data["runtime"].get("hwnd")}
title: {data["runtime"].get("title")}
```

## Safety Boundary

```text
old_offsets_trusted: false
capture_started: false
live_collection_allowed: false
capture_planning_allowed: {str(data["safety"].get("capture_planning_allowed")).lower()}
movement_or_input_sent: false
memory_scan_or_read_started: false
reloadui_sent: false
```

## Output Paths

```text
report: {rel(REPORT)}
summary: {rel(SUMMARY)}
log: {rel(LOG)}
```

## Next Step

{data["next_step"]}

## Git Snapshot

```text
branch: {data["git"].get("branch")}
head: {data["git"].get("head")}
```

Git status:

```text
{data["git"].get("status_short")}
```

Recent commits:

```text
{data["git"].get("log_oneline_5")}
```

## Machine-Readable Summary

```json
{json.dumps(data, indent=2, sort_keys=True)}
```
"""


def parse(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Write a conservative RiftScan capture-readiness report.")
    p.add_argument("--timeout-seconds", type=int, default=45)
    p.add_argument("--git-timeout-seconds", type=int, default=15)
    p.add_argument("--skip-focus-preflight", action="store_true")
    p.add_argument("--strict-exit-code", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse(argv)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    log("readiness_start", version=APP_VERSION)

    if args.skip_focus_preflight:
        focus_cmd = {"success": True, "skipped": True, "args": [rel(FOCUS_CMD)]}
    elif FOCUS_CMD.exists():
        focus_cmd = run(["cmd", "/c", str(FOCUS_CMD)], args.timeout_seconds)
    else:
        focus_cmd = {"success": False, "error": "missing focus launcher", "args": [rel(FOCUS_CMD)]}

    baseline = read_json(BASELINE_SUMMARY)
    focus = read_json(FOCUS_SUMMARY)
    windows = read_json(WINDOWS_JSON)
    win = selected_window(focus)
    wins = windows_list(windows)
    baseline_safety = baseline.get("safety") if isinstance(baseline.get("safety"), dict) else {}
    baseline_runtime = baseline.get("runtime") if isinstance(baseline.get("runtime"), dict) else {}

    blockers: list[str] = []
    if baseline.get("_read_success") is False:
        blockers.append(f"Post-update baseline summary could not be read: {baseline.get('_read_error')}.")
    if baseline.get("status") != "pass":
        blockers.append("Post-update baseline is not PASS for the current client.")
    if baseline.get("display_status") != "PASS":
        blockers.append("Post-update baseline display_status is not PASS.")
    false_or_block(baseline_safety.get("old_offsets_trusted"), "baseline.safety.old_offsets_trusted", blockers)
    false_or_block(baseline_safety.get("capture_started"), "baseline.safety.capture_started", blockers)
    false_or_block(baseline_safety.get("movement_or_input_sent"), "baseline.safety.movement_or_input_sent", blockers)
    false_or_block(baseline_safety.get("memory_scan_or_read_started"), "baseline.safety.memory_scan_or_read_started", blockers)
    false_or_block(baseline_safety.get("reloadui_sent"), "baseline.safety.reloadui_sent", blockers)
    if baseline_safety.get("live_capture_allowed") is not False:
        blockers.append("Baseline live_capture_allowed is not false; readiness gate must not inherit broad live permission.")
    if baseline_runtime.get("focus_status") != "foreground_verified":
        blockers.append("Baseline focus_status is not foreground_verified.")
    if baseline_runtime.get("selected_window_present") is not True:
        blockers.append("Baseline selected_window_present is not true.")
    if int(baseline_runtime.get("windows_entry_count") or 0) < 1:
        blockers.append("Baseline windows_entry_count is less than 1.")

    if not args.skip_focus_preflight and not focus_cmd.get("success"):
        blockers.append("Current focus preflight command did not complete successfully.")
    if focus.get("_read_success") is False:
        blockers.append(f"Current focus summary could not be read: {focus.get('_read_error')}.")
    if windows.get("_read_success") is False:
        blockers.append(f"Current windows JSON could not be read: {windows.get('_read_error')}.")
    if focus.get("status") != "foreground_verified":
        blockers.append("Current focus status is not foreground_verified.")
    if not win:
        blockers.append("Current selected_window is missing or null.")
    if not wins:
        blockers.append("Current windows.json has no window entries.")

    b_pid = baseline_runtime.get("pid")
    c_pid = current_pid(focus, win)
    if b_pid and c_pid and str(b_pid) != str(c_pid):
        blockers.append("Current RIFT PID differs from the post-update baseline; rerun Post-Update Baseline.")
    b_hwnd = baseline_runtime.get("hwnd")
    c_hwnd = current_hwnd(win)
    if b_hwnd and c_hwnd and str(b_hwnd).lower() != str(c_hwnd).lower():
        blockers.append("Current RIFT HWND differs from the post-update baseline; rerun Post-Update Baseline.")

    status = "pass" if not blockers else "blocked_waiting_for_current_baseline"
    data = {
        "schema_version": "riftscan.capture_readiness.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": status,
        "display_status": "PASS" if status == "pass" else "BLOCKED",
        "blockers": blockers,
        "baseline": {
            "summary_path": rel(BASELINE_SUMMARY),
            "status": baseline.get("status"),
            "display_status": baseline.get("display_status"),
            "created_utc": baseline.get("created_utc"),
            "runtime": baseline_runtime,
            "safety": baseline_safety,
        },
        "runtime": {
            "focus_status": focus.get("status"),
            "selected_window_present": bool(win),
            "windows_entry_count": len(wins),
            "pid": c_pid,
            "hwnd": c_hwnd,
            "title": win.get("title"),
        },
        "safety": {
            "old_offsets_trusted": False,
            "capture_started": False,
            "live_collection_allowed": False,
            "capture_planning_allowed": status == "pass",
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "reloadui_sent": False,
        },
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
        "focus_command_result": focus_cmd,
        "source_artifacts": {
            "post_update_baseline_summary": baseline,
            "focus_summary": focus,
            "windows": windows,
        },
        "git": git_snapshot(args.git_timeout_seconds),
        "next_step": (
            "Create or refresh a metadata-only focus-gated capture plan."
            if status == "pass"
            else "Run a fresh Post-Update Baseline after the current updated client is confirmed stable in-world."
        ),
    }

    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("readiness_finish", status=status, blocker_count=len(blockers))

    print(f"CAPTURE READINESS: {data['display_status']}")
    print(f"Report: {rel(REPORT)}")
    print(f"Summary: {rel(SUMMARY)}")
    print(f"Log: {rel(LOG)}")
    for blocker in blockers:
        print(f"- {blocker}")

    if args.strict_exit_code and status != "pass":
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
