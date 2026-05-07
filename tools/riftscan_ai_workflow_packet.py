#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-ai-workflow-packet-v1.8.0
# Total character count: 000000
# Purpose: Build a compact offline AI workflow packet from current RiftScan handoff and gate artifacts.
# Safety boundary: Reads existing artifacts and local git metadata only. No focus preflight, live capture, input, movement, memory scan/read, process attach, offset validation, RiftReader command execution, or /reloadui.

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-ai-workflow-packet-v1.8.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "ai-workflow"
REPORT = OUT_DIR / "AI_WORKFLOW_PACKET.md"
SUMMARY = OUT_DIR / "ai-workflow-summary.json"
LOG = OUT_DIR / "ai-workflow-log.jsonl"
HISTORY_DIR = OUT_DIR / "history"
HISTORY_INDEX = HISTORY_DIR / "index.jsonl"

README_CURRENT = REPO_ROOT / "handoffs" / "current" / "README_CURRENT.md"
DISCOVERY_LEDGER_REPORT = REPO_ROOT / "handoffs" / "current" / "discovery-ledger" / "DISCOVERY_LEDGER_REPORT.md"
DISCOVERY_LEDGER_SUMMARY = REPO_ROOT / "handoffs" / "current" / "discovery-ledger" / "discovery-ledger-summary.json"
OFFLINE_WORKFLOW_SUMMARY = REPO_ROOT / "handoffs" / "current" / "offline-workflow-check" / "offline-workflow-check-summary.json"
OPERATOR_SUMMARY = REPO_ROOT / "handoffs" / "current" / "operator" / "operator-current-gate-summary.json"
CANDIDATE_LEDGER_CONSUMER_SUMMARY = REPO_ROOT / "handoffs" / "current" / "candidate-ledger-consumer" / "candidate-ledger-consumer-summary.json"
CANDIDATE_LEDGER_CONSUMER_REPORT = REPO_ROOT / "handoffs" / "current" / "candidate-ledger-consumer" / "CANDIDATE_LEDGER_CONSUMER_REPORT.md"
AGENTS_CONTRACT = REPO_ROOT / "AGENTS.md"
AGENT_WORKFLOW_DOC = REPO_ROOT / "docs" / "agent-execution-workflow.md"
DISCOVERY_LEDGER_DOC = REPO_ROOT / "docs" / "discovery-ledger-workflow.md"
AI_WORKFLOW_PACKET_SCHEMA_DOC = REPO_ROOT / "docs" / "ai-workflow-packet-schema.md"


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


def append_log(event: str, **fields: Any) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with LOG.open("a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps({"created_utc": utc(), "event": event, **fields}, sort_keys=True) + "\n")


def load_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
        if isinstance(data, dict):
            return data
        return {"_read_success": False, "_read_error": "json_root_not_object", "_path": rel(path)}
    except FileNotFoundError:
        return {"_read_success": False, "_read_error": "file_not_found", "_path": rel(path)}
    except Exception as exc:  # noqa: BLE001 - artifact packets must preserve exact diagnostics.
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def safe_history_token(value: Any) -> str:
    text = str(value or "unknown")
    safe = "".join(ch if ch.isalnum() or ch in ("-", "_") else "-" for ch in text).strip("-")
    return safe[:96] or "unknown"


def unique_path(path: Path) -> Path:
    if not path.exists():
        return path

    for index in range(2, 1000):
        candidate = path.with_name(f"{path.stem}-{index}{path.suffix}")
        if not candidate.exists():
            return candidate
    raise RuntimeError(f"unable to create unique archive path for {path}")


def archive_existing_outputs(previous_packet: dict[str, Any] | None) -> dict[str, Any]:
    if not SUMMARY.exists():
        return {
            "status": "NO_PREVIOUS_PACKET",
            "history_dir": rel(HISTORY_DIR),
            "history_index": rel(HISTORY_INDEX),
            "artifacts": {},
        }

    packet = previous_packet if isinstance(previous_packet, dict) else {}
    stamp = safe_history_token(packet.get("created_utc") or utc())
    version = safe_history_token(packet.get("app_version") or "unknown-version")
    archive_stem = f"{stamp}-{version}"

    HISTORY_DIR.mkdir(parents=True, exist_ok=True)
    artifacts: dict[str, str] = {}

    summary_archive = unique_path(HISTORY_DIR / f"ai-workflow-summary-{archive_stem}.json")
    shutil.copy2(SUMMARY, summary_archive)
    artifacts["summary"] = rel(summary_archive)

    if REPORT.exists():
        report_archive = unique_path(HISTORY_DIR / f"AI_WORKFLOW_PACKET-{archive_stem}.md")
        shutil.copy2(REPORT, report_archive)
        artifacts["report"] = rel(report_archive)

    return {
        "status": "ARCHIVED",
        "history_dir": rel(HISTORY_DIR),
        "history_index": rel(HISTORY_INDEX),
        "archive_stem": archive_stem,
        "source_created_utc": packet.get("created_utc"),
        "source_app_version": packet.get("app_version"),
        "artifacts": artifacts,
    }


def build_history_index_entry(archive: dict[str, Any], *, indexed_utc: str | None = None) -> dict[str, Any]:
    return {
        "schema_version": "riftscan.ai_workflow_packet_history_index.v1",
        "indexed_utc": indexed_utc or utc(),
        "archive_stem": archive.get("archive_stem"),
        "source_created_utc": archive.get("source_created_utc"),
        "source_app_version": archive.get("source_app_version"),
        "artifacts": archive.get("artifacts") if isinstance(archive.get("artifacts"), dict) else {},
    }


def append_history_index(archive: dict[str, Any]) -> dict[str, Any]:
    if archive.get("status") != "ARCHIVED":
        return {
            "status": "SKIPPED",
            "path": rel(HISTORY_INDEX),
            "reason": str(archive.get("status") or "not_archived"),
        }

    HISTORY_DIR.mkdir(parents=True, exist_ok=True)
    entry = build_history_index_entry(archive)
    with HISTORY_INDEX.open("a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(entry, sort_keys=True) + "\n")

    return {
        "status": "APPENDED",
        "path": rel(HISTORY_INDEX),
        "entry": entry,
    }


def run_git(args: list[str], timeout_seconds: int = 15) -> dict[str, Any]:
    try:
        p = subprocess.run(["git", *args], cwd=REPO_ROOT, text=True, capture_output=True, timeout=timeout_seconds, check=False)
        return {
            "args": ["git", *args],
            "exit_code": p.returncode,
            "status": "pass" if p.returncode == 0 else "fail",
            "stdout": p.stdout,
            "stderr": p.stderr,
        }
    except subprocess.TimeoutExpired as exc:
        return {
            "args": ["git", *args],
            "exit_code": None,
            "status": "fail",
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or "",
            "error": f"timed out after {timeout_seconds} seconds",
        }


def git_snapshot() -> dict[str, Any]:
    head = run_git(["rev-parse", "HEAD"])
    status = run_git(["status", "--short", "--branch"])
    recent = run_git(["log", "--oneline", "-5"])
    parity = run_git(["rev-list", "--left-right", "--count", "origin/main...main"])
    return {
        "head": head.get("stdout", "").strip() if head.get("exit_code") == 0 else None,
        "status_short_branch": status.get("stdout", "").strip() if status.get("exit_code") == 0 else status.get("stderr", ""),
        "log_oneline_5": recent.get("stdout", "").strip() if recent.get("exit_code") == 0 else recent.get("stderr", ""),
        "origin_main_left_right_count": parity.get("stdout", "").strip() if parity.get("exit_code") == 0 else parity.get("stderr", ""),
        "command_status": {
            "head": head.get("status"),
            "status": status.get("status"),
            "log": recent.get("status"),
            "origin_main_parity": parity.get("status"),
        },
    }


def get_nested(root: dict[str, Any], *keys: str) -> Any:
    current: Any = root
    for key in keys:
        if not isinstance(current, dict):
            return None
        current = current.get(key)
    return current


def compact_candidate(candidate: dict[str, Any]) -> dict[str, Any]:
    return {
        "stable_id": candidate.get("stable_id"),
        "candidate_id": candidate.get("candidate_id"),
        "kind": candidate.get("kind"),
        "state": candidate.get("state"),
        "claim_level": candidate.get("claim_level"),
        "proof_level": candidate.get("proof_level"),
        "source_absolute_address_hex": candidate.get("source_absolute_address_hex"),
        "source_base_address_hex": candidate.get("source_base_address_hex"),
        "source_offset_hex": candidate.get("source_offset_hex"),
        "axis_order": candidate.get("axis_order"),
        "support_count": candidate.get("support_count"),
        "best_max_abs_distance": candidate.get("best_max_abs_distance"),
        "riftreader_status": candidate.get("riftreader_status"),
        "next_validation_step": candidate.get("next_validation_step"),
        "ledger_live_movement_authorized": candidate.get("ledger_live_movement_authorized"),
    }


def artifact_failed(artifact: dict[str, Any]) -> bool:
    return artifact.get("_read_success") is False


def selected_packet_value(packet: dict[str, Any], path: tuple[str, ...]) -> Any:
    if path == ("blocker_count",):
        blockers = packet.get("blockers")
        return len(blockers) if isinstance(blockers, list) else None
    if path == ("warning_count",):
        warnings = packet.get("warnings")
        return len(warnings) if isinstance(warnings, list) else None
    return get_nested(packet, *path)


PACKET_DIFF_FIELDS: list[tuple[str, tuple[str, ...]]] = [
    ("app_version", ("app_version",)),
    ("status", ("status",)),
    ("blocker_count", ("blocker_count",)),
    ("warning_count", ("warning_count",)),
    ("current_best_stable_id", ("current_best_candidate", "stable_id")),
    ("current_best_address", ("current_best_candidate", "source_absolute_address_hex")),
    ("candidate_consumer_status", ("candidate_ledger_consumer", "status")),
    ("safe_candidate_count", ("candidate_ledger_consumer", "safe_candidate_count")),
    ("rejected_candidate_count", ("candidate_ledger_consumer", "rejected_candidate_count")),
    ("artifact_stale_count", ("candidate_ledger_consumer", "artifact_age", "stale_count")),
    ("artifact_missing_count", ("candidate_ledger_consumer", "artifact_age", "missing_count")),
    ("current_best_stale_count", ("candidate_ledger_consumer", "artifact_age", "current_best_stale_count")),
    ("current_best_missing_count", ("candidate_ledger_consumer", "artifact_age", "current_best_missing_count")),
    ("discovery_ledger_contract_status", ("discovery_ledger", "candidate_ledger_contract_validation", "status")),
    ("offline_workflow_status", ("offline_workflow_check", "status")),
    ("operator_live_collection_allowed", ("operator_gate", "live_collection_allowed")),
]


def packet_diff_compared_fields() -> list[dict[str, str]]:
    return [
        {
            "field": name,
            "packet_path": ".".join(path),
        }
        for name, path in PACKET_DIFF_FIELDS
    ]


def build_previous_packet_diff(previous: dict[str, Any], current: dict[str, Any]) -> dict[str, Any]:
    if artifact_failed(previous):
        return {
            "schema_version": "riftscan.ai_workflow_packet_diff.v1",
            "status": "NO_PREVIOUS_PACKET",
            "previous_packet_available": False,
            "previous_read_error": previous.get("_read_error"),
            "change_count": 0,
            "changes": [],
        }

    changes: list[dict[str, Any]] = []
    for name, path in PACKET_DIFF_FIELDS:
        before = selected_packet_value(previous, path)
        after = selected_packet_value(current, path)
        if before != after:
            changes.append(
                {
                    "field": name,
                    "before": before,
                    "after": after,
                }
            )

    return {
        "schema_version": "riftscan.ai_workflow_packet_diff.v1",
        "status": "CHANGED" if changes else "UNCHANGED",
        "previous_packet_available": True,
        "previous_created_utc": previous.get("created_utc"),
        "previous_app_version": previous.get("app_version"),
        "current_created_utc": current.get("created_utc"),
        "current_app_version": current.get("app_version"),
        "change_count": len(changes),
        "changes": changes,
    }


def build_packet_from_artifacts(
    discovery: dict[str, Any],
    consumer: dict[str, Any],
    offline: dict[str, Any],
    operator: dict[str, Any],
    git: dict[str, Any],
    *,
    created_utc: str | None = None,
) -> dict[str, Any]:
    blockers: list[str] = []
    warnings: list[str] = []

    if artifact_failed(discovery):
        blockers.append(f"Discovery Ledger summary unavailable: {discovery.get('_read_error')}")
    if artifact_failed(consumer):
        warnings.append(f"Candidate Ledger Consumer summary unavailable: {consumer.get('_read_error')}")
    if artifact_failed(offline):
        warnings.append(f"Offline Workflow Check summary unavailable: {offline.get('_read_error')}")
    if artifact_failed(operator):
        warnings.append(f"Operator gate summary unavailable: {operator.get('_read_error')}")

    contract = discovery.get("candidate_ledger_contract_validation") if isinstance(discovery.get("candidate_ledger_contract_validation"), dict) else {}
    if discovery and not artifact_failed(discovery):
        if contract.get("status") != "PASS":
            blockers.append("Discovery Ledger candidate-ledger contract validation is not PASS.")
        if get_nested(discovery, "safety", "ledger_live_movement_authorized") is not False:
            blockers.append("Discovery Ledger safety flag ledger_live_movement_authorized is not false.")
        if get_nested(discovery, "safety", "movement_or_input_sent") is not False:
            blockers.append("Discovery Ledger safety flag movement_or_input_sent is not false.")

    if offline and not artifact_failed(offline) and offline.get("status") != "pass":
        warnings.append("Offline Workflow Check is not PASS; inspect failed_checks before continuing.")

    if consumer and not artifact_failed(consumer):
        if consumer.get("status") != "PASS":
            blockers.append("Candidate Ledger Consumer is not PASS.")
        if get_nested(consumer, "safety", "live_action_authorized") is not False:
            blockers.append("Candidate Ledger Consumer safety flag live_action_authorized is not false.")
        consumer_warnings = consumer.get("warnings") if isinstance(consumer.get("warnings"), list) else []
        warnings.extend(f"Candidate Ledger Consumer: {item}" for item in consumer_warnings)
        if get_nested(consumer, "artifact_age", "current_best_missing_count"):
            warnings.append("Candidate Ledger Consumer reports missing source artifact(s) for the current best candidate.")
        if get_nested(consumer, "artifact_age", "current_best_stale_count"):
            warnings.append("Candidate Ledger Consumer reports stale source artifact(s) for the current best candidate.")

    live_collection_allowed = operator.get("live_collection_allowed") if isinstance(operator, dict) else None
    if live_collection_allowed is not False:
        warnings.append("Operator live_collection_allowed is not confirmed false in the current summary; this AI packet still authorizes offline work only.")

    consumer_best = consumer.get("current_best_candidate") if isinstance(consumer.get("current_best_candidate"), dict) else {}
    best_raw = discovery.get("current_best_candidate") if isinstance(discovery.get("current_best_candidate"), dict) else {}
    best = dict(consumer_best) if consumer_best else (compact_candidate(best_raw) if best_raw else {})
    if best and best.get("live_use_authorized", best.get("ledger_live_movement_authorized")) is not False:
        blockers.append("Current best candidate does not preserve offline-only live-use safety.")

    recommended_files = [
        rel(README_CURRENT),
        rel(REPORT),
        rel(SUMMARY),
        rel(CANDIDATE_LEDGER_CONSUMER_REPORT),
        rel(CANDIDATE_LEDGER_CONSUMER_SUMMARY),
        rel(DISCOVERY_LEDGER_REPORT),
        rel(DISCOVERY_LEDGER_SUMMARY),
        rel(DISCOVERY_LEDGER_DOC),
        rel(AI_WORKFLOW_PACKET_SCHEMA_DOC),
        rel(AGENTS_CONTRACT),
        rel(AGENT_WORKFLOW_DOC),
    ]

    top_next_actions = [
        "Keep work offline while RiftReader or the operator owns the game window.",
        "Start from this AI Workflow Packet, then inspect the Candidate Ledger Consumer and Discovery Ledger reports.",
        "Use only stored artifacts, docs, schema checks, report generation, and deterministic tests.",
        "Do not start focus preflight, live capture, process attach, memory reads, movement/input, RiftReader commands, offset validation, or /reloadui.",
        "Treat the current best coordinate candidate as offline evidence only until a fresh exact PID/HWND proof readback is explicitly authorized.",
        "If adding a helper, follow the Python tool + thin CMD wrapper + Markdown/JSON/JSONL artifact pattern.",
        "Run py_compile, helper self-tests, Offline Workflow Check, JSON/JSONL validation, dotnet build/test/format, and git diff checks at the milestone boundary.",
        "Commit and push coherent offline workflow milestones only after validation passes.",
        "If any artifact conflicts, prefer the newest PASS machine-readable artifact and preserve older artifacts as historical evidence.",
        "Review Previous packet diff before deciding whether a new offline slice changed truth or only refreshed timestamps/logs.",
    ]

    packet_status = "PASS" if not blockers else "BLOCKED"
    return {
        "schema_version": "riftscan.ai_workflow_packet.v1",
        "created_utc": created_utc or utc(),
        "app_version": APP_VERSION,
        "status": packet_status,
        "display_status": packet_status,
        "mode": "offline_ai_workflow",
        "blockers": blockers,
        "warnings": warnings,
        "recommended_first_files": recommended_files,
        "current_best_candidate": best,
        "candidate_ledger_consumer": {
            "status": consumer.get("status"),
            "display_status": consumer.get("display_status"),
            "safe_candidate_count": consumer.get("safe_candidate_count"),
            "rejected_candidate_count": consumer.get("rejected_candidate_count"),
            "live_action_authorized": get_nested(consumer, "safety", "live_action_authorized"),
            "artifact_age": {
                "max_age_hours": get_nested(consumer, "artifact_age", "max_age_hours"),
                "checked_count": get_nested(consumer, "artifact_age", "checked_count"),
                "stale_count": get_nested(consumer, "artifact_age", "stale_count"),
                "missing_count": get_nested(consumer, "artifact_age", "missing_count"),
                "current_best_stale_count": get_nested(consumer, "artifact_age", "current_best_stale_count"),
                "current_best_missing_count": get_nested(consumer, "artifact_age", "current_best_missing_count"),
            },
        },
        "discovery_ledger": {
            "status": discovery.get("status"),
            "candidate_count": discovery.get("candidate_count"),
            "candidate_ledger_contract_validation": {
                "status": contract.get("status"),
                "candidate_count": contract.get("candidate_count"),
                "error_count": contract.get("error_count"),
                "warning_count": contract.get("warning_count"),
                "path": contract.get("path"),
            },
            "ledger_live_movement_authorized": get_nested(discovery, "safety", "ledger_live_movement_authorized"),
        },
        "offline_workflow_check": {
            "status": offline.get("status"),
            "display_status": offline.get("display_status"),
            "failed_check_count": offline.get("failed_check_count"),
            "failed_checks": offline.get("failed_checks"),
        },
        "operator_gate": {
            "status": operator.get("status"),
            "display_status": operator.get("display_status"),
            "live_collection_allowed": live_collection_allowed,
            "next_action": operator.get("next_action"),
        },
        "git": git,
        "previous_packet_diff_compared_fields": packet_diff_compared_fields(),
        "top_next_actions": top_next_actions,
        "ai_resume_prompt": (
            "Resume RiftScan in offline AI workflow mode. Read handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md, "
            "then the Candidate Ledger Consumer and Discovery Ledger artifacts. Do not touch live RIFT, focus preflight, capture, process attach/memory reads, "
            "movement/input, RiftReader commands, offset validation, or /reloadui unless the user explicitly authorizes live work."
        ),
        "source_artifacts": {
            "readme_current": rel(README_CURRENT),
            "discovery_ledger_report": rel(DISCOVERY_LEDGER_REPORT),
            "discovery_ledger_summary": rel(DISCOVERY_LEDGER_SUMMARY),
            "candidate_ledger_consumer_report": rel(CANDIDATE_LEDGER_CONSUMER_REPORT),
            "candidate_ledger_consumer_summary": rel(CANDIDATE_LEDGER_CONSUMER_SUMMARY),
            "offline_workflow_summary": rel(OFFLINE_WORKFLOW_SUMMARY),
            "operator_summary": rel(OPERATOR_SUMMARY),
            "agents_contract": rel(AGENTS_CONTRACT),
            "agent_workflow_doc": rel(AGENT_WORKFLOW_DOC),
            "discovery_ledger_doc": rel(DISCOVERY_LEDGER_DOC),
            "ai_workflow_packet_schema_doc": rel(AI_WORKFLOW_PACKET_SCHEMA_DOC),
        },
        "paths": {
            "report": rel(REPORT),
            "summary": rel(SUMMARY),
            "log": rel(LOG),
            "history_dir": rel(HISTORY_DIR),
            "history_index": rel(HISTORY_INDEX),
        },
        "safety": {
            "offline_only": True,
            "live_action_authorized": False,
            "focus_preflight_started": False,
            "live_capture_started": False,
            "process_attach_or_memory_read_started": False,
            "memory_scan_or_read_started": False,
            "movement_or_input_sent": False,
            "riftreader_command_executed": False,
            "offset_validation_started": False,
            "reloadui_sent": False,
        },
    }


def build_packet(previous_packet: dict[str, Any] | None = None) -> dict[str, Any]:
    append_log("build_start")
    data = build_packet_from_artifacts(
        load_json(DISCOVERY_LEDGER_SUMMARY),
        load_json(CANDIDATE_LEDGER_CONSUMER_SUMMARY),
        load_json(OFFLINE_WORKFLOW_SUMMARY),
        load_json(OPERATOR_SUMMARY),
        git_snapshot(),
    )
    data["previous_packet_diff"] = build_previous_packet_diff(previous_packet or {"_read_success": False, "_read_error": "previous_packet_not_loaded"}, data)
    append_log(
        "build_finish",
        status=data["status"],
        blocker_count=len(data["blockers"]),
        warning_count=len(data["warnings"]),
        previous_packet_diff_status=get_nested(data, "previous_packet_diff", "status"),
        previous_packet_change_count=get_nested(data, "previous_packet_diff", "change_count"),
    )
    return data


def report_lines(data: dict[str, Any]) -> list[str]:
    best = data.get("current_best_candidate") if isinstance(data.get("current_best_candidate"), dict) else {}
    lines = [
        "# RiftScan Offline AI Workflow Packet",
        "",
        f"Created UTC: `{data.get('created_utc')}`",
        f"App version: `{data.get('app_version')}`",
        "",
        "## Result",
        "",
        "```text",
        f"status: {data.get('status')}",
        f"mode: {data.get('mode')}",
        f"live_action_authorized: {get_nested(data, 'safety', 'live_action_authorized')}",
        f"discovery_ledger_contract: {get_nested(data, 'discovery_ledger', 'candidate_ledger_contract_validation', 'status')}",
        f"candidate_ledger_consumer: {get_nested(data, 'candidate_ledger_consumer', 'status')}",
        "```",
        "",
        "## Current best offline candidate",
        "",
    ]
    if best:
        lines.extend(
            [
                "| Field | Value |",
                "|---|---|",
                f"| Stable ID | `{best.get('stable_id')}` |",
                f"| State | `{best.get('state')}` |",
                f"| Claim level | `{best.get('claim_level')}` |",
                f"| Address | `{best.get('source_absolute_address_hex')}` |",
                f"| Base + offset | `{best.get('source_base_address_hex')}` + `{best.get('source_offset_hex')}` |",
                f"| Live use authorized | `{best.get('live_use_authorized', best.get('ledger_live_movement_authorized'))}` |",
                f"| Next validation | `{best.get('next_validation_step')}` |",
                "",
            ]
        )
    else:
        lines.extend(["No current best candidate is available from the Discovery Ledger summary.", ""])

    lines.extend(
        [
            "## Candidate ledger consumer",
            "",
            "```text",
            f"status: {get_nested(data, 'candidate_ledger_consumer', 'status')}",
            f"safe_candidate_count: {get_nested(data, 'candidate_ledger_consumer', 'safe_candidate_count')}",
            f"rejected_candidate_count: {get_nested(data, 'candidate_ledger_consumer', 'rejected_candidate_count')}",
            f"artifact_stale_count: {get_nested(data, 'candidate_ledger_consumer', 'artifact_age', 'stale_count')}",
            f"artifact_missing_count: {get_nested(data, 'candidate_ledger_consumer', 'artifact_age', 'missing_count')}",
            f"current_best_stale_count: {get_nested(data, 'candidate_ledger_consumer', 'artifact_age', 'current_best_stale_count')}",
            f"current_best_missing_count: {get_nested(data, 'candidate_ledger_consumer', 'artifact_age', 'current_best_missing_count')}",
            "```",
            "",
        ]
    )

    lines.extend(["## Blockers", ""])
    blockers = data.get("blockers") if isinstance(data.get("blockers"), list) else []
    if blockers:
        lines.extend(f"- {item}" for item in blockers)
    else:
        lines.append("- None for offline AI workflow.")

    packet_diff = data.get("previous_packet_diff") if isinstance(data.get("previous_packet_diff"), dict) else {}
    lines.extend(
        [
            "",
            "## Previous packet diff",
            "",
            "```text",
            f"status: {packet_diff.get('status', 'not_run')}",
            f"previous_created_utc: {packet_diff.get('previous_created_utc')}",
            f"previous_app_version: {packet_diff.get('previous_app_version')}",
            f"change_count: {packet_diff.get('change_count', 'unknown')}",
            "```",
        ]
    )
    changes = packet_diff.get("changes") if isinstance(packet_diff.get("changes"), list) else []
    if changes:
        lines.extend(
            [
                "",
                "| Field | Before | After |",
                "|---|---|---|",
            ]
        )
        for change in changes[:20]:
            lines.append(
                f"| `{change.get('field')}` | `{change.get('before')}` | `{change.get('after')}` |"
            )
        if len(changes) > 20:
            lines.append(f"| ... | {len(changes) - 20} more change(s) omitted from Markdown table | see JSON summary |")
    lines.extend(
        [
            "",
            f"Compared fields are listed in JSON at `previous_packet_diff_compared_fields` and documented in `{rel(AI_WORKFLOW_PACKET_SCHEMA_DOC)}`.",
        ]
    )

    archive = data.get("previous_packet_archive") if isinstance(data.get("previous_packet_archive"), dict) else {}
    if archive:
        lines.extend(
            [
                "",
                "## Previous packet archive",
                "",
                "```json",
                json.dumps(archive, indent=2, sort_keys=True),
                "```",
            ]
        )

    history_index = data.get("packet_history_index") if isinstance(data.get("packet_history_index"), dict) else {}
    if history_index:
        lines.extend(
            [
                "",
                "## Packet history index",
                "",
                "```json",
                json.dumps(history_index, indent=2, sort_keys=True),
                "```",
            ]
        )

    lines.extend(["", "## Warnings", ""])
    warnings = data.get("warnings") if isinstance(data.get("warnings"), list) else []
    if warnings:
        lines.extend(f"- {item}" for item in warnings)
    else:
        lines.append("- None.")

    lines.extend(["", "## Recommended first files", ""])
    for path in data.get("recommended_first_files", []):
        lines.append(f"- `{path}`")

    lines.extend(["", "## Top next actions", ""])
    for index, action in enumerate(data.get("top_next_actions", []), start=1):
        lines.append(f"{index}. {action}")

    lines.extend(
        [
            "",
            "## AI resume prompt",
            "",
            "```text",
            str(data.get("ai_resume_prompt") or ""),
            "```",
            "",
            "## Safety",
            "",
            "```json",
            json.dumps(data.get("safety", {}), indent=2, sort_keys=True),
            "```",
            "",
            "## Source artifacts",
            "",
            "```json",
            json.dumps(data.get("source_artifacts", {}), indent=2, sort_keys=True),
            "```",
            "",
        ]
    )
    return lines


def write_outputs(data: dict[str, Any], previous_packet: dict[str, Any] | None = None) -> None:
    archive = archive_existing_outputs(previous_packet)
    history_index = append_history_index(archive)
    data["previous_packet_archive"] = archive
    data["packet_history_index"] = history_index
    write_json(SUMMARY, data)
    write_text(REPORT, "\n".join(report_lines(data)))
    append_log(
        "outputs_written",
        report=rel(REPORT),
        summary=rel(SUMMARY),
        status=data["status"],
        previous_packet_archive_status=archive.get("status"),
        packet_history_index_status=history_index.get("status"),
        packet_history_index_path=history_index.get("path"),
    )


def format_diff_value(value: Any) -> str:
    if value is None:
        return "null"
    if isinstance(value, bool):
        return str(value).lower()
    if isinstance(value, (int, float)):
        return str(value)
    return json.dumps(value, sort_keys=True)


def packet_diff_lines(data: dict[str, Any], *, source_label: str = "refreshed") -> list[str]:
    packet_diff = data.get("previous_packet_diff") if isinstance(data.get("previous_packet_diff"), dict) else {}
    compared_fields = data.get("previous_packet_diff_compared_fields")
    changes = packet_diff.get("changes") if isinstance(packet_diff.get("changes"), list) else []
    archive = data.get("previous_packet_archive") if isinstance(data.get("previous_packet_archive"), dict) else {}
    lines = [
        "RIFTSCAN AI WORKFLOW PACKET DIFF",
        f"source: {source_label}",
        f"status: {packet_diff.get('status', 'not_run')}",
        f"change_count: {packet_diff.get('change_count', 'unknown')}",
        f"previous_created_utc: {packet_diff.get('previous_created_utc')}",
        f"current_created_utc: {packet_diff.get('current_created_utc')}",
        f"previous_app_version: {packet_diff.get('previous_app_version')}",
        f"current_app_version: {packet_diff.get('current_app_version')}",
        f"compared_field_count: {len(compared_fields) if isinstance(compared_fields, list) else 'unknown'}",
        f"schema_doc: {rel(AI_WORKFLOW_PACKET_SCHEMA_DOC)}",
        f"summary: {rel(SUMMARY)}",
        f"report: {rel(REPORT)}",
        f"previous_packet_archive_status: {archive.get('status', 'unknown')}",
        f"history_dir: {archive.get('history_dir', rel(HISTORY_DIR))}",
        f"history_index: {archive.get('history_index', rel(HISTORY_INDEX))}",
        "changes:",
    ]
    if changes:
        for change in changes:
            lines.append(
                f"- {change.get('field')}: {format_diff_value(change.get('before'))} -> {format_diff_value(change.get('after'))}"
            )
    else:
        lines.append("- none")
    lines.append("Safety: offline packet only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.")
    return lines


def run_self_test() -> int:
    fake_discovery = {
        "status": "ledger_written",
        "candidate_count": 1,
        "safety": {
            "ledger_live_movement_authorized": False,
            "movement_or_input_sent": False,
        },
        "candidate_ledger_contract_validation": {
            "status": "PASS",
            "candidate_count": 1,
            "error_count": 0,
            "warning_count": 0,
            "path": "candidate_ledger.jsonl",
        },
        "current_best_candidate": {
            "stable_id": "coordinate::fixture::0x1020",
            "candidate_id": "fixture",
            "kind": "coordinate_vec3",
            "state": "validated_candidate_historical_checkpoint",
            "claim_level": "validated_candidate",
            "proof_level": "fixture",
            "source_absolute_address_hex": "0x1020",
            "source_base_address_hex": "0x1000",
            "source_offset_hex": "0x20",
            "axis_order": "xyz",
            "support_count": 3,
            "best_max_abs_distance": 0,
            "next_validation_step": "rerun exact current PID/HWND proof readback before any live input",
            "ledger_live_movement_authorized": False,
        },
    }
    fake_offline = {"status": "pass", "display_status": "PASS", "failed_check_count": 0, "failed_checks": []}
    fake_consumer = {
        "status": "PASS",
        "display_status": "PASS",
        "safe_candidate_count": 1,
        "rejected_candidate_count": 0,
        "safety": {"live_action_authorized": False},
        "artifact_age": {
            "max_age_hours": 24.0,
            "checked_count": 1,
            "stale_count": 0,
            "missing_count": 0,
            "current_best_stale_count": 0,
            "current_best_missing_count": 0,
        },
    }
    fake_operator = {"status": "PASS", "display_status": "PASS", "live_collection_allowed": False}
    fake_git = {"head": "fixture", "status_short_branch": "## main...origin/main", "origin_main_left_right_count": "0\t0"}

    passing = build_packet_from_artifacts(fake_discovery, fake_consumer, fake_offline, fake_operator, fake_git, created_utc="2026-01-01T00:00:00Z")
    bad_discovery = dict(fake_discovery)
    bad_discovery["safety"] = {"ledger_live_movement_authorized": True, "movement_or_input_sent": False}
    blocked = build_packet_from_artifacts(bad_discovery, fake_consumer, fake_offline, fake_operator, fake_git, created_utc="2026-01-01T00:00:00Z")

    failures: list[str] = []
    if passing.get("status") != "PASS":
        failures.append("valid_fixture_did_not_pass")
    if get_nested(passing, "safety", "live_action_authorized") is not False:
        failures.append("packet_must_not_authorize_live_action")
    if blocked.get("status") != "BLOCKED":
        failures.append("bad_ledger_safety_did_not_block")
    if "AI_WORKFLOW_PACKET.md" not in "\n".join(report_lines(passing)):
        failures.append("report_missing_packet_reference")
    unchanged_diff = build_previous_packet_diff(passing, dict(passing))
    if unchanged_diff.get("status") != "UNCHANGED":
        failures.append("unchanged_packet_diff_not_detected")
    changed_packet = json.loads(json.dumps(passing))
    changed_packet["candidate_ledger_consumer"]["safe_candidate_count"] = 2
    changed_diff = build_previous_packet_diff(passing, changed_packet)
    if changed_diff.get("status") != "CHANGED" or changed_diff.get("change_count") != 1:
        failures.append("changed_packet_diff_not_detected")
    changed_packet["previous_packet_diff"] = changed_diff
    diff_text = "\n".join(packet_diff_lines(changed_packet))
    if "safe_candidate_count" not in diff_text or "1 -> 2" not in diff_text:
        failures.append("print_diff_changed_field_missing")
    passing["previous_packet_diff"] = unchanged_diff
    passing["previous_packet_archive"] = {"status": "NO_PREVIOUS_PACKET", "history_dir": rel(HISTORY_DIR), "history_index": rel(HISTORY_INDEX), "artifacts": {}}
    saved_diff_text = "\n".join(packet_diff_lines(passing, source_label="saved_existing_no_refresh"))
    if "- none" not in saved_diff_text:
        failures.append("print_diff_unchanged_missing_none")
    if "source: saved_existing_no_refresh" not in saved_diff_text:
        failures.append("show_existing_diff_source_missing")
    history_entry = build_history_index_entry(
        {
            "archive_stem": "fixture-stem",
            "source_created_utc": "2026-01-01T00:00:00Z",
            "source_app_version": "fixture-version",
            "artifacts": {"summary": "handoffs/current/ai-workflow/history/summary.json", "report": "handoffs/current/ai-workflow/history/report.md"},
        },
        indexed_utc="2026-01-01T00:00:01Z",
    )
    if history_entry.get("schema_version") != "riftscan.ai_workflow_packet_history_index.v1":
        failures.append("history_index_entry_schema_missing")
    if get_nested(passing, "paths", "history_index") != rel(HISTORY_INDEX):
        failures.append("packet_paths_missing_history_index")
    token = safe_history_token("2026-05-07T17:32:05Z")
    if ":" in token or token != "2026-05-07T17-32-05Z":
        failures.append("history_token_not_windows_safe")
    compared_fields = passing.get("previous_packet_diff_compared_fields")
    if not isinstance(compared_fields, list) or len(compared_fields) != len(PACKET_DIFF_FIELDS):
        failures.append("packet_diff_compared_fields_missing_or_wrong_count")
    elif "safe_candidate_count" not in {item.get("field") for item in compared_fields if isinstance(item, dict)}:
        failures.append("packet_diff_compared_fields_missing_safe_candidate_count")

    result = {
        "schema_version": "riftscan.ai_workflow_packet.self_test.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if not failures else "FAIL",
        "failures": failures,
        "safety": {
            "writes_artifacts": False,
            "runs_focus_preflight": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "process_attach_or_memory_read_started": False,
            "riftreader_command_executed": False,
            "reloadui_sent": False,
        },
    }
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0 if not failures else 1


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build a compact offline AI workflow packet from current RiftScan artifacts.")
    parser.add_argument("--self-test", action="store_true", help="Run offline self-test only; writes no artifacts.")
    parser.add_argument("--print-summary", action="store_true", help="Print the generated packet summary after writing artifacts.")
    parser.add_argument("--print-diff", action="store_true", help="Print the previous-packet diff after writing artifacts.")
    parser.add_argument("--show-existing-diff", action="store_true", help="Print the saved packet diff without refreshing artifacts or writing logs.")
    parser.add_argument("--strict-exit-code", action="store_true", help="Return nonzero when the packet status is not PASS.")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    if args.self_test:
        return run_self_test()

    if args.show_existing_diff:
        saved_packet = load_json(SUMMARY)
        if artifact_failed(saved_packet):
            print(f"Unable to read saved AI workflow packet: {saved_packet.get('_read_error')} ({saved_packet.get('_path')})", file=sys.stderr)
            return 1
        print("\n".join(packet_diff_lines(saved_packet, source_label="saved_existing_no_refresh")))
        return 1 if args.strict_exit_code and saved_packet.get("status") != "PASS" else 0

    previous_packet = load_json(SUMMARY)
    packet = build_packet(previous_packet)
    write_outputs(packet, previous_packet)

    if args.print_summary:
        print(json.dumps(packet, indent=2, sort_keys=True))
    elif args.print_diff:
        print("\n".join(packet_diff_lines(packet)))
    else:
        print(f"RIFTSCAN AI WORKFLOW PACKET: {rel(REPORT)}")
        print(f"Summary: {rel(SUMMARY)}")
        print(f"Status: {packet['status']}")
        print("Safety: offline packet only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.")
    return 1 if args.strict_exit_code and packet["status"] != "PASS" else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# End of script: riftscan_ai_workflow_packet.py
