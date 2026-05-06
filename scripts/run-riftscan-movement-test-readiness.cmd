@echo off
rem RiftScan script metadata
rem Version: run-riftscan-movement-test-readiness-v1.0.0
rem Total character count: 598
rem Purpose: Launch the conservative RiftScan movement-test readiness checker.
rem Safety boundary: This launcher only validates readiness artifacts; it does not capture memory, send input, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_movement_test_readiness.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan movement test readiness exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-movement-test-readiness.cmd
