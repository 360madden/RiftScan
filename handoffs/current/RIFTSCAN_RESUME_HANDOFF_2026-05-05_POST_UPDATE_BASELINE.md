---
schema_version: riftscan.resume_handoff.v1
handoff_id: RIFTSCAN_RESUME_HANDOFF_2026-05-05_POST_UPDATE_BASELINE
created_utc: 2026-05-05T18:25:29Z
repo: 360madden/RiftScan
branch: main
local_repo_root: "C:\\RIFT MODDING\\Riftscan"
primary_status: game_updated_then_maintenance
next_milestone: post_update_baseline_verification
live_capture_allowed: false
---

# RIFTSCAN_RESUME_HANDOFF_2026-05-05_POST_UPDATE_BASELINE

## Purpose

Resume RiftScan development after the May 5, 2026 RIFT client update and maintenance window.

The next work must establish a clean **post-update baseline** before any offset, capture, coordinate, actor-yaw, camera-yaw, or memory-scanning work is trusted.

## Current Situation

The user reported:

```text
RIFT game client updated.
RIFT then went down for maintenance.
```

Treat all pre-update live-memory assumptions as stale until revalidated.

This handoff is intentionally a **planning and safety gate** handoff only. It does not authorize live capture, movement, input automation, memory scanning, `/reloadui`, or coordinate recovery.

## Repo Context

```text
Repo: 360madden/RiftScan
Local path: C:\RIFT MODDING\Riftscan
Branch: main
Known earlier May 5 work: guided Operator workflow UX
Expected Operator version after earlier pushed work: riftscan-operator-app-v3.8.7
```

Important existing paths:

```text
scripts/riftscan-operator-app.cmd
tools/riftscan_operator_app.py
scripts/run-rift-focus-control.cmd
tools/rift_focus_control.py
handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
handoffs/current/focus-control-local/
handoffs/current/patch-intake/
```

## Strategic Decision

Do **not** proceed directly into live capture after maintenance.

The correct sequence is:

```text
1. Pull latest repo state.
2. Launch the updated RIFT client.
3. Confirm maintenance is over.
4. Log into a stable in-world character state.
5. Run Operator status/preflight only.
6. Write a post-update baseline report.
7. Only after the baseline passes, decide whether to run capture/offset work.
```

## Hard Guardrails

Until post-update baseline passes:

```text
NO live memory capture.
NO coordinate recovery probe.
NO actor yaw/camera yaw validation.
NO movement automation.
NO keypress/mouse automation.
NO /reloadui.
NO stairway capture.
NO assumption that old offsets remain valid.
```

Git guardrails remain unchanged:

```text
Never use git add .
Stage only explicit allowlisted files.
Do not commit installer payload junk.
Do not commit __pycache__.
Do not commit .riftscan-local cache files.
```

## Post-Update Baseline Data Requirements

The first baseline after maintenance should record at minimum:

```text
client_launched: yes/no
maintenance_over: yes/no
login_successful: yes/no
world_loaded: yes/no
character_name: if available
shard: if available
zone_or_location: if available
process_name: expected rift_x64
pid: current runtime PID
hwnd: current runtime HWND
title: expected RIFT
focus_status: expected foreground_verified
operator_app_version: expected riftscan-operator-app-v3.8.7
git_branch: main
git_head: current commit SHA
git_status_short: recorded verbatim
old_offsets_trusted: false until separately revalidated
capture_allowed_after_this_report: false unless explicitly promoted
```

Recommended output files for the next implementation step:

```text
handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md
handoffs/current/post-update-baseline/post-update-baseline-summary.json
handoffs/current/post-update-baseline/post-update-baseline-log.jsonl
```

## Manual Operator Procedure After Maintenance

From PowerShell:

```powershell
cd "C:\RIFT MODDING\Riftscan"
git pull --ff-only
.\scripts\riftscan-operator-app.cmd
```

In the Operator app:

```text
1. Refresh Status
2. Run Focus Preflight
3. Run Full Live Preflight
4. Write AI Report
5. Open Report
```

Expected safe outcome:

```text
Focus status: foreground_verified
Full live preflight: PASS
Operator report updated
No capture started
No movement/input sent
No memory scan/read started
```

If any blocker appears, stop and record it. Do not proceed to capture.

## Recommended Next Coding Milestone

Add a conservative Operator workflow/button:

```text
Post-Update Baseline
```

The button should do only this:

```text
1. Read current git HEAD/status.
2. Run focus preflight or reuse the current focus summary.
3. Validate foreground_verified.
4. Record whether selected_window exists.
5. Record whether windows.json has entries.
6. Write post-update baseline Markdown, JSON, and JSONL outputs.
7. Display PASS/FAIL with explicit blockers.
```

The button must not:

```text
- capture memory
- send movement/input
- run coordinate recovery
- run scanner probes
- issue /reloadui
- commit automatically
- push automatically
```

## Acceptance Criteria For The Next Safe Resume

A post-update baseline pass is acceptable only if all are true:

```text
RIFT client is back online.
User is logged into a stable in-world state.
RIFT window is enumerated.
selected_window is present.
focus_status is foreground_verified.
Operator report is regenerated after the update.
git status is reviewed.
old offsets are still marked untrusted.
No live capture was performed during baseline.
```

A failure is acceptable if it clearly lists blockers, for example:

```text
POST-UPDATE BASELINE: FAIL
- Maintenance is not over.
- World is not loaded.
- Focus status is not foreground_verified.
- selected_window is missing.
- windows.json has no entries.
```

## Machine-Readable Resume Block

```json
{
  "schema_version": "riftscan.resume_handoff.v1",
  "handoff_id": "RIFTSCAN_RESUME_HANDOFF_2026-05-05_POST_UPDATE_BASELINE",
  "created_utc": "2026-05-05T18:25:29Z",
  "repo": "360madden/RiftScan",
  "branch": "main",
  "local_repo_root": "C:\\RIFT MODDING\\Riftscan",
  "trigger": "RIFT client updated, then entered maintenance",
  "primary_decision": "pause live capture and require post-update baseline before memory work",
  "live_capture_allowed": false,
  "old_offsets_trusted": false,
  "expected_operator_version": "riftscan-operator-app-v3.8.7",
  "next_manual_commands": [
    "cd \"C:\\RIFT MODDING\\Riftscan\"",
    "git pull --ff-only",
    ".\\scripts\\riftscan-operator-app.cmd"
  ],
  "next_operator_actions": [
    "Refresh Status",
    "Run Focus Preflight",
    "Run Full Live Preflight",
    "Write AI Report",
    "Open Report"
  ],
  "blocked_until": [
    "maintenance over",
    "stable in-world login",
    "post-update baseline report written"
  ],
  "must_not_do_before_baseline_pass": [
    "live memory capture",
    "coordinate recovery",
    "actor yaw validation",
    "camera yaw validation",
    "movement automation",
    "keypress automation",
    "mouse automation",
    "/reloadui",
    "stairway capture"
  ],
  "recommended_next_coding_task": "Add conservative Post-Update Baseline workflow/button to Operator app",
  "recommended_output_paths": [
    "handoffs/current/post-update-baseline/POST_UPDATE_BASELINE_REPORT.md",
    "handoffs/current/post-update-baseline/post-update-baseline-summary.json",
    "handoffs/current/post-update-baseline/post-update-baseline-log.jsonl"
  ],
  "git_guardrails": [
    "never use git add .",
    "stage explicit allowlisted paths only",
    "do not commit installer payload junk",
    "do not commit __pycache__",
    "do not commit .riftscan-local cache files"
  ]
}
```

## Recommended New Chat Prompt

```text
Read handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_POST_UPDATE_BASELINE.md and resume RiftScan from it. RIFT updated and then went down for maintenance, so do not run live capture or trust old offsets yet. First task after maintenance: run post-update baseline only, then write the baseline report. Next coding task: add a conservative Post-Update Baseline Operator workflow/button that records status and blockers only.
```

## Bottom Line

The project is in a controlled pause for live testing. Use the downtime for repo/operator/report hardening. After maintenance, the first valid action is a post-update baseline, not capture.
