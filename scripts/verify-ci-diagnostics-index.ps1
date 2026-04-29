# version: 0.1.0
# purpose: Validate RiftScan CI diagnostics index file lists and SHA256 hashes before artifact handoff.
[CmdletBinding()]
param(
    [string]$IndexPath,
    [string]$Root = "artifacts/ci-diagnostics"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-JsonProperty {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    if ($null -eq $Object -or -not ($Object.PSObject.Properties.Name -contains $Name)) {
        throw "$Context is missing required field: $Name"
    }

    return $Object.$Name
}

function Assert-NonEmptyString {
    param(
        [object]$Value,
        [string]$Name,
        [string]$Context
    )

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        throw "$Context has empty required field: $Name"
    }

    return [string]$Value
}

if ([string]::IsNullOrWhiteSpace($IndexPath)) {
    $IndexPath = Join-Path $Root "index.json"
}

$fullIndexPath = [System.IO.Path]::GetFullPath($IndexPath)
if (-not (Test-Path -LiteralPath $fullIndexPath -PathType Leaf)) {
    throw "CI diagnostics index not found: $fullIndexPath"
}

$actualRoot = [System.IO.Path]::GetDirectoryName($fullIndexPath)
$index = Get-Content -LiteralPath $fullIndexPath -Raw | ConvertFrom-Json
$schemaVersion = Assert-NonEmptyString `
    -Value (Assert-JsonProperty -Object $index -Name "schema_version" -Context $fullIndexPath) `
    -Name "schema_version" `
    -Context $fullIndexPath
if ($schemaVersion -ne "riftscan.ci_diagnostics_index.v1") {
    throw "Unsupported CI diagnostics index schema in ${fullIndexPath}: $schemaVersion"
}

$createdUtc = Assert-NonEmptyString `
    -Value (Assert-JsonProperty -Object $index -Name "created_utc" -Context $fullIndexPath) `
    -Name "created_utc" `
    -Context $fullIndexPath
$createdUtcParsed = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse($createdUtc, [ref]$createdUtcParsed)) {
    throw "CI diagnostics index created_utc is not a valid timestamp in ${fullIndexPath}: $createdUtc"
}

$recordedRoot = Assert-NonEmptyString `
    -Value (Assert-JsonProperty -Object $index -Name "root" -Context $fullIndexPath) `
    -Name "root" `
    -Context $fullIndexPath

$fileCountValue = Assert-JsonProperty -Object $index -Name "file_count" -Context $fullIndexPath
try {
    $expectedFileCount = [int]$fileCountValue
}
catch {
    throw "CI diagnostics index file_count is not a valid integer in ${fullIndexPath}: $fileCountValue"
}
if ($expectedFileCount -lt 0) {
    throw "CI diagnostics index file_count must be non-negative in ${fullIndexPath}: $expectedFileCount"
}

$totalBytesValue = Assert-JsonProperty -Object $index -Name "total_bytes" -Context $fullIndexPath
try {
    $expectedTotalBytes = [int64]$totalBytesValue
}
catch {
    throw "CI diagnostics index total_bytes is not a valid Int64 in ${fullIndexPath}: $totalBytesValue"
}
if ($expectedTotalBytes -lt 0) {
    throw "CI diagnostics index total_bytes must be non-negative in ${fullIndexPath}: $expectedTotalBytes"
}

$filesValue = Assert-JsonProperty -Object $index -Name "files" -Context $fullIndexPath
if ($null -eq $filesValue) {
    throw "$fullIndexPath has empty required field: files"
}

$files = @($filesValue)
if ($expectedFileCount -ne $files.Count) {
    throw "CI diagnostics index file_count mismatch in ${fullIndexPath}: expected $expectedFileCount, found $($files.Count)."
}

$actualTotalBytes = [int64]0
foreach ($file in $files) {
    $fileContext = "CI diagnostics index file entry in $fullIndexPath"
    $filePath = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $file -Name "path" -Context $fileContext) `
        -Name "path" `
        -Context $fileContext
    $bytesValue = Assert-JsonProperty -Object $file -Name "bytes" -Context $fileContext
    try {
        $expectedLength = [int64]$bytesValue
    }
    catch {
        throw "CI diagnostics index file bytes is not a valid Int64 for ${filePath}: $bytesValue"
    }
    if ($expectedLength -lt 0) {
        throw "CI diagnostics index file bytes must be non-negative for ${filePath}: $expectedLength"
    }

    $expectedHash = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $file -Name "sha256" -Context $fileContext) `
        -Name "sha256" `
        -Context $fileContext
    if ($expectedHash -notmatch '^[0-9a-f]{64}$') {
        throw "CI diagnostics index SHA256 must be 64 lowercase hex characters for ${filePath}: $expectedHash"
    }

    $artifactPath = [System.IO.Path]::GetFullPath((Join-Path $actualRoot $filePath))
    $relativeArtifactPath = [System.IO.Path]::GetRelativePath($actualRoot, $artifactPath)
    if ($relativeArtifactPath.StartsWith("..", [StringComparison]::Ordinal) -or [System.IO.Path]::IsPathRooted($relativeArtifactPath)) {
        throw "CI diagnostics index file escapes diagnostics root: $filePath"
    }
    if ($artifactPath -eq $fullIndexPath) {
        throw "CI diagnostics index must not list itself: $filePath"
    }
    if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
        throw "CI diagnostics indexed file is missing: $artifactPath"
    }

    $actualLength = (Get-Item -LiteralPath $artifactPath).Length
    if ($actualLength -ne $expectedLength) {
        throw "CI diagnostics index byte mismatch for $artifactPath. Expected $expectedLength, found $actualLength."
    }

    $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "CI diagnostics index SHA256 mismatch for $artifactPath. Expected $expectedHash, found $actualHash."
    }

    $actualTotalBytes += $actualLength
}

if ($actualTotalBytes -ne $expectedTotalBytes) {
    throw "CI diagnostics index total_bytes mismatch in ${fullIndexPath}: expected $expectedTotalBytes, found $actualTotalBytes."
}

[ordered]@{
    result_schema_version = "riftscan.ci_diagnostics_index_verification.v1"
    success = $true
    index_path = $fullIndexPath
    recorded_root = $recordedRoot
    actual_root = $actualRoot
    file_count = $files.Count
    total_bytes = $actualTotalBytes
} | ConvertTo-Json -Depth 4

# END_OF_SCRIPT
