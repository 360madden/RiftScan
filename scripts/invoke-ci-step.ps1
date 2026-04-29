# version: 0.1.0
# purpose: Run a CI command while preserving stdout/stderr and machine-readable step status.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Step,

    [Parameter(Mandatory = $true)]
    [string]$LogPath,

    [string]$StatusPath,

    [string[]]$Command
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($null -eq $Command -or $Command.Count -eq 0 -or [string]::IsNullOrWhiteSpace($Command[0])) {
    throw "Provide the command to run with -Command @('<executable>', '<arg1>', ...)."
}

$fullLogPath = [System.IO.Path]::GetFullPath($LogPath)
if ([string]::IsNullOrWhiteSpace($StatusPath)) {
    $logDirectory = [System.IO.Path]::GetDirectoryName($fullLogPath)
    $logName = [System.IO.Path]::GetFileNameWithoutExtension($fullLogPath)
    $StatusPath = Join-Path $logDirectory "$logName-status.json"
}
$fullStatusPath = [System.IO.Path]::GetFullPath($StatusPath)

New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($fullLogPath)) -Force | Out-Null
New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($fullStatusPath)) -Force | Out-Null

$executable = $Command[0]
$arguments = @($Command | Select-Object -Skip 1)
$displayCommand = @($executable) + $arguments
$startedUtc = [DateTimeOffset]::UtcNow
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "+ $($displayCommand -join ' ')"
$output = & $executable @arguments 2>&1
$exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
$stopwatch.Stop()

$output | Tee-Object -FilePath $fullLogPath

[ordered]@{
    schema_version = "riftscan.ci_step_status.v1"
    step = $Step
    exit_code = $exitCode
    command = $displayCommand
    log_path = [System.IO.Path]::GetRelativePath((Get-Location).Path, $fullLogPath).Replace('\', '/')
    started_utc = $startedUtc.ToString("O")
    completed_utc = [DateTimeOffset]::UtcNow.ToString("O")
    elapsed_ms = $stopwatch.ElapsedMilliseconds
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $fullStatusPath -Encoding utf8

if ($exitCode -ne 0) {
    exit $exitCode
}

# END_OF_SCRIPT
