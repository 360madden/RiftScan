# Live RIFT control policy

## Operator authorization

The operator explicitly authorized Codex to control the RIFT window as needed for
live validation work. Treat this as permission to focus the verified RIFT window
and send bounded test commands when the task requires live proof.

## Boundary

Live input remains scaffolding, not scanner core behavior. Do not add window
focus, keyboard input, launcher control, or coordinate clicking to
`src/RiftScan.Core`, `src/RiftScan.Capture`, `src/RiftScan.Analysis`, or
`src/RiftScan.Cli`.

The allowed helper for bounded slash-command validation is:

```powershell
.\scripts\send-rift-slash-command.ps1 `
  -CommandText "/rbx status" `
  -TargetProcessName rift_x64 `
  -TargetTitleContains RIFT `
  -Focus
```

For addon-state proof, prefer the verified wrapper:

```powershell
.\scripts\invoke-rift-addon-command-verified.ps1 `
  -CommandText "/rbx waypoint-test 20 0" `
  -ExpectedLastCommand "waypoint-test" `
  -ExpectedWaypointHasWaypoint true `
  -ReloadUiAfterCommand
```

The wrapper sends the bounded command, rescans `ReaderBridgeExport`
SavedVariables through `riftscan rift addon-api-observations`, writes JSON/JSONL
evidence under `reports/generated`, and exits nonzero when the expected addon
state is not observed.

If the default PowerShell sender cannot see the foreground RIFT window from the
current execution context, use the AutoHotkey `SendText` backend:

```powershell
.\scripts\invoke-rift-addon-command-verified.ps1 `
  -SenderBackend autohotkey-sendtext `
  -CommandText "/rbx waypoint-clear" `
  -ExpectedLastCommand "waypoint-clear" `
  -ExpectedWaypointHasWaypoint false `
  -ReloadUiAfterCommand
```

This backend still verifies the target PID/window and records foreground
diagnostics in the result JSON before any memory-capture followup is trusted.

## Safety rules

- Verify the target process and window title before sending input.
- Refuse input unless the verified RIFT window is foreground after the focus
  gate.
- Prefer addon/API commands such as `/rbx export`, `/rbx waypoint-test`, and
  `/rbx waypoint-clear` over manual UI clicking.
- Preserve captured session artifacts and addon scan outputs.
- Mark helper-driven live validation as scaffolding evidence; final memory truth
  still requires replayable RiftScan artifacts.
