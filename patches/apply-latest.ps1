# Version: riftscan-patch-runner-v3.9-alpha1
# Purpose: Skeleton runner for RiftScan pending patch manifests. It logs to handoffs/current/patch-runner, fails cleanly when patches/pending/PATCH_MANIFEST.json is absent, and never commits or pushes.
# Total character count: 012614

[CmdletBinding()]
param(
    [string]$ManifestPath = '',
    [string]$LogRoot = '',
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RunnerVersion = 'riftscan-patch-runner-v3.9-alpha1'
$SchemaVersion = 'riftscan.patch_runner_log.v1'
$NoManifestExitCode = 2
$ManifestParseExitCode = 3
$SkeletonOnlyExitCode = 5
$UnhandledExitCode = 99

$script:RunnerVersion = $RunnerVersion
$script:SchemaVersion = $SchemaVersion
$script:RunId = ''
$script:StartedUtc = ''
$script:RepoRoot = ''
$script:ManifestFullPath = ''
$script:LogRootPath = ''
$script:LogFile = ''
$script:SummaryFile = ''
$script:OutputFile = ''
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false

function Get-UtcNowString {
    return [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}

function Get-SafeTimestamp {
    return [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $script:RepoRoot $PathValue))
}

function ConvertTo-RepoRelativePath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $fullPath = [System.IO.Path]::GetFullPath($PathValue)
    $rootPath = [System.IO.Path]::GetFullPath($script:RepoRoot)

    if (-not $rootPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootPath = $rootPath + [System.IO.Path]::DirectorySeparatorChar
    }

    if ($fullPath.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($rootPath.Length).Replace('\', '/')
    }

    return $fullPath
}

function Get-ObjectPropertyValue {
    param(
        [Parameter(Mandatory = $true)][AllowNull()][object]$ObjectValue,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [AllowNull()][object]$DefaultValue = $null
    )

    if ($null -eq $ObjectValue) {
        return $DefaultValue
    }

    $property = $ObjectValue.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $DefaultValue
    }

    if ($null -eq $property.Value) {
        return $DefaultValue
    }

    return $property.Value
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][object]$Value
    )

    $parent = Split-Path -Parent $PathValue
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $jsonText = $Value | ConvertTo-Json -Depth 32
    [System.IO.File]::WriteAllText($PathValue, $jsonText + [Environment]::NewLine, $script:Utf8NoBom)
}

function Add-RunnerEvent {
    param(
        [Parameter(Mandatory = $true)][string]$Event,
        [string]$Level = 'info',
        [hashtable]$Data = @{}
    )

    $payload = [ordered]@{
        schema_version = $script:SchemaVersion
        runner_version = $script:RunnerVersion
        run_id = $script:RunId
        utc = Get-UtcNowString
        level = $Level
        event = $Event
        data = $Data
    }

    $line = $payload | ConvertTo-Json -Depth 32 -Compress
    [System.IO.File]::AppendAllText($script:LogFile, $line + [Environment]::NewLine, $script:Utf8NoBom)
}

function Invoke-GitStatusShort {
    $previousLocation = (Get-Location).ProviderPath
    try {
        Set-Location -LiteralPath $script:RepoRoot
        $output = & git status --short 2>&1
        $exitCode = $LASTEXITCODE
        return [ordered]@{
            command = 'git status --short'
            exit_code = $exitCode
            output = @($output) -join [Environment]::NewLine
        }
    }
    catch {
        return [ordered]@{
            command = 'git status --short'
            exit_code = 1
            output = $_.Exception.Message
        }
    }
    finally {
        Set-Location -LiteralPath $previousLocation
    }
}

function Write-RunnerSummaryAndExit {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [string[]]$Blockers = @(),
        [object]$Manifest = $null,
        [object]$Details = $null
    )

    $completedUtc = Get-UtcNowString
    $gitStatus = Invoke-GitStatusShort

    $summary = [ordered]@{
        schema_version = $script:SchemaVersion
        runner_version = $script:RunnerVersion
        run_id = $script:RunId
        started_utc = $script:StartedUtc
        completed_utc = $completedUtc
        status = $Status
        exit_code = $ExitCode
        repo_root = $script:RepoRoot
        manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath
        log_root = ConvertTo-RepoRelativePath $script:LogRootPath
        log_file = ConvertTo-RepoRelativePath $script:LogFile
        summary_file = ConvertTo-RepoRelativePath $script:SummaryFile
        output_file = ConvertTo-RepoRelativePath $script:OutputFile
        patch_applied = $false
        auto_commit = $false
        auto_push = $false
        operator_app_behavior_changed = $false
        blockers = @($Blockers)
        manifest = $Manifest
        git_status = $gitStatus
        details = $Details
    }

    Add-RunnerEvent -Event 'runner_completed' -Level $(if ($ExitCode -eq 0) { 'info' } else { 'error' }) -Data @{
        status = $Status
        exit_code = $ExitCode
        blockers = @($Blockers)
    }

    Write-JsonFile -PathValue $script:SummaryFile -Value $summary

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add(('RIFTSCAN PATCH RUNNER: {0}' -f $Status.ToUpperInvariant())) | Out-Null
    $lines.Add(('Runner: {0}' -f $script:RunnerVersion)) | Out-Null
    $lines.Add(('Run ID: {0}' -f $script:RunId)) | Out-Null
    $lines.Add(('Manifest: {0}' -f (ConvertTo-RepoRelativePath $script:ManifestFullPath))) | Out-Null
    $lines.Add(('Log: {0}' -f (ConvertTo-RepoRelativePath $script:LogFile))) | Out-Null
    $lines.Add(('Summary: {0}' -f (ConvertTo-RepoRelativePath $script:SummaryFile))) | Out-Null
    $lines.Add('Patch applied: false') | Out-Null
    $lines.Add('Auto-commit: false') | Out-Null
    $lines.Add('Auto-push: false') | Out-Null

    if ($Blockers.Count -gt 0) {
        $lines.Add('') | Out-Null
        $lines.Add('Blockers:') | Out-Null
        foreach ($blocker in $Blockers) {
            $lines.Add(('- {0}' -f $blocker)) | Out-Null
        }
    }

    [System.IO.File]::WriteAllText($script:OutputFile, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, $script:Utf8NoBom)

    if ($Json) {
        $summary | ConvertTo-Json -Depth 32
    }
    else {
        foreach ($line in $lines) {
            Write-Host $line
        }
    }

    exit $ExitCode
}

try {
    $script:RunnerVersion = $RunnerVersion
    $script:SchemaVersion = $SchemaVersion
    $script:RunId = 'patch_runner_' + (Get-SafeTimestamp)
    $script:StartedUtc = Get-UtcNowString
    $script:RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        $ManifestPath = 'patches\pending\PATCH_MANIFEST.json'
    }

    if ([string]::IsNullOrWhiteSpace($LogRoot)) {
        $LogRoot = 'handoffs\current\patch-runner'
    }

    $script:ManifestFullPath = Resolve-RepoPath $ManifestPath
    $script:LogRootPath = Resolve-RepoPath $LogRoot
    New-Item -ItemType Directory -Path $script:LogRootPath -Force | Out-Null

    $script:LogFile = Join-Path $script:LogRootPath 'patch-runner-log.jsonl'
    $script:SummaryFile = Join-Path $script:LogRootPath 'patch-runner-summary.json'
    $script:OutputFile = Join-Path $script:LogRootPath 'patch-runner-output.txt'

    Remove-Item -LiteralPath $script:LogFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $script:SummaryFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $script:OutputFile -Force -ErrorAction SilentlyContinue

    Add-RunnerEvent -Event 'runner_started' -Data @{
        manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath
        log_root = ConvertTo-RepoRelativePath $script:LogRootPath
        skeleton_only = $true
        auto_commit = $false
        auto_push = $false
    }

    if (-not (Test-Path -LiteralPath $script:ManifestFullPath -PathType Leaf)) {
        Add-RunnerEvent -Event 'manifest_missing' -Level 'error' -Data @{
            manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath
        }

        Write-RunnerSummaryAndExit `
            -Status 'fail_no_pending_manifest' `
            -ExitCode $NoManifestExitCode `
            -Blockers @('No pending patch manifest exists at patches/pending/PATCH_MANIFEST.json.') `
            -Details ([ordered]@{
                expected_manifest = 'patches/pending/PATCH_MANIFEST.json'
                example_manifest = 'patches/pending/PATCH_MANIFEST.example.json'
                next_action = 'Create or copy a reviewed PATCH_MANIFEST.json before running the patch runner.'
            })
    }

    $manifestText = [System.IO.File]::ReadAllText($script:ManifestFullPath)
    try {
        $manifest = $manifestText | ConvertFrom-Json
    }
    catch {
        Add-RunnerEvent -Event 'manifest_parse_failed' -Level 'error' -Data @{
            manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath
            error = $_.Exception.Message
        }

        Write-RunnerSummaryAndExit `
            -Status 'fail_manifest_parse_error' `
            -ExitCode $ManifestParseExitCode `
            -Blockers @('Pending PATCH_MANIFEST.json exists but is not valid JSON.') `
            -Details ([ordered]@{ error = $_.Exception.Message })
    }

    $manifestSchemaVersion = [string](Get-ObjectPropertyValue -ObjectValue $manifest -PropertyName 'schema_version' -DefaultValue '')
    $manifestPatchId = [string](Get-ObjectPropertyValue -ObjectValue $manifest -PropertyName 'patch_id' -DefaultValue '')

    Add-RunnerEvent -Event 'manifest_loaded' -Data @{
        manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath
        schema_version = $manifestSchemaVersion
        patch_id = $manifestPatchId
    }

    Write-RunnerSummaryAndExit `
        -Status 'blocked_skeleton_only' `
        -ExitCode $SkeletonOnlyExitCode `
        -Blockers @('v3.9-alpha1 is a skeleton runner only. It logs and validates manifest presence, but it does not extract, compile, run patchers, commit, or push.') `
        -Manifest $manifest `
        -Details ([ordered]@{
            implemented_now = @(
                'repo-root PowerShell launcher path',
                'current patch-runner log directory creation',
                'clean failure when pending PATCH_MANIFEST.json is absent',
                'pending manifest JSON parse check when present',
                'strict-mode-safe manifest summary property reads',
                'structured JSONL and summary JSON output',
                'explicit no auto-commit/no auto-push reporting'
            )
            intentionally_not_implemented_yet = @(
                'git pull --ff-only',
                'bundle hash verification',
                'bundle extraction',
                'patcher compile/run',
                'post-patch validation',
                'automatic commit',
                'automatic push'
            )
        })
}
catch {
    $message = '{0}: {1}' -f $_.Exception.GetType().FullName, $_.Exception.Message

    if ($script:LogFile) {
        try {
            Add-RunnerEvent -Event 'unhandled_error' -Level 'error' -Data @{ error = $message }
        }
        catch {
            # Keep the original error visible even if logging fails.
        }
    }

    if ($script:SummaryFile -and $script:OutputFile -and $script:ManifestFullPath -and $script:LogRootPath) {
        try {
            Write-RunnerSummaryAndExit `
                -Status 'fail_unhandled_error' `
                -ExitCode $UnhandledExitCode `
                -Blockers @($message) `
                -Details ([ordered]@{ error = $message })
        }
        catch {
            Write-Error $message
            exit $UnhandledExitCode
        }
    }

    Write-Error $message
    exit $UnhandledExitCode
}

# End of script
