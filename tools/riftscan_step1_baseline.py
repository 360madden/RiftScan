# version: 0.1.0
# total_character_count: 015445
# purpose: Run RiftScan Step 1 baseline standing capture, then export a compact ChatGPT-readable handoff without copying raw snapshot binaries.

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
LIVE_TEST_CMD = REPO_ROOT / "scripts" / "live-test-riftscan.cmd"
REPORTS_ROOT = REPO_ROOT / "reports" / "generated"
HANDOFF_ROOT = REPO_ROOT / "handoffs" / "current"
STEP_DIR = HANDOFF_ROOT / "step1_baseline"

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

RAW_OR_LARGE_SUFFIXES = {
    ".bin",
    ".raw",
    ".dump",
    ".dmp",
}


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def path_for_display(path: Path) -> str:
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


def read_json(path: Path) -> Any | None:
    if not path.exists() or not path.is_file():
        return None

    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception:
        return None


def reset_handoff() -> None:
    if HANDOFF_ROOT.exists():
        shutil.rmtree(HANDOFF_ROOT)

    STEP_DIR.mkdir(parents=True, exist_ok=True)


def command_for_platform(args: list[str]) -> list[str]:
    if os.name == "nt":
        return ["cmd.exe", "/c", *args]

    return args


def run_command(args: list[str]) -> dict[str, Any]:
    command = command_for_platform(args)
    started_utc = utc_now_iso()

    try:
        result = subprocess.run(
            command,
            cwd=str(REPO_ROOT),
            text=True,
            capture_output=True,
            shell=False,
        )

        return {
            "schema_version": "riftscan.local_command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
            "success": result.returncode == 0,
            "returncode": result.returncode,
            "cwd": str(REPO_ROOT),
            "args": args,
            "executed_args": command,
            "stdout": result.stdout,
            "stderr": result.stderr,
        }
    except Exception as exc:
        return {
            "schema_version": "riftscan.local_command_result.v1",
            "started_utc": started_utc,
            "finished_utc": utc_now_iso(),
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


def newest_manual_live_test_dir(after_timestamp: float | None = None) -> Path | None:
    if not REPORTS_ROOT.exists():
        return None

    candidates: list[Path] = []
    for path in REPORTS_ROOT.iterdir():
        if not path.is_dir() or not path.name.startswith("manual-live-test-"):
            continue

        if after_timestamp is not None and path.stat().st_mtime < after_timestamp:
            continue

        candidates.append(path)

    if not candidates:
        return None

    return max(candidates, key=lambda item: item.stat().st_mtime)


def resolve_path_from_run_summary(value: Any) -> Path | None:
    if not isinstance(value, str) or not value.strip():
        return None

    candidate = Path(value)
    if candidate.is_absolute():
        return candidate

    return (REPO_ROOT / candidate).resolve()


def should_copy_run_file(path: Path, max_file_bytes: int) -> tuple[bool, str]:
    if not path.is_file():
        return False, "not_file"

    if path.suffix.lower() in RAW_OR_LARGE_SUFFIXES:
        return False, "raw_or_dump_suffix"

    if path.stat().st_size > max_file_bytes:
        return False, f"over_max_file_bytes_{max_file_bytes}"

    if path.name in ALLOWLIST_RUN_FILES:
        return True, "allowlisted_step1_artifact"

    return False, "not_step1_allowlisted"


def copy_file(source: Path, destination: Path, included: list[dict[str, Any]], reason: str) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    included.append(
        {
            "source": path_for_display(source),
            "destination": path_for_display(destination),
            "bytes": source.stat().st_size,
            "reason": reason,
        }
    )


def collect_run_files(run_dir: Path | None, max_file_bytes: int) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    included: list[dict[str, Any]] = []
    excluded: list[dict[str, Any]] = []

    if run_dir is None:
        return included, excluded

    for source in sorted(run_dir.rglob("*")):
        if not source.is_file():
            continue

        should_copy, reason = should_copy_run_file(source, max_file_bytes)
        if should_copy:
            destination = STEP_DIR / source.relative_to(run_dir)
            copy_file(source, destination, included, reason)
        else:
            excluded.append(
                {
                    "source": path_for_display(source),
                    "bytes": source.stat().st_size,
                    "reason": reason,
                }
            )

    return included, excluded


def collect_session_manifest(run_summary: Any, max_file_bytes: int, included: list[dict[str, Any]], excluded: list[dict[str, Any]]) -> Path | None:
    if not isinstance(run_summary, dict):
        return None

    session_path = resolve_path_from_run_summary(run_summary.get("session_path"))
    if session_path is None:
        return None

    manifest = session_path / "manifest.json"
    if manifest.exists() and manifest.is_file():
        if manifest.stat().st_size <= max_file_bytes:
            copy_file(manifest, STEP_DIR / "session-manifest.json", included, "session_manifest")
        else:
            excluded.append(
                {
                    "source": path_for_display(manifest),
                    "bytes": manifest.stat().st_size,
                    "reason": f"session_manifest_over_max_file_bytes_{max_file_bytes}",
                }
            )

    snapshots_dir = session_path / "snapshots"
    if snapshots_dir.exists():
        for source in sorted(snapshots_dir.rglob("*")):
            if source.is_file():
                excluded.append(
                    {
                        "source": path_for_display(source),
                        "bytes": source.stat().st_size,
                        "reason": "raw_session_snapshot_not_copied",
                    }
                )

    return session_path


def get_nested_bool(document: Any, key: str) -> bool | None:
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

    delta_interpretation = None
    if isinstance(delta_summary, dict):
        delta_interpretation = delta_summary.get("interpretation")

    summary = {
        "schema_version": "riftscan.step1_baseline_handoff.v1",
        "created_utc": utc_now_iso(),
        "step": 1,
        "label": STEP_LABEL,
        "stimulus": STIMULUS,
        "movement_attempted": False,
        "preflight_command": ".\\scripts\\live-test-riftscan.cmd -PreflightOnly",
        "capture_command": ".\\scripts\\live-test-riftscan.cmd -Stimulus passive_idle -PreCaptureWaitMilliseconds 0",
        "preflight_success": bool(preflight_result.get("success")),
        "capture_command_success": None if capture_result_command is None else bool(capture_result_command.get("success")),
        "capture_success": get_nested_bool(capture_result, "success"),
        "verify_success": get_nested_bool(verify_result, "success"),
        "analyze_success": get_nested_bool(analyze_result, "success"),
        "run_directory": None if run_dir is None else path_for_display(run_dir),
        "session_directory": None if session_path is None else path_for_display(session_path),
        "delta_interpretation": delta_interpretation,
        "run_summary_status": run_summary.get("status") if isinstance(run_summary, dict) else None,
        "notes": [
            "This is Step 1 only: standing baseline capture.",
            "No movement was attempted by this script.",
            "No primary movement delta is acceptable for this baseline step.",
            "Raw session snapshot binaries are intentionally excluded from this handoff.",
        ],
    }

    return summary


def build_handoff_markdown(summary: dict[str, Any]) -> str:
    preflight_success = summary.get("preflight_success")
    capture_success = summary.get("capture_success")
    verify_success = summary.get("verify_success")
    analyze_success = summary.get("analyze_success")
    delta_interpretation = summary.get("delta_interpretation")
    run_directory = summary.get("run_directory")
    session_directory = summary.get("session_directory")

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

- Run directory: `{run_directory}`
- Session directory: `{session_directory}`

## Result

- Preflight success: `{preflight_success}`
- Capture success: `{capture_success}`
- Verify success: `{verify_success}`
- Analyze success: `{analyze_success}`
- Delta interpretation: `{delta_interpretation}`

## Interpretation

For Step 1, this is a baseline capture at the player's current standing location.

Expected result:

- The session should verify.
- Analysis should complete.
- The primary coordinate triplet may stay stable.
- `stimulus_not_observed_or_no_primary_triplet_delta` is not automatically a failure for this baseline step.

## Files included

See `files-included.json`.

## Files excluded

See `files-excluded.json`.

Raw memory snapshot binaries are intentionally excluded from the handoff.
"""


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run RiftScan Step 1 baseline standing capture and export a compact handoff."
    )
    parser.add_argument(
        "--max-file-bytes",
        type=int,
        default=5_000_000,
        help="Maximum individual artifact size copied into the handoff. Default: 5,000,000.",
    )
    parser.add_argument(
        "--skip-preflight",
        action="store_true",
        help="Skip the standalone preflight command. Not recommended for normal Step 1 runs.",
    )

    args = parser.parse_args()

    reset_handoff()

    if not LIVE_TEST_CMD.exists():
        error_summary = {
            "schema_version": "riftscan.step1_baseline_handoff.v1",
            "created_utc": utc_now_iso(),
            "step": 1,
            "label": STEP_LABEL,
            "stimulus": STIMULUS,
            "movement_attempted": False,
            "preflight_success": False,
            "capture_success": False,
            "error": f"Missing wrapper: {path_for_display(LIVE_TEST_CMD)}",
        }
        write_json(HANDOFF_ROOT / "step1-summary.json", error_summary)
        write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(error_summary))
        return 1

    preflight_result = {
        "schema_version": "riftscan.local_command_result.v1",
        "success": True,
        "returncode": 0,
        "skipped": True,
        "args": [str(LIVE_TEST_CMD), "-PreflightOnly"],
    }

    if not args.skip_preflight:
        preflight_result = run_command([str(LIVE_TEST_CMD), "-PreflightOnly"])

    write_json(HANDOFF_ROOT / "preflight-command-result.json", preflight_result)

    if not preflight_result.get("success"):
        latest_preflight_run = newest_manual_live_test_dir()
        included, excluded = collect_run_files(latest_preflight_run, args.max_file_bytes)
        write_json(HANDOFF_ROOT / "files-included.json", included)
        write_json(HANDOFF_ROOT / "files-excluded.json", excluded)

        summary = build_summary(
            preflight_result=preflight_result,
            capture_result_command=None,
            run_dir=latest_preflight_run,
            session_path=None,
        )
        write_json(HANDOFF_ROOT / "step1-summary.json", summary)
        write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(summary))
        print(f"Preflight failed. Handoff written to {HANDOFF_ROOT}")
        return 1

    before_capture_time = datetime.now().timestamp()
    capture_command_result = run_command(
        [
            str(LIVE_TEST_CMD),
            "-Stimulus",
            STIMULUS,
            "-PreCaptureWaitMilliseconds",
            "0",
        ]
    )

    write_json(HANDOFF_ROOT / "capture-command-result.json", capture_command_result)

    run_dir = newest_manual_live_test_dir(after_timestamp=before_capture_time)
    if run_dir is None:
        run_dir = newest_manual_live_test_dir()

    included, excluded = collect_run_files(run_dir, args.max_file_bytes)

    run_summary = read_json(STEP_DIR / "run-summary.json")
    session_path = collect_session_manifest(run_summary, args.max_file_bytes, included, excluded)

    write_json(HANDOFF_ROOT / "files-included.json", included)
    write_json(HANDOFF_ROOT / "files-excluded.json", excluded)

    summary = build_summary(
        preflight_result=preflight_result,
        capture_result_command=capture_command_result,
        run_dir=run_dir,
        session_path=session_path,
    )

    write_json(HANDOFF_ROOT / "step1-summary.json", summary)
    write_text(HANDOFF_ROOT / "STEP1_HANDOFF.md", build_handoff_markdown(summary))

    if not capture_command_result.get("success"):
        print(f"Capture command failed. Handoff written to {HANDOFF_ROOT}")
        return 1

    print(f"Step 1 baseline handoff written to {HANDOFF_ROOT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT_MARKER
