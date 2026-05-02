# version: 0.1.0
# purpose: Stale-guarded manual live-testing wrapper for RiftScan without adding core capture features.
[CmdletBinding()]
param(
    [ValidateSet("passive_idle", "passive_world_activity", "move_forward", "turn_left", "turn_right", "camera_only")]
    [string]$Stimulus = "move_forward",

    [int]$ProcessId = 0,

    [string]$ProcessName = "rift_x64",

    [string]$RiftReaderRepo = "C:\RIFT MODDING\RiftReader",

    [string]$RiftScanCliProject = "",

    [string]$ReaderBridgeFile = "",

    [int]$MaxReaderBridgeAgeSeconds = 300,

    [int]$PreCaptureWaitMilliseconds = 10000,

    [int]$Samples = 80,

    [int]$IntervalMilliseconds = 150,

    [int]$MaxRegions = 3,

    [int]$MaxBytesPerRegion = 4096,

    [int64]$MaxTotalBytes = 1048576,

    [string[]]$ExtraBaseAddress = @(),

    [switch]$AllowUnproven,

    [switch]$PreflightOnly,

    [string]$RunRoot = "reports\generated",

    [string]$SessionRoot = "sessions"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([Parameter(Mandatory)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSScriptRoot) $Path))
}

function New-Directory {
    param([Parameter(Mandatory)][string]$Path)
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory)]$Value,
        [Parameter(Mandatory)][string]$Path
    )
    New-Directory -Path (Split-Path -Parent $Path)
    $Value | ConvertTo-Json -Depth 32 | Set-Content -Path $Path -Encoding UTF8
}

function Resolve-RiftProcess {
    param([int]$RequestedProcessId, [string]$RequestedProcessName)

    if ($RequestedProcessId -gt 0) {
        $process = Get-Process -Id $RequestedProcessId -ErrorAction Stop
        if ($RequestedProcessName -and $process.ProcessName -ne $RequestedProcessName -and "$($process.ProcessName).exe" -ne $RequestedProcessName) {
            throw "Process ID $RequestedProcessId is '$($process.ProcessName)', not '$RequestedProcessName'."
        }
        return $process
    }

    $name = $RequestedProcessName
    if ($name.EndsWith(".exe", [StringComparison]::OrdinalIgnoreCase)) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($name)
    }

    $matches = @(Get-Process -Name $name -ErrorAction SilentlyContinue)
    if ($matches.Count -eq 0) {
        throw "No process found with name '$RequestedProcessName'. Start RIFT or pass -ProcessId."
    }

    $windowed = @($matches | Where-Object { -not [string]::IsNullOrWhiteSpace($_.MainWindowTitle) })
    if ($windowed.Count -eq 1) {
        return $windowed[0]
    }

    if ($matches.Count -eq 1) {
        return $matches[0]
    }

    $ids = ($matches | ForEach-Object { "$($_.Id):$($_.MainWindowTitle)" }) -join ", "
    throw "Multiple '$RequestedProcessName' processes found ($ids). Re-run with -ProcessId <pid>."
}

function Resolve-ReaderBridgeFile {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "ReaderBridge file not found: $ExplicitPath"
        }
        return [System.IO.Path]::GetFullPath($ExplicitPath)
    }

    $savedRoot = Join-Path $env:USERPROFILE "OneDrive\Documents\RIFT\Interface\Saved"
    if (-not (Test-Path -LiteralPath $savedRoot)) {
        throw "Could not find default RIFT SavedVariables root: $savedRoot. Pass -ReaderBridgeFile."
    }

    $latest = Get-ChildItem -Path $savedRoot -Recurse -Filter "ReaderBridgeExport.lua" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latest) {
        throw "No ReaderBridgeExport.lua found under $savedRoot. Pass -ReaderBridgeFile."
    }

    return $latest.FullName
}

function Invoke-JsonCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$StdoutPath,
        [string]$WorkingDirectory = ""
    )

    $oldLocation = Get-Location
    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        Set-Location -LiteralPath $WorkingDirectory
    }

    try {
        $output = & $FilePath @Arguments
        $exitCode = if ($global:LASTEXITCODE -is [int]) { $global:LASTEXITCODE } else { 0 }
        New-Directory -Path (Split-Path -Parent $StdoutPath)
        $output | Set-Content -Path $StdoutPath -Encoding UTF8
        if ($exitCode -ne 0) {
            throw "Command failed with exit code ${exitCode}: $FilePath $($Arguments -join ' ')"
        }

        $text = ($output -join [Environment]::NewLine).Trim()
        if ([string]::IsNullOrWhiteSpace($text)) {
            throw "Command produced no JSON output: $FilePath $($Arguments -join ' ')"
        }

        return $text | ConvertFrom-Json
    }
    finally {
        Set-Location $oldLocation
    }
}

function Invoke-RiftScanJson {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$StdoutPath
    )

    return Invoke-JsonCommand -FilePath "dotnet" -Arguments (@("run", "--project", $script:RiftScanCliProjectFull, "--") + $Arguments) -StdoutPath $StdoutPath
}

function ConvertTo-Float32 {
    param([byte[]]$Bytes, [int]$Offset)
    if ($Offset + 4 -gt $Bytes.Length) {
        return $null
    }
    return [System.BitConverter]::ToSingle($Bytes, $Offset)
}

function Get-Triplet {
    param([byte[]]$Bytes, [int]$Offset)
    if ($Offset + 12 -gt $Bytes.Length) {
        return $null
    }
    return @(
        [System.BitConverter]::ToSingle($Bytes, $Offset),
        [System.BitConverter]::ToSingle($Bytes, $Offset + 4),
        [System.BitConverter]::ToSingle($Bytes, $Offset + 8)
    )
}

function New-DeltaSummary {
    param(
        [Parameter(Mandatory)][string]$SessionPath,
        [Parameter(Mandatory)][string]$SourceObjectAddress,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $indexPath = Join-Path $SessionPath "snapshots\index.jsonl"
    if (-not (Test-Path -LiteralPath $indexPath)) {
        throw "Snapshot index not found: $indexPath"
    }

    $entries = Get-Content -LiteralPath $indexPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_ | ConvertFrom-Json }
    $sourceEntries = @($entries | Where-Object { $_.base_address_hex -eq $SourceObjectAddress } | Sort-Object snapshot_id)
    if ($sourceEntries.Count -eq 0) {
        throw "No snapshots found with base_address_hex $SourceObjectAddress."
    }

    $offsets = @(0x48, 0x88, 0xD8, 0xE4)
    $rows = foreach ($entry in $sourceEntries) {
        $snapshotPath = Join-Path $SessionPath ($entry.path -replace "/", [System.IO.Path]::DirectorySeparatorChar)
        $bytes = [System.IO.File]::ReadAllBytes($snapshotPath)
        $values = [ordered]@{
            snapshot_id = $entry.snapshot_id
        }
        foreach ($offset in $offsets) {
            $triplet = Get-Triplet -Bytes $bytes -Offset $offset
            if ($null -ne $triplet) {
                $values["plus_0x$($offset.ToString('X'))"] = $triplet
            }
        }
        [pscustomobject]$values
    }

    $summaries = foreach ($offset in $offsets) {
        $property = "plus_0x$($offset.ToString('X'))"
        $values = @($rows | Where-Object { $_.PSObject.Properties.Name -contains $property } | ForEach-Object { $_.$property })
        if ($values.Count -eq 0) {
            continue
        }

        $first = @($values[0])
        $last = @($values[$values.Count - 1])
        $span = for ($axis = 0; $axis -lt 3; $axis++) {
            $axisValues = @($values | ForEach-Object { [double]$_[$axis] })
            ([double]($axisValues | Measure-Object -Maximum).Maximum) - ([double]($axisValues | Measure-Object -Minimum).Minimum)
        }
        $delta = for ($axis = 0; $axis -lt 3; $axis++) {
            [double]$last[$axis] - [double]$first[$axis]
        }
        $spanDistance = [Math]::Sqrt(($span | ForEach-Object { $_ * $_ } | Measure-Object -Sum).Sum)
        $deltaDistance = [Math]::Sqrt(($delta | ForEach-Object { $_ * $_ } | Measure-Object -Sum).Sum)
        [pscustomobject]@{
            offset_hex = "+0x$($offset.ToString('X'))"
            first = $first
            last = $last
            delta = $delta
            delta_distance = $deltaDistance
            span = $span
            span_distance = $spanDistance
        }
    }

    $primary = @($summaries | Where-Object { $_.offset_hex -eq "+0x48" } | Select-Object -First 1)
    $interpretation = if ($primary.Count -gt 0 -and $primary[0].span_distance -ge 0.25) {
        "stimulus_observed_primary_triplet_changed"
    } else {
        "stimulus_not_observed_or_no_primary_triplet_delta"
    }

    $summary = [pscustomobject]@{
        schema_version = "riftscan.manual_live_wrapper_delta_summary.v1"
        created_utc = (Get-Date).ToUniversalTime().ToString("o")
        session_path = $SessionPath
        source_object_address = $SourceObjectAddress
        sample_count = $sourceEntries.Count
        triplet_summaries = @($summaries)
        interpretation = $interpretation
    }

    Write-JsonFile -Value $summary -Path $OutputPath
    return $summary
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RiftScanCliProject)) {
    $RiftScanCliProject = Join-Path $repoRoot "src\RiftScan.Cli\RiftScan.Cli.csproj"
}
$RiftScanCliProjectFull = Resolve-RepoPath -Path $RiftScanCliProject
$runRootFull = Resolve-RepoPath -Path $RunRoot
$sessionRootFull = Resolve-RepoPath -Path $SessionRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDirectory = Join-Path $runRootFull "manual-live-test-$timestamp"
New-Directory -Path $runDirectory

$issues = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

try {
    if (-not (Test-Path -LiteralPath $RiftScanCliProjectFull)) {
        throw "RiftScan CLI project not found: $RiftScanCliProjectFull"
    }

    $process = Resolve-RiftProcess -RequestedProcessId $ProcessId -RequestedProcessName $ProcessName
    $processStartUtc = $process.StartTime.ToUniversalTime()
    $readerBridge = Resolve-ReaderBridgeFile -ExplicitPath $ReaderBridgeFile
    $readerBridgeItem = Get-Item -LiteralPath $readerBridge
    $readerBridgeAgeSeconds = [Math]::Max(0, [int]((Get-Date).ToUniversalTime() - $readerBridgeItem.LastWriteTimeUtc).TotalSeconds)

    if ($readerBridgeAgeSeconds -gt $MaxReaderBridgeAgeSeconds) {
        $issues.Add("ReaderBridgeExport.lua is stale by file time: age ${readerBridgeAgeSeconds}s > max ${MaxReaderBridgeAgeSeconds}s.")
    }

    $readerRunCmd = Join-Path $RiftReaderRepo "scripts\run-reader.cmd"
    if (-not (Test-Path -LiteralPath $readerRunCmd)) {
        $issues.Add("RiftReader run-reader.cmd not found: $readerRunCmd")
    }

    $anchor = $null
    $sourceObjectAddress = $null
    $traceObjectAddress = $null

    if (Test-Path -LiteralPath $readerRunCmd) {
        $anchorPath = Join-Path $runDirectory "riftreader-read-player-coord-anchor.json"
        $anchor = Invoke-JsonCommand -FilePath $readerRunCmd -Arguments @("--pid", "$($process.Id)", "--read-player-coord-anchor", "--json") -StdoutPath $anchorPath -WorkingDirectory $RiftReaderRepo

        if ($anchor.ProcessId -ne $process.Id) {
            $issues.Add("RiftReader anchor PID $($anchor.ProcessId) does not match live PID $($process.Id).")
        }
        if ($anchor.TraceMatchesProcess -ne $true) {
            $issues.Add("RiftReader anchor TraceMatchesProcess is not true.")
        }
        if ($null -eq $anchor.SourceObjectMatch -or $anchor.SourceObjectMatch.CoordMatchesWithinTolerance -ne $true) {
            $issues.Add("Source object coordinate sample does not match ReaderBridge within tolerance.")
        }
        if ([string]::IsNullOrWhiteSpace($anchor.SourceObjectAddress)) {
            $issues.Add("RiftReader anchor did not emit SourceObjectAddress.")
        } else {
            $sourceObjectAddress = $anchor.SourceObjectAddress
        }
        if (-not [string]::IsNullOrWhiteSpace($anchor.ObjectBaseAddress)) {
            $traceObjectAddress = $anchor.ObjectBaseAddress
        }
    }

    $baseAddresses = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($sourceObjectAddress)) {
        $baseAddresses.Add($sourceObjectAddress)
    }
    if (-not [string]::IsNullOrWhiteSpace($traceObjectAddress) -and $traceObjectAddress -ne $sourceObjectAddress) {
        $baseAddresses.Add($traceObjectAddress)
    }
    foreach ($address in $ExtraBaseAddress) {
        if (-not [string]::IsNullOrWhiteSpace($address) -and -not $baseAddresses.Contains($address)) {
            $baseAddresses.Add($address)
        }
    }

    if ($baseAddresses.Count -eq 0) {
        $issues.Add("No safe base addresses were resolved for capture.")
    }

    $freshnessStatus = if ($issues.Count -eq 0) { "fresh_enough_for_manual_capture" } else { "blocked" }
    $freshnessVerdictPath = Join-Path $runDirectory "freshness-verdict.json"
    $freshnessVerdict = [pscustomobject]@{
        schema_version = "riftscan.manual_live_wrapper_freshness_verdict.v1"
        created_utc = (Get-Date).ToUniversalTime().ToString("o")
        status = $freshnessStatus
        issues = @($issues)
        warnings = @($warnings)
        process = [pscustomobject]@{
            id = $process.Id
            name = $process.ProcessName
            path = $process.Path
            main_window_title = $process.MainWindowTitle
            start_time_utc = $processStartUtc.ToString("o")
        }
        readerbridge = [pscustomobject]@{
            path = $readerBridge
            last_write_utc = $readerBridgeItem.LastWriteTimeUtc.ToString("o")
            age_seconds = $readerBridgeAgeSeconds
            max_age_seconds = $MaxReaderBridgeAgeSeconds
        }
        riftreader_anchor = if ($null -eq $anchor) { $null } else { [pscustomobject]@{
            source_object_address = $sourceObjectAddress
            trace_object_address = $traceObjectAddress
            source_object_matches_readerbridge = $anchor.SourceObjectMatch.CoordMatchesWithinTolerance
            trace_matches_process = $anchor.TraceMatchesProcess
        }}
        capture_plan = [pscustomobject]@{
            stimulus = $Stimulus
            base_addresses = @($baseAddresses)
            pre_capture_wait_ms = $PreCaptureWaitMilliseconds
            samples = $Samples
            interval_ms = $IntervalMilliseconds
        }
    }
    Write-JsonFile -Value $freshnessVerdict -Path $freshnessVerdictPath

    if ($freshnessStatus -ne "fresh_enough_for_manual_capture" -and -not $AllowUnproven) {
        Write-Host "BLOCKED: freshness checks failed. No capture started." -ForegroundColor Red
        Write-Host "Verdict: $freshnessVerdictPath"
        foreach ($issue in $issues) {
            Write-Host " - $issue" -ForegroundColor Red
        }
        exit 2
    }

    if ($PreflightOnly) {
        Write-Host "Preflight complete: $freshnessStatus"
        Write-Host "Verdict: $freshnessVerdictPath"
        exit 0
    }

    $sessionId = "manual-live-$Stimulus-$timestamp"
    $sessionPath = Join-Path $sessionRootFull $sessionId
    $captureResultPath = Join-Path $runDirectory "capture-result.json"

    Write-Host "Freshness verdict: $freshnessStatus"
    Write-Host "Verdict file: $freshnessVerdictPath"
    Write-Host "Starting capture. If this is a movement/turn/camera stimulus, perform it during the pre-capture wait and keep it going until capture completes." -ForegroundColor Yellow

    $captureArgs = @(
        "capture", "passive",
        "--pid", "$($process.Id)",
        "--process", $ProcessName,
        "--out", $sessionPath,
        "--samples", "$Samples",
        "--interval-ms", "$IntervalMilliseconds",
        "--max-regions", "$MaxRegions",
        "--max-bytes-per-region", "$MaxBytesPerRegion",
        "--max-total-bytes", "$MaxTotalBytes",
        "--base-addresses", (($baseAddresses | Select-Object -Unique) -join ","),
        "--stimulus", $Stimulus,
        "--stimulus-note", "manual_live_wrapper freshness_verdict=$freshnessVerdictPath",
        "--pre-capture-wait-ms", "$PreCaptureWaitMilliseconds"
    )

    $capture = Invoke-RiftScanJson -Arguments $captureArgs -StdoutPath $captureResultPath
    if ($capture.success -ne $true) {
        throw "Capture failed. Result saved at $captureResultPath"
    }

    $verifyPath = Join-Path $runDirectory "verify-session-result.json"
    $analyzePath = Join-Path $runDirectory "analyze-session-result.json"
    $reportPath = Join-Path $runDirectory "report-session-result.json"
    $verify = Invoke-RiftScanJson -Arguments @("verify", "session", $sessionPath) -StdoutPath $verifyPath
    $analyze = Invoke-RiftScanJson -Arguments @("analyze", "session", $sessionPath, "--all") -StdoutPath $analyzePath
    $report = Invoke-RiftScanJson -Arguments @("report", "session", $sessionPath, "--top", "50") -StdoutPath $reportPath

    $deltaSummaryPath = Join-Path $runDirectory "delta-summary.json"
    $deltaSummary = $null
    if (-not [string]::IsNullOrWhiteSpace($sourceObjectAddress)) {
        $deltaSummary = New-DeltaSummary -SessionPath $sessionPath -SourceObjectAddress $sourceObjectAddress -OutputPath $deltaSummaryPath
    }

    $runSummary = [pscustomobject]@{
        schema_version = "riftscan.manual_live_wrapper_run_summary.v1"
        created_utc = (Get-Date).ToUniversalTime().ToString("o")
        status = "complete"
        run_directory = $runDirectory
        freshness_verdict = $freshnessVerdictPath
        session_path = $sessionPath
        capture_result = $captureResultPath
        verify_result = $verifyPath
        analyze_result = $analyzePath
        report_result = $reportPath
        delta_summary = $deltaSummaryPath
        stimulus_observed = if ($null -eq $deltaSummary) { $null } else { $deltaSummary.interpretation }
        report_markdown = $report.report_path
        report_json = $report.report_json_path
        verify_success = $verify.success
        analyze_success = $analyze.success
    }
    $runSummaryPath = Join-Path $runDirectory "run-summary.json"
    Write-JsonFile -Value $runSummary -Path $runSummaryPath

    Write-Host "Complete."
    Write-Host "Session: $sessionPath"
    Write-Host "Run summary: $runSummaryPath"
    Write-Host "Delta summary: $deltaSummaryPath"
    if ($null -ne $deltaSummary -and $deltaSummary.interpretation -ne "stimulus_observed_primary_triplet_changed") {
        Write-Host "WARNING: primary coordinate triplet did not move. Treat this as no stimulus proof." -ForegroundColor Yellow
    }
}
catch {
    $errorPath = Join-Path $runDirectory "error.json"
    Write-JsonFile -Value ([pscustomobject]@{
        schema_version = "riftscan.manual_live_wrapper_error.v1"
        created_utc = (Get-Date).ToUniversalTime().ToString("o")
        status = "error"
        message = $_.Exception.Message
        run_directory = $runDirectory
    }) -Path $errorPath
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error artifact: $errorPath"
    exit 1
}

# END_OF_SCRIPT_MARKER
