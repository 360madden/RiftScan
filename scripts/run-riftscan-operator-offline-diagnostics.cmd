@echo off
rem RiftScan script metadata
rem Version: run-riftscan-operator-offline-diagnostics-v1.0.0
rem Total character count: 1899
rem Purpose: Run no-GUI RiftScan Operator diagnostics and refresh the Operator report.
rem Safety boundary: Safe helper diagnostics only; no live capture, input, movement, memory scan/read, offset validation, RiftReader validation, or /reloadui.

setlocal
cd /d "%~dp0.."

set STEP=Offline Workflow Check
echo.
echo === %STEP% ===
call ".\scripts\run-riftscan-offline-workflow-check.cmd"
if errorlevel 1 goto failed

set STEP=Operator self-test
echo.
echo === %STEP% ===
python ".\tools\riftscan_operator_app.py" --self-test
if errorlevel 1 goto failed

set STEP=Post-Update Baseline self-test
echo.
echo === %STEP% ===
python ".\tools\riftscan_post_update_baseline.py" --self-test
if errorlevel 1 goto failed

set STEP=Capture Readiness self-test
echo.
echo === %STEP% ===
python ".\tools\riftscan_capture_readiness.py" --self-test
if errorlevel 1 goto failed

set STEP=Capture Plan Check
echo.
echo === %STEP% ===
call ".\scripts\run-riftscan-capture-plan-check.cmd" --strict-exit-code
if errorlevel 1 goto failed

set STEP=Movement Test Readiness
echo.
echo === %STEP% ===
call ".\scripts\run-riftscan-movement-test-readiness.cmd" --strict-exit-code
if errorlevel 1 goto failed

set STEP=Operator report refresh
echo.
echo === %STEP% ===
python ".\tools\riftscan_operator_app.py" --write-report
if errorlevel 1 goto failed

echo.
echo RIFTSCAN OPERATOR OFFLINE DIAGNOSTICS: PASS
echo Operator report: handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
echo Gate summary: handoffs/current/operator/operator-current-gate-summary.json
exit /b 0

:failed
set EXITCODE=%ERRORLEVEL%
echo.
echo RIFTSCAN OPERATOR OFFLINE DIAGNOSTICS: FAIL during %STEP%.
echo Exit code: %EXITCODE%
exit /b %EXITCODE%

rem End of script: run-riftscan-operator-offline-diagnostics.cmd
