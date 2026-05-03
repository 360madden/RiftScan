@echo off
REM Version: run-rift-focus-control-v2
REM Purpose: Run the tracked RiftScan RIFT focus-control probe from the repository root.
REM Character count: 318

setlocal
cd /d "%~dp0\.."

python ".\tools\rift_focus_control.py" %*
if errorlevel 1 (
    echo Focus control failed. Handoff: %CD%\handoffs\current\focus-control-local
    exit /b 1
)

exit /b 0

REM End of script
