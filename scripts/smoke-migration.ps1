# version: 0.1.0
# purpose: Fixture-only RiftScan session migration smoke validation without RIFT or live process access.
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
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("riftscan-migration-smoke-" + [Guid]::NewGuid().ToString("N"))
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

function Assert-DirectoryExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Expected directory was not created: $Path"
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
    $fixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/valid-session-v0"
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
    $planPath = Join-Path $tempRoot "v0-to-v1-migration-plan.json"
    $migratedSession = Join-Path $tempRoot "migrated-v1-session"
    $applyPlanPath = Join-Path $tempRoot "v0-to-v1-apply-plan.json"

    Invoke-DotNet -Arguments @("build", "RiftScan.slnx", "--configuration", $Configuration)

    $planResult = Invoke-RiftScanJson -Arguments @(
        "migrate", "session", $fixtureSource,
        "--to-schema", "riftscan.session.v1",
        "--plan-out", $planPath
    )
    if (-not $planResult.success -or $planResult.status -ne "planned_source_schema_upgrade") {
        throw "Expected successful planned_source_schema_upgrade dry-run result."
    }
    Assert-FileExists -Path $planPath

    $applyResult = Invoke-RiftScanJson -Arguments @(
        "migrate", "session", $fixtureSource,
        "--to-schema", "riftscan.session.v1",
        "--apply",
        "--out", $migratedSession,
        "--plan-out", $applyPlanPath
    )
    if (-not $applyResult.success -or $applyResult.status -ne "applied_source_schema_upgrade") {
        throw "Expected successful applied_source_schema_upgrade result."
    }
    if ($applyResult.migration_output_path -ne (Resolve-Path $migratedSession).Path) {
        throw "migration_output_path did not match migrated session directory."
    }

    Assert-DirectoryExists -Path $migratedSession
    Assert-FileExists -Path (Join-Path $migratedSession "manifest.json")
    Assert-FileExists -Path (Join-Path $migratedSession "checksums.json")
    Assert-FileExists -Path $applyPlanPath

    $verifyResult = Invoke-RiftScanJson -Arguments @("verify", "session", $migratedSession)
    if (-not $verifyResult.success) {
        throw "Migrated session verification failed."
    }

    $manifest = Get-Content (Join-Path $migratedSession "manifest.json") -Raw | ConvertFrom-Json
    if ($manifest.schema_version -ne "riftscan.session.v1") {
        throw "Migrated manifest schema_version was not riftscan.session.v1."
    }

    $manifestPath = Write-SmokeManifest -OutputRoot $tempRoot -SmokeName "migration"
    Assert-FileExists -Path $manifestPath
    $smokeManifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($smokeManifest.schema_version -ne "riftscan.smoke_manifest.v1" -or $smokeManifest.file_count -lt 1) {
        throw "Migration smoke manifest was invalid."
    }

    Write-Host "Migration smoke passed."
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
