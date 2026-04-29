# Agent execution workflow

RiftScan agent work should use coherent milestone blocks, not micro-step reporting.

## Default execution cadence

1. Define the smallest coherent product milestone.
2. Batch related code, tests, smoke proof, and docs in one implementation pass.
3. Use targeted tests only when they shorten debugging or isolate compile/API uncertainty.
4. Run full validation at the milestone boundary.
5. Report only completed milestone evidence or a real blocker.

## What counts as a coherent milestone

A milestone should produce a usable scanner capability or a stronger truth-discovery workflow surface. Examples:

- CLI flag + service ingestion + verifier + tests + smoke proof + docs.
- Analyzer scoring improvement + rejection diagnostics + deterministic tests + report output.
- Capture workflow improvement + artifact schema support + replay verification + operator docs.

## What to avoid

- Stopping after a single helper function when the feature surface is unfinished.
- Running the whole test suite after every tiny edit.
- Reporting partial progress as if it were product capability.
- Expanding into unrelated rewrites while avoiding the current blocker.

## Validation boundary

Before claiming a milestone complete, run the strongest practical validation:

```powershell
dotnet test .\RiftScan.slnx --configuration Release --no-restore
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-fixture.ps1 -OutputRoot .\reports\generated\<milestone-stamp>
dotnet format .\RiftScan.slnx --verify-no-changes --no-restore
git diff --check
```

If any command cannot be run, state the exact reason and the unverified risk.

## Live RIFT window control authorization

As of 2026-04-29, the operator explicitly authorized autonomous Codex control of the local RIFT game window when needed for RiftScan live testing. This authorization applies to focusing the exact `rift_x64` / `RIFT` window and sending bounded stimulus input such as turn-left, turn-right, camera-only, or short movement actions required to label capture sessions.

Boundaries:

- Keep RiftScan core read-only; do not add game input, launcher control, credential handling, or window automation to scanner core.
- Verify the target process/window before sending input.
- Prefer short, reversible input bursts and record the stimulus label in session artifacts.
- Do not control Glyph launcher/auth flows or handle credentials/tokens.
- Stop live input if the target window/process cannot be verified or if input appears to affect the wrong window.

## Rule of thumb

Smallest coherent milestone, not smallest possible code change.
