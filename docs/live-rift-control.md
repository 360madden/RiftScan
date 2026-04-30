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

## Safety rules

- Verify the target process and window title before sending input.
- Refuse input unless the verified RIFT window is foreground after the focus
  gate.
- Prefer addon/API commands such as `/rbx export`, `/rbx waypoint-test`, and
  `/rbx waypoint-clear` over manual UI clicking.
- Preserve captured session artifacts and addon scan outputs.
- Mark helper-driven live validation as scaffolding evidence; final memory truth
  still requires replayable RiftScan artifacts.
