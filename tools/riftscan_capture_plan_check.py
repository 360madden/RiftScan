#!/usr/bin/env python3
# RiftScan script metadata
# Version: riftscan-capture-plan-check-v1.0.0
# Total character count: 26155
# Purpose: Validate the latest metadata-only focus-gated capture plan before any future live-collection gate.
# Safety boundary: Reads existing plan/gate artifacts only. No focus preflight, live capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Callable
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

APP_VERSION = "riftscan-capture-plan-check-v1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[1]
PLAN_ROOT = REPO_ROOT / "plans" / "focus-gated-capture-plans"
LATEST_CAPTURE_PLAN = PLAN_ROOT / "LATEST_CAPTURE_PLAN.txt"
OPERATOR_DIR = REPO_ROOT / "handoffs" / "current" / "operator"
OPERATOR_GATE_SUMMARY = OPERATOR_DIR / "operator-current-gate-summary.json"
OUT_DIR = REPO_ROOT / "handoffs" / "current" / "capture-plan-check"
REPORT = OUT_DIR / "CAPTURE_PLAN_CHECK_REPORT.md"
SUMMARY = OUT_DIR / "capture-plan-check-summary.json"
LOG = OUT_DIR / "capture-plan-check-log.jsonl"

EXPECTED_FILES = {
    "capture-session-manifest.json",
    "capture-log.jsonl",
    "focus-summary-before.json",
    "focus-summary-after.json",
    "operator-report.md",
}
REQUIRED_SOURCE_ARTIFACT_KEYS = {
    "focus_summary",
    "windows_json",
    "focus_log",
    "operator_report",
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


def read_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
        log("json_read", path=rel(path))
        return data
    except Exception as exc:
        log("json_read_failed", path=rel(path), error=f"{type(exc).__name__}: {exc}")
        return {"_read_success": False, "_read_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def inside(child: Path, parent: Path) -> bool:
    try:
        child.resolve().relative_to(parent.resolve())
        return True
    except ValueError:
        return False


def repo_path(value: Any) -> Path | None:
    if not isinstance(value, str) or not value.strip():
        return None
    candidate = Path(value.strip())
    if not candidate.is_absolute():
        candidate = REPO_ROOT / candidate
    try:
        candidate.resolve().relative_to(REPO_ROOT.resolve())
    except ValueError:
        return None
    return candidate


def source_artifact_exists(path_value: Any) -> bool:
    candidate = repo_path(path_value)
    return bool(candidate and candidate.exists())


def read_latest_capture_plan() -> dict[str, Any]:
    if not LATEST_CAPTURE_PLAN.exists():
        return {
            "status": "none",
            "reason": "latest capture-plan pointer is missing",
            "pointer_path": rel(LATEST_CAPTURE_PLAN),
        }

    plan_rel = LATEST_CAPTURE_PLAN.read_text(encoding="utf-8", errors="replace").strip()
    if not plan_rel:
        return {
            "status": "none",
            "reason": "latest capture-plan pointer is empty",
            "pointer_path": rel(LATEST_CAPTURE_PLAN),
        }

    plan_dir = REPO_ROOT / plan_rel
    manifest_path = plan_dir / "capture-plan.json"
    handoff_path = plan_dir / "CAPTURE_PLAN_HANDOFF.md"
    return {
        "status": "present",
        "pointer_path": rel(LATEST_CAPTURE_PLAN),
        "latest_plan": plan_rel,
        "plan_dir": rel(plan_dir),
        "plan_dir_inside_repo": inside(plan_dir, REPO_ROOT),
        "plan_dir_under_root": inside(plan_dir, PLAN_ROOT),
        "manifest_path": rel(manifest_path),
        "handoff_path": rel(handoff_path),
        "manifest_exists": manifest_path.exists(),
        "handoff_exists": handoff_path.exists(),
        "manifest": read_json(manifest_path),
    }


def read_operator_gate_summary() -> dict[str, Any]:
    return read_json(OPERATOR_GATE_SUMMARY)


def false_required(value: Any, label: str, blockers: list[str]) -> None:
    if value is not False:
        blockers.append(f"{label} is not false.")


def true_required(value: Any, label: str, blockers: list[str]) -> None:
    if value is not True:
        blockers.append(f"{label} is not true.")


def evaluate_capture_plan(
    latest: dict[str, Any],
    operator_gate: dict[str, Any],
    *,
    artifact_exists: Callable[[Any], bool] = source_artifact_exists,
) -> dict[str, Any]:
    blockers: list[str] = []
    warnings: list[str] = []
    checks: dict[str, Any] = {}

    if latest.get("status") != "present":
        blockers.append(str(latest.get("reason") or "No latest capture plan is present."))
        manifest: dict[str, Any] = {}
    else:
        if latest.get("plan_dir_inside_repo") is not True:
            blockers.append("Latest capture-plan path is outside the repo.")
        if latest.get("plan_dir_under_root") is not True:
            blockers.append("Latest capture-plan path is outside plans/focus-gated-capture-plans.")
        true_required(latest.get("manifest_exists"), "latest capture-plan manifest exists", blockers)
        true_required(latest.get("handoff_exists"), "latest capture-plan handoff exists", blockers)
        manifest = latest.get("manifest") if isinstance(latest.get("manifest"), dict) else {}

    if manifest.get("_read_success") is False:
        blockers.append(f"Capture-plan manifest could not be read: {manifest.get('_read_error')}.")

    checks["schema_version"] = manifest.get("schema_version")
    checks["plan_status"] = manifest.get("status")
    checks["metadata_only"] = manifest.get("metadata_only")
    checks["capture_started"] = manifest.get("capture_started")
    checks["capture_completed"] = manifest.get("capture_completed")

    if manifest.get("schema_version") != "riftscan.focus_gated_capture_plan.v1":
        blockers.append("Capture-plan schema_version is not riftscan.focus_gated_capture_plan.v1.")
    if manifest.get("status") != "capture_plan_created":
        blockers.append("Capture-plan status is not capture_plan_created.")
    true_required(manifest.get("metadata_only"), "capture-plan metadata_only", blockers)
    false_required(manifest.get("capture_started"), "capture-plan capture_started", blockers)
    false_required(manifest.get("capture_completed"), "capture-plan capture_completed", blockers)

    expected_files = manifest.get("expected_files")
    checks["expected_files"] = expected_files
    if not isinstance(expected_files, list) or not expected_files:
        blockers.append("Capture-plan expected_files is missing or empty.")
        expected_file_set: set[str] = set()
    else:
        expected_file_set = {str(item) for item in expected_files}
    missing_expected = sorted(EXPECTED_FILES - expected_file_set)
    if missing_expected:
        blockers.append("Capture-plan expected_files is missing required metadata outputs: " + ", ".join(missing_expected) + ".")

    full_preflight = manifest.get("full_live_preflight") if isinstance(manifest.get("full_live_preflight"), dict) else {}
    checks["full_live_preflight"] = full_preflight
    if full_preflight.get("status") != "PASS":
        blockers.append("Capture-plan full_live_preflight.status is not PASS.")
    if full_preflight.get("focus_status") != "foreground_verified":
        blockers.append("Capture-plan full_live_preflight.focus_status is not foreground_verified.")
    if full_preflight.get("process_name") != "rift_x64":
        blockers.append("Capture-plan full_live_preflight.process_name is not rift_x64.")
    if not full_preflight.get("window_hwnd"):
        blockers.append("Capture-plan full_live_preflight.window_hwnd is missing.")
    try:
        windows_count = int(full_preflight.get("windows_count") or 0)
    except (TypeError, ValueError):
        windows_count = 0
    if windows_count < 1:
        blockers.append("Capture-plan full_live_preflight.windows_count is less than 1.")

    source_artifacts = manifest.get("source_artifacts") if isinstance(manifest.get("source_artifacts"), dict) else {}
    checks["source_artifacts"] = source_artifacts
    missing_source_keys = sorted(REQUIRED_SOURCE_ARTIFACT_KEYS - set(source_artifacts.keys()))
    if missing_source_keys:
        blockers.append("Capture-plan source_artifacts is missing required focus/preflight keys: " + ", ".join(missing_source_keys) + ".")
    missing_source_paths = []
    for key in sorted(REQUIRED_SOURCE_ARTIFACT_KEYS & set(source_artifacts.keys())):
        if not artifact_exists(source_artifacts.get(key)):
            missing_source_paths.append(f"{key}={source_artifacts.get(key)}")
    if missing_source_paths:
        blockers.append("Capture-plan focus/preflight source artifacts are missing: " + ", ".join(missing_source_paths) + ".")

    guardrails = manifest.get("guardrails") if isinstance(manifest.get("guardrails"), list) else []
    guardrail_text = "\n".join(str(item).lower() for item in guardrails)
    checks["guardrail_count"] = len(guardrails)
    for token, label in [
        ("metadata only", "metadata-only guardrail"),
        ("no capture", "no-capture guardrail"),
        ("no movement/input", "no movement/input guardrail"),
        ("no memory scan/read", "no memory scan/read guardrail"),
        ("no /reloadui", "no /reloadui guardrail"),
    ]:
        if token not in guardrail_text:
            warnings.append(f"Capture-plan guardrails do not explicitly mention {label}.")

    if operator_gate.get("_read_success") is False:
        blockers.append(f"Operator current gate summary could not be read: {operator_gate.get('_read_error')}.")
    if operator_gate.get("metadata_capture_plan_gate") != "PASS":
        blockers.append("Operator metadata_capture_plan_gate is not PASS.")
    false_required(operator_gate.get("live_collection_allowed"), "Operator gate live_collection_allowed", blockers)
    false_required(operator_gate.get("old_offsets_trusted"), "Operator gate old_offsets_trusted", blockers)
    gate_baseline = operator_gate.get("post_update_baseline") if isinstance(operator_gate.get("post_update_baseline"), dict) else {}
    gate_readiness = operator_gate.get("capture_readiness") if isinstance(operator_gate.get("capture_readiness"), dict) else {}
    gate_link = operator_gate.get("capture_readiness_baseline_link") if isinstance(operator_gate.get("capture_readiness_baseline_link"), dict) else {}
    if gate_baseline.get("display_status") != "PASS":
        blockers.append("Operator gate post_update_baseline display_status is not PASS.")
    if gate_readiness.get("display_status") != "PASS":
        blockers.append("Operator gate capture_readiness display_status is not PASS.")
    if gate_link.get("status") != "match":
        blockers.append("Operator gate capture_readiness_baseline_link is not match.")

    status = "pass" if not blockers else "blocked_capture_plan_not_valid"
    return {
        "status": status,
        "display_status": "PASS" if status == "pass" else "BLOCKED",
        "blockers": blockers,
        "warnings": warnings,
        "checks": checks,
        "latest_capture_plan": {
            "status": latest.get("status"),
            "latest_plan": latest.get("latest_plan"),
            "pointer_path": latest.get("pointer_path"),
            "manifest_path": latest.get("manifest_path"),
            "handoff_path": latest.get("handoff_path"),
            "manifest_exists": latest.get("manifest_exists"),
            "handoff_exists": latest.get("handoff_exists"),
        },
        "operator_gate": {
            "summary_path": rel(OPERATOR_GATE_SUMMARY),
            "metadata_capture_plan_gate": operator_gate.get("metadata_capture_plan_gate"),
            "live_collection_allowed": operator_gate.get("live_collection_allowed"),
            "old_offsets_trusted": operator_gate.get("old_offsets_trusted"),
            "post_update_baseline_display_status": gate_baseline.get("display_status"),
            "capture_readiness_display_status": gate_readiness.get("display_status"),
            "capture_readiness_baseline_link": gate_link.get("status"),
            "next_action": operator_gate.get("next_action"),
        },
        "safety": {
            "metadata_only": True,
            "capture_started": False,
            "capture_completed": False,
            "live_collection_allowed": False,
            "capture_plan_review_allowed": status == "pass",
            "movement_or_input_sent": False,
            "memory_scan_or_read_started": False,
            "offset_validation_started": False,
            "riftreader_validation_started": False,
            "reloadui_sent": False,
        },
    }


def git_snapshot(timeout: int) -> dict[str, Any]:
    head = run(["git", "rev-parse", "HEAD"], timeout)
    status = run(["git", "status", "--short"], timeout)
    recent = run(["git", "log", "--oneline", "-5"], timeout)
    return {
        "head": head.get("stdout", "").strip() if head.get("success") else None,
        "status_short": status.get("stdout", "") if status.get("success") else status.get("stderr", ""),
        "log_oneline_5": recent.get("stdout", "").strip() if recent.get("success") else recent.get("stderr", ""),
    }


def build_report(data: dict[str, Any]) -> str:
    blockers = data.get("blockers") or ["None"]
    warnings = data.get("warnings") or ["None"]
    blocker_text = "\n".join(f"- {blocker}" for blocker in blockers)
    warning_text = "\n".join(f"- {warning}" for warning in warnings)
    plan = data.get("latest_capture_plan") if isinstance(data.get("latest_capture_plan"), dict) else {}
    operator_gate = data.get("operator_gate") if isinstance(data.get("operator_gate"), dict) else {}
    return f"""# RiftScan Capture Plan Check Report

## Result

```text
CAPTURE PLAN CHECK: {data["display_status"]}
status: {data["status"]}
```

## Blockers

{blocker_text}

## Warnings

{warning_text}

## Latest Capture Plan

```text
latest_plan: {plan.get("latest_plan")}
manifest: {plan.get("manifest_path")}
handoff: {plan.get("handoff_path")}
manifest_exists: {plan.get("manifest_exists")}
handoff_exists: {plan.get("handoff_exists")}
```

## Operator Gate

```text
summary_path: {operator_gate.get("summary_path")}
metadata_capture_plan_gate: {operator_gate.get("metadata_capture_plan_gate")}
post_update_baseline: {operator_gate.get("post_update_baseline_display_status")}
capture_readiness: {operator_gate.get("capture_readiness_display_status")}
capture_readiness_baseline_link: {operator_gate.get("capture_readiness_baseline_link")}
live_collection_allowed: {operator_gate.get("live_collection_allowed")}
old_offsets_trusted: {operator_gate.get("old_offsets_trusted")}
next_action: {operator_gate.get("next_action")}
```

## PASS-but-not-live Meaning

```text
metadata_capture_plan_gate: PASS means metadata capture-plan review/refinement is allowed.
live_collection_allowed: false means real capture, scanner/discovery probes, movement/input, /reloadui, offset validation, and RiftReader validation are still blocked.
```

## Safety Boundary

```text
metadata_only: true
capture_started: false
capture_completed: false
live_collection_allowed: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
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


def self_test_latest() -> dict[str, Any]:
    return {
        "status": "present",
        "pointer_path": "plans/focus-gated-capture-plans/LATEST_CAPTURE_PLAN.txt",
        "latest_plan": "plans/focus-gated-capture-plans/self_test_capture_plan",
        "plan_dir": "plans/focus-gated-capture-plans/self_test_capture_plan",
        "plan_dir_inside_repo": True,
        "plan_dir_under_root": True,
        "manifest_path": "plans/focus-gated-capture-plans/self_test_capture_plan/capture-plan.json",
        "handoff_path": "plans/focus-gated-capture-plans/self_test_capture_plan/CAPTURE_PLAN_HANDOFF.md",
        "manifest_exists": True,
        "handoff_exists": True,
        "manifest": {
            "schema_version": "riftscan.focus_gated_capture_plan.v1",
            "status": "capture_plan_created",
            "metadata_only": True,
            "capture_started": False,
            "capture_completed": False,
            "expected_files": sorted(EXPECTED_FILES),
            "full_live_preflight": {
                "status": "PASS",
                "focus_status": "foreground_verified",
                "process_name": "rift_x64",
                "window_hwnd": 1234,
                "windows_count": 1,
            },
            "source_artifacts": {
                "focus_summary": "handoffs/current/focus-control-local/focus-control-summary.json",
                "windows_json": "handoffs/current/focus-control-local/windows.json",
                "focus_log": "handoffs/current/focus-control-local/focus-control-log.jsonl",
                "operator_report": "handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md",
            },
            "guardrails": [
                "Metadata only.",
                "No capture started.",
                "No movement/input sent.",
                "No memory scan/read started.",
                "No /reloadui sent.",
            ],
        },
    }


def self_test_gate() -> dict[str, Any]:
    return {
        "metadata_capture_plan_gate": "PASS",
        "live_collection_allowed": False,
        "old_offsets_trusted": False,
        "post_update_baseline": {"display_status": "PASS"},
        "capture_readiness": {"display_status": "PASS"},
        "capture_readiness_baseline_link": {"status": "match"},
        "next_action": "Run Capture Plan Check and review the latest metadata-only capture plan; live collection/discovery still requires an explicit future gate.",
    }


def run_self_test() -> tuple[bool, dict[str, Any]]:
    tests: list[dict[str, Any]] = []

    def record(
        name: str,
        expected_status: str,
        *,
        latest_patch: dict[str, Any] | None = None,
        manifest_patch: dict[str, Any] | None = None,
        gate_patch: dict[str, Any] | None = None,
        missing_artifacts: set[str] | None = None,
        expected_blocker_part: str | None = None,
    ) -> None:
        latest = clone_json(self_test_latest())
        gate = clone_json(self_test_gate())
        if latest_patch:
            latest.update(latest_patch)
        if manifest_patch:
            manifest = latest.get("manifest") if isinstance(latest.get("manifest"), dict) else {}
            manifest.update(manifest_patch)
        if gate_patch:
            gate.update(gate_patch)
        missing = missing_artifacts or set()
        evaluation = evaluate_capture_plan(
            latest,
            gate,
            artifact_exists=lambda value: str(value) not in missing,
        )
        blockers = evaluation["blockers"]
        status_ok = evaluation["status"] == expected_status
        expected_display_status = "PASS" if expected_status == "pass" else "BLOCKED"
        display_ok = evaluation["display_status"] == expected_display_status
        blocker_ok = expected_blocker_part is None or any(expected_blocker_part in blocker for blocker in blockers)
        safety = evaluation.get("safety") if isinstance(evaluation.get("safety"), dict) else {}
        safety_ok = (
            safety.get("live_collection_allowed") is False
            and safety.get("movement_or_input_sent") is False
            and safety.get("memory_scan_or_read_started") is False
            and safety.get("reloadui_sent") is False
        )
        tests.append(
            {
                "name": name,
                "expected_status": expected_status,
                "actual_status": evaluation["status"],
                "expected_display_status": expected_display_status,
                "actual_display_status": evaluation["display_status"],
                "blockers": blockers,
                "pass": status_ok and display_ok and blocker_ok and safety_ok,
            }
        )

    record("valid metadata-only plan", "pass")
    record(
        "blocked capture_started true",
        "blocked_capture_plan_not_valid",
        manifest_patch={"capture_started": True},
        expected_blocker_part="capture-plan capture_started",
    )
    record(
        "blocked missing expected files",
        "blocked_capture_plan_not_valid",
        manifest_patch={"expected_files": ["capture-session-manifest.json"]},
        expected_blocker_part="expected_files is missing",
    )
    record(
        "blocked missing source artifact",
        "blocked_capture_plan_not_valid",
        missing_artifacts={"handoffs/current/focus-control-local/windows.json"},
        expected_blocker_part="source artifacts are missing",
    )
    record(
        "blocked operator live collection allowed",
        "blocked_capture_plan_not_valid",
        gate_patch={"live_collection_allowed": True},
        expected_blocker_part="live_collection_allowed",
    )

    passed = all(test["pass"] for test in tests)
    return passed, {
        "schema_version": "riftscan.capture_plan_check_self_test.v1",
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
    p = argparse.ArgumentParser(description="Validate the latest metadata-only RiftScan capture plan.")
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
    log("capture_plan_check_start", version=APP_VERSION)
    latest = read_latest_capture_plan()
    operator_gate = read_operator_gate_summary()
    evaluation = evaluate_capture_plan(latest, operator_gate)
    data = {
        "schema_version": "riftscan.capture_plan_check.v1",
        "created_utc": utc(),
        "app_version": APP_VERSION,
        **evaluation,
        "paths": {"report": rel(REPORT), "summary": rel(SUMMARY), "log": rel(LOG)},
        "source_artifacts": {
            "latest_capture_plan_pointer": rel(LATEST_CAPTURE_PLAN),
            "operator_gate_summary": rel(OPERATOR_GATE_SUMMARY),
        },
        "git": git_snapshot(args.git_timeout_seconds),
        "next_step": (
            "Review/refine the latest metadata-only capture plan. Real live collection remains blocked until a separate live-collection gate is explicitly approved."
            if evaluation["status"] == "pass"
            else "Fix or regenerate the metadata-only capture plan before considering any future live-collection gate."
        ),
    }

    write_json(SUMMARY, data)
    write_text(REPORT, build_report(data))
    log("capture_plan_check_finish", status=data["status"], blocker_count=len(data["blockers"]))

    print(f"CAPTURE PLAN CHECK: {data['display_status']}")
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

# End of script: riftscan_capture_plan_check.py
