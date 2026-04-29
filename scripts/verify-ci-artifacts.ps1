# version: 0.1.0
# purpose: Verify all RiftScan CI artifact integrity proofs from one command.
[CmdletBinding()]
param(
    [string]$Root = "artifacts"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$fullRoot = [System.IO.Path]::GetFullPath($Root)
if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
    throw "CI artifact root does not exist: $fullRoot"
}

$smokeVerifier = Join-Path $PSScriptRoot "verify-smoke-manifest.ps1"
$diagnosticsVerifier = Join-Path $PSScriptRoot "verify-ci-diagnostics-index.ps1"
$diagnosticsRoot = Join-Path $fullRoot "ci-diagnostics"

if (-not (Test-Path -LiteralPath $diagnosticsRoot -PathType Container)) {
    throw "CI diagnostics directory does not exist: $diagnosticsRoot"
}

$smokeJson = & $smokeVerifier -Root $fullRoot
$smokeManifests = @($smokeJson | ConvertFrom-Json)

$diagnosticsJson = & $diagnosticsVerifier -Root $diagnosticsRoot
$diagnosticsResult = $diagnosticsJson | ConvertFrom-Json

[ordered]@{
    result_schema_version = "riftscan.ci_artifacts_verification.v1"
    success = $true
    root = $fullRoot
    smoke_manifest_count = $smokeManifests.Count
    smoke_manifests = $smokeManifests
    diagnostics_index = $diagnosticsResult
} | ConvertTo-Json -Depth 8

# END_OF_SCRIPT
