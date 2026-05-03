# RiftScan Patch Runner

Version: `riftscan-patch-runner-docs-v1.0`

Purpose: Provide a repeatable patch delivery path for RiftScan patches with `git pull --ff-only`, bundle hash verification, controlled extraction, patch execution, compile checks, and patch-runner logs.

## Standard command

```powershell
cd "C:\\RIFT MODDING\\Riftscan"
.\patches\apply-latest.cmd
```

## What the runner does

1. Verifies the repo is clean before applying a patch.
2. Runs `git pull --ff-only`.
3. Reads `patches\pending\PATCH_MANIFEST.json`.
4. Verifies the patch bundle exists.
5. Verifies the patch bundle SHA256.
6. Extracts the bundle to `patches\pending\.extract`.
7. Verifies expected extracted files.
8. Compiles the patcher.
9. Runs the patcher.
10. Compiles `tools\riftscan_operator_app.py`.
11. Writes patch-runner logs.

## What the runner does not do

- It does not auto-commit.
- It does not auto-push.
- It does not run the GUI workflow.
- It does not delete backups after patching.

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

A pending patch should provide:

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

## End of document
