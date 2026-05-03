# Version: riftscan-operator-app-v2
# Purpose: Windows Tkinter helper app for RiftScan operator workflow: run focus preflight, run full live preflight gate, validate handoffs, write AI-ready reports, clean known junk, and safely commit/push allowlisted files.
# Total character count: 17128

from __future__ import annotations

import datetime as dt
import json
import os
import shutil
import subprocess
import threading
import tkinter as tk
from pathlib import Path
from tkinter import messagebox, scrolledtext
from typing import Any


APP_VERSION = "riftscan-operator-app-v2"
REPO_ROOT = Path(__file__).resolve().parents[1]
FOCUS_SCRIPT = REPO_ROOT / "scripts" / "run-rift-focus-control.cmd"
HANDOFF_DIR = REPO_ROOT / "handoffs" / "current" / "focus-control-local"
OPERATOR_DIR = REPO_ROOT / "handoffs" / "current" / "operator"
REPORT_PATH = OPERATOR_DIR / "RIFTSCAN_OPERATOR_HANDOFF.md"
FOCUS_SUMMARY = HANDOFF_DIR / "focus-control-summary.json"
WINDOWS_JSON = HANDOFF_DIR / "windows.json"
FOCUS_LOG = HANDOFF_DIR / "focus-control-log.jsonl"

ALLOWLIST = [
    "handoffs/current/focus-control-local",
    "handoffs/current/operator",
    "scripts/run-rift-focus-control.cmd",
    "scripts/riftscan-operator-app.cmd",
    "tools/rift_focus_control.py",
    "tools/riftscan_operator_app.py",
]

JUNK_LITERAL = [
    "None",
    "dict[str",
    "list[dict[str",
    "str",
    "README.txt",
    "rift_focus_local_simple_v2.zip",
]

JUNK_GLOBS = [
    "__pycache__",
    "tools/__pycache__",
    "scripts/__pycache__",
    "*.bak-*",
    "*.repair-bak-*",
]


def utc_now() -> str:
    return dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path) -> str:
    try:
        return path.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path)


def run_command(args: list[str], timeout: int = 90) -> tuple[int, str, str]:
    try:
        completed = subprocess.run(
            args,
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            timeout=timeout,
            shell=False,
        )
        return completed.returncode, completed.stdout, completed.stderr
    except subprocess.TimeoutExpired as exc:
        out = exc.stdout or ""
        err = exc.stderr or ""
        return 124, out, f"{err}\nTIMEOUT after {timeout} seconds"
    except Exception as exc:
        return 1, "", f"{type(exc).__name__}: {exc}"


def read_text(path: Path) -> str:
    if not path.exists():
        return f"[missing: {rel(path)}]"
    return path.read_text(encoding="utf-8", errors="replace")


def tail_text(path: Path, lines: int = 60) -> str:
    return "\n".join(read_text(path).splitlines()[-lines:])


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"_missing": rel(path)}
    try:
        return json.loads(path.read_text(encoding="utf-8", errors="replace"))
    except Exception as exc:
        return {"_error": f"{type(exc).__name__}: {exc}", "_path": rel(path)}


def json_block(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False)


def focus_line(summary: dict[str, Any]) -> str:
    status = summary.get("status", "unknown")
    process = summary.get("process") or {}
    selected = summary.get("selected_window") or {}
    pid = process.get("Id", "n/a")
    hwnd = selected.get("hwnd_hex", "n/a")
    title = selected.get("title", "n/a")
    return f"status={status} pid={pid} hwnd={hwnd} title={title}"


def validate_full_live_preflight(summary: dict[str, Any], windows: dict[str, Any]) -> tuple[bool, list[str]]:
    issues: list[str] = []

    if summary.get("_missing"):
        issues.append(f"{summary['_missing']} is missing.")
    if summary.get("_error"):
        issues.append(f"{summary.get('_path', rel(FOCUS_SUMMARY))} failed to parse: {summary['_error']}")
    if windows.get("_missing"):
        issues.append(f"{windows['_missing']} is missing.")
    if windows.get("_error"):
        issues.append(f"{windows.get('_path', rel(WINDOWS_JSON))} failed to parse: {windows['_error']}")

    if summary.get("status") != "foreground_verified":
        issues.append("Focus status is not foreground_verified.")
    if not summary.get("selected_window"):
        issues.append("selected_window is missing or null.")

    window_entries = windows.get("windows")
    if not isinstance(window_entries, list) or not window_entries:
        issues.append("windows.json has no window entries.")

    return not issues, issues


def write_operator_report() -> Path:
    OPERATOR_DIR.mkdir(parents=True, exist_ok=True)

    status_code, git_status, git_status_err = run_command(["git", "status", "--short"], timeout=30)
    log_code, git_log, git_log_err = run_command(["git", "log", "--oneline", "-5"], timeout=30)

    summary = load_json(FOCUS_SUMMARY)
    windows = load_json(WINDOWS_JSON)
    log_tail = tail_text(FOCUS_LOG, 60)

    focus_ok = summary.get("status") == "foreground_verified"
    full_ok, validation_issues = validate_full_live_preflight(summary, windows)
    issues: list[str] = [f"- {issue}" for issue in validation_issues]

    if status_code != 0:
        issues.append("- git status command failed.")
    if log_code != 0:
        issues.append("- git log command failed.")
    if not issues:
        issues.append("- No blocking operator issues detected.")

    report = f"""# RiftScan Operator Handoff

Created UTC: `{utc_now()}`
App version: `{APP_VERSION}`
Repo root: `{REPO_ROOT}`

## Operator Assessment

Full live preflight gate: `{"PASS" if full_ok and status_code == 0 and log_code == 0 else "FAIL"}`
Focus preflight: `{"PASS" if focus_ok else "FAIL"}`
Summary: `{focus_line(summary)}`

{chr(10).join(issues)}

## Git Status

Exit code: `{status_code}`

```text
{git_status or git_status_err}
```

## Recent Commits

Exit code: `{log_code}`

```text
{git_log or git_log_err}
```

## Focus Summary JSON

```json
{json_block(summary)}
```

## Windows JSON

```json
{json_block(windows)}
```

## Focus Log Tail

```jsonl
{log_tail}
```

## AI Review Prompt

```text
Review this RiftScan operator handoff. Tell me the next safest practical step, and give exact commands only if local execution is needed.
```

## Guardrails

- The full live preflight is conservative: focus + validation + report only.
- The full live preflight does not run movement, input, capture, memory scans, or `/reloadui`.
- The helper stages only explicit allowlisted paths.
- The helper never runs `git add .`.
- Known junk cleanup uses literal paths/globs from the helper configuration.
"""
    REPORT_PATH.write_text(report, encoding="utf-8")
    return REPORT_PATH


def clean_known_junk() -> list[str]:
    removed: list[str] = []

    for name in JUNK_LITERAL:
        path = REPO_ROOT / name
        if path.exists():
            if path.is_dir():
                shutil.rmtree(path)
            else:
                path.unlink()
            removed.append(rel(path))

    for pattern in JUNK_GLOBS:
        for path in REPO_ROOT.glob(pattern):
            if path.exists():
                if path.is_dir():
                    shutil.rmtree(path)
                else:
                    path.unlink()
                removed.append(rel(path))

    return removed


class RiftScanOperatorApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title("RiftScan Operator")
        self.geometry("980x700")
        self.minsize(820, 560)

        self.status_var = tk.StringVar(value="Ready.")
        self.focus_var = tk.StringVar(value="Focus: unknown")

        header = tk.Frame(self)
        header.pack(fill=tk.X, padx=10, pady=(10, 4))

        tk.Label(header, text="RiftScan Operator", font=("Segoe UI", 16, "bold")).pack(side=tk.LEFT)
        tk.Label(header, textvariable=self.focus_var, font=("Segoe UI", 10)).pack(side=tk.RIGHT)

        button_row = tk.Frame(self)
        button_row.pack(fill=tk.X, padx=10, pady=4)

        buttons = [
            ("Refresh Status", self.refresh_status),
            ("Run Full Live Preflight", self.run_full_live_preflight),
            ("Run Focus Preflight", self.run_focus_preflight),
            ("Write AI Report", self.write_report_clicked),
            ("Clean Known Junk", self.clean_junk_clicked),
            ("Commit Allowlist", self.commit_clicked),
            ("Push", self.push_clicked),
            ("Open Report", self.open_report_clicked),
        ]

        for label, command in buttons:
            tk.Button(button_row, text=label, command=command).pack(side=tk.LEFT, padx=4, pady=4)

        tk.Label(self, textvariable=self.status_var, anchor="w").pack(fill=tk.X, padx=10, pady=(2, 4))

        self.output = scrolledtext.ScrolledText(self, wrap=tk.WORD, font=("Consolas", 10))
        self.output.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))

        self.append(f"Repo root: {REPO_ROOT}\n")
        self.refresh_status()

    def append(self, text: str) -> None:
        self.output.insert(tk.END, text)
        if not text.endswith("\n"):
            self.output.insert(tk.END, "\n")
        self.output.see(tk.END)

    def set_status(self, text: str) -> None:
        self.status_var.set(text)
        self.append(text)

    def run_async(self, label: str, func: Any) -> None:
        def worker() -> None:
            self.after(0, lambda: self.set_status(f"Running: {label}"))
            try:
                result = func()
                if result is not None:
                    self.after(0, lambda: self.append(str(result)))
            except Exception as exc:
                self.after(0, lambda: messagebox.showerror("RiftScan Operator", f"{type(exc).__name__}: {exc}"))
                self.after(0, lambda: self.append(f"ERROR: {type(exc).__name__}: {exc}"))
            finally:
                self.after(0, lambda: self.status_var.set("Ready."))

        threading.Thread(target=worker, daemon=True).start()

    def refresh_status(self) -> None:
        summary = load_json(FOCUS_SUMMARY)
        line = focus_line(summary)
        self.focus_var.set(f"Focus: {line}")

        code, status, err = run_command(["git", "status", "--short"], timeout=30)
        log_code, log, log_err = run_command(["git", "log", "--oneline", "-3"], timeout=30)

        self.append("\n=== STATUS ===")
        self.append(line)
        self.append("\nGit status:")
        self.append(status if code == 0 else err)
        self.append("\nRecent commits:")
        self.append(log if log_code == 0 else log_err)

    def run_focus_preflight(self) -> None:
        def task() -> str:
            if not FOCUS_SCRIPT.exists():
                return f"ERROR: missing {rel(FOCUS_SCRIPT)}"

            code, out, err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)
            summary = load_json(FOCUS_SUMMARY)
            report_path = write_operator_report()
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            lines = [
                "\n=== FOCUS PREFLIGHT ===",
                f"Exit code: {code}",
                out.strip(),
                err.strip(),
                focus_line(summary),
                f"Operator report: {rel(report_path)}",
            ]
            return "\n".join(line for line in lines if line)

        self.run_async("focus preflight", task)

    def run_full_live_preflight(self) -> None:
        def task() -> str:
            focus_code = 1
            focus_out = ""
            focus_err = ""

            if FOCUS_SCRIPT.exists():
                focus_code, focus_out, focus_err = run_command(["cmd", "/c", str(FOCUS_SCRIPT)], timeout=90)

            summary = load_json(FOCUS_SUMMARY)
            windows = load_json(WINDOWS_JSON)
            valid, issues = validate_full_live_preflight(summary, windows)

            if not FOCUS_SCRIPT.exists():
                issues.append(f"Missing {rel(FOCUS_SCRIPT)}.")
            elif focus_code != 0:
                issues.append(f"Focus-control script exited with code {focus_code}.")

            git_status_code, git_status, git_status_err = run_command(["git", "status", "--short"], timeout=30)
            git_log_code, git_log, git_log_err = run_command(["git", "log", "--oneline", "-5"], timeout=30)

            if git_status_code != 0:
                issues.append("git status --short failed.")
            if git_log_code != 0:
                issues.append("git log --oneline -5 failed.")

            report_path = write_operator_report()
            self.after(0, lambda: self.focus_var.set(f"Focus: {focus_line(summary)}"))

            process = summary.get("process") or {}
            selected = summary.get("selected_window") or {}
            pid = process.get("Id", "n/a")
            hwnd = selected.get("hwnd_hex", "n/a")
            title = selected.get("title", "n/a")

            pass_gate = valid and focus_code == 0 and git_status_code == 0 and git_log_code == 0 and not issues

            lines = ["\n=== FULL LIVE PREFLIGHT ==="]
            if pass_gate:
                lines.extend(
                    [
                        "FULL LIVE PREFLIGHT: PASS",
                        f"Focus: {summary.get('status', 'unknown')}",
                        f"PID: {pid}",
                        f"HWND: {hwnd}",
                        f"Title: {title}",
                        f"Operator report: {rel(report_path)}",
                    ]
                )
            else:
                lines.append("FULL LIVE PREFLIGHT: FAIL")
                lines.extend(f"- {issue}" for issue in issues)
                lines.append(f"Operator report: {rel(report_path)}")

            lines.extend(
                [
                    "",
                    "Focus-control stdout:",
                    focus_out.strip() or "[empty]",
                    "",
                    "Focus-control stderr:",
                    focus_err.strip() or "[empty]",
                    "",
                    "git status --short:",
                    git_status.strip() if git_status_code == 0 and git_status.strip() else ("[clean]" if git_status_code == 0 else git_status_err.strip()),
                    "",
                    "git log --oneline -5:",
                    git_log.strip() if git_log_code == 0 else git_log_err.strip(),
                ]
            )
            return "\n".join(lines)

        self.run_async("full live preflight", task)

    def write_report_clicked(self) -> None:
        path = write_operator_report()
        self.append(f"\nWrote operator report: {rel(path)}")

    def clean_junk_clicked(self) -> None:
        if not messagebox.askyesno("Clean known junk", "Remove known junk files/cache directories?"):
            return
        removed = clean_known_junk()
        if removed:
            self.append("\nRemoved:")
            for item in removed:
                self.append(f"- {item}")
        else:
            self.append("\nNo known junk found.")
        self.refresh_status()

    def commit_clicked(self) -> None:
        if not messagebox.askyesno(
            "Commit allowlisted files",
            "Stage only allowlisted focus/operator paths and commit? Push remains separate.",
        ):
            return

        def task() -> str:
            write_operator_report()
            existing = [path for path in ALLOWLIST if (REPO_ROOT / path).exists()]
            if not existing:
                return "No allowlisted paths exist to stage."

            add_code, add_out, add_err = run_command(["git", "add", "--", *existing], timeout=60)
            if add_code != 0:
                return f"git add failed:\n{add_out}\n{add_err}"

            diff_code, _, _ = run_command(["git", "diff", "--cached", "--quiet"], timeout=30)
            if diff_code == 0:
                return "No staged changes after allowlisted git add."

            commit_code, commit_out, commit_err = run_command(
                ["git", "commit", "-m", "Update RiftScan operator handoff"],
                timeout=90,
            )
            self.after(0, self.refresh_status)
            return f"git commit exit={commit_code}\n{commit_out}\n{commit_err}"

        self.run_async("commit allowlisted files", task)

    def push_clicked(self) -> None:
        if not messagebox.askyesno("Push", "Run git push for the current branch?"):
            return

        def task() -> str:
            code, out, err = run_command(["git", "push"], timeout=120)
            self.after(0, self.refresh_status)
            return f"git push exit={code}\n{out}\n{err}"

        self.run_async("push", task)

    def open_report_clicked(self) -> None:
        if not REPORT_PATH.exists():
            write_operator_report()
        try:
            os.startfile(str(REPORT_PATH))  # type: ignore[attr-defined]
        except Exception as exc:
            messagebox.showerror("Open report failed", f"{type(exc).__name__}: {exc}")


def main() -> int:
    app = RiftScanOperatorApp()
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# End of script
