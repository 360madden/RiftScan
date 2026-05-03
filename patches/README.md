# RiftScan Patch Runner

Version: `riftscan-patch-runner-docs-v1.1`

Purpose: Provide a repeatable patch delivery path for RiftScan patches with explicit stages, structured logs, and no automatic commit/push behavior.

## Standard command

```powershell
cd "C:\RIFT MODDING\Riftscan"
.\patches\apply-latest.cmd
```

## Current implementation status

Current runner: `riftscan-patch-runner-v3.9-alpha2`

This is an alpha-stage runner. It is intentionally conservative.

Implemented now:

1. Resolves the repo root from `patches\apply-latest.cmd`.
2. Runs `patches\apply-latest.ps1`.
3. Writes logs to `handoffs\current\patch-runner\`.
4. Fails cleanly when `patches\pending\PATCH_MANIFEST.json` is missing.
5. Parses `PATCH_MANIFEST.json` when present.
6. Validates the pending manifest shape and obvious placeholder/unsafe values.
7. Stops before any patch application.

Not implemented yet:

1. `git pull --ff-only`
2. Bundle file existence checks
3. Bundle SHA256 verification
4. Bundle extraction
5. Patcher compilation
6. Patcher execution
7. Post-patch validation command execution
8. Automatic commit
9. Automatic push

## Full target behavior

The mature runner is intended to eventually:

1. Verify the repo is clean before applying a patch.
2. Run `git pull --ff-only`.
3. Read `patches\pending\PATCH_MANIFEST.json`.
4. Verify the patch bundle exists.
5. Verify the patch bundle SHA256.
6. Extract the bundle to `patches\pending\.extract`.
7. Verify expected extracted files.
8. Compile the patcher.
9. Run the patcher.
10. Compile `tools\riftscan_operator_app.py`.
11. Write patch-runner logs.

## Current alpha exit codes

```text
2  fail_no_pending_manifest
3  fail_manifest_parse_error
4  fail_manifest_validation
5  blocked_validation_only
99 fail_unhandled_error
```

`blocked_validation_only` means the manifest passed validation, but the runner deliberately refused to apply the patch because the current alpha stage is validation-only.

## What the runner does not do

- It does not auto-commit.
- It does not auto-push.
- It does not run the GUI workflow.
- It does not delete backups after patching.
- In `v3.9-alpha2`, it does not extract, compile, execute, or apply patches.

Runtime validation still happens manually through the Operator app.

## Logs

Current logs:

```text
handoffs/current/patch-runner/
```

Archived logs:

```text
handoffs/archive/patch-runs/<timestamp>/
```

## Pending patch files

A pending patch should eventually provide:

```text
patches/pending/PATCH_MANIFEST.json
patches/pending/patch-bundle.zip
patches/pending/patch-bundle.zip.sha256
```

or:

```text
patches/pending/PATCH_MANIFEST.json
patches/pending/patch-bundle.zip.b64
```

when base64 transport is used.

In `v3.9-alpha2`, the manifest is validated, but the referenced bundle files are not opened, extracted, hashed, or executed yet.

## Minimum manifest shape

```json
{
  "schema_version": "riftscan.patch_manifest.v1",
  "example_only": false,
  "patch_id": "short-safe-id",
  "patch_title": "Human-readable patch title",
  "created_utc": "2026-05-03T18:00:00Z",
  "runner_min_version": "riftscan-patch-runner-v3.9-alpha2",
  "status": "pending",
  "bundle": {
    "path": "patches/pending/patch-bundle.zip",
    "sha256": "64 lowercase or uppercase hex characters",
    "base64_path": null
  },
  "patcher": {
    "type": "powershell",
    "entry_point": "patches/pending/.extract/apply-patch.ps1",
    "arguments": []
  },
  "expected_extracted_files": [
    "apply-patch.ps1",
    "PATCH_NOTES.md"
  ],
  "validation": {
    "post_patch_commands": [
      "python -m py_compile tools/riftscan_operator_app.py"
    ],
    "operator_app_behavior_change_expected": false
  },
  "guardrails": [
    "Patch runner must not auto-commit.",
    "Patch runner must not auto-push."
  ]
}
```

## End of document
