# version: 0.1.0
# purpose: Fixture-only RiftScan CLI smoke validation without RIFT or live process access.
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$KeepOutput,
    [string]$OutputRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("riftscan-smoke-" + [Guid]::NewGuid().ToString("N"))
}
else {
    $tempRoot = [System.IO.Path]::GetFullPath($OutputRoot)
}
$cliPrefix = @("run", "--project", "src/RiftScan.Cli/RiftScan.Cli.csproj", "--configuration", $Configuration, "--no-build", "--")

function Invoke-DotNet {
    param([string[]]$Arguments)

    Write-Host "+ dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Invoke-RiftScan {
    param([string[]]$Arguments)

    Invoke-DotNet -Arguments ($script:cliPrefix + $Arguments)
}

function Invoke-RiftScanJson {
    param([string[]]$Arguments)

    $command = $script:cliPrefix + $Arguments
    Write-Host "+ dotnet $($command -join ' ')"
    $output = & dotnet @command
    if ($LASTEXITCODE -ne 0) {
        throw "riftscan command failed with exit code $LASTEXITCODE. Output: $output"
    }

    return ($output | ConvertFrom-Json)
}

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file was not created: $Path"
    }
}

function Write-SmokeManifest {
    param(
        [string]$OutputRoot,
        [string]$SmokeName
    )

    $fullOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    $files = Get-ChildItem -LiteralPath $fullOutputRoot -File -Recurse |
        Where-Object { $_.Name -ne "smoke-manifest.json" } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [System.IO.Path]::GetRelativePath($fullOutputRoot, $_.FullName).Replace('\', '/')
            [ordered]@{
                path = $relativePath
                bytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }

    $manifest = [ordered]@{
        schema_version = "riftscan.smoke_manifest.v1"
        smoke_name = $SmokeName
        output_root = $fullOutputRoot
        created_utc = [DateTimeOffset]::UtcNow.ToString("O")
        file_count = @($files).Count
        files = @($files)
    }

    $manifestPath = Join-Path $fullOutputRoot "smoke-manifest.json"
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    return $manifestPath
}
Push-Location $repoRoot
try {
    $fixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/valid-session"
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureSource "manifest.json") -PathType Leaf)) {
        throw "Fixture session not found: $fixtureSource"
    }

    if (Test-Path -LiteralPath $tempRoot) {
        $repoArtifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
        $systemTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $isSafeOutputRoot = $tempRoot.StartsWith($repoArtifactRoot, [StringComparison]::OrdinalIgnoreCase) -or
            $tempRoot.StartsWith($systemTempRoot, [StringComparison]::OrdinalIgnoreCase)
        if (-not $isSafeOutputRoot) {
            throw "Refusing to clean existing smoke output outside repo artifacts or system temp: $tempRoot"
        }

        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    $sessionA = Join-Path $tempRoot "session-a"
    $sessionB = Join-Path $tempRoot "session-b"
    $reportRoot = Join-Path $tempRoot "reports"
    New-Item -ItemType Directory -Path $reportRoot | Out-Null
    Copy-Item -Path $fixtureSource -Destination $sessionA -Recurse
    Copy-Item -Path $fixtureSource -Destination $sessionB -Recurse

    Invoke-DotNet -Arguments @("build", "RiftScan.slnx", "--configuration", $Configuration)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionA)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionA, "--top", "10")
    Invoke-RiftScan -Arguments @("report", "session", $sessionA, "--top", "10")
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionB, "--top", "10")

    $comparisonJson = Join-Path $reportRoot "fixture-comparison.json"
    $comparisonMarkdown = Join-Path $reportRoot "fixture-comparison.md"
    $nextPlan = Join-Path $reportRoot "fixture-next-capture-plan.json"
    Invoke-RiftScan -Arguments @(
        "compare", "sessions", $sessionA, $sessionB,
        "--top", "10",
        "--out", $comparisonJson,
        "--report-md", $comparisonMarkdown,
        "--next-plan", $nextPlan
    )

    Assert-FileExists -Path (Join-Path $sessionA "report.md")
    Assert-FileExists -Path $comparisonJson
    Assert-FileExists -Path $comparisonMarkdown
    Assert-FileExists -Path $nextPlan

    $summaryJson = Join-Path $reportRoot "fixture-session-summary.json"
    $summaryResult = Invoke-RiftScanJson -Arguments @("session", "summary", $sessionA, "--json-out", $summaryJson)
    if (-not $summaryResult.success -or $summaryResult.artifact_count -lt 1) {
        throw "Expected successful session summary with generated artifacts."
    }

    Assert-FileExists -Path $summaryJson

    $sessionInventory = Join-Path $reportRoot "fixture-session-inventory.json"
    $inventoryResult = Invoke-RiftScanJson -Arguments @("session", "inventory", $sessionA, "--json-out", $sessionInventory)
    if (-not $inventoryResult.success -or $inventoryResult.summary.artifact_count -lt 1 -or $inventoryResult.prune_inventory.candidate_count -lt 1) {
        throw "Expected successful session inventory with generated artifacts and prune candidates."
    }
    Assert-FileExists -Path $sessionInventory

    $pruneInventory = Join-Path $reportRoot "fixture-prune-inventory.json"
    $pruneResult = Invoke-RiftScanJson -Arguments @("session", "prune", $sessionA, "--dry-run", "--json-out", $pruneInventory)
    if (-not $pruneResult.success -or -not $pruneResult.dry_run) {
        throw "Expected successful session prune dry-run result."
    }
    if ($pruneResult.candidate_count -lt 1) {
        throw "Expected session prune dry-run to find generated artifact candidates."
    }
    Assert-FileExists -Path (Join-Path $sessionA "report.md")
    Assert-FileExists -Path $pruneInventory

    $manifestPath = Write-SmokeManifest -OutputRoot $tempRoot -SmokeName "fixture"
    Assert-FileExists -Path $manifestPath
    $smokeManifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($smokeManifest.schema_version -ne "riftscan.smoke_manifest.v1" -or $smokeManifest.file_count -lt 1) {
        throw "Fixture smoke manifest was invalid."
    }

    Write-Host "Fixture smoke passed."
    if ($KeepOutput) {
        Write-Host "Output preserved: $tempRoot"
    }
}
finally {
    Pop-Location
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

# END_OF_SCRIPT
