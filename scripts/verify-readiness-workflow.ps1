# version: 0.1.0
# purpose: Verify a comparison truth-readiness packet, generate capability status, and verify the capability packet.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TruthReadinessPath,

    [string]$ScalarEvidenceSetPath,

    [string]$CapabilityStatusPath,

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$truthReadinessFullPath = if ([System.IO.Path]::IsPathRooted($TruthReadinessPath)) {
    [System.IO.Path]::GetFullPath($TruthReadinessPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $TruthReadinessPath))
}
if (-not (Test-Path -LiteralPath $truthReadinessFullPath -PathType Leaf)) {
    throw "Truth-readiness file does not exist: $truthReadinessFullPath"
}

$scalarEvidenceSetFullPath = $null
if (-not [string]::IsNullOrWhiteSpace($ScalarEvidenceSetPath)) {
    $scalarEvidenceSetFullPath = if ([System.IO.Path]::IsPathRooted($ScalarEvidenceSetPath)) {
        [System.IO.Path]::GetFullPath($ScalarEvidenceSetPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ScalarEvidenceSetPath))
    }
    if (-not (Test-Path -LiteralPath $scalarEvidenceSetFullPath -PathType Leaf)) {
        throw "Scalar evidence set file does not exist: $scalarEvidenceSetFullPath"
    }
}

if ([string]::IsNullOrWhiteSpace($CapabilityStatusPath)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $CapabilityStatusPath = "reports/generated/capability-status-$stamp.json"
}

$capabilityStatusFullPath = if ([System.IO.Path]::IsPathRooted($CapabilityStatusPath)) {
    [System.IO.Path]::GetFullPath($CapabilityStatusPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $CapabilityStatusPath))
}
$capabilityStatusDirectory = Split-Path -Parent $capabilityStatusFullPath
if (-not [string]::IsNullOrWhiteSpace($capabilityStatusDirectory)) {
    New-Item -ItemType Directory -Force -Path $capabilityStatusDirectory | Out-Null
}

$cliPrefix = @(
    "run",
    "--project",
    "src/RiftScan.Cli/RiftScan.Cli.csproj",
    "--configuration",
    $Configuration,
    "--no-build",
    "--"
)

function Invoke-RiftScan {
    param([string[]]$Arguments)

    Push-Location $repoRoot
    try {
        $command = $script:cliPrefix + $Arguments
        Write-Host "+ dotnet $($command -join ' ')"
        & dotnet @command
        if ($LASTEXITCODE -ne 0) {
            throw "riftscan command failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

Invoke-RiftScan -Arguments @(
    "verify",
    "comparison-readiness",
    $truthReadinessFullPath
)

if (-not [string]::IsNullOrWhiteSpace($scalarEvidenceSetFullPath)) {
    Invoke-RiftScan -Arguments @(
        "verify",
        "scalar-evidence-set",
        $scalarEvidenceSetFullPath
    )
}

$capabilityArguments = @(
    "report",
    "capability",
    "--truth-readiness",
    $truthReadinessFullPath
)
if (-not [string]::IsNullOrWhiteSpace($scalarEvidenceSetFullPath)) {
    $capabilityArguments += @(
        "--scalar-evidence-set",
        $scalarEvidenceSetFullPath
    )
}

$capabilityArguments += @(
    "--json-out",
    $capabilityStatusFullPath
)

Invoke-RiftScan -Arguments $capabilityArguments

Invoke-RiftScan -Arguments @(
    "verify",
    "capability-status",
    $capabilityStatusFullPath
)

[ordered]@{
    schema_version = "riftscan.readiness_workflow_verification.v1"
    success = $true
    truth_readiness_path = $truthReadinessFullPath
    scalar_evidence_set_path = $scalarEvidenceSetFullPath
    capability_status_path = $capabilityStatusFullPath
} | ConvertTo-Json -Depth 4

# END_OF_SCRIPT
