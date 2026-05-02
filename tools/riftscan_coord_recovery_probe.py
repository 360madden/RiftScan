# version: 0.1.0
# total_character_count: computed_by_self_check
# purpose: Probe fresh RIFT coordinate candidates by calling RiftReader as an external tool, then export a compact diagnostic handoff without modifying RiftReader.

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
DEFAULT_RIFTREADER_CMD = Path(r"C:\RIFT MODDING\RiftReader\scripts\run-reader.cmd")
HANDOFF_ROOT = REPO_ROOT / "handoffs" / "current" / "coord-recovery"
LOG_PATH = HANDOFF_ROOT / "step-log.jsonl"

DEFAULT_PROCESS_NAME = "rift_x64"
DEFAULT_SCAN_TOLERANCE = "0.05"
DEFAULT_SCAN_CONTEXT = "96"
DEFAULT_MAX_HITS = "32"

EXCLUDED_SUFFIXES = {".bin", ".raw", ".dump", ".dmp"}
ALLOWLIST_OUTPUT_FILES = {
    "RECOVERY_HANDOFF.md",
    "coord-recovery-summary.json",
    "riftreader-scan-readerbridge-player-coords.json",
    "command-result.json",
    "process-info.json",
    "step-log.jsonl",
    "files-included.json",
    "files-excluded.json",
}


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def display_path(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT.resolve())).replace("\\", "/")
    except ValueError:
        return str(path)


def write_text(path: Path, value: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8")


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding="utf-8")


def append_jsonl(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(value, ensure_ascii=False) + "\n")


def log_event(event: str, **fields: Any) -> None:
    append_jsonl(LOG_PATH, {"timestamp_utc": utc_now_iso(), "event": event, **fields})


def reset_handoff() -> None:
    if HANDOFF_ROOT.exists():
        shutil.rmtree(HANDOFF_ROOT)
    HANDOFF_ROOT.mkdir(parents=True, exist_ok=True)
    log_event("handoff_reset", handoff_root=display_path(HANDOFF_ROOT))


def command_for_platform(args: list[str]) -> list[str]:
    if os.name == "nt":
        return ["cmd.exe", "/c", *args]
    return args


def run_command(args: list[str], timeout_seconds: int, cwd: Path | None = None) -> dict[str, Any]:
    executed_args = command_for_platform(args)
    started_utc = utc_now_iso()
    started_monotonic = time.monotonic()
    working_directory = cwd or REPO_ROOT

    log_event(
        "command_start",
        args=args,
        executed_args=executed_args,
        cwd=str(working_directory),
        timeout_seconds=timeout_seconds,
    )

    try:
        result = subprocess.run(
            executed_args,
            cwd=str(working_directory),
            text=True,
            capture_output=True,
            shell=False,
            timeout=timeout_seconds,
        )
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": result.returncode == 0,
            "returncode": result.returncode,
            "cwd": str(working_directory),
            "args": args,
            "executed_args": executed_args,
            "stdout": result.stdout,
            "stderr": result.stderr,
        }
        log_event(
            "command_finish",
            success=payload["success"],
            returncode=result.returncode,
            elapsed_ms=elapsed_ms,
            stdout_length=len(result.stdout),
            stderr_length=len(result.stderr),
        )
        return payload

    except subprocess.TimeoutExpired as exc:
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "timed_out": True,
            "timeout_seconds": timeout_seconds,
            "cwd": str(working_directory),
            "args": args,
            "executed_args": executed_args,
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or "",
            "exception_type": type(exc).__name__,
            "exception_message": str(exc),
        }
        log_event("command_timeout", elapsed_ms=elapsed_ms, timeout_seconds=timeout_seconds)
        return payload

    except Exception as exc:
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "cwd": str(working_directory),
            "args": args,
            "executed_args": executed_args,
            "stdout": "",
            "stderr": "",
            "exception_type": type(exc).__name__,
            "exception_message": str(exc),
        }
        log_event("command_exception", error_type=type(exc).__name__, error=str(exc), elapsed_ms=elapsed_ms)
        return payload


def parse_json_stdout(command_result: dict[str, Any]) -> Any | None:
    stdout = command_result.get("stdout")
    if not isinstance(stdout, str) or not stdout.strip():
        log_event("stdout_json_missing")
        return None

    text = stdout.strip()
    try:
        parsed = json.loads(text)
        log_event("stdout_json_parsed", top_level_type=type(parsed).__name__)
        return parsed
    except json.JSONDecodeError:
        first_brace = text.find("{")
        last_brace = text.rfind("}")
        if first_brace >= 0 and last_brace > first_brace:
            try:
                parsed = json.loads(text[first_brace:last_brace + 1])
                log_event("stdout_json_parsed_from_slice", first_brace=first_brace, last_brace=last_brace)
                return parsed
            except json.JSONDecodeError as exc:
                log_event("stdout_json_parse_failed", error=str(exc))
                return None

        log_event("stdout_json_parse_failed", error="No JSON object found in stdout")
        return None


def resolve_process_by_powershell(process_name: str, timeout_seconds: int) -> tuple[dict[str, Any] | None, list[str]]:
    ps_script = (
        f"$items = @(Get-Process -Name '{process_name}' -ErrorAction SilentlyContinue | "
        "Select-Object Id,ProcessName,Path,MainWindowTitle,StartTime); "
        "$items | ConvertTo-Json -Depth 4"
    )
    result = run_command(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", ps_script], timeout_seconds)
    issues: list[str] = []

    if not result.get("success"):
        issues.append("process_lookup_command_failed")
        write_json(HANDOFF_ROOT / "process-info.json", {"command_result": result, "issues": issues})
        return None, issues

    stdout = result.get("stdout")
    if not isinstance(stdout, str) or not stdout.strip():
        issues.append(f"no_process_found_named_{process_name}")
        write_json(HANDOFF_ROOT / "process-info.json", {"command_result": result, "issues": issues})
        return None, issues

    try:
        parsed = json.loads(stdout)
    except json.JSONDecodeError as exc:
        issues.append(f"process_lookup_json_parse_failed:{exc}")
        write_json(HANDOFF_ROOT / "process-info.json", {"command_result": result, "issues": issues})
        return None, issues

    processes = parsed if isinstance(parsed, list) else [parsed]
    processes = [item for item in processes if isinstance(item, dict)]

    if len(processes) == 0:
        issues.append(f"no_process_found_named_{process_name}")
    elif len(processes) > 1:
        issues.append(f"multiple_processes_found_named_{process_name}; pass --pid")
        log_event("multiple_processes_found", count=len(processes), processes=processes)

    selected = processes[0] if len(processes) == 1 else None
    write_json(HANDOFF_ROOT / "process-info.json", {"processes": processes, "selected": selected, "issues": issues})
    return selected, issues


def process_info_from_explicit_pid(pid: int) -> dict[str, Any]:
    process_info = {"Id": pid, "ProcessName": None, "Path": None, "MainWindowTitle": None, "StartTime": None}
    write_json(HANDOFF_ROOT / "process-info.json", {"processes": [process_info], "selected": process_info, "issues": []})
    log_event("explicit_pid_selected", pid=pid)
    return process_info


def extract_candidate_like_values(value: Any) -> list[dict[str, Any]]:
    candidates: list[dict[str, Any]] = []

    def walk(node: Any, path: str) -> None:
        if isinstance(node, dict):
            address_keys = [
                key for key in node.keys()
                if "address" in key.lower() or key.lower() in {"hit", "pointer", "base"}
            ]
            if address_keys:
                item = {"path": path}
                for key in address_keys:
                    item[key] = node.get(key)
                candidates.append(item)
            for key, child in node.items():
                walk(child, f"{path}.{key}" if path else key)
        elif isinstance(node, list):
            for index, child in enumerate(node):
                walk(child, f"{path}[{index}]")

    walk(value, "")
    return candidates


def summarize_scan(parsed_scan: Any, command_result: dict[str, Any], args: argparse.Namespace, process_id: int | None) -> dict[str, Any]:
    candidates = extract_candidate_like_values(parsed_scan)
    hit_count = None

    if isinstance(parsed_scan, dict):
        for key in ("HitCount", "hit_count", "Hits", "hits", "CandidateCount", "candidate_count"):
            candidate_value = parsed_scan.get(key)
            if isinstance(candidate_value, int):
                hit_count = candidate_value
                break
            if isinstance(candidate_value, list):
                hit_count = len(candidate_value)
                break

    status = "command_failed"
    if command_result.get("success") and parsed_scan is None:
        status = "json_unparsed"
    elif command_result.get("success") and hit_count == 0:
        status = "no_coordinate_candidates"
    elif command_result.get("success") and (hit_count is None or hit_count > 0):
        status = "coordinate_candidates_observed"

    summary = {
        "schema_version": "riftscan.coord_recovery_probe_summary.v1",
        "created_utc": utc_now_iso(),
        "status": status,
        "final_truth_claim": False,
        "manual_confirmation_required": True,
        "process_id": process_id,
        "scan": {
            "command_success": bool(command_result.get("success")),
            "returncode": command_result.get("returncode"),
            "tolerance": args.scan_tolerance,
            "scan_context": args.scan_context,
            "max_hits": args.max_hits,
            "hit_count": hit_count,
            "candidate_like_value_count": len(candidates),
            "candidate_like_values": candidates[:64],
        },
        "interpretation": [
            "This is fresh coordinate candidate discovery only.",
            "Hits, if present, are coordinate candidates, not recovered coordinate truth.",
            "Old coord anchors are not required and are not trusted by this probe.",
            "RiftReader is called only as an external command-line tool; this script does not modify RiftReader.",
        ],
    }
    log_event("scan_summary_built", status=status, hit_count=hit_count, candidate_like_value_count=len(candidates))
    return summary


def should_include_output_file(path: Path, max_file_bytes: int) -> tuple[bool, str]:
    if not path.is_file():
        return False, "not_file"
    if path.suffix.lower() in EXCLUDED_SUFFIXES:
        return False, "excluded_suffix"
    if path.stat().st_size > max_file_bytes:
        return False, f"over_max_file_bytes_{max_file_bytes}"
    if path.name in ALLOWLIST_OUTPUT_FILES:
        return True, "allowlisted_coord_recovery_output"
    return False, "not_allowlisted"


def write_file_lists(max_file_bytes: int) -> None:
    included: list[dict[str, Any]] = []
    excluded: list[dict[str, Any]] = []

    for path in sorted(HANDOFF_ROOT.rglob("*")):
        if not path.is_file():
            continue
        include, reason = should_include_output_file(path, max_file_bytes)
        item = {
            "path": display_path(path),
            "bytes": path.stat().st_size,
            "reason": reason,
        }
        if include:
            included.append(item)
        else:
            excluded.append(item)

    write_json(HANDOFF_ROOT / "files-included.json", included)
    write_json(HANDOFF_ROOT / "files-excluded.json", excluded)
    log_event("file_lists_written", included_count=len(included), excluded_count=len(excluded))


def build_handoff_markdown(summary: dict[str, Any]) -> str:
    return f"""# RiftScan Fresh Coordinate Recovery Probe

## Purpose

This handoff contains a fresh coordinate candidate scan for RIFT after prior coordinate anchors/offsets became suspect.

This does not claim recovered coordinate truth.

## Scope

- RiftScan repository only.
- RiftReader is called as an external command-line tool.
- RiftReader files are not modified.
- No movement automation.
- No foreground automation.
- No old anchor is required.

## Result

- Status: `{summary.get("status")}`
- Process ID: `{summary.get("process_id")}`
- Command success: `{summary.get("scan", {}).get("command_success")}`
- Return code: `{summary.get("scan", {}).get("returncode")}`
- Hit count: `{summary.get("scan", {}).get("hit_count")}`
- Candidate-like value count: `{summary.get("scan", {}).get("candidate_like_value_count")}`

## Files

- `coord-recovery-summary.json`
- `riftreader-scan-readerbridge-player-coords.json`
- `command-result.json`
- `process-info.json`
- `step-log.jsonl`
- `files-included.json`
- `files-excluded.json`

## Interpretation

If hits are present, they are fresh coordinate candidates only. The next phase must validate candidates with capture and movement contrast before promoting any new anchor.
"""


def run_self_check(args: argparse.Namespace) -> int:
    reset_handoff()
    log_event("self_check_start")

    fake_scan = {
        "Mode": "float-sequence-scan",
        "ProcessId": 12345,
        "ProcessName": "rift_x64",
        "HitCount": 2,
        "Hits": [
            {"Address": "0x1000", "ContextAddress": "0x0FF0"},
            {"Address": "0x2000", "ContextAddress": "0x1FF0"},
        ],
    }
    fake_command = {
        "schema_version": "riftscan.command_result.v1",
        "success": True,
        "returncode": 0,
        "stdout": json.dumps(fake_scan),
        "stderr": "",
    }

    parsed = parse_json_stdout(fake_command)
    summary = summarize_scan(parsed, fake_command, args, process_id=12345)

    write_json(HANDOFF_ROOT / "riftreader-scan-readerbridge-player-coords.json", parsed)
    write_json(HANDOFF_ROOT / "command-result.json", fake_command)
    write_json(HANDOFF_ROOT / "process-info.json", {"selected": {"Id": 12345}, "issues": []})
    write_json(HANDOFF_ROOT / "coord-recovery-summary.json", summary)
    write_text(HANDOFF_ROOT / "RECOVERY_HANDOFF.md", build_handoff_markdown(summary))

    fake_raw = HANDOFF_ROOT / "fake.bin"
    fake_raw.write_bytes(b"raw")
    write_file_lists(args.max_file_bytes)

    included = json.loads((HANDOFF_ROOT / "files-included.json").read_text(encoding="utf-8"))
    excluded = json.loads((HANDOFF_ROOT / "files-excluded.json").read_text(encoding="utf-8"))

    raw_excluded = any(item.get("path", "").endswith("fake.bin") for item in excluded)
    summary_ok = summary["status"] == "coordinate_candidates_observed"
    included_ok = any(item.get("path", "").endswith("coord-recovery-summary.json") for item in included)

    if not (raw_excluded and summary_ok and included_ok):
        log_event("self_check_finish", success=False, raw_excluded=raw_excluded, summary_ok=summary_ok, included_ok=included_ok)
        return 1

    log_event("self_check_finish", success=True)
    print(f"Self-check passed. Handoff written to {HANDOFF_ROOT}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Probe fresh RIFT coordinate candidates without relying on old coord anchors.")
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--process-name", default=DEFAULT_PROCESS_NAME)
    parser.add_argument("--riftreader-cmd", default=str(DEFAULT_RIFTREADER_CMD))
    parser.add_argument("--scan-tolerance", default=DEFAULT_SCAN_TOLERANCE)
    parser.add_argument("--scan-context", default=DEFAULT_SCAN_CONTEXT)
    parser.add_argument("--max-hits", default=DEFAULT_MAX_HITS)
    parser.add_argument("--timeout-seconds", type=int, default=600)
    parser.add_argument("--max-file-bytes", type=int, default=5_000_000)
    parser.add_argument("--self-check", action="store_true")
    args = parser.parse_args()

    if args.self_check:
        return run_self_check(args)

    reset_handoff()
    log_event(
        "script_start",
        script=display_path(SCRIPT_PATH),
        repo_root=display_path(REPO_ROOT),
        process_name=args.process_name,
        explicit_pid=args.pid,
        riftreader_cmd=args.riftreader_cmd,
    )

    riftreader_cmd = Path(args.riftreader_cmd)
    if not riftreader_cmd.is_file():
        summary = {
            "schema_version": "riftscan.coord_recovery_probe_summary.v1",
            "created_utc": utc_now_iso(),
            "status": "riftreader_command_missing",
            "final_truth_claim": False,
            "manual_confirmation_required": True,
            "riftreader_cmd": str(riftreader_cmd),
        }
        write_json(HANDOFF_ROOT / "coord-recovery-summary.json", summary)
        write_text(HANDOFF_ROOT / "RECOVERY_HANDOFF.md", build_handoff_markdown(summary))
        write_file_lists(args.max_file_bytes)
        log_event("script_finish", success=False, reason="riftreader_command_missing")
        print(f"RiftReader command missing: {riftreader_cmd}")
        return 1

    if args.pid > 0:
        process_info = process_info_from_explicit_pid(args.pid)
        process_id = int(process_info["Id"])
    else:
        process_info, process_issues = resolve_process_by_powershell(args.process_name, args.timeout_seconds)
        if process_info is None or process_issues:
            summary = {
                "schema_version": "riftscan.coord_recovery_probe_summary.v1",
                "created_utc": utc_now_iso(),
                "status": "process_resolution_failed",
                "final_truth_claim": False,
                "manual_confirmation_required": True,
                "issues": process_issues,
            }
            write_json(HANDOFF_ROOT / "coord-recovery-summary.json", summary)
            write_text(HANDOFF_ROOT / "RECOVERY_HANDOFF.md", build_handoff_markdown(summary))
            write_file_lists(args.max_file_bytes)
            log_event("script_finish", success=False, reason="process_resolution_failed")
            print(f"Process resolution failed. Handoff written to {HANDOFF_ROOT}")
            return 1
        process_id = int(process_info["Id"])

    scan_args = [
        str(riftreader_cmd),
        "--pid",
        str(process_id),
        "--scan-readerbridge-player-coords",
        "--scan-tolerance",
        str(args.scan_tolerance),
        "--scan-context",
        str(args.scan_context),
        "--max-hits",
        str(args.max_hits),
        "--json",
    ]
    command_result = run_command(scan_args, args.timeout_seconds)
    write_json(HANDOFF_ROOT / "command-result.json", command_result)

    parsed_scan = parse_json_stdout(command_result)
    if parsed_scan is not None:
        write_json(HANDOFF_ROOT / "riftreader-scan-readerbridge-player-coords.json", parsed_scan)

    summary = summarize_scan(parsed_scan, command_result, args, process_id=process_id)
    write_json(HANDOFF_ROOT / "coord-recovery-summary.json", summary)
    write_text(HANDOFF_ROOT / "RECOVERY_HANDOFF.md", build_handoff_markdown(summary))
    write_file_lists(args.max_file_bytes)

    success = command_result.get("success") is True and parsed_scan is not None
    log_event("script_finish", success=success, status=summary["status"])

    print(f"Coordinate recovery handoff written to {HANDOFF_ROOT}")
    return 0 if success else 1


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT_MARKER
