# RiftScan Current Resume Pointer

## Current source of truth

Start here for new RiftScan work:

1. `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_NEXT_TOP_10_WORKFLOW.md`
2. `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-05_TRANSFER_OPERATOR_GUIDE.md`
3. `docs/helper-tooling-policy.md`

## Latest completed workflow milestone before this document

```text
f0f0362 Wire post-update baseline into operator app
```

## Current direction

```text
C#/.NET = product/core engine
Python = helper apps, operator workflows, gates, reports, patch intake
CMD = thin launchers for easy operation
PowerShell = rare tiny Windows bridge only
```

Do not rewrite the C# core into Python. Use Python for workflow/control-plane tooling that writes deterministic Markdown, JSON, and JSONL artifacts.

## Safety state

- RIFT updated recently; old live discovery assumptions remain suspect until rerun through current gates.
- Do not resume live capture, coordinate recovery, actor/camera discovery, movement/input, `/reloadui`, or offset validation until a fresh current-client baseline passes.
- Treat RiftReader assumptions as unvalidated after a game update unless RiftReader-specific recovery docs and live proof say otherwise.

## Milestone publishing rule

For meaningful workflow milestones:

1. inspect first
2. patch surgically
3. run relevant validation
4. `git status --short`
5. stage explicit paths only, never `git add .`
6. commit
7. push
8. verify `main...origin/main` is `0 0`

## Current next recommended action

GUI-smoke-test the Operator `Post-Update Baseline` button against the current updated RIFT client. If current client is stable in-world, produce a fresh PASS baseline before any collection/discovery work.
