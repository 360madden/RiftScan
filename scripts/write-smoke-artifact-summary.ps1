# version: 0.1.0
# purpose: Write a GitHub Actions job summary for RiftScan smoke artifact manifests.
[CmdletBinding()]
param(
    [string]$Root = "artifacts",
    [string]$ArtifactName = "riftscan-smoke-artifacts",
    [int]$RetentionDays = 14,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$fullRoot = [System.IO.Path]::GetFullPath($Root)
if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
    throw "Smoke artifact root does not exist: $fullRoot"
}

$manifests = Get-ChildItem -LiteralPath $fullRoot -Filter "smoke-manifest.json" -File -Recurse |
    Sort-Object FullName |
    ForEach-Object {
        $manifest = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
        [ordered]@{
            path = [System.IO.Path]::GetRelativePath($fullRoot, $_.FullName).Replace('\', '/')
            smoke_name = $manifest.smoke_name
            file_count = [int]$manifest.file_count
            bytes = (@($manifest.files) | ForEach-Object { [int64]$_.bytes } | Measure-Object -Sum).Sum
        }
    }

if (@($manifests).Count -eq 0) {
    throw "No smoke manifests were found under: $fullRoot"
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("## RiftScan smoke artifacts")
$lines.Add("")
$lines.Add("- Artifact: ``$ArtifactName``")
$lines.Add("- Retention: $RetentionDays days")
$lines.Add("- Manifest count: $(@($manifests).Count)")
$lines.Add("")
$lines.Add("| Smoke | Files | Bytes | Manifest |")
$lines.Add("| --- | ---: | ---: | --- |")
foreach ($manifest in $manifests) {
    $lines.Add("| $($manifest.smoke_name) | $($manifest.file_count) | $($manifest.bytes) | ``$($manifest.path)`` |")
}
$lines.Add("")

$summary = ($lines -join [Environment]::NewLine)
if ([string]::IsNullOrWhiteSpace($SummaryPath)) {
    Write-Output $summary
}
else {
    $fullSummaryPath = [System.IO.Path]::GetFullPath($SummaryPath)
    New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($fullSummaryPath)) -Force | Out-Null
    Add-Content -LiteralPath $fullSummaryPath -Value $summary
}

# END_OF_SCRIPT
