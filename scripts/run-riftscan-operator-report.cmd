@echo off
rem RiftScan script metadata
rem Version: run-riftscan-operator-report-v1.0.0
rem Total character count: 643
rem Purpose: Refresh the RiftScan Operator handoff/report and gate summary without launching the GUI.
rem Safety boundary: This launcher only writes Operator report artifacts; it does not capture memory, send input, scan memory, validate offsets, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_operator_app.py" --write-report %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan operator report refresh exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-operator-report.cmd
