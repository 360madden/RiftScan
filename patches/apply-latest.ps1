# Version: riftscan-patch-runner-v3.9-alpha2
# Purpose: Manifest-validation-only runner for RiftScan pending patch manifests. Logs to handoffs/current/patch-runner, validates PATCH_MANIFEST.json, and never extracts, applies, commits, or pushes.
# Total character count: 019910

[CmdletBinding()]
param(
    [string]$ManifestPath = '',
    [string]$LogRoot = '',
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RunnerVersion = 'riftscan-patch-runner-v3.9-alpha2'
$SchemaVersion = 'riftscan.patch_runner_log.v1'
$ExitNoManifest = 2
$ExitParse = 3
$ExitValidation = 4
$ExitValidationOnly = 5
$ExitUnhandled = 99

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

function Get-UtcNowString { return [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ') }
function Get-SafeTimestamp { return [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ') }

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    if ([System.IO.Path]::IsPathRooted($PathValue)) { return [System.IO.Path]::GetFullPath($PathValue) }
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

function Get-Prop {
    param(
        [Parameter(Mandatory = $true)][AllowNull()][object]$ObjectValue,
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][object]$DefaultValue = $null
    )
    if ($null -eq $ObjectValue) { return $DefaultValue }
    $property = $ObjectValue.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) { return $DefaultValue }
    $value = $property.Value
    if ($value -is [System.Array]) { return ,$value }
    return $value
}

function Test-StringArray {
    param([AllowNull()][object]$Value)
    if ($null -eq $Value -or $Value -is [string]) { return $false }
    $items = @($Value)
    if ($items.Count -eq 0) { return $false }
    foreach ($item in $items) {
        if (-not ($item -is [string]) -or [string]::IsNullOrWhiteSpace($item)) { return $false }
    }
    return $true
}

function Test-SafeRepoRelativePath {
    param([AllowNull()][object]$Value)
    if (-not ($Value -is [string]) -or [string]::IsNullOrWhiteSpace($Value)) { return $false }
    if ([System.IO.Path]::IsPathRooted($Value)) { return $false }
    $normalized = $Value.Replace('\', '/')
    if ($normalized.StartsWith('/') -or $normalized.StartsWith('../') -or $normalized.Contains('/../') -or $normalized.EndsWith('/..')) {
        return $false
    }
    return $true
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][object]$Value
    )
    $parent = Split-Path -Parent $PathValue
    if (-not [string]::IsNullOrWhiteSpace($parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    [System.IO.File]::WriteAllText($PathValue, (($Value | ConvertTo-Json -Depth 32) + [Environment]::NewLine), $script:Utf8NoBom)
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
    [System.IO.File]::AppendAllText($script:LogFile, (($payload | ConvertTo-Json -Depth 32 -Compress) + [Environment]::NewLine), $script:Utf8NoBom)
}

function Invoke-GitStatusShort {
    $previousLocation = (Get-Location).ProviderPath
    try {
        Set-Location -LiteralPath $script:RepoRoot
        $output = & git status --short 2>&1
        return [ordered]@{ command = 'git status --short'; exit_code = $LASTEXITCODE; output = @($output) -join [Environment]::NewLine }
    }
    catch {
        return [ordered]@{ command = 'git status --short'; exit_code = 1; output = $_.Exception.Message }
    }
    finally {
        Set-Location -LiteralPath $previousLocation
    }
}

function Test-PatchManifest {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $issues = New-Object System.Collections.Generic.List[string]
    $warnings = New-Object System.Collections.Generic.List[string]

    $schemaVersion = [string](Get-Prop $Manifest 'schema_version' '')
    $exampleOnly = Get-Prop $Manifest 'example_only' $null
    $patchId = [string](Get-Prop $Manifest 'patch_id' '')
    $patchTitle = [string](Get-Prop $Manifest 'patch_title' '')
    $createdUtcRaw = Get-Prop $Manifest 'created_utc' ''
    $createdUtc = if ($createdUtcRaw -is [datetime]) { $createdUtcRaw.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture) } else { [string]$createdUtcRaw }
    $runnerMinVersion = [string](Get-Prop $Manifest 'runner_min_version' '')
    $status = [string](Get-Prop $Manifest 'status' '')
    $bundle = Get-Prop $Manifest 'bundle' $null
    $patcher = Get-Prop $Manifest 'patcher' $null
    $expectedExtractedFiles = Get-Prop $Manifest 'expected_extracted_files' $null
    $validation = Get-Prop $Manifest 'validation' $null
    $guardrails = Get-Prop $Manifest 'guardrails' $null

    if ($schemaVersion -ne 'riftscan.patch_manifest.v1') { $issues.Add('schema_version must equal riftscan.patch_manifest.v1.') | Out-Null }
    if ($exampleOnly -eq $true) { $issues.Add('example_only must be false or absent for a runnable pending manifest.') | Out-Null }
    if ([string]::IsNullOrWhiteSpace($patchId) -or $patchId -eq 'example-do-not-run') { $issues.Add('patch_id must be a non-placeholder string.') | Out-Null }
    elseif ($patchId -notmatch '^[A-Za-z0-9._-]+$') { $issues.Add('patch_id may only contain letters, numbers, dot, underscore, or hyphen.') | Out-Null }
    if ([string]::IsNullOrWhiteSpace($patchTitle) -or $patchTitle -eq 'Example pending patch manifest') { $issues.Add('patch_title must be a non-placeholder string.') | Out-Null }
    if ($createdUtc -notmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$') { $issues.Add('created_utc must be a UTC timestamp like 2026-05-03T18:00:00Z.') | Out-Null }
    if ([string]::IsNullOrWhiteSpace($runnerMinVersion)) { $issues.Add('runner_min_version is required.') | Out-Null }
    elseif ($runnerMinVersion -ne $script:RunnerVersion) { $warnings.Add(('runner_min_version is {0}; current runner is {1}.' -f $runnerMinVersion, $script:RunnerVersion)) | Out-Null }
    if ($status -notin @('pending', 'ready_for_validation')) { $issues.Add('status must be pending or ready_for_validation.') | Out-Null }

    if ($null -eq $bundle) {
        $issues.Add('bundle object is required.') | Out-Null
    }
    else {
        $bundlePath = Get-Prop $bundle 'path' $null
        $bundleSha256 = [string](Get-Prop $bundle 'sha256' '')
        $bundleBase64Path = Get-Prop $bundle 'base64_path' $null
        if (-not (Test-SafeRepoRelativePath $bundlePath)) { $issues.Add('bundle.path must be a safe repo-relative path.') | Out-Null }
        elseif (-not ([string]$bundlePath).Replace('\', '/').StartsWith('patches/pending/')) { $issues.Add('bundle.path must stay under patches/pending/.') | Out-Null }
        if ([string]::IsNullOrWhiteSpace($bundleSha256) -or $bundleSha256 -eq 'REPLACE_WITH_PATCH_BUNDLE_SHA256' -or $bundleSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
            $issues.Add('bundle.sha256 must be a real 64-character hex SHA256 value.') | Out-Null
        }
        if ($null -ne $bundleBase64Path) {
            if (-not (Test-SafeRepoRelativePath $bundleBase64Path)) { $issues.Add('bundle.base64_path must be null or a safe repo-relative path.') | Out-Null }
            elseif (-not ([string]$bundleBase64Path).Replace('\', '/').StartsWith('patches/pending/')) { $issues.Add('bundle.base64_path must stay under patches/pending/.') | Out-Null }
        }
    }

    if ($null -eq $patcher) {
        $issues.Add('patcher object is required.') | Out-Null
    }
    else {
        $patcherType = [string](Get-Prop $patcher 'type' '')
        $entryPoint = Get-Prop $patcher 'entry_point' $null
        $arguments = Get-Prop $patcher 'arguments' @()
        if ($patcherType -ne 'powershell') { $issues.Add('patcher.type must be powershell for this runner stage.') | Out-Null }
        if (-not (Test-SafeRepoRelativePath $entryPoint)) { $issues.Add('patcher.entry_point must be a safe repo-relative path.') | Out-Null }
        elseif (-not ([string]$entryPoint).Replace('\', '/').StartsWith('patches/pending/.extract/')) { $issues.Add('patcher.entry_point must stay under patches/pending/.extract/.') | Out-Null }
        foreach ($argument in @($arguments)) {
            if (-not ($argument -is [string])) { $issues.Add('patcher.arguments must contain strings only.') | Out-Null; break }
        }
    }

    if (-not (Test-StringArray $expectedExtractedFiles)) {
        $issues.Add('expected_extracted_files must be a non-empty array of strings.') | Out-Null
    }
    else {
        foreach ($expectedFile in @($expectedExtractedFiles)) {
            if (-not (Test-SafeRepoRelativePath $expectedFile)) { $issues.Add(('expected_extracted_files contains unsafe path: {0}' -f $expectedFile)) | Out-Null }
        }
    }

    if ($null -eq $validation) {
        $issues.Add('validation object is required.') | Out-Null
    }
    else {
        $postPatchCommands = Get-Prop $validation 'post_patch_commands' $null
        $operatorBehaviorExpected = Get-Prop $validation 'operator_app_behavior_change_expected' $null
        if ($null -ne $postPatchCommands -and -not (Test-StringArray $postPatchCommands)) { $issues.Add('validation.post_patch_commands must be an array of command strings when present.') | Out-Null }
        if (-not ($operatorBehaviorExpected -is [bool])) { $issues.Add('validation.operator_app_behavior_change_expected must be boolean.') | Out-Null }
    }

    if (-not (Test-StringArray $guardrails)) {
        $issues.Add('guardrails must be a non-empty array of strings.') | Out-Null
    }
    else {
        $guardrailText = (@($guardrails) -join "`n").ToLowerInvariant()
        if (-not $guardrailText.Contains('auto-commit')) { $warnings.Add('guardrails should explicitly mention no auto-commit.') | Out-Null }
        if (-not $guardrailText.Contains('auto-push')) { $warnings.Add('guardrails should explicitly mention no auto-push.') | Out-Null }
    }

    return [ordered]@{
        status = $(if ($issues.Count -eq 0) { 'PASS' } else { 'FAIL' })
        issue_count = $issues.Count
        warning_count = $warnings.Count
        issues = @($issues)
        warnings = @($warnings)
        checked_fields = @('schema_version', 'example_only', 'patch_id', 'patch_title', 'created_utc', 'runner_min_version', 'status', 'bundle.path', 'bundle.sha256', 'bundle.base64_path', 'patcher.type', 'patcher.entry_point', 'patcher.arguments', 'expected_extracted_files', 'validation.post_patch_commands', 'validation.operator_app_behavior_change_expected', 'guardrails')
    }
}

function Write-RunnerSummaryAndExit {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [string[]]$Blockers = @(),
        [object]$Manifest = $null,
        [object]$ManifestValidation = $null,
        [object]$Details = $null
    )

    $summary = [ordered]@{
        schema_version = $script:SchemaVersion
        runner_version = $script:RunnerVersion
        run_id = $script:RunId
        started_utc = $script:StartedUtc
        completed_utc = Get-UtcNowString
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
        manifest = $Manifest
        manifest_validation = $ManifestValidation
        blockers = @($Blockers)
        git_status = Invoke-GitStatusShort
        details = $Details
    }

    Add-RunnerEvent -Event 'runner_completed' -Level $(if ($ExitCode -eq 0) { 'info' } else { 'error' }) -Data @{ status = $Status; exit_code = $ExitCode; blockers = @($Blockers); manifest_validation = $ManifestValidation }
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
    if ($null -ne $ManifestValidation) {
        $lines.Add(('Manifest validation: {0}' -f $ManifestValidation.status)) | Out-Null
        $lines.Add(('Manifest validation issues: {0}' -f $ManifestValidation.issue_count)) | Out-Null
        $lines.Add(('Manifest validation warnings: {0}' -f $ManifestValidation.warning_count)) | Out-Null
    }
    if ($Blockers.Count -gt 0) {
        $lines.Add('') | Out-Null
        $lines.Add('Blockers:') | Out-Null
        foreach ($blocker in $Blockers) { $lines.Add(('- {0}' -f $blocker)) | Out-Null }
    }

    [System.IO.File]::WriteAllText($script:OutputFile, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, $script:Utf8NoBom)
    if ($Json) { $summary | ConvertTo-Json -Depth 32 } else { foreach ($line in $lines) { Write-Host $line } }
    exit $ExitCode
}

try {
    $script:RunId = 'patch_runner_' + (Get-SafeTimestamp)
    $script:StartedUtc = Get-UtcNowString
    $script:RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

    if ([string]::IsNullOrWhiteSpace($ManifestPath)) { $ManifestPath = 'patches\pending\PATCH_MANIFEST.json' }
    if ([string]::IsNullOrWhiteSpace($LogRoot)) { $LogRoot = 'handoffs\current\patch-runner' }

    $script:ManifestFullPath = Resolve-RepoPath $ManifestPath
    $script:LogRootPath = Resolve-RepoPath $LogRoot
    New-Item -ItemType Directory -Path $script:LogRootPath -Force | Out-Null

    $script:LogFile = Join-Path $script:LogRootPath 'patch-runner-log.jsonl'
    $script:SummaryFile = Join-Path $script:LogRootPath 'patch-runner-summary.json'
    $script:OutputFile = Join-Path $script:LogRootPath 'patch-runner-output.txt'
    Remove-Item -LiteralPath $script:LogFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $script:SummaryFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $script:OutputFile -Force -ErrorAction SilentlyContinue

    Add-RunnerEvent -Event 'runner_started' -Data @{ manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath; log_root = ConvertTo-RepoRelativePath $script:LogRootPath; validation_only = $true; patch_application_enabled = $false; auto_commit = $false; auto_push = $false }

    if (-not (Test-Path -LiteralPath $script:ManifestFullPath -PathType Leaf)) {
        Add-RunnerEvent -Event 'manifest_missing' -Level 'error' -Data @{ manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath }
        Write-RunnerSummaryAndExit -Status 'fail_no_pending_manifest' -ExitCode $ExitNoManifest -Blockers @('No pending patch manifest exists at patches/pending/PATCH_MANIFEST.json.') -Details ([ordered]@{ current_stage = 'v3.9-alpha2 manifest validation only'; expected_manifest = 'patches/pending/PATCH_MANIFEST.json'; example_manifest = 'patches/pending/PATCH_MANIFEST.example.json'; next_action = 'Create or copy a reviewed PATCH_MANIFEST.json before running the patch runner.' })
    }

    try {
        $manifest = [System.IO.File]::ReadAllText($script:ManifestFullPath) | ConvertFrom-Json
    }
    catch {
        Add-RunnerEvent -Event 'manifest_parse_failed' -Level 'error' -Data @{ manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath; error = $_.Exception.Message }
        Write-RunnerSummaryAndExit -Status 'fail_manifest_parse_error' -ExitCode $ExitParse -Blockers @('Pending PATCH_MANIFEST.json exists but is not valid JSON.') -Details ([ordered]@{ error = $_.Exception.Message })
    }

    $manifestValidation = Test-PatchManifest -Manifest $manifest
    Add-RunnerEvent -Event 'manifest_loaded' -Data @{ manifest_path = ConvertTo-RepoRelativePath $script:ManifestFullPath; schema_version = [string](Get-Prop $manifest 'schema_version' ''); patch_id = [string](Get-Prop $manifest 'patch_id' '') }
    Add-RunnerEvent -Event 'manifest_validated' -Level $(if ($manifestValidation.status -eq 'PASS') { 'info' } else { 'error' }) -Data @{ manifest_validation = $manifestValidation }

    if ($manifestValidation.status -ne 'PASS') {
        Write-RunnerSummaryAndExit -Status 'fail_manifest_validation' -ExitCode $ExitValidation -Blockers @($manifestValidation.issues) -Manifest $manifest -ManifestValidation $manifestValidation -Details ([ordered]@{ current_stage = 'v3.9-alpha2 manifest validation only'; patch_application_enabled = $false; next_action = 'Fix PATCH_MANIFEST.json validation issues before any future patch-application runner stage.' })
    }

    Write-RunnerSummaryAndExit -Status 'blocked_validation_only' -ExitCode $ExitValidationOnly -Blockers @('v3.9-alpha2 validates PATCH_MANIFEST.json only. It does not extract bundles, compile patchers, run patchers, commit, or push.') -Manifest $manifest -ManifestValidation $manifestValidation -Details ([ordered]@{ current_stage = 'v3.9-alpha2 manifest validation only'; patch_application_enabled = $false; intentionally_not_implemented_yet = @('git pull --ff-only', 'bundle file existence check', 'bundle hash verification', 'bundle extraction', 'patcher compile/run', 'post-patch validation execution', 'automatic commit', 'automatic push') })
}
catch {
    $message = '{0}: {1}' -f $_.Exception.GetType().FullName, $_.Exception.Message
    if ($script:LogFile) {
        try { Add-RunnerEvent -Event 'unhandled_error' -Level 'error' -Data @{ error = $message } } catch { }
    }
    if ($script:SummaryFile -and $script:OutputFile -and $script:ManifestFullPath -and $script:LogRootPath) {
        try {
            Write-RunnerSummaryAndExit -Status 'fail_unhandled_error' -ExitCode $ExitUnhandled -Blockers @($message) -Details ([ordered]@{ error = $message })
        }
        catch {
            Write-Error $message
            exit $ExitUnhandled
        }
    }
    Write-Error $message
    exit $ExitUnhandled
}

# End of script
