# CI smoke artifacts

RiftScan CI preserves fixture-only proof artifacts for every push and pull request run. These artifacts are generated without RIFT or live process access.

## What CI uploads

The `ci` workflow uploads one artifact per commit SHA:

```text
riftscan-smoke-artifacts-<commit-sha>
```

Retention is currently 14 days.

The artifact contains:

- `ci-diagnostics/run-info.json` - workflow/run metadata written before restore/build/test.
- `ci-diagnostics/index.json` - file list, byte sizes, and SHA256 hashes for the diagnostics files present at upload time.
- `ci-diagnostics/dotnet-restore.log` - captured `dotnet restore` output.
- `ci-diagnostics/dotnet-restore-status.json` - machine-readable restore step status with exit code, command, elapsed time, and log path.
- `ci-diagnostics/dotnet-build.log` - captured `dotnet build` output.
- `ci-diagnostics/dotnet-build-status.json` - machine-readable build step status with exit code, command, elapsed time, and log path.
- `ci-diagnostics/dotnet-test.log` - captured `dotnet test` output.
- `ci-diagnostics/dotnet-test-status.json` - machine-readable test step status with exit code, command, elapsed time, and log path.
- `ci-diagnostics/dotnet-format.log` - captured `dotnet format --verify-no-changes` output.
- `ci-diagnostics/dotnet-format-status.json` - machine-readable format step status with exit code, command, elapsed time, and log path.
- `smoke-fixture/` - fixture verify/analyze/report/compare/summary/inventory/prune outputs.
- `smoke-migration/` - v0-to-v1 migration plan/apply/verify outputs.
- `smoke-fixture/smoke-manifest.json` - file list, byte sizes, and SHA256 hashes for fixture smoke artifacts.
- `smoke-migration/smoke-manifest.json` - file list, byte sizes, and SHA256 hashes for migration smoke artifacts.

The `ci-diagnostics/` files are written early so failed runs still upload useful machine-readable context when the smoke artifacts were not reached.

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

The verifier fails nonzero if a manifest is missing required fields, `created_utc` is malformed, a listed file is missing, a file size differs, a SHA256 hash is malformed or differs, a manifest has a bad file count, or a manifest path attempts to escape its output root.

## Regenerate proof artifacts locally

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-fixture.ps1 -KeepOutput -OutputRoot artifacts/smoke-fixture
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-migration.ps1 -KeepOutput -OutputRoot artifacts/smoke-migration
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-smoke-manifest.ps1 -Root artifacts
```

These commands are fixture-only. They do not attach to RIFT and do not use live process access.

## Trace the CLI build

The CLI exposes its package version and source metadata:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- --version
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- --version --json
```

Text output starts with:

```text
riftscan 0.1.0
```

JSON output uses schema `riftscan.version_result.v1` and includes `version`, `informational_version`, and `source_revision`.
