@echo off
rem RiftScan script metadata
rem Version: run-riftscan-movement-execution-gate-v1.0.0
rem Total character count: 000000
rem Purpose: Launch the final no-input RiftScan movement execution gate.
rem Safety boundary: This launcher may run focus/live-wrapper preflight, but it does not capture memory, send movement/input, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_movement_execution_gate.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan movement execution gate exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-movement-execution-gate.cmd
