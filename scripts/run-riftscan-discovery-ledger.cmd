@echo off
rem RiftScan script metadata
rem Version: run-riftscan-discovery-ledger-v1.0.0
rem Total character count: 000000
rem Purpose: Refresh the offline RiftScan discovery ledger from stored artifacts.
rem Safety boundary: Offline artifact inventory only; no focus preflight, live capture, input, movement, memory scan/read, process attach, RiftReader command execution, or /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_discovery_ledger.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan discovery ledger exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-discovery-ledger.cmd
