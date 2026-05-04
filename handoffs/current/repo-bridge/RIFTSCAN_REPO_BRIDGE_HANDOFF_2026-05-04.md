# RIFTSCAN Repo Bridge + Patcher Rollback Handoff — 2026-05-04

## Purpose

Document the current RiftScan state, the repo-mediated helper-app idea, the safer on-demand Operator direction, and the safe rollback plan for any patcher service/listener work.

This handoff is intentionally conservative. It does **not** claim that an always-on listener/server is installed or working. It records the design decision to **avoid always-on behavior for now** and move toward an **on-demand Operator workflow**.

---

## Repo Context

```text
Repo: 360madden/RiftScan
Local path: C:\RIFT MODDING\Riftscan
Branch: main
Preferred workflow: RiftScan Operator GUI first
Manual PowerShell fallback: only when Operator fails
```

---

## Current Known Working State

Confirmed from the prior RiftScan handoffs:

```text
- RIFT foreground focus control is working and tracked.
- Full Live Preflight gate is working/tracked.
- RiftScan Operator GUI workflow is the preferred control surface.
- Focus-Gated Session Dry Run exists and is metadata-only.
- Dry-run session manifest and dry-run handoff exist.
- Operator report generation exists.
- Commit Allowlist + Push workflow exists.
```

Important files already in the workflow:

```text
tools/rift_focus_control.py
scripts/run-rift-focus-control.cmd

tools/riftscan_operator_app.py
scripts/riftscan-operator-app.cmd

handoffs/current/focus-control-local/
handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md

sessions/focus-gated-dry-runs/
sessions/focus-gated-dry-runs/LATEST_DRY_RUN.txt
```

Known good focus state from prior handoff:

```text
status=foreground_verified
process=rift_x64
pid=29420
hwnd=0x4E0F42
title=RIFT
```

---

## Current Patch Runner State

Current documented patch-runner scope:

```text
Patch Runner v3.9-alpha2 — manifest validation only
```

Alpha2 behavior:

```text
- Validates PATCH_MANIFEST.json.
- Writes patch-runner logs.
- Fails cleanly on no manifest / parse error / validation error.
- Stops before extraction or patch execution.
- Does not auto-commit.
- Does not auto-push.
- Does not change Operator app behavior.
```

Existing patch-runner handoff files:

```text
handoffs/current/patch-runner/RIFTSCAN_PATCH_RUNNER_ALPHA2_HANDOFF.md
handoffs/current/patch-runner/patch-runner-alpha2-handoff.json
```

Known patch-runner commits from prior handoff:

```text
aa33ecf Add patch runner alpha2 handoff
a18cbc9 Add patch runner alpha2 JSON handoff
98aba37 Update patch manifest example for alpha2
f52f064 Add patch manifest validation alpha2
3bb367d Clarify patch runner alpha scope
```

---

## Design Discussion Captured

### ChatGPT/cloud execution boundary

ChatGPT can help with:

```text
- code review
- patch generation
- artifact analysis
- portable Python helper execution against uploaded/available files
- official-documentation lookup through web tools when needed
- GitHub/repo review through connected/indexed sources when available
```

ChatGPT cannot directly do these from the cloud sandbox:

```text
- run Joey's local PowerShell
- open C:\RIFT MODDING\Riftscan directly
- attach to RIFT
- check live HWND/process state
- use Joey's browser session/cookies
- execute local Windows/RIFT helper apps unless Joey runs them locally
```

Therefore, repo helper apps are valuable when they convert local state into structured artifacts:

```text
Local helper runs on Joey's PC
        -> writes JSON/MD/log artifacts
        -> artifacts are committed/pasted/uploaded
        -> ChatGPT analyzes evidence and generates next patch/plan
```

---

## Architecture Decision

### Decision

Prefer an **on-demand Operator workflow** first instead of an always-on listener/server.

### Reason

An always-on service/listener has more failure modes:

```text
- can run at the wrong time
- can conflict with active local edits
- can pull while Joey is working
- can process a bad package automatically
- can create git-state confusion
- requires locking, scheduling, retry logic, and credential handling
```

An on-demand Operator workflow is lower-risk:

```text
Joey opens Operator
        -> clicks Check Online Patch Inbox
        -> Operator lists pending packages/manifests
        -> Joey validates/stages/processes explicitly
        -> Operator writes report
        -> Commit Allowlist + Push records evidence
```

---

## Future Plan — Repo-Delivered Patch Flow

### Phase 0 — Keep current alpha2 baseline

```text
Keep Patch Runner v3.9-alpha2 as manifest-validation-only.
Do not advance to extraction or patch execution until validation tests are boring and repeatable.
```

### Phase 1 — Operator On-Demand Inbox Discovery

Add an Operator button such as:

```text
Check Online Patch Inbox
```

Discovery-only behavior:

```text
- fetch/pull repo inbox metadata
- list available package manifests/pointers
- show package ID, version, hash, requested action
- write discovery report
- no download required unless metadata already exists locally
- no extraction
- no patch application
```

Suggested repo inbox path:

```text
.riftscan/inbox/patch-packages/
```

Suggested local-only workspace:

```text
.riftscan-local/
  package-cache/
  staging/
  logs/
  locks/
```

`.riftscan-local/` should remain untracked unless intentionally adding examples.

### Phase 2 — Validate Package

Add an Operator button such as:

```text
Validate Online Package
```

Behavior:

```text
- download or locate package
- verify SHA256
- verify PATCH_MANIFEST.json schema
- verify expected files
- block unknown requested_action values
- write validation report
- no extraction into live repo
```

### Phase 3 — Extract to Staging

Add an Operator button such as:

```text
Extract Package to Staging
```

Behavior:

```text
- extract only into .riftscan-local/staging/<package_id>/
- block path traversal
- block absolute paths
- block unexpected overwrites
- write staging report
- no live repo modification
```

### Phase 4 — Patch Dry Run

Add an Operator button such as:

```text
Run Patch Dry Run
```

Behavior:

```text
- compare staged files to repo
- list intended adds/modifies/deletes
- write dry-run report
- no real file replacement yet
```

### Phase 5 — Real Apply With Explicit Approval

Only after Phases 1-4 are proven.

Behavior:

```text
- require explicit GUI approval
- create backup metadata
- apply only manifest-allowlisted paths
- write apply report
- run validation commands
- stage explicit paths only
- never run git add .
```

---

## Mandatory Guardrails

The repo-delivered patch flow must be deny-by-default:

```text
- No arbitrary shell command execution from a manifest.
- No always-on background polling until on-demand flow is proven.
- No extraction directly into repo root.
- No path traversal.
- No absolute extraction paths.
- No auto-apply.
- No auto-commit unless a later explicit Operator setting allows it.
- No auto-push unless a later explicit Operator setting allows it.
- No git add .
- No force-push rollback unless deliberately chosen as a last resort.
```

Allowed actions must be names mapped in code, for example:

```python
ALLOWED_ACTIONS = {
    "discover_patch_inbox": discover_patch_inbox,
    "validate_patch_manifest": validate_patch_manifest,
    "validate_package_hash": validate_package_hash,
    "extract_package_to_staging": extract_package_to_staging,
    "run_patch_dry_run": run_patch_dry_run,
}
```

Do not accept raw command fields like:

```json
{ "command": "powershell ..." }
```

---

## Safe Rollback Summary

Recommended rollback:

```text
Soft-rollback any service/listener path to on-demand Operator-only.
Keep alpha2 manifest validator unless specifically rejected.
Preserve handoffs and evidence.
Use git revert for pushed commits, not destructive force-push.
```

Rollback details are in:

```text
handoffs/current/repo-bridge/PATCHER_SERVICE_ROLLBACK_PLAN_2026-05-04.md
handoffs/current/repo-bridge/patcher-service-rollback-plan-2026-05-04.json
```

---

## Next Recommended Task

Do **not** implement an always-on listener next.

Next task should be:

```text
Add Operator on-demand Check Online Patch Inbox — discovery only.
```

This should:

```text
- add a button to tools/riftscan_operator_app.py
- read a repo inbox path or local pulled metadata
- list pending patch manifests/pointers
- validate basic JSON shape only
- write an inbox discovery report
- update operator handoff
- stop before download, extraction, or patch application
```

---

## Resume Prompt For Next Chat

```text
Read handoffs/current/repo-bridge/RIFTSCAN_REPO_BRIDGE_HANDOFF_2026-05-04.md, REPO_DELIVERED_PATCH_FLOW_PLAN_2026-05-04.md, and PATCHER_SERVICE_ROLLBACK_PLAN_2026-05-04.md. Resume RiftScan from the conservative repo-bridge plan. First task: keep Patch Runner v3.9-alpha2 as manifest-validation-only and add an Operator on-demand "Check Online Patch Inbox" discovery-only workflow. Do not add always-on polling, package extraction, patch application, auto-commit, or auto-push.
```
