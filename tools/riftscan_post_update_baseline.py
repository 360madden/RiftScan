#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-post-update-baseline-v1.0.0
# Total character count: 12072
# Purpose: Write a conservative post-update baseline report after a RIFT client update/maintenance window.
# Safety boundary: Records status only. No memory capture, input, movement, scanning, coordinate recovery, or /reloadui.

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-post-update-baseline-v1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
FOCUS_CMD = REPO_ROOT / "scripts" / "run-rift-focus-control.cmd"
FOCUS_DIR = REPO_ROOT / "handoffs" / "current" / "focus-control-local"
FOCUS_SUMMARY = FOCUS_DIR / "focus-control-summary.json"
WINDOWS_JSON = FOCUS_DIR / "windows.json"
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "post-update-baseline"
REPORT = OUT_DIR / "POST_UPDATE_BASELINE_REPORT.md"
SUMMARY = OUT_DIR / "post-update-baseline-summary.json"
LOG = OUT_DIR / "post-update-baseline-log.jsonl"


def utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT))
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
    except Exception as exc:
        result = {
            "success": False,
            "returncode": None,
            "args": args,
            "stdout": "",
            "stderr": "",
            "error": str(exc),
            "exception_type": type(exc).__name__,
        }
        log("command_exception", args=args, exception_type=type(exc).__name__)
        return result


def read_json(path: Path) -> Any:
    if not path.exists():
        log("json_missing", path=rel(path))
        return {"_read_success": False, "_path": rel(path), "_read_error": "missing"}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        log("json_read", path=rel(path))
        return data
    except Exception as exc:
        log("json_read_failed", path=rel(path), exception_type=type(exc).__name__)
        return {"_read_success": False, "_path": rel(path), "_read_error": str(exc)}


def first(*values: Any) -> Any:
    for value in values:
        if value not in (None, "", [], {}):
            return value
    return None


def dig(data: Any, *keys: str) -> Any:
    cur = data
    for key in keys:
        if not isinstance(cur, dict):
            return None
        cur = cur.get(key)
    return cur


def selected_window(summary: Any) -> Any:
    if not isinstance(summary, dict):
        return None
    return first(summary.get("selected_window"), dig(summary, "focus_result", "selected_window"), dig(summary, "result", "selected_window"))


def focus_status(summary: Any) -> Any:
    if not isinstance(summary, dict):
        return None
    return first(summary.get("status"), dig(summary, "focus_result", "status"), dig(summary, "result", "status"))


def windows_list(windows: Any) -> list[Any]:
    if isinstance(windows, list):
        return windows
    if isinstance(windows, dict):
        value = first(windows.get("windows"), windows.get("entries"), dig(windows, "result", "windows"))
        if isinstance(value, list):
            return value
    return []


def runtime_field(summary: Any, window: Any, key: str) -> Any:
    if not isinstance(summary, dict):
        summary = {}
    if not isinstance(window, dict):
        window = {}
    return first(summary.get(key), dig(summary, "focus_result", key), dig(summary, "selected_window", key), window.get(key))


def git_snapshot(timeout: int) -> dict[str, Any]:
    branch = run(["git", "rev-parse", "--abbrev-ref", "HEAD"], timeout)
    head = run(["git", "rev-parse", "HEAD"], timeout)
    status = run(["git", "status", "--short"], timeout)
    recent = run(["git", "log", "--oneline", "-5"], timeout)
    return {
        "branch": branch.get("stdout", "").strip(),
        "head": head.get("stdout", "").strip(),
        "status_short": status.get("stdout", "").rstrip(),
        "log_oneline_5": recent.get("stdout", "").rstrip(),
    }


def build_report(data: dict[str, Any]) -> str:
    blockers = data["blockers"] or ["None"]
    blocker_text = "\n".join(f"- {b}" for b in blockers)
    return f"""# RiftScan Post-Update Baseline Report

## Result

```text
POST-UPDATE BASELINE: {data["display_status"]}
status: {data["status"]}
```

## Blockers

{blocker_text}

## Runtime

```text
focus_status: {data["runtime"]["focus_status"]}
selected_window_present: {data["runtime"]["selected_window_present"]}
windows_entry_count: {data["runtime"]["windows_entry_count"]}
pid: {data["runtime"]["pid"]}
hwnd: {data["runtime"]["hwnd"]}
title: {data["runtime"]["title"]}
character_name: {data["runtime"]["character_name"]}
shard: {data["runtime"]["shard"]}
zone_or_location: {data["runtime"]["zone_or_location"]}
```

## Manual State

```text
maintenance_over: {data["manual_state"]["maintenance_over"]}
login_successful: {data["manual_state"]["login_successful"]}
world_loaded: {data["manual_state"]["world_loaded"]}
```

## Safety Boundary

```text
old_offsets_trusted: false
live_capture_allowed: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
reloadui_sent: false
```

## Git Snapshot

```text
branch: {data["git"]["branch"]}
head: {data["git"]["head"]}
```

Git status:

```text
{data["git"]["status_short"]}
```

Recent commits:

```text
{data["git"]["log_oneline_5"]}
```

## Output Paths

```text
report: {rel(REPORT)}
summary: {rel(SUMMARY)}
log: {rel(LOG)}
```

## Machine-Readable Summary

```json
{json.dumps(data, indent=2, sort_keys=True)}
```
"""


def parse(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Write a conservative RiftScan post-update baseline report.")
    p.add_argument("--timeout-seconds", type=int, default=45)
    p.add_argument("--git-timeout-seconds", type=int, default=15)
    p.add_argument("--skip-focus-preflight", action="store_true")
    p.add_argument("--maintenance-over", action="store_true")
    p.add_argument("--login-successful", action="store_true")
    p.add_argument("--world-loaded", action="store_true")
    p.add_argument("--assume-in-world", action="store_true")
    p.add_argument("--character-name", default="")
    p.add_argument("--shard", default="")
    p.add_argument("--zone-or-location", default="")
    p.add_argument("--strict-exit-code", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse(argv)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    log("baseline_start", version=APP_VERSION)

    if args.skip_focus_preflight:
        focus_cmd = {"success": True, "skipped": True, "args": [rel(FOCUS_CMD)]}
    elif FOCUS_CMD.exists():
        focus_cmd = run([str(FOCUS_CMD)], args.timeout_seconds)
    else:
        focus_cmd = {"success": False, "error": "missing focus launcher", "args": [rel(FOCUS_CMD)]}

    focus = read_json(FOCUS_SUMMARY)
    windows = read_json(WINDOWS_JSON)
    win = selected_window(focus)
    wins = windows_list(windows)
    fstatus = focus_status(focus)

    manual = {
        "maintenance_over": bool(args.maintenance_over or args.assume_in_world),
        "login_successful": bool(args.login_successful or args.assume_in_world),
        "world_loaded": bool(args.world_loaded or args.assume_in_world),
    }

    blockers: list[str] = []
    if not manual["maintenance_over"]:
        blockers.append("Maintenance is not confirmed over.")
    if not manual["login_successful"]:
        blockers.append("Login is not confirmed successful.")
    if not manual["world_loaded"]:
        blockers.append("Stable in-world state is not confirmed.")
    if not args.skip_focus_preflight and not focus_cmd.get("success"):
        blockers.append("Focus preflight command did not complete successfully.")
    if isinstance(focus, dict) and focus.get("_read_success") is False:
        blockers.append(f"Focus summary could not be read: {focus.get('_read_error')}.")
    if isinstance(windows, dict) and windows.get("_read_success") is False:
        blockers.append(f"Windows JSON could not be read: {windows.get('_read_error')}.")
    if fstatus != "foreground_verified":
        blockers.append("Focus status is not foreground_verified.")
    if not win:
        blockers.append("selected_window is missing or null.")
    if not wins:
        blockers.append("windows.json has no window entries.")

    status = "pass" if not blockers else "blocked_waiting_for_game_or_focus"
    data = {
        "schema_version": "riftscan.post_update_baseline.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": status,
        "display_status": "PASS" if status == "pass" else "BLOCKED",
        "blockers": blockers,
        "manual_state": manual,
        "safety": {
            "old_offsets_trusted": False,
            "live_capture_allowed": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "reloadui_sent": False,
        },
        "runtime": {
            "focus_status": fstatus,
            "selected_window_present": bool(win),
            "windows_entry_count": len(wins),
            "pid": runtime_field(focus, win, "pid"),
            "hwnd": runtime_field(focus, win, "hwnd"),
            "title": runtime_field(focus, win, "title"),
            "character_name": args.character_name or None,
            "shard": args.shard or None,
            "zone_or_location": args.zone_or_location or None,
        },
        "git": git_snapshot(args.git_timeout_seconds),
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
        "focus_command_result": focus_cmd,
        "source_artifacts": {"focus_summary": focus, "windows": windows},
    }

    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("baseline_finish", status=status, blocker_count=len(blockers))

    print(f"POST-UPDATE BASELINE: {data['display_status']}")
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

# End of script: riftscan_post_update_baseline.py
