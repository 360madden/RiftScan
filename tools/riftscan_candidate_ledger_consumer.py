#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-candidate-ledger-consumer-v1.1.1
# Total character count: 000000
# Purpose: Build a safe offline-only consumer view of the Discovery Ledger candidate_ledger.jsonl artifact.
# Safety boundary: Reads existing JSON/JSONL artifacts only. No focus preflight, live capture, input, movement, memory scan/read, process attach, offset validation, RiftReader command execution, or /reloadui.

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from riftscan_discovery_ledger import validate_candidate_ledger  # noqa: E402

APP_VERSION = "riftscan-candidate-ledger-consumer-v1.1.1"
REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "candidate-ledger-consumer"
REPORT = OUT_DIR / "CANDIDATE_LEDGER_CONSUMER_REPORT.md"
SUMMARY = OUT_DIR / "candidate-ledger-consumer-summary.json"
LOG = OUT_DIR / "candidate-ledger-consumer-log.jsonl"
LOG_ENABLED = True
DISCOVERY_LEDGER_DIR = REPO_ROOT / "handoffs" / "current" / "discovery-ledger"
DISCOVERY_SUMMARY = DISCOVERY_LEDGER_DIR / "discovery-ledger-summary.json"
CANDIDATE_LEDGER = DISCOVERY_LEDGER_DIR / "candidate_ledger.jsonl"
DEFAULT_MAX_ARTIFACT_AGE_HOURS = 24.0

ALLOWED_OFFLINE_STATES = {
    "candidate",
    "validated_candidate_historical_checkpoint",
    "historical_stale_trace_blocked",
    "historical_candidate_scan_only",
}


def utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return str(path)


def resolve_artifact_path(path_text: str) -> Path:
    path = Path(path_text)
    return path if path.is_absolute() else REPO_ROOT / path


def iso_from_timestamp(timestamp: float) -> str:
    return datetime.fromtimestamp(timestamp, timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, data: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(data, encoding="utf-8", newline="\n")


def append_log(event: str, **fields: Any) -> None:
    if not LOG_ENABLED:
        return
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
    except Exception as exc:  # noqa: BLE001 - preserve exact artifact diagnostics.
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def load_jsonl_objects(path: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    rows: list[dict[str, Any]] = []
    issues: list[dict[str, Any]] = []
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    except FileNotFoundError:
        return [], [{"severity": "error", "code": "candidate_ledger_missing", "message": "candidate_ledger.jsonl does not exist."}]

    for line_number, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            parsed = json.loads(line)
        except json.JSONDecodeError as exc:
            issues.append({"severity": "error", "code": "jsonl_parse_error", "line": line_number, "message": str(exc)})
            continue
        if not isinstance(parsed, dict):
            issues.append({"severity": "error", "code": "jsonl_root_must_be_object", "line": line_number, "message": "JSONL row is not an object."})
            continue
        rows.append(parsed)
    return rows, issues


def compact_candidate(entry: dict[str, Any], source_index: int) -> dict[str, Any]:
    return {
        "source_index": source_index,
        "stable_id": entry.get("stable_id"),
        "candidate_id": entry.get("candidate_id"),
        "kind": entry.get("kind"),
        "state": entry.get("state"),
        "claim_level": entry.get("claim_level"),
        "proof_level": entry.get("proof_level"),
        "source": entry.get("source"),
        "source_absolute_address_hex": entry.get("source_absolute_address_hex"),
        "source_base_address_hex": entry.get("source_base_address_hex"),
        "source_offset_hex": entry.get("source_offset_hex"),
        "axis_order": entry.get("axis_order"),
        "support_count": entry.get("support_count"),
        "best_max_abs_distance": entry.get("best_max_abs_distance"),
        "next_validation_step": entry.get("next_validation_step"),
        "source_artifacts": entry.get("source_artifacts") if isinstance(entry.get("source_artifacts"), list) else [],
        "consumer_status": "available_offline_only",
        "live_use_authorized": False,
        "allowed_downstream_uses": [
            "offline_review",
            "report_generation",
            "replay_analysis_seed",
            "future_validation_planning",
        ],
        "forbidden_downstream_uses": [
            "movement",
            "input",
            "live_capture",
            "process_attach",
            "memory_read",
            "offset_validation",
            "riftreader_command",
            "reloadui",
        ],
    }


def row_rejection_reasons(entry: dict[str, Any]) -> list[str]:
    reasons: list[str] = []
    if entry.get("ledger_live_movement_authorized") is not False:
        reasons.append("ledger_live_movement_authorized_is_not_false")
    if entry.get("state") not in ALLOWED_OFFLINE_STATES:
        reasons.append("unknown_state")
    if not entry.get("stable_id"):
        reasons.append("missing_stable_id")
    if not entry.get("next_validation_step"):
        reasons.append("missing_next_validation_step")
    artifacts = entry.get("source_artifacts")
    if not isinstance(artifacts, list) or not artifacts:
        reasons.append("missing_source_artifacts")
    return reasons


def artifact_age_record(path_text: str, now: datetime, max_age_hours: float) -> dict[str, Any]:
    resolved = resolve_artifact_path(path_text)
    record: dict[str, Any] = {
        "path": path_text,
        "resolved_path": str(resolved),
        "exists": resolved.exists(),
        "max_age_hours": max_age_hours,
    }
    if not resolved.exists():
        record.update(
            {
                "status": "missing",
                "age_hours": None,
                "mtime_utc": None,
                "stale": True,
            }
        )
        return record

    mtime = datetime.fromtimestamp(resolved.stat().st_mtime, timezone.utc)
    age_hours = max(0.0, (now - mtime).total_seconds() / 3600.0)
    stale = age_hours > max_age_hours
    record.update(
        {
            "status": "stale" if stale else "fresh",
            "mtime_utc": iso_from_timestamp(resolved.stat().st_mtime),
            "age_hours": round(age_hours, 3),
            "stale": stale,
        }
    )
    return record


def build_artifact_age_summary(
    safe_candidates: list[dict[str, Any]],
    current_best: dict[str, Any] | None,
    now: datetime,
    max_age_hours: float,
) -> dict[str, Any]:
    all_paths: list[str] = []
    for candidate in safe_candidates:
        for path in candidate.get("source_artifacts", []):
            if isinstance(path, str) and path.strip() and path not in all_paths:
                all_paths.append(path)

    current_paths: list[str] = []
    if isinstance(current_best, dict):
        for path in current_best.get("source_artifacts", []):
            if isinstance(path, str) and path.strip() and path not in current_paths:
                current_paths.append(path)

    records = [artifact_age_record(path, now, max_age_hours) for path in all_paths]
    current_records = [artifact_age_record(path, now, max_age_hours) for path in current_paths]
    stale_records = [record for record in records if record.get("stale")]
    missing_records = [record for record in records if record.get("status") == "missing"]
    current_stale = [record for record in current_records if record.get("stale")]
    current_missing = [record for record in current_records if record.get("status") == "missing"]
    return {
        "max_age_hours": max_age_hours,
        "checked_count": len(records),
        "stale_count": len(stale_records),
        "missing_count": len(missing_records),
        "current_best_checked_count": len(current_records),
        "current_best_stale_count": len(current_stale),
        "current_best_missing_count": len(current_missing),
        "records": records,
        "current_best_records": current_records,
    }


def build_consumer_view_from_data(
    discovery_summary: dict[str, Any],
    rows: list[dict[str, Any]],
    validation: dict[str, Any],
    parse_issues: list[dict[str, Any]] | None = None,
    *,
    max_artifact_age_hours: float = DEFAULT_MAX_ARTIFACT_AGE_HOURS,
    now: datetime | None = None,
    created_utc: str | None = None,
) -> dict[str, Any]:
    parse_issues = parse_issues or []
    blockers: list[str] = []
    warnings: list[str] = []
    safe_candidates: list[dict[str, Any]] = []
    rejected_candidates: list[dict[str, Any]] = []

    if discovery_summary.get("_read_success") is False:
        blockers.append(f"Discovery Ledger summary unavailable: {discovery_summary.get('_read_error')}")
    if validation.get("status") != "PASS":
        blockers.append("Candidate ledger contract validation is not PASS.")
    if parse_issues:
        blockers.append("Candidate ledger JSONL parse issues exist.")

    embedded_validation = (
        discovery_summary.get("candidate_ledger_contract_validation")
        if isinstance(discovery_summary.get("candidate_ledger_contract_validation"), dict)
        else {}
    )
    if embedded_validation and embedded_validation.get("status") != "PASS":
        blockers.append("Discovery Ledger embedded contract validation is not PASS.")

    if not rows:
        blockers.append("Candidate ledger has no rows to consume.")

    for index, entry in enumerate(rows, start=1):
        reasons = row_rejection_reasons(entry)
        if reasons:
            rejected_candidates.append(
                {
                    "source_index": index,
                    "stable_id": entry.get("stable_id"),
                    "state": entry.get("state"),
                    "reasons": reasons,
                }
            )
            continue
        safe_candidates.append(compact_candidate(entry, index))

    if rejected_candidates:
        blockers.append("One or more candidate rows were rejected by consumer safety rules.")

    current_best_stable_id = discovery_summary.get("current_best_candidate_stable_id")
    current_best = next((item for item in safe_candidates if item.get("stable_id") == current_best_stable_id), None)
    if current_best_stable_id and not current_best:
        warnings.append("Discovery Ledger current_best_candidate_stable_id is not present in the safe candidate set.")
    if not current_best and safe_candidates:
        current_best = safe_candidates[0]

    now_utc = now or datetime.now(timezone.utc)
    artifact_age = build_artifact_age_summary(safe_candidates, current_best, now_utc, max_artifact_age_hours)
    if artifact_age["current_best_missing_count"]:
        warnings.append("Current best candidate has missing source artifact paths; rerun offline ledger refresh before trusting it for planning.")
    if artifact_age["current_best_stale_count"]:
        warnings.append("Current best candidate has stale source artifact paths; require fresh proof before any live use.")
    historical_stale_count = artifact_age["stale_count"] - artifact_age["current_best_stale_count"]
    if historical_stale_count > 0:
        warnings.append(f"Historical/non-current candidate rows include {historical_stale_count} stale source artifact(s); keep them historical.")

    status = "PASS" if not blockers else "BLOCKED"
    return {
        "schema_version": "riftscan.candidate_ledger_consumer.v1",
        "created_utc": created_utc or utc(),
        "app_version": APP_VERSION,
        "status": status,
        "display_status": status,
        "mode": "offline_candidate_ledger_consumer",
        "blockers": blockers,
        "warnings": warnings,
        "candidate_ledger_contract_validation": {
            "status": validation.get("status"),
            "candidate_count": validation.get("candidate_count"),
            "error_count": validation.get("error_count"),
            "warning_count": validation.get("warning_count"),
            "path": validation.get("path"),
        },
        "safe_candidate_count": len(safe_candidates),
        "rejected_candidate_count": len(rejected_candidates),
        "current_best_candidate": current_best,
        "safe_candidates": safe_candidates,
        "rejected_candidates": rejected_candidates,
        "parse_issues": parse_issues,
        "artifact_age": artifact_age,
        "allowed_downstream_uses": [
            "offline_review",
            "report_generation",
            "replay_analysis_seed",
            "future_validation_planning",
        ],
        "forbidden_downstream_uses": [
            "movement",
            "input",
            "live_capture",
            "process_attach",
            "memory_read",
            "offset_validation",
            "riftreader_command",
            "reloadui",
        ],
        "source_artifacts": {
            "discovery_ledger_summary": rel(DISCOVERY_SUMMARY),
            "candidate_ledger": rel(CANDIDATE_LEDGER),
        },
        "paths": {
            "report": rel(REPORT),
            "summary": rel(SUMMARY),
            "log": rel(LOG),
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


def build_consumer_view(max_artifact_age_hours: float = DEFAULT_MAX_ARTIFACT_AGE_HOURS) -> dict[str, Any]:
    append_log("build_start")
    discovery_summary = load_json(DISCOVERY_SUMMARY)
    rows, parse_issues = load_jsonl_objects(CANDIDATE_LEDGER)
    validation = validate_candidate_ledger(CANDIDATE_LEDGER)
    result = build_consumer_view_from_data(
        discovery_summary,
        rows,
        validation,
        parse_issues,
        max_artifact_age_hours=max_artifact_age_hours,
    )
    append_log(
        "build_finish",
        status=result["status"],
        safe_candidate_count=result["safe_candidate_count"],
        rejected_candidate_count=result["rejected_candidate_count"],
    )
    return result


def report_lines(data: dict[str, Any]) -> list[str]:
    current = data.get("current_best_candidate") if isinstance(data.get("current_best_candidate"), dict) else {}
    lines = [
        "# RiftScan Candidate Ledger Consumer Report",
        "",
        f"Created UTC: `{data.get('created_utc')}`",
        f"App version: `{data.get('app_version')}`",
        "",
        "## Result",
        "",
        "```text",
        f"status: {data.get('status')}",
        f"mode: {data.get('mode')}",
        f"contract_validation: {data.get('candidate_ledger_contract_validation', {}).get('status')}",
        f"safe_candidate_count: {data.get('safe_candidate_count')}",
        f"rejected_candidate_count: {data.get('rejected_candidate_count')}",
        f"live_action_authorized: {data.get('safety', {}).get('live_action_authorized')}",
        "```",
        "",
        "## Artifact age",
        "",
        "```text",
        f"max_age_hours: {data.get('artifact_age', {}).get('max_age_hours')}",
        f"checked_count: {data.get('artifact_age', {}).get('checked_count')}",
        f"stale_count: {data.get('artifact_age', {}).get('stale_count')}",
        f"missing_count: {data.get('artifact_age', {}).get('missing_count')}",
        f"current_best_stale_count: {data.get('artifact_age', {}).get('current_best_stale_count')}",
        f"current_best_missing_count: {data.get('artifact_age', {}).get('current_best_missing_count')}",
        "```",
        "",
        "## Current best offline candidate",
        "",
    ]
    if current:
        lines.extend(
            [
                "| Field | Value |",
                "|---|---|",
                f"| Stable ID | `{current.get('stable_id')}` |",
                f"| State | `{current.get('state')}` |",
                f"| Address | `{current.get('source_absolute_address_hex')}` |",
                f"| Base + offset | `{current.get('source_base_address_hex')}` + `{current.get('source_offset_hex')}` |",
                f"| Live use authorized | `{current.get('live_use_authorized')}` |",
                f"| Next validation | `{current.get('next_validation_step')}` |",
                "",
            ]
        )
    else:
        lines.extend(["No safe current best candidate is available.", ""])

    lines.extend(["## Safe candidates", "", "| State | Candidate / kind | Address | Consumer status |", "|---|---|---|---|"])
    for candidate in data.get("safe_candidates", []):
        label = candidate.get("candidate_id") or candidate.get("kind")
        lines.append(f"| `{candidate.get('state')}` | `{label}` | `{candidate.get('source_absolute_address_hex') or '-'}` | `{candidate.get('consumer_status')}` |")
    if not data.get("safe_candidates"):
        lines.append("| none | - | - | - |")

    lines.extend(["", "## Blockers", ""])
    blockers = data.get("blockers") if isinstance(data.get("blockers"), list) else []
    lines.extend(f"- {item}" for item in blockers) if blockers else lines.append("- None.")

    lines.extend(["", "## Warnings", ""])
    warnings = data.get("warnings") if isinstance(data.get("warnings"), list) else []
    lines.extend(f"- {item}" for item in warnings) if warnings else lines.append("- None.")

    lines.extend(["", "## Forbidden downstream uses", ""])
    for item in data.get("forbidden_downstream_uses", []):
        lines.append(f"- {item}")

    lines.extend(
        [
            "",
            "## Safety",
            "",
            "```json",
            json.dumps(data.get("safety", {}), indent=2, sort_keys=True),
            "```",
            "",
        ]
    )
    return lines


def write_outputs(data: dict[str, Any]) -> None:
    write_json(SUMMARY, data)
    write_text(REPORT, "\n".join(report_lines(data)))
    append_log("outputs_written", report=rel(REPORT), summary=rel(SUMMARY), status=data["status"])


def run_self_test() -> int:
    fake_summary = {
        "current_best_candidate_stable_id": "coordinate::fixture::0x1020",
        "candidate_ledger_contract_validation": {"status": "PASS"},
    }
    fake_validation = {
        "status": "PASS",
        "candidate_count": 1,
        "error_count": 0,
        "warning_count": 0,
        "path": "fixture-candidate-ledger.jsonl",
    }
    good_row = {
        "stable_id": "coordinate::fixture::0x1020",
        "candidate_id": "fixture",
        "kind": "coordinate_vec3",
        "state": "validated_candidate_historical_checkpoint",
        "claim_level": "validated_candidate",
        "proof_level": "fixture",
        "source": "fixture",
        "source_absolute_address_hex": "0x1020",
        "source_base_address_hex": "0x1000",
        "source_offset_hex": "0x20",
        "axis_order": "xyz",
        "support_count": 3,
        "best_max_abs_distance": 0,
        "next_validation_step": "rerun exact current PID/HWND proof readback before live input",
        "ledger_live_movement_authorized": False,
        "source_artifacts": ["fixture.json"],
    }
    fake_now = datetime(2026, 1, 1, tzinfo=timezone.utc)
    good = build_consumer_view_from_data(
        fake_summary,
        [good_row],
        fake_validation,
        [],
        now=fake_now,
        created_utc="2026-01-01T00:00:00Z",
    )
    bad_row = dict(good_row)
    bad_row["ledger_live_movement_authorized"] = True
    bad = build_consumer_view_from_data(
        fake_summary,
        [bad_row],
        fake_validation,
        [],
        now=fake_now,
        created_utc="2026-01-01T00:00:00Z",
    )

    failures: list[str] = []
    if good.get("status") != "PASS":
        failures.append("good_fixture_did_not_pass")
    if good.get("safe_candidate_count") != 1:
        failures.append("good_fixture_safe_candidate_count_wrong")
    if good.get("current_best_candidate", {}).get("live_use_authorized") is not False:
        failures.append("consumer_must_not_authorize_live_use")
    if bad.get("status") != "BLOCKED":
        failures.append("bad_live_authorization_did_not_block")
    if not bad.get("rejected_candidates"):
        failures.append("bad_candidate_not_rejected")
    if good.get("artifact_age", {}).get("current_best_missing_count") != 1:
        failures.append("missing_fixture_source_artifact_not_reported")
    if not parse_args(["--check-only"]).check_only:
        failures.append("check_only_arg_not_parsed")
    if not parse_args(["--no-write"]).check_only:
        failures.append("no_write_alias_not_parsed")

    result = {
        "schema_version": "riftscan.candidate_ledger_consumer.self_test.v1",
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
    parser = argparse.ArgumentParser(description="Build a safe offline-only consumer view of the candidate ledger.")
    parser.add_argument("--self-test", action="store_true", help="Run offline self-test only; writes no artifacts.")
    parser.add_argument("--check-only", "--no-write", dest="check_only", action="store_true", help="Build and validate the consumer view without writing report, summary, or log artifacts.")
    parser.add_argument("--print-summary", action="store_true", help="Print the generated consumer summary.")
    parser.add_argument("--strict-exit-code", action="store_true", help="Return nonzero when the consumer status is not PASS.")
    parser.add_argument("--max-artifact-age-hours", type=float, default=DEFAULT_MAX_ARTIFACT_AGE_HOURS, help="Warn when source artifacts are older than this many hours.")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    global LOG_ENABLED

    args = parse_args(argv)
    if args.self_test:
        return run_self_test()

    LOG_ENABLED = not args.check_only
    data = build_consumer_view(max_artifact_age_hours=args.max_artifact_age_hours)
    if args.check_only:
        data["mode"] = "offline_candidate_ledger_consumer_check_only"
        data.setdefault("safety", {})["writes_artifacts"] = False
    else:
        write_outputs(data)

    if args.print_summary:
        print(json.dumps(data, indent=2, sort_keys=True))
    elif args.check_only:
        print(f"RIFTSCAN CANDIDATE LEDGER CONSUMER CHECK-ONLY: {data['status']}")
        print("No report, summary, or log artifacts were written.")
        print("Safety: offline consumer only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.")
    else:
        print(f"RIFTSCAN CANDIDATE LEDGER CONSUMER: {rel(REPORT)}")
        print(f"Summary: {rel(SUMMARY)}")
        print(f"Status: {data['status']}")
        print("Safety: offline consumer only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.")
    return 1 if args.strict_exit_code and data["status"] != "PASS" else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# End of script: riftscan_candidate_ledger_consumer.py
