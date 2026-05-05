# Version: riftscan-patch-intake-v1.2.1
# Purpose: Local RiftScan Patch Intake Helper. Provides an always-on-top paste GUI plus gated validate/dry-run/apply/process/commit/push controls for machine-readable RiftScan clipboard patch payloads. No clipboard watcher, no service, no listener, no polling, no dot staging, no automatic commit, no automatic push.
# Total character count: 84636

from __future__ import annotations

import argparse
import base64
import gzip
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import textwrap
import tkinter as tk
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from tkinter import messagebox, scrolledtext, ttk
from typing import Any, Callable


APP_VERSION = "riftscan-patch-intake-v1.2.1"
MAGIC = "RIFTSCAN_CLIPBOARD_PATCH_V1"
MAGIC_CHUNKED = "RIFTSCAN_CHUNKED_PATCH_V1"
DEFAULT_REPO_ROOT = Path(__file__).resolve().parents[1]
STATE_DIR_REL = ".riftscan-local/patch-intake"
REPORT_DIR_REL = "handoffs/current/patch-intake"
ACCEPTED_LEDGER_NAME = "accepted-patches.json"
LAST_VALIDATION_JSON = "last-validation-result.json"
LAST_DRY_RUN_JSON = "last-dry-run-result.json"
LAST_APPLY_JSON = "last-apply-result.json"
LAST_PROCESS_JSON = "last-process-result.json"
LAST_COMMIT_JSON = "last-commit-result.json"
LAST_PUSH_JSON = "last-push-result.json"
PATCH_INTAKE_LOG_JSONL = "patch-intake-log.jsonl"
REPORT_MD = "PATCH_INTAKE_REPORT.md"

ALLOWED_PAYLOAD_TYPES = {"python_applier_base64_gzip"}
REQUIRED_FORBIDDEN_ACTIONS = {
    "git_add_dot",
    "auto_commit",
    "auto_push",
    "force_push",
    "service",
    "listener",
    "polling",
    "scheduled_task",
}
FORBIDDEN_PAYLOAD_PATTERNS = [
    r"\bgit\s+add\s+\.",
    r"\bgit\s+commit\b",
    r"\bgit\s+push\b",
    r"\bgit\s+reset\s+--hard\b",
    r"\bNew-Service\b",
    r"\bRegister-ScheduledTask\b",
    r"\bStart-Service\b",
    r"\bSet-Service\b",
    r"\bwhile\s+True\s*:",
]
ALLOWED_POST_APPLY_CHECKS = {
    "py_compile_target",
    "py_compile_operator",
    "py_compile_patch_intake",
    "patch_intake_self_test",
    "operator_marker_verify",
    "git_status_check",
}
FORBIDDEN_POST_APPLY_CHECKS = {
    "raw_command",
    "powershell_from_manifest",
    "cmd_from_manifest",
    "shell_from_manifest",
}
COMMIT_SUCCESS_CODES = {"PASS_PROCESSED", "PASS_APPLIED", "PASS_ALREADY_PATCHED"}


@dataclass
class ParsedPayload:
    manifest: dict[str, Any]
    payload_text: str
    payload_bytes: bytes
    raw_text: str


def utc_now() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def json_block(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False, sort_keys=True)


def repo_root_from_arg(raw: str | None) -> Path:
    if raw:
        return Path(raw).expanduser().resolve()
    return DEFAULT_REPO_ROOT.resolve()


def rel(repo_root: Path, path: Path) -> str:
    try:
        return path.resolve().relative_to(repo_root.resolve()).as_posix()
    except Exception:
        return str(path)


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json_block(value) + "\n", encoding="utf-8")


def read_json(path: Path, default: Any) -> Any:
    if not path.exists():
        return default
    try:
        return json.loads(path.read_text(encoding="utf-8", errors="replace"))
    except Exception:
        return default


def run_command(args: list[str], cwd: Path, timeout: int = 90) -> tuple[int, str, str]:
    try:
        completed = subprocess.run(
            args,
            cwd=str(cwd),
            capture_output=True,
            text=True,
            timeout=timeout,
            shell=False,
        )
        return completed.returncode, completed.stdout, completed.stderr
    except subprocess.TimeoutExpired as exc:
        return 124, exc.stdout or "", f"{exc.stderr or ''}\nTIMEOUT after {timeout} seconds"
    except Exception as exc:
        return 1, "", f"{type(exc).__name__}: {exc}"


def parse_version_tuple(version: str) -> tuple[int, ...]:
    match = re.search(r"v(\d+(?:\.\d+)+)", str(version))
    if not match:
        return ()
    return tuple(int(part) for part in match.group(1).split("."))


def is_version_newer(to_version: str, from_version: str) -> bool:
    if from_version == "none":
        return True
    to_tuple = parse_version_tuple(to_version)
    from_tuple = parse_version_tuple(from_version)
    return bool(to_tuple and from_tuple and to_tuple > from_tuple)


def parse_timestamp(value: str) -> datetime | None:
    text = str(value or "").strip()
    if not text:
        return None
    try:
        if text.endswith("Z"):
            return datetime.fromisoformat(text[:-1] + "+00:00")
        return datetime.fromisoformat(text)
    except Exception:
        try:
            return datetime.strptime(text, "%Y%m%dT%H%M%SZ").replace(tzinfo=UTC)
        except Exception:
            return None


def get_paths(repo_root: Path) -> dict[str, Path]:
    state_dir = repo_root / STATE_DIR_REL
    report_dir = repo_root / REPORT_DIR_REL
    return {
        "state_dir": state_dir,
        "report_dir": report_dir,
        "ledger": state_dir / ACCEPTED_LEDGER_NAME,
        "validation_json": report_dir / LAST_VALIDATION_JSON,
        "dry_run_json": report_dir / LAST_DRY_RUN_JSON,
        "apply_json": report_dir / LAST_APPLY_JSON,
        "process_json": report_dir / LAST_PROCESS_JSON,
        "commit_json": report_dir / LAST_COMMIT_JSON,
        "push_json": report_dir / LAST_PUSH_JSON,
        "event_log": report_dir / PATCH_INTAKE_LOG_JSONL,
        "report_md": report_dir / REPORT_MD,
        "staging_dir": state_dir / "staging",
        "logs_dir": state_dir / "logs",
    }


def load_ledger(repo_root: Path) -> dict[str, Any]:
    return read_json(get_paths(repo_root)["ledger"], {"accepted": [], "last_accepted_created_utc": None})


def save_ledger(repo_root: Path, ledger: dict[str, Any]) -> None:
    write_json(get_paths(repo_root)["ledger"], ledger)


def result_artifacts(repo_root: Path, *names: str) -> list[str]:
    paths = get_paths(repo_root)
    mapping = {
        "validation": paths["validation_json"],
        "dry_run": paths["dry_run_json"],
        "apply": paths["apply_json"],
        "process": paths["process_json"],
        "commit": paths["commit_json"],
        "push": paths["push_json"],
        "event_log": paths["event_log"],
        "report": paths["report_md"],
    }
    return [rel(repo_root, mapping[name]) for name in names if name in mapping]


def event_envelope(
    event: str,
    stage: str,
    status: str,
    code: str,
    package_id: str = "",
    target_file: str = "",
    artifact_paths: list[str] | None = None,
    extra: dict[str, Any] | None = None,
) -> dict[str, Any]:
    envelope: dict[str, Any] = {
        "schema_version": "riftscan.event_log.v1",
        "created_utc": utc_now(),
        "component": "patch_intake",
        "app_version": APP_VERSION,
        "event": event,
        "stage": stage,
        "status": status,
        "code": code,
        "package_id": package_id,
        "target_file": target_file,
        "artifact_paths": artifact_paths or [],
    }
    if extra:
        envelope["extra"] = extra
    return envelope


def append_event(
    repo_root: Path,
    event: str,
    stage: str,
    status: str,
    code: str,
    package_id: str = "",
    target_file: str = "",
    artifact_paths: list[str] | None = None,
    extra: dict[str, Any] | None = None,
) -> None:
    paths = get_paths(repo_root)
    paths["event_log"].parent.mkdir(parents=True, exist_ok=True)
    envelope = event_envelope(event, stage, status, code, package_id, target_file, artifact_paths, extra)
    with paths["event_log"].open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(envelope, ensure_ascii=False, sort_keys=True) + "\n")


def git_status(repo_root: Path) -> tuple[bool, list[str], str]:
    code, out, err = run_command(["git", "status", "--short"], cwd=repo_root, timeout=30)
    if code != 0:
        return False, [], err.strip() or out.strip()

    unsafe: list[str] = []
    lines = [line for line in out.splitlines() if line.strip()]
    for line in lines:
        if re.match(r"^\?\? \.riftscan-local/", line):
            continue
        if re.match(r"^\?\? handoffs/current/patch-intake/", line):
            continue
        if re.match(r"^ M handoffs/current/patch-intake/", line):
            continue
        if re.match(r"^\?\? tools/__pycache__/", line):
            continue
        if re.match(r"^\?\? scripts/__pycache__/", line):
            continue
        unsafe.append(line)

    return not unsafe, unsafe, out


def git_status_lines(repo_root: Path) -> tuple[int, list[str], str, str]:
    code, out, err = run_command(["git", "status", "--short"], cwd=repo_root, timeout=30)
    return code, [line for line in out.splitlines() if line.strip()], out, err


def parse_status_path(line: str) -> str:
    text = line[3:].strip() if len(line) >= 3 else line.strip()
    if " -> " in text:
        text = text.split(" -> ", 1)[1].strip()
    return text.replace("\\", "/")



def payload_encoding_diagnostics(payload_text: str, manifest: dict[str, Any] | None = None) -> dict[str, Any]:
    raw = payload_text or ""
    cleaned = "".join(raw.split())
    allowed = set("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=")
    invalid_positions = [index for index, char in enumerate(cleaned) if char not in allowed]
    first_invalid = invalid_positions[0] if invalid_positions else None
    first_padding = cleaned.find("=") if "=" in cleaned else -1
    terminal_padding = len(cleaned) - len(cleaned.rstrip("="))
    padding_before_terminal = False
    if first_padding != -1:
        padding_before_terminal = any(char != "=" for char in cleaned[first_padding:])
    diagnostics: dict[str, Any] = {
        "schema_version": "riftscan.payload_encoding_diagnostics.v1",
        "raw_length": len(raw),
        "cleaned_length": len(cleaned),
        "removed_whitespace_count": len(raw) - len(cleaned),
        "length_mod4": len(cleaned) % 4,
        "padding_count": cleaned.count("="),
        "terminal_padding_count": terminal_padding,
        "first_padding_index": first_padding,
        "padding_before_terminal": padding_before_terminal,
        "invalid_character_count": len(invalid_positions),
        "first_invalid_character_position": first_invalid,
        "encoded_payload_sha256": hashlib.sha256(cleaned.encode("ascii", errors="ignore")).hexdigest(),
        "encoded_payload_length": len(cleaned),
        "suggested_padding_needed": (4 - (len(cleaned) % 4)) % 4,
        "issues": [],
    }
    if manifest:
        expected_length = manifest.get("encoded_payload_length")
        expected_hash = str(manifest.get("encoded_payload_sha256", "")).lower()
        if expected_length is not None and int(expected_length) != len(cleaned):
            diagnostics["issues"].append("encoded_payload_length mismatch")
        if expected_hash and expected_hash != diagnostics["encoded_payload_sha256"]:
            diagnostics["issues"].append("encoded_payload_sha256 mismatch")
    if invalid_positions:
        diagnostics["issues"].append("invalid base64 character present")
    if terminal_padding > 2:
        diagnostics["issues"].append("too much terminal padding")
    if padding_before_terminal:
        diagnostics["issues"].append("padding appears before encoded payload end")
    if len(cleaned) % 4 != 0:
        diagnostics["issues"].append("encoded payload length is not divisible by 4")
    return diagnostics


def decode_base64_gzip_payload(payload_text: str, manifest: dict[str, Any] | None = None) -> tuple[bytes | None, dict[str, Any]]:
    diagnostics = payload_encoding_diagnostics(payload_text, manifest)
    cleaned = "".join((payload_text or "").split())
    if diagnostics["invalid_character_count"]:
        diagnostics["failure_code"] = "FAIL_BAD_PAYLOAD_CHARACTER"
        return None, diagnostics
    if diagnostics["padding_before_terminal"] or diagnostics["terminal_padding_count"] > 2:
        diagnostics["failure_code"] = "FAIL_BAD_PAYLOAD_PADDING"
        return None, diagnostics
    if diagnostics["length_mod4"] != 0:
        diagnostics["failure_code"] = "FAIL_BAD_PAYLOAD_PADDING"
        return None, diagnostics
    if manifest:
        expected_hash = str(manifest.get("encoded_payload_sha256", "")).lower()
        if expected_hash and expected_hash != diagnostics["encoded_payload_sha256"]:
            diagnostics["failure_code"] = "FAIL_ENCODED_PAYLOAD_HASH_MISMATCH"
            return None, diagnostics
    try:
        compressed = base64.b64decode(cleaned, validate=True)
    except Exception as exc:
        diagnostics["failure_code"] = "FAIL_BAD_PAYLOAD_ENCODING"
        diagnostics["decode_error"] = f"{type(exc).__name__}: {exc}"
        return None, diagnostics
    try:
        payload_bytes = gzip.decompress(compressed)
    except Exception as exc:
        diagnostics["failure_code"] = "FAIL_BAD_PAYLOAD_GZIP"
        diagnostics["gzip_error"] = f"{type(exc).__name__}: {exc}"
        return None, diagnostics
    diagnostics["decoded_payload_sha256"] = hashlib.sha256(payload_bytes).hexdigest()
    diagnostics["decoded_payload_length"] = len(payload_bytes)
    if manifest:
        expected_decoded = str(manifest.get("decoded_payload_sha256") or manifest.get("payload_sha256") or "").lower()
        if expected_decoded and expected_decoded != diagnostics["decoded_payload_sha256"]:
            diagnostics["failure_code"] = "FAIL_HASH_MISMATCH"
            return None, diagnostics
    diagnostics["failure_code"] = None
    return payload_bytes, diagnostics


def parse_chunked_payload(stripped: str) -> tuple[ParsedPayload | None, dict[str, Any]]:
    result: dict[str, Any] = {"status": "unknown", "code": "UNKNOWN", "issues": []}
    marker_chunks = "---CHUNKS---"
    marker_end = "---END---"
    if marker_chunks not in stripped or marker_end not in stripped:
        result["status"] = "fail"
        result["code"] = "FAIL_MISSING_PAYLOAD"
        result["issues"].append("Chunked payload markers are missing.")
        return None, result
    after_magic = stripped[len(MAGIC_CHUNKED):].strip()
    manifest_text, rest = after_magic.split(marker_chunks, 1)
    chunk_text, _ = rest.split(marker_end, 1)
    try:
        manifest = json.loads(manifest_text.strip())
    except Exception as exc:
        result["status"] = "fail"
        result["code"] = "FAIL_BAD_MANIFEST"
        result["issues"].append(f"{type(exc).__name__}: {exc}")
        return None, result
    chunk_header = re.compile(r"---CHUNK\s+(\d+)/(\d+)\s+sha256=([0-9a-fA-F]{64})\s+length=(\d+)---")
    matches = list(chunk_header.finditer(chunk_text))
    if not matches:
        result["status"] = "fail"
        result["code"] = "FAIL_MISSING_CHUNK"
        result["issues"].append("No chunk headers found.")
        return None, result
    chunk_map: dict[int, str] = {}
    diagnostics: dict[str, Any] = {"schema_version": "riftscan.chunked_payload_diagnostics.v1", "declared_total_chunks": None, "observed_chunk_count": len(matches), "chunks": [], "issues": []}
    declared_total = None
    for index, match in enumerate(matches):
        chunk_no = int(match.group(1)); total = int(match.group(2)); expected_hash = match.group(3).lower(); expected_length = int(match.group(4))
        if declared_total is None:
            declared_total = total; diagnostics["declared_total_chunks"] = total
        elif total != declared_total:
            diagnostics["issues"].append("inconsistent total chunk count")
        body_start = match.end(); body_end = matches[index + 1].start() if index + 1 < len(matches) else len(chunk_text)
        cleaned_body = "".join(chunk_text[body_start:body_end].split())
        actual_hash = hashlib.sha256(cleaned_body.encode("ascii", errors="ignore")).hexdigest(); actual_length = len(cleaned_body)
        chunk_info = {"chunk": chunk_no, "total": total, "expected_length": expected_length, "actual_length": actual_length, "expected_sha256": expected_hash, "actual_sha256": actual_hash, "status": "pass"}
        if chunk_no in chunk_map:
            chunk_info["status"] = "fail"; diagnostics["issues"].append(f"duplicate chunk {chunk_no}")
        if actual_length != expected_length:
            chunk_info["status"] = "fail"; diagnostics["issues"].append(f"chunk {chunk_no} length mismatch")
        if actual_hash != expected_hash:
            chunk_info["status"] = "fail"; diagnostics["issues"].append(f"chunk {chunk_no} hash mismatch")
        chunk_map[chunk_no] = cleaned_body; diagnostics["chunks"].append(chunk_info)
    if declared_total is None:
        result["status"] = "fail"; result["code"] = "FAIL_MISSING_CHUNK"; result["issues"].append("Chunk total could not be determined."); result["chunk_diagnostics"] = diagnostics; return None, result
    missing = [number for number in range(1, declared_total + 1) if number not in chunk_map]
    if missing:
        diagnostics["issues"].append("missing chunks: " + ", ".join(str(item) for item in missing))
    if diagnostics["issues"]:
        result["status"] = "fail"; result["code"] = "FAIL_CHUNK_HASH_MISMATCH" if any("hash mismatch" in item for item in diagnostics["issues"]) else "FAIL_MISSING_CHUNK"; result["issues"].extend(diagnostics["issues"]); result["chunk_diagnostics"] = diagnostics; return None, result
    encoded_payload = "".join(chunk_map[number] for number in range(1, declared_total + 1))
    payload_bytes, payload_diagnostics = decode_base64_gzip_payload(encoded_payload, manifest)
    result["chunk_diagnostics"] = diagnostics; result["payload_diagnostics"] = payload_diagnostics
    if payload_bytes is None:
        result["status"] = "fail"; result["code"] = str(payload_diagnostics.get("failure_code") or "FAIL_BAD_PAYLOAD_ENCODING")
        if payload_diagnostics.get("issues"):
            result["issues"].extend(payload_diagnostics["issues"])
        if payload_diagnostics.get("decode_error"):
            result["issues"].append(payload_diagnostics["decode_error"])
        if payload_diagnostics.get("gzip_error"):
            result["issues"].append(payload_diagnostics["gzip_error"])
        return None, result
    return ParsedPayload(manifest=manifest, payload_text=encoded_payload, payload_bytes=payload_bytes, raw_text=stripped), result

def parse_payload(text: str) -> tuple[ParsedPayload | None, dict[str, Any]]:
    raw = text or ""
    result: dict[str, Any] = {"status": "unknown", "code": "UNKNOWN", "issues": []}
    stripped = raw.strip()
    if stripped.startswith(MAGIC_CHUNKED):
        return parse_chunked_payload(stripped)
    if not stripped.startswith(MAGIC):
        result["status"] = "fail"; result["code"] = "FAIL_BAD_HEADER"; result["issues"].append(f"Payload must start with {MAGIC} or {MAGIC_CHUNKED}."); return None, result
    marker_payload = "---PAYLOAD---"; marker_end = "---END---"
    if marker_payload not in stripped or marker_end not in stripped:
        result["status"] = "fail"; result["code"] = "FAIL_MISSING_PAYLOAD"; result["issues"].append("Payload block markers are missing."); return None, result
    after_magic = stripped[len(MAGIC):].strip(); manifest_text, rest = after_magic.split(marker_payload, 1); payload_text, _ = rest.split(marker_end, 1)
    try:
        manifest = json.loads(manifest_text.strip())
    except Exception as exc:
        result["status"] = "fail"; result["code"] = "FAIL_BAD_MANIFEST"; result["issues"].append(f"{type(exc).__name__}: {exc}"); return None, result
    payload_bytes, diagnostics = decode_base64_gzip_payload(payload_text, manifest)
    result["payload_diagnostics"] = diagnostics
    if payload_bytes is None:
        result["status"] = "fail"; result["code"] = str(diagnostics.get("failure_code") or "FAIL_BAD_PAYLOAD_ENCODING")
        if diagnostics.get("issues"):
            result["issues"].extend(diagnostics["issues"])
        if diagnostics.get("decode_error"):
            result["issues"].append(diagnostics["decode_error"])
        if diagnostics.get("gzip_error"):
            result["issues"].append(diagnostics["gzip_error"])
        return None, result
    return ParsedPayload(manifest=manifest, payload_text=payload_text.strip(), payload_bytes=payload_bytes, raw_text=raw), result

def manifest_summary(manifest: dict[str, Any]) -> dict[str, Any]:
    commit = manifest.get("commit") if isinstance(manifest.get("commit"), dict) else None
    return {
        "magic": manifest.get("magic"),
        "schema_version": manifest.get("schema_version"),
        "package_id": manifest.get("package_id"),
        "created_utc": manifest.get("created_utc"),
        "repo": manifest.get("repo"),
        "component": manifest.get("component"),
        "from_version": manifest.get("from_version"),
        "to_version": manifest.get("to_version"),
        "target_repo_root": manifest.get("target_repo_root"),
        "target_file": manifest.get("target_file"),
        "payload_type": manifest.get("payload_type"),
        "applier": manifest.get("applier"),
        "post_apply_checks": manifest.get("post_apply_checks"),
        "commit": commit,
    }


def validate_post_apply_checks(manifest: dict[str, Any], issues: list[str]) -> list[str]:
    checks = manifest.get("post_apply_checks") or []
    if not isinstance(checks, list):
        issues.append("post_apply_checks must be a list when present.")
        return []
    normalized = [str(check).strip() for check in checks if str(check).strip()]
    forbidden = [check for check in normalized if check in FORBIDDEN_POST_APPLY_CHECKS]
    unknown = [check for check in normalized if check not in ALLOWED_POST_APPLY_CHECKS]
    if forbidden:
        issues.append("Forbidden post-apply checks requested: " + ", ".join(sorted(forbidden)))
    if unknown:
        issues.append("Unknown post-apply checks requested: " + ", ".join(sorted(unknown)))
    return normalized


def validate_commit_metadata_shape(manifest: dict[str, Any], issues: list[str], require_commit: bool = False) -> dict[str, Any] | None:
    commit = manifest.get("commit")
    if commit is None:
        if require_commit:
            issues.append("Manifest commit block is required.")
        return None
    if not isinstance(commit, dict):
        issues.append("Manifest commit block must be an object.")
        return None

    message = str(commit.get("message", "")).strip()
    stage_paths = commit.get("stage_paths")
    if not message:
        issues.append("commit.message is required.")
    if not isinstance(stage_paths, list) or not stage_paths:
        issues.append("commit.stage_paths must be a non-empty list.")
    else:
        for raw in stage_paths:
            path_text = str(raw).strip().replace("\\", "/")
            if not is_safe_stage_path(path_text):
                issues.append(f"Unsafe commit.stage_paths entry: {raw}")
    if bool(commit.get("push")):
        issues.append("commit.push must be false for v1.1.")
    return commit


def validate_parsed_payload(parsed: ParsedPayload, repo_root: Path, mode: str = "validate") -> dict[str, Any]:
    manifest = parsed.manifest
    payload_bytes = parsed.payload_bytes
    payload_source = payload_bytes.decode("utf-8", errors="replace")
    paths = get_paths(repo_root)
    ledger = load_ledger(repo_root)
    issues: list[str] = []
    warnings: list[str] = []

    package_id = str(manifest.get("package_id", ""))
    created_utc = str(manifest.get("created_utc", ""))
    from_version = str(manifest.get("from_version", ""))
    to_version = str(manifest.get("to_version", ""))
    target_root = Path(str(manifest.get("target_repo_root", ""))).expanduser()
    target_file_rel = str(manifest.get("target_file", ""))
    target_file = repo_root / target_file_rel if target_file_rel else repo_root
    payload_type = str(manifest.get("payload_type", ""))
    declared_hash = str(manifest.get("payload_sha256", "")).lower()
    actual_hash = hashlib.sha256(payload_bytes).hexdigest()
    forbidden_actions = set(manifest.get("forbidden_actions") or [])
    accepted = ledger.get("accepted") or []

    result: dict[str, Any] = {
        "schema_version": "riftscan.patch_intake_result.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "mode": mode,
        "status": "fail",
        "code": "FAIL_UNKNOWN",
        "package_id": package_id,
        "payload_type": payload_type,
        "from_version": from_version,
        "to_version": to_version,
        "target_repo_root": str(target_root),
        "repo_root": str(repo_root),
        "target_file": target_file_rel,
        "payload_sha256": actual_hash,
        "manifest": manifest_summary(manifest),
        "issues": issues,
        "warnings": warnings,
        "report_path": rel(repo_root, paths["report_md"]),
    }

    if manifest.get("magic") not in {MAGIC, MAGIC_CHUNKED}:
        issues.append("Manifest magic does not match.")
        result["code"] = "FAIL_BAD_MANIFEST"
    if manifest.get("schema_version") not in {"riftscan.clipboard_patch.v1", "riftscan.chunked_clipboard_patch.v1"}:
        issues.append("Manifest schema_version must be riftscan.clipboard_patch.v1 or riftscan.chunked_clipboard_patch.v1.")
        result["code"] = "FAIL_BAD_MANIFEST"
    if manifest.get("repo") != "RiftScan":
        issues.append("Manifest repo must be RiftScan.")
        result["code"] = "FAIL_WRONG_REPO"
    if not package_id:
        issues.append("package_id is required.")
        result["code"] = "FAIL_BAD_MANIFEST"
    if not created_utc or parse_timestamp(created_utc) is None:
        issues.append("created_utc is required and must parse.")
        result["code"] = "FAIL_BAD_MANIFEST"

    last_accepted = ledger.get("last_accepted_created_utc")
    created_dt = parse_timestamp(created_utc)
    last_dt = parse_timestamp(str(last_accepted)) if last_accepted else None
    if created_dt and last_dt and created_dt <= last_dt:
        issues.append("Patch timestamp is not newer than last accepted patch.")
        result["code"] = "FAIL_STALE_PATCH"

    if any(entry.get("package_id") == package_id for entry in accepted if isinstance(entry, dict)):
        issues.append("package_id was already accepted.")
        result["code"] = "FAIL_STALE_PATCH"

    if target_root.resolve() != repo_root.resolve():
        issues.append("target_repo_root does not match selected repo root.")
        result["code"] = "FAIL_WRONG_REPO"

    if payload_type not in ALLOWED_PAYLOAD_TYPES:
        issues.append(f"Unsupported payload_type: {payload_type}")
        result["code"] = "FAIL_BAD_MANIFEST"

    if declared_hash != actual_hash:
        issues.append("payload_sha256 does not match decoded payload.")
        result["code"] = "FAIL_HASH_MISMATCH"

    missing_forbidden = REQUIRED_FORBIDDEN_ACTIONS - forbidden_actions
    if missing_forbidden:
        issues.append("Manifest forbidden_actions is missing required entries: " + ", ".join(sorted(missing_forbidden)))
        result["code"] = "FAIL_FORBIDDEN_ACTION"

    for pattern in FORBIDDEN_PAYLOAD_PATTERNS:
        if re.search(pattern, payload_source, flags=re.IGNORECASE):
            issues.append(f"Payload source contains forbidden pattern: {pattern}")
            result["code"] = "FAIL_FORBIDDEN_ACTION"

    before_commit_issue_count = len(issues)
    validate_post_apply_checks(manifest, issues)
    validate_commit_metadata_shape(manifest, issues, require_commit=False)
    if len(issues) > before_commit_issue_count and any("Unsafe commit.stage_paths" in issue for issue in issues):
        result["code"] = "FAIL_COMMIT_UNSAFE_STAGE_PATH"
    elif len(issues) > before_commit_issue_count and result["code"] == "FAIL_UNKNOWN":
        result["code"] = "FAIL_COMMIT_MISSING_METADATA"

    if not target_file_rel:
        issues.append("target_file is required.")
        result["code"] = "FAIL_BAD_MANIFEST"
    elif not target_file.exists():
        issues.append(f"Target file does not exist: {target_file}")
        result["code"] = "FAIL_TARGET_FILE_MISSING"
    else:
        target_text = target_file.read_text(encoding="utf-8", errors="replace")
        if from_version and from_version not in target_text:
            already_markers = manifest.get("required_result_markers") or []
            if all(str(marker) in target_text for marker in already_markers):
                result["status"] = "pass"
                result["code"] = "PASS_ALREADY_PATCHED"
                result["issues"] = []
                result["warnings"] = warnings
                return result
            issues.append(f"from_version marker not found in target file: {from_version}")
            result["code"] = "FAIL_VERSION_FLOW"

        for marker in manifest.get("required_existing_markers") or []:
            if str(marker) not in target_text:
                issues.append(f"Required existing marker missing: {marker}")
                result["code"] = "FAIL_VERSION_FLOW"

    if from_version and to_version and not is_version_newer(to_version, from_version):
        issues.append("to_version is not newer than from_version.")
        result["code"] = "FAIL_VERSION_FLOW"

    git_ok, unsafe, git_text = git_status(repo_root)
    result["git_status_short"] = git_text
    if not git_ok:
        result["unsafe_git_status"] = unsafe
        issues.append("Repo has unsafe dirty files.")
        if result["code"] == "FAIL_UNKNOWN":
            result["code"] = "FAIL_REPO_DIRTY"

    if issues:
        if result["code"] == "FAIL_UNKNOWN":
            result["code"] = "FAIL_VALIDATION"
        return result

    result["status"] = "pass"
    result["code"] = "PASS_VALID_PATCH"
    return result


def write_report(repo_root: Path, result: dict[str, Any], kind: str) -> None:
    paths = get_paths(repo_root)
    paths["report_dir"].mkdir(parents=True, exist_ok=True)

    json_targets = {
        "validation": paths["validation_json"],
        "dry_run": paths["dry_run_json"],
        "apply": paths["apply_json"],
        "process": paths["process_json"],
        "commit": paths["commit_json"],
        "push": paths["push_json"],
    }
    if kind in json_targets:
        write_json(json_targets[kind], result)

    md = (
        "# RiftScan Patch Intake Report\n\n"
        f"Created UTC: `{utc_now()}`\n\n"
        f"Kind: `{kind}`\n\n"
        "```json\n"
        f"{json_block(result)}\n"
        "```\n"
    )
    paths["report_md"].write_text(md, encoding="utf-8")


def log_result(repo_root: Path, result: dict[str, Any], event: str, stage: str, artifacts: list[str]) -> None:
    append_event(
        repo_root,
        event=event,
        stage=stage,
        status=str(result.get("status", "unknown")),
        code=str(result.get("code", "UNKNOWN")),
        package_id=str(result.get("package_id", "")),
        target_file=str(result.get("target_file", "")),
        artifact_paths=artifacts,
    )


def validate_payload_text(text: str, repo_root: Path, kind: str = "validation") -> dict[str, Any]:
    parsed, parse_result = parse_payload(text)
    if parsed is None:
        result = {
            "schema_version": "riftscan.patch_intake_result.v1",
            "created_utc": utc_now(),
            "app_version": APP_VERSION,
            "mode": kind,
            **parse_result,
        }
        write_report(repo_root, result, kind)
        log_result(repo_root, result, "validate", "parse_payload", result_artifacts(repo_root, "validation", "event_log", "report"))
        return result

    result = validate_parsed_payload(parsed, repo_root, mode=kind)
    write_report(repo_root, result, kind)
    log_result(repo_root, result, "validate", "validate_manifest", result_artifacts(repo_root, "validation", "event_log", "report"))
    return result


def stage_payload(parsed: ParsedPayload, repo_root: Path, package_id: str) -> Path:
    paths = get_paths(repo_root)
    staging = paths["staging_dir"] / package_id
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir(parents=True, exist_ok=True)

    applier_name = str(parsed.manifest.get("applier", "patch-applier.py"))
    applier_path = staging / applier_name
    applier_path.write_bytes(parsed.payload_bytes)
    return applier_path


def dry_run_payload_text(text: str, repo_root: Path) -> dict[str, Any]:
    parsed, parse_result = parse_payload(text)
    if parsed is None:
        result = {
            "schema_version": "riftscan.patch_intake_result.v1",
            "created_utc": utc_now(),
            "app_version": APP_VERSION,
            "mode": "dry_run",
            **parse_result,
        }
        write_report(repo_root, result, "dry_run")
        log_result(repo_root, result, "dry_run", "parse_payload", result_artifacts(repo_root, "dry_run", "event_log", "report"))
        return result

    result = validate_parsed_payload(parsed, repo_root, mode="dry_run")
    if result.get("code") == "PASS_ALREADY_PATCHED":
        write_report(repo_root, result, "dry_run")
        log_result(repo_root, result, "dry_run", "already_patched", result_artifacts(repo_root, "dry_run", "event_log", "report"))
        return result
    if result.get("status") != "pass":
        write_report(repo_root, result, "dry_run")
        log_result(repo_root, result, "dry_run", "validate_manifest", result_artifacts(repo_root, "dry_run", "event_log", "report"))
        return result

    package_id = str(parsed.manifest.get("package_id"))
    applier_path = stage_payload(parsed, repo_root, package_id)

    code, out, err = run_command([sys.executable, "-m", "py_compile", str(applier_path)], cwd=repo_root, timeout=60)
    result["applier_path"] = rel(repo_root, applier_path)
    result["compile_exit_code"] = code
    result["compile_stdout"] = out
    result["compile_stderr"] = err
    if code != 0:
        result["status"] = "fail"
        result["code"] = "FAIL_APPLIER_COMPILE"
    else:
        result["status"] = "pass"
        result["code"] = "PASS_DRY_RUN"

    write_report(repo_root, result, "dry_run")
    log_result(repo_root, result, "dry_run", "compile_applier", result_artifacts(repo_root, "dry_run", "event_log", "report"))
    return result


def run_named_checks(parsed: ParsedPayload, repo_root: Path) -> list[dict[str, Any]]:
    manifest = parsed.manifest
    checks = manifest.get("post_apply_checks") or []
    results: list[dict[str, Any]] = []
    for check in [str(item).strip() for item in checks if str(item).strip()]:
        item: dict[str, Any] = {
            "check": check,
            "status": "fail",
            "code": "FAIL_CHECK_UNKNOWN",
            "stdout": "",
            "stderr": "",
            "exit_code": None,
        }
        if check not in ALLOWED_POST_APPLY_CHECKS:
            item["code"] = "FAIL_CHECK_NOT_ALLOWED"
            results.append(item)
            continue

        if check == "py_compile_target":
            target = repo_root / str(manifest.get("target_file", ""))
            if target.suffix.lower() != ".py":
                item.update({"status": "skip", "code": "SKIP_NOT_PYTHON_TARGET"})
            else:
                code, out, err = run_command([sys.executable, "-m", "py_compile", str(target)], cwd=repo_root, timeout=60)
                item.update({"exit_code": code, "stdout": out, "stderr": err, "status": "pass" if code == 0 else "fail", "code": "PASS_CHECK" if code == 0 else "FAIL_CHECK"})
        elif check == "py_compile_operator":
            target = repo_root / "tools" / "riftscan_operator_app.py"
            code, out, err = run_command([sys.executable, "-m", "py_compile", str(target)], cwd=repo_root, timeout=60)
            item.update({"exit_code": code, "stdout": out, "stderr": err, "status": "pass" if code == 0 else "fail", "code": "PASS_CHECK" if code == 0 else "FAIL_CHECK"})
        elif check == "py_compile_patch_intake":
            target = repo_root / "tools" / "riftscan_patch_intake_app.py"
            code, out, err = run_command([sys.executable, "-m", "py_compile", str(target)], cwd=repo_root, timeout=60)
            item.update({"exit_code": code, "stdout": out, "stderr": err, "status": "pass" if code == 0 else "fail", "code": "PASS_CHECK" if code == 0 else "FAIL_CHECK"})
        elif check == "patch_intake_self_test":
            target = repo_root / "tools" / "riftscan_patch_intake_app.py"
            code, out, err = run_command([sys.executable, str(target), "--self-test"], cwd=repo_root, timeout=180)
            item.update({"exit_code": code, "stdout": out, "stderr": err, "status": "pass" if code == 0 else "fail", "code": "PASS_CHECK" if code == 0 else "FAIL_CHECK"})
        elif check == "operator_marker_verify":
            target = repo_root / "tools" / "riftscan_operator_app.py"
            markers = manifest.get("operator_required_markers") or []
            text = target.read_text(encoding="utf-8", errors="replace") if target.exists() else ""
            missing = [str(marker) for marker in markers if str(marker) not in text]
            item["missing_markers"] = missing
            item["status"] = "pass" if not missing else "fail"
            item["code"] = "PASS_CHECK" if not missing else "FAIL_CHECK"
        elif check == "git_status_check":
            code, out, err = run_command(["git", "status", "--short"], cwd=repo_root, timeout=30)
            item.update({"exit_code": code, "stdout": out, "stderr": err, "status": "pass" if code == 0 else "fail", "code": "PASS_CHECK" if code == 0 else "FAIL_CHECK"})
        results.append(item)
    return results


def apply_after_dry(parsed: ParsedPayload, repo_root: Path, dry: dict[str, Any]) -> dict[str, Any]:
    if dry.get("code") == "PASS_ALREADY_PATCHED":
        result = dict(dry)
        result["mode"] = "apply"
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "already_patched", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result
    if dry.get("code") != "PASS_DRY_RUN":
        result = dict(dry)
        result["mode"] = "apply"
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "blocked_by_dry_run", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result

    applier_path = repo_root / str(dry.get("applier_path"))
    code, out, err = run_command([sys.executable, str(applier_path), "--repo-root", str(repo_root)], cwd=repo_root, timeout=120)

    result = dict(dry)
    result["mode"] = "apply"
    result["apply_exit_code"] = code
    result["apply_stdout"] = out
    result["apply_stderr"] = err

    if code != 0:
        result["status"] = "fail"
        result["code"] = "FAIL_APPLIER_RUNTIME"
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "run_applier", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result

    target_file = repo_root / str(parsed.manifest.get("target_file"))
    target_text = target_file.read_text(encoding="utf-8", errors="replace")
    missing = [
        str(marker)
        for marker in parsed.manifest.get("required_result_markers") or []
        if str(marker) not in target_text
    ]
    if missing:
        result["status"] = "fail"
        result["code"] = "FAIL_VERIFY_MARKERS"
        result["missing_result_markers"] = missing
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "verify_result_markers", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result

    check_results = run_named_checks(parsed, repo_root)
    result["post_apply_checks"] = check_results
    failed_checks = [check for check in check_results if check.get("status") == "fail"]
    if failed_checks:
        result["status"] = "fail"
        result["code"] = "FAIL_POST_APPLY_CHECK"
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "post_apply_checks", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result

    ledger = load_ledger(repo_root)
    accepted = ledger.get("accepted") or []
    accepted.append({
        "package_id": parsed.manifest.get("package_id"),
        "created_utc": parsed.manifest.get("created_utc"),
        "from_version": parsed.manifest.get("from_version"),
        "to_version": parsed.manifest.get("to_version"),
        "accepted_utc": utc_now(),
    })
    ledger["accepted"] = accepted
    ledger["last_accepted_created_utc"] = parsed.manifest.get("created_utc")
    save_ledger(repo_root, ledger)

    result["status"] = "pass"
    result["code"] = "PASS_APPLIED"
    write_report(repo_root, result, "apply")
    log_result(repo_root, result, "apply", "verify_result_markers", result_artifacts(repo_root, "apply", "event_log", "report"))
    return result


def apply_payload_text(text: str, repo_root: Path) -> dict[str, Any]:
    parsed, parse_result = parse_payload(text)
    if parsed is None:
        result = {
            "schema_version": "riftscan.patch_intake_result.v1",
            "created_utc": utc_now(),
            "app_version": APP_VERSION,
            "mode": "apply",
            **parse_result,
        }
        write_report(repo_root, result, "apply")
        log_result(repo_root, result, "apply", "parse_payload", result_artifacts(repo_root, "apply", "event_log", "report"))
        return result

    dry = dry_run_payload_text(text, repo_root)
    return apply_after_dry(parsed, repo_root, dry)


def write_process_result(repo_root: Path, result: dict[str, Any]) -> dict[str, Any]:
    write_report(repo_root, result, "process")
    log_result(repo_root, result, "process", str(result.get("stage", "write_last_process_result")), result_artifacts(repo_root, "process", "event_log", "report"))
    return result


def process_payload_text(
    text: str,
    repo_root: Path,
    prompt_callback: Callable[[], bool] | None = None,
) -> dict[str, Any]:
    parsed, parse_result = parse_payload(text)
    base: dict[str, Any] = {
        "schema_version": "riftscan.patch_intake_process_result.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "mode": "process",
        "status": "fail",
        "code": "FAIL_PROCESS",
        "stage": "parse_payload",
        "issues": [],
        "validation_result": None,
        "dry_run_result": None,
        "apply_result": None,
    }
    if parsed is None:
        base.update(parse_result)
        return write_process_result(repo_root, base)

    base["package_id"] = parsed.manifest.get("package_id")
    base["target_file"] = parsed.manifest.get("target_file")
    base["manifest"] = manifest_summary(parsed.manifest)

    validation = validate_parsed_payload(parsed, repo_root, mode="process_validate")
    write_report(repo_root, validation, "validation")
    log_result(repo_root, validation, "validate", "process_validate", result_artifacts(repo_root, "validation", "event_log", "report"))
    base["validation_result"] = validation
    if validation.get("code") == "PASS_ALREADY_PATCHED":
        base.update({"status": "pass", "code": "PASS_ALREADY_PATCHED", "stage": "validate"})
        return write_process_result(repo_root, base)
    if validation.get("status") != "pass":
        base.update({"status": "fail", "code": validation.get("code", "FAIL_VALIDATION"), "stage": "validate", "issues": validation.get("issues", [])})
        return write_process_result(repo_root, base)

    dry = dry_run_payload_text(text, repo_root)
    base["dry_run_result"] = dry
    if dry.get("code") == "PASS_ALREADY_PATCHED":
        base.update({"status": "pass", "code": "PASS_ALREADY_PATCHED", "stage": "dry_run"})
        return write_process_result(repo_root, base)
    if dry.get("code") != "PASS_DRY_RUN":
        base.update({"status": "fail", "code": dry.get("code", "FAIL_DRY_RUN"), "stage": "dry_run", "issues": dry.get("issues", [])})
        return write_process_result(repo_root, base)

    prompt_approved = True
    prompt_mode = "headless_no_prompt_callback"
    if prompt_callback is not None:
        prompt_mode = "gui_apply_prompt"
        prompt_approved = bool(prompt_callback())
    base["apply_prompt"] = {"mode": prompt_mode, "approved": prompt_approved}
    append_event(
        repo_root,
        event="process",
        stage="apply_prompt",
        status="pass" if prompt_approved else "fail",
        code="PASS_APPLY_PROMPT_APPROVED" if prompt_approved else "FAIL_APPLY_CANCELLED",
        package_id=str(parsed.manifest.get("package_id", "")),
        target_file=str(parsed.manifest.get("target_file", "")),
        artifact_paths=result_artifacts(repo_root, "event_log"),
    )
    if not prompt_approved:
        base.update({"status": "fail", "code": "FAIL_APPLY_CANCELLED", "stage": "apply_prompt", "issues": ["Operator cancelled apply after dry run."]})
        return write_process_result(repo_root, base)

    apply_result = apply_after_dry(parsed, repo_root, dry)
    base["apply_result"] = apply_result
    if apply_result.get("code") == "PASS_ALREADY_PATCHED":
        base.update({"status": "pass", "code": "PASS_ALREADY_PATCHED", "stage": "apply"})
    elif apply_result.get("code") == "PASS_APPLIED":
        base.update({"status": "pass", "code": "PASS_PROCESSED", "stage": "write_last_process_result"})
    else:
        base.update({"status": "fail", "code": apply_result.get("code", "FAIL_APPLY"), "stage": "apply", "issues": apply_result.get("issues", [])})
    return write_process_result(repo_root, base)


def make_payload(manifest: dict[str, Any], payload_source: str) -> str:
    payload_bytes = payload_source.encode("utf-8")
    manifest = dict(manifest)
    manifest["payload_sha256"] = hashlib.sha256(payload_bytes).hexdigest()
    encoded = base64.b64encode(gzip.compress(payload_bytes)).decode("ascii")
    wrapped = "\n".join(textwrap.wrap(encoded, width=76))
    return (
        f"{MAGIC}\n"
        f"{json_block(manifest)}\n"
        "---PAYLOAD---\n"
        f"{wrapped}\n"
        "---END---\n"
    )


def make_chunked_payload(manifest: dict[str, Any], payload_source: str, chunk_size: int = 1200) -> str:
    payload_bytes = payload_source.encode("utf-8")
    encoded = base64.b64encode(gzip.compress(payload_bytes)).decode("ascii")
    manifest = dict(manifest)
    manifest["magic"] = MAGIC_CHUNKED
    manifest["schema_version"] = "riftscan.chunked_clipboard_patch.v1"
    manifest["payload_sha256"] = hashlib.sha256(payload_bytes).hexdigest()
    manifest["decoded_payload_sha256"] = manifest["payload_sha256"]
    manifest["encoded_payload_sha256"] = hashlib.sha256(encoded.encode("ascii")).hexdigest()
    manifest["encoded_payload_length"] = len(encoded)
    chunks = [encoded[index:index + chunk_size] for index in range(0, len(encoded), chunk_size)]
    manifest["total_chunks"] = len(chunks)
    parts = [f"{MAGIC_CHUNKED}\n", json_block(manifest), "\n---CHUNKS---\n"]
    for number, chunk in enumerate(chunks, start=1):
        chunk_hash = hashlib.sha256(chunk.encode("ascii")).hexdigest()
        parts.append(f"---CHUNK {number}/{len(chunks)} sha256={chunk_hash} length={len(chunk)}---\n")
        parts.append("\n".join(textwrap.wrap(chunk, width=76)) + "\n")
    parts.append("---END---\n")
    return "".join(parts)


def make_example_payload(repo_root: Path, valid: bool = True, with_commit: bool = False) -> str:
    created = utc_now()
    target_file = "tools/riftscan_patch_intake_app.py"
    from_version = APP_VERSION
    to_version = "riftscan-patch-intake-v1.1.1"
    applier = (
        "from __future__ import annotations\n"
        "import argparse\n"
        "from pathlib import Path\n"
        "\n"
        "def main() -> int:\n"
        "    parser = argparse.ArgumentParser()\n"
        "    parser.add_argument('--repo-root', required=True)\n"
        "    args = parser.parse_args()\n"
        "    path = Path(args.repo_root) / 'tools' / 'riftscan_patch_intake_app.py'\n"
        "    text = path.read_text(encoding='utf-8', errors='replace')\n"
        "    if 'riftscan-patch-intake-v1.1.1' not in text:\n"
        "        text = text.replace('riftscan-patch-intake-v1.1.0', 'riftscan-patch-intake-v1.1.1')\n"
        "        path.write_text(text, encoding='utf-8')\n"
        "    return 0\n"
        "\n"
        "if __name__ == '__main__':\n"
        "    raise SystemExit(main())\n"
    )
    manifest = {
        "magic": MAGIC,
        "schema_version": "riftscan.clipboard_patch.v1",
        "package_id": "example-valid-patch" if valid else "example-invalid-patch",
        "created_utc": created,
        "repo": "RiftScan",
        "component": "patch-intake",
        "from_version": from_version,
        "to_version": to_version,
        "target_repo_root": str(repo_root),
        "target_file": target_file,
        "payload_type": "python_applier_base64_gzip",
        "applier": "patch-applier.py",
        "required_existing_markers": [from_version],
        "required_result_markers": [to_version],
        "post_apply_checks": ["py_compile_patch_intake", "git_status_check"],
        "forbidden_actions": sorted(REQUIRED_FORBIDDEN_ACTIONS),
    }
    if with_commit:
        manifest["commit"] = {
            "message": "Self-test example commit",
            "stage_paths": ["tools/riftscan_patch_intake_app.py", "handoffs/current/patch-intake/"],
            "push": False,
        }
    if not valid:
        manifest["repo"] = "WrongRepo"
    return make_payload(manifest, applier)


def is_safe_stage_path(path_text: str) -> bool:
    text = str(path_text or "").strip().replace("\\", "/")
    if not text or text == ".":
        return False
    if text.startswith("/") or re.match(r"^[A-Za-z]:", text):
        return False
    if ".." in Path(text).parts:
        return False
    if any(char in text for char in "*?[]"):
        return False
    if text.startswith(".riftscan-local/") or text == ".riftscan-local":
        return False
    return True


def path_is_covered(path_text: str, stage_paths: list[str]) -> bool:
    normalized = path_text.strip().replace("\\", "/")
    for stage in stage_paths:
        item = stage.strip().replace("\\", "/")
        if not item:
            continue
        if normalized == item.rstrip("/"):
            return True
        if item.endswith("/") and normalized.startswith(item):
            return True
        if normalized.startswith(item.rstrip("/") + "/"):
            return True
    return False


def get_last_successful_patch_result(repo_root: Path) -> tuple[dict[str, Any] | None, str]:
    paths = get_paths(repo_root)
    for name, path in [("process", paths["process_json"]), ("apply", paths["apply_json"])]:
        result = read_json(path, None)
        if isinstance(result, dict):
            code = str(result.get("code", ""))
            apply_result = result.get("apply_result") if isinstance(result.get("apply_result"), dict) else {}
            apply_code = str(apply_result.get("code", ""))
            if code in COMMIT_SUCCESS_CODES or apply_code in COMMIT_SUCCESS_CODES:
                return result, name
    return None, ""


def validate_commit_request(repo_root: Path, last_result: dict[str, Any] | None) -> tuple[bool, list[str], dict[str, Any] | None]:
    issues: list[str] = []
    if not last_result:
        return False, ["No successful process/apply result exists."], None

    manifest = last_result.get("manifest")
    if not isinstance(manifest, dict):
        apply_result = last_result.get("apply_result")
        if isinstance(apply_result, dict) and isinstance(apply_result.get("manifest"), dict):
            manifest = apply_result.get("manifest")
    if not isinstance(manifest, dict):
        return False, ["Last result has no manifest metadata."], None

    commit = validate_commit_metadata_shape(manifest, issues, require_commit=True)
    if commit is None:
        return False, issues or ["Commit metadata is missing."], None

    stage_paths = [str(item).strip().replace("\\", "/") for item in commit.get("stage_paths", [])]
    code, lines, out, err = git_status_lines(repo_root)
    if code != 0:
        return False, [err.strip() or out.strip() or "Unable to read git status."], commit

    unexpected: list[str] = []
    for line in lines:
        path = parse_status_path(line)
        if path.startswith(".riftscan-local/"):
            continue
        if path.startswith("tools/__pycache__/") or path.startswith("scripts/__pycache__/"):
            continue
        if not path_is_covered(path, stage_paths):
            unexpected.append(line)

    if unexpected:
        issues.append("Repo has unexpected dirty files outside commit.stage_paths.")
    if issues:
        return False, issues + unexpected, commit
    return True, [], commit


def commit_result(repo_root: Path) -> dict[str, Any]:
    last_result, source = get_last_successful_patch_result(repo_root)
    ok, issues, commit = validate_commit_request(repo_root, last_result)
    result: dict[str, Any] = {
        "schema_version": "riftscan.patch_intake_commit_result.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "mode": "commit",
        "status": "fail",
        "code": "FAIL_COMMIT",
        "source_result": source,
        "issues": issues,
    }
    if not last_result:
        result["code"] = "FAIL_COMMIT_WITHOUT_APPLY"
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "preconditions", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result
    if commit is None:
        result["code"] = "FAIL_COMMIT_MISSING_METADATA"
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "metadata", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result
    if issues:
        if any("Unsafe" in issue for issue in issues):
            result["code"] = "FAIL_COMMIT_UNSAFE_STAGE_PATH"
        else:
            result["code"] = "FAIL_REPO_DIRTY"
        result["commit"] = commit
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "preconditions", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result

    stage_paths = [str(item).strip().replace("\\", "/") for item in commit.get("stage_paths", [])]
    add_args = ["git", "add", "--"] + stage_paths
    add_code, add_out, add_err = run_command(add_args, cwd=repo_root, timeout=60)
    result["stage_paths"] = stage_paths
    result["stage_exit_code"] = add_code
    result["stage_stdout"] = add_out
    result["stage_stderr"] = add_err
    if add_code != 0:
        result["code"] = "FAIL_COMMIT"
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "stage_paths", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result

    diff_code, diff_out, diff_err = run_command(["git", "diff", "--cached", "--name-status"], cwd=repo_root, timeout=30)
    result["cached_diff_exit_code"] = diff_code
    result["cached_diff_name_status"] = diff_out
    result["cached_diff_stderr"] = diff_err
    if diff_code != 0 or not diff_out.strip():
        result["code"] = "FAIL_STAGED_DIFF_UNEXPECTED"
        result["issues"] = ["No staged diff was found."] if not diff_out.strip() else [diff_err.strip()]
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "cached_diff", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result

    commit_code, commit_out, commit_err = run_command(["git", "commit", "-m", str(commit.get("message"))], cwd=repo_root, timeout=90)
    result["commit_exit_code"] = commit_code
    result["commit_stdout"] = commit_out
    result["commit_stderr"] = commit_err
    if commit_code != 0:
        result["code"] = "FAIL_COMMIT"
        write_report(repo_root, result, "commit")
        log_result(repo_root, result, "commit", "create_commit", result_artifacts(repo_root, "commit", "event_log", "report"))
        return result

    head_code, head_out, head_err = run_command(["git", "rev-parse", "HEAD"], cwd=repo_root, timeout=30)
    result["head_exit_code"] = head_code
    result["commit_sha"] = head_out.strip() if head_code == 0 else ""
    result["head_stderr"] = head_err
    result["status"] = "pass"
    result["code"] = "PASS_COMMITTED"
    result["issues"] = []
    write_report(repo_root, result, "commit")
    log_result(repo_root, result, "commit", "create_commit", result_artifacts(repo_root, "commit", "event_log", "report"))
    return result


def push_verify_remote(repo_root: Path, simulate: bool = False) -> dict[str, Any]:
    paths = get_paths(repo_root)
    last_commit = read_json(paths["commit_json"], {})
    result: dict[str, Any] = {
        "schema_version": "riftscan.patch_intake_push_result.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "mode": "push",
        "status": "fail",
        "code": "FAIL_PUSH",
        "issues": [],
    }
    if not isinstance(last_commit, dict) or last_commit.get("code") != "PASS_COMMITTED":
        result["code"] = "FAIL_PUSH_WITHOUT_COMMIT"
        result["issues"] = ["No successful commit result exists."]
        write_report(repo_root, result, "push")
        log_result(repo_root, result, "push", "preconditions", result_artifacts(repo_root, "push", "event_log", "report"))
        return result

    local_code, local_out, local_err = run_command(["git", "rev-parse", "HEAD"], cwd=repo_root, timeout=30)
    result["local_head_exit_code"] = local_code
    result["local_head"] = local_out.strip()
    result["local_head_stderr"] = local_err
    if local_code != 0:
        result["code"] = "FAIL_REMOTE_VERIFY"
        result["issues"] = [local_err.strip() or "Unable to read local HEAD."]
        write_report(repo_root, result, "push")
        log_result(repo_root, result, "verify_remote", "local_head", result_artifacts(repo_root, "push", "event_log", "report"))
        return result

    if simulate:
        result["status"] = "pass"
        result["code"] = "PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED"
        result["simulated"] = True
        write_report(repo_root, result, "push")
        log_result(repo_root, result, "verify_remote", "simulated", result_artifacts(repo_root, "push", "event_log", "report"))
        return result

    push_code, push_out, push_err = run_command(["git", "push"], cwd=repo_root, timeout=120)
    result["push_exit_code"] = push_code
    result["push_stdout"] = push_out
    result["push_stderr"] = push_err
    append_event(
        repo_root,
        event="push",
        stage="push_current_branch",
        status="pass" if push_code == 0 else "fail",
        code="PASS_PUSH" if push_code == 0 else "FAIL_PUSH_REJECTED",
        artifact_paths=result_artifacts(repo_root, "event_log"),
    )
    if push_code != 0:
        result["code"] = "FAIL_PUSH_REJECTED"
        result["issues"] = [push_err.strip() or push_out.strip()]
        write_report(repo_root, result, "push")
        log_result(repo_root, result, "push", "push_current_branch", result_artifacts(repo_root, "push", "event_log", "report"))
        return result

    remote_code, remote_out, remote_err = run_command(["git", "ls-remote", "origin", "main"], cwd=repo_root, timeout=60)
    result["remote_exit_code"] = remote_code
    result["remote_stdout"] = remote_out
    result["remote_stderr"] = remote_err
    remote_sha = remote_out.split()[0] if remote_code == 0 and remote_out.strip() else ""
    result["remote_main_sha"] = remote_sha
    if remote_sha != result["local_head"]:
        result["code"] = "FAIL_REMOTE_VERIFY"
        result["issues"] = ["Remote main does not match local HEAD."]
        write_report(repo_root, result, "push")
        log_result(repo_root, result, "verify_remote", "compare_remote_main", result_artifacts(repo_root, "push", "event_log", "report"))
        return result

    result["status"] = "pass"
    result["code"] = "PASS_PUSHED_AND_VERIFIED"
    write_report(repo_root, result, "push")
    log_result(repo_root, result, "verify_remote", "compare_remote_main", result_artifacts(repo_root, "push", "event_log", "report"))
    return result


def run_self_test() -> tuple[bool, dict[str, Any]]:
    tests: list[dict[str, Any]] = []

    def record(name: str, expected: str, actual: dict[str, Any]) -> None:
        tests.append({
            "name": name,
            "expected": expected,
            "actual": actual.get("code"),
            "pass": actual.get("code") == expected,
            "issues": actual.get("issues"),
        })

    with tempfile.TemporaryDirectory(prefix="riftscan_patch_intake_selftest_") as tmp:
        repo = Path(tmp) / "Riftscan"
        tools = repo / "tools"
        tools.mkdir(parents=True)
        target = tools / "dummy_target.py"
        target.write_text("# Version: riftscan-dummy-v1.0.0\nprint('dummy')\n", encoding="utf-8")
        handoff_root = repo / "handoffs" / "current"
        handoff_root.mkdir(parents=True, exist_ok=True)
        (handoff_root / ".gitkeep").write_text("", encoding="utf-8")

        run_command(["git", "init"], cwd=repo, timeout=30)
        run_command(["git", "config", "user.email", "selftest@example.local"], cwd=repo, timeout=30)
        run_command(["git", "config", "user.name", "Self Test"], cwd=repo, timeout=30)
        run_command(["git", "add", "--", "tools/dummy_target.py", "handoffs/current/.gitkeep"], cwd=repo, timeout=30)
        run_command(["git", "commit", "-m", "init"], cwd=repo, timeout=30)

        base_manifest = {
            "magic": MAGIC,
            "schema_version": "riftscan.clipboard_patch.v1",
            "package_id": "dummy-v100-to-v101",
            "created_utc": "2026-05-04T17:00:00Z",
            "repo": "RiftScan",
            "component": "self-test",
            "from_version": "riftscan-dummy-v1.0.0",
            "to_version": "riftscan-dummy-v1.0.1",
            "target_repo_root": str(repo),
            "target_file": "tools/dummy_target.py",
            "payload_type": "python_applier_base64_gzip",
            "applier": "patch-applier.py",
            "required_existing_markers": ["riftscan-dummy-v1.0.0"],
            "required_result_markers": ["riftscan-dummy-v1.0.1"],
            "post_apply_checks": ["py_compile_target", "git_status_check"],
            "forbidden_actions": sorted(REQUIRED_FORBIDDEN_ACTIONS),
        }
        commit_manifest = dict(base_manifest)
        commit_manifest["commit"] = {
            "message": "Self-test commit result",
            "stage_paths": ["tools/dummy_target.py", "handoffs/current/patch-intake/"],
            "push": False,
        }
        unsafe_commit_manifest = dict(base_manifest)
        unsafe_commit_manifest["package_id"] = "dummy-unsafe-commit"
        unsafe_commit_manifest["commit"] = {
            "message": "Unsafe self-test commit result",
            "stage_paths": ["."],
            "push": False,
        }
        missing_commit_manifest = dict(base_manifest)
        missing_commit_manifest["package_id"] = "dummy-missing-commit"

        applier = (
            "from __future__ import annotations\n"
            "import argparse\n"
            "from pathlib import Path\n"
            "def main() -> int:\n"
            "    parser = argparse.ArgumentParser()\n"
            "    parser.add_argument('--repo-root', required=True)\n"
            "    args = parser.parse_args()\n"
            "    path = Path(args.repo_root) / 'tools' / 'dummy_target.py'\n"
            "    text = path.read_text(encoding='utf-8')\n"
            "    text = text.replace('riftscan-dummy-v1.0.0', 'riftscan-dummy-v1.0.1')\n"
            "    path.write_text(text, encoding='utf-8')\n"
            "    return 0\n"
            "if __name__ == '__main__':\n"
            "    raise SystemExit(main())\n"
        )
        valid_payload = make_payload(base_manifest, applier)
        commit_payload = make_payload(commit_manifest, applier)
        unsafe_commit_payload = make_payload(unsafe_commit_manifest, applier)

        record("empty payload", "FAIL_BAD_HEADER", validate_payload_text("", repo))
        record("wrong header", "FAIL_BAD_HEADER", validate_payload_text("NOPE\n{}", repo))
        record("bad json", "FAIL_BAD_MANIFEST", validate_payload_text(f"{MAGIC}\n{{bad\n---PAYLOAD---\nabc\n---END---", repo))
        record("missing payload", "FAIL_MISSING_PAYLOAD", validate_payload_text(f"{MAGIC}\n{{}}", repo))

        mismatch_manifest = dict(base_manifest)
        mismatch_manifest["package_id"] = "dummy-hash-mismatch"
        mismatch_manifest["payload_sha256"] = "0" * 64
        compressed = base64.b64encode(gzip.compress(applier.encode("utf-8"))).decode("ascii")
        mismatch_payload = f"{MAGIC}\n{json_block(mismatch_manifest)}\n---PAYLOAD---\n{compressed}\n---END---\n"
        record("hash mismatch", "FAIL_HASH_MISMATCH", validate_payload_text(mismatch_payload, repo))

        ledger = {
            "accepted": [],
            "last_accepted_created_utc": "2026-05-04T18:00:00Z",
        }
        save_ledger(repo, ledger)
        record("stale timestamp", "FAIL_STALE_PATCH", validate_payload_text(valid_payload, repo))
        save_ledger(repo, {"accepted": [], "last_accepted_created_utc": None})

        wrong_repo_manifest = dict(base_manifest)
        wrong_repo_manifest["package_id"] = "dummy-wrong-repo"
        wrong_repo_manifest["target_repo_root"] = str(repo / "Other")
        wrong_repo_payload = make_payload(wrong_repo_manifest, applier)
        record("wrong repo", "FAIL_WRONG_REPO", validate_payload_text(wrong_repo_payload, repo))

        record("valid dry run", "PASS_DRY_RUN", dry_run_payload_text(valid_payload, repo))
        record("commit without apply", "FAIL_COMMIT_WITHOUT_APPLY", commit_result(repo))
        missing_commit_process = process_payload_text(make_payload(missing_commit_manifest, applier), repo)
        record("process payload without commit metadata", "PASS_PROCESSED", missing_commit_process)
        record("commit missing metadata", "FAIL_COMMIT_MISSING_METADATA", commit_result(repo))

        target.write_text("# Version: riftscan-dummy-v1.0.0\nprint('dummy')\n", encoding="utf-8")
        save_ledger(repo, {"accepted": [], "last_accepted_created_utc": None})
        record("unsafe commit stage path validation", "FAIL_COMMIT_UNSAFE_STAGE_PATH", validate_payload_text(unsafe_commit_payload, repo))

        target.write_text("# Version: riftscan-dummy-v1.0.0\nprint('dummy')\n", encoding="utf-8")
        save_ledger(repo, {"accepted": [], "last_accepted_created_utc": None})
        record("process payload", "PASS_PROCESSED", process_payload_text(commit_payload, repo))
        record("commit in temp repo", "PASS_COMMITTED", commit_result(repo))
        record("push verify simulated", "PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED", push_verify_remote(repo, simulate=True))

        chunk_repo = Path(tmp) / "ChunkRiftscan"
        chunk_tools = chunk_repo / "tools"
        chunk_tools.mkdir(parents=True)
        chunk_target = chunk_tools / "chunk_target.py"
        chunk_target.write_text("# Version: riftscan-chunk-v1.0.0\nprint('chunk')\n", encoding="utf-8")
        (chunk_repo / "handoffs" / "current").mkdir(parents=True, exist_ok=True)
        (chunk_repo / "handoffs" / "current" / ".gitkeep").write_text("", encoding="utf-8")
        run_command(["git", "init"], cwd=chunk_repo, timeout=30)
        run_command(["git", "add", "tools/chunk_target.py", "handoffs/current/.gitkeep"], cwd=chunk_repo, timeout=30)
        run_command(["git", "-c", "user.email=selftest@example.local", "-c", "user.name=Self Test", "commit", "-m", "init"], cwd=chunk_repo, timeout=30)
        chunk_applier = textwrap.dedent("""
            from __future__ import annotations
            import argparse
            from pathlib import Path

            def main() -> int:
                parser = argparse.ArgumentParser()
                parser.add_argument('--repo-root', required=True)
                args = parser.parse_args()
                path = Path(args.repo_root) / 'tools' / 'chunk_target.py'
                text = path.read_text(encoding='utf-8')
                text = text.replace('riftscan-chunk-v1.0.0', 'riftscan-chunk-v1.0.1')
                path.write_text(text, encoding='utf-8')
                return 0

            if __name__ == '__main__':
                raise SystemExit(main())
            """).lstrip()
        chunk_manifest = {"package_id": "chunk-valid-v100-to-v101", "created_utc": "2026-05-05T01:00:00Z", "repo": "RiftScan", "component": "self-test", "from_version": "riftscan-chunk-v1.0.0", "to_version": "riftscan-chunk-v1.0.1", "target_repo_root": str(chunk_repo), "target_file": "tools/chunk_target.py", "payload_type": "python_applier_base64_gzip", "applier": "chunk-applier.py", "required_existing_markers": ["riftscan-chunk-v1.0.0"], "required_result_markers": ["riftscan-chunk-v1.0.1"], "forbidden_actions": sorted(REQUIRED_FORBIDDEN_ACTIONS)}
        chunk_payload = make_chunked_payload(chunk_manifest, chunk_applier, chunk_size=80)
        record("chunked dry run", "PASS_DRY_RUN", dry_run_payload_text(chunk_payload, chunk_repo))
        bad_hash_payload = re.sub(r"sha256=[0-9a-f]{64}", "sha256=" + "0" * 64, chunk_payload, count=1)
        record("chunked bad chunk hash", "FAIL_CHUNK_HASH_MISMATCH", validate_payload_text(bad_hash_payload, chunk_repo))
        missing_chunk_payload = re.sub(r"---CHUNK 1/\d+ sha256=[0-9a-f]{64} length=\d+---\n.*?(?=---CHUNK 2/)", "", chunk_payload, count=1, flags=re.DOTALL)
        record("chunked missing chunk", "FAIL_MISSING_CHUNK", validate_payload_text(missing_chunk_payload, chunk_repo))


    passed = all(test["pass"] for test in tests)
    summary = {
        "schema_version": "riftscan.patch_intake_self_test.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "status": "PASS" if passed else "FAIL",
        "tests": tests,
    }
    return passed, summary



# v1.1.1 override: push verify nonzero remote match handling.
def push_verify_remote(repo_root: Path, simulate: bool = False) -> dict[str, Any]:
    """Push current main and treat remote SHA verification as authoritative.

    Fix marker: push verify nonzero remote match.
    This keeps the v1.1 self-test API compatible with simulate=True while
    preventing a false failure when git push emits stderr/noisy nonzero output
    but origin/main already equals local HEAD.
    """
    paths = get_paths(repo_root)

    if simulate:
        result = {
            "schema_version": "riftscan.patch_intake_push_result.v1",
            "created_utc": utc_now(),
            "app_version": APP_VERSION,
            "mode": "push",
            "status": "pass",
            "code": "PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED",
            "stage": "simulate",
            "verification_authority": "simulation",
            "push_nonzero_ignored_after_remote_match": False,
            "warnings": [],
            "issues": [],
        }
        write_report(repo_root, result, "push")
        append_event(
            repo_root,
            event="verify_remote",
            stage="simulate",
            status="pass",
            code="PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED",
            artifact_paths=result_artifacts(repo_root, "push", "event_log", "report"),
            extra={"verification_authority": "simulation"},
        )
        return result

    commit_result = read_json(paths["commit_json"], {})
    issues: list[str] = []
    warnings: list[str] = []
    artifact_paths = result_artifacts(repo_root, "push", "event_log", "report")

    result: dict[str, Any] = {
        "schema_version": "riftscan.patch_intake_push_result.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "mode": "push",
        "status": "fail",
        "code": "FAIL_UNKNOWN",
        "source_result": "commit",
        "verification_authority": "remote_sha_match",
        "push_nonzero_ignored_after_remote_match": False,
        "warnings": warnings,
        "issues": issues,
    }

    if commit_result.get("code") != "PASS_COMMITTED":
        issues.append("Successful commit result is required before push.")
        result["code"] = "FAIL_PUSH_WITHOUT_COMMIT"
        write_report(repo_root, result, "push")
        append_event(
            repo_root,
            event="push",
            stage="precondition",
            status="fail",
            code=str(result["code"]),
            artifact_paths=artifact_paths,
            extra={"verification_authority": "remote_sha_match"},
        )
        return result

    head_code, head_out, head_err = run_command(["git", "rev-parse", "HEAD"], cwd=repo_root, timeout=30)
    local_head = head_out.strip()
    result["head_exit_code"] = head_code
    result["head_stdout"] = head_out
    result["head_stderr"] = head_err
    result["local_head"] = local_head

    if head_code != 0 or not local_head:
        issues.append("Could not resolve local HEAD.")
        result["code"] = "FAIL_REMOTE_VERIFY"
        write_report(repo_root, result, "push")
        append_event(
            repo_root,
            event="verify_remote",
            stage="local_head",
            status="fail",
            code=str(result["code"]),
            artifact_paths=artifact_paths,
            extra={"verification_authority": "remote_sha_match"},
        )
        return result

    push_code, push_out, push_err = run_command(["git", "push", "origin", "main"], cwd=repo_root, timeout=180)
    result["push_exit_code"] = push_code
    result["push_stdout"] = push_out
    result["push_stderr"] = push_err

    remote_code, remote_out, remote_err = run_command(
        ["git", "ls-remote", "origin", "refs/heads/main"],
        cwd=repo_root,
        timeout=60,
    )
    result["remote_verify_exit_code"] = remote_code
    result["remote_verify_stdout"] = remote_out
    result["remote_verify_stderr"] = remote_err

    remote_line = remote_out.strip()
    remote_sha = remote_line.split()[0] if remote_line else ""
    result["remote_line"] = remote_line
    result["remote_sha"] = remote_sha

    if remote_code != 0 or not remote_sha:
        issues.append("Could not verify remote main SHA.")
        result["code"] = "FAIL_REMOTE_VERIFY"
        write_report(repo_root, result, "push")
        append_event(
            repo_root,
            event="verify_remote",
            stage="remote_lookup",
            status="fail",
            code=str(result["code"]),
            artifact_paths=artifact_paths,
            extra={"verification_authority": "remote_sha_match"},
        )
        return result

    if remote_sha != local_head:
        if push_code != 0:
            issues.append("git push failed and remote main does not match local HEAD.")
            result["code"] = "FAIL_PUSH_REJECTED"
        else:
            issues.append("Remote main does not match local HEAD after push.")
            result["code"] = "FAIL_REMOTE_VERIFY"
        write_report(repo_root, result, "push")
        append_event(
            repo_root,
            event="verify_remote",
            stage="compare_remote",
            status="fail",
            code=str(result["code"]),
            artifact_paths=artifact_paths,
            extra={
                "verification_authority": "remote_sha_match",
                "local_head": local_head,
                "remote_sha": remote_sha,
            },
        )
        return result

    if push_code != 0:
        warnings.append("WARN_PUSH_NONZERO_VERIFY_REMOTE")
        result["push_nonzero_ignored_after_remote_match"] = True

    result["status"] = "pass"
    result["code"] = "PASS_PUSHED_AND_VERIFIED"
    write_report(repo_root, result, "push")
    append_event(
        repo_root,
        event="verify_remote",
        stage="compare_remote",
        status="pass",
        code="PASS_PUSHED_AND_VERIFIED",
        artifact_paths=artifact_paths,
        extra={
            "verification_authority": "remote_sha_match",
            "local_head": local_head,
            "remote_sha": remote_sha,
            "push_exit_code": push_code,
            "push_nonzero_ignored_after_remote_match": result["push_nonzero_ignored_after_remote_match"],
        },
    )
    return result

class PatchIntakeApp(tk.Tk):
    def __init__(self, repo_root: Path) -> None:
        super().__init__()
        self.repo_root = repo_root
        self.title(f"RiftScan Patch Intake Helper - {APP_VERSION}")
        self.geometry("1120x820")
        self.attributes("-topmost", True)
        self.status_var = tk.StringVar(value="Paste a RiftScan payload, then process or use advanced gates.")
        self._build_ui()

    def _build_ui(self) -> None:
        top = ttk.Frame(self)
        top.pack(fill=tk.X, padx=8, pady=6)
        ttk.Label(top, text=f"Repo: {self.repo_root}").pack(side=tk.LEFT)
        ttk.Label(top, textvariable=self.status_var).pack(side=tk.RIGHT)

        primary = ttk.LabelFrame(self, text="Primary")
        primary.pack(fill=tk.X, padx=8, pady=4)
        ttk.Button(primary, text="Process Payload", command=self.process_payload).pack(side=tk.LEFT, padx=4, pady=4)

        advanced = ttk.LabelFrame(self, text="Advanced gates")
        advanced.pack(fill=tk.X, padx=8, pady=4)
        for text, command in [
            ("Validate Payload", self.validate_payload),
            ("Dry Run", self.dry_run),
            ("Apply Patch", self.apply_patch),
        ]:
            ttk.Button(advanced, text=text, command=command).pack(side=tk.LEFT, padx=4, pady=4)

        commit_frame = ttk.LabelFrame(self, text="Commit / push")
        commit_frame.pack(fill=tk.X, padx=8, pady=4)
        for text, command in [
            ("Commit Result", self.commit_result),
            ("Push + Verify Remote", self.push_verify_remote),
        ]:
            ttk.Button(commit_frame, text=text, command=command).pack(side=tk.LEFT, padx=4, pady=4)

        utility = ttk.LabelFrame(self, text="Utility")
        utility.pack(fill=tk.X, padx=8, pady=4)
        for text, command in [
            ("Run Self-Test", self.self_test),
            ("Load Example Valid", self.load_example_valid),
            ("Load Example Invalid", self.load_example_invalid),
            ("Open Report", self.open_report),
            ("Open Log", self.open_log),
            ("Clear", self.clear),
        ]:
            ttk.Button(utility, text=text, command=command).pack(side=tk.LEFT, padx=4, pady=4)

        self.input_box = scrolledtext.ScrolledText(self, wrap=tk.WORD, height=24)
        self.input_box.pack(fill=tk.BOTH, expand=True, padx=8, pady=6)

        ttk.Label(self, text="Output").pack(anchor=tk.W, padx=8)
        self.output_box = scrolledtext.ScrolledText(self, wrap=tk.WORD, height=13)
        self.output_box.pack(fill=tk.BOTH, expand=False, padx=8, pady=6)

    def get_input(self) -> str:
        return self.input_box.get("1.0", tk.END)

    def write_output(self, value: Any) -> None:
        text = value if isinstance(value, str) else json_block(value)
        self.output_box.delete("1.0", tk.END)
        self.output_box.insert(tk.END, text + "\n")
        if isinstance(value, dict):
            self.status_var.set(f"{value.get('code', 'UNKNOWN')}")

    def validate_payload(self) -> None:
        self.write_output(validate_payload_text(self.get_input(), self.repo_root))

    def dry_run(self) -> None:
        self.write_output(dry_run_payload_text(self.get_input(), self.repo_root))

    def apply_patch(self) -> None:
        if not messagebox.askyesno("Apply patch", "Dry-run and apply this payload now? This will not commit or push."):
            return
        self.write_output(apply_payload_text(self.get_input(), self.repo_root))

    def process_payload(self) -> None:
        def prompt() -> bool:
            return messagebox.askyesno("Process payload", "Validation and dry-run passed. Apply now? This will not commit or push.")
        self.write_output(process_payload_text(self.get_input(), self.repo_root, prompt_callback=prompt))

    def commit_result(self) -> None:
        if not messagebox.askyesno("Commit result", "Stage only manifest-declared paths and create the commit now?"):
            return
        self.write_output(commit_result(self.repo_root))

    def push_verify_remote(self) -> None:
        if not messagebox.askyesno("Push + verify", "Push current branch and verify origin/main equals local HEAD?"):
            return
        self.write_output(push_verify_remote(self.repo_root))

    def self_test(self) -> None:
        passed, summary = run_self_test()
        self.write_output(summary)
        self.status_var.set("PASS_SELF_TEST" if passed else "FAIL_SELF_TEST")

    def load_example_valid(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.input_box.insert(tk.END, make_example_payload(self.repo_root, valid=True, with_commit=True))

    def load_example_invalid(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.input_box.insert(tk.END, make_example_payload(self.repo_root, valid=False))

    def open_report(self) -> None:
        path = get_paths(self.repo_root)["report_md"]
        if path.exists():
            os.startfile(path)
        else:
            messagebox.showerror("Open Report", f"Missing report: {path}")

    def open_log(self) -> None:
        path = get_paths(self.repo_root)["event_log"]
        if path.exists():
            os.startfile(path)
        else:
            messagebox.showerror("Open Log", f"Missing log: {path}")

    def clear(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.output_box.delete("1.0", tk.END)
        self.status_var.set("Cleared.")


def read_payload_file(path: str) -> str:
    return Path(path).expanduser().read_text(encoding="utf-8", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser(description="RiftScan Patch Intake Helper")
    parser.add_argument("--repo-root", default=None)
    parser.add_argument("--validate-file", default=None)
    parser.add_argument("--dry-run-file", default=None)
    parser.add_argument("--apply-file", default=None)
    parser.add_argument("--process-file", default=None)
    parser.add_argument("--commit-result", action="store_true")
    parser.add_argument("--push-verify-remote", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--make-example-valid", action="store_true")
    parser.add_argument("--make-example-invalid", action="store_true")
    args = parser.parse_args()

    repo_root = repo_root_from_arg(args.repo_root)

    if args.self_test:
        passed, summary = run_self_test()
        print(json_block(summary))
        return 0 if passed else 1

    if args.make_example_valid:
        print(make_example_payload(repo_root, valid=True, with_commit=True))
        return 0

    if args.make_example_invalid:
        print(make_example_payload(repo_root, valid=False))
        return 0

    if args.validate_file:
        result = validate_payload_text(read_payload_file(args.validate_file), repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    if args.dry_run_file:
        result = dry_run_payload_text(read_payload_file(args.dry_run_file), repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    if args.apply_file:
        result = apply_payload_text(read_payload_file(args.apply_file), repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    if args.process_file:
        result = process_payload_text(read_payload_file(args.process_file), repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    if args.commit_result:
        result = commit_result(repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    if args.push_verify_remote:
        result = push_verify_remote(repo_root)
        print(json_block(result))
        return 0 if result.get("status") == "pass" else 1

    app = PatchIntakeApp(repo_root)
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# END_OF_SCRIPT
