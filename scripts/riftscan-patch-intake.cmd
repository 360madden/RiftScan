@echo off
setlocal
REM Version: riftscan-patch-intake-launcher-v1.0.0
REM Purpose: Thin launcher for tools\riftscan_patch_intake_app.py. It does not apply logic itself; it only passes arguments to Python and returns Python's exit code.
REM Total character count: 510

set "SCRIPT_DIR=%~dp0"
set "PYTHON_APP=%SCRIPT_DIR%..\tools\riftscan_patch_intake_app.py"

if not exist "%PYTHON_APP%" (
  echo FAIL_MISSING_HELPER: %PYTHON_APP%
  exit /b 1
)

python "%PYTHON_APP%" %*
exit /b %ERRORLEVEL%

REM End of script
