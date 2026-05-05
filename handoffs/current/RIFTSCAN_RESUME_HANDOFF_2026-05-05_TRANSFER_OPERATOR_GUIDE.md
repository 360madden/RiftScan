# RIFTSCAN_RESUME_HANDOFF_2026-05-05_TRANSFER_OPERATOR_GUIDE

```yaml
schema_version: riftscan.resume_handoff.v1
created_utc: "2026-05-05T12:58:00Z"
project: RiftScan
repo: 360madden/RiftScan
local_repo_root: "C:\\RIFT MODDING\\Riftscan"
branch: main
handoff_style: concise_machine_readable_markdown
source_of_truth_priority:
  - git_history
  - committed_repo_artifacts
  - handoffs/current/*
  - this_handoff
```

## Resume baseline

```yaml
current_confirmed_state:
  working_tree: clean
  remote_verified: true
  latest_confirmed_commit:
    sha_short: "12a3c9d"
    sha_full: "12a3c9dc0ec70bc6d81f4cb474e29a7413accb7c"
    message: "Add guided Operator workflow UX"

recent_key_commits:
  - sha_short: "12a3c9d"
    message: "Add guided Operator workflow UX"
    purpose: "Operator v3.8.7 guided workflow UX."
  - sha_short: "476b770"
    message: "Repair patch intake transfer reliability self-tests"
    purpose: "Patch Intake Helper v1.2.1 transfer reliability, chunked payload support, self-test repair."
  - sha_short: "a6d72a8"
    message: "Update RiftScan operator handoff"
    purpose: "Metadata-only Operator workflow proof: plan, bad-focus run, clean retry, analysis, comparison."
  - sha_short: "90f9397"
    message: "Fix patch intake push verification false failure"
    purpose: "Patch Intake Helper v1.1.1 remote-SHA-authoritative push verification."
  - sha_short: "5a0577f"
    message: "Implement patch intake helper v1.1"
    purpose: "Patch Intake Helper process/commit/push button workflow."
```

## Current applications

### Patch Intake Helper

```yaml
component: patch_intake_helper
current_version: "riftscan-patch-intake-v1.2.1"
main_file: "tools/riftscan_patch_intake_app.py"
launcher: "scripts/riftscan-patch-intake.cmd"
purpose:
  - paste/process machine-readable patch payloads
  - validate/dry-run/apply via explicit gates
  - commit and push through explicit buttons
  - avoid long manual scripts
supported_payloads:
  - "RIFTSCAN_CLIPBOARD_PATCH_V1"
  - "RIFTSCAN_CHUNKED_PATCH_V1"
key_buttons:
  - "Process Payload"
  - "Validate Payload"
  - "Dry Run"
  - "Apply Patch"
  - "Commit Result"
  - "Push + Verify Remote"
  - "Run Self-Test"
transfer_reliability:
  chunked_payload_support: true
  expected_diagnostics:
    - bad header
    - bad manifest
    - missing payload markers
    - malformed base64/padding
    - decoded payload hash mismatch
    - chunk hash mismatch
    - missing chunk
  self_test_status_at_handoff: PASS
  known_good_self_test_cases:
    - "chunked dry run: PASS"
    - "chunked bad chunk hash: FAIL_CHUNK_HASH_MISMATCH"
    - "chunked missing chunk: FAIL_MISSING_CHUNK"
```

### Operator Helper App

```yaml
component: operator_helper_app
current_version: "riftscan-operator-app-v3.8.7"
main_file: "tools/riftscan_operator_app.py"
launcher: "scripts/riftscan-operator-app.cmd"
purpose:
  - guided button-pusher workflow
  - focus/full-live preflight
  - metadata-only focus-gated capture planning
  - OS/window/process/focus metadata collector
  - offline analysis and comparison
  - safe allowlisted commit/push
key_tabs_observed:
  - Main
  - Planning
  - Diagnostics
  - Legacy
  - Git
key_v3_8_7_additions:
  - "Workflow Guide"
  - guided button order
  - "LEAVE RIFT FOREGROUND" warning
  - metadata collector focus-discipline confirmation
```

Operator v3.8.7 is not a real capture/memory/input milestone. It is UX/guidance around existing metadata-only workflows.

## Completed milestone summary

### Phase 1 — Patch Intake reliability

```yaml
status: complete
result:
  - Patch Intake Helper v1.1.0 added Process Payload, Commit Result, Push + Verify Remote.
  - v1.1.1 fixed push verification false failure.
  - v1.2.1 added transfer reliability and chunked payload support.
```

Key lesson:

```yaml
long_inline_base64:
  status: discouraged
  reason:
    - fragile paste transport
    - padding errors before gzip/SHA checks
    - hard to diagnose
preferred_payloads:
  small_patch: "compact surgical payload"
  medium_patch: "RIFTSCAN_CHUNKED_PATCH_V1"
  large_patch: "package/zip with manifest + SHA256 only when explicitly needed"
```

### Phase 2 — Metadata-only Operator workflow proof

```yaml
status: complete
commit: "a6d72a8"
created_artifacts:
  capture_plan: "plans/focus-gated-capture-plans/20260505T003511Z_focus_gated_capture_plan"
  failed_focus_session: "sessions/focus-gated-captures/20260505T003619Z_window_process_metadata_collector"
  clean_focus_session: "sessions/focus-gated-captures/20260505T004015Z_window_process_metadata_collector"
result:
  capture_plan: PASS
  first_collector: "PASS artifact contract, but analysis FAIL due to focus loss"
  first_analysis:
    status: FAIL
    focus_lost: 47
    anomalies: 3
    errors: 1
    warnings: 2
  second_collector: PASS
  second_analysis:
    status: PASS
    focus_lost: 0
    anomalies: 0
    errors: 0
    warnings: 0
  comparison:
    status: PASS
    previous_analysis_status: FAIL
    latest_analysis_status: PASS
    difference_count: 5
```

Interpretation:

```yaml
result_meaning:
  - The collector and analyzer are functioning.
  - The analyzer correctly catches bad focus discipline.
  - The workflow passes cleanly when RIFT remains foreground.
  - During metadata collection, the user must not click ChatGPT, PowerShell, the Operator window, or another window.
```

### Phase 3 — Operator guided workflow UX

```yaml
status: complete
commit: "12a3c9dc0ec70bc6d81f4cb474e29a7413accb7c"
message: "Add guided Operator workflow UX"
result:
  - Operator upgraded to v3.8.7.
  - Added/confirmed guided button-pusher workflow.
  - Added foreground-focus warning before metadata collection.
  - Pushed and remote verified.
```

## Interaction and execution policy

The user is a button-pusher/operator. The assistant should handle code generation, diagnostics, planning, and self-checks.

```yaml
user_role:
  - paste payloads
  - click GUI buttons
  - run very short launcher/status commands
  - send outputs/screenshots
assistant_role:
  - design the patch/tooling
  - generate payloads/scripts
  - self-check generated code
  - keep steps sequential
  - avoid unnecessary manual burden
  - verify remote commits when possible
```

### Preferred patch workflow

```yaml
preferred_patch_flow:
  - assistant generates payload
  - assistant self-checks payload/applier before user runs it
  - user opens Patch Intake Helper
  - user pastes payload
  - user clicks "Process Payload"
  - if PASS, user clicks "Commit Result"
  - assistant reviews commit output
  - if good, user clicks "Push + Verify Remote"
  - assistant verifies remote commit if possible
  - cleanup post-push runtime artifacts
```

### Required pre-patcher self-check statement

Before giving the user any payload, provide:

```text
PRE-PATCHER CHECK: PASS

Checked:
- payload/applier compiles
- synthetic apply passes
- target compiles after synthetic apply
- result markers verified
- forbidden-pattern scan passes
- manifest stage paths are explicit
- no git add .
Known limitation:
- actual local repo mutation still requires Patch Intake Helper
```

If not checked:

```text
PRE-PATCHER CHECK: INCOMPLETE
Do not apply yet.
```

### PowerShell policy

Short PowerShell is acceptable. Long pasted PowerShell is not.

```yaml
powershell_rules:
  acceptable:
    - "cd \"C:\\RIFT MODDING\\Riftscan\""
    - "git status --short"
    - ".\\scripts\\riftscan-patch-intake.cmd"
    - ".\\scripts\\riftscan-operator-app.cmd"
    - "Remove-Item -Recurse -Force \".\\tools\\__pycache__\" -ErrorAction SilentlyContinue"
  avoid:
    - long multiline scripts
    - functions / try-catch blocks pasted interactively
    - multi-stage git logic
    - conditional PASS/FAIL reporting in PowerShell
    - cleanup + verify + commit + push in one pasted block
```

Reason: interactive PowerShell can continue after a failure and print misleading PASS lines. Put complex logic into Python or committed helper functions.

### Python/tooling policy

```yaml
python_policy:
  - use Python for complex automation
  - assistant generates Python
  - assistant self-checks Python
  - user only runs short launcher command or uses GUI buttons
  - prefer committed helper features over temporary chat scripts
```

### Cleanup policy

Post-commit/push helper runtime artifacts are usually not committed.

Common cleanup after successful push:

```powershell
cd "C:\RIFT MODDING\Riftscan"
git restore -- handoffs/current/patch-intake/PATCH_INTAKE_REPORT.md handoffs/current/patch-intake/patch-intake-log.jsonl
Remove-Item -LiteralPath ".\handoffs\current\patch-intake\last-commit-result.json", ".\handoffs\current\patch-intake\last-push-result.json" -Force -ErrorAction SilentlyContinue
git status --short
```

Use only if those are the only dirty files. Do not clean real code/artifacts blindly.

## Hard constraints

```yaml
hard_forbidden:
  - git add .
  - hidden auto-commit
  - hidden auto-push
  - force push unless explicitly chosen as last resort
  - service install
  - listener
  - polling
  - scheduled task
  - arbitrary shell commands from manifests
  - raw shell commands from payloads
  - movement/input/mouse/reloadui during metadata-only work
  - process memory read during metadata-only workflow
```

## Operator workflow criteria

Use Operator v3.8.7 as the button-pusher front end.

Recommended sequence:

```yaml
operator_sequence:
  - "Workflow Guide"
  - "Run Full Live Preflight"
  - "Create Focus-Gated Capture Plan"
  - "Run Window/Process Metadata Collector"
  - "Analyze Latest Session"
  - "Compare Sessions"
  - "Clean Known Junk"
  - "Commit Allowlist"
  - "Push"
```

Focus-discipline rule:

```yaml
metadata_collection_focus_rule:
  during_collector_window:
    user_must_not_click:
      - ChatGPT
      - PowerShell
      - Operator window
      - other apps
    expected_clean_analysis:
      focus_lost_count: 0
      anomalies: 0
      errors: 0
      warnings: 0
```

If focus is lost:

```yaml
focus_loss_handling:
  - do not assume code defect
  - retry with RIFT foreground and hands off
  - preserve failure artifact only if useful for comparison
  - analysis FAIL with focus_lost > 0 is expected behavior
```

## Current recommended next work

Do not re-add Run Full Live Preflight. It already exists.

```yaml
next_options:
  preferred:
    id: operator_v3_8_7_visual_validation
    goal: "Launch Operator and visually confirm Workflow Guide, Main-tab sequence, and LEAVE RIFT FOREGROUND warning."
    no_repo_patch_needed_unless: "UI guidance is missing/wrong."

  next_engineering_milestone:
    id: operator_v3_8_8_guided_state_status
    goal: "Add clearer status labels/recommended next step state machine if v3.8.7 guide is still too manual."
    scope:
      - visible PASS/FAIL state labels
      - recommended next step line
      - no live capture expansion
      - no memory/input/reloadui

  later:
    id: real_capture_planning
    goal: "Plan transition from metadata-only collector toward real focus-gated capture."
    prerequisite:
      - stable guided workflow
      - clean focus discipline
      - explicit capture contract
      - no ambiguity about whether movement/input/memory read is allowed
```

## Current safe resume prompt

```text
Resume RiftScan from the latest pushed main.

Current confirmed state:
- Working tree was clean after Operator v3.8.7 push/cleanup.
- Patch Intake Helper is riftscan-patch-intake-v1.2.1.
- Operator Helper App is riftscan-operator-app-v3.8.7.
- Latest key commit is 12a3c9d / 12a3c9dc0ec70bc6d81f4cb474e29a7413accb7c, "Add guided Operator workflow UX".
- Patch Intake supports RIFTSCAN_CLIPBOARD_PATCH_V1 and RIFTSCAN_CHUNKED_PATCH_V1.
- Operator metadata-only workflow has been proven with one bad-focus FAIL session and one clean-focus PASS session.

Use Patch Intake Helper for repo patches:
Process Payload -> Commit Result -> Push + Verify Remote.
Avoid long pasted PowerShell; use Python/helper buttons for complex logic.
No git add .
No hidden auto-commit or auto-push.
No service/listener/polling.
For Operator metadata collection, RIFT must remain foreground.

Next task:
First visually validate Operator v3.8.7 Workflow Guide and focus warning. If the guide is correct, plan the next safe milestone: either Operator v3.8.8 guided state/status labels or real-capture planning. Do not jump into movement/input/memory-read capture without an explicit plan and gate.
```

## End marker

```yaml
end_marker: END_RIFTSCAN_RESUME_HANDOFF_2026_05_05_TRANSFER_OPERATOR_GUIDE
```
