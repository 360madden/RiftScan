# RiftScan Resume Handoff — Current API Coordinate Truth / Trace Anchor Blocked

**Created:** 2026-05-06 06:50 EDT / 2026-05-06T10:50Z
**Repo:** `C:\RIFT MODDING\Riftscan`
**Branch:** `main`
**Remote:** `360madden/RiftScan`
**Latest pushed HEAD before this handoff:** `4d7a06b15cd2a443060fe32142490ac5186f2401 Add movement execution gate`
**Current worktree:** dirty / uncommitted progress exists
**Primary rule:** today's RIFT update means older live proofs are historical until rerun against the current client.

---

## 🚨 TL;DR

✅ Fresh in-game API coordinate data was found after today's RIFT update.
✅ RiftReader `read-player-current` matched the fresh API coords exactly at memory address `0x1DA682DF690`.
✅ RiftScan made a short read-only passive targeted capture at that address and found a matching vec3 candidate at `0x1DA682DF690 + 0x0`.
🟡 This is **current API + read-only memory candidate truth**, not final movement-grade recovered truth.
🚫 Movement remains **BLOCKED** because the RiftReader coord-trace proof anchor is stale: trace PID `41220` != current RIFT PID `11220`.
🚫 No movement/input/reloadui was sent.
⚠️ A read-only targeted RiftScan memory capture **was started** to satisfy the user's explicit request for current coord truth via in-game API + RiftScan + RiftReader.

---

## ✅ Current Evidence Table

| Area | Status | Evidence |
|---|---:|---|
| Fresh ReaderBridge/API coords | ✅ PASS | `reports/generated/addon-api-truth-current-coords-fresh-20260506-103803.json` |
| Current player | ✅ PASS | `Atank`, `Sanctum of the Vigil`, zone `z487C9102D2EA79BE` |
| API coord | ✅ PASS | X `7511.5297851562`, Y `904.47998046875`, Z `3040.2800292969` |
| RiftReader current read | ✅ PASS | `reports/generated/riftreader-read-player-current-20260506-104154.json` |
| RiftReader current memory address | ✅ PASS | `0x1DA682DF690`, coords match API within tolerance |
| RiftScan passive capture | ✅ PASS | `sessions/current-api-coord-readonly-20260506-064252` |
| RiftScan addon match | ✅ PASS | `reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.json` |
| Coord trace proof anchor | 🚫 BLOCKED | stale trace PID `41220`, current PID `11220`, `TraceMatchesProcess=false` |
| Movement Execution Gate | 🚫 BLOCKED | `handoffs/current/movement-execution-gate/movement-execution-gate-summary.json` |
| Movement/input/reloadui | ✅ NOT DONE | safety preserved |

---

## 🧭 Current Live / Gate Truth

### Fresh API coordinate truth

From `handoffs/current/coord-api-truth/coord-api-truth-summary.json`:

```json
{
  "status": "api_and_riftscan_memory_candidate_matched_trace_anchor_blocked",
  "display_status": "PARTIAL_PASS_TRACE_BLOCKED",
  "coordinate_truth_level": "current_api_plus_readonly_memory_candidate",
  "movement_execution_allowed": false,
  "live_collection_allowed": false,
  "old_offsets_trusted": false
}
```

Current player:

```json
{
  "unit_name": "Atank",
  "unit_id": "u035400012FA2D207",
  "location_name": "Sanctum of the Vigil",
  "zone_id": "z487C9102D2EA79BE",
  "coordinate_x": 7511.5297851562,
  "coordinate_y": 904.47998046875,
  "coordinate_z": 3040.2800292969,
  "file_last_write_utc": "2026-05-06T10:34:50.511034+00:00"
}
```

### RiftReader current read

`reports/generated/riftreader-read-player-current-20260506-104154.json` showed:

| Field | Value |
|---|---|
| PID | `11220` |
| Address | `0x1DA682DF690` |
| Level | `45` |
| Health | `18208` |
| Memory coords | `7511.53, 904.48, 3040.28` |
| API coords | `7511.5297851562, 904.47998046875, 3040.2800292969` |
| Match | `CoordMatchesWithinTolerance=true`, deltas `0/0/0` |
| Proof grade | `false` because coord-trace anchor is stale |

### RiftScan read-only capture + addon match

Session: `sessions/current-api-coord-readonly-20260506-064252`

Important result:

| Field | Value |
|---|---|
| Candidate | `rift-addon-coordinate-candidate-000001` |
| Target base | `0x1DA682DF690` |
| Absolute address | `0x1DA682DF690` |
| Offset | `0x0` |
| Axis | `xyz` |
| Samples captured | `8` |
| Match count | `8` |
| Best max abs distance | `5.002220859751105E-11` |
| Warning | addon match is validation evidence, not final truth |

---

## 🚫 Current Blocker

Movement Execution Gate is still BLOCKED:

```json
{
  "display_status": "BLOCKED",
  "movement_execution_allowed": false,
  "blockers": [
    "live-test-riftscan preflight failed for move_forward.",
    "live-test-riftscan preflight issue: RiftReader anchor TraceMatchesProcess is not true.",
    "live-test-riftscan preflight issue: Source object coordinate sample does not match ReaderBridge within tolerance."
  ]
}
```

Root cause now appears narrower than before:

- ✅ ReaderBridge freshness is no longer stale.
- ✅ Current heuristic memory coordinate address is known: `0x1DA682DF690`.
- 🚫 The old coord trace artifact still points at old process/address data:
  - `TraceProcessId=41220`
  - current RIFT `PID=11220`
  - `TraceMatchesProcess=false`
  - old source object address `0x216F2F26020`
  - old object base `0x216F2F26068`

**Do not use the old `0x216...` trace addresses as current truth.**

---

## 🧪 Commands / Work Already Run This Session

### RiftScan add-on API scans

```powershell
$stamp = "20260506-103803"
$sv = "C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\Saved"
$cli = ".\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll"
```

Artifacts created:

- `reports/generated/addon-api-observation-scan-current-coords-20260506-103803.json`
- `reports/generated/addon-api-observations-current-coords-20260506-103803.jsonl`
- `reports/generated/addon-api-truth-current-coords-20260506-103803.json`
- `reports/generated/addon-api-observation-scan-current-coords-fresh-20260506-103803.json`
- `reports/generated/addon-api-observations-current-coords-fresh-20260506-103803.jsonl`
- `reports/generated/addon-api-truth-current-coords-fresh-20260506-103803.json`

### Movement Execution Gate rerun

```powershell
.\scripts\run-riftscan-movement-execution-gate.cmd --strict-exit-code
```

Result: `BLOCKED`, exit code acceptable for blocked gate; no movement/input/capture started by gate.

### RiftReader correct current-PID runs

Run from `C:\RIFT MODDING\RiftReader` with current PID `11220`:

- `reports/generated/riftreader-readerbridge-snapshot-20260506-104154.json`
- `reports/generated/riftreader-read-player-current-20260506-104154.json`
- `reports/generated/riftreader-read-player-coord-anchor-20260506-104154.json`

⚠️ Earlier failed/bad attempts exist and should not be used as proof:

- `reports/generated/riftreader-readerbridge-snapshot-20260506-104025.json`
- `reports/generated/riftreader-read-player-current-20260506-104025.json`
- `reports/generated/riftreader-read-player-coord-anchor-20260506-104025.json`
- `reports/generated/riftreader-read-player-current-20260506-104059.json` — zero/failed
- `reports/generated/riftreader-read-player-coord-anchor-20260506-104059.json` — zero/failed

### RiftScan targeted read-only passive capture

Session: `sessions/current-api-coord-readonly-20260506-064252`

```powershell
dotnet .\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll capture passive `
  --process rift_x64 `
  --samples 12 `
  --interval-ms 100 `
  --max-regions 1 `
  --max-bytes-per-region 512 `
  --max-total-bytes 4096 `
  --base-addresses 0x1DA682DF690 `
  --stimulus passive_idle `
  --out sessions/current-api-coord-readonly-20260506-064252
```

Then:

```powershell
dotnet .\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll verify session sessions/current-api-coord-readonly-20260506-064252
dotnet .\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll analyze session sessions/current-api-coord-readonly-20260506-064252 --all
dotnet .\src\RiftScan.Cli\bin\Release\net10.0\riftscan.dll report session sessions/current-api-coord-readonly-20260506-064252 --top 50
```

Then addon coordinate match:

- `reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.json`
- `reports/generated/current-api-coord-readonly-20260506-064252-addon-coordinate-matches.md`

### Tracked coord truth handoff artifacts created

- `handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md`
- `handoffs/current/coord-api-truth/coord-api-truth-summary.json`
- `handoffs/current/coord-api-truth/coord-api-truth-log.jsonl`

---

## 🧩 Current Dirty Worktree

Current status at handoff creation:

```text
## main...origin/main
 M handoffs/current/focus-control-local/focus-control-log.jsonl
 M handoffs/current/focus-control-local/focus-control-summary.json
 M handoffs/current/focus-control-local/process-command-result.json
 M handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md
 M handoffs/current/movement-execution-gate/movement-execution-gate-log.jsonl
 M handoffs/current/movement-execution-gate/movement-execution-gate-summary.json
 M handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md
 M handoffs/current/operator/operator-current-gate-summary.json
 M tools/riftscan_operator_app.py
?? handoffs/current/coord-api-truth/
```

After this handoff file is written, it will also appear as untracked/modified under `handoffs/current/`.

---

## ⚠️ Mid-Patch Code State

`tools/riftscan_operator_app.py` is partially updated from `v3.8.20` to `v3.8.21`.

Already done:

- Header version bumped to `riftscan-operator-app-v3.8.21`.
- `APP_VERSION` bumped to `riftscan-operator-app-v3.8.21`.
- Added coord truth constants:
  - `COORD_API_TRUTH_DIR`
  - `COORD_API_TRUTH_REPORT`
  - `COORD_API_TRUTH_SUMMARY`
  - `COORD_API_TRUTH_LOG`
- Added `handoffs/current/coord-api-truth` to `ALLOWLIST`.
- Added function `latest_coord_api_truth_summary()`.

Still needed:

- In `write_operator_report()`, add:
  - `coord_api_truth = latest_coord_api_truth_summary()`
  - A `## Latest Coord API Truth` section containing `json_block(coord_api_truth)`.
- Optionally include coord truth in `operator-current-gate-summary.json`, but do this carefully with tests because it affects self-test expectations.
- Run operator validation after patching.

Relevant search result:

```text
tools/riftscan_operator_app.py:611:def latest_coord_api_truth_summary() -> dict[str, Any]
```

No `## Latest Coord API Truth` section exists yet in the operator report generator as of this handoff.

---

## ✅ Safety / Boundary State

| Boundary | State |
|---|---:|
| Movement/input sent | ✅ No |
| `/reloadui` sent | ✅ No |
| Launcher/client control changed | ✅ No |
| Raw shell commands from manifests | ✅ No |
| Services/listeners/polling added | ✅ No |
| Hidden commit/push | ✅ No |
| Old offsets trusted | 🚫 No |
| Live collection allowed | 🚫 No |
| Movement execution allowed | 🚫 No |
| Read-only memory capture started | ⚠️ Yes, explicitly for current coord truth |

---

## ▶️ Exact Resume Instructions for New Chat

Start here:

```powershell
cd "C:\RIFT MODDING\Riftscan"
git status --short --branch
git log --oneline -10
```

Read these first:

1. `AGENTS.md`
2. `handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_0650_COORD_API_TRUTH_TRACE_BLOCKED.md`
3. `handoffs/current/coord-api-truth/COORD_API_TRUTH_REPORT.md`
4. `handoffs/current/coord-api-truth/coord-api-truth-summary.json`
5. `handoffs/current/movement-execution-gate/MOVEMENT_EXECUTION_GATE_REPORT.md`
6. `handoffs/current/movement-execution-gate/movement-execution-gate-summary.json`
7. `handoffs/current/operator/RIFTSCAN_OPERATOR_HANDOFF.md`
8. `tools/riftscan_operator_app.py`

Then finish the smallest coherent slice:

1. Complete Operator report inclusion of latest Coord API Truth.
2. Validate Python compile/self-test/write-report.
3. Validate coord truth JSON.
4. Review all changed files.
5. Stage explicit allowlisted paths only.
6. Commit and push.

Recommended validation commands:

```powershell
python -m py_compile tools\riftscan_operator_app.py
python tools\riftscan_operator_app.py --self-test
python tools\riftscan_operator_app.py --write-report
python -m json.tool handoffs\current\coord-api-truth\coord-api-truth-summary.json > $null
python -m json.tool handoffs\current\operator\operator-current-gate-summary.json > $null
git diff --check
```

Preferred extra validation before push:

```powershell
dotnet build .\RiftScan.slnx --configuration Release --no-restore
dotnet test .\RiftScan.slnx --configuration Release --no-build --no-restore
```

Explicit staging example — do **not** use `git add .`:

```powershell
git add -- `
  tools/riftscan_operator_app.py `
  handoffs/current/coord-api-truth `
  handoffs/current/movement-execution-gate `
  handoffs/current/focus-control-local `
  handoffs/current/operator `
  handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_0650_COORD_API_TRUTH_TRACE_BLOCKED.md
```

Suggested commit message:

```text
Record current API coordinate candidate
```

After commit:

```powershell
git push
git status --short --branch
git log --oneline -5
git rev-list --left-right --count origin/main...main
```

---

## 🧠 Next Top 10 Highest-Value Actions

| Rank | Action | Why |
|---:|---|---|
| 1 | ✅ Finish Operator coord-truth report wiring | Prevents this new truth surface from being lost in the next workflow handoff |
| 2 | 🧪 Run Operator self-test/write-report + JSON validation | Confirms partial `v3.8.21` patch did not regress guided workflow |
| 3 | 📦 Commit/push coord truth artifacts and operator patch | Keeps GitHub current at the milestone boundary |
| 4 | 🔎 In RiftReader, inspect current coord-trace rebuild/recovery docs/scripts | Need current-PID trace proof before movement |
| 5 | 🧷 Rebuild or refresh coord-trace proof anchor for current RIFT PID | Converts heuristic coord match toward proof-grade coordinate recovery |
| 6 | 🚦 Rerun Movement Execution Gate after trace refresh | The gate is the current no-go/go source for movement |
| 7 | 🧩 If coordinate mismatch remains, update the preflight linkage to consume current proof artifacts | Avoids stale `0x216...` anchor poisoning current-client checks |
| 8 | 🛡️ Add/refresh a formal Coord API Truth section/button/gate only if it stays surgical | Keeps Python helper workflow coherent without bloating core C# |
| 9 | 🧱 Run full safe validation: offline diagnostics, dotnet build/test/format | Locks down repo health before any live movement attempt |
| 10 | 🕹️ Only after Movement Execution Gate PASS: prepare the smallest bounded movement smoke test | Movement must require fresh PID/HWND/focus + current coord proof + abort controls |

---

## ✅ Exact Next Recommended Step

Finish the incomplete `tools/riftscan_operator_app.py` `v3.8.21` patch by adding `Latest Coord API Truth` to `write_operator_report()`, validate it, then commit/push the coord truth milestone before attempting any RiftReader trace rebuild or movement work.

---

## Resume Prompt for New Chat

```text
Resume RiftScan from C:\RIFT MODDING\Riftscan on branch main.
Read AGENTS.md first, then read handoffs/current/RIFTSCAN_RESUME_HANDOFF_2026-05-06_0650_COORD_API_TRUTH_TRACE_BLOCKED.md completely.
Do not rely on stale pre-update artifacts.
Current truth: fresh API coords and read-only RiftScan/RiftReader memory candidate matched at 0x1DA682DF690, but movement is still blocked because RiftReader coord-trace proof anchor is stale (TraceProcessId 41220 != current PID 11220).
First finish the partial Operator v3.8.21 patch by wiring Latest Coord API Truth into write_operator_report(), validate, commit, and push.
No git add ., no hidden auto-commit/push, no movement/input/reloadui, no offset trust until Movement Execution Gate passes.
Use emojis/tables for clarity and include top 10 next recommended actions in final answers.
```
