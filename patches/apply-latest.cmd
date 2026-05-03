@echo off
REM Version: riftscan-patch-runner-cmd-v1.1
REM Purpose: Repo-root launcher for the RiftScan patch runner. It resolves PowerShell, runs patches\apply-latest.ps1, and preserves the runner exit code.
REM Total character count: 735

setlocal
cd /d "%~dp0.." || exit /b 1

where pwsh >nul 2>nul
if not errorlevel 1 goto run_pwsh

where powershell >nul 2>nul
if not errorlevel 1 goto run_windows_powershell

echo ERROR: Neither pwsh nor powershell was found on PATH.
exit /b 1

:run_pwsh
pwsh -NoProfile -ExecutionPolicy Bypass -File ".\patches\apply-latest.ps1"
exit /b %ERRORLEVEL%

:run_windows_powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\patches\apply-latest.ps1"
exit /b %ERRORLEVEL%

REM End of script
