# RiftScan / RiftReader Actor Coordinate Discovery Handoff

## TL;DR

Discovery priority is now explicitly **RiftReader first, RiftScan second**:

- **RiftReader** is the fast live/custom-reader debug scanner.
- **RiftScan** is the replayable artifact/capture/analyzer layer.
- Do not block discovery on RiftScan process polish.
- Use RiftReader to resolve the current live proof coordinate source, then feed only the high-value target addresses into RiftScan for small replayable sessions.

Current live proof result for RIFT PID `41220`:

- Validated source object: `0x216F2F26020`
- Validated coord region: `0x216F2F26068`
- Source coord relative offset: `0x48`
- Valid coord triplet: `0x216F2F26068` = `0x216F2F26020 + 0x48`
- Bridge/selector/source-chain candidate: `0x216BE6A0000`
- Rejected stale trace object/control: `0x21693FB9E48`

## Repos and roles

### `C:\RIFT MODDING\RiftReader`

Role: fast live/debug discovery.

Use this repo to:

- resolve proof coord anchor from the custom reader path;
- validate current live coords against ReaderBridge;
- perform targeted pointer/source-chain scans;
- read small neighborhoods around source/bridge candidates.

Avoid as first move:

- full-process exact coordinate scans; they were too slow in this session;
- CE refresh paths unless custom-reader path fails and the user explicitly approves.

### `C:\RIFT MODDING\Riftscan`

Role: preserve replayable evidence and run offline analysis.

Use this repo to:

- store bridge packets from RiftReader results;
- capture targeted sessions around only the discovered addresses;
- verify/analyze/report captured sessions;
- compare across sessions after movement/turn labels exist.

Do not use RiftScan as the first live discovery engine when RiftReader can produce a current proof anchor faster.

## Current RIFT process

Observed current process during discovery:

- Process name: `rift_x64`
- PID: `41220`
- Window title: `RIFT`
- Path: `C:\Program Files (x86)\Glyph\Games\RIFT\Live\rift_x64.exe`

If resuming later, refresh PID first. Do not assume `41220` is still valid.

## RiftReader artifacts created

Directory:

```text
C:\RIFT MODDING\RiftReader\scripts\captures\codex-riftscan-delegate-20260430-093907
```

Important files:

```text
read-player-coord-anchor.json
resolved-proof-coord-anchor.json
scan-pointer-object-base.json
scan-pointer-coord-region.json
scan-pointer-trace-object-base.json
read-object-neighborhood.json
read-coord-neighborhood.json
read-trace-object-neighborhood.json
read-pointer-owner-region-216A50B0000.json
read-pointer-owner-region-216BE6A0000.json
read-pointer-owner-region-216E6E80000.json
```

## RiftScan artifacts created

Bridge packet:

```text
C:\RIFT MODDING\Riftscan\reports\generated\riftreader-delegate-actor-coordinate-scan-20260430-094639.json
```

Targeted replay session:

```text
C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839
```

Generated report:

```text
C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839\report.md
C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839\report.json
```

## Validated live coordinate truth

From RiftReader `resolved-proof-coord-anchor.json`:

```json
{
  "Mode": "proof-coord-anchor",
  "ProcessName": "rift_x64",
  "ProcessId": 41220,
  "CanonicalCoordSourceKind": "coord-trace-source-object",
  "MatchSource": "readerbridge-live",
  "ObjectBaseAddress": "0x216F2F26020",
  "CoordRegionAddress": "0x216F2F26068",
  "SourceObjectAddress": "0x216F2F26020",
  "SourceCoordRelativeOffset": 72,
  "CoordXRelativeOffset": 0,
  "CoordYRelativeOffset": 4,
  "CoordZRelativeOffset": 8
}
```

Live sample:

```text
X = 7259.063
Y = 875.5653
Z = 3052.8816
```

ReaderBridge expected:

```text
X = 7259.0600585938
Y = 875.57000732422
Z = 3052.8798828125
```

Match deltas:

```text
DeltaX = 0.0029296875
DeltaY = -0.004699707
DeltaZ = 0.0017089844
```

Conclusion: `0x216F2F26020 + 0x48` is the current proof-grade player coordinate triplet for this live process.

## Rejected trace object

The trace object is not the coordinate source:

```text
Trace object base: 0x21693FB9E48
Trace target:      0x21693FB9FA0
```

It failed coordinate comparison:

```text
CoordX = 6.8294895E+22
CoordY = 10.903187
CoordZ = 4.3990764E+21
```

Conclusion: keep `0x21693FB9E48` as a control/source-chain trace object, not actor coordinate truth.

## Best bridge / selector / owner candidate

Strongest candidate region:

```text
0x216BE6A0000
```

High-signal evidence:

```text
0x216BE6A00A8 -> 0x21693FB9E48    # rejected trace object
0x216BE6A00B0 -> 0x216F2F26020    # validated source object
0x216BE6A00F8 -> 0x7FF7879B117E    # coord access instruction
```

It also contains multiple `0x975E1E...` addresses, which ties it back to the prior RiftScan passive root family. Treat the old `0x975E...` values as provenance/root-family evidence, not current live addresses.

Status:

- High confidence as selector/source-chain bridge.
- Not yet promoted as stable actor owner because no movement/turn capture has labeled update behavior.

## Object layout observations

Validated source object:

```text
0x216F2F26020
```

Coord-like triplets observed in the object neighborhood:

```text
+0x48 = 7259.063, 875.5653, 3052.8816    # validated current proof coord
+0x88 = 7259.063, 875.5653, 3052.8816
+0xD8 = 7259.063, 875.5653, 3052.8816
+0xE4 = 7222.65, 873.141, 3026.55        # nearby secondary triplet
```

Need movement/turn labels to determine which duplicated triplets are current/previous/target/render/cache roles.

## Commands that produced the useful result

### RiftReader proof anchor

```powershell
cd 'C:\RIFT MODDING\RiftReader'
.\scripts\run-reader.cmd --pid 41220 --read-player-coord-anchor --json
.\scripts\resolve-proof-coord-anchor.ps1 -ProcessId 41220 -SkipRefresh -ProofCoordAnchorFile 'C:\RIFT MODDING\RiftReader\scripts\captures\codex-riftscan-delegate-20260430-093907\resolved-proof-coord-anchor.json' -Json
```

### RiftReader targeted scans

```powershell
.\scripts\run-reader.cmd --pid 41220 --scan-pointer 0x216F2F26020 --pointer-width 8 --scan-context 64 --max-hits 128 --json
.\scripts\run-reader.cmd --pid 41220 --scan-pointer 0x216F2F26068 --pointer-width 8 --scan-context 64 --max-hits 128 --json
.\scripts\run-reader.cmd --pid 41220 --scan-pointer 0x21693FB9E48 --pointer-width 8 --scan-context 64 --max-hits 128 --json
.\scripts\run-reader.cmd --pid 41220 --address 0x216F2F26020 --length 768 --json
.\scripts\run-reader.cmd --pid 41220 --address 0x216F2F26068 --length 256 --json
.\scripts\run-reader.cmd --pid 41220 --address 0x21693FB9E48 --length 512 --json
```

### RiftScan targeted replay capture

```powershell
cd 'C:\RIFT MODDING\Riftscan'
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj -- capture passive --pid 41220 --out 'C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839' --samples 4 --interval-ms 100 --max-regions 3 --max-bytes-per-region 4096 --max-total-bytes 49152 --base-addresses 0x216F2F26020,0x216BE6A0000,0x21693FB9E48 --stimulus passive_idle
```

### RiftScan validation/analysis/report

```powershell
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj -- verify session 'C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839'
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj -- analyze session 'C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839' --all
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj -- report session 'C:\RIFT MODDING\Riftscan\sessions\codex-riftreader-delegate-actor-coords-20260430-094839' --top 50
```

## Validation status

Successful:

- RiftReader proof anchor resolved.
- RiftReader live sample matched ReaderBridge coordinates.
- RiftReader pointer scans found the bridge table candidate.
- RiftScan bridge packet is valid JSON.
- RiftScan targeted capture succeeded.
- RiftScan verify succeeded with no issues.
- RiftScan analyze/report succeeded.

RiftScan targeted passive session stats:

```text
regions_captured = 3
snapshots_captured = 12
bytes_captured = 49152
region_read_failure_count = 0
```

## Known slow path to avoid

The whole-process exact ReaderBridge coordinate scan was too slow and was stopped:

```text
--scan-readerbridge-player-coords --scan-context 64 --max-hits 64
```

It ran for about 84 seconds before being killed. Do not use this as the first move in the fast discovery loop. Prefer proof anchor + pointer/source-chain scans.

## Current repo state caveat

At the time of this handoff, both repos already had unrelated dirty work from prior activity.

RiftScan had modified/untracked source files from earlier actor-coordinate owner tooling work. This handoff did not attempt to stage or clean them.

RiftReader had existing modified files:

```text
reader/RiftReader.Reader/Telemetry/TelemetrySources.cs
scripts/capture-actor-orientation.ps1
scripts/navigation/new-forward-smoke-route.ps1
scripts/resolve-proof-coord-anchor.ps1
```

Do not assume those edits were created by this handoff step.

## Next smallest action

Run a movement-labeled targeted capture around only:

```text
0x216F2F26020
0x216BE6A0000
0x21693FB9E48 optional control
```

Preferred workflow:

1. Refresh proof anchor with RiftReader.
2. Build the target address list from the fresh anchor.
3. Start a short RiftScan capture with an intervention wait.
4. During wait, perform a small real movement forward or approve Codex to send one movement key pulse.
5. Analyze/report and compare against the passive session.

Suggested capture shape:

```powershell
cd 'C:\RIFT MODDING\Riftscan'
dotnet run --project .\src\RiftScan.Cli\RiftScan.Cli.csproj -- capture passive --pid <fresh_pid> --out 'sessions\actor-coord-move-forward-<timestamp>' --samples 8 --interval-ms 100 --max-regions 3 --max-bytes-per-region 4096 --max-total-bytes 98304 --base-addresses <fresh_source_object>,<fresh_bridge_table>,<fresh_trace_object> --stimulus move_forward --stimulus-note controlled_move_forward_actor_coord_layout_label --intervention-wait-ms 120000 --intervention-poll-ms 2000
```

## Top 5 recommended next actions

1. Run the short movement-labeled targeted capture to label `+0x48`, `+0x88`, `+0xD8`, and `+0xE4` behavior.
2. Run a turn-only targeted capture after that to split coordinate fields from actor yaw/orientation fields.
3. Add a small RiftScan verifier that consumes the RiftReader bridge packet and emits a stable summary, instead of hand-written bridging.
4. Keep using RiftReader proof anchor refresh before every live capture so stale addresses do not poison RiftScan sessions.
5. Avoid full-process exact scans unless region-bounded; they are too slow for the fast discovery lane.
