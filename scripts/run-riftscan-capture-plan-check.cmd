@echo off
rem RiftScan script metadata
rem Version: run-riftscan-capture-plan-check-v1.0.0
rem Total character count: 575
rem Purpose: Launch the conservative RiftScan capture-plan checker.
rem Safety boundary: This launcher only validates existing metadata artifacts; it does not capture memory, send input, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_capture_plan_check.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan capture plan check exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-capture-plan-check.cmd
