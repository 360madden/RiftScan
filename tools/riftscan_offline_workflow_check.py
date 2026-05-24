#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-offline-workflow-check-v1.0.13
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

APP_VERSION = "riftscan-offline-workflow-check-v1.0.13"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "offline-workflow-check"
REPORT = OUT_DIR / "OFFLINE_WORKFLOW_CHECK_REPORT.md"
SUMMARY = OUT_DIR / "offline-workflow-check-summary.json"
LOG = OUT_DIR / "offline-workflow-check-log.jsonl"
LOG_ENABLED = True
AI_WORKFLOW_SUMMARY = REPO_ROOT / "handoffs" / "current" / "ai-workflow" / "ai-workflow-summary.json"
AI_WORKFLOW_HISTORY_SUMMARY = REPO_ROOT / "handoffs" / "current" / "ai-workflow" / "ai-workflow-history-index-summary.json"
AI_WORKFLOW_HISTORY_REPORT = REPO_ROOT / "handoffs" / "current" / "ai-workflow" / "AI_WORKFLOW_HISTORY_INDEX_REPORT.md"
AI_WORKFLOW_SCHEMA_DOC = REPO_ROOT / "docs" / "ai-workflow-packet-schema.md"
AI_WORKFLOW_HISTORY_DIR = REPO_ROOT / "handoffs" / "current" / "ai-workflow" / "history"
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
    if not LOG_ENABLED:
        return
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


def read_jsonl_file(path: Path) -> tuple[list[dict[str, Any]] | None, str | None]:
    entries: list[dict[str, Any]] = []
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except FileNotFoundError:
        return None, f"missing file: {rel(path)}"
    except OSError as exc:
        return None, f"unable to read {rel(path)}: {exc}"

    for line_number, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            data = json.loads(line)
        except json.JSONDecodeError as exc:
            return None, f"invalid jsonl in {rel(path)} line {line_number}: {exc}"
        if not isinstance(data, dict):
            return None, f"jsonl row is not an object in {rel(path)} line {line_number}"
        entries.append(data)
    return entries, None


def repo_artifact_path(value: Any) -> Path | None:
    if not isinstance(value, str) or not value:
        return None
    raw_path = Path(value)
    path = raw_path if raw_path.is_absolute() else REPO_ROOT / raw_path
    try:
        resolved = path.resolve()
        resolved.relative_to(REPO_ROOT.resolve())
    except (OSError, ValueError):
        return None
    return resolved


def validate_ai_workflow_packet_contract(summary: dict[str, Any], schema_doc_text: str, *, validate_current_artifacts: bool = True) -> list[str]:
    errors: list[str] = []

    paths = summary.get("paths")
    if not isinstance(paths, dict):
        errors.append("paths is missing or not an object")
        paths = {}
    else:
        expected_paths = {
            "history_report": AI_WORKFLOW_HISTORY_REPORT,
            "history_summary": AI_WORKFLOW_HISTORY_SUMMARY,
        }
        for field, expected_path in expected_paths.items():
            path_value = paths.get(field)
            path = repo_artifact_path(path_value)
            if path is None:
                errors.append(f"paths.{field} is missing or outside the repo")
                continue
            if path.resolve() != expected_path.resolve():
                errors.append(f"paths.{field} is not {rel(expected_path)}")
            if validate_current_artifacts and not path.is_file():
                errors.append(f"paths.{field} does not exist: {path_value}")

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

    archive = summary.get("previous_packet_archive")
    if not isinstance(archive, dict):
        errors.append("previous_packet_archive is missing or not an object")
    else:
        archive_status = archive.get("status")
        if archive_status not in {"ARCHIVED", "NO_PREVIOUS_PACKET"}:
            errors.append("previous_packet_archive.status is not a recognized value")

        history_dir = repo_artifact_path(archive.get("history_dir"))
        if history_dir is None:
            errors.append("previous_packet_archive.history_dir is missing or outside the repo")
        elif archive_status == "ARCHIVED" and not history_dir.is_dir():
            errors.append(f"previous_packet_archive.history_dir does not exist: {archive.get('history_dir')}")

        artifacts = archive.get("artifacts")
        if archive_status == "ARCHIVED":
            history_index = repo_artifact_path(archive.get("history_index"))
            if history_index is None:
                errors.append("previous_packet_archive.history_index is missing or outside the repo")
            elif not history_index.is_file():
                errors.append(f"previous_packet_archive.history_index does not exist: {archive.get('history_index')}")

            packet_history_index = summary.get("packet_history_index")
            if not isinstance(packet_history_index, dict):
                errors.append("packet_history_index is missing or not an object")
            else:
                if packet_history_index.get("status") != "APPENDED":
                    errors.append("packet_history_index.status is not APPENDED for an archived packet")
                if packet_history_index.get("path") != archive.get("history_index"):
                    errors.append("packet_history_index.path does not match previous_packet_archive.history_index")

            if not isinstance(artifacts, dict):
                errors.append("previous_packet_archive.artifacts is missing or not an object")
            else:
                for artifact_name in ("summary", "report"):
                    artifact_path_text = artifacts.get(artifact_name)
                    artifact_path = repo_artifact_path(artifact_path_text)
                    if artifact_path is None:
                        errors.append(f"previous_packet_archive.artifacts.{artifact_name} is missing or outside the repo")
                        continue
                    if not artifact_path.is_file():
                        errors.append(f"previous_packet_archive.artifacts.{artifact_name} does not exist: {artifact_path_text}")
                summary_path = repo_artifact_path(artifacts.get("summary"))
                if summary_path and summary_path.is_file():
                    archived_summary, archived_error = read_json_file(summary_path)
                    if archived_error:
                        errors.append(f"previous_packet_archive.artifacts.summary is not valid JSON: {archived_error}")
                    elif not isinstance(archived_summary, dict):
                        errors.append("previous_packet_archive.artifacts.summary is not a JSON object")

                if history_index and history_index.is_file():
                    index_entries, index_error = read_jsonl_file(history_index)
                    if index_error:
                        errors.append(f"previous_packet_archive.history_index is invalid: {index_error}")
                    else:
                        for entry_index, entry in enumerate(index_entries or [], start=1):
                            if entry.get("schema_version") != "riftscan.ai_workflow_packet_history_index.v1":
                                errors.append(f"previous_packet_archive.history_index[{entry_index}].schema_version is invalid")
                            entry_artifacts = entry.get("artifacts")
                            if not isinstance(entry_artifacts, dict):
                                errors.append(f"previous_packet_archive.history_index[{entry_index}].artifacts is missing or not an object")
                                continue
                            for artifact_name in ("summary", "report"):
                                artifact_path_text = entry_artifacts.get(artifact_name)
                                artifact_path = repo_artifact_path(artifact_path_text)
                                if artifact_path is None:
                                    errors.append(f"previous_packet_archive.history_index[{entry_index}].artifacts.{artifact_name} is missing or outside the repo")
                                    continue
                                if not artifact_path.is_file():
                                    errors.append(f"previous_packet_archive.history_index[{entry_index}].artifacts.{artifact_name} does not exist: {artifact_path_text}")
                            summary_path = repo_artifact_path(entry_artifacts.get("summary"))
                            if summary_path and summary_path.is_file():
                                archived_summary, archived_error = read_json_file(summary_path)
                                if archived_error:
                                    errors.append(f"previous_packet_archive.history_index[{entry_index}].artifacts.summary is not valid JSON: {archived_error}")
                                elif not isinstance(archived_summary, dict):
                                    errors.append(f"previous_packet_archive.history_index[{entry_index}].artifacts.summary is not a JSON object")
                        expected_summary = artifacts.get("summary")
                        expected_report = artifacts.get("report")
                        matching_entries = [
                            entry
                            for entry in index_entries or []
                            if isinstance(entry.get("artifacts"), dict)
                            and entry["artifacts"].get("summary") == expected_summary
                            and entry["artifacts"].get("report") == expected_report
                        ]
                        if not matching_entries:
                            errors.append("previous_packet_archive.history_index has no entry for the archived summary/report")

    if "previous_packet_archive" not in schema_doc_text:
        errors.append("schema doc does not mention previous_packet_archive")
    if "history_index" not in schema_doc_text:
        errors.append("schema doc does not mention history_index")
    if "packet_history_index" not in schema_doc_text:
        errors.append("schema doc does not mention packet_history_index")
    if "AI_WORKFLOW_HISTORY_INDEX_REPORT.md" not in schema_doc_text:
        errors.append("schema doc does not mention AI_WORKFLOW_HISTORY_INDEX_REPORT.md")
    if "ai-workflow-history-index-summary.json" not in schema_doc_text:
        errors.append("schema doc does not mention ai-workflow-history-index-summary.json")

    if validate_current_artifacts:
        history_summary, history_summary_error = read_json_file(AI_WORKFLOW_HISTORY_SUMMARY)
        if history_summary_error:
            errors.append(f"history index summary is invalid: {history_summary_error}")
        else:
            if history_summary.get("schema_version") != "riftscan.ai_workflow_packet_history_report.v1":
                errors.append("history index summary schema_version is not riftscan.ai_workflow_packet_history_report.v1")
            if history_summary.get("status") not in {"PASS", "FAIL"}:
                errors.append("history index summary status is not PASS or FAIL")
            verification = history_summary.get("verification")
            if not isinstance(verification, dict):
                errors.append("history index summary verification is missing or not an object")
            elif verification.get("status") != "PASS":
                errors.append("history index summary verification.status is not PASS")
            history = history_summary.get("history_index")
            if not isinstance(history, dict):
                errors.append("history index summary history_index is missing or not an object")
            elif not isinstance(history.get("entries"), list):
                errors.append("history index summary history_index.entries is missing or not a list")

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


def helper_checks(timeout_seconds: int, *, check_only: bool = False) -> list[tuple[str, list[str], int]]:
    checks = [
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
    ]

    if check_only:
        checks.extend(
            [
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
                    "candidate_ledger_consumer_check_only",
                    [sys.executable, "tools/riftscan_candidate_ledger_consumer.py", "--check-only", "--strict-exit-code"],
                    timeout_seconds,
                ),
                (
                    "ai_workflow_packet_self_test",
                    [sys.executable, "tools/riftscan_ai_workflow_packet.py", "--self-test"],
                    timeout_seconds,
                ),
                (
                    "ai_workflow_history_index_verify",
                    [sys.executable, "tools/riftscan_ai_workflow_packet.py", "--verify-history-index", "--history-limit", "0"],
                    timeout_seconds,
                ),
                (
                    "patch_intake_self_test",
                    [sys.executable, "tools/riftscan_patch_intake_app.py", "--self-test"],
                    max(timeout_seconds, 180),
                ),
            ]
        )
        return checks

    checks.extend(
        [
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
    )
    return checks


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
    global LOG_ENABLED

    LOG_ENABLED = not args.check_only
    if not args.check_only:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
    log("offline_workflow_check_start", version=APP_VERSION)
    results = [run_command(name, cmd, timeout) for name, cmd, timeout in helper_checks(args.timeout_seconds, check_only=args.check_only)]
    results.append(run_ai_workflow_packet_contract_check())
    clean_python_caches()
    summary = summarize_results(results)
    data = {
        "schema_version": "riftscan.offline_workflow_check.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "mode": "check_only" if args.check_only else "write_artifacts",
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
            "writes_report_artifacts": not args.check_only,
        },
        "git": git_snapshot(args.git_timeout_seconds),
    }
    if not args.check_only:
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
        "paths": {
            "history_report": "handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md",
            "history_summary": "handoffs/current/ai-workflow/ai-workflow-history-index-summary.json",
        },
        "previous_packet_diff": {
            "schema_version": "riftscan.ai_workflow_packet_diff.v1",
            "status": "UNCHANGED",
            "change_count": 0,
            "changes": [],
        },
        "previous_packet_diff_compared_fields": [
            {"field": field, "packet_path": field.replace("_", ".")} for field in REQUIRED_AI_PACKET_DIFF_FIELDS
        ],
        "previous_packet_archive": {
            "status": "NO_PREVIOUS_PACKET",
            "history_dir": "handoffs/current/ai-workflow/history",
            "history_index": "handoffs/current/ai-workflow/history/index.jsonl",
            "artifacts": {},
        },
    }
    valid_doc = "previous_packet_diff_compared_fields\nprevious_packet_archive\nhistory_index\npacket_history_index\nAI_WORKFLOW_HISTORY_INDEX_REPORT.md\nai-workflow-history-index-summary.json\n" + "\n".join(f"`{field}`" for field in REQUIRED_AI_PACKET_DIFF_FIELDS)
    valid_errors = validate_ai_workflow_packet_contract(valid_summary, valid_doc, validate_current_artifacts=False)
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
        "paths": {
            "history_report": "handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md",
            "history_summary": "handoffs/current/ai-workflow/ai-workflow-history-index-summary.json",
        },
        "previous_packet_diff": {
            "schema_version": "riftscan.ai_workflow_packet_diff.v1",
            "status": "UNCHANGED",
            "change_count": 0,
            "changes": [],
        },
        "previous_packet_diff_compared_fields": [
            {"field": "app_version", "packet_path": "app_version"},
        ],
        "previous_packet_archive": {
            "status": "NO_PREVIOUS_PACKET",
            "history_dir": "handoffs/current/ai-workflow/history",
            "history_index": "handoffs/current/ai-workflow/history/index.jsonl",
            "artifacts": {},
        },
    }
    invalid_errors = validate_ai_workflow_packet_contract(invalid_summary, valid_doc, validate_current_artifacts=False)
    tests.append(
        {
            "name": "ai packet contract blocks missing fields",
            "expected": "fail",
            "actual": "fail" if invalid_errors else "pass",
            "errors": invalid_errors,
            "pass": bool(invalid_errors),
        }
    )
    invalid_archive_summary = dict(valid_summary)
    invalid_archive_summary["previous_packet_archive"] = {
        "status": "ARCHIVED",
        "history_dir": "handoffs/current/ai-workflow/history",
        "artifacts": {},
    }
    invalid_archive_errors = validate_ai_workflow_packet_contract(invalid_archive_summary, valid_doc, validate_current_artifacts=False)
    tests.append(
        {
            "name": "ai packet contract blocks archived packet without artifact paths",
            "expected": "fail",
            "actual": "fail" if invalid_archive_errors else "pass",
            "errors": invalid_archive_errors,
            "pass": bool(invalid_archive_errors),
        }
    )
    check_only_names = {name for name, _cmd, _timeout in helper_checks(120, check_only=True)}
    mutating_names = {name for name, _cmd, _timeout in helper_checks(120, check_only=False)}
    check_only_errors: list[str] = []
    for forbidden in ("discovery_ledger_refresh", "candidate_ledger_consumer_refresh"):
        if forbidden in check_only_names:
            check_only_errors.append(f"{forbidden}_present")
    for required in ("discovery_ledger_validate_existing", "candidate_ledger_consumer_check_only", "ai_workflow_history_index_verify"):
        if required not in check_only_names:
            check_only_errors.append(f"{required}_missing")
    for required in ("discovery_ledger_refresh", "candidate_ledger_consumer_refresh"):
        if required not in mutating_names:
            check_only_errors.append(f"{required}_missing_from_write_mode")
    tests.append(
        {
            "name": "check-only helper set excludes refresh/write checks",
            "expected": "pass",
            "actual": "pass" if not check_only_errors else "fail",
            "errors": check_only_errors,
            "pass": not check_only_errors,
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
    p.add_argument("--check-only", "--no-write", dest="check_only", action="store_true", help="Run read-only checks without refreshing ledgers or writing report, summary, or log artifacts.")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse(argv)
    if args.self_test:
        passed, summary = run_self_test()
        print(json.dumps(summary, indent=2, sort_keys=True))
        return 0 if passed else 1

    data = run_offline_check(args)
    print(f"OFFLINE WORKFLOW CHECK: {data['display_status']}")
    if args.check_only:
        print("Mode: check-only")
        print("No report, summary, or log artifacts were written.")
    else:
        print(f"Report: {rel(REPORT)}")
        print(f"Summary: {rel(SUMMARY)}")
        print(f"Log: {rel(LOG)}")
    for failure in data["failed_checks"]:
        print(f"- {failure}")
    return 0 if data["status"] == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# End of script: riftscan_offline_workflow_check.py
