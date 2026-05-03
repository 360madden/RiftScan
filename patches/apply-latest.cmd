@echo off
REM Version: riftscan-patch-runner-cmd-v1.0
REM Purpose: Repo-root launcher for the RiftScan patch runner. It resolves PowerShell, then runs patches\apply-latest.ps1.
REM Total character count: 654

setlocal
cd /d "%~dp0.." || exit /b 1

where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File ".\patches\apply-latest.ps1"
    exit /b %ERRORLEVEL%
)

where powershell >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\patches\apply-latest.ps1"
    exit /b %ERRORLEVEL%
)

echo ERROR: Neither pwsh nor powershell was found on PATH.
exit /b 1

REM End of script
