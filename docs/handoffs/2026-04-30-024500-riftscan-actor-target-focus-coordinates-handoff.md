# RiftScan actor/target/focus coordinate discovery handoff

Created: 2026-04-30 02:45 America/New_York
Repo: `C:\RIFT MODDING\Riftscan`
Branch at handoff: `main`
HEAD at handoff: `0f6adca Match actor coordinates from addon truth summary` (`main`, `origin/main`, `origin/HEAD`)
Reason: conversation was stopped early during compaction; preserve the live evidence, uncommitted patch state, and next safest actions.

## TL;DR

- A very recent RIFT client/update warning means old coordinate-match artifacts are historical only until refreshed against new live capture data.
- The latest fresh addon/API truth proves current player coordinates only.
- Fresh target/focus/focus-target truth is not currently available: the addon export observed no selected target/focus after a live refresh and a Tab-target attempt.
- The uncommitted patch expands addon/API truth parsing to classify and promote `target`, `focus`, and `focus_target` coordinate sources.
- Distinct target/focus memory matching should not continue until a fresh selected target/focus is exported by the addon.

## Current repo/worktree state

`git status --short --branch` at handoff start:

```text
## main...origin/main
 M docs/addon-api-observation-workflow.md
 M src/RiftScan.Rift/Addons/RiftAddonApiObservationService.cs
 M src/RiftScan.Rift/Addons/RiftAddonApiTruthSummaryResult.cs
 M src/RiftScan.Rift/Addons/RiftAddonApiTruthSummaryService.cs
 M tests/RiftScan.Tests/CommandResultContractTests.cs
 M tests/RiftScan.Tests/RiftAddonApiObservationServiceTests.cs
 M tests/RiftScan.Tests/RiftAddonApiTruthSummaryServiceTests.cs
```

Diff summary before this handoff file:

```text
7 files changed, 158 insertions(+), 12 deletions(-)
```

This handoff file itself is an additional uncommitted doc artifact.

## Uncommitted code/doc patch already in place

### `src/RiftScan.Rift/Addons/RiftAddonApiObservationService.cs`

- `ContextKeyRegex` now recognizes:
  - `focusTarget`
  - `focus_target`
  - `playerTarget`
  - `player_target`
  - `focus`
- API-source classification treats `focus` / `focus_target` as `Inspect.Unit.Detail` sources.
- World-coordinate kind classification now maps:
  - `focus` -> `focus`
  - `focusTarget` / `focus_target` -> `focus_target`
  - `playerTarget` / `player_target` -> `target`

### `src/RiftScan.Rift/Addons/RiftAddonApiTruthSummaryResult.cs`

- Adds result JSON fields:
  - `latest_focus`
  - `latest_focus_target`

### `src/RiftScan.Rift/Addons/RiftAddonApiTruthSummaryService.cs`

- Promotes latest `focus` and `focus_target` coordinate observations into truth records.
- Sets `LatestFocus` and `LatestFocusTarget` in the summary result.
- Adds missing-truth warnings:
  - `no_focus_coordinate_truth_observed`
  - `no_focus_target_coordinate_truth_observed`
- Updates diagnostics text so missing target/focus/waypoint records are explicitly treated as absent observation, not probable truth.

### Tests/docs updated

- `tests/RiftScan.Tests/CommandResultContractTests.cs`
  - pins `latest_focus` and `latest_focus_target` in the command contract.
- `tests/RiftScan.Tests/RiftAddonApiObservationServiceTests.cs`
  - adds fixture coverage for target/focus/focus-target context parsing.
- `tests/RiftScan.Tests/RiftAddonApiTruthSummaryServiceTests.cs`
  - extends truth summary fixture assertions for focus/focus-target promotion.
- `docs/addon-api-observation-workflow.md`
  - documents that actor/player matching can use `--truth-kind target`, `--truth-kind focus`, and `--truth-kind focus_target`.
  - warns that self-target intentionally matches the player family and is not distinct target proof.

## Validation already run before interruption

Focused tests passed:

```powershell
dotnet test .\tests\RiftScan.Tests\RiftScan.Tests.csproj --configuration Release --filter "FullyQualifiedName~RiftAddonApiObservationServiceTests|FullyQualifiedName~RiftAddonApiTruthSummaryServiceTests|FullyQualifiedName~CommandResultContractTests|FullyQualifiedName~RiftSessionAddonCoordinateMatchServiceTests"
```

Result recorded in the prior work stream: `66 passed`.

Full solution validation still needs to be rerun before commit:

```powershell
dotnet build .\RiftScan.slnx --configuration Release
dotnet test .\RiftScan.slnx --configuration Release --no-build
dotnet format .\RiftScan.slnx --verify-no-changes
git diff --check
```

## Live RIFT/window-control authorization note

The user explicitly authorized current and future control of the RIFT window as needed for live testing.

Boundary to preserve:

- RIFT window control is allowed as helper/live-test scaffolding.
- Do not put input, launcher automation, coordinate clicking, or window-control behavior into RiftScan scanner core.
- RiftScan core remains read-only process observation plus offline analysis.
- If helper input is used, record the exact helper command/artifact and keep it separate from capture/analyzer truth claims.

## Current live process snapshot

Observed at handoff:

```text
Process: rift_x64
PID: 41220
Window title: RIFT
MainWindowHandle: 0xBD0D94
Start time: 2026-04-28 14:06:20 local
```

PID/HWND can drift; verify again before any live action.

## Fresh addon/API truth after recent update warning

Important: use the fresh artifacts below as the current live truth baseline. Older match files remain useful historical evidence only.

### Fresh live refresh

Truth summary:

```text
reports/generated/addon-api-truth-summary-actor-target-focus-live-refresh-20260430-063653.json
```

Observation scan:

```text
reports/generated/addon-api-observation-scan-actor-target-focus-live-refresh-20260430-063653.json
```

Observation JSONL:

```text
reports/generated/addon-api-observations-actor-target-focus-live-refresh-20260430-063653.jsonl
```

Key facts:

```text
observation_count: 3
truth_record_count: 3
observed kinds: current_player=1, player_loc=1, waypoint_status=1
source file last write: 2026-04-30T06:36:53.8252096+00:00
latest_player: Atank
zone_id: z487C9102D2EA79BE
location: Sanctum Watch
world XYZ: 7237.6196289062, 873.46997070312, 3051.0598144531
latest_target: null
latest_focus: null
latest_focus_target: null
warnings include: no_target_coordinate_truth_observed, no_focus_coordinate_truth_observed, no_focus_target_coordinate_truth_observed
```

### Tab-target attempt after refresh

Truth summary:

```text
reports/generated/addon-api-truth-summary-20260430-0638-tabtarget.json
```

Result:

```text
observation_count: 3
truth_record_count: 3
observed kinds: current_player=1, player_loc=1, waypoint_status=1
target/focus/focus_target: still null
```

Interpretation: Tab did not acquire a target at the current Sanctum Watch location, or the selected unit did not persist into the addon export. Do not claim target/focus coordinate truth from this attempt.

## Historical artifacts that must not be over-promoted

Latest committed artifact/feature before this uncommitted patch:

```text
0f6adca Match actor coordinates from addon truth summary
```

That commit added `riftscan rift match-addon-coords --truth-summary --truth-kind(s)` support and passed CI run `25150748579`.

Historical evidence from the old session:

```text
sessions/live-waypoint-317-223-wide256-20260430-0521-passive
reports/generated/actor-api-truth-summary-current-20260430-021443.json
reports/generated/actor-coordinate-matches-current-truthsummary-vs-wide256-20260430-021443.json
reports/generated/target-coordinate-matches-current-truthsummary-vs-wide256-20260430-021443.json
```

Historical current-player match:

```text
base family: 0x975E1D8000
top offset example: +0x47EC
axis: xyz
support: 8 snapshots
best max abs distance: 0.09375
```

Caution:

- The old target match used stale/self-target truth and matched the same player family.
- It is not distinct target actor proof.
- Because the client was recently updated, do not use old capture/match artifacts as current offset truth until a new live capture confirms them.

## Direct blocker

The current blocker is not parser support; it is missing fresh target/focus addon truth.

Current fresh addon truth says:

```text
player coordinates: observed
target coordinates: not observed
focus coordinates: not observed
focus-target coordinates: not observed
```

Distinct actor/player/target/focus coordinate discovery needs at least one of:

1. fresh selected target exported by the addon,
2. fresh focus or focus-target exported by the addon,
3. fresh live capture while a known target/focus is selected, with a matching truth summary from the same moment.

## Resume commands

### 1. Re-ground worktree

```powershell
cd 'C:\RIFT MODDING\Riftscan'
git status --short --branch
git log -1 --oneline --decorate
git diff --stat
```

### 2. Verify RIFT process before live action

```powershell
Get-Process rift_x64 -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,StartTime,MainWindowTitle,@{Name='MainWindowHandleHex';Expression={'0x{0:X}' -f $_.MainWindowHandle}}
```

### 3. If target/focus is manually selected in-game, refresh addon truth

Use the verified wrapper first if focus works:

```powershell
.\scripts\invoke-rift-addon-command-verified.ps1 -SenderBackend autohotkey-sendtext -SenderTextMode sendevent -CommandText "/rbx export" -Label actor-target-focus-selected-refresh -TargetProcessId <PID> -OpenChatBeforeCommand -ReloadUiAfterCommand -PostReloadDelaySeconds 8
```

If wrapper focus verification is blocked, direct AHK send was previously more reliable after Minion was closed:

```powershell
.\scripts\send-rift-slash-command-ahk.ps1 -CommandText "/rbx export" -TargetProcessId <PID> -TargetTitleContains RIFT -Focus -OpenChatBeforeCommand -TextMode sendevent -FocusDelayMilliseconds 1000 -PostSendDelayMilliseconds 500
.\scripts\send-rift-slash-command-ahk.ps1 -CommandText "/reloadui" -TargetProcessId <PID> -TargetTitleContains RIFT -Focus -OpenChatBeforeCommand -TextMode sendevent -FocusDelayMilliseconds 1000 -PostSendDelayMilliseconds 8000
```

Then regenerate/inspect addon API scan and truth summary using existing repo commands/scripts. Prefer a new label so artifacts are not confused with old evidence.

### 4. If fresh target/focus truth appears, run matcher only against a current capture

Do not reuse stale memory captures after the update for final truth. First create a fresh passive session package against the current `rift_x64`, then match against the fresh truth summary:

```powershell
# inspect exact CLI if needed
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe capture passive --help

# after current capture succeeds, example shape only:
.\src\RiftScan.Cli\bin\Release\net10.0\riftscan.exe rift match-addon-coords <fresh-session-path> --truth-summary <fresh-truth-summary.json> --truth-kind current_player --truth-kind target --truth-kind focus --truth-kind focus_target --out reports/generated/<fresh-match-output>.json
```

### 5. Validate before committing parser patch

```powershell
dotnet build .\RiftScan.slnx --configuration Release
dotnet test .\RiftScan.slnx --configuration Release --no-build
dotnet format .\RiftScan.slnx --verify-no-changes
git diff --check
```

Suggested commit message after validation:

```text
Promote focus target addon coordinate truth
```

## Recommended immediate strategy

1. Finish and validate the parser/truth-summary patch already in the worktree.
2. Get a fresh target/focus selected in-game, preferably manually or by a low-risk helper action outside scanner core.
3. Export/reload and produce a new addon API truth summary that contains non-null `latest_target`, `latest_focus`, or `latest_focus_target`.
4. Create a fresh read-only capture package after the recent client update.
5. Run offline matching against that same fresh truth summary; do not claim coordinate recovery from old stale capture/match artifacts.

## Top 5 next best recommended actions

1. Run full validation on the current uncommitted parser/docs patch.
2. Commit/push the parser patch if validation passes.
3. Acquire real fresh target/focus truth from the addon by selecting an NPC/player/focus target and exporting after `/reloadui`.
4. Capture a new valid live session package against the current `rift_x64` process.
5. Match fresh addon truth to the fresh capture and publish only observed/candidate truth with artifact paths.
