# version: 0.1.0
# purpose: Validate RiftScan smoke-manifest.json file lists and SHA256 hashes before artifact upload or handoff.
[CmdletBinding()]
param(
    [string[]]$ManifestPath,
    [string]$Root
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-Manifest {
    param([string]$Path)

    $fullManifestPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullManifestPath -PathType Leaf)) {
        throw "Smoke manifest not found: $fullManifestPath"
    }

    $manifest = Get-Content -LiteralPath $fullManifestPath -Raw | ConvertFrom-Json
    if ($manifest.schema_version -ne "riftscan.smoke_manifest.v1") {
        throw "Unsupported smoke manifest schema in ${fullManifestPath}: $($manifest.schema_version)"
    }

    $outputRoot = [System.IO.Path]::GetFullPath($manifest.output_root)
    if (-not (Test-Path -LiteralPath $outputRoot -PathType Container)) {
        throw "Smoke manifest output_root does not exist: $outputRoot"
    }

    $files = @($manifest.files)
    if ($manifest.file_count -ne $files.Count) {
        throw "Smoke manifest file_count mismatch in ${fullManifestPath}: expected $($manifest.file_count), found $($files.Count)."
    }

    foreach ($file in $files) {
        if ([string]::IsNullOrWhiteSpace($file.path)) {
            throw "Smoke manifest contains a file entry with an empty path: $fullManifestPath"
        }

        $artifactPath = [System.IO.Path]::GetFullPath((Join-Path $outputRoot $file.path))
        $relativeArtifactPath = [System.IO.Path]::GetRelativePath($outputRoot, $artifactPath)
        if ($relativeArtifactPath.StartsWith("..", [StringComparison]::Ordinal) -or [System.IO.Path]::IsPathRooted($relativeArtifactPath)) {
            throw "Smoke manifest file escapes output_root: $($file.path)"
        }
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
            throw "Smoke manifest file is missing: $artifactPath"
        }

        $actualLength = (Get-Item -LiteralPath $artifactPath).Length
        if ($actualLength -ne [int64]$file.bytes) {
            throw "Smoke manifest byte mismatch for $artifactPath. Expected $($file.bytes), found $actualLength."
        }

        $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $file.sha256) {
            throw "Smoke manifest SHA256 mismatch for $artifactPath. Expected $($file.sha256), found $actualHash."
        }
    }

    [ordered]@{
        manifest_path = $fullManifestPath
        smoke_name = $manifest.smoke_name
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
