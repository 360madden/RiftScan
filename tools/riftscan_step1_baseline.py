# version: 0.1.1
# total_character_count: computed_by_py_compile_test
# purpose: Run RiftScan Step 1 baseline capture and export a compact handoff with timestamped diagnostics.

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
LIVE_TEST_CMD = REPO_ROOT / "scripts" / "live-test-riftscan.cmd"
REPORTS_ROOT = REPO_ROOT / "reports" / "generated"
HANDOFF_ROOT = REPO_ROOT / "handoffs" / "current"
STEP_DIR = HANDOFF_ROOT / "step1_baseline"
LOG_PATH = HANDOFF_ROOT / "step1-log.jsonl"

STEP_LABEL = "baseline_standing"
STIMULUS = "passive_idle"

ALLOWLIST_RUN_FILES = {
    "freshness-verdict.json",
    "riftreader-read-player-coord-anchor.json",
    "capture-result.json",
    "verify-session-result.json",
    "analyze-session-result.json",
    "report-session-result.json",
    "delta-summary.json",
    "run-summary.json",
    "error.json",
}

EXCLUDED_SUFFIXES = {".bin", ".raw", ".dump", ".dmp"}


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


def log_event(event: str, **fields: Any) -> None:
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    entry = {"timestamp_utc": utc_now_iso(), "event": event, **fields}
    with LOG_PATH.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(entry, ensure_ascii=False) + "\n")


def read_json(path: Path) -> Any | None:
    if not path.is_file():
        log_event("json_missing", path=display_path(path))
        return None

    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        log_event("json_loaded", path=display_path(path))
        return data
    except Exception as exc:
        log_event("json_load_failed", path=display_path(path), error_type=type(exc).__name__, error=str(exc))
        return None


def reset_handoff() -> None:
    if HANDOFF_ROOT.exists():
        shutil.rmtree(HANDOFF_ROOT)
    STEP_DIR.mkdir(parents=True, exist_ok=True)
    log_event("handoff_reset", handoff_root=display_path(HANDOFF_ROOT))


def command_for_platform(args: list[str]) -> list[str]:
    if os.name == "nt":
        return ["cmd.exe", "/c", *args]
    return args


def run_command(args: list[str], timeout_seconds: int) -> dict[str, Any]:
    command = command_for_platform(args)
    started_utc = utc_now_iso()
    started_monotonic = time.monotonic()

    log_event("command_start", args=args, executed_args=command, timeout_seconds=timeout_seconds)

    try:
        result = subprocess.run(
            command,
            cwd=str(REPO_ROOT),
            text=True,
            capture_output=True,
            shell=False,
            timeout=timeout_seconds,
        )
        elapsed_ms = int((time.monotonic() - started_monotonic) * 1000)
        payload = {
            "schema_version": "riftscan.local_command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": result.returncode == 0,
            "returncode": result.returncode,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": command,
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
            "schema_version": "riftscan.local_command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "timed_out": True,
            "timeout_seconds": timeout_seconds,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": command,
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
            "schema_version": "riftscan.local_command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "elapsed_ms": elapsed_ms,
            "success": False,
            "returncode": None,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": command,
            "stdout": "",
            "stderr": "",
            "exception_type": type(exc).__name__,
            "exception_message": str(exc),
        }
        log_event("command_exception", error_type=type(exc).__name__, error=str(exc), elapsed_ms=elapsed_ms)
        return payload


def manual_live_test_dirs() -> set[Path]:
    if not REPORTS_ROOT.exists():
        return set()
    return {path for path in REPORTS_ROOT.iterdir() if path.is_dir() and path.name.startswith("manual-live-test-")}


def newest_manual_live_test_dir() -> Path | None:
    candidates = list(manual_live_test_dirs())
    if not candidates:
        log_event("run_dir_not_found")
        return None

    selected = max(candidates, key=lambda path: path.stat().st_mtime)
    log_event("run_dir_selected_newest", run_dir=display_path(selected))
    return selected


def select_new_run_dir(before: set[Path], after: set[Path]) -> Path | None:
    created = sorted(after - before, key=lambda path: path.stat().st_mtime)
    log_event(
        "run_dir_diff",
        before_count=len(before),
        after_count=len(after),
        created_count=len(created),
        created=[display_path(path) for path in created],
    )

    if not created:
        return None

    selected = created[-1]
    log_event("run_dir_selected_created", run_dir=display_path(selected))
    return selected


def resolve_repo_relative_or_absolute(value: Any) -> Path | None:
    if not isinstance(value, str) or not value.strip():
        return None

    path = Path(value)
    return path if path.is_absolute() else (REPO_ROOT / path).resolve()


def should_copy_file(path: Path, max_file_bytes: int) -> tuple[bool, str]:
    if not path.is_file():
        return False, "not_file"
    if path.suffix.lower() in EXCLUDED_SUFFIXES:
        return False, "excluded_suffix"
    if path.stat().st_size > max_file_bytes:
        return False, f"over_max_file_bytes_{max_file_bytes}"
    if path.name in ALLOWLIST_RUN_FILES:
        return True, "allowlisted_step1_artifact"
    return False, "not_allowlisted"


def copy_file(source: Path, destination: Path, included: list[dict[str, Any]], reason: str) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    item = {
        "source": display_path(source),
        "destination": display_path(destination),
        "bytes": source.stat().st_size,
        "reason": reason,
    }
    included.append(item)
    log_event("file_copied", **item)


def collect_run_files(run_dir: Path | None, max_file_bytes: int) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    included: list[dict[str, Any]] = []
    excluded: list[dict[str, Any]] = []

    if run_dir is None:
        log_event("collect_run_files_skipped")
        return included, excluded

    log_event("collect_run_files_start", run_dir=display_path(run_dir))
    for source in sorted(run_dir.rglob("*")):
        if not source.is_file():
            continue

        should_copy, reason = should_copy_file(source, max_file_bytes)
        if should_copy:
            copy_file(source, STEP_DIR / source.relative_to(run_dir), included, reason)
        else:
            item = {"source": display_path(source), "bytes": source.stat().st_size, "reason": reason}
            excluded.append(item)
            log_event("file_excluded", **item)

    log_event("collect_run_files_finish", included_count=len(included), excluded_count=len(excluded))
    return included, excluded


def collect_session_manifest(
    run_summary: Any,
    max_file_bytes: int,
    included: list[dict[str, Any]],
    excluded: list[dict[str, Any]],
) -> Path | None:
    if not isinstance(run_summary, dict):
        log_event("session_manifest_skipped_no_run_summary")
        return None

    session_path = resolve_repo_relative_or_absolute(run_summary.get("session_path"))
    if session_path is None:
        log_event("session_manifest_skipped_no_session_path")
        return None

    manifest = session_path / "manifest.json"
    if manifest.is_file() and manifest.stat().st_size <= max_file_bytes:
        copy_file(manifest, STEP_DIR / "session-manifest.json", included, "session_manifest")
    elif manifest.exists():
        item = {"source": display_path(manifest), "bytes": manifest.stat().st_size, "reason": "session_manifest_too_large"}
        excluded.append(item)
        log_event("file_excluded", **item)
    else:
        log_event("session_manifest_missing", manifest=display_path(manifest))

    snapshots_dir = session_path / "snapshots"
    if snapshots_dir.exists():
        for source in sorted(snapshots_dir.rglob("*")):
            if source.is_file():
                item = {"source": display_path(source), "bytes": source.stat().st_size, "reason": "session_snapshot_not_copied"}
                excluded.append(item)
                log_event("file_excluded", **item)

    return session_path


def get_bool(document: Any, key: str) -> bool | None:
    if isinstance(document, dict) and isinstance(document.get(key), bool):
        return document[key]
    return None


def build_summary(
    preflight_result: dict[str, Any],
    capture_result_command: dict[str, Any] | None,
    run_dir: Path | None,
    session_path: Path | None,
) -> dict[str, Any]:
    run_summary = read_json(STEP_DIR / "run-summary.json")
    capture_result = read_json(STEP_DIR / "capture-result.json")
    verify_result = read_json(STEP_DIR / "verify-session-result.json")
    analyze_result = read_json(STEP_DIR / "analyze-session-result.json")
    delta_summary = read_json(STEP_DIR / "delta-summary.json")

    summary = {
        "schema_version": "riftscan.step1_baseline_handoff.v1",
        "created_utc": utc_now_iso(),
        "step": 1,
        "label": STEP_LABEL,
        "stimulus": STIMULUS,
        "movement_attempted": False,
        "log_path": display_path(LOG_PATH),
        "preflight_command": ".\\scripts\\live-test-riftscan.cmd -PreflightOnly",
        "capture_command": ".\\scripts\\live-test-riftscan.cmd -Stimulus passive_idle -PreCaptureWaitMilliseconds 0",
        "preflight_success": bool(preflight_result.get("success")),
        "capture_command_success": None if capture_result_command is None else bool(capture_result_command.get("success")),
        "capture_success": get_bool(capture_result, "success"),
        "verify_success": get_bool(verify_result, "success"),
        "analyze_success": get_bool(analyze_result, "success"),
        "run_directory": None if run_dir is None else display_path(run_dir),
        "session_directory": None if session_path is None else display_path(session_path),
        "delta_interpretation": delta_summary.get("interpretation") if isinstance(delta_summary, dict) else None,
        "run_summary_status": run_summary.get("status") if isinstance(run_summary, dict) else None,
        "notes": [
            "This is Step 1 only: standing baseline capture.",
            "No movement was attempted by this script.",
            "No primary movement delta is acceptable for this baseline step.",
            "See step1-log.jsonl for timestamped diagnostic events.",
        ],
    }
    log_event("summary_built", summary=summary)
    return summary


def build_handoff_markdown(summary: dict[str, Any]) -> str:
    return f"""# RiftScan Step 1 Baseline Handoff

## Purpose

This handoff contains the result of a single standing baseline capture.

No movement was attempted.

## Commands

Preflight:

```powershell
.\\scripts\\live-test-riftscan.cmd -PreflightOnly
```

Capture:

```powershell
.\\scripts\\live-test-riftscan.cmd -Stimulus passive_idle -PreCaptureWaitMilliseconds 0
```

## Source run

- Run directory: `{summary.get("run_directory")}`
- Session directory: `{summary.get("session_directory")}`

## Result

- Preflight success: `{summary.get("preflight_success")}`
- Capture success: `{summary.get("capture_success")}`
- Verify success: `{summary.get("verify_success")}`
- Analyze success: `{summary.get("analyze_success")}`
- Delta interpretation: `{summary.get("delta_interpretation")}`

## Diagnostics

- Timestamped log: `{summary.get("log_path")}`

## Interpretation

For Step 1, this is a baseline capture at the player's current standing location.

For this baseline step, no primary movement delta is not automatically a failure.

## Files included

See `files-included.json`.

## Files excluded

See `files-excluded.json`.
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Run RiftScan Step 1 baseline capture and export a compact handoff.")
    parser.add_argument("--max-file-bytes", type=int, default=5_000_000)
    parser.add_argument("--timeout-seconds", type=int, default=600)
    parser.add_argument("--skip-preflight", action="store_true")
    args = parser.parse_args()

    reset_handoff()
    log_event(
        "script_start",
        script=display_path(SCRIPT_PATH),
        repo_root=display_path(REPO_ROOT),
        live_test_cmd=display_path(LIVE_TEST_CMD),
        max_file_bytes=args.max_file_bytes,
        timeout_seconds=args.timeout_seconds,
        skip_preflight=args.skip_preflight,
    )

    if not LIVE_TEST_CMD.exists():
        summary = {
            "schema_version": "riftscan.step1_baseline_handoff.v1",
            "created_utc": utc_now_iso(),
            "step": 1,
            "label": STEP_LABEL,
            "stimulus": STIMULUS,
            "movement_attempted": False,
            "log_path": display_path(LOG_PATH),
            "preflight_success": False,
            "capture_success": False,
            "error": f"Missing wrapper: {display_path(LIVE_TEST_CMD)}",
        }
        write_json(HANDOFF_ROOT / "step1-summary.json", summary)
        write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(summary))
        log_event("script_finish", success=False, reason="wrapper_missing")
        return 1

    if args.skip_preflight:
        preflight_result = {
            "schema_version": "riftscan.local_command_result.v1",
            "success": True,
            "returncode": 0,
            "skipped": True,
            "args": [str(LIVE_TEST_CMD), "-PreflightOnly"],
        }
        log_event("preflight_skipped")
    else:
        preflight_result = run_command([str(LIVE_TEST_CMD), "-PreflightOnly"], args.timeout_seconds)

    write_json(HANDOFF_ROOT / "preflight-command-result.json", preflight_result)

    if not preflight_result.get("success"):
        run_dir = newest_manual_live_test_dir()
        included, excluded = collect_run_files(run_dir, args.max_file_bytes)
        write_json(HANDOFF_ROOT / "files-included.json", included)
        write_json(HANDOFF_ROOT / "files-excluded.json", excluded)
        summary = build_summary(preflight_result, None, run_dir, None)
        write_json(HANDOFF_ROOT / "step1-summary.json", summary)
        write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(summary))
        log_event("script_finish", success=False, reason="preflight_failed")
        print(f"Preflight failed. Handoff written to {HANDOFF_ROOT}")
        return 1

    before_dirs = manual_live_test_dirs()
    capture_result = run_command(
        [str(LIVE_TEST_CMD), "-Stimulus", STIMULUS, "-PreCaptureWaitMilliseconds", "0"],
        args.timeout_seconds,
    )
    write_json(HANDOFF_ROOT / "capture-command-result.json", capture_result)

    after_dirs = manual_live_test_dirs()
    run_dir = select_new_run_dir(before_dirs, after_dirs) or newest_manual_live_test_dir()

    included, excluded = collect_run_files(run_dir, args.max_file_bytes)
    run_summary = read_json(STEP_DIR / "run-summary.json")
    session_path = collect_session_manifest(run_summary, args.max_file_bytes, included, excluded)

    write_json(HANDOFF_ROOT / "files-included.json", included)
    write_json(HANDOFF_ROOT / "files-excluded.json", excluded)

    summary = build_summary(preflight_result, capture_result, run_dir, session_path)
    write_json(HANDOFF_ROOT / "step1-summary.json", summary)
    write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(summary))

    if not capture_result.get("success"):
        log_event("script_finish", success=False, reason="capture_failed")
        print(f"Capture command failed. Handoff written to {HANDOFF_ROOT}")
        return 1

    log_event("script_finish", success=True)
    print(f"Step 1 baseline handoff written to {HANDOFF_ROOT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT_MARKER
