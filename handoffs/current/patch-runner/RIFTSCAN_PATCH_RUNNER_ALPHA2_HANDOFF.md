# RIFTSCAN_PATCH_RUNNER_ALPHA2_HANDOFF_2026-05-03

## Purpose

Resume RiftScan patch-runner development from the current `v3.9-alpha2` state with minimal context loss.

Current milestone completed:

```text
Patch Runner v3.9-alpha2 — manifest validation only
```

The runner is still intentionally conservative. It validates `PATCH_MANIFEST.json` shape and stops before any patch application.

---

## Repo

```text
Repo: 360madden/RiftScan
Local path: C:\RIFT MODDING\Riftscan
Branch: main
Workflow: GitHub main first, then local git pull --ff-only
```

Important local note:

```text
Several patch-runner alpha2 commits were created directly on GitHub main.
The local repo must run git pull --ff-only before testing alpha2 locally.
```

---

## Latest Patch-Runner Code Milestone Before This Handoff

```text
98aba37 Update patch manifest example for alpha2
f52f064 Add patch manifest validation alpha2
3bb367d Clarify patch runner alpha scope
8588366 Add patch runner skeleton
929b355 Add patch runner documentation
55ac845 Add patch runner command launcher
```

This handoff itself may appear as a later commit after those milestones.

---

## Current Important Files

```text
patches/README.md
patches/apply-latest.cmd
patches/apply-latest.ps1
patches/pending/PATCH_MANIFEST.example.json
handoffs/current/patch-runner/
```

Current runner files:

```text
patches/apply-latest.cmd       -> riftscan-patch-runner-cmd-v1.1
patches/apply-latest.ps1       -> riftscan-patch-runner-v3.9-alpha2
PATCH_MANIFEST.example.json    -> riftscan-patch-manifest-example-v3.9-alpha2
patches/README.md              -> riftscan-patch-runner-docs-v1.1
```

---

## What Is Completed

### 1. Patch runner documentation clarified

`patches/README.md` now separates:

```text
Current alpha behavior
Future full-runner target behavior
```

Current alpha behavior is explicitly limited to manifest-path handling, logging, JSON parsing, manifest validation, and stopping before patch application.

---

### 2. Command launcher fixed

`patches/apply-latest.cmd` was updated from `v1.0` to `v1.1`.

The original `.cmd` wrapper had a real Windows batch bug: it could return `0` even when the PowerShell runner exited `2`.

The fixed launcher now routes through labels and preserves `%ERRORLEVEL%` after PowerShell exits.

Known validated local behavior from v3.9-alpha1:

```text
RIFTSCAN PATCH RUNNER: FAIL_NO_PENDING_MANIFEST
$LASTEXITCODE = 2
```

---

### 3. PowerShell runner advanced to v3.9-alpha2

`patches/apply-latest.ps1` is now:

```text
riftscan-patch-runner-v3.9-alpha2
```

It validates `PATCH_MANIFEST.json` only.

It still does not:

```text
run git pull
verify bundle existence
verify bundle hash against an actual file
extract a bundle
compile a patcher
run a patcher
run post-patch validation commands
commit
push
change Operator app behavior
```

---

## Alpha2 Manifest Validation Rules

The alpha2 runner checks these manifest fields:

```text
schema_version
example_only
patch_id
patch_title
created_utc
runner_min_version
status
bundle.path
bundle.sha256
bundle.base64_path
patcher.type
patcher.entry_point
patcher.arguments
expected_extracted_files
validation.post_patch_commands
validation.operator_app_behavior_change_expected
guardrails
```

Validation includes:

```text
schema_version must equal riftscan.patch_manifest.v1
example_only must not be true for a real pending manifest
patch_id must not be placeholder and must use safe characters
patch_title must not be placeholder
created_utc must look like UTC ISO format ending in Z
status must be pending or ready_for_validation
bundle.path must be safe repo-relative and under patches/pending/
bundle.sha256 must be 64 hex characters
bundle.base64_path must be null or safe repo-relative under patches/pending/
patcher.type must be powershell
patcher.entry_point must be safe repo-relative and under patches/pending/.extract/
patcher.arguments must be strings only
expected_extracted_files must be non-empty string array
validation.operator_app_behavior_change_expected must be boolean
guardrails must be non-empty string array
```

---

## Alpha2 Exit Codes

```text
2  fail_no_pending_manifest
3  fail_manifest_parse_error
4  fail_manifest_validation
5  blocked_validation_only
99 fail_unhandled_error
```

Meaning:

```text
exit 2 -> no pending PATCH_MANIFEST.json exists
exit 3 -> PATCH_MANIFEST.json exists but is invalid JSON
exit 4 -> JSON parsed but failed manifest validation
exit 5 -> manifest validation passed, but alpha2 intentionally stops before patch application
```

Exit `5` is expected for a valid manifest at this stage.

---

## Expected Local Pull/Test Workflow

Run locally:

```powershell
cd "C:\RIFT MODDING\Riftscan"
git pull --ff-only
.\patches\apply-latest.cmd
$LASTEXITCODE
```

Expected when no real pending manifest exists:

```text
RIFTSCAN PATCH RUNNER: FAIL_NO_PENDING_MANIFEST
2
```

This proves the default no-manifest failure path still works.

---

## Expected Invalid-Manifest Test

Use the example manifest as a deliberately invalid pending manifest:

```powershell
cd "C:\RIFT MODDING\Riftscan"
Copy-Item -LiteralPath ".\patches\pending\PATCH_MANIFEST.example.json" -Destination ".\patches\pending\PATCH_MANIFEST.json" -Force
.\patches\apply-latest.cmd
$LASTEXITCODE
Remove-Item -LiteralPath ".\patches\pending\PATCH_MANIFEST.json" -Force
```

Expected:

```text
RIFTSCAN PATCH RUNNER: FAIL_MANIFEST_VALIDATION
4
```

Expected blockers include placeholder/example fields such as:

```text
example_only must be false or absent for a runnable pending manifest
patch_id must be a non-placeholder string
patch_title must be a non-placeholder string
status must be pending or ready_for_validation
bundle.sha256 must be a real 64-character hex SHA256 value
```

Do not commit `patches/pending/PATCH_MANIFEST.json` from this test.

---

## Expected Valid-Shape Manifest Test

A temporary valid-shape manifest should pass validation but still stop before patch application.

Expected result:

```text
RIFTSCAN PATCH RUNNER: BLOCKED_VALIDATION_ONLY
5
```

This means alpha2 successfully validated the manifest and refused to continue because patch application is not implemented yet.

Do not commit temporary pending manifests.

---

## Current Log Behavior

The runner writes/overwrites:

```text
handoffs/current/patch-runner/patch-runner-log.jsonl
handoffs/current/patch-runner/patch-runner-summary.json
handoffs/current/patch-runner/patch-runner-output.txt
```

Important: these logs capture `git status --short` at runtime. If tests are run while files are modified or untracked, the summary will show that runtime state. That is expected.

Only commit patch-runner logs when they are intentionally being used as proof artifacts.

---

## Guardrails

The patch runner must continue to obey these boundaries until explicitly advanced:

```text
No patch extraction
No patcher execution
No post-patch command execution
No Operator app behavior change
No automatic commit
No automatic push
No git add .
```

---

## Next Recommended Work

### Immediate next step

Locally validate alpha2:

```text
1. git pull --ff-only
2. run no-manifest test -> expect exit 2
3. run invalid example-manifest test -> expect exit 4
4. run valid-shape manifest test -> expect exit 5
5. commit only intentional proof logs if desired
```

### Next development milestone after proof

```text
Patch Runner v3.9-alpha3 — bundle presence and SHA256 verification only
```

Alpha3 should still not extract or run patchers until bundle verification is proven.

Recommended alpha3 boundaries:

```text
- require clean manifest validation
- verify bundle file exists
- calculate SHA256 of bundle file
- compare against manifest bundle.sha256
- fail cleanly on mismatch
- still do not extract
- still do not run patcher
- still no commit/push
```

---

## New Chat Resume Prompt

```text
Read handoffs/current/patch-runner/RIFTSCAN_PATCH_RUNNER_ALPHA2_HANDOFF.md and handoffs/current/patch-runner/patch-runner-alpha2-handoff.json. Resume RiftScan from Patch Runner v3.9-alpha2. First, help me locally pull and validate alpha2 with no-manifest, invalid example-manifest, and valid-shape manifest tests. Do not advance to extraction or patch execution yet.
```

---

## End of handoff
