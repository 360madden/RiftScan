# version: 0.1.0
# purpose: Fixture-only RiftScan CLI smoke validation without RIFT or live process access.
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$KeepOutput
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("riftscan-smoke-" + [Guid]::NewGuid().ToString("N"))
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

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file was not created: $Path"
    }
}

Push-Location $repoRoot
try {
    $fixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/valid-session"
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureSource "manifest.json") -PathType Leaf)) {
        throw "Fixture session not found: $fixtureSource"
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
