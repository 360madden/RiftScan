# version: 0.1.0
# purpose: Write a machine-readable index for CI diagnostics files preserved in GitHub Actions artifacts.
[CmdletBinding()]
param(
    [string]$Root = "artifacts/ci-diagnostics",
    [string]$IndexPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$fullRoot = [System.IO.Path]::GetFullPath($Root)
if ([string]::IsNullOrWhiteSpace($IndexPath)) {
    $IndexPath = Join-Path $fullRoot "index.json"
}
$fullIndexPath = [System.IO.Path]::GetFullPath($IndexPath)

New-Item -ItemType Directory -Path $fullRoot -Force | Out-Null
New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($fullIndexPath)) -Force | Out-Null

$files = Get-ChildItem -LiteralPath $fullRoot -File -Recurse |
    Where-Object { [System.IO.Path]::GetFullPath($_.FullName) -ne $fullIndexPath } |
    Sort-Object FullName |
    ForEach-Object {
        $relativePath = [System.IO.Path]::GetRelativePath($fullRoot, $_.FullName).Replace('\', '/')
        [ordered]@{
            path = $relativePath
            bytes = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

$fileList = @($files)
$totalBytes = [int64]0
if ($fileList.Count -gt 0) {
    $totalBytes = [int64](($fileList | ForEach-Object { [int64]$_.bytes } | Measure-Object -Sum).Sum)
}

$index = [ordered]@{
    schema_version = "riftscan.ci_diagnostics_index.v1"
    created_utc = [DateTimeOffset]::UtcNow.ToString("O")
    root = $fullRoot
    file_count = $fileList.Count
    total_bytes = $totalBytes
    files = $fileList
}

$index | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $fullIndexPath -Encoding utf8

[ordered]@{
    index_path = $fullIndexPath
    file_count = $fileList.Count
    total_bytes = $index.total_bytes
} | ConvertTo-Json -Depth 3

# END_OF_SCRIPT
