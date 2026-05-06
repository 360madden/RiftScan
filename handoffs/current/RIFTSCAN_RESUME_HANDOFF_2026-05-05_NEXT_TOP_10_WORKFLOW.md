---
schema_version: riftscan.resume_handoff.v1
handoff_id: RIFTSCAN_RESUME_HANDOFF_2026-05-05_NEXT_TOP_10_WORKFLOW
created_utc: 2026-05-06T01:18:00Z
repo: 360madden/RiftScan
branch: main
local_repo_root: "C:\\RIFT MODDING\\Riftscan"
latest_verified_commit: "115c31a54dbaaec2892735335831a32a4ec7cd02"
latest_verified_commit_subject: "Record post-update baseline pass"
workflow_mode: "Operator-first with scripted manual bridge when needed"
old_offsets_trusted: false
live_collection_allowed_now: false
next_best_task: "Wire Post-Update Baseline into the Operator GUI"
---

> **Historical / superseded notice — 2026-05-06**
>
> This handoff is preserved for audit context. Its original first next task, `Wire Post-Update Baseline into the Operator GUI`, is complete.
>
> Current resume entry point:
>
> ```text
> handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_OPERATOR_GATE_WORKFLOW.md
> handoffs/current/operator/operator-current-gate-summary.json
> ```
>
> Do not use this file's ranked next-step ordering without first checking the current gate summary.

# RiftScan Resume Handoff — Next Top 10 Workflow Plan

## 1. Purpose

This is the current resume handoff for continuing RiftScan from a new ChatGPT Project chat without losing workflow discipline.

It fixes a known continuity problem: new chats sometimes drift into generic advice instead of giving exact scripts, exact PowerShell blocks, validation checks, explicit staging paths, commit/push steps, and verification.

The required continuation pattern is:

```text
plan -> patch/script -> user runs -> user pastes output -> validate -> commit/push -> verify -> document
```

## 2. Current Verified Progress

Latest pushed checkpoint:

```text
115c31a54dbaaec2892735335831a32a4ec7cd02 Record post-update baseline pass
```

Verified state after the game update:

```text
RIFT came back online.
Atank loaded in-world.
Shard: Deepwood
Location: Sanctum of the Vigil
Post-update baseline: PASS
Focus status: foreground_verified
Selected RIFT window: present
RIFT window entries: 1
Repo status after push: clean
```

Important safety state:

```text
old_offsets_trusted: false
live_collection_allowed_now: false
capture_started: false
movement_or_client_action_started: false
reloadui_sent: false
```

This means the repo has a good post-update checkpoint, but it has **not** yet promoted any old offset or discovery assumption.

## 3. Current Important Files

```text
tools/riftscan_operator_app.py
scripts/riftscan-operator-app.cmd

tools/rift_focus_control.py
scripts/run-rift-focus-control.cmd
handoffs/current/focus-control-local/

tools/riftscan_post_update_baseline.py
scripts/run-riftscan-post-update-baseline.cmd
handoffs/current/post-update-baseline/

handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
```

## 4. Future Assistant Workflow Contract

Future ChatGPT responses must follow this contract.

```yaml
workflow_contract:
  default_mode: operator_first
  fallback_mode: exact_powershell_or_scripted_manual_bridge
  required_response_shape:
    - direct_status
    - exact_next_action
    - command_block_when_user_action_is_needed
    - expected_output
    - interpretation_of_user_output
    - explicit_commit_or_cleanup_step
  must_not:
    - use_git_add_dot
    - give_generic_advice_without_commands
    - skip_validation
    - advance_to_collection_before_gate
    - trust_old_offsets_without_revalidation
  must_prefer:
    - one_script_or_one_button_workflows
    - explicit_path_allowlists
    - machine_readable_outputs
    - pass_fail_blocked_status
    - small_verifiable_commits
```

When manual intervention is needed, provide a complete command block and wait for pasted output. Do not give loose instructions like "run the tests" without exact commands.

## 5. Git and Artifact Rules

```yaml
git_rules:
  never:
    - git add .
    - stage unknown junk
    - commit local cache directories
  always:
    - run git status --short before staging
    - stage explicit paths only
    - commit with a precise message
    - push
    - verify with git status --short and git log --oneline -5

artifact_rules:
  every_workflow_should_write:
    - markdown_report_or_handoff
    - json_summary
    - jsonl_log_when_execution_occurs
  every_report_should_include:
    - schema_version
    - created_utc
    - status
    - blockers
    - source_artifacts
    - safety_boundary
```

## 6. Ranked Top 10 Next Steps

### 1. Wire Post-Update Baseline into the Operator GUI

```yaml
rank: 1
priority: P0
reason: The baseline tool works, but it is still outside the Operator GUI. This is the biggest workflow gap.
expected_change:
  - add Operator button named "Post-Update Baseline"
  - call or mirror scripts/run-riftscan-post-update-baseline.cmd
  - display PASS/BLOCKED/FAIL and blocker list
  - update Operator report with latest baseline summary
must_not:
  - auto_commit
  - auto_push
  - start_live_collection
acceptance:
  - GUI produces the same PASS as the CLI tool
  - report includes latest baseline data
  - repo can be committed through explicit allowlist
commit_message: "Wire post-update baseline into operator app"
```

### 2. Add Capture-Readiness Gate — metadata only

```yaml
rank: 2
priority: P0
reason: There should be one controlled gate between baseline PASS and any later runtime collection work.
expected_change:
  - add readiness report
  - consume post-update baseline summary
  - verify focus/full-preflight state
  - keep collection permission false unless explicitly promoted later
outputs:
  - handoffs/current/capture-readiness/CAPTURE_READINESS_REPORT.md
  - handoffs/current/capture-readiness/capture-readiness-summary.json
  - handoffs/current/capture-readiness/capture-readiness-log.jsonl
acceptance:
  - readiness PASS/BLOCKED is explicit
  - blockers are actionable
commit_message: "Add capture readiness gate"
```

### 3. Create a Current Source-of-Truth Pointer

```yaml
rank: 3
priority: P0
reason: Multiple handoffs exist. Future chats need one obvious current entry point.
expected_change:
  - add or update handoffs/current/README_CURRENT.md
  - point to this handoff as current resume source
  - list latest verified commit and first task
acceptance:
  - new chat can resume from one file
commit_message: "Document current RiftScan resume source of truth"
```

### 4. Expand Operator Commit Allowlist

```yaml
rank: 4
priority: P1
reason: New baseline/readiness artifacts need to be staged safely by the Operator.
expected_allowlist_additions:
  - handoffs/current/post-update-baseline
  - handoffs/current/capture-readiness
  - tools/riftscan_post_update_baseline.py
  - scripts/run-riftscan-post-update-baseline.cmd
must_not_allow:
  - .riftscan-local
  - __pycache__
  - payload
  - installer_zip_or_cache_junk
acceptance:
  - Commit Allowlist stages only intended paths
commit_message: "Expand operator allowlist for baseline artifacts"
```

### 5. Add Focus-Gated Capture Plan — metadata only

```yaml
rank: 5
priority: P1
reason: Previous planning identified capture-plan metadata as the next planning layer before any actual collection.
expected_change:
  - create a planned capture manifest
  - record duration target, stimulus name, expected files, preflight requirements, abort conditions, operator notes
  - update Operator report with latest plan summary
preferred_path:
  - plans/focus-gated-capture-plans/
acceptance:
  - plan exists
  - metadata_only is true
  - no runtime collection started
commit_message: "Add focus-gated capture plan workflow"
```

### 6. Add Offline Fixture Tests for Workflow Tools

```yaml
rank: 6
priority: P1
reason: Baseline/readiness/plan logic should be testable without the game client.
expected_tests:
  - baseline blocked without manual world flags
  - baseline pass with fixture foreground data
  - readiness blocked when baseline is missing
  - plan blocked when readiness fails
acceptance:
  - tests run without live game dependency
commit_message: "Add offline workflow fixture tests"
```

### 7. Normalize Workflow Schemas and Status Codes

```yaml
rank: 7
priority: P2
reason: All workflow outputs should share predictable field names.
standard_fields:
  - schema_version
  - created_utc
  - app_version
  - status
  - display_status
  - blockers
  - safety
  - paths
  - source_artifacts
status_values:
  - pass
  - blocked
  - fail_validation
  - fail_exception
acceptance:
  - future tools can consume reports consistently
commit_message: "Normalize workflow report schemas"
```

### 8. Define Passive Session Artifact Schema — design/validator only

```yaml
rank: 8
priority: P2
reason: Before any collection implementation, lock down the artifact shape and validator.
expected_change:
  - document session artifact schema
  - add sample fixture
  - add validator command or test
acceptance:
  - sample fixture validates
  - no live dependency
commit_message: "Define passive session artifact schema"
```

### 9. Implement Minimal Passive Session Vertical Slice

```yaml
rank: 9
priority: P3
reason: Only after gates and schemas exist should a minimal passive session be added.
preconditions:
  - post_update_baseline_pass
  - capture_readiness_pass
  - capture_plan_exists
  - artifact_schema_validator_exists
acceptance:
  - bounded session artifact written
  - manifest and checks are present
  - no analysis mixed into collection step
commit_message: "Add minimal passive session vertical slice"
```

### 10. Add Offline Triage Report From Stored Artifacts

```yaml
rank: 10
priority: P3
reason: Analysis should operate from stored artifacts, not from ad hoc live work.
expected_change:
  - consume stored session artifact
  - produce ranked region/candidate triage report
  - include rejection reasons and next validation recommendation
acceptance:
  - report can run from fixture or stored session
commit_message: "Add offline session triage report"
```

## 7. Explicitly Deferred Work

Do not start these until the gates above exist and pass:

```text
coordinate recovery
actor/camera signal work
stairway capture
active stimulus collection
player-first offset hunting
one-off scanner creation
```

## 8. Optimized New Chat Prompt

Copy/paste this into a new Project chat:

```text
Read `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_NEXT_TOP_10_WORKFLOW.md` and resume RiftScan from it.

Current verified checkpoint:
- Repo: 360madden/RiftScan
- Local path: C:\RIFT MODDING\Riftscan
- Branch: main
- Latest verified commit: 115c31a Record post-update baseline pass
- Repo was clean after push.
- RIFT updated, came back online, and Atank loaded in Sanctum of the Vigil on Deepwood.
- Post-update baseline passed with focus_status=foreground_verified, selected_window_present=true, windows_entry_count=1.
- Old offsets are still untrusted.
- Live collection is still not allowed.

Workflow expectations:
- Use the Operator-first workflow whenever possible.
- When the Operator cannot do a step yet, provide exact PowerShell/script blocks for manual bridge work.
- Chain work as: plan -> patch/script -> user runs -> user pastes output -> validate -> commit/push -> verify -> document.
- Give explicit commands, expected output, and next action.
- Never use `git add .`; stage only explicit safe paths.
- Every meaningful workflow should produce Markdown + JSON + JSONL when execution occurs.
- Keep safety boundaries explicit and do not advance old offsets without gates.

First task:
Wire the existing post-update baseline tool into the RiftScan Operator GUI as a `Post-Update Baseline` button. It must call or mirror `scripts/run-riftscan-post-update-baseline.cmd`, display PASS/BLOCKED/FAIL and blockers in the GUI, update the Operator report with the latest baseline summary, and keep all current guardrails.

After implementation, provide the exact local test commands and expected output. If direct GitHub write access is available, commit/push and verify by reading the pushed commit/file back. If not, provide a precise patch script or zip + one-script apply path.
```

## 9. Machine-Readable Resume Block

```json
{
  "schema_version": "riftscan.resume_handoff.v1",
  "handoff_id": "RIFTSCAN_RESUME_HANDOFF_2026-05-05_NEXT_TOP_10_WORKFLOW",
  "created_utc": "2026-05-06T01:18:00Z",
  "repo": "360madden/RiftScan",
  "branch": "main",
  "local_repo_root": "C:\\RIFT MODDING\\Riftscan",
  "latest_verified_commit": "115c31a54dbaaec2892735335831a32a4ec7cd02",
  "latest_verified_commit_subject": "Record post-update baseline pass",
  "current_checkpoint": {
    "post_update_baseline": "pass",
    "character_name": "Atank",
    "shard": "Deepwood",
    "zone_or_location": "Sanctum of the Vigil",
    "focus_status": "foreground_verified",
    "selected_window_present": true,
    "windows_entry_count": 1,
    "old_offsets_trusted": false,
    "live_collection_allowed_now": false,
    "repo_clean_after_push": true
  },
  "first_task": "Wire Post-Update Baseline into the Operator GUI",
  "ranked_next_steps": [
    "Wire Post-Update Baseline into Operator GUI",
    "Add Capture-Readiness Gate metadata-only",
    "Create Current Source-of-Truth Pointer",
    "Expand Operator Commit Allowlist",
    "Add Focus-Gated Capture Plan metadata-only",
    "Add Offline Fixture Tests for Workflow Tools",
    "Normalize Workflow Schemas and Status Codes",
    "Define Passive Session Artifact Schema design/validator only",
    "Implement Minimal Passive Session Vertical Slice",
    "Add Offline Triage Report From Stored Artifacts"
  ],
  "workflow_contract": {
    "mode": "Operator-first with scripted manual bridge",
    "chain": ["plan", "patch/script", "user_runs", "user_pastes_output", "validate", "commit/push", "verify", "document"],
    "never": ["git add .", "generic advice without commands", "skip validation", "trust old offsets without gates"]
  }
}
```

## 10. Bottom Line

RiftScan is in a clean post-update baseline PASS state. The next optimal work is to put the new baseline capability inside the Operator GUI, then add one capture-readiness gate before any deeper runtime collection or offset work.
