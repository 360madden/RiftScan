@echo off
setlocal
REM version: 0.1.0
REM purpose: CMD entrypoint for stale-guarded manual RiftScan live testing.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0live-test-riftscan.ps1" %*
exit /b %ERRORLEVEL%
REM END_OF_SCRIPT_MARKER
