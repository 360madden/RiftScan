@echo off
rem RiftScan script metadata
rem Version: check-riftscan-offline-workflow-v1.0.0
rem Total character count: 000000
rem Purpose: Run conservative offline RiftScan helper workflow checks without writing report artifacts.
rem Safety boundary: Offline validation only; no capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_offline_workflow_check.py" --check-only %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan offline workflow check-only exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: check-riftscan-offline-workflow.cmd
