@echo off
rem RiftScan script metadata
rem Version: run-riftscan-capture-readiness-v1.0.0
rem Total character count: 584
rem Purpose: Launch the conservative RiftScan capture-readiness Python report writer.
rem Safety boundary: This launcher only runs the readiness report writer; it does not capture memory, send input, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_capture_readiness.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan capture readiness exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-capture-readiness.cmd
