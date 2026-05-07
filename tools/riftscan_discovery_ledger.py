#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-discovery-ledger-v1.1.0
# Total character count: 000000
# Purpose: Build and validate an offline, replayable discovery ledger from stored RiftScan/RiftReader artifacts.
# Safety boundary: Reads existing JSON/Markdown/session artifacts only. No RIFT focus preflight, live capture, input, movement, memory scan/read, process attach, offset validation, RiftReader command execution, or /reloadui.

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-discovery-ledger-v1.1.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RIFTREADER_ROOT = Path(r"C:\RIFT MODDING\RiftReader")
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "discovery-ledger"
SUMMARY = OUT_DIR / "discovery-ledger-summary.json"
REPORT = OUT_DIR / "DISCOVERY_LEDGER_REPORT.md"
CANDIDATE_LEDGER = OUT_DIR / "candidate_ledger.jsonl"
LOG = OUT_DIR / "discovery-ledger-log.jsonl"
LEDGER_SCHEMA_VERSION = "riftscan.discovery_ledger.v1"
LEDGER_VALIDATION_SCHEMA_VERSION = "riftscan.discovery_ledger_validation.v1"
ALLOWED_STATES = {
    "candidate",
    "validated_candidate_historical_checkpoint",
    "historical_stale_trace_blocked",
    "historical_candidate_scan_only",
}
ALLOWED_CLAIM_LEVELS = {"observed", "candidate", "validated_candidate"}
REQUIRED_COMMON_FIELDS = [
    "stable_id",
    "kind",
    "state",
    "claim_level",
    "proof_level",
    "source",
    "ledger_live_movement_authorized",
    "next_validation_step",
    "source_artifacts",
    "warnings",
]
REQUIRED_COORDINATE_FIELDS = [
    "candidate_id",
    "source_absolute_address_hex",
    "axis_order",
    "support_count",
    "best_max_abs_distance",
    "best_memory_xyz",
    "best_addon_xyz",
]

COORD_API_TRUTH_SUMMARY = REPO_ROOT / "handoffs" / "current" / "coord-api-truth" / "coord-api-truth-summary.json"
COORD_RECOVERY_SUMMARY = REPO_ROOT / "handoffs" / "current" / "coord-recovery" / "coord-recovery-summary.json"
MOVEMENT_EXECUTION_GATE_SUMMARY = REPO_ROOT / "handoffs" / "current" / "movement-execution-gate" / "movement-execution-gate-summary.json"


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


def load_json(path: Path | None) -> dict[str, Any]:
    if path is None:
        return {"_read_success": False, "_read_error": "path_not_provided"}
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
        if isinstance(data, dict):
            return data
        return {"_read_success": False, "_read_error": "json_root_not_object", "_path": rel(path)}
    except FileNotFoundError:
        return {"_read_success": False, "_read_error": "file_not_found", "_path": rel(path)}
    except Exception as exc:  # noqa: BLE001 - report artifact diagnostics must preserve exact exception.
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def newest_file(root: Path, pattern: str) -> Path | None:
    if not root.exists():
        return None
    files = [path for path in root.glob(pattern) if path.is_file()]
    if not files:
        return None
    return max(files, key=lambda path: path.stat().st_mtime)


def newest_recursive_file(root: Path, pattern: str) -> Path | None:
    if not root.exists():
        return None
    files = [path for path in root.rglob(pattern) if path.is_file()]
    if not files:
        return None
    return max(files, key=lambda path: path.stat().st_mtime)


def stable_id(*parts: Any) -> str:
    normalized = [str(part).strip().replace("\\", "/") for part in parts if part is not None and str(part).strip()]
    return "::".join(normalized)


def get_nested(root: dict[str, Any], *keys: str) -> Any:
    current: Any = root
    for key in keys:
        if not isinstance(current, dict):
            return None
        current = current.get(key)
    return current


def first_candidate(match_result: dict[str, Any]) -> dict[str, Any]:
    candidates = match_result.get("candidates")
    if isinstance(candidates, list) and candidates and isinstance(candidates[0], dict):
        return candidates[0]
    return {}


def candidate_from_match(
    match_path: Path | None,
    match_result: dict[str, Any],
    riftreader_pointer_path: Path | None,
    riftreader_pointer: dict[str, Any],
) -> dict[str, Any] | None:
    candidate = first_candidate(match_result)
    if not candidate:
        return None

    address = candidate.get("source_absolute_address_hex")
    pointer_candidate = riftreader_pointer.get("riftscanCandidateSource") if isinstance(riftreader_pointer.get("riftscanCandidateSource"), dict) else {}
    pointer_gate = riftreader_pointer.get("movementGate") if isinstance(riftreader_pointer.get("movementGate"), dict) else {}
    pointer_latest = riftreader_pointer.get("latestValidation") if isinstance(riftreader_pointer.get("latestValidation"), dict) else {}
    pointer_anchor = riftreader_pointer.get("proofAnchorCache") if isinstance(riftreader_pointer.get("proofAnchorCache"), dict) else {}
    pointer_address = pointer_candidate.get("sourceAbsoluteAddressHex") or pointer_anchor.get("candidateAddressHex")
    pointer_matches = bool(address and pointer_address and str(address).lower() == str(pointer_address).lower())

    proof_green = (
        pointer_matches
        and riftreader_pointer.get("status") is not None
        and pointer_gate.get("coordinateProofGateSatisfiedAtLatestValidation") is True
        and pointer_latest.get("status") == "valid"
        and pointer_latest.get("movementAllowed") is True
        and pointer_latest.get("noCheatEngine") is True
    )

    if proof_green:
        state = "validated_candidate_historical_checkpoint"
        proof_level = "riftscan_candidate_plus_riftreader_no_ce_multisample_and_post_readback"
        next_validation = "rerun exact current PID/HWND proof readback before any more live movement"
    else:
        state = "candidate"
        proof_level = "riftscan_addon_coordinate_match_candidate"
        next_validation = "pair with current RiftReader no-CE readback/promotion before movement"

    source_artifacts = [rel(match_path)] if match_path else []
    if riftreader_pointer_path:
        source_artifacts.append(str(riftreader_pointer_path))
    latest_summary = pointer_latest.get("summaryFile")
    if isinstance(latest_summary, str) and latest_summary.strip():
        source_artifacts.append(latest_summary)

    forward_series = riftreader_pointer.get("latestForwardSeries")
    latest_smoke = riftreader_pointer.get("latestForwardSmoke")
    forward_tests = riftreader_pointer.get("forwardMovementTests")

    return {
        "stable_id": stable_id("coordinate", candidate.get("candidate_id"), address),
        "candidate_id": candidate.get("candidate_id"),
        "kind": "coordinate_vec3",
        "state": state,
        "claim_level": "validated_candidate" if proof_green else "candidate",
        "proof_level": proof_level,
        "source": "riftscan_addon_coordinate_match",
        "source_session_id": match_result.get("session_id"),
        "source_session_path": match_result.get("session_path"),
        "source_region_id": candidate.get("source_region_id"),
        "source_base_address_hex": candidate.get("source_base_address_hex"),
        "source_offset_hex": candidate.get("source_offset_hex"),
        "source_absolute_address_hex": address,
        "axis_order": candidate.get("axis_order"),
        "support_count": candidate.get("support_count"),
        "observation_support_count": candidate.get("observation_support_count"),
        "best_max_abs_distance": candidate.get("best_max_abs_distance"),
        "best_memory_xyz": [
            candidate.get("best_memory_x"),
            candidate.get("best_memory_y"),
            candidate.get("best_memory_z"),
        ],
        "best_addon_xyz": [
            candidate.get("best_addon_x"),
            candidate.get("best_addon_y"),
            candidate.get("best_addon_z"),
        ],
        "riftscan_validation_status": candidate.get("validation_status"),
        "riftreader_pointer_matched_candidate": pointer_matches,
        "riftreader_status": riftreader_pointer.get("status"),
        "latest_validation": {
            "generated_at_utc": pointer_latest.get("generatedAtUtc"),
            "status": pointer_latest.get("status"),
            "movement_allowed_at_capture_time": pointer_latest.get("movementAllowed"),
            "movement_sent_by_readback": pointer_latest.get("movementSent"),
            "no_cheat_engine": pointer_latest.get("noCheatEngine"),
            "stable_across_readback_samples": pointer_latest.get("stableAcrossReadbackSamples"),
            "readback_recorded_sample_count": pointer_latest.get("readbackRecordedSampleCount"),
            "readback_total_region_read_failures": pointer_latest.get("readbackTotalRegionReadFailures"),
            "proof_anchor_max_age_seconds": pointer_latest.get("proofAnchorMaxAgeSeconds"),
            "current_coordinate": pointer_latest.get("currentCoordinate"),
        },
        "proof_anchor_cache": {
            "generated_at_utc": pointer_anchor.get("generatedAtUtc"),
            "proof_method": pointer_anchor.get("proofMethod"),
            "canonical_coord_source_kind": pointer_anchor.get("canonicalCoordSourceKind"),
            "proof_validation_status": pointer_anchor.get("proofValidationStatus"),
            "pose_count": pointer_anchor.get("poseCount"),
            "max_reference_planar_displacement": pointer_anchor.get("maxReferencePlanarDisplacement"),
            "max_delta_error": pointer_anchor.get("maxDeltaError"),
        },
        "movement_evidence": {
            "active_movement_input_resumed_by_user": pointer_gate.get("activeMovementInputResumedByUser"),
            "requires_fresh_preflight_immediately_before_movement": pointer_gate.get("requiresFreshPreflightImmediatelyBeforeMovement"),
            "currently_requires_revalidation_before_more_movement": pointer_gate.get("currentlyRequiresRevalidationBeforeMoreMovement"),
            "latest_forward_smoke_status": latest_smoke.get("status") if isinstance(latest_smoke, dict) else None,
            "latest_forward_series_status": forward_series.get("status") if isinstance(forward_series, dict) else None,
            "latest_forward_series_completed_pulse_count": forward_series.get("completedPulseCount") if isinstance(forward_series, dict) else None,
            "latest_forward_series_requested_pulse_count": forward_series.get("requestedPulseCount") if isinstance(forward_series, dict) else None,
            "proof_gated_pulse_status": get_nested(forward_tests, "proofGatedPulse", "status") if isinstance(forward_tests, dict) else None,
        },
        "ledger_live_movement_authorized": False,
        "next_validation_step": next_validation,
        "source_artifacts": source_artifacts,
        "warnings": [
            "offline_ledger_does_not_authorize_live_movement",
            "fresh_pid_hwnd_preflight_required_before_any_input",
        ],
    }


def legacy_coord_api_entry(summary_path: Path, summary: dict[str, Any], superseded_by: str | None) -> dict[str, Any] | None:
    capture = summary.get("riftscan_readonly_capture") if isinstance(summary.get("riftscan_readonly_capture"), dict) else {}
    if not capture:
        return None
    trace = summary.get("riftreader_coord_trace_anchor") if isinstance(summary.get("riftreader_coord_trace_anchor"), dict) else {}
    return {
        "stable_id": stable_id("coordinate", capture.get("candidate_id"), capture.get("candidate_source_absolute_address_hex"), "legacy_coord_api_truth"),
        "candidate_id": capture.get("candidate_id"),
        "kind": "coordinate_vec3",
        "state": "historical_stale_trace_blocked",
        "claim_level": "candidate",
        "proof_level": summary.get("coordinate_truth_level"),
        "source": "coord_api_truth_handoff",
        "source_session_id": capture.get("session_id"),
        "source_session_path": capture.get("session_path"),
        "source_base_address_hex": capture.get("candidate_source_base_address_hex") or capture.get("source_base_address_hex"),
        "source_offset_hex": capture.get("candidate_source_offset_hex") or capture.get("source_offset_hex"),
        "source_absolute_address_hex": capture.get("candidate_source_absolute_address_hex"),
        "axis_order": capture.get("axis_order"),
        "support_count": capture.get("support_count"),
        "best_max_abs_distance": capture.get("best_max_abs_distance"),
        "best_memory_xyz": [
            capture.get("best_memory_x"),
            capture.get("best_memory_y"),
            capture.get("best_memory_z"),
        ],
        "best_addon_xyz": [
            capture.get("best_addon_x"),
            capture.get("best_addon_y"),
            capture.get("best_addon_z"),
        ],
        "trace_anchor": {
            "trace_matches_process": trace.get("trace_matches_process"),
            "trace_process_id": trace.get("trace_process_id"),
            "process_id": trace.get("process_id"),
            "blocked_reason": trace.get("blocked_reason"),
        },
        "superseded_by_stable_id": superseded_by,
        "ledger_live_movement_authorized": False,
        "next_validation_step": "keep as historical evidence unless explicitly replaying the stale-trace blocker",
        "source_artifacts": [rel(summary_path)],
        "warnings": [
            "old_coord_trace_anchor_does_not_match_current_process",
            "not_movement_grade_truth",
        ],
    }


def coord_recovery_entry(summary_path: Path, summary: dict[str, Any]) -> dict[str, Any] | None:
    scan = summary.get("scan") if isinstance(summary.get("scan"), dict) else {}
    if not scan:
        return None
    values = scan.get("candidate_like_values") if isinstance(scan.get("candidate_like_values"), list) else []
    return {
        "stable_id": stable_id("coordinate_scan", summary.get("process_id"), summary.get("created_utc")),
        "kind": "coordinate_candidate_scan",
        "state": "historical_candidate_scan_only",
        "claim_level": "observed",
        "proof_level": "candidate_like_values_only",
        "source": "coord_recovery_probe_summary",
        "process_id": summary.get("process_id"),
        "hit_count": scan.get("hit_count"),
        "candidate_like_value_count": scan.get("candidate_like_value_count"),
        "sample_candidate_addresses": [
            value.get("AddressHex")
            for value in values[:10]
            if isinstance(value, dict) and value.get("AddressHex")
        ],
        "final_truth_claim": summary.get("final_truth_claim"),
        "manual_confirmation_required": summary.get("manual_confirmation_required"),
        "ledger_live_movement_authorized": False,
        "next_validation_step": "do not use for current-client movement proof; keep only as historical search context",
        "source_artifacts": [rel(summary_path)],
        "warnings": [
            "candidate_scan_not_truth",
            "historical_process_specific",
        ],
    }


def session_inventory() -> dict[str, Any]:
    session_root = REPO_ROOT / "sessions"
    manifests = sorted(session_root.rglob("manifest.json"), key=lambda path: path.stat().st_mtime, reverse=True) if session_root.exists() else []
    latest: list[dict[str, Any]] = []
    for manifest_path in manifests[:20]:
        manifest = load_json(manifest_path)
        latest.append(
            {
                "path": rel(manifest_path.parent),
                "session_id": manifest.get("session_id") or manifest_path.parent.name,
                "capture_mode": manifest.get("capture_mode"),
                "status": manifest.get("status"),
                "process_id": manifest.get("process_id"),
                "process_name": manifest.get("process_name"),
                "snapshot_count": manifest.get("snapshot_count"),
                "region_count": manifest.get("region_count"),
                "total_bytes_raw": manifest.get("total_bytes_raw"),
                "last_write_utc_inferred_from_filesystem": datetime.fromtimestamp(manifest_path.stat().st_mtime, timezone.utc).isoformat().replace("+00:00", "Z"),
            }
        )
    return {
        "session_manifest_count": len(manifests),
        "latest_sessions": latest,
    }


def build_ledger(riftreader_root: Path) -> dict[str, Any]:
    append_log("build_start", riftreader_root=str(riftreader_root))

    pointer_path = riftreader_root / "docs" / "recovery" / "current-proof-anchor-readback.json"
    pointer = load_json(pointer_path)

    pointer_match_file = get_nested(pointer, "riftscanCandidateSource", "matchFile")
    match_path = Path(pointer_match_file) if isinstance(pointer_match_file, str) and pointer_match_file.strip() else None
    if match_path is None or not match_path.exists():
        match_path = newest_file(REPO_ROOT / "reports" / "generated", "*addon-coordinate-matches.json")
    match = load_json(match_path)

    candidates: list[dict[str, Any]] = []
    current_candidate = candidate_from_match(match_path, match, pointer_path if pointer_path.exists() else None, pointer)
    if current_candidate:
        candidates.append(current_candidate)

    coord_api = load_json(COORD_API_TRUTH_SUMMARY)
    legacy = legacy_coord_api_entry(COORD_API_TRUTH_SUMMARY, coord_api, current_candidate.get("stable_id") if current_candidate else None)
    if legacy:
        candidates.append(legacy)

    coord_recovery = load_json(COORD_RECOVERY_SUMMARY)
    recovery = coord_recovery_entry(COORD_RECOVERY_SUMMARY, coord_recovery)
    if recovery:
        candidates.append(recovery)

    movement_gate = load_json(MOVEMENT_EXECUTION_GATE_SUMMARY)
    riftreader_handoff = newest_file(riftreader_root / "docs" / "handoffs", "*handoff*.md")
    latest_proof_summary = newest_recursive_file(riftreader_root / "scripts" / "captures", "proof-anchor-currentpid-*-readback-summary-*.json")

    blockers = [
        "offline ledger cannot authorize live movement or claim current window focus",
        "RiftReader pointer says fresh preflight is required before more movement",
    ]
    if get_nested(coord_api, "riftreader_coord_trace_anchor", "trace_matches_process") is False:
        blockers.append("older Coord API Truth artifact remains stale-trace-blocked")
    if movement_gate.get("display_status") == "BLOCKED":
        blockers.append("RiftScan Movement Execution Gate artifact is blocked/stale relative to newer RiftReader proof lane")

    current_best = current_candidate or (candidates[0] if candidates else None)
    summary = {
        "schema_version": LEDGER_SCHEMA_VERSION,
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "ledger_written" if candidates else "no_candidates_found",
        "scope": "offline_artifact_inventory_no_live_process_access",
        "safety": {
            "offline_only": True,
            "focus_preflight_started": False,
            "live_capture_started": False,
            "process_attach_or_memory_read_started": False,
            "movement_or_input_sent": False,
            "riftreader_command_executed": False,
            "reloadui_sent": False,
            "ledger_live_movement_authorized": False,
        },
        "current_best_candidate_stable_id": current_best.get("stable_id") if current_best else None,
        "current_best_candidate": current_best,
        "candidate_count": len(candidates),
        "candidates": candidates,
        "blockers": blockers,
        "source_artifacts": {
            "riftscan_match_file": rel(match_path) if match_path else None,
            "riftscan_coord_api_truth_summary": rel(COORD_API_TRUTH_SUMMARY),
            "riftscan_coord_recovery_summary": rel(COORD_RECOVERY_SUMMARY),
            "riftscan_movement_execution_gate_summary": rel(MOVEMENT_EXECUTION_GATE_SUMMARY),
            "riftreader_current_proof_pointer": str(pointer_path),
            "riftreader_latest_handoff": str(riftreader_handoff) if riftreader_handoff else None,
            "riftreader_latest_proof_summary": str(latest_proof_summary) if latest_proof_summary else None,
        },
        "source_artifact_status": {
            "riftscan_match_file_exists": bool(match_path and match_path.exists()),
            "riftscan_coord_api_truth_summary_exists": COORD_API_TRUTH_SUMMARY.exists(),
            "riftscan_coord_recovery_summary_exists": COORD_RECOVERY_SUMMARY.exists(),
            "riftreader_current_proof_pointer_exists": pointer_path.exists(),
            "riftreader_latest_handoff_exists": bool(riftreader_handoff),
            "riftreader_latest_proof_summary_exists": bool(latest_proof_summary),
        },
        "inventory": session_inventory(),
        "next_recommended_actions": [
            "Use the RiftReader May 7 current-proof pointer as the latest discovery status, but treat it as requiring fresh preflight before more movement.",
            "Keep the RiftScan candidate at 0x2400EA32120 as the current best coordinate candidate source.",
            "Do not promote the older 0x1DA682DF690 Coord API Truth artifact beyond historical stale-trace-blocked evidence.",
            "When the game window is available, have RiftReader rerun exact PID/HWND proof readback rather than rediscovering from scratch.",
            "If PID/HWND changed, reacquire via RiftScan-first candidate import/readback/promotion instead of CE or heuristic caches.",
        ],
        "output_paths": {
            "summary": rel(SUMMARY),
            "report": rel(REPORT),
            "candidate_ledger": rel(CANDIDATE_LEDGER),
            "log": rel(LOG),
        },
    }
    append_log("build_finish", status=summary["status"], candidate_count=len(candidates))
    return summary


def report_lines(summary: dict[str, Any]) -> list[str]:
    best = summary.get("current_best_candidate") if isinstance(summary.get("current_best_candidate"), dict) else {}
    candidates = summary.get("candidates") if isinstance(summary.get("candidates"), list) else []
    lines = [
        "# RiftScan Offline Discovery Ledger",
        "",
        f"Created UTC: `{summary.get('created_utc')}`",
        f"App version: `{summary.get('app_version')}`",
        "",
        "## Result",
        "",
        "```text",
        f"status: {summary.get('status')}",
        f"scope: {summary.get('scope')}",
        f"candidate_count: {summary.get('candidate_count')}",
        f"ledger_live_movement_authorized: {get_nested(summary, 'safety', 'ledger_live_movement_authorized')}",
        "```",
        "",
        "## Current best candidate",
        "",
    ]
    if best:
        lines.extend(
            [
                "| Field | Value |",
                "|---|---|",
                f"| Candidate | `{best.get('candidate_id')}` |",
                f"| State | `{best.get('state')}` |",
                f"| Claim level | `{best.get('claim_level')}` |",
                f"| Proof level | `{best.get('proof_level')}` |",
                f"| Address | `{best.get('source_absolute_address_hex')}` |",
                f"| Base + offset | `{best.get('source_base_address_hex')}` + `{best.get('source_offset_hex')}` |",
                f"| Axis | `{best.get('axis_order')}` |",
                f"| Support | `{best.get('support_count')}` snapshots / `{best.get('observation_support_count')}` observations |",
                f"| Best max abs distance | `{best.get('best_max_abs_distance')}` |",
                f"| RiftReader status | `{best.get('riftreader_status')}` |",
                f"| Next validation | `{best.get('next_validation_step')}` |",
                "",
            ]
        )
    else:
        lines.extend(["No current best candidate found.", ""])

    lines.extend(
        [
            "## Candidate ledger",
            "",
            "| State | Candidate / kind | Address | Proof level | Next validation |",
            "|---|---|---|---|---|",
        ]
    )
    for candidate in candidates:
        label = candidate.get("candidate_id") or candidate.get("kind")
        address = candidate.get("source_absolute_address_hex") or "-"
        lines.append(
            f"| `{candidate.get('state')}` | `{label}` | `{address}` | `{candidate.get('proof_level')}` | `{candidate.get('next_validation_step')}` |"
        )
    if not candidates:
        lines.append("| none | - | - | - | - |")

    lines.extend(
        [
            "",
            "## Blockers / guardrails",
            "",
        ]
    )
    for blocker in summary.get("blockers", []):
        lines.append(f"- {blocker}")

    lines.extend(
        [
            "",
            "## Source artifacts",
            "",
            "```json",
            json.dumps(summary.get("source_artifacts", {}), indent=2, sort_keys=True),
            "```",
            "",
            "## Safety",
            "",
            "```json",
            json.dumps(summary.get("safety", {}), indent=2, sort_keys=True),
            "```",
            "",
            "## Next recommended actions",
            "",
        ]
    )
    for index, action in enumerate(summary.get("next_recommended_actions", []), start=1):
        lines.append(f"{index}. {action}")

    lines.append("")
    return lines


def write_outputs(summary: dict[str, Any]) -> None:
    write_json(SUMMARY, summary)
    lines = [json.dumps(candidate, sort_keys=True) for candidate in summary.get("candidates", [])]
    write_text(CANDIDATE_LEDGER, "\n".join(lines) + ("\n" if lines else ""))
    write_text(REPORT, "\n".join(report_lines(summary)))
    append_log("outputs_written", summary=rel(SUMMARY), report=rel(REPORT), candidate_ledger=rel(CANDIDATE_LEDGER))


def is_blank(value: Any) -> bool:
    return value is None or (isinstance(value, str) and not value.strip())


def issue(
    issues: list[dict[str, Any]],
    code: str,
    message: str,
    *,
    severity: str = "error",
    line: int | None = None,
    stable_id: Any = None,
    field: str | None = None,
) -> None:
    item: dict[str, Any] = {
        "severity": severity,
        "code": code,
        "message": message,
    }
    if line is not None:
        item["line"] = line
    if not is_blank(stable_id):
        item["stable_id"] = stable_id
    if field:
        item["field"] = field
    issues.append(item)


def require_present(entry: dict[str, Any], field: str, issues: list[dict[str, Any]], line: int, stable_id: Any) -> None:
    if field not in entry or is_blank(entry.get(field)):
        issue(issues, "missing_required_field", f"Missing required field: {field}", line=line, stable_id=stable_id, field=field)


def require_list(entry: dict[str, Any], field: str, issues: list[dict[str, Any]], line: int, stable_id: Any) -> list[Any]:
    value = entry.get(field)
    if not isinstance(value, list):
        issue(issues, "field_must_be_list", f"Field must be a list: {field}", line=line, stable_id=stable_id, field=field)
        return []
    return value


def validate_candidate_entries(entries: list[dict[str, Any]], source_name: str) -> list[dict[str, Any]]:
    issues: list[dict[str, Any]] = []
    seen_stable_ids: set[str] = set()

    for index, entry in enumerate(entries, start=1):
        line = index
        stable_id = entry.get("stable_id")
        if is_blank(stable_id):
            issue(issues, "missing_stable_id", "Every candidate ledger row must have a stable_id.", line=line, field="stable_id")
            stable_id = f"{source_name}:line-{line}"
        else:
            stable_id_text = str(stable_id)
            if stable_id_text in seen_stable_ids:
                issue(issues, "duplicate_stable_id", "stable_id must be unique within the candidate ledger.", line=line, stable_id=stable_id)
            seen_stable_ids.add(stable_id_text)

        for field in REQUIRED_COMMON_FIELDS:
            require_present(entry, field, issues, line, stable_id)

        state = entry.get("state")
        if state not in ALLOWED_STATES:
            issue(issues, "unknown_state", f"State is not in the allowed contract set: {state}", line=line, stable_id=stable_id, field="state")

        claim_level = entry.get("claim_level")
        if claim_level not in ALLOWED_CLAIM_LEVELS:
            issue(
                issues,
                "unknown_claim_level",
                f"Claim level is not in the allowed contract set: {claim_level}",
                line=line,
                stable_id=stable_id,
                field="claim_level",
            )

        if entry.get("ledger_live_movement_authorized") is not False:
            issue(
                issues,
                "ledger_must_not_authorize_live_movement",
                "Offline discovery ledger rows must keep ledger_live_movement_authorized=false.",
                line=line,
                stable_id=stable_id,
                field="ledger_live_movement_authorized",
            )

        artifacts = require_list(entry, "source_artifacts", issues, line, stable_id)
        if not artifacts or any(not isinstance(path, str) or not path.strip() for path in artifacts):
            issue(
                issues,
                "source_artifacts_must_be_nonempty_strings",
                "source_artifacts must contain at least one non-empty artifact path.",
                line=line,
                stable_id=stable_id,
                field="source_artifacts",
            )

        warnings = require_list(entry, "warnings", issues, line, stable_id)
        if not warnings:
            issue(issues, "warnings_required", "warnings must contain at least one guardrail note.", line=line, stable_id=stable_id, field="warnings")

        kind = entry.get("kind")
        if kind == "coordinate_vec3":
            for field in REQUIRED_COORDINATE_FIELDS:
                require_present(entry, field, issues, line, stable_id)
            for vector_field in ("best_memory_xyz", "best_addon_xyz"):
                vector = entry.get(vector_field)
                if not isinstance(vector, list) or len(vector) != 3:
                    issue(
                        issues,
                        "coordinate_vector_must_have_three_values",
                        f"{vector_field} must be a three-value coordinate vector.",
                        line=line,
                        stable_id=stable_id,
                        field=vector_field,
                    )

        if state == "validated_candidate_historical_checkpoint":
            for field in ("source_base_address_hex", "source_offset_hex", "source_absolute_address_hex"):
                require_present(entry, field, issues, line, stable_id)
            if entry.get("riftreader_pointer_matched_candidate") is not True:
                issue(
                    issues,
                    "validated_checkpoint_requires_pointer_match",
                    "Validated checkpoint rows must show the RiftReader pointer matched the RiftScan candidate.",
                    line=line,
                    stable_id=stable_id,
                    field="riftreader_pointer_matched_candidate",
                )
            latest = entry.get("latest_validation") if isinstance(entry.get("latest_validation"), dict) else {}
            if latest.get("no_cheat_engine") is not True:
                issue(
                    issues,
                    "validated_checkpoint_requires_no_ce",
                    "Validated checkpoint rows must preserve no-Cheat-Engine proof evidence.",
                    line=line,
                    stable_id=stable_id,
                    field="latest_validation.no_cheat_engine",
                )
            if latest.get("movement_sent_by_readback") is not False:
                issue(
                    issues,
                    "readback_must_not_send_movement",
                    "Proof readback evidence must not have sent movement.",
                    line=line,
                    stable_id=stable_id,
                    field="latest_validation.movement_sent_by_readback",
                )
            next_step = str(entry.get("next_validation_step") or "").lower()
            if "proof readback" not in next_step or "before" not in next_step:
                issue(
                    issues,
                    "validated_checkpoint_requires_fresh_readback_next_step",
                    "Validated checkpoint rows must still require a fresh proof readback before live input.",
                    line=line,
                    stable_id=stable_id,
                    field="next_validation_step",
                )
            if "offline_ledger_does_not_authorize_live_movement" not in warnings:
                issue(
                    issues,
                    "validated_checkpoint_missing_offline_warning",
                    "Validated checkpoint rows must warn that the offline ledger does not authorize live movement.",
                    line=line,
                    stable_id=stable_id,
                    field="warnings",
                )

        if state == "historical_stale_trace_blocked":
            trace = entry.get("trace_anchor") if isinstance(entry.get("trace_anchor"), dict) else {}
            if trace.get("trace_matches_process") is not False:
                issue(
                    issues,
                    "stale_trace_rows_must_preserve_blocker",
                    "Historical stale-trace rows must preserve trace_matches_process=false.",
                    line=line,
                    stable_id=stable_id,
                    field="trace_anchor.trace_matches_process",
                )

        if state == "historical_candidate_scan_only":
            if kind != "coordinate_candidate_scan":
                issue(
                    issues,
                    "candidate_scan_state_kind_mismatch",
                    "historical_candidate_scan_only rows must use kind=coordinate_candidate_scan.",
                    line=line,
                    stable_id=stable_id,
                    field="kind",
                )
            if entry.get("claim_level") != "observed":
                issue(
                    issues,
                    "candidate_scan_claim_level_must_be_observed",
                    "Historical candidate scans are observed search context only.",
                    line=line,
                    stable_id=stable_id,
                    field="claim_level",
                )
            if entry.get("final_truth_claim") is not False:
                issue(
                    issues,
                    "candidate_scan_must_not_claim_truth",
                    "Historical candidate scans must keep final_truth_claim=false.",
                    line=line,
                    stable_id=stable_id,
                    field="final_truth_claim",
                )

    return issues


def validate_candidate_ledger(path: Path) -> dict[str, Any]:
    issues: list[dict[str, Any]] = []
    entries: list[dict[str, Any]] = []
    line_count = 0

    if not path.exists():
        issue(issues, "candidate_ledger_missing", "Candidate ledger JSONL file does not exist.", field="path")
    else:
        for line_count, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), start=1):
            if not line.strip():
                continue
            try:
                parsed = json.loads(line)
            except json.JSONDecodeError as exc:
                issue(issues, "jsonl_parse_error", f"Line is not valid JSON: {exc}", line=line_count)
                continue
            if not isinstance(parsed, dict):
                issue(issues, "jsonl_root_must_be_object", "Each JSONL row must be a JSON object.", line=line_count)
                continue
            entries.append(parsed)

    if entries:
        issues.extend(validate_candidate_entries(entries, rel(path)))
    elif path.exists():
        issue(issues, "candidate_ledger_empty", "Candidate ledger exists but contains no candidate rows.", field="path")

    errors = [item for item in issues if item.get("severity") != "warning"]
    return {
        "schema_version": LEDGER_VALIDATION_SCHEMA_VERSION,
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if not errors else "FAIL",
        "display_status": "PASS" if not errors else "FAIL",
        "path": rel(path),
        "line_count": line_count,
        "candidate_count": len(entries),
        "error_count": len(errors),
        "warning_count": len(issues) - len(errors),
        "issues": issues,
        "safety": {
            "offline_only": True,
            "writes_artifacts": False,
            "runs_focus_preflight": False,
            "capture_started": False,
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "process_attach_or_memory_read_started": False,
            "riftreader_command_executed": False,
            "reloadui_sent": False,
            "ledger_live_movement_authorized": False,
        },
    }


def run_self_test() -> int:
    fake_match = {
        "session_id": "fixture-session",
        "candidates": [
            {
                "candidate_id": "rift-addon-coordinate-candidate-000001",
                "source_region_id": "region-1",
                "source_base_address_hex": "0x1000",
                "source_offset_hex": "0x20",
                "source_absolute_address_hex": "0x1020",
                "axis_order": "xyz",
                "support_count": 3,
                "observation_support_count": 1,
                "best_max_abs_distance": 0,
                "best_memory_x": 1.0,
                "best_memory_y": 2.0,
                "best_memory_z": 3.0,
                "best_addon_x": 1.0,
                "best_addon_y": 2.0,
                "best_addon_z": 3.0,
                "validation_status": "candidate_unverified",
            }
        ],
    }
    fake_pointer = {
        "status": "valid-after-test",
        "riftscanCandidateSource": {"sourceAbsoluteAddressHex": "0x1020"},
        "movementGate": {
            "coordinateProofGateSatisfiedAtLatestValidation": True,
            "currentlyRequiresRevalidationBeforeMoreMovement": True,
            "requiresFreshPreflightImmediatelyBeforeMovement": True,
        },
        "latestValidation": {
            "status": "valid",
            "movementAllowed": True,
            "movementSent": False,
            "noCheatEngine": True,
            "stableAcrossReadbackSamples": True,
        },
        "proofAnchorCache": {
            "candidateAddressHex": "0x1020",
            "proofMethod": "no-ce-riftscan-reference-multisample",
        },
    }
    candidate = candidate_from_match(Path("fixture-match.json"), fake_match, Path("fixture-pointer.json"), fake_pointer)
    failures: list[str] = []
    if not candidate:
        failures.append("candidate_not_built")
    elif candidate.get("state") != "validated_candidate_historical_checkpoint":
        failures.append(f"unexpected_state={candidate.get('state')}")
    elif candidate.get("ledger_live_movement_authorized") is not False:
        failures.append("ledger_must_not_authorize_live_movement")

    fake_legacy = {
        "coordinate_truth_level": "current_api_plus_readonly_memory_candidate",
        "riftscan_readonly_capture": {
            "candidate_id": "old",
            "axis_order": "xyz",
            "candidate_source_absolute_address_hex": "0xDEAD",
            "support_count": 8,
            "best_max_abs_distance": 0.0,
            "best_memory_x": 1.0,
            "best_memory_y": 2.0,
            "best_memory_z": 3.0,
            "best_addon_x": 1.0,
            "best_addon_y": 2.0,
            "best_addon_z": 3.0,
        },
        "riftreader_coord_trace_anchor": {
            "trace_matches_process": False,
            "trace_process_id": 1,
            "process_id": 2,
        },
    }
    legacy = legacy_coord_api_entry(Path("legacy.json"), fake_legacy, "new")
    if not legacy or legacy.get("state") != "historical_stale_trace_blocked":
        failures.append("legacy_state_not_stale_trace_blocked")

    recovery = {
        "stable_id": "coordinate_scan::fixture",
        "kind": "coordinate_candidate_scan",
        "state": "historical_candidate_scan_only",
        "claim_level": "observed",
        "proof_level": "candidate_like_values_only",
        "source": "self_test",
        "sample_candidate_addresses": ["0x1000"],
        "final_truth_claim": False,
        "manual_confirmation_required": True,
        "ledger_live_movement_authorized": False,
        "next_validation_step": "keep only as historical search context",
        "source_artifacts": ["fixture-recovery.json"],
        "warnings": ["candidate_scan_not_truth"],
    }
    valid_entries = [entry for entry in (candidate, legacy, recovery) if entry]
    validation_issues = validate_candidate_entries(valid_entries, "self-test")
    validation_errors = [item for item in validation_issues if item.get("severity") != "warning"]
    if validation_errors:
        failures.append(f"valid_entries_failed_contract={validation_errors}")

    bad_candidate = dict(candidate or {})
    bad_candidate["ledger_live_movement_authorized"] = True
    bad_issues = validate_candidate_entries([bad_candidate], "self-test-bad")
    if not any(item.get("code") == "ledger_must_not_authorize_live_movement" for item in bad_issues):
        failures.append("bad_candidate_did_not_fail_live_movement_contract")

    result = {
        "schema_version": "riftscan.discovery_ledger.self_test.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        "status": "PASS" if not failures else "FAIL",
        "failures": failures,
        "contract_validation_issue_count": len(validation_issues),
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
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0 if not failures else 1


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build an offline RiftScan discovery ledger from stored artifacts.")
    parser.add_argument("--riftreader-root", default=str(DEFAULT_RIFTREADER_ROOT), help="RiftReader repo root to read tracked proof pointer artifacts from.")
    parser.add_argument("--out-dir", default=str(OUT_DIR), help="Output directory for ledger artifacts.")
    parser.add_argument("--self-test", action="store_true", help="Run offline self-test only; writes no artifacts.")
    parser.add_argument("--validate-existing", action="store_true", help="Validate the existing candidate_ledger.jsonl contract without writing artifacts.")
    parser.add_argument("--print-summary", action="store_true", help="Print the generated summary JSON after writing artifacts.")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    if args.self_test:
        return run_self_test()

    global OUT_DIR, SUMMARY, REPORT, CANDIDATE_LEDGER, LOG
    OUT_DIR = Path(args.out_dir)
    SUMMARY = OUT_DIR / "discovery-ledger-summary.json"
    REPORT = OUT_DIR / "DISCOVERY_LEDGER_REPORT.md"
    CANDIDATE_LEDGER = OUT_DIR / "candidate_ledger.jsonl"
    LOG = OUT_DIR / "discovery-ledger-log.jsonl"

    if args.validate_existing:
        validation = validate_candidate_ledger(CANDIDATE_LEDGER)
        print(json.dumps(validation, indent=2, sort_keys=True))
        return 0 if validation["status"] == "PASS" else 1

    summary = build_ledger(Path(args.riftreader_root))
    write_outputs(summary)
    if args.print_summary:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(f"RIFTSCAN DISCOVERY LEDGER: {rel(REPORT)}")
        print(f"Summary: {rel(SUMMARY)}")
        print(f"Candidate ledger: {rel(CANDIDATE_LEDGER)}")
        print("Safety: offline artifact inventory only; no focus, capture, input, movement, memory read, RiftReader command, or /reloadui was run.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
