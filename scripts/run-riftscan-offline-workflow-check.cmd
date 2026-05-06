@echo off
rem RiftScan script metadata
rem Version: run-riftscan-offline-workflow-check-v1.0.0
rem Total character count: 633
rem Purpose: Run conservative offline RiftScan helper workflow checks and write report artifacts.
rem Safety boundary: Offline validation only; no capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_offline_workflow_check.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan offline workflow check exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-offline-workflow-check.cmd
