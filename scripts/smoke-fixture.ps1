# version: 0.1.0
# purpose: Fixture-only RiftScan CLI smoke validation without RIFT or live process access.
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$KeepOutput,
    [string]$OutputRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("riftscan-smoke-" + [Guid]::NewGuid().ToString("N"))
}
else {
    $tempRoot = [System.IO.Path]::GetFullPath($OutputRoot)
}
$cliPrefix = @("run", "--project", "src/RiftScan.Cli/RiftScan.Cli.csproj", "--configuration", $Configuration, "--no-build", "--")

function Invoke-DotNet {
    param([string[]]$Arguments)

    Write-Host "+ dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Invoke-RiftScan {
    param([string[]]$Arguments)

    Invoke-DotNet -Arguments ($script:cliPrefix + $Arguments)
}

function Invoke-RiftScanJson {
    param([string[]]$Arguments)

    $command = $script:cliPrefix + $Arguments
    Write-Host "+ dotnet $($command -join ' ')"
    $output = & dotnet @command
    if ($LASTEXITCODE -ne 0) {
        throw "riftscan command failed with exit code $LASTEXITCODE. Output: $output"
    }

    return ($output | ConvertFrom-Json)
}

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file was not created: $Path"
    }
}

function Write-JsonFile {
    param(
        [string]$Path,
        [object]$Value,
        [int]$Depth = 8
    )

    $Value | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $Path -Encoding utf8
}

function Write-JsonLine {
    param(
        [string]$Path,
        [object]$Value,
        [switch]$Append
    )

    $json = $Value | ConvertTo-Json -Depth 8 -Compress
    if ($Append) {
        Add-Content -LiteralPath $Path -Value $json -Encoding utf8
    }
    else {
        Set-Content -LiteralPath $Path -Value $json -Encoding utf8
    }
}

function Write-FloatSnapshot {
    param(
        [string]$Path,
        [float]$Value
    )

    $bytes = [byte[]]::new(16)
    [BitConverter]::GetBytes($Value).CopyTo($bytes, 4)
    [System.IO.File]::WriteAllBytes($Path, $bytes)
}

function Write-DualScalarSnapshot {
    param(
        [string]$Path,
        [float]$ActorValue,
        [float]$CameraValue
    )

    $bytes = [byte[]]::new(16)
    [BitConverter]::GetBytes($ActorValue).CopyTo($bytes, 4)
    [BitConverter]::GetBytes($CameraValue).CopyTo($bytes, 8)
    [System.IO.File]::WriteAllBytes($Path, $bytes)
}

function Write-FloatArraySnapshot {
    param(
        [string]$Path,
        [float[]]$Values
    )

    $bytes = [byte[]]::new($Values.Count * 4)
    for ($i = 0; $i -lt $Values.Count; $i++) {
        [BitConverter]::GetBytes($Values[$i]).CopyTo($bytes, $i * 4)
    }

    [System.IO.File]::WriteAllBytes($Path, $bytes)
}

function New-ChecksumEntry {
    param(
        [string]$SessionPath,
        [string]$RelativePath
    )

    $fullPath = Join-Path $SessionPath $RelativePath
    [ordered]@{
        path = $RelativePath.Replace('\', '/')
        sha256_hex = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        bytes = (Get-Item -LiteralPath $fullPath).Length
    }
}

function New-ScalarFixtureSession {
    param(
        [string]$Path,
        [string]$SessionId,
        [string]$StimulusLabel,
        [float[]]$Values
    )

    if ($Values.Count -ne 3) {
        throw "Scalar fixture sessions require exactly three values."
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $Path "snapshots") | Out-Null
    $manifest = [ordered]@{
        schema_version = "riftscan.session.v1"
        session_id = $SessionId
        project_version = "0.1.0"
        created_utc = "2026-04-29T13:50:00Z"
        machine_name = "fixture-machine"
        os_version = "fixture-os"
        process_name = "fixture_process"
        process_id = 4321
        process_start_time_utc = "2026-04-29T13:49:00Z"
        capture_mode = "fixture_scalar_labeled"
        snapshot_count = 3
        region_count = 1
        total_bytes_raw = 48
        total_bytes_stored = 48
        compression = "none"
        checksum_algorithm = "SHA256"
        status = "complete"
    }
    Write-JsonFile -Path (Join-Path $Path "manifest.json") -Value $manifest

    $regions = [ordered]@{
        regions = @(
            [ordered]@{
                region_id = "region-0001"
                base_address_hex = "0x40000000"
                size_bytes = 16
                protection = "PAGE_READWRITE"
                state = "MEM_COMMIT"
                type = "MEM_PRIVATE"
            }
        )
    }
    Write-JsonFile -Path (Join-Path $Path "regions.json") -Value $regions
    Write-JsonFile -Path (Join-Path $Path "modules.json") -Value ([ordered]@{ modules = @() })

    $indexPath = Join-Path $Path "snapshots/index.jsonl"
    if (Test-Path -LiteralPath $indexPath) {
        Remove-Item -LiteralPath $indexPath -Force
    }

    $snapshotRelativePaths = @()
    for ($i = 0; $i -lt $Values.Count; $i++) {
        $snapshotNumber = $i + 1
        $snapshotId = "snapshot-{0:D6}" -f $snapshotNumber
        $snapshotRelativePath = "snapshots/region-0001-snapshot-{0:D6}.bin" -f $snapshotNumber
        $snapshotFullPath = Join-Path $Path $snapshotRelativePath
        Write-FloatSnapshot -Path $snapshotFullPath -Value $Values[$i]
        $snapshotRelativePaths += $snapshotRelativePath
        Write-JsonLine -Path $indexPath -Append:($i -gt 0) -Value ([ordered]@{
            snapshot_id = $snapshotId
            region_id = "region-0001"
            path = $snapshotRelativePath
            base_address_hex = "0x40000000"
            size_bytes = 16
            checksum_sha256_hex = (Get-FileHash -LiteralPath $snapshotFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    }

    Write-JsonLine -Path (Join-Path $Path "stimuli.jsonl") -Value ([ordered]@{
        schema_version = "riftscan.stimulus.v1"
        session_id = $SessionId
        stimulus_id = "stimulus-000001"
        label = $StimulusLabel
        start_snapshot_id = "snapshot-000001"
        end_snapshot_id = "snapshot-000003"
        created_utc = "2026-04-29T13:50:01Z"
        source = "fixture"
        notes = "deterministic scalar fixture for smoke validation"
    })

    $checksumRelativePaths = @(
        "manifest.json",
        "regions.json",
        "modules.json",
        "snapshots/index.jsonl",
        "stimuli.jsonl"
    ) + $snapshotRelativePaths
    Write-JsonFile -Path (Join-Path $Path "checksums.json") -Value ([ordered]@{
        algorithm = "SHA256"
        entries = @($checksumRelativePaths | ForEach-Object { New-ChecksumEntry -SessionPath $Path -RelativePath $_ })
    })
}

function New-DualScalarFixtureSession {
    param(
        [string]$Path,
        [string]$SessionId,
        [string]$StimulusLabel,
        [float[]]$ActorValues,
        [float[]]$CameraValues
    )

    if ($ActorValues.Count -ne 3 -or $CameraValues.Count -ne 3) {
        throw "Dual scalar fixture sessions require exactly three actor and three camera values."
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $Path "snapshots") | Out-Null
    $manifest = [ordered]@{
        schema_version = "riftscan.session.v1"
        session_id = $SessionId
        project_version = "0.1.0"
        created_utc = "2026-04-29T14:40:00Z"
        machine_name = "fixture-machine"
        os_version = "fixture-os"
        process_name = "fixture_process"
        process_id = 6321
        process_start_time_utc = "2026-04-29T14:39:00Z"
        capture_mode = "fixture_dual_scalar_labeled"
        snapshot_count = 3
        region_count = 1
        total_bytes_raw = 48
        total_bytes_stored = 48
        compression = "none"
        checksum_algorithm = "SHA256"
        status = "complete"
    }
    Write-JsonFile -Path (Join-Path $Path "manifest.json") -Value $manifest

    $regions = [ordered]@{
        regions = @(
            [ordered]@{
                region_id = "region-0001"
                base_address_hex = "0x60000000"
                size_bytes = 16
                protection = "PAGE_READWRITE"
                state = "MEM_COMMIT"
                type = "MEM_PRIVATE"
            }
        )
    }
    Write-JsonFile -Path (Join-Path $Path "regions.json") -Value $regions
    Write-JsonFile -Path (Join-Path $Path "modules.json") -Value ([ordered]@{ modules = @() })

    $indexPath = Join-Path $Path "snapshots/index.jsonl"
    if (Test-Path -LiteralPath $indexPath) {
        Remove-Item -LiteralPath $indexPath -Force
    }

    $snapshotRelativePaths = @()
    for ($i = 0; $i -lt $ActorValues.Count; $i++) {
        $snapshotNumber = $i + 1
        $snapshotId = "snapshot-{0:D6}" -f $snapshotNumber
        $snapshotRelativePath = "snapshots/region-0001-snapshot-{0:D6}.bin" -f $snapshotNumber
        $snapshotFullPath = Join-Path $Path $snapshotRelativePath
        Write-DualScalarSnapshot -Path $snapshotFullPath -ActorValue $ActorValues[$i] -CameraValue $CameraValues[$i]
        $snapshotRelativePaths += $snapshotRelativePath
        Write-JsonLine -Path $indexPath -Append:($i -gt 0) -Value ([ordered]@{
            snapshot_id = $snapshotId
            region_id = "region-0001"
            path = $snapshotRelativePath
            base_address_hex = "0x60000000"
            size_bytes = 16
            checksum_sha256_hex = (Get-FileHash -LiteralPath $snapshotFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    }

    Write-JsonLine -Path (Join-Path $Path "stimuli.jsonl") -Value ([ordered]@{
        schema_version = "riftscan.stimulus.v1"
        session_id = $SessionId
        stimulus_id = "stimulus-000001"
        label = $StimulusLabel
        start_snapshot_id = "snapshot-000001"
        end_snapshot_id = "snapshot-000003"
        created_utc = "2026-04-29T14:40:01Z"
        source = "fixture"
        notes = "deterministic dual scalar fixture for smoke validation"
    })

    $checksumRelativePaths = @(
        "manifest.json",
        "regions.json",
        "modules.json",
        "snapshots/index.jsonl",
        "stimuli.jsonl"
    ) + $snapshotRelativePaths
    Write-JsonFile -Path (Join-Path $Path "checksums.json") -Value ([ordered]@{
        algorithm = "SHA256"
        entries = @($checksumRelativePaths | ForEach-Object { New-ChecksumEntry -SessionPath $Path -RelativePath $_ })
    })
}

function New-EntityLayoutFixtureSession {
    param(
        [string]$Path,
        [string]$SessionId
    )

    New-Item -ItemType Directory -Force -Path (Join-Path $Path "snapshots") | Out-Null
    $manifest = [ordered]@{
        schema_version = "riftscan.session.v1"
        session_id = $SessionId
        project_version = "0.1.0"
        created_utc = "2026-04-29T14:30:00Z"
        machine_name = "fixture-machine"
        os_version = "fixture-os"
        process_name = "fixture_process"
        process_id = 5321
        process_start_time_utc = "2026-04-29T14:29:00Z"
        capture_mode = "fixture_entity_layout"
        snapshot_count = 3
        region_count = 1
        total_bytes_raw = 384
        total_bytes_stored = 384
        compression = "none"
        checksum_algorithm = "SHA256"
        status = "complete"
    }
    Write-JsonFile -Path (Join-Path $Path "manifest.json") -Value $manifest

    $regions = [ordered]@{
        regions = @(
            [ordered]@{
                region_id = "region-0001"
                base_address_hex = "0x50000000"
                size_bytes = 128
                protection = "PAGE_READWRITE"
                state = "MEM_COMMIT"
                type = "MEM_PRIVATE"
            }
        )
    }
    Write-JsonFile -Path (Join-Path $Path "regions.json") -Value $regions
    Write-JsonFile -Path (Join-Path $Path "modules.json") -Value ([ordered]@{ modules = @() })

    $indexPath = Join-Path $Path "snapshots/index.jsonl"
    if (Test-Path -LiteralPath $indexPath) {
        Remove-Item -LiteralPath $indexPath -Force
    }

    $values = 1..32 | ForEach-Object { [float]$_ }
    $snapshotRelativePaths = @()
    for ($i = 0; $i -lt 3; $i++) {
        $snapshotNumber = $i + 1
        $snapshotId = "snapshot-{0:D6}" -f $snapshotNumber
        $snapshotRelativePath = "snapshots/region-0001-snapshot-{0:D6}.bin" -f $snapshotNumber
        $snapshotFullPath = Join-Path $Path $snapshotRelativePath
        Write-FloatArraySnapshot -Path $snapshotFullPath -Values $values
        $snapshotRelativePaths += $snapshotRelativePath
        Write-JsonLine -Path $indexPath -Append:($i -gt 0) -Value ([ordered]@{
            snapshot_id = $snapshotId
            region_id = "region-0001"
            path = $snapshotRelativePath
            base_address_hex = "0x50000000"
            size_bytes = 128
            checksum_sha256_hex = (Get-FileHash -LiteralPath $snapshotFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    }

    $checksumRelativePaths = @(
        "manifest.json",
        "regions.json",
        "modules.json",
        "snapshots/index.jsonl"
    ) + $snapshotRelativePaths
    Write-JsonFile -Path (Join-Path $Path "checksums.json") -Value ([ordered]@{
        algorithm = "SHA256"
        entries = @($checksumRelativePaths | ForEach-Object { New-ChecksumEntry -SessionPath $Path -RelativePath $_ })
    })
}

function Write-SmokeManifest {
    param(
        [string]$OutputRoot,
        [string]$SmokeName
    )

    $fullOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    $files = Get-ChildItem -LiteralPath $fullOutputRoot -File -Recurse |
        Where-Object { $_.Name -ne "smoke-manifest.json" } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [System.IO.Path]::GetRelativePath($fullOutputRoot, $_.FullName).Replace('\', '/')
            [ordered]@{
                path = $relativePath
                bytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }

    $manifest = [ordered]@{
        schema_version = "riftscan.smoke_manifest.v1"
        smoke_name = $SmokeName
        output_root = $fullOutputRoot
        created_utc = [DateTimeOffset]::UtcNow.ToString("O")
        file_count = @($files).Count
        files = @($files)
    }

    $manifestPath = Join-Path $fullOutputRoot "smoke-manifest.json"
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    return $manifestPath
}
Push-Location $repoRoot
try {
    $fixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/valid-session"
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureSource "manifest.json") -PathType Leaf)) {
        throw "Fixture session not found: $fixtureSource"
    }
    $changingFixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/changing-float-session"
    if (-not (Test-Path -LiteralPath (Join-Path $changingFixtureSource "manifest.json") -PathType Leaf)) {
        throw "Changing fixture session not found: $changingFixtureSource"
    }
    $vec3PassiveFixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/vec3-passive-session"
    if (-not (Test-Path -LiteralPath (Join-Path $vec3PassiveFixtureSource "manifest.json") -PathType Leaf)) {
        throw "Vec3 passive fixture session not found: $vec3PassiveFixtureSource"
    }
    $vec3MoveFixtureSource = Join-Path $repoRoot "tests/RiftScan.Tests/Fixtures/vec3-move-forward-session"
    if (-not (Test-Path -LiteralPath (Join-Path $vec3MoveFixtureSource "manifest.json") -PathType Leaf)) {
        throw "Vec3 move-forward fixture session not found: $vec3MoveFixtureSource"
    }

    if (Test-Path -LiteralPath $tempRoot) {
        $repoArtifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
        $systemTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $isSafeOutputRoot = $tempRoot.StartsWith($repoArtifactRoot, [StringComparison]::OrdinalIgnoreCase) -or
            $tempRoot.StartsWith($systemTempRoot, [StringComparison]::OrdinalIgnoreCase)
        if (-not $isSafeOutputRoot) {
            throw "Refusing to clean existing smoke output outside repo artifacts or system temp: $tempRoot"
        }

        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    $sessionA = Join-Path $tempRoot "session-a"
    $sessionB = Join-Path $tempRoot "session-b"
    $sessionChangingFloat = Join-Path $tempRoot "session-changing-float"
    $sessionChangingFloatB = Join-Path $tempRoot "session-changing-float-b"
    $sessionVec3Passive = Join-Path $tempRoot "session-vec3-passive"
    $sessionVec3Move = Join-Path $tempRoot "session-vec3-move-forward"
    $sessionEntityLayoutA = Join-Path $tempRoot "session-entity-layout-a"
    $sessionEntityLayoutB = Join-Path $tempRoot "session-entity-layout-b"
    $sessionScalarPassive = Join-Path $tempRoot "session-scalar-passive"
    $sessionScalarTurnLeft = Join-Path $tempRoot "session-scalar-turn-left"
    $sessionScalarTurnRight = Join-Path $tempRoot "session-scalar-turn-right"
    $sessionScalarCameraOnly = Join-Path $tempRoot "session-scalar-camera-only"
    $sessionCameraScalarPassive = Join-Path $tempRoot "session-camera-scalar-passive"
    $sessionCameraScalarTurnLeft = Join-Path $tempRoot "session-camera-scalar-turn-left"
    $sessionCameraScalarTurnRight = Join-Path $tempRoot "session-camera-scalar-turn-right"
    $sessionCameraScalarCameraOnly = Join-Path $tempRoot "session-camera-scalar-camera-only"
    $sessionDualScalarPassive = Join-Path $tempRoot "session-dual-scalar-passive"
    $sessionDualScalarTurnLeft = Join-Path $tempRoot "session-dual-scalar-turn-left"
    $sessionDualScalarTurnRight = Join-Path $tempRoot "session-dual-scalar-turn-right"
    $sessionDualScalarCameraOnly = Join-Path $tempRoot "session-dual-scalar-camera-only"
    $sessionDualScalarRepeatPassive = Join-Path $tempRoot "session-dual-scalar-repeat-passive"
    $sessionDualScalarRepeatTurnLeft = Join-Path $tempRoot "session-dual-scalar-repeat-turn-left"
    $sessionDualScalarRepeatTurnRight = Join-Path $tempRoot "session-dual-scalar-repeat-turn-right"
    $sessionDualScalarRepeatCameraOnly = Join-Path $tempRoot "session-dual-scalar-repeat-camera-only"
    $reportRoot = Join-Path $tempRoot "reports"
    New-Item -ItemType Directory -Path $reportRoot | Out-Null
    Copy-Item -Path $fixtureSource -Destination $sessionA -Recurse
    Copy-Item -Path $fixtureSource -Destination $sessionB -Recurse
    Copy-Item -Path $changingFixtureSource -Destination $sessionChangingFloat -Recurse
    Copy-Item -Path $changingFixtureSource -Destination $sessionChangingFloatB -Recurse
    Copy-Item -Path $vec3PassiveFixtureSource -Destination $sessionVec3Passive -Recurse
    Copy-Item -Path $vec3MoveFixtureSource -Destination $sessionVec3Move -Recurse
    New-EntityLayoutFixtureSession -Path $sessionEntityLayoutA -SessionId "fixture-entity-layout-a-session"
    New-EntityLayoutFixtureSession -Path $sessionEntityLayoutB -SessionId "fixture-entity-layout-b-session"
    New-ScalarFixtureSession -Path $sessionScalarPassive -SessionId "fixture-scalar-passive-session" -StimulusLabel "passive_idle" -Values @([float]1.5, [float]1.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionScalarTurnLeft -SessionId "fixture-scalar-turn-left-session" -StimulusLabel "turn_left" -Values @([float]1.5, [float]2.5, [float]3.5)
    New-ScalarFixtureSession -Path $sessionScalarTurnRight -SessionId "fixture-scalar-turn-right-session" -StimulusLabel "turn_right" -Values @([float]3.5, [float]2.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionScalarCameraOnly -SessionId "fixture-scalar-camera-only-session" -StimulusLabel "camera_only" -Values @([float]1.5, [float]1.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionCameraScalarPassive -SessionId "fixture-camera-scalar-passive-session" -StimulusLabel "passive_idle" -Values @([float]1.5, [float]1.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionCameraScalarTurnLeft -SessionId "fixture-camera-scalar-turn-left-session" -StimulusLabel "turn_left" -Values @([float]1.5, [float]1.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionCameraScalarTurnRight -SessionId "fixture-camera-scalar-turn-right-session" -StimulusLabel "turn_right" -Values @([float]1.5, [float]1.5, [float]1.5)
    New-ScalarFixtureSession -Path $sessionCameraScalarCameraOnly -SessionId "fixture-camera-scalar-camera-only-session" -StimulusLabel "camera_only" -Values @([float]1.5, [float]2.5, [float]3.5)
    New-DualScalarFixtureSession -Path $sessionDualScalarPassive -SessionId "fixture-dual-scalar-passive-session" -StimulusLabel "passive_idle" -ActorValues @([float]1.5, [float]1.5, [float]1.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarTurnLeft -SessionId "fixture-dual-scalar-turn-left-session" -StimulusLabel "turn_left" -ActorValues @([float]1.5, [float]2.5, [float]3.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarTurnRight -SessionId "fixture-dual-scalar-turn-right-session" -StimulusLabel "turn_right" -ActorValues @([float]3.5, [float]2.5, [float]1.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarCameraOnly -SessionId "fixture-dual-scalar-camera-only-session" -StimulusLabel "camera_only" -ActorValues @([float]1.5, [float]1.5, [float]1.5) -CameraValues @([float]2.0, [float]3.0, [float]4.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarRepeatPassive -SessionId "fixture-dual-scalar-repeat-passive-session" -StimulusLabel "passive_idle" -ActorValues @([float]1.5, [float]1.5, [float]1.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarRepeatTurnLeft -SessionId "fixture-dual-scalar-repeat-turn-left-session" -StimulusLabel "turn_left" -ActorValues @([float]1.5, [float]2.5, [float]3.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarRepeatTurnRight -SessionId "fixture-dual-scalar-repeat-turn-right-session" -StimulusLabel "turn_right" -ActorValues @([float]3.5, [float]2.5, [float]1.5) -CameraValues @([float]2.0, [float]2.0, [float]2.0)
    New-DualScalarFixtureSession -Path $sessionDualScalarRepeatCameraOnly -SessionId "fixture-dual-scalar-repeat-camera-only-session" -StimulusLabel "camera_only" -ActorValues @([float]1.5, [float]1.5, [float]1.5) -CameraValues @([float]2.0, [float]3.0, [float]4.0)

    Invoke-DotNet -Arguments @("build", "RiftScan.slnx", "--configuration", $Configuration)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionA)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionA, "--top", "10")
    Invoke-RiftScan -Arguments @("report", "session", $sessionA, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionA)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionB, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionB)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionChangingFloat)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionChangingFloat, "--top", "10")
    Invoke-RiftScan -Arguments @("report", "session", $sessionChangingFloat, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionChangingFloat)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionChangingFloatB, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionChangingFloatB)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionVec3Passive)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionVec3Passive, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionVec3Passive)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionVec3Move)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionVec3Move, "--top", "10")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionVec3Move)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionEntityLayoutA)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionEntityLayoutA, "--top", "100")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionEntityLayoutA)
    Invoke-RiftScan -Arguments @("verify", "session", $sessionEntityLayoutB)
    Invoke-RiftScan -Arguments @("analyze", "session", $sessionEntityLayoutB, "--top", "100")
    Invoke-RiftScan -Arguments @("verify", "session", $sessionEntityLayoutB)
    foreach ($scalarSession in @(
        $sessionScalarPassive,
        $sessionScalarTurnLeft,
        $sessionScalarTurnRight,
        $sessionScalarCameraOnly,
        $sessionCameraScalarPassive,
        $sessionCameraScalarTurnLeft,
        $sessionCameraScalarTurnRight,
        $sessionCameraScalarCameraOnly,
        $sessionDualScalarPassive,
        $sessionDualScalarTurnLeft,
        $sessionDualScalarTurnRight,
        $sessionDualScalarCameraOnly,
        $sessionDualScalarRepeatPassive,
        $sessionDualScalarRepeatTurnLeft,
        $sessionDualScalarRepeatTurnRight,
        $sessionDualScalarRepeatCameraOnly
    )) {
        Invoke-RiftScan -Arguments @("verify", "session", $scalarSession)
        Invoke-RiftScan -Arguments @("analyze", "session", $scalarSession, "--top", "10")
        Invoke-RiftScan -Arguments @("verify", "session", $scalarSession)
    }

    $comparisonJson = Join-Path $reportRoot "fixture-comparison.json"
    $comparisonMarkdown = Join-Path $reportRoot "fixture-comparison.md"
    $nextPlan = Join-Path $reportRoot "fixture-next-capture-plan.json"
    Invoke-RiftScan -Arguments @(
        "compare", "sessions", $sessionA, $sessionB,
        "--top", "10",
        "--out", $comparisonJson,
        "--report-md", $comparisonMarkdown,
        "--next-plan", $nextPlan
    )

    Assert-FileExists -Path (Join-Path $sessionA "report.md")
    Assert-FileExists -Path $comparisonJson
    Assert-FileExists -Path $comparisonMarkdown
    Assert-FileExists -Path $nextPlan

    $changingComparisonJson = Join-Path $reportRoot "changing-float-comparison.json"
    $changingComparisonMarkdown = Join-Path $reportRoot "changing-float-comparison.md"
    $changingNextPlan = Join-Path $reportRoot "changing-float-next-capture-plan.json"
    Invoke-RiftScan -Arguments @(
        "compare", "sessions", $sessionChangingFloat, $sessionChangingFloatB,
        "--top", "10",
        "--out", $changingComparisonJson,
        "--report-md", $changingComparisonMarkdown,
        "--next-plan", $changingNextPlan
    )

    Assert-FileExists -Path $changingComparisonJson
    Assert-FileExists -Path $changingComparisonMarkdown
    Assert-FileExists -Path $changingNextPlan
    $changingComparison = Get-Content $changingComparisonJson -Raw | ConvertFrom-Json
    if ($changingComparison.matching_value_candidate_count -lt 1) {
        throw "Expected changing-float comparison to match at least one typed value candidate."
    }

    $changingFloatMatch = @($changingComparison.value_candidate_matches) |
        Where-Object {
            $_.base_address_hex -eq "0x20000000" -and
            $_.offset_hex -eq "0x4" -and
            $_.data_type -eq "float32" -and
            $_.recommendation -eq "stable_typed_value_lane_candidate"
        } |
        Select-Object -First 1
    if ($null -eq $changingFloatMatch) {
        throw "Expected changing-float comparison to preserve the float32 lane match at 0x20000000+0x4."
    }

    $vec3BehaviorComparisonJson = Join-Path $reportRoot "vec3-behavior-comparison.json"
    $vec3BehaviorComparisonMarkdown = Join-Path $reportRoot "vec3-behavior-comparison.md"
    $vec3BehaviorNextPlan = Join-Path $reportRoot "vec3-behavior-next-capture-plan.json"
    $vec3BehaviorTruthReadiness = Join-Path $reportRoot "vec3-behavior-truth-readiness.json"
    Invoke-RiftScan -Arguments @(
        "compare", "sessions", $sessionVec3Passive, $sessionVec3Move,
        "--top", "10",
        "--out", $vec3BehaviorComparisonJson,
        "--report-md", $vec3BehaviorComparisonMarkdown,
        "--next-plan", $vec3BehaviorNextPlan,
        "--truth-readiness", $vec3BehaviorTruthReadiness
    )

    Assert-FileExists -Path $vec3BehaviorComparisonJson
    Assert-FileExists -Path $vec3BehaviorComparisonMarkdown
    Assert-FileExists -Path $vec3BehaviorNextPlan
    Assert-FileExists -Path $vec3BehaviorTruthReadiness
    $vec3BehaviorComparison = Get-Content $vec3BehaviorComparisonJson -Raw | ConvertFrom-Json
    if ($vec3BehaviorComparison.vec3_behavior_summary.behavior_contrast_count -lt 1) {
        throw "Expected vec3 behavior comparison to emit at least one behavior contrast candidate."
    }

    $vec3BehaviorMatch = @($vec3BehaviorComparison.vec3_candidate_matches) |
        Where-Object {
            $_.base_address_hex -eq "0x30000000" -and
            $_.offset_hex -eq "0x0" -and
            $_.session_a_stimulus_label -eq "passive_idle" -and
            $_.session_b_stimulus_label -eq "move_forward" -and
            $_.recommendation -eq "passive_to_move_vec3_behavior_contrast_candidate"
        } |
        Select-Object -First 1
    if ($null -eq $vec3BehaviorMatch) {
        throw "Expected vec3 behavior comparison to preserve passive-to-move contrast at 0x30000000+0x0."
    }

    $vec3BehaviorPlan = Get-Content $vec3BehaviorNextPlan -Raw | ConvertFrom-Json
    if ($vec3BehaviorPlan.recommended_mode -ne "review_existing_behavior_contrast") {
        throw "Expected vec3 behavior next plan to recommend reviewing the existing behavior contrast."
    }

    $entityLayoutComparisonJson = Join-Path $reportRoot "entity-layout-comparison.json"
    $entityLayoutComparisonMarkdown = Join-Path $reportRoot "entity-layout-comparison.md"
    $entityLayoutNextPlan = Join-Path $reportRoot "entity-layout-next-capture-plan.json"
    $entityLayoutTruthReadiness = Join-Path $reportRoot "entity-layout-truth-readiness.json"
    Invoke-RiftScan -Arguments @(
        "compare", "sessions", $sessionEntityLayoutA, $sessionEntityLayoutB,
        "--top", "100",
        "--out", $entityLayoutComparisonJson,
        "--report-md", $entityLayoutComparisonMarkdown,
        "--next-plan", $entityLayoutNextPlan,
        "--truth-readiness", $entityLayoutTruthReadiness
    )
    Assert-FileExists -Path $entityLayoutComparisonJson
    Assert-FileExists -Path $entityLayoutComparisonMarkdown
    Assert-FileExists -Path $entityLayoutNextPlan
    Assert-FileExists -Path $entityLayoutTruthReadiness
    $entityLayoutComparison = Get-Content $entityLayoutComparisonJson -Raw | ConvertFrom-Json
    $stableEntityLayout = @($entityLayoutComparison.entity_layout_matches) |
        Where-Object {
            $_.recommendation -eq "stable_entity_layout_candidate_across_sessions" -and
            $_.overlap_bytes -ge 64 -and
            $_.session_a_score -ge 75 -and
            $_.session_b_score -ge 75
        } |
        Select-Object -First 1
    if ($null -eq $stableEntityLayout) {
        throw "Expected wide fixture comparison to produce stable entity layout evidence."
    }
    $entityLayoutReadinessVerification = Invoke-RiftScanJson -Arguments @("verify", "comparison-readiness", $entityLayoutTruthReadiness)
    if (-not $entityLayoutReadinessVerification.success) {
        throw "Expected entity layout truth-readiness packet to verify successfully."
    }
    $entityLayoutReadinessJson = Get-Content $entityLayoutTruthReadiness -Raw | ConvertFrom-Json
    if ($entityLayoutReadinessJson.entity_layout.readiness -ne "strong_candidate") {
        throw "Expected entity layout truth readiness to be strong_candidate."
    }

    $scalarEvidenceSetJson = Join-Path $reportRoot "fixture-scalar-evidence-set.json"
    $scalarEvidenceSetMarkdown = Join-Path $reportRoot "fixture-scalar-evidence-set.md"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-set", $sessionChangingFloat, $sessionChangingFloatB,
        "--top", "10",
        "--out", $scalarEvidenceSetJson,
        "--report-md", $scalarEvidenceSetMarkdown
    )
    Assert-FileExists -Path $scalarEvidenceSetJson
    Assert-FileExists -Path $scalarEvidenceSetMarkdown
    $scalarEvidenceVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-evidence-set", $scalarEvidenceSetJson)
    if (-not $scalarEvidenceVerification.success -or $scalarEvidenceVerification.session_count -lt 2) {
        throw "Expected generated scalar evidence set to verify successfully."
    }

    $validatedScalarEvidenceSetJson = Join-Path $reportRoot "fixture-validated-scalar-evidence-set.json"
    $validatedScalarEvidenceSetMarkdown = Join-Path $reportRoot "fixture-validated-scalar-evidence-set.md"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-set", $sessionScalarPassive, $sessionScalarTurnLeft, $sessionScalarTurnRight, $sessionScalarCameraOnly,
        "--top", "10",
        "--out", $validatedScalarEvidenceSetJson,
        "--report-md", $validatedScalarEvidenceSetMarkdown
    )
    Assert-FileExists -Path $validatedScalarEvidenceSetJson
    Assert-FileExists -Path $validatedScalarEvidenceSetMarkdown
    $validatedScalarEvidenceVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-evidence-set", $validatedScalarEvidenceSetJson)
    if (-not $validatedScalarEvidenceVerification.success -or $validatedScalarEvidenceVerification.ranked_candidate_count -lt 1) {
        throw "Expected validated scalar evidence set to verify with at least one ranked candidate."
    }
    $validatedScalarEvidence = Get-Content $validatedScalarEvidenceSetJson -Raw | ConvertFrom-Json
    $actorYawScalar = @($validatedScalarEvidence.ranked_candidates) |
        Where-Object {
            $_.classification -eq "actor_yaw_angle_scalar_candidate" -and
            $_.truth_readiness -eq "validated_candidate" -and
            $_.passive_stable -eq $true -and
            $_.opposite_turn_polarity -eq $true -and
            $_.camera_turn_separation -eq "turn_changes_camera_only_stable"
        } |
        Select-Object -First 1
    if ($null -eq $actorYawScalar) {
        throw "Expected validated scalar fixture evidence to rank an actor_yaw_angle_scalar_candidate."
    }

    $validatedCameraScalarEvidenceSetJson = Join-Path $reportRoot "fixture-validated-camera-scalar-evidence-set.json"
    $validatedCameraScalarEvidenceSetMarkdown = Join-Path $reportRoot "fixture-validated-camera-scalar-evidence-set.md"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-set", $sessionCameraScalarPassive, $sessionCameraScalarTurnLeft, $sessionCameraScalarTurnRight, $sessionCameraScalarCameraOnly,
        "--top", "10",
        "--out", $validatedCameraScalarEvidenceSetJson,
        "--report-md", $validatedCameraScalarEvidenceSetMarkdown
    )
    Assert-FileExists -Path $validatedCameraScalarEvidenceSetJson
    Assert-FileExists -Path $validatedCameraScalarEvidenceSetMarkdown
    $validatedCameraScalarEvidenceVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-evidence-set", $validatedCameraScalarEvidenceSetJson)
    if (-not $validatedCameraScalarEvidenceVerification.success -or $validatedCameraScalarEvidenceVerification.ranked_candidate_count -lt 1) {
        throw "Expected validated camera scalar evidence set to verify with at least one ranked candidate."
    }
    $validatedCameraScalarEvidence = Get-Content $validatedCameraScalarEvidenceSetJson -Raw | ConvertFrom-Json
    $cameraScalar = @($validatedCameraScalarEvidence.ranked_candidates) |
        Where-Object {
            $_.classification -eq "camera_orientation_angle_scalar_candidate" -and
            $_.truth_readiness -eq "validated_candidate" -and
            $_.passive_stable -eq $true -and
            $_.camera_only_changed -eq $true -and
            $_.camera_turn_separation -eq "camera_only_changes_turn_stable"
        } |
        Select-Object -First 1
    if ($null -eq $cameraScalar) {
        throw "Expected validated scalar fixture evidence to rank a camera_orientation_angle_scalar_candidate."
    }

    $validatedCombinedScalarEvidenceSetJson = Join-Path $reportRoot "fixture-validated-combined-scalar-evidence-set.json"
    $validatedCombinedScalarEvidenceSetMarkdown = Join-Path $reportRoot "fixture-validated-combined-scalar-evidence-set.md"
    $validatedCombinedScalarTruthCandidates = Join-Path $reportRoot "fixture-validated-combined-scalar-truth-candidates.jsonl"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-set", $sessionDualScalarPassive, $sessionDualScalarTurnLeft, $sessionDualScalarTurnRight, $sessionDualScalarCameraOnly,
        "--top", "10",
        "--out", $validatedCombinedScalarEvidenceSetJson,
        "--report-md", $validatedCombinedScalarEvidenceSetMarkdown,
        "--truth-out", $validatedCombinedScalarTruthCandidates
    )
    Assert-FileExists -Path $validatedCombinedScalarEvidenceSetJson
    Assert-FileExists -Path $validatedCombinedScalarEvidenceSetMarkdown
    Assert-FileExists -Path $validatedCombinedScalarTruthCandidates
    $validatedCombinedScalarEvidenceVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-evidence-set", $validatedCombinedScalarEvidenceSetJson)
    if (-not $validatedCombinedScalarEvidenceVerification.success -or $validatedCombinedScalarEvidenceVerification.ranked_candidate_count -lt 2) {
        throw "Expected validated combined scalar evidence set to verify with actor and camera candidates."
    }
    $validatedCombinedScalarEvidence = Get-Content $validatedCombinedScalarEvidenceSetJson -Raw | ConvertFrom-Json
    $combinedActorYawScalar = @($validatedCombinedScalarEvidence.ranked_candidates) |
        Where-Object {
            $_.classification -eq "actor_yaw_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x4" -and
            $_.truth_readiness -eq "validated_candidate" -and
            $_.camera_turn_separation -eq "turn_changes_camera_only_stable"
        } |
        Select-Object -First 1
    $combinedCameraScalar = @($validatedCombinedScalarEvidence.ranked_candidates) |
        Where-Object {
            $_.classification -eq "camera_orientation_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x8" -and
            $_.truth_readiness -eq "validated_candidate" -and
            $_.camera_turn_separation -eq "camera_only_changes_turn_stable"
        } |
        Select-Object -First 1
    if ($null -eq $combinedActorYawScalar -or $null -eq $combinedCameraScalar) {
        throw "Expected combined scalar fixture evidence to rank validated actor and camera candidates in one packet."
    }

    $validatedCombinedScalarRepeatEvidenceSetJson = Join-Path $reportRoot "fixture-validated-combined-scalar-repeat-evidence-set.json"
    $validatedCombinedScalarRepeatEvidenceSetMarkdown = Join-Path $reportRoot "fixture-validated-combined-scalar-repeat-evidence-set.md"
    $validatedCombinedScalarRepeatTruthCandidates = Join-Path $reportRoot "fixture-validated-combined-scalar-repeat-truth-candidates.jsonl"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-set", $sessionDualScalarRepeatPassive, $sessionDualScalarRepeatTurnLeft, $sessionDualScalarRepeatTurnRight, $sessionDualScalarRepeatCameraOnly,
        "--top", "10",
        "--out", $validatedCombinedScalarRepeatEvidenceSetJson,
        "--report-md", $validatedCombinedScalarRepeatEvidenceSetMarkdown,
        "--truth-out", $validatedCombinedScalarRepeatTruthCandidates
    )
    Assert-FileExists -Path $validatedCombinedScalarRepeatEvidenceSetJson
    Assert-FileExists -Path $validatedCombinedScalarRepeatEvidenceSetMarkdown
    Assert-FileExists -Path $validatedCombinedScalarRepeatTruthCandidates
    $validatedCombinedScalarRepeatEvidenceVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-evidence-set", $validatedCombinedScalarRepeatEvidenceSetJson)
    if (-not $validatedCombinedScalarRepeatEvidenceVerification.success -or $validatedCombinedScalarRepeatEvidenceVerification.ranked_candidate_count -lt 2) {
        throw "Expected repeated combined scalar evidence set to verify with actor and camera candidates."
    }

    $combinedScalarTruthRecovery = Join-Path $reportRoot "fixture-combined-scalar-truth-recovery.json"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-truth", $validatedCombinedScalarTruthCandidates, $validatedCombinedScalarRepeatTruthCandidates,
        "--top", "10",
        "--out", $combinedScalarTruthRecovery
    )
    Assert-FileExists -Path $combinedScalarTruthRecovery
    $combinedScalarTruthRecoveryVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-truth-recovery", $combinedScalarTruthRecovery)
    if (-not $combinedScalarTruthRecoveryVerification.success -or $combinedScalarTruthRecoveryVerification.recovered_candidate_count -lt 2) {
        throw "Expected combined scalar truth recovery packet to verify with actor and camera candidates."
    }
    $combinedScalarTruthRecoveryJson = Get-Content $combinedScalarTruthRecovery -Raw | ConvertFrom-Json
    if (-not $combinedScalarTruthRecoveryJson.success -or $combinedScalarTruthRecoveryJson.recovered_candidate_count -lt 2) {
        throw "Expected combined scalar truth recovery to recover actor and camera candidates from repeated packets."
    }
    $recoveredActorYawScalar = @($combinedScalarTruthRecoveryJson.recovered_candidates) |
        Where-Object {
            $_.classification -eq "actor_yaw_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x4" -and
            $_.truth_readiness -eq "recovered_candidate" -and
            $_.supporting_file_count -ge 2
        } |
        Select-Object -First 1
    $recoveredCameraScalar = @($combinedScalarTruthRecoveryJson.recovered_candidates) |
        Where-Object {
            $_.classification -eq "camera_orientation_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x8" -and
            $_.truth_readiness -eq "recovered_candidate" -and
            $_.supporting_file_count -ge 2
        } |
        Select-Object -First 1
    if ($null -eq $recoveredActorYawScalar -or $null -eq $recoveredCameraScalar) {
        throw "Expected combined scalar truth recovery to preserve recovered actor and camera candidates."
    }

    $combinedScalarTruthCorroboration = Join-Path $reportRoot "fixture-combined-scalar-truth-corroboration.jsonl"
    Write-JsonLine -Path $combinedScalarTruthCorroboration -Value ([ordered]@{
        schema_version = "riftscan.scalar_truth_corroboration.v1"
        base_address_hex = "0x60000000"
        offset_hex = "0x4"
        data_type = "float32"
        classification = "actor_yaw_angle_scalar_candidate"
        corroboration_status = "corroborated"
        source = "fixture_addon_actor_truth"
        evidence_summary = "fixture actor yaw externally corroborated"
    })
    Write-JsonLine -Path $combinedScalarTruthCorroboration -Append -Value ([ordered]@{
        schema_version = "riftscan.scalar_truth_corroboration.v1"
        base_address_hex = "0x60000000"
        offset_hex = "0x8"
        data_type = "float32"
        classification = "camera_orientation_angle_scalar_candidate"
        corroboration_status = "corroborated"
        source = "fixture_camera_truth"
        evidence_summary = "fixture camera orientation externally corroborated"
    })
    $combinedScalarTruthCorroborationVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-corroboration", $combinedScalarTruthCorroboration)
    if (-not $combinedScalarTruthCorroborationVerification.success -or $combinedScalarTruthCorroborationVerification.entry_count -lt 2) {
        throw "Expected combined scalar truth corroboration JSONL to verify with actor and camera entries."
    }

    $combinedScalarTruthPromotion = Join-Path $reportRoot "fixture-combined-scalar-truth-promotion.json"
    Invoke-RiftScan -Arguments @(
        "compare", "scalar-promotion", $combinedScalarTruthRecovery,
        "--corroboration", $combinedScalarTruthCorroboration,
        "--top", "10",
        "--out", $combinedScalarTruthPromotion
    )
    Assert-FileExists -Path $combinedScalarTruthPromotion
    $combinedScalarTruthPromotionVerification = Invoke-RiftScanJson -Arguments @("verify", "scalar-truth-promotion", $combinedScalarTruthPromotion)
    if (-not $combinedScalarTruthPromotionVerification.success -or $combinedScalarTruthPromotionVerification.promoted_candidate_count -lt 2) {
        throw "Expected combined scalar truth promotion packet to verify with actor and camera promotions."
    }
    $combinedScalarTruthPromotionJson = Get-Content $combinedScalarTruthPromotion -Raw | ConvertFrom-Json
    $promotedActorYawScalar = @($combinedScalarTruthPromotionJson.promoted_candidates) |
        Where-Object {
            $_.classification -eq "actor_yaw_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x4" -and
            $_.truth_readiness -eq "corroborated_candidate" -and
            $_.corroboration_status -eq "corroborated"
        } |
        Select-Object -First 1
    $promotedCameraScalar = @($combinedScalarTruthPromotionJson.promoted_candidates) |
        Where-Object {
            $_.classification -eq "camera_orientation_angle_scalar_candidate" -and
            $_.offset_hex -eq "0x8" -and
            $_.truth_readiness -eq "corroborated_candidate" -and
            $_.corroboration_status -eq "corroborated"
        } |
        Select-Object -First 1
    if ($null -eq $promotedActorYawScalar -or $null -eq $promotedCameraScalar) {
        throw "Expected scalar truth promotion to mark actor and camera recovered candidates as corroborated_candidate."
    }

    $capabilityStatus = Join-Path $reportRoot "fixture-capability-status.json"
    & pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts/verify-readiness-workflow.ps1") `
        -TruthReadinessPath $vec3BehaviorTruthReadiness `
        -ScalarEvidenceSetPath $validatedScalarEvidenceSetJson `
        -CapabilityStatusPath $capabilityStatus `
        -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Readiness workflow helper failed with exit code $LASTEXITCODE."
    }

    Assert-FileExists -Path $capabilityStatus
    $capabilityVerification = Invoke-RiftScanJson -Arguments @("verify", "capability-status", $capabilityStatus)
    if (-not $capabilityVerification.success -or $capabilityVerification.capability_count -lt 17) {
        throw "Expected generated capability status to verify successfully."
    }

    $capabilityStatusJson = Get-Content $capabilityStatus -Raw | ConvertFrom-Json
    if ($capabilityStatusJson.capability_count -lt 17 -or [string]::IsNullOrWhiteSpace($capabilityStatusJson.scalar_evidence_set_path)) {
        throw "Expected capability status to include implemented scanner capabilities."
    }
    $actorYawStatus = @($capabilityStatusJson.truth_components) |
        Where-Object { $_.component -eq "actor_yaw" } |
        Select-Object -First 1
    if ($null -eq $actorYawStatus -or $actorYawStatus.evidence_readiness -ne "validated_candidate") {
        throw "Expected capability status to promote scalar actor_yaw readiness to validated_candidate."
    }

    $cameraCapabilityStatus = Join-Path $reportRoot "fixture-camera-capability-status.json"
    Invoke-RiftScan -Arguments @(
        "report", "capability",
        "--truth-readiness", $vec3BehaviorTruthReadiness,
        "--scalar-evidence-set", $validatedCameraScalarEvidenceSetJson,
        "--json-out", $cameraCapabilityStatus
    )
    Assert-FileExists -Path $cameraCapabilityStatus
    $cameraCapabilityVerification = Invoke-RiftScanJson -Arguments @("verify", "capability-status", $cameraCapabilityStatus)
    if (-not $cameraCapabilityVerification.success -or $cameraCapabilityVerification.capability_count -lt 17) {
        throw "Expected generated camera capability status to verify successfully."
    }
    $cameraCapabilityStatusJson = Get-Content $cameraCapabilityStatus -Raw | ConvertFrom-Json
    $cameraStatus = @($cameraCapabilityStatusJson.truth_components) |
        Where-Object { $_.component -eq "camera_orientation" } |
        Select-Object -First 1
    if ($null -eq $cameraStatus -or $cameraStatus.evidence_readiness -ne "validated_candidate") {
        throw "Expected capability status to promote scalar camera_orientation readiness to validated_candidate."
    }

    $combinedCapabilityStatus = Join-Path $reportRoot "fixture-combined-capability-status.json"
    Invoke-RiftScan -Arguments @(
        "report", "capability",
        "--truth-readiness", $entityLayoutTruthReadiness,
        "--truth-readiness", $vec3BehaviorTruthReadiness,
        "--scalar-evidence-set", $validatedCombinedScalarEvidenceSetJson,
        "--scalar-truth-recovery", $combinedScalarTruthRecovery,
        "--scalar-truth-promotion", $combinedScalarTruthPromotion,
        "--json-out", $combinedCapabilityStatus
    )
    Assert-FileExists -Path $combinedCapabilityStatus
    $combinedCapabilityVerification = Invoke-RiftScanJson -Arguments @("verify", "capability-status", $combinedCapabilityStatus)
    if (-not $combinedCapabilityVerification.success -or $combinedCapabilityVerification.capability_count -lt 17) {
        throw "Expected generated combined capability status to verify successfully."
    }
    $combinedCapabilityStatusJson = Get-Content $combinedCapabilityStatus -Raw | ConvertFrom-Json
    if (@($combinedCapabilityStatusJson.scalar_evidence_set_paths).Count -ne 1 -or
        @($combinedCapabilityStatusJson.scalar_evidence_set_paths)[0] -ne $validatedCombinedScalarEvidenceSetJson) {
        throw "Expected combined capability status to record the single dual-lane scalar evidence set path."
    }
    if (@($combinedCapabilityStatusJson.scalar_truth_recovery_paths).Count -ne 1 -or
        @($combinedCapabilityStatusJson.scalar_truth_recovery_paths)[0] -ne $combinedScalarTruthRecovery) {
        throw "Expected combined capability status to record the dual-lane scalar truth recovery path."
    }
    if (@($combinedCapabilityStatusJson.scalar_truth_promotion_paths).Count -ne 1 -or
        @($combinedCapabilityStatusJson.scalar_truth_promotion_paths)[0] -ne $combinedScalarTruthPromotion) {
        throw "Expected combined capability status to record the dual-lane scalar truth promotion path."
    }
    if (@($combinedCapabilityStatusJson.truth_readiness_paths).Count -lt 2) {
        throw "Expected combined capability status to record both truth-readiness paths."
    }
    $combinedEntityLayoutStatus = @($combinedCapabilityStatusJson.truth_components) |
        Where-Object { $_.component -eq "entity_layout" } |
        Select-Object -First 1
    $combinedActorYawStatus = @($combinedCapabilityStatusJson.truth_components) |
        Where-Object { $_.component -eq "actor_yaw" } |
        Select-Object -First 1
    $combinedCameraStatus = @($combinedCapabilityStatusJson.truth_components) |
        Where-Object { $_.component -eq "camera_orientation" } |
        Select-Object -First 1
    if ($null -eq $combinedEntityLayoutStatus -or $combinedEntityLayoutStatus.evidence_readiness -ne "strong_candidate") {
        throw "Expected combined capability status to keep entity_layout readiness as strong_candidate."
    }
    if ($null -eq $combinedActorYawStatus -or $combinedActorYawStatus.evidence_readiness -ne "corroborated_candidate") {
        throw "Expected combined capability status to promote scalar actor_yaw readiness to corroborated_candidate."
    }
    if ($null -eq $combinedCameraStatus -or $combinedCameraStatus.evidence_readiness -ne "corroborated_candidate") {
        throw "Expected combined capability status to promote scalar camera_orientation readiness to corroborated_candidate."
    }
    if (@($combinedCapabilityStatusJson.evidence_missing).Count -ne 0) {
        throw "Expected combined capability status to have no missing evidence after merged entity, position, actor, and camera packets."
    }
    if (@($combinedCapabilityStatusJson.next_recommended_actions) -contains "recapture_with_explicit_stimulus_labels") {
        throw "Expected combined capability status to suppress stale recapture recommendations when all components are ready."
    }

    $summaryJson = Join-Path $reportRoot "fixture-session-summary.json"
    $summaryResult = Invoke-RiftScanJson -Arguments @("session", "summary", $sessionA, "--json-out", $summaryJson)
    if (-not $summaryResult.success -or $summaryResult.artifact_count -lt 1) {
        throw "Expected successful session summary with generated artifacts."
    }

    Assert-FileExists -Path $summaryJson

    $sessionInventory = Join-Path $reportRoot "fixture-session-inventory.json"
    $inventoryResult = Invoke-RiftScanJson -Arguments @("session", "inventory", $sessionA, "--json-out", $sessionInventory)
    if (-not $inventoryResult.success -or $inventoryResult.summary.artifact_count -lt 1 -or $inventoryResult.prune_inventory.candidate_count -lt 1) {
        throw "Expected successful session inventory with generated artifacts and prune candidates."
    }
    Assert-FileExists -Path $sessionInventory

    $pruneInventory = Join-Path $reportRoot "fixture-prune-inventory.json"
    $pruneResult = Invoke-RiftScanJson -Arguments @("session", "prune", $sessionA, "--dry-run", "--json-out", $pruneInventory)
    if (-not $pruneResult.success -or -not $pruneResult.dry_run) {
        throw "Expected successful session prune dry-run result."
    }
    if ($pruneResult.candidate_count -lt 1) {
        throw "Expected session prune dry-run to find generated artifact candidates."
    }
    Assert-FileExists -Path (Join-Path $sessionA "report.md")
    Assert-FileExists -Path $pruneInventory

    $manifestPath = Write-SmokeManifest -OutputRoot $tempRoot -SmokeName "fixture"
    Assert-FileExists -Path $manifestPath
    $smokeManifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($smokeManifest.schema_version -ne "riftscan.smoke_manifest.v1" -or $smokeManifest.file_count -lt 1) {
        throw "Fixture smoke manifest was invalid."
    }

    Write-Host "Fixture smoke passed."
    if ($KeepOutput) {
        Write-Host "Output preserved: $tempRoot"
    }
}
finally {
    Pop-Location
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

# END_OF_SCRIPT
