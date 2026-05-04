# Version: riftscan-patch-intake-v1.0.0
# Purpose: Local RiftScan Patch Intake Helper. Provides an always-on-top paste GUI plus headless validate/dry-run/apply/self-test modes for machine-readable RiftScan clipboard patch payloads. No clipboard watcher, no service, no listener, no polling, no git add, no auto-commit, no auto-push.
# Total character count: 31815

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
from tkinter import filedialog, messagebox, scrolledtext, ttk
from typing import Any


APP_VERSION = "riftscan-patch-intake-v1.0.0"
MAGIC = "RIFTSCAN_CLIPBOARD_PATCH_V1"
DEFAULT_REPO_ROOT = Path(__file__).resolve().parents[1]
STATE_DIR_REL = ".riftscan-local/patch-intake"
REPORT_DIR_REL = "handoffs/current/patch-intake"
ACCEPTED_LEDGER_NAME = "accepted-patches.json"
LAST_VALIDATION_JSON = "last-validation-result.json"
LAST_DRY_RUN_JSON = "last-dry-run-result.json"
LAST_APPLY_JSON = "last-apply-result.json"
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
        "report_md": report_dir / REPORT_MD,
        "staging_dir": state_dir / "staging",
        "logs_dir": state_dir / "logs",
    }


def load_ledger(repo_root: Path) -> dict[str, Any]:
    return read_json(get_paths(repo_root)["ledger"], {"accepted": [], "last_accepted_created_utc": None})


def save_ledger(repo_root: Path, ledger: dict[str, Any]) -> None:
    write_json(get_paths(repo_root)["ledger"], ledger)


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


def parse_payload(text: str) -> tuple[ParsedPayload | None, dict[str, Any]]:
    raw = text or ""
    result: dict[str, Any] = {
        "status": "unknown",
        "code": "UNKNOWN",
        "issues": [],
    }

    stripped = raw.strip()
    if not stripped.startswith(MAGIC):
        result["status"] = "fail"
        result["code"] = "FAIL_BAD_HEADER"
        result["issues"].append(f"Payload must start with {MAGIC}.")
        return None, result

    marker_payload = "---PAYLOAD---"
    marker_end = "---END---"

    if marker_payload not in stripped or marker_end not in stripped:
        result["status"] = "fail"
        result["code"] = "FAIL_MISSING_PAYLOAD"
        result["issues"].append("Payload block markers are missing.")
        return None, result

    after_magic = stripped[len(MAGIC):].strip()
    manifest_text, rest = after_magic.split(marker_payload, 1)
    payload_text, _ = rest.split(marker_end, 1)

    try:
        manifest = json.loads(manifest_text.strip())
    except Exception as exc:
        result["status"] = "fail"
        result["code"] = "FAIL_BAD_MANIFEST"
        result["issues"].append(f"{type(exc).__name__}: {exc}")
        return None, result

    try:
        compressed = base64.b64decode("".join(payload_text.split()), validate=True)
        payload_bytes = gzip.decompress(compressed)
    except Exception as exc:
        result["status"] = "fail"
        result["code"] = "FAIL_BAD_PAYLOAD_ENCODING"
        result["issues"].append(f"{type(exc).__name__}: {exc}")
        return None, result

    return ParsedPayload(manifest=manifest, payload_text=payload_text.strip(), payload_bytes=payload_bytes, raw_text=raw), result


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
        "issues": issues,
        "warnings": warnings,
        "report_path": rel(repo_root, paths["report_md"]),
    }

    if manifest.get("magic") != MAGIC:
        issues.append("Manifest magic does not match.")
        result["code"] = "FAIL_BAD_MANIFEST"
    if manifest.get("schema_version") != "riftscan.clipboard_patch.v1":
        issues.append("Manifest schema_version must be riftscan.clipboard_patch.v1.")
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

    if kind == "validation":
        write_json(paths["validation_json"], result)
    elif kind == "dry_run":
        write_json(paths["dry_run_json"], result)
    elif kind == "apply":
        write_json(paths["apply_json"], result)

    md = (
        "# RiftScan Patch Intake Report\n\n"
        f"Created UTC: `{utc_now()}`\n\n"
        f"Kind: `{kind}`\n\n"
        "```json\n"
        f"{json_block(result)}\n"
        "```\n"
    )
    paths["report_md"].write_text(md, encoding="utf-8")


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
        return result

    result = validate_parsed_payload(parsed, repo_root, mode=kind)
    write_report(repo_root, result, kind)
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
        return result

    result = validate_parsed_payload(parsed, repo_root, mode="dry_run")
    if result.get("code") == "PASS_ALREADY_PATCHED":
        write_report(repo_root, result, "dry_run")
        return result
    if result.get("status") != "pass":
        write_report(repo_root, result, "dry_run")
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
        return result

    dry = dry_run_payload_text(text, repo_root)
    if dry.get("code") == "PASS_ALREADY_PATCHED":
        write_report(repo_root, dry, "apply")
        return dry
    if dry.get("code") != "PASS_DRY_RUN":
        write_report(repo_root, dry, "apply")
        return dry

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
    return result


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


def make_example_payload(repo_root: Path, valid: bool = True) -> str:
    created = utc_now()
    target_file = "tools/riftscan_patch_intake_app.py"
    from_version = APP_VERSION
    to_version = "riftscan-patch-intake-v1.0.1"
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
        "    if 'riftscan-patch-intake-v1.0.1' not in text:\n"
        "        text = text.replace('riftscan-patch-intake-v1.0.0', 'riftscan-patch-intake-v1.0.1')\n"
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
        "forbidden_actions": sorted(REQUIRED_FORBIDDEN_ACTIONS),
    }
    if not valid:
        manifest["repo"] = "WrongRepo"
    return make_payload(manifest, applier)


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
        run_command(["git", "add", "tools/dummy_target.py", "handoffs/current/.gitkeep"], cwd=repo, timeout=30)
        run_command(["git", "-c", "user.email=selftest@example.local", "-c", "user.name=Self Test", "commit", "-m", "init"], cwd=repo, timeout=30)

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
            "forbidden_actions": sorted(REQUIRED_FORBIDDEN_ACTIONS),
        }
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
        record("valid apply", "PASS_APPLIED", apply_payload_text(valid_payload, repo))

    passed = all(test["pass"] for test in tests)
    summary = {
        "schema_version": "riftscan.patch_intake_self_test.v1",
        "created_utc": utc_now(),
        "app_version": APP_VERSION,
        "status": "PASS" if passed else "FAIL",
        "tests": tests,
    }
    return passed, summary


class PatchIntakeApp(tk.Tk):
    def __init__(self, repo_root: Path) -> None:
        super().__init__()
        self.repo_root = repo_root
        self.title(f"RiftScan Patch Intake Helper - {APP_VERSION}")
        self.geometry("1000x760")
        self.attributes("-topmost", True)
        self.status_var = tk.StringVar(value="Paste a RiftScan payload, then validate.")
        self._build_ui()

    def _build_ui(self) -> None:
        top = ttk.Frame(self)
        top.pack(fill=tk.X, padx=8, pady=6)
        ttk.Label(top, text=f"Repo: {self.repo_root}").pack(side=tk.LEFT)
        ttk.Label(top, textvariable=self.status_var).pack(side=tk.RIGHT)

        buttons = ttk.Frame(self)
        buttons.pack(fill=tk.X, padx=8, pady=4)

        for text, command in [
            ("Validate Payload", self.validate_payload),
            ("Dry Run", self.dry_run),
            ("Apply Patch", self.apply_patch),
            ("Run Self-Test", self.self_test),
            ("Load Example Valid", self.load_example_valid),
            ("Load Example Invalid", self.load_example_invalid),
            ("Open Report", self.open_report),
            ("Clear", self.clear),
        ]:
            ttk.Button(buttons, text=text, command=command).pack(side=tk.LEFT, padx=4, pady=2)

        self.input_box = scrolledtext.ScrolledText(self, wrap=tk.WORD, height=24)
        self.input_box.pack(fill=tk.BOTH, expand=True, padx=8, pady=6)

        ttk.Label(self, text="Output").pack(anchor=tk.W, padx=8)
        self.output_box = scrolledtext.ScrolledText(self, wrap=tk.WORD, height=12)
        self.output_box.pack(fill=tk.BOTH, expand=False, padx=8, pady=6)

    def payload_text(self) -> str:
        return self.input_box.get("1.0", tk.END)

    def show_result(self, result: dict[str, Any]) -> None:
        self.output_box.delete("1.0", tk.END)
        self.output_box.insert(tk.END, json_block(result))
        self.status_var.set(str(result.get("code", "UNKNOWN")))

    def validate_payload(self) -> None:
        self.show_result(validate_payload_text(self.payload_text(), self.repo_root, "validation"))

    def dry_run(self) -> None:
        self.show_result(dry_run_payload_text(self.payload_text(), self.repo_root))

    def apply_patch(self) -> None:
        if not messagebox.askyesno("Apply Patch", "Apply this validated patch to the local repo?"):
            return
        self.show_result(apply_payload_text(self.payload_text(), self.repo_root))

    def self_test(self) -> None:
        passed, summary = run_self_test()
        self.show_result(summary)
        self.status_var.set("SELF_TEST: PASS" if passed else "SELF_TEST: FAIL")

    def load_example_valid(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.input_box.insert(tk.END, make_example_payload(self.repo_root, valid=True))
        self.status_var.set("Loaded example valid payload.")

    def load_example_invalid(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.input_box.insert(tk.END, make_example_payload(self.repo_root, valid=False))
        self.status_var.set("Loaded example invalid payload.")

    def open_report(self) -> None:
        report = get_paths(self.repo_root)["report_md"]
        report.parent.mkdir(parents=True, exist_ok=True)
        if not report.exists():
            report.write_text("# RiftScan Patch Intake Report\n\nNo report yet.\n", encoding="utf-8")
        os.startfile(str(report))

    def clear(self) -> None:
        self.input_box.delete("1.0", tk.END)
        self.output_box.delete("1.0", tk.END)
        self.status_var.set("Cleared.")


def read_file(path: str) -> str:
    return Path(path).read_text(encoding="utf-8", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser(description="RiftScan Patch Intake Helper")
    parser.add_argument("--repo-root", default=None)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--validate-file", default=None)
    parser.add_argument("--dry-run-file", default=None)
    parser.add_argument("--apply-file", default=None)
    parser.add_argument("--gui", action="store_true")
    args = parser.parse_args()

    repo_root = repo_root_from_arg(args.repo_root)

    if args.self_test:
        passed, summary = run_self_test()
        print(json_block(summary))
        print("SELF_TEST: PASS" if passed else "SELF_TEST: FAIL")
        return 0 if passed else 1

    if args.validate_file:
        result = validate_payload_text(read_file(args.validate_file), repo_root, "validation")
        print(json_block(result))
        return 0 if str(result.get("code", "")).startswith("PASS") else 1

    if args.dry_run_file:
        result = dry_run_payload_text(read_file(args.dry_run_file), repo_root)
        print(json_block(result))
        return 0 if str(result.get("code", "")).startswith("PASS") else 1

    if args.apply_file:
        result = apply_payload_text(read_file(args.apply_file), repo_root)
        print(json_block(result))
        return 0 if str(result.get("code", "")).startswith("PASS") else 1

    app = PatchIntakeApp(repo_root)
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# End of script
