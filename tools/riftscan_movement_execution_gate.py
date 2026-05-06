#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-movement-execution-gate-v1.0.0
# Total character count: 000000
# Purpose: Run the final no-input gate immediately before a future bounded move_forward live test.
# Safety boundary: May run focus/live-wrapper preflight, but does not send movement/input, start capture, scan memory, validate offsets, or run /reloadui.

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-movement-execution-gate-v1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "movement-execution-gate"
REPORT = OUT_DIR / "MOVEMENT_EXECUTION_GATE_REPORT.md"
SUMMARY = OUT_DIR / "movement-execution-gate-summary.json"
LOG = OUT_DIR / "movement-execution-gate-log.jsonl"
MOVEMENT_READINESS_SUMMARY = REPO_ROOT / "handoffs" / "current" / "movement-test-readiness" / "movement-test-readiness-summary.json"
OPERATOR_GATE_SUMMARY = REPO_ROOT / "handoffs" / "current" / "operator" / "operator-current-gate-summary.json"
FOCUS_CMD = REPO_ROOT / "scripts" / "run-rift-focus-control.cmd"
FOCUS_SUMMARY = REPO_ROOT / "handoffs" / "current" / "focus-control-local" / "focus-control-summary.json"
LIVE_TEST_CMD = REPO_ROOT / "scripts" / "live-test-riftscan.cmd"


def utc_dt() -> datetime:
    return datetime.now(timezone.utc).replace(microsecond=0)


def utc() -> str:
    return utc_dt().isoformat().replace("+00:00", "Z")


def rel(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
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


def read_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
        log("json_read", path=rel(path))
        return data
    except Exception as exc:
        log("json_read_failed", path=rel(path), error=f"{type(exc).__name__}: {exc}")
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def run(name: str, args: list[str], timeout: int) -> dict[str, Any]:
    log("command_start", name=name, args=args)
    try:
        p = subprocess.run(args, cwd=REPO_ROOT, text=True, capture_output=True, timeout=timeout, check=False)
        result = {
            "name": name,
            "success": p.returncode == 0,
            "returncode": p.returncode,
            "args": args,
            "stdout": p.stdout,
            "stderr": p.stderr,
        }
        log("command_finish", name=name, returncode=p.returncode, success=p.returncode == 0)
        return result
    except subprocess.TimeoutExpired as exc:
        result = {
            "name": name,
            "success": False,
            "returncode": None,
            "args": args,
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or "",
            "error": f"timed out after {timeout} seconds",
        }
        log("command_timeout", name=name, timeout_seconds=timeout)
        return result


def tail_text(value: Any, limit: int = 4000) -> str:
    text = str(value or "")
    if len(text) <= limit:
        return text
    return text[-limit:]


def evaluate(
    *,
    movement_readiness: dict[str, Any],
    operator_gate: dict[str, Any],
    focus_summary: dict[str, Any],
    focus_command: dict[str, Any],
    wrapper_preflight: dict[str, Any],
    skip_focus_preflight: bool,
    skip_wrapper_preflight: bool,
) -> dict[str, Any]:
    blockers: list[str] = []
    warnings: list[str] = []

    if movement_readiness.get("_read_success") is False:
        blockers.append(f"Movement Test Readiness summary could not be read: {movement_readiness.get('_read_error')}.")
    if movement_readiness.get("display_status") != "PASS" or movement_readiness.get("status") != "pass":
        blockers.append("Movement Test Readiness is not PASS.")

    if operator_gate.get("_read_success") is False:
        blockers.append(f"Operator gate summary could not be read: {operator_gate.get('_read_error')}.")
    if operator_gate.get("metadata_capture_plan_gate") != "PASS":
        blockers.append("Operator metadata_capture_plan_gate is not PASS.")
    if operator_gate.get("live_collection_allowed") is not False:
        blockers.append("Operator live_collection_allowed is not false; refusing broad ambiguous live permission.")
    if operator_gate.get("old_offsets_trusted") is not False:
        blockers.append("Operator old_offsets_trusted is not false.")
    if (operator_gate.get("movement_test_readiness") or {}).get("display_status") != "PASS":
        blockers.append("Operator report does not currently reference Movement Test Readiness as PASS.")

    if not skip_focus_preflight and not focus_command.get("success"):
        blockers.append("Focus preflight command failed before movement execution gate.")
    if focus_summary.get("_read_success") is False:
        blockers.append(f"Focus summary could not be read: {focus_summary.get('_read_error')}.")
    if focus_summary.get("status") != "foreground_verified":
        blockers.append("Focus summary status is not foreground_verified.")
    selected = focus_summary.get("selected_window") if isinstance(focus_summary.get("selected_window"), dict) else {}
    if not selected:
        blockers.append("Focus summary selected_window is missing.")

    if not LIVE_TEST_CMD.exists():
        blockers.append(f"Live-test wrapper is missing: {rel(LIVE_TEST_CMD)}.")
    if not skip_wrapper_preflight and not wrapper_preflight.get("success"):
        blockers.append("live-test-riftscan preflight failed for move_forward.")
        for line in str(wrapper_preflight.get("stdout") or "").splitlines():
            stripped = line.strip()
            if stripped.startswith("- "):
                blockers.append(f"live-test-riftscan preflight issue: {stripped[2:]}")
    if skip_wrapper_preflight:
        warnings.append("Wrapper preflight was skipped; movement execution is not allowed from this gate result.")

    movement_execution_allowed = not blockers and not skip_wrapper_preflight and not skip_focus_preflight
    expires_utc = (utc_dt() + timedelta(minutes=5)).isoformat().replace("+00:00", "Z") if movement_execution_allowed else None
    return {
        "status": "pass" if movement_execution_allowed else "blocked_movement_execution_not_allowed",
        "display_status": "PASS" if movement_execution_allowed else "BLOCKED",
        "movement_execution_allowed": movement_execution_allowed,
        "expires_utc": expires_utc,
        "blockers": blockers,
        "warnings": warnings,
        "recommended_command": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100",
        "checks": {
            "movement_test_readiness": {
                "display_status": movement_readiness.get("display_status"),
                "status": movement_readiness.get("status"),
            },
            "operator_gate": {
                "metadata_capture_plan_gate": operator_gate.get("metadata_capture_plan_gate"),
                "live_collection_allowed": operator_gate.get("live_collection_allowed"),
                "old_offsets_trusted": operator_gate.get("old_offsets_trusted"),
                "movement_test_readiness": (operator_gate.get("movement_test_readiness") or {}).get("display_status"),
            },
            "focus": {
                "status": focus_summary.get("status"),
                "pid": (focus_summary.get("process") or {}).get("Id"),
                "hwnd": selected.get("hwnd") or selected.get("hwnd_hex"),
                "title": selected.get("title"),
            },
            "commands": {
                "focus_preflight": {
                    **{k: focus_command.get(k) for k in ("success", "returncode", "args", "error")},
                    "stdout_tail": tail_text(focus_command.get("stdout")),
                    "stderr_tail": tail_text(focus_command.get("stderr")),
                },
                "wrapper_preflight": {
                    **{k: wrapper_preflight.get(k) for k in ("success", "returncode", "args", "error")},
                    "stdout_tail": tail_text(wrapper_preflight.get("stdout")),
                    "stderr_tail": tail_text(wrapper_preflight.get("stderr")),
                },
            },
        },
        "safety": {
            "movement_execution_gate_only": True,
            "focus_preflight_started": not skip_focus_preflight,
            "wrapper_preflight_started": not skip_wrapper_preflight,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started_by_riftscan": False,
            "riftreader_anchor_preflight_started": not skip_wrapper_preflight,
            "offset_validation_started": False,
            "reloadui_sent": False,
        },
    }


def build_report(data: dict[str, Any]) -> str:
    blockers = data.get("blockers") or ["None"]
    warnings = data.get("warnings") or ["None"]
    blocker_text = "\n".join(f"- {blocker}" for blocker in blockers)
    warning_text = "\n".join(f"- {warning}" for warning in warnings)
    wrapper = (((data.get("checks") or {}).get("commands") or {}).get("wrapper_preflight") or {})
    return f"""# RiftScan Movement Execution Gate Report

## Result

```text
MOVEMENT EXECUTION GATE: {data["display_status"]}
status: {data["status"]}
movement_execution_allowed: {str(data["movement_execution_allowed"]).lower()}
expires_utc: {data.get("expires_utc")}
```

## Blockers

{blocker_text}

## Warnings

{warning_text}

## Recommended Exact Movement Command

Only use this command while `movement_execution_allowed=true` and before `expires_utc`:

```powershell
{data["recommended_command"]}
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
{wrapper.get("stdout_tail") or ""}
```

stderr:

```text
{wrapper.get("stderr_tail") or ""}
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


def run_self_test() -> tuple[bool, dict[str, Any]]:
    movement = {"display_status": "PASS", "status": "pass"}
    operator = {"metadata_capture_plan_gate": "PASS", "live_collection_allowed": False, "old_offsets_trusted": False, "movement_test_readiness": {"display_status": "PASS"}}
    focus = {"status": "foreground_verified", "process": {"Id": 1}, "selected_window": {"hwnd": 2, "title": "RIFT"}}
    ok_cmd = {"success": True, "returncode": 0, "args": []}
    cases = [
        ("pass", "pass", movement, operator, focus, ok_cmd, ok_cmd, False, False, None),
        ("blocked readiness", "blocked_movement_execution_not_allowed", {"display_status": "BLOCKED", "status": "blocked"}, operator, focus, ok_cmd, ok_cmd, False, False, "Movement Test Readiness"),
        ("blocked focus", "blocked_movement_execution_not_allowed", movement, operator, {"status": "not_foreground", "selected_window": {}}, ok_cmd, ok_cmd, False, False, "foreground_verified"),
        ("blocked wrapper", "blocked_movement_execution_not_allowed", movement, operator, focus, ok_cmd, {"success": False, "returncode": 2}, False, False, "preflight failed"),
        ("blocked skipped wrapper", "blocked_movement_execution_not_allowed", movement, operator, focus, ok_cmd, ok_cmd, False, True, None),
    ]
    tests: list[dict[str, Any]] = []
    for name, expected, movement_doc, operator_doc, focus_doc, focus_cmd, wrapper_cmd, skip_focus, skip_wrapper, blocker_part in cases:
        result = evaluate(
            movement_readiness=movement_doc,
            operator_gate=operator_doc,
            focus_summary=focus_doc,
            focus_command=focus_cmd,
            wrapper_preflight=wrapper_cmd,
            skip_focus_preflight=skip_focus,
            skip_wrapper_preflight=skip_wrapper,
        )
        blockers = result["blockers"]
        blocker_ok = blocker_part is None or any(blocker_part in blocker for blocker in blockers)
        tests.append({"name": name, "expected": expected, "actual": result["status"], "blockers": blockers, "pass": result["status"] == expected and blocker_ok})
    passed = all(test["pass"] for test in tests)
    return passed, {
        "schema_version": "riftscan.movement_execution_gate_self_test.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if passed else "FAIL",
        "case_count": len(tests),
        "tests": tests,
        "safety": {
            "writes_current_artifacts": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started_by_riftscan": False,
            "reloadui_sent": False,
        },
    }


def parse(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Run final no-input gate before a guarded move_forward live test.")
    p.add_argument("--skip-focus-preflight", action="store_true")
    p.add_argument("--skip-wrapper-preflight", action="store_true")
    p.add_argument("--timeout-seconds", type=int, default=120)
    p.add_argument("--strict-exit-code", action="store_true")
    p.add_argument("--self-test", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse(argv)
    if args.self_test:
        passed, summary = run_self_test()
        print(json.dumps(summary, indent=2, sort_keys=True))
        return 0 if passed else 1

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    log("movement_execution_gate_start", version=APP_VERSION)
    focus_command = {"success": True, "skipped": True, "args": [rel(FOCUS_CMD)]}
    if not args.skip_focus_preflight:
        focus_command = run("focus_preflight", ["cmd", "/c", str(FOCUS_CMD)], args.timeout_seconds)
    wrapper_preflight = {"success": True, "skipped": True, "args": [rel(LIVE_TEST_CMD), "-Stimulus", "move_forward", "-PreflightOnly"]}
    if not args.skip_wrapper_preflight:
        wrapper_preflight = run(
            "live_test_wrapper_preflight",
            ["cmd", "/c", str(LIVE_TEST_CMD), "-Stimulus", "move_forward", "-PreflightOnly"],
            max(args.timeout_seconds, 180),
        )
    data = {
        "schema_version": "riftscan.movement_execution_gate.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        **evaluate(
            movement_readiness=read_json(MOVEMENT_READINESS_SUMMARY),
            operator_gate=read_json(OPERATOR_GATE_SUMMARY),
            focus_summary=read_json(FOCUS_SUMMARY),
            focus_command=focus_command,
            wrapper_preflight=wrapper_preflight,
            skip_focus_preflight=args.skip_focus_preflight,
            skip_wrapper_preflight=args.skip_wrapper_preflight,
        ),
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
    }
    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("movement_execution_gate_finish", status=data["status"], blocker_count=len(data["blockers"]))

    print(f"MOVEMENT EXECUTION GATE: {data['display_status']}")
    print(f"movement_execution_allowed: {str(data['movement_execution_allowed']).lower()}")
    print(f"Report: {rel(REPORT)}")
    print(f"Summary: {rel(SUMMARY)}")
    print(f"Log: {rel(LOG)}")
    for blocker in data["blockers"]:
        print(f"- {blocker}")

    if args.strict_exit_code and data["status"] != "pass":
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# End of script: riftscan_movement_execution_gate.py
