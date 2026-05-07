@echo off
rem RiftScan script metadata
rem Version: run-riftscan-candidate-ledger-consumer-v1.0.0
rem Total character count: 000000
rem Purpose: Refresh the safe offline-only candidate ledger consumer view.
rem Safety boundary: Offline artifact inventory only; no focus preflight, live capture, input, movement, memory scan/read, process attach, RiftReader command execution, offset validation, or /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_candidate_ledger_consumer.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan candidate ledger consumer exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-candidate-ledger-consumer.cmd
