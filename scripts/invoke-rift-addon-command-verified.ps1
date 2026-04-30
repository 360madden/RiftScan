# version: 0.1.0
# purpose: Send a bounded RIFT addon slash command, rescan SavedVariables, and verify expected addon/API state.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CommandText,

    [string]$ExpectedLastCommand,

    [ValidateSet("any", "true", "false")]
    [string]$ExpectedWaypointHasWaypoint = "any",

    [string]$SavedVariablesRoot = "C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\Saved",

    [string]$AddonName = "ReaderBridgeExport",

    [string]$OutputDirectory = ".\reports\generated",

    [string]$Label,

    [string]$RiftScanCliPath = ".\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll",

    [string]$SenderPath = ".\scripts\send-rift-slash-command.ps1",

    [string]$TargetProcessName = "rift_x64",

    [int]$TargetProcessId,

    [string]$TargetWindowHandle,

    [string]$TargetTitleContains = "RIFT",

    [switch]$ReloadUiAfterCommand,

    [switch]$SkipSend,

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $CommandText.StartsWith("/", [System.StringComparison]::Ordinal)) {
    throw "CommandText must be a slash command that starts with '/'."
}

if (-not (Test-Path -LiteralPath $SavedVariablesRoot)) {
    throw "SavedVariablesRoot was not found: $SavedVariablesRoot"
}

$expectedWaypointHasWaypointValue = switch ($ExpectedWaypointHasWaypoint) {
    "true" { $true }
    "false" { $false }
    default { $null }
}

function ConvertTo-SafeLabel {
    param([string]$Value)

    $safe = if ([string]::IsNullOrWhiteSpace($Value)) {
        "addon-command"
    }
    else {
        $Value.Trim().ToLowerInvariant() -replace '[^a-z0-9_.-]+', '-'
    }

    return $safe.Trim("-")
}

function Invoke-JsonCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processStartInfo.FileName = "powershell.exe"
    $processStartInfo.UseShellExecute = $false
    $processStartInfo.RedirectStandardOutput = $true
    $processStartInfo.RedirectStandardError = $true
    $processStartInfo.Arguments = (@("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $FilePath) + $Arguments |
        ForEach-Object { '"' + ([string]$_ -replace '"', '\"') + '"' }) -join " "

    $process = [System.Diagnostics.Process]::Start($processStartInfo)
    $stdoutText = $process.StandardOutput.ReadToEnd()
    $stderrText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    return [pscustomobject]@{
        exit_code = $process.ExitCode
        stdout = @($stdoutText -split "\r?\n" | Where-Object { $_ -ne "" })
        stderr = @($stderrText -split "\r?\n" | Where-Object { $_ -ne "" })
    }
}

function Invoke-RiftScanCli {
    param([string[]]$Arguments)

    $resolvedCliPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RiftScanCliPath)
    if (Test-Path -LiteralPath $resolvedCliPath) {
        $output = & dotnet $resolvedCliPath @Arguments 2>&1
        return [pscustomobject]@{
            exit_code = $LASTEXITCODE
            command = "dotnet $resolvedCliPath"
            output = @($output | ForEach-Object { [string]$_ })
        }
    }

    $projectPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(".\src\RiftScan.Cli\RiftScan.Cli.csproj")
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "RiftScan CLI was not found at '$resolvedCliPath', and project fallback was not found at '$projectPath'. Build Release first."
    }

    $output = & dotnet run --project $projectPath --configuration Release -- @Arguments 2>&1
    return [pscustomobject]@{
        exit_code = $LASTEXITCODE
        command = "dotnet run --project $projectPath --configuration Release --"
        output = @($output | ForEach-Object { [string]$_ })
    }
}

function Get-WaypointStatusObservation {
    param([object]$Scan)

    return @($Scan.observations | Where-Object { $_.kind -eq "waypoint_status" } | Select-Object -First 1)[0]
}

$startedUtc = (Get-Date).ToUniversalTime()
$labelValue = ConvertTo-SafeLabel -Value $(if ([string]::IsNullOrWhiteSpace($Label)) { $CommandText.TrimStart("/") } else { $Label })
$stamp = $startedUtc.ToString("yyyyMMdd-HHmmss", [System.Globalization.CultureInfo]::InvariantCulture)
$outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$jsonOut = Join-Path $outputRoot "addon-api-observation-scan-$labelValue-$stamp.json"
$jsonlOut = Join-Path $outputRoot "addon-api-observations-$labelValue-$stamp.jsonl"
$resultOut = Join-Path $outputRoot "verified-addon-command-$labelValue-$stamp.json"

$sendResult = $null
$reloadResult = $null
$sendArguments = @(
    "-CommandText", $CommandText,
    "-TargetProcessName", $TargetProcessName,
    "-TargetTitleContains", $TargetTitleContains
)
if ($TargetProcessId -gt 0) {
    $sendArguments += @("-TargetProcessId", [string]$TargetProcessId)
}

if (-not [string]::IsNullOrWhiteSpace($TargetWindowHandle)) {
    $sendArguments += @("-TargetWindowHandle", $TargetWindowHandle)
}

if ($DryRun) {
    $sendArguments += "-DryRun"
}

if (-not $SkipSend) {
    $senderFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SenderPath)
    if (-not (Test-Path -LiteralPath $senderFullPath)) {
        throw "SenderPath was not found: $senderFullPath"
    }

    $sendArguments += "-Focus"
    $sendResult = Invoke-JsonCommand -FilePath $senderFullPath -Arguments $sendArguments

    if ($ReloadUiAfterCommand -and $sendResult.exit_code -eq 0) {
        $reloadArguments = @(
            "-CommandText", "/reloadui",
            "-TargetProcessName", $TargetProcessName,
            "-TargetTitleContains", $TargetTitleContains,
            "-Focus"
        )
        if ($TargetProcessId -gt 0) {
            $reloadArguments += @("-TargetProcessId", [string]$TargetProcessId)
        }

        if (-not [string]::IsNullOrWhiteSpace($TargetWindowHandle)) {
            $reloadArguments += @("-TargetWindowHandle", $TargetWindowHandle)
        }

        if ($DryRun) {
            $reloadArguments += "-DryRun"
        }

        $reloadResult = Invoke-JsonCommand -FilePath $senderFullPath -Arguments $reloadArguments
    }
}

$scanArgs = @(
    "rift",
    "addon-api-observations",
    $SavedVariablesRoot,
    "--addon-name",
    $AddonName,
    "--jsonl-out",
    $jsonlOut,
    "--json-out",
    $jsonOut
)
$scanResult = Invoke-RiftScanCli -Arguments $scanArgs
if ($scanResult.exit_code -ne 0) {
    throw "addon-api-observations failed with exit code $($scanResult.exit_code): $($scanResult.output -join [Environment]::NewLine)"
}

$scan = Get-Content -LiteralPath $jsonOut -Raw | ConvertFrom-Json
$status = Get-WaypointStatusObservation -Scan $scan
$failures = New-Object System.Collections.Generic.List[string]

if (-not $SkipSend -and $sendResult -and $sendResult.exit_code -ne 0) {
    $failures.Add("send_command_failed_exit_code_$($sendResult.exit_code)")
}

if ($ReloadUiAfterCommand -and $reloadResult -and $reloadResult.exit_code -ne 0) {
    $failures.Add("reloadui_command_failed_exit_code_$($reloadResult.exit_code)")
}

if (-not $status) {
    $failures.Add("waypoint_status_observation_missing")
}
else {
    if (-not [string]::IsNullOrWhiteSpace($ExpectedLastCommand) -and
        -not [string]::Equals([string]$status.waypoint_last_command, $ExpectedLastCommand, [System.StringComparison]::OrdinalIgnoreCase)) {
        $failures.Add("waypoint_last_command_mismatch")
    }

    if ($null -ne $expectedWaypointHasWaypointValue -and
        [bool]$status.waypoint_has_waypoint -ne $expectedWaypointHasWaypointValue) {
        $failures.Add("waypoint_has_waypoint_mismatch")
    }
}

$success = $failures.Count -eq 0
$result = [pscustomobject]@{
    result_schema_version = "riftscan.verified_addon_command_result.v1"
    success = $success
    dry_run = $DryRun.IsPresent
    skip_send = $SkipSend.IsPresent
    command_text = $CommandText
    expected_last_command = $ExpectedLastCommand
    expected_waypoint_has_waypoint = $expectedWaypointHasWaypointValue
    sender_path = if ($SkipSend) { $null } else { $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SenderPath) }
    send_exit_code = if ($sendResult) { $sendResult.exit_code } else { $null }
    send_stdout = if ($sendResult) { $sendResult.stdout } else { @() }
    send_stderr = if ($sendResult) { $sendResult.stderr } else { @() }
    reloadui_after_command = $ReloadUiAfterCommand.IsPresent
    reload_exit_code = if ($reloadResult) { $reloadResult.exit_code } else { $null }
    reload_stdout = if ($reloadResult) { $reloadResult.stdout } else { @() }
    reload_stderr = if ($reloadResult) { $reloadResult.stderr } else { @() }
    saved_variables_root = $SavedVariablesRoot
    addon_name = $AddonName
    scan_json_path = $jsonOut
    scan_jsonl_path = $jsonlOut
    observation_count = [int]$scan.observation_count
    waypoint_anchor_count = [int]$scan.waypoint_anchor_count
    waypoint_status_present = [bool]($null -ne $status)
    waypoint_has_waypoint = if ($status) { $status.waypoint_has_waypoint } else { $null }
    waypoint_last_command = if ($status) { $status.waypoint_last_command } else { $null }
    waypoint_update_count = if ($status) { $status.waypoint_update_count } else { $null }
    waypoint_x = if ($status) { $status.waypoint_x } else { $null }
    waypoint_z = if ($status) { $status.waypoint_z } else { $null }
    failures = $failures.ToArray()
    result_path = $resultOut
    started_utc = $startedUtc.ToString("o", [System.Globalization.CultureInfo]::InvariantCulture)
    completed_utc = (Get-Date).ToUniversalTime().ToString("o", [System.Globalization.CultureInfo]::InvariantCulture)
}

$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resultOut -Encoding UTF8
$result | ConvertTo-Json -Depth 6

if (-not $success) {
    exit 1
}

# END_OF_SCRIPT
