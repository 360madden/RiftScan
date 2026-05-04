# Repo-Delivered Patch Flow Plan — 2026-05-04

## Purpose

Define the safe future design for repo-delivered patch packages without creating an always-on local service as the first implementation.

---

## Starting Point

```text
Patch Runner v3.9-alpha2 = manifest validation only
```

The current alpha2 scope is intentionally narrow. It validates `PATCH_MANIFEST.json`, writes logs, fails cleanly, and stops before extraction or patch execution.

---

## Preferred Mode

```text
RiftScan Operator on-demand workflow
```

Do not start with:

```text
always-on local listener
background polling service
auto-download -> auto-extract -> auto-apply
```

---

## Proposed Paths

Online/repo inbox:

```text
.riftscan/inbox/patch-packages/
```

Local-only workspace:

```text
.riftscan-local/
  package-cache/
  staging/
  logs/
  locks/
```

Result artifacts:

```text
handoffs/current/repo-bridge/
handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
```

---

## Phase Plan

| Phase | Name | Allowed | Blocked |
|---:|---|---|---|
| 0 | Keep alpha2 baseline | manifest validation | download, extract, apply |
| 1 | Check Online Patch Inbox | discover manifests/pointers, write report | package download, extraction, apply |
| 2 | Validate Online Package | cache package, verify SHA256, validate manifest | repo overwrite, apply |
| 3 | Extract Package to Staging | extract to `.riftscan-local/staging/<package_id>/` | live repo modification |
| 4 | Run Patch Dry Run | compare staged files to repo, write dry-run report | live file replacement |
| 5 | Apply Validated Patch | explicit GUI-approved apply, backup metadata, validation | silent apply, `git add .` |

---

## Manifest Model

Suggested package manifest shape:

```json
{
  "schema_version": "riftscan.patch_package.v1",
  "package_id": "20260504T103000Z_alpha3",
  "package_name": "RiftScan_Patch_Runner_Alpha3.zip",
  "source_type": "repo_inbox_or_release_asset",
  "sha256": "EXPECTED_HASH_HERE",
  "requested_action": "validate_extract_stage_only",
  "allowed_processor": "patch_runner",
  "expected_files": [
    "PATCH_MANIFEST.json"
  ]
}
```

---

## Action Model

Use named actions only:

```python
ALLOWED_ACTIONS = {
    "discover_patch_inbox": discover_patch_inbox,
    "validate_patch_manifest": validate_patch_manifest,
    "validate_package_hash": validate_package_hash,
    "extract_package_to_staging": extract_package_to_staging,
    "run_patch_dry_run": run_patch_dry_run,
}
```

Do not permit raw shell command fields.

---

## Next Concrete Implementation

Add a discovery-only Operator button:

```text
Check Online Patch Inbox
```

Expected behavior:

```text
- Reads repo inbox metadata available in the local repo after pull/fetch.
- Lists pending package manifests/pointers.
- Validates only basic JSON shape.
- Writes handoffs/current/repo-bridge/patch-inbox-discovery-result.json.
- Updates handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md.
- Stops before package download, extraction, or patch application.
```

---

## End State

Once proven, the flow can become:

```text
Check Online Patch Inbox
 -> Validate Package
 -> Extract to Staging
 -> Dry Run Apply
 -> Explicitly Approved Apply
 -> Commit Allowlist
 -> Push
```

Do not implement background polling until this on-demand path is stable.
