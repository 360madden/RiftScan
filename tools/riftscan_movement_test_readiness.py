#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-movement-test-readiness-v1.0.0
# Total character count: 24326
# Purpose: Validate that RiftScan is ready to begin a future guarded live game-world movement test.
# Safety boundary: Readiness validation only. No focus preflight, live capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-movement-test-readiness-v1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "movement-test-readiness"
REPORT = OUT_DIR / "MOVEMENT_TEST_READINESS_REPORT.md"
SUMMARY = OUT_DIR / "movement-test-readiness-summary.json"
LOG = OUT_DIR / "movement-test-readiness-log.jsonl"
OPERATOR_GATE_SUMMARY = REPO_ROOT / "handoffs" / "current" / "operator" / "operator-current-gate-summary.json"
CAPTURE_PLAN_CHECK_SUMMARY = REPO_ROOT / "handoffs" / "current" / "capture-plan-check" / "capture-plan-check-summary.json"
LIVE_COLLECTION_GATE_CHECKLIST = REPO_ROOT / "handoffs" / "current" / "live-collection-gate" / "LIVE_COLLECTION_GATE_CHECKLIST.md"
LIVE_COLLECTION_GATE_SUMMARY = REPO_ROOT / "handoffs" / "current" / "live-collection-gate" / "live-collection-gate-summary.json"
LIVE_TEST_CMD = REPO_ROOT / "scripts" / "live-test-riftscan.cmd"
LIVE_TEST_PS1 = REPO_ROOT / "scripts" / "live-test-riftscan.ps1"
FOCUS_CONTROL = REPO_ROOT / "tools" / "rift_focus_control.py"
DEFAULT_RIFTREADER_REPO = Path(r"C:\RIFT MODDING\RiftReader")

REQUIRED_WRAPPER_TOKENS = {
    "move_forward stimulus": "move_forward",
    "preflight only switch": "PreflightOnly",
    "pre-capture wait": "PreCaptureWaitMilliseconds",
    "ReaderBridge freshness": "ReaderBridgeExport.lua",
    "RiftReader anchor read": "--read-player-coord-anchor",
    "RiftScan passive capture": "capture\", \"passive",
    "delta summary": "New-DeltaSummary",
    "movement-proof interpretation": "stimulus_observed_primary_triplet_changed",
}
REQUIRED_AUTH_TOKENS = {
    "docs/agent-execution-workflow.md": ["autonomous Codex control", "bounded stimulus input"],
    "docs/live-rift-control.md": ["operator explicitly authorized", "Verify the target process"],
}


def utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


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


def read_text(path: Path) -> str:
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
        log("text_read", path=rel(path))
        return text
    except Exception as exc:
        log("text_read_failed", path=rel(path), error=f"{type(exc).__name__}: {exc}")
        return ""


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
        log("command_timeout", args=args, timeout_seconds=timeout)
        return result


def git_snapshot(timeout: int) -> dict[str, Any]:
    head = run(["git", "rev-parse", "HEAD"], timeout)
    status = run(["git", "status", "--short"], timeout)
    recent = run(["git", "log", "--oneline", "-5"], timeout)
    return {
        "head": head.get("stdout", "").strip() if head.get("success") else None,
        "status_short": status.get("stdout", "") if status.get("success") else status.get("stderr", ""),
        "log_oneline_5": recent.get("stdout", "").strip() if recent.get("success") else recent.get("stderr", ""),
    }


def display_status(summary: dict[str, Any], *keys: str) -> Any:
    current: Any = summary
    for key in keys:
        if not isinstance(current, dict):
            return None
        current = current.get(key)
    return current


def evaluate_readiness(
    *,
    operator_gate: dict[str, Any],
    capture_plan_check: dict[str, Any],
    live_gate: dict[str, Any],
    live_gate_checklist_text: str,
    live_test_cmd_exists: bool,
    live_test_ps1_text: str,
    focus_control_exists: bool,
    riftreader_run_cmd_exists: bool,
    auth_docs: dict[str, str],
) -> dict[str, Any]:
    blockers: list[str] = []
    warnings: list[str] = []
    checks: dict[str, Any] = {}

    if operator_gate.get("_read_success") is False:
        blockers.append(f"Operator gate summary could not be read: {operator_gate.get('_read_error')}.")
    if capture_plan_check.get("_read_success") is False:
        blockers.append(f"Capture Plan Check summary could not be read: {capture_plan_check.get('_read_error')}.")
    if live_gate.get("_read_success") is False:
        blockers.append(f"Live-collection gate summary could not be read: {live_gate.get('_read_error')}.")

    gate_baseline = operator_gate.get("post_update_baseline") if isinstance(operator_gate.get("post_update_baseline"), dict) else {}
    gate_readiness = operator_gate.get("capture_readiness") if isinstance(operator_gate.get("capture_readiness"), dict) else {}
    gate_link = operator_gate.get("capture_readiness_baseline_link") if isinstance(operator_gate.get("capture_readiness_baseline_link"), dict) else {}
    latest_plan = operator_gate.get("latest_capture_plan") if isinstance(operator_gate.get("latest_capture_plan"), dict) else {}
    checks["operator_gate"] = {
        "metadata_capture_plan_gate": operator_gate.get("metadata_capture_plan_gate"),
        "post_update_baseline": gate_baseline.get("display_status"),
        "capture_readiness": gate_readiness.get("display_status"),
        "capture_readiness_baseline_link": gate_link.get("status"),
        "latest_capture_plan_status": latest_plan.get("status"),
        "live_collection_allowed": operator_gate.get("live_collection_allowed"),
        "old_offsets_trusted": operator_gate.get("old_offsets_trusted"),
    }
    if operator_gate.get("metadata_capture_plan_gate") != "PASS":
        blockers.append("Operator metadata_capture_plan_gate is not PASS.")
    if gate_baseline.get("display_status") != "PASS":
        blockers.append("Post-Update Baseline is not PASS in the Operator gate.")
    if gate_readiness.get("display_status") != "PASS":
        blockers.append("Capture Readiness is not PASS in the Operator gate.")
    if gate_link.get("status") != "match":
        blockers.append("Capture Readiness baseline link is not match.")
    if latest_plan.get("status") != "valid_metadata_only":
        blockers.append("Latest capture plan is not valid_metadata_only in the Operator gate.")
    if operator_gate.get("live_collection_allowed") is not False:
        blockers.append("Operator live_collection_allowed is not false; readiness gate refuses ambiguous broad live permission.")
    if operator_gate.get("old_offsets_trusted") is not False:
        blockers.append("Operator old_offsets_trusted is not false.")

    cpc_safety = capture_plan_check.get("safety") if isinstance(capture_plan_check.get("safety"), dict) else {}
    checks["capture_plan_check"] = {
        "display_status": capture_plan_check.get("display_status"),
        "status": capture_plan_check.get("status"),
        "capture_plan_review_allowed": cpc_safety.get("capture_plan_review_allowed"),
        "live_collection_allowed": cpc_safety.get("live_collection_allowed"),
        "movement_or_input_sent": cpc_safety.get("movement_or_input_sent"),
        "memory_scan_or_read_started": cpc_safety.get("memory_scan_or_read_started"),
    }
    if capture_plan_check.get("display_status") != "PASS" or capture_plan_check.get("status") != "pass":
        blockers.append("Capture Plan Check is not PASS.")
    if cpc_safety.get("live_collection_allowed") is not False:
        blockers.append("Capture Plan Check live_collection_allowed is not false.")
    if cpc_safety.get("movement_or_input_sent") is not False:
        blockers.append("Capture Plan Check movement_or_input_sent is not false.")
    if cpc_safety.get("memory_scan_or_read_started") is not False:
        blockers.append("Capture Plan Check memory_scan_or_read_started is not false.")

    checks["live_collection_gate"] = {
        "summary_exists": live_gate.get("_read_success") is not False,
        "display_status": live_gate.get("display_status"),
        "status": live_gate.get("status"),
        "live_collection_allowed": live_gate.get("live_collection_allowed"),
        "checklist_mentions_movement": "movement" in live_gate_checklist_text.lower(),
        "checklist_mentions_abort": "abort" in live_gate_checklist_text.lower(),
    }
    if not live_gate_checklist_text:
        blockers.append("Live-collection gate checklist is missing or unreadable.")
    if live_gate.get("live_collection_allowed") is not False:
        blockers.append("Live-collection gate summary must still have live_collection_allowed=false before movement execution.")
    if "movement" not in live_gate_checklist_text.lower() or "abort" not in live_gate_checklist_text.lower():
        blockers.append("Live-collection gate checklist does not explicitly cover movement and abort conditions.")

    missing_wrapper_tokens = [label for label, token in REQUIRED_WRAPPER_TOKENS.items() if token not in live_test_ps1_text]
    checks["movement_wrapper"] = {
        "cmd_exists": live_test_cmd_exists,
        "ps1_readable": bool(live_test_ps1_text),
        "missing_tokens": missing_wrapper_tokens,
        "focus_control_exists": focus_control_exists,
        "riftreader_run_cmd_exists": riftreader_run_cmd_exists,
    }
    if not live_test_cmd_exists:
        blockers.append(f"Movement live-test CMD wrapper is missing: {rel(LIVE_TEST_CMD)}.")
    if not live_test_ps1_text:
        blockers.append(f"Movement live-test PowerShell wrapper is missing or unreadable: {rel(LIVE_TEST_PS1)}.")
    if missing_wrapper_tokens:
        blockers.append("Movement live-test wrapper is missing required guard features: " + ", ".join(missing_wrapper_tokens) + ".")
    if not focus_control_exists:
        blockers.append(f"Focus-control helper is missing: {rel(FOCUS_CONTROL)}.")
    if not riftreader_run_cmd_exists:
        blockers.append("RiftReader run-reader.cmd is missing; movement wrapper cannot refresh proof-grade coordinate anchors.")

    missing_auth_tokens: dict[str, list[str]] = {}
    for rel_path, tokens in REQUIRED_AUTH_TOKENS.items():
        text = auth_docs.get(rel_path, "")
        missing = [token for token in tokens if token.lower() not in text.lower()]
        if missing:
            missing_auth_tokens[rel_path] = missing
    checks["authorization_docs"] = {"missing_tokens": missing_auth_tokens}
    if missing_auth_tokens:
        blockers.append("Movement/control authorization docs are missing required current authorization language.")

    status = "pass" if not blockers else "blocked_movement_test_not_ready"
    return {
        "status": status,
        "display_status": "PASS" if status == "pass" else "BLOCKED",
        "blockers": blockers,
        "warnings": warnings,
        "checks": checks,
        "readiness": {
            "ready_for_live_game_world_movement_testing": status == "pass",
            "ready_for_movement_execution_now": False,
            "requires_final_live_rerun_before_execution": True,
            "recommended_stimulus": "move_forward",
            "recommended_pre_capture_wait_ms": 3000,
            "recommended_live_wrapper_preflight": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreflightOnly",
            "recommended_live_wrapper_capture": ".\\scripts\\live-test-riftscan.cmd -Stimulus move_forward -PreCaptureWaitMilliseconds 3000 -Samples 40 -IntervalMilliseconds 100",
            "movement_execution_note": "This readiness check does not send input. A future live run must verify the exact RIFT window immediately before any bounded movement stimulus.",
        },
        "safety": {
            "readiness_check_only": True,
            "focus_preflight_started": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "offset_validation_started": False,
            "riftreader_validation_started": False,
            "reloadui_sent": False,
        },
    }


def build_report(data: dict[str, Any]) -> str:
    blockers = data.get("blockers") or ["None"]
    warnings = data.get("warnings") or ["None"]
    blocker_text = "\n".join(f"- {blocker}" for blocker in blockers)
    warning_text = "\n".join(f"- {warning}" for warning in warnings)
    readiness = data.get("readiness") if isinstance(data.get("readiness"), dict) else {}
    return f"""# RiftScan Movement Test Readiness Report

## Result

```text
MOVEMENT TEST READINESS: {data["display_status"]}
status: {data["status"]}
ready_for_live_game_world_movement_testing: {str(readiness.get("ready_for_live_game_world_movement_testing")).lower()}
ready_for_movement_execution_now: false
```

## Blockers

{blocker_text}

## Warnings

{warning_text}

## Meaning

`PASS` means the repository control-plane is ready to proceed to a separately gated live game-world movement test plan.
It does **not** mean this check sent movement/input or started capture.

## Recommended Future Live Movement Shape

```text
preflight: {readiness.get("recommended_live_wrapper_preflight")}
capture: {readiness.get("recommended_live_wrapper_capture")}
stimulus: {readiness.get("recommended_stimulus")}
pre_capture_wait_ms: {readiness.get("recommended_pre_capture_wait_ms")}
```

## Safety Boundary

```text
readiness_check_only: true
focus_preflight_started: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
reloadui_sent: false
requires_final_live_rerun_before_execution: true
```

## Output Paths

```text
report: {rel(REPORT)}
summary: {rel(SUMMARY)}
log: {rel(LOG)}
```

## Git Snapshot

```text
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


def clone_json(data: dict[str, Any]) -> dict[str, Any]:
    return json.loads(json.dumps(data))


def self_test_operator_gate() -> dict[str, Any]:
    return {
        "metadata_capture_plan_gate": "PASS",
        "live_collection_allowed": False,
        "old_offsets_trusted": False,
        "post_update_baseline": {"display_status": "PASS"},
        "capture_readiness": {"display_status": "PASS"},
        "capture_readiness_baseline_link": {"status": "match"},
        "latest_capture_plan": {"status": "valid_metadata_only"},
    }


def self_test_capture_plan_check() -> dict[str, Any]:
    return {
        "status": "pass",
        "display_status": "PASS",
        "safety": {
            "capture_plan_review_allowed": True,
            "live_collection_allowed": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
        },
    }


def self_test_live_gate() -> dict[str, Any]:
    return {"status": "defined_not_satisfied", "display_status": "BLOCKED", "live_collection_allowed": False}


def self_test_wrapper_text() -> str:
    return "\n".join(REQUIRED_WRAPPER_TOKENS.values())


def self_test_auth_docs() -> dict[str, str]:
    return {path: " ".join(tokens) for path, tokens in REQUIRED_AUTH_TOKENS.items()}


def run_self_test() -> tuple[bool, dict[str, Any]]:
    tests: list[dict[str, Any]] = []

    def record(
        name: str,
        expected_status: str,
        *,
        operator_patch: dict[str, Any] | None = None,
        capture_plan_patch: dict[str, Any] | None = None,
        wrapper_text: str | None = None,
        riftreader_run_cmd_exists: bool = True,
        expected_blocker_part: str | None = None,
    ) -> None:
        operator_gate = clone_json(self_test_operator_gate())
        capture_plan_check = clone_json(self_test_capture_plan_check())
        if operator_patch:
            operator_gate.update(operator_patch)
        if capture_plan_patch:
            capture_plan_check.update(capture_plan_patch)
        evaluation = evaluate_readiness(
            operator_gate=operator_gate,
            capture_plan_check=capture_plan_check,
            live_gate=clone_json(self_test_live_gate()),
            live_gate_checklist_text="movement abort",
            live_test_cmd_exists=True,
            live_test_ps1_text=self_test_wrapper_text() if wrapper_text is None else wrapper_text,
            focus_control_exists=True,
            riftreader_run_cmd_exists=riftreader_run_cmd_exists,
            auth_docs=self_test_auth_docs(),
        )
        blockers = evaluation["blockers"]
        status_ok = evaluation["status"] == expected_status
        expected_display = "PASS" if expected_status == "pass" else "BLOCKED"
        display_ok = evaluation["display_status"] == expected_display
        blocker_ok = expected_blocker_part is None or any(expected_blocker_part in blocker for blocker in blockers)
        safety = evaluation.get("safety") if isinstance(evaluation.get("safety"), dict) else {}
        safety_ok = (
            safety.get("movement_or_input_sent") is False
            and safety.get("capture_started") is False
            and safety.get("memory_scan_or_read_started") is False
            and safety.get("reloadui_sent") is False
        )
        tests.append(
            {
                "name": name,
                "expected_status": expected_status,
                "actual_status": evaluation["status"],
                "expected_display_status": expected_display,
                "actual_display_status": evaluation["display_status"],
                "blockers": blockers,
                "pass": status_ok and display_ok and blocker_ok and safety_ok,
            }
        )

    record("all readiness inputs pass", "pass")
    record(
        "blocked operator gate",
        "blocked_movement_test_not_ready",
        operator_patch={"metadata_capture_plan_gate": "BLOCKED"},
        expected_blocker_part="metadata_capture_plan_gate",
    )
    record(
        "blocked capture plan check",
        "blocked_movement_test_not_ready",
        capture_plan_patch={"display_status": "BLOCKED", "status": "blocked_capture_plan_not_valid"},
        expected_blocker_part="Capture Plan Check is not PASS",
    )
    record(
        "blocked wrapper missing movement support",
        "blocked_movement_test_not_ready",
        wrapper_text="PreflightOnly",
        expected_blocker_part="wrapper is missing required guard features",
    )
    record(
        "blocked missing RiftReader",
        "blocked_movement_test_not_ready",
        riftreader_run_cmd_exists=False,
        expected_blocker_part="RiftReader run-reader.cmd is missing",
    )

    passed = all(test["pass"] for test in tests)
    return passed, {
        "schema_version": "riftscan.movement_test_readiness_self_test.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if passed else "FAIL",
        "case_count": len(tests),
        "tests": tests,
        "safety": {
            "writes_current_artifacts": False,
            "runs_focus_preflight": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "reloadui_sent": False,
        },
    }


def parse(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Validate readiness for a future guarded RiftScan live movement test.")
    p.add_argument("--riftreader-repo", default=str(DEFAULT_RIFTREADER_REPO))
    p.add_argument("--git-timeout-seconds", type=int, default=15)
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
    log("movement_test_readiness_start", version=APP_VERSION)
    riftreader_run_cmd = Path(args.riftreader_repo) / "scripts" / "run-reader.cmd"
    auth_docs = {path: read_text(REPO_ROOT / path) for path in REQUIRED_AUTH_TOKENS}
    evaluation = evaluate_readiness(
        operator_gate=read_json(OPERATOR_GATE_SUMMARY),
        capture_plan_check=read_json(CAPTURE_PLAN_CHECK_SUMMARY),
        live_gate=read_json(LIVE_COLLECTION_GATE_SUMMARY),
        live_gate_checklist_text=read_text(LIVE_COLLECTION_GATE_CHECKLIST),
        live_test_cmd_exists=LIVE_TEST_CMD.exists(),
        live_test_ps1_text=read_text(LIVE_TEST_PS1),
        focus_control_exists=FOCUS_CONTROL.exists(),
        riftreader_run_cmd_exists=riftreader_run_cmd.exists(),
        auth_docs=auth_docs,
    )
    data = {
        "schema_version": "riftscan.movement_test_readiness.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        **evaluation,
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
        "source_artifacts": {
            "operator_gate_summary": rel(OPERATOR_GATE_SUMMARY),
            "capture_plan_check_summary": rel(CAPTURE_PLAN_CHECK_SUMMARY),
            "live_collection_gate_checklist": rel(LIVE_COLLECTION_GATE_CHECKLIST),
            "live_collection_gate_summary": rel(LIVE_COLLECTION_GATE_SUMMARY),
            "live_test_cmd": rel(LIVE_TEST_CMD),
            "live_test_ps1": rel(LIVE_TEST_PS1),
            "focus_control": rel(FOCUS_CONTROL),
            "riftreader_run_cmd": str(riftreader_run_cmd),
        },
        "git": git_snapshot(args.git_timeout_seconds),
    }

    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("movement_test_readiness_finish", status=data["status"], blocker_count=len(data["blockers"]))

    print(f"MOVEMENT TEST READINESS: {data['display_status']}")
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

# End of script: riftscan_movement_test_readiness.py
