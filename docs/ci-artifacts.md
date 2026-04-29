# CI smoke artifacts

RiftScan CI preserves fixture-only proof artifacts for every push and pull request run. These artifacts are generated without RIFT or live process access.

## What CI uploads

The `ci` workflow uploads one artifact per commit SHA:

```text
riftscan-smoke-artifacts-<commit-sha>
```

Retention is currently 14 days.

The artifact contains:

- `smoke-fixture/` - fixture verify/analyze/report/compare/summary/inventory/prune outputs.
- `smoke-migration/` - v0-to-v1 migration plan/apply/verify outputs.
- `smoke-fixture/smoke-manifest.json` - file list, byte sizes, and SHA256 hashes for fixture smoke artifacts.
- `smoke-migration/smoke-manifest.json` - file list, byte sizes, and SHA256 hashes for migration smoke artifacts.

## Download from GitHub Actions

1. Open the CI run from the README badge or the Actions tab.
2. Open the completed `build-test-smoke` job.
3. Read the `RiftScan smoke artifacts` job summary for artifact name, manifest count, file counts, and byte totals.
4. Download the artifact named `riftscan-smoke-artifacts-<commit-sha>`.
5. Extract it to a local directory, for example `artifacts/` at the repository root.

## Verify downloaded artifacts locally

From the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-smoke-manifest.ps1 -Root artifacts
```

Expected output is JSON similar to:

```json
[
  {
    "manifest_path": "C:\\path\\to\\artifacts\\smoke-fixture\\smoke-manifest.json",
    "smoke_name": "fixture",
    "file_count": 34,
    "output_root": "C:\\path\\to\\artifacts\\smoke-fixture"
  },
  {
    "manifest_path": "C:\\path\\to\\artifacts\\smoke-migration\\smoke-manifest.json",
    "smoke_name": "migration",
    "file_count": 8,
    "output_root": "C:\\path\\to\\artifacts\\smoke-migration"
  }
]
```

The verifier fails nonzero if a listed file is missing, a file size differs, a SHA256 hash differs, a manifest has a bad file count, or a manifest path attempts to escape its output root.

## Regenerate proof artifacts locally

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-fixture.ps1 -KeepOutput -OutputRoot artifacts/smoke-fixture
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-migration.ps1 -KeepOutput -OutputRoot artifacts/smoke-migration
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-smoke-manifest.ps1 -Root artifacts
```

These commands are fixture-only. They do not attach to RIFT and do not use live process access.
