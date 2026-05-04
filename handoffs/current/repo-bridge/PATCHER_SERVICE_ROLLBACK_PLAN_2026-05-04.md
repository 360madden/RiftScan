# Patcher Service Safe Rollback Plan — 2026-05-04

## Purpose

Provide a safe rollback path if any patcher service/listener work exists or begins drifting toward always-on behavior.

This is a conservative rollback. It preserves evidence and avoids destructive Git history changes.

---

## Rollback Goal

```text
Return patcher service/listener work to a safe on-demand Operator-only baseline.
```

Keep this if still useful:

```text
Patch Runner v3.9-alpha2 manifest validation only
```

Disable or remove this for now:

```text
always-on listener
background polling service
automatic package download
automatic extraction
automatic patch application
```

---

## Rollback Levels

| Level | Name | Description | Recommendation |
|---:|---|---|---|
| 1 | Disable only | Keep files but disable service/listener entry points. | Best first move |
| 2 | Remove Operator integration | Remove UI paths that trigger automatic patcher behavior. | Use if UI is confusing |
| 3 | Revert service/listener commits | Keep alpha2 validator, revert service/listener code. | Best if service direction is rejected |
| 4 | Revert all patcher commits | Remove patcher subsystem entirely. | Not recommended unless fully rejected |

Recommended now:

```text
Level 1 or Level 3
```

---

## Step 1 — Freeze Current State

Run first:

```powershell
cd "C:\RIFT MODDING\Riftscan"
git status --short
git log --oneline -12
git branch backup-before-patcher-rollback
git tag backup-before-patcher-rollback
```

If the branch/tag already exists, use a timestamped name:

```powershell
git branch backup-before-patcher-rollback-20260504
git tag backup-before-patcher-rollback-20260504
```

---

## Step 2 — Inspect for Any Local Service or Scheduled Task

Do not delete anything by guessing names.

```powershell
Get-Service | Where-Object { $_.Name -like "*RiftScan*" -or $_.DisplayName -like "*RiftScan*" }

Get-ScheduledTask | Where-Object { $_.TaskName -like "*RiftScan*" -or $_.TaskPath -like "*RiftScan*" }
```

If found, disable first, delete later only after validation.

```powershell
Stop-Service -Name "SERVICE_NAME_HERE" -ErrorAction SilentlyContinue
Set-Service -Name "SERVICE_NAME_HERE" -StartupType Disabled

Disable-ScheduledTask -TaskName "TASK_NAME_HERE"
```

---

## Step 3 — Prefer Soft Rollback

Soft rollback means:

```text
- keep handoffs
- keep alpha2 validator
- disable always-on execution paths
- remove or hide automatic service buttons if they exist
- document the new on-demand direction
```

This avoids losing useful work.

---

## Step 4 — Use Git Revert for Pushed Commits

If service/listener commits were pushed and must be undone:

```powershell
git revert <commit_sha>
```

Do not default to:

```powershell
git reset --hard <old_sha>
git push --force
```

Force-push should be a last resort only.

---

## Step 5 — Validate After Rollback

Run:

```powershell
git status --short
git log --oneline -8
```

Then validate behavior:

```text
- Operator app still launches.
- Patch Runner alpha2 still validates manifest only, if retained.
- No service or scheduled task is enabled.
- No auto-download/extract/apply path remains active.
- No repo mutation happens without explicit operator action.
```

---

## Step 6 — Commit the Rollback Documentation or Code Change

Use explicit staging only:

```powershell
git add handoffs/current/repo-bridge/RIFTSCAN_REPO_BRIDGE_HANDOFF_2026-05-04.md
git add handoffs/current/repo-bridge/repo-bridge-handoff-2026-05-04.json
git add handoffs/current/repo-bridge/REPO_DELIVERED_PATCH_FLOW_PLAN_2026-05-04.md
git add handoffs/current/repo-bridge/repo-delivered-patch-flow-plan-2026-05-04.json
git add handoffs/current/repo-bridge/PATCHER_SERVICE_ROLLBACK_PLAN_2026-05-04.md
git add handoffs/current/repo-bridge/patcher-service-rollback-plan-2026-05-04.json
git commit -m "Add repo bridge and patcher rollback handoff"
git push
```

Never use:

```powershell
git add .
```

---

## Final Recommendation

Keep:

```text
Patch Runner alpha2 manifest validation
Operator app allowlisted commit/push workflow
machine-readable handoffs
```

Move future patch delivery to:

```text
Operator on-demand Check Online Patch Inbox
```

Do not proceed with:

```text
always-on patcher service
```

until the on-demand flow is proven safe and boring.
