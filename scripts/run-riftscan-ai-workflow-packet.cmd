@echo off
rem RiftScan script metadata
rem Version: run-riftscan-ai-workflow-packet-v1.0.0
rem Total character count: 000000
rem Purpose: Refresh the offline AI workflow packet from current RiftScan artifacts.
rem Safety boundary: Offline artifact inventory only; no focus preflight, live capture, input, movement, memory scan/read, process attach, RiftReader command execution, offset validation, or /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_ai_workflow_packet.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan AI workflow packet exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-ai-workflow-packet.cmd
