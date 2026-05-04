# PATCH_RUNNER_ALPHA2_VALIDATOR_HOTFIX

Patch ID: `patch-runner-alpha2-validator-hotfix-v3`
Created UTC: `2026-05-04T03:56:08Z`

## Result

Applied local validator hotfix to `patches/apply-latest.ps1`.

## Fixes

- Preserves JSON arrays returned by `Get-Prop`, preventing single-item arrays from being flattened.
- Normalizes `created_utc` when `ConvertFrom-Json` returns a `[datetime]` object, preventing valid UTC timestamps from failing the ISO regex check.

## Parse Check

```text
exit=0
POWERSHELL_PARSE_OK

```

## Guardrails

- No bundle extraction.
- No patcher execution.
- No post-patch command execution by the runner.
- No `git add`.
- No commit.
- No push.

## Next Expected Validation

Run the valid-shape alpha2 manifest test again through RiftScan Operator.

Expected:

```text
PATCH RUNNER: BLOCKED/PASS_ALPHA2
Runner status: blocked_validation_only
Exit code: 5
```

<!-- End of hotfix handoff. -->
