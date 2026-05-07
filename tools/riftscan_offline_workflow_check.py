#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-offline-workflow-check-v1.0.8
# Total character count: 000000
# Purpose: Run conservative offline helper workflow checks, refresh the offline discovery ledger, and write deterministic report artifacts.
# Safety boundary: Offline validation only. No RIFT focus preflight, live capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-offline-workflow-check-v1.0.8"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "offline-workflow-check"
REPORT = OUT_DIR / "OFFLINE_WORKFLOW_CHECK_REPORT.md"
SUMMARY = OUT_DIR / "offline-workflow-check-summary.json"
LOG = OUT_DIR / "offline-workflow-check-log.jsonl"
AI_WORKFLOW_SUMMARY = REPO_ROOT / "handoffs" / "current" / "ai-workflow" / "ai-workflow-summary.json"
AI_WORKFLOW_SCHEMA_DOC = REPO_ROOT / "docs" / "ai-workflow-packet-schema.md"
REQUIRED_AI_PACKET_DIFF_FIELDS = [
    "app_version",
    "status",
    "blocker_count",
    "warning_count",
    "current_best_stable_id",
    "current_best_address",
    "candidate_consumer_status",
    "safe_candidate_count",
    "rejected_candidate_count",
    "artifact_stale_count",
    "artifact_missing_count",
    "current_best_stale_count",
    "current_best_missing_count",
    "discovery_ledger_contract_status",
    "offline_workflow_status",
    "operator_live_collection_allowed",
]


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


def run_command(name: str, args: list[str], timeout: int) -> dict[str, Any]:
    log("command_start", name=name, args=args)
    try:
        p = subprocess.run(args, cwd=REPO_ROOT, text=True, capture_output=True, timeout=timeout, check=False)
        result = {
            "name": name,
            "args": args,
            "exit_code": p.returncode,
            "status": "pass" if p.returncode == 0 else "fail",
            "stdout": p.stdout,
            "stderr": p.stderr,
        }
        log("command_finish", name=name, exit_code=p.returncode, status=result["status"])
        return result
    except subprocess.TimeoutExpired as exc:
        result = {
            "name": name,
            "args": args,
            "exit_code": None,
            "status": "fail",
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or "",
            "error": f"timed out after {timeout} seconds",
        }
        log("command_timeout", name=name, timeout_seconds=timeout)
        return result


def git_snapshot(timeout: int) -> dict[str, Any]:
    head = run_command("git_head", ["git", "rev-parse", "HEAD"], timeout)
    status = run_command("git_status_short", ["git", "status", "--short"], timeout)
    recent = run_command("git_log_oneline_5", ["git", "log", "--oneline", "-5"], timeout)
    return {
        "head": head.get("stdout", "").strip() if head.get("exit_code") == 0 else None,
        "status_short": status.get("stdout", "") if status.get("exit_code") == 0 else status.get("stderr", ""),
        "log_oneline_5": recent.get("stdout", "").strip() if recent.get("exit_code") == 0 else recent.get("stderr", ""),
        "command_status": {
            "head": head.get("status"),
            "status": status.get("status"),
            "log": recent.get("status"),
        },
    }


def clean_python_caches() -> None:
    cache_dir = REPO_ROOT / "tools" / "__pycache__"
    if not cache_dir.exists():
        return

    resolved = cache_dir.resolve()
    try:
        resolved.relative_to(REPO_ROOT.resolve())
    except ValueError:
        log("python_cache_cleanup_skipped", path=str(cache_dir), reason="outside_repo")
        return

    shutil.rmtree(resolved, ignore_errors=True)
    log("python_cache_cleanup_finish", path=rel(cache_dir))


def read_json_file(path: Path) -> tuple[dict[str, Any] | None, str | None]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return None, f"missing file: {rel(path)}"
    except json.JSONDecodeError as exc:
        return None, f"invalid json in {rel(path)}: {exc}"
    except OSError as exc:
        return None, f"unable to read {rel(path)}: {exc}"
    if not isinstance(data, dict):
        return None, f"json root is not an object: {rel(path)}"
    return data, None


def validate_ai_workflow_packet_contract(summary: dict[str, Any], schema_doc_text: str) -> list[str]:
    errors: list[str] = []

    packet_diff = summary.get("previous_packet_diff")
    if not isinstance(packet_diff, dict):
        errors.append("previous_packet_diff is missing or not an object")
    else:
        if packet_diff.get("schema_version") != "riftscan.ai_workflow_packet_diff.v1":
            errors.append("previous_packet_diff.schema_version is not riftscan.ai_workflow_packet_diff.v1")
        if packet_diff.get("status") not in {"NO_PREVIOUS_PACKET", "UNCHANGED", "CHANGED"}:
            errors.append("previous_packet_diff.status is not a recognized value")
        if not isinstance(packet_diff.get("change_count"), int):
            errors.append("previous_packet_diff.change_count is not an integer")
        if not isinstance(packet_diff.get("changes"), list):
            errors.append("previous_packet_diff.changes is not a list")

    compared_fields = summary.get("previous_packet_diff_compared_fields")
    if not isinstance(compared_fields, list):
        errors.append("previous_packet_diff_compared_fields is missing or not a list")
        compared_field_names: set[str] = set()
    else:
        compared_field_names = set()
        for index, item in enumerate(compared_fields):
            if not isinstance(item, dict):
                errors.append(f"previous_packet_diff_compared_fields[{index}] is not an object")
                continue
            field = item.get("field")
            packet_path = item.get("packet_path")
            if not isinstance(field, str) or not field:
                errors.append(f"previous_packet_diff_compared_fields[{index}].field is missing")
            else:
                compared_field_names.add(field)
            if not isinstance(packet_path, str) or not packet_path:
                errors.append(f"previous_packet_diff_compared_fields[{index}].packet_path is missing")

    missing_fields = [field for field in REQUIRED_AI_PACKET_DIFF_FIELDS if field not in compared_field_names]
    if missing_fields:
        errors.append(f"previous_packet_diff_compared_fields missing required field(s): {', '.join(missing_fields)}")

    if "previous_packet_diff_compared_fields" not in schema_doc_text:
        errors.append("schema doc does not mention previous_packet_diff_compared_fields")
    missing_doc_fields = [field for field in REQUIRED_AI_PACKET_DIFF_FIELDS if f"`{field}`" not in schema_doc_text]
    if missing_doc_fields:
        errors.append(f"schema doc missing compared field(s): {', '.join(missing_doc_fields)}")

    return errors


def run_ai_workflow_packet_contract_check() -> dict[str, Any]:
    name = "ai_workflow_packet_contract"
    log("artifact_contract_check_start", name=name)
    summary, error = read_json_file(AI_WORKFLOW_SUMMARY)
    if error:
        errors = [error]
        schema_doc_text = ""
    else:
        try:
            schema_doc_text = AI_WORKFLOW_SCHEMA_DOC.read_text(encoding="utf-8")
            errors = validate_ai_workflow_packet_contract(summary or {}, schema_doc_text)
        except FileNotFoundError:
            errors = [f"missing file: {rel(AI_WORKFLOW_SCHEMA_DOC)}"]
            schema_doc_text = ""
        except OSError as exc:
            errors = [f"unable to read {rel(AI_WORKFLOW_SCHEMA_DOC)}: {exc}"]
            schema_doc_text = ""

    status = "pass" if not errors else "fail"
    result = {
        "name": name,
        "args": [],
        "exit_code": 0 if not errors else 1,
        "status": status,
        "stdout": f"checked_fields={len(REQUIRED_AI_PACKET_DIFF_FIELDS)}\n",
        "stderr": "\n".join(errors),
        "summary_path": rel(AI_WORKFLOW_SUMMARY),
        "schema_doc_path": rel(AI_WORKFLOW_SCHEMA_DOC),
        "required_field_count": len(REQUIRED_AI_PACKET_DIFF_FIELDS),
        "errors": errors,
    }
    log("artifact_contract_check_finish", name=name, status=status, error_count=len(errors))
    return result


def helper_checks(timeout_seconds: int) -> list[tuple[str, list[str], int]]:
    return [
        (
            "py_compile_helpers",
            [
                sys.executable,
                "-m",
                "py_compile",
                "tools/riftscan_operator_app.py",
                "tools/riftscan_post_update_baseline.py",
                "tools/riftscan_capture_readiness.py",
                "tools/riftscan_patch_intake_app.py",
                "tools/riftscan_offline_workflow_check.py",
                "tools/riftscan_capture_plan_check.py",
                "tools/riftscan_movement_test_readiness.py",
                "tools/riftscan_movement_execution_gate.py",
                "tools/riftscan_discovery_ledger.py",
                "tools/riftscan_ai_workflow_packet.py",
                "tools/riftscan_candidate_ledger_consumer.py",
            ],
            timeout_seconds,
        ),
        (
            "offline_workflow_check_self_test",
            [sys.executable, "tools/riftscan_offline_workflow_check.py", "--self-test"],
            timeout_seconds,
        ),
        ("operator_self_test", [sys.executable, "tools/riftscan_operator_app.py", "--self-test"], timeout_seconds),
        (
            "post_update_baseline_self_test",
            [sys.executable, "tools/riftscan_post_update_baseline.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "capture_readiness_self_test",
            [sys.executable, "tools/riftscan_capture_readiness.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "capture_plan_check_self_test",
            [sys.executable, "tools/riftscan_capture_plan_check.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "movement_test_readiness_self_test",
            [sys.executable, "tools/riftscan_movement_test_readiness.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "movement_execution_gate_self_test",
            [sys.executable, "tools/riftscan_movement_execution_gate.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "discovery_ledger_self_test",
            [sys.executable, "tools/riftscan_discovery_ledger.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "discovery_ledger_refresh",
            [sys.executable, "tools/riftscan_discovery_ledger.py"],
            max(timeout_seconds, 120),
        ),
        (
            "discovery_ledger_validate_existing",
            [sys.executable, "tools/riftscan_discovery_ledger.py", "--validate-existing"],
            timeout_seconds,
        ),
        (
            "candidate_ledger_consumer_self_test",
            [sys.executable, "tools/riftscan_candidate_ledger_consumer.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "candidate_ledger_consumer_refresh",
            [sys.executable, "tools/riftscan_candidate_ledger_consumer.py", "--strict-exit-code"],
            timeout_seconds,
        ),
        (
            "ai_workflow_packet_self_test",
            [sys.executable, "tools/riftscan_ai_workflow_packet.py", "--self-test"],
            timeout_seconds,
        ),
        (
            "patch_intake_self_test",
            [sys.executable, "tools/riftscan_patch_intake_app.py", "--self-test"],
            max(timeout_seconds, 180),
        ),
    ]


def summarize_results(results: list[dict[str, Any]]) -> dict[str, Any]:
    failed = [item for item in results if item.get("status") != "pass"]
    return {
        "status": "pass" if not failed else "fail",
        "display_status": "PASS" if not failed else "FAIL",
        "failed_check_count": len(failed),
        "failed_checks": [item.get("name") for item in failed],
    }


def build_report(data: dict[str, Any]) -> str:
    failures = data.get("failed_checks") or ["None"]
    failure_text = "\n".join(f"- {failure}" for failure in failures)
    checks = data.get("checks") if isinstance(data.get("checks"), list) else []
    check_lines = []
    for check in checks:
        check_lines.append(f"- `{check.get('status')}` `{check.get('name')}` exit=`{check.get('exit_code')}`")
    return f"""# RiftScan Offline Workflow Check Report

## Result

```text
OFFLINE WORKFLOW CHECK: {data["display_status"]}
status: {data["status"]}
failed_check_count: {data["failed_check_count"]}
```

## Failed Checks

{failure_text}

## Checks

{chr(10).join(check_lines)}

## Output Paths

```text
report: {rel(REPORT)}
summary: {rel(SUMMARY)}
log: {rel(LOG)}
```

## Safety Boundary

```text
offline_only: true
focus_preflight_started: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
riftreader_command_executed: false
reloadui_sent: false
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


def run_offline_check(args: argparse.Namespace) -> dict[str, Any]:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    log("offline_workflow_check_start", version=APP_VERSION)
    results = [run_command(name, cmd, timeout) for name, cmd, timeout in helper_checks(args.timeout_seconds)]
    results.append(run_ai_workflow_packet_contract_check())
    clean_python_caches()
    summary = summarize_results(results)
    data = {
        "schema_version": "riftscan.offline_workflow_check.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        **summary,
        "checks": results,
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
        "safety": {
            "offline_only": True,
            "focus_preflight_started": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "offset_validation_started": False,
            "riftreader_validation_started": False,
            "riftreader_command_executed": False,
            "reloadui_sent": False,
        },
        "git": git_snapshot(args.git_timeout_seconds),
    }
    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("offline_workflow_check_finish", status=data["status"], failed_check_count=data["failed_check_count"])
    return data


def run_self_test() -> tuple[bool, dict[str, Any]]:
    cases = [
        {
            "name": "all pass",
            "results": [{"name": "a", "status": "pass"}, {"name": "b", "status": "pass"}],
            "expected": "pass",
        },
        {
            "name": "one fail",
            "results": [{"name": "a", "status": "pass"}, {"name": "b", "status": "fail"}],
            "expected": "fail",
        },
    ]
    tests = []
    for case in cases:
        summary = summarize_results(case["results"])
        tests.append(
            {
                "name": case["name"],
                "expected": case["expected"],
                "actual": summary["status"],
                "failed_checks": summary["failed_checks"],
                "pass": summary["status"] == case["expected"],
            }
        )
    valid_summary = {
        "previous_packet_diff": {
            "schema_version": "riftscan.ai_workflow_packet_diff.v1",
            "status": "UNCHANGED",
            "change_count": 0,
            "changes": [],
        },
        "previous_packet_diff_compared_fields": [
            {"field": field, "packet_path": field.replace("_", ".")} for field in REQUIRED_AI_PACKET_DIFF_FIELDS
        ],
    }
    valid_doc = "previous_packet_diff_compared_fields\n" + "\n".join(f"`{field}`" for field in REQUIRED_AI_PACKET_DIFF_FIELDS)
    valid_errors = validate_ai_workflow_packet_contract(valid_summary, valid_doc)
    tests.append(
        {
            "name": "ai packet contract pass",
            "expected": "pass",
            "actual": "pass" if not valid_errors else "fail",
            "errors": valid_errors,
            "pass": not valid_errors,
        }
    )
    invalid_summary = {
        "previous_packet_diff": {
            "schema_version": "riftscan.ai_workflow_packet_diff.v1",
            "status": "UNCHANGED",
            "change_count": 0,
            "changes": [],
        },
        "previous_packet_diff_compared_fields": [
            {"field": "app_version", "packet_path": "app_version"},
        ],
    }
    invalid_errors = validate_ai_workflow_packet_contract(invalid_summary, valid_doc)
    tests.append(
        {
            "name": "ai packet contract blocks missing fields",
            "expected": "fail",
            "actual": "fail" if invalid_errors else "pass",
            "errors": invalid_errors,
            "pass": bool(invalid_errors),
        }
    )
    passed = all(test["pass"] for test in tests)
    return passed, {
        "schema_version": "riftscan.offline_workflow_check_self_test.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if passed else "FAIL",
        "case_count": len(tests),
        "tests": tests,
        "safety": {
            "writes_artifacts": False,
            "runs_focus_preflight": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "riftreader_command_executed": False,
            "reloadui_sent": False,
        },
    }


def parse(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Run conservative offline RiftScan helper workflow checks.")
    p.add_argument("--timeout-seconds", type=int, default=120)
    p.add_argument("--git-timeout-seconds", type=int, default=15)
    p.add_argument("--self-test", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse(argv)
    if args.self_test:
        passed, summary = run_self_test()
        print(json.dumps(summary, indent=2, sort_keys=True))
        return 0 if passed else 1

    data = run_offline_check(args)
    print(f"OFFLINE WORKFLOW CHECK: {data['display_status']}")
    print(f"Report: {rel(REPORT)}")
    print(f"Summary: {rel(SUMMARY)}")
    print(f"Log: {rel(LOG)}")
    for failure in data["failed_checks"]:
        print(f"- {failure}")
    return 0 if data["status"] == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# End of script: riftscan_offline_workflow_check.py
