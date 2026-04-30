# RiftScan waypoint API handoff

## TL;DR

RiftScan main is clean and synced. The current best waypoint strategy is API-first: use `Inspect.Map.Waypoint.Get("player")` and `Event.Map.Waypoint.Update`, not `/loc` and not TomTom.

`/loc` is now documented/implemented as a lower-priority manual fallback because it needs foreground game-window focus and uninterrupted keyboard input. TomTom is explicitly excluded until it is developed enough to be trusted.

## Current repo state

- Repo: `C:\RIFT MODDING\Riftscan`
- Branch: `main`
- Latest pushed commit before this handoff: `d65ee1e Document API-first waypoint discovery`
- GitHub Actions for `d65ee1e`: success, run `25142319005`
- Current local status before creating this handoff: clean/synced with `origin/main`

Recent relevant commits:

```text
d65ee1e Document API-first waypoint discovery
6c93573 Preserve loc equivalent source
c0e3fde Add loc observation support
28a76d6 Stop using TomTom observations
781448c Add addon API observation scanner
5d995bb Add coordinate mirror context analyzer
```

## Decisions locked in

1. **Do not rely on TomTom.**
   - TomTom SavedVariables are intentionally ignored by RiftScan.
   - Regression test confirms `TomTom.lua` emits zero observations.

2. **Do not make `/loc` the primary lane.**
   - `/loc` requires game focus and clean input control.
   - It is a manual/live-input fallback only.

3. **Use the in-game waypoint API first.**
   - `Inspect.Map.Waypoint.Get("player")` returns the player's current waypoint `x,z` when one exists.
   - `Event.Map.Waypoint.Update(units)` signals waypoint changes.
   - This avoids focus/keyboard-control risk.

4. **Distinguish true `/loc` from API-derived loc-equivalent.**
   - True slash output: `confidence_level = ingame_loc_output`.
   - API-derived equivalent: `confidence_level = loc_equivalent_from_api` and `api_source = Inspect.Unit.Detail.coordX_coordZ`.

## RiftScan implementation status

RiftScan has these shipped pieces:

- `riftscan rift addon-api-observations <SavedVariables path>` command.
- `RiftAddonApiObservationService` parses:
  - current player world XYZ from addon SavedVariables.
  - target/nearby/party coordinate observations when present.
  - direct waypoint tables as `kind = waypoint` / `api_source = Inspect.Map.Waypoint.Get`.
  - player loc observations as `kind = player_loc`.
- JSON contract fields include:
  - `waypoint_x`, `waypoint_z`
  - `loc_x`, `loc_y`, `loc_z`
  - `raw_text`
  - `api_source`
  - `confidence_level`

Important docs:

- `docs/addon-api-observation-workflow.md`

Important live artifacts:

- `reports/generated/addon-api-observation-scan-live-loc-equivalent-20260430.json`
- `reports/generated/addon-api-observations-live-loc-equivalent-20260430.jsonl`

## Addon-side local state

ReaderBridgeExport is outside the RiftScan git repo:

```text
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\main.lua
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\README.md
```

Local addon edits made:

- Added `safeMapWaypointGet(unit)` using `Inspect.Map.Waypoint.Get`.
- Added `buildPlayerWaypoint("player")` to export `waypoint = { x, z, unit, source }` when active.
- Added `buildPlayerWaypointStatus(...)` diagnostic intent:
  - `apiAvailable`
  - `hasWaypoint`
  - `updateCount`
  - `lastUpdateAt`
  - `lastUpdateUnits`
- Added `Event.Map.Waypoint.Update` hook intent.
- Added `loc` equivalent from player `Inspect.Unit.Detail.coordX/coordY/coordZ`, clearly labeled non-TomTom and not true slash `/loc`.

Static addon validation:

```text
luac -p C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\main.lua
```

passed.

## Live validation status

A clipboard-based reload/export/save cycle did successfully write `loc` into live SavedVariables:

```lua
loc = {
  locationName = "Sanctum Watch",
  note = "Derived non-TomTom /loc-equivalent; compare against live /loc output before treating as captured slash output.",
  raw = "/loc-equivalent Sanctum Watch 7233.810059 3051.379883",
  source = "Inspect.Unit.Detail.coordX_coordZ",
  x = 7233.8100585938,
  y = 873.01995849609,
  z = 3051.3798828125,
  zone = "z487C9102D2EA79BE"
}
```

RiftScan parsed that into `kind = player_loc`, `confidence_level = loc_equivalent_from_api`.

However, the later `waypointStatus` diagnostic did **not** appear in SavedVariables after the attempted reload/export/save cycle. Treat that as unresolved. Likely next checks:

1. Full client restart or a verified clean `/reloadui` after the latest addon edit.
2. Confirm no addon runtime error in the RIFT console.
3. Confirm the currently loaded addon file is the edited `ReaderBridgeExport\main.lua`.
4. Set/activate a real player waypoint before expecting a `waypoint` table.

Current SavedVariables after the last live check did **not** include `waypoint =` or `waypointStatus =`.

## Exact API docs used

Local API docs confirm:

```lua
x, z = Inspect.Map.Waypoint.Get(unit)
```

and:

```lua
Event.Map.Waypoint.Update(units)
```

Relevant files:

```text
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\RIFT_API_Docs\LLM_RIFT_API\Map\inspect_map_waypoint_get.md
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\RIFT_API_Docs\LLM_RIFT_API\Map\event_map_waypoint_update.md
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\RIFT_API_Docs\LLM_RIFT_API\Map\command_map_waypoint_set.md
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\RIFT_API_Docs\LLM_RIFT_API\Map\command_map_waypoint_clear.md
```

## Recommended next action

Do **not** continue with `/loc` automation. Instead:

1. Make sure ReaderBridgeExport reloads the latest addon file cleanly.
2. Set an actual player waypoint in-game.
3. Trigger `ReaderBridgeExport` export/save.
4. Check SavedVariables for:
   - `waypoint = { x = ..., z = ..., source = "Inspect.Map.Waypoint.Get" }`
   - or `waypointStatus = { apiAvailable = true, hasWaypoint = false }`
5. Run:

```powershell
dotnet run --project C:\RIFT MODDING\Riftscan\src\RiftScan.Cli\RiftScan.Cli.csproj --configuration Release -- rift addon-api-observations "C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\Saved" --addon-name ReaderBridgeExport --json-out reports/generated/addon-api-observation-scan-waypoint-live.json --jsonl-out reports/generated/addon-api-observations-waypoint-live.jsonl
```

## Resume prompt

```text
Resume in C:\RIFT MODDING\Riftscan. Read docs/handoffs/2026-04-29-212311-riftscan-waypoint-api-handoff.md first. Current waypoint strategy is API-first: Inspect.Map.Waypoint.Get("player") and Event.Map.Waypoint.Update. Do not rely on TomTom. Treat /loc as manual fallback only. Next: verify ReaderBridgeExport loads latest local addon edits, set/activate a real waypoint, force export/save, then run riftscan rift addon-api-observations and inspect whether waypoint or waypointStatus appears.
```
