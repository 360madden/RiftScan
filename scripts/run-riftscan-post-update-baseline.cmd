@echo off
rem RiftScan script metadata
rem Version: run-riftscan-post-update-baseline-v1.0.0
rem Total character count: 598
rem Purpose: Launch the conservative RiftScan post-update baseline Python report writer.
rem Safety boundary: This launcher only runs the baseline report writer; it does not capture memory, send input, or run /reloadui.

setlocal
cd /d "%~dp0.."

python ".\tools\riftscan_post_update_baseline.py" %*
set EXITCODE=%ERRORLEVEL%

echo.
echo RiftScan post-update baseline exited with code %EXITCODE%.
exit /b %EXITCODE%

rem End of script: run-riftscan-post-update-baseline.cmd
