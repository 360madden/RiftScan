@echo off
REM Version: riftscan-operator-app-launcher-v2
REM Purpose: Launch the RiftScan Windows operator helper app from the repository root.
REM Total character count: 444

setlocal
cd /d "%~dp0\.."

if not exist ".\tools\riftscan_operator_app.py" (
    echo ERROR: Missing .\tools\riftscan_operator_app.py
    exit /b 1
)

python ".\tools\riftscan_operator_app.py" %*
if errorlevel 1 (
    echo RiftScan operator app exited with an error.
    exit /b 1
)

exit /b 0

REM End of script
