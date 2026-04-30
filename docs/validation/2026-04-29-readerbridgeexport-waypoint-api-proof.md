# ReaderBridgeExport waypoint API live proof

## TL;DR

Live validation confirmed the API-first waypoint lane works. `ReaderBridgeExport`
can set a temporary player waypoint with `Command.Map.Waypoint.Set`, read it back
with `Inspect.Map.Waypoint.Get("player")`, persist it to SavedVariables, and
RiftScan parses it as a `kind = "waypoint"` observation.

This is addon/API validation evidence, not final memory-offset recovery.

## Run context

- Local date: 2026-04-29 EDT
- UTC evidence time: 2026-04-30 02:20:53 UTC
- RIFT process verified before live input:
  - process: `rift_x64`
  - PID: `41220`
  - window title: `RIFT`
- External addon source, outside this repo:
  - `C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\main.lua`
  - `C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\README.md`

## Addon-side changes used for the proof

`ReaderBridgeExport` now includes explicit waypoint test helpers:

- `/rbx waypoint-test [dx dz]`
  - reads current player coords from `Inspect.Unit.Detail`.
  - sets a temporary waypoint at player coord plus offset using
    `Command.Map.Waypoint.Set`.
  - default offset is `20 0`.
- `/rbx waypoint-clear`
  - clears the current player waypoint using `Command.Map.Waypoint.Clear`.
- `waypointStatus` records:
  - `apiAvailable`
  - `setApiAvailable`
  - `clearApiAvailable`
  - `hasWaypoint`
  - `lastCommand*`
  - `lastUpdate*`

Static validation passed:

```powershell
luac -p "C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\AddOns\ReaderBridgeExport\main.lua"
```

## Live command sequence

The RIFT window was foreground-verified, then these commands were sent through
chat input:

```text
/reloadui
/rbx waypoint-test 20 0
/rbx export
/reloadui
```

The final `/reloadui` forced the addon state to disk through RIFT's normal
SavedVariables save path.

## SavedVariables evidence

File:

```text
C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\Saved\rift315.1@gmail.com\Deepwood\Atank\SavedVariables\ReaderBridgeExport.lua
```

Observed persisted waypoint block:

```lua
waypoint = {
  source = "Inspect.Map.Waypoint.Get",
  unit = "player",
  x = 7257.6196289062,
  z = 3051.0598144531
},
waypointStatus = {
  apiAvailable = true,
  clearApiAvailable = true,
  hasWaypoint = true,
  lastCommand = "waypoint-test",
  lastCommandX = 7257.6196289062,
  lastCommandZ = 3051.0598144531,
  setApiAvailable = true,
  source = "Inspect.Map.Waypoint.Get",
  unit = "player",
  updateCount = 1,
  x = 7257.6196289062,
  z = 3051.0598144531
}
```

The corresponding player location in the same snapshot was:

```text
x = 7237.6196289062
z = 3051.0598144531
```

The test waypoint was exactly the requested `+20, +0` offset from the player X/Z.

## RiftScan parser proof

Command:

```powershell
dotnet run --project "C:\RIFT MODDING\Riftscan\src\RiftScan.Cli\RiftScan.Cli.csproj" --configuration Release -- rift addon-api-observations "C:\Users\mrkoo\OneDrive\Documents\RIFT\Interface\Saved" --addon-name ReaderBridgeExport --json-out reports/generated/addon-api-observation-scan-waypoint-live-20260430.json --jsonl-out reports/generated/addon-api-observations-waypoint-live-20260430.jsonl
```

Generated local artifacts:

- `C:\RIFT MODDING\Riftscan\reports\generated\addon-api-observation-scan-waypoint-live-20260430.json`
- `C:\RIFT MODDING\Riftscan\reports\generated\addon-api-observations-waypoint-live-20260430.jsonl`

Parser result:

- `success = true`
- `files_scanned = 132`
- `observation_count = 4`
- waypoint observation:
  - `observation_id = rift-addon-api-obs-000004`
  - `kind = waypoint`
  - `source_pattern = waypoint_table_xz`
  - `api_source = Inspect.Map.Waypoint.Get`
  - `source_mode = DirectAPI`
  - `coordinate_space = map_xz`
  - `confidence_level = addon_savedvariables_direct`
  - `waypoint_x = 7257.6196289062`
  - `waypoint_z = 3051.0598144531`

## Conclusion

The current best waypoint-info strategy remains API-first:

1. use addon/API output as semantic truth context;
2. prefer `Inspect.Map.Waypoint.Get("player")` over `/loc` automation;
3. keep TomTom excluded;
4. use the saved `waypoint` and current-player coord pair as a high-signal
   offline analyzer anchor;
5. still require RiftScan memory artifacts before claiming recovered memory truth.

## Remaining risk

The `ReaderBridgeExport` addon source is outside this RiftScan repo. Preserve or
promote it into a proper tracked addon-support repo before depending on those
helper commands long term.
