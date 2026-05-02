# RIFT Foreground Status Probe

## Purpose

Read-only diagnostic that reports whether RIFT is currently the foreground window.

## Result

- Status: `status_json_unparsed`
- RIFT foreground: `None`
- Foreground PID: `None`
- RIFT process count: `None`

## Safety

- Does not change focus.
- Does not call SetForegroundWindow.
- Does not click.
- Does not send keyboard input.
- Does not run `/reloadui`.

## Files

- `foreground-status-summary.json`
- `foreground-status.json`
- `command-result.json`
- `step-log.jsonl`
