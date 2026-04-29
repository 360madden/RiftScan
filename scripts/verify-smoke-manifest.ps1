# version: 0.1.0
# purpose: Validate RiftScan smoke-manifest.json file lists and SHA256 hashes before artifact upload or handoff.
[CmdletBinding()]
param(
    [string[]]$ManifestPath,
    [string]$Root
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

function Assert-Manifest {
    param([string]$Path)

    $fullManifestPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullManifestPath -PathType Leaf)) {
        throw "Smoke manifest not found: $fullManifestPath"
    }

    $manifest = Get-Content -LiteralPath $fullManifestPath -Raw | ConvertFrom-Json
    $schemaVersion = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $manifest -Name "schema_version" -Context $fullManifestPath) `
        -Name "schema_version" `
        -Context $fullManifestPath
    if ($schemaVersion -ne "riftscan.smoke_manifest.v1") {
        throw "Unsupported smoke manifest schema in ${fullManifestPath}: $schemaVersion"
    }

    $smokeName = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $manifest -Name "smoke_name" -Context $fullManifestPath) `
        -Name "smoke_name" `
        -Context $fullManifestPath
    $outputRootValue = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $manifest -Name "output_root" -Context $fullManifestPath) `
        -Name "output_root" `
        -Context $fullManifestPath
    $createdUtc = Assert-NonEmptyString `
        -Value (Assert-JsonProperty -Object $manifest -Name "created_utc" -Context $fullManifestPath) `
        -Name "created_utc" `
        -Context $fullManifestPath
    $createdUtcParsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($createdUtc, [ref]$createdUtcParsed)) {
        throw "Smoke manifest created_utc is not a valid timestamp in ${fullManifestPath}: $createdUtc"
    }

    $fileCountValue = Assert-JsonProperty -Object $manifest -Name "file_count" -Context $fullManifestPath
    try {
        $expectedFileCount = [int]$fileCountValue
    }
    catch {
        throw "Smoke manifest file_count is not a valid integer in ${fullManifestPath}: $fileCountValue"
    }
    if ($expectedFileCount -lt 0) {
        throw "Smoke manifest file_count must be non-negative in ${fullManifestPath}: $expectedFileCount"
    }

    $filesValue = Assert-JsonProperty -Object $manifest -Name "files" -Context $fullManifestPath
    if ($null -eq $filesValue) {
        throw "$fullManifestPath has empty required field: files"
    }

    $outputRoot = [System.IO.Path]::GetFullPath($outputRootValue)
    if (-not (Test-Path -LiteralPath $outputRoot -PathType Container)) {
        throw "Smoke manifest output_root does not exist: $outputRoot"
    }

    $files = @($filesValue)
    if ($expectedFileCount -ne $files.Count) {
        throw "Smoke manifest file_count mismatch in ${fullManifestPath}: expected $expectedFileCount, found $($files.Count)."
    }

    foreach ($file in $files) {
        $fileContext = "Smoke manifest file entry in $fullManifestPath"
        $filePath = Assert-NonEmptyString `
            -Value (Assert-JsonProperty -Object $file -Name "path" -Context $fileContext) `
            -Name "path" `
            -Context $fileContext
        $bytesValue = Assert-JsonProperty -Object $file -Name "bytes" -Context $fileContext
        try {
            $expectedLength = [int64]$bytesValue
        }
        catch {
            throw "Smoke manifest file bytes is not a valid Int64 for ${filePath}: $bytesValue"
        }
        if ($expectedLength -lt 0) {
            throw "Smoke manifest file bytes must be non-negative for ${filePath}: $expectedLength"
        }

        $expectedHash = Assert-NonEmptyString `
            -Value (Assert-JsonProperty -Object $file -Name "sha256" -Context $fileContext) `
            -Name "sha256" `
            -Context $fileContext
        if ($expectedHash -notmatch '^[0-9a-f]{64}$') {
            throw "Smoke manifest SHA256 must be 64 lowercase hex characters for ${filePath}: $expectedHash"
        }

        $artifactPath = [System.IO.Path]::GetFullPath((Join-Path $outputRoot $filePath))
        $relativeArtifactPath = [System.IO.Path]::GetRelativePath($outputRoot, $artifactPath)
        if ($relativeArtifactPath.StartsWith("..", [StringComparison]::Ordinal) -or [System.IO.Path]::IsPathRooted($relativeArtifactPath)) {
            throw "Smoke manifest file escapes output_root: $filePath"
        }
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
            throw "Smoke manifest file is missing: $artifactPath"
        }

        $actualLength = (Get-Item -LiteralPath $artifactPath).Length
        if ($actualLength -ne $expectedLength) {
            throw "Smoke manifest byte mismatch for $artifactPath. Expected $expectedLength, found $actualLength."
        }

        $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $expectedHash) {
            throw "Smoke manifest SHA256 mismatch for $artifactPath. Expected $expectedHash, found $actualHash."
        }
    }

    [ordered]@{
        manifest_path = $fullManifestPath
        smoke_name = $smokeName
        file_count = $files.Count
        output_root = $outputRoot
    }
}

if ([string]::IsNullOrWhiteSpace($Root) -and ($null -eq $ManifestPath -or $ManifestPath.Count -eq 0)) {
    throw "Provide -ManifestPath <path>[,<path>...] or -Root <artifact-root>."
}

$expandedManifestPaths = @(
    foreach ($path in @($ManifestPath)) {
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $path.Split(',', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($Root)) {
        $fullRoot = [System.IO.Path]::GetFullPath($Root)
        if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
            throw "Smoke manifest root does not exist: $fullRoot"
        }

        Get-ChildItem -LiteralPath $fullRoot -Filter "smoke-manifest.json" -File -Recurse |
            Sort-Object FullName |
            ForEach-Object { $_.FullName }
    }
)

if ($expandedManifestPaths.Count -eq 0) {
    throw "No smoke manifests were found."
}

$results = foreach ($path in $expandedManifestPaths) {
    Assert-Manifest -Path $path
}

$results | ConvertTo-Json -Depth 4

# END_OF_SCRIPT
