# Scalar truth live run checklist

Use this checklist for each live scalar truth attempt. Fill it in before promoting any scalar evidence.

## Run metadata

- Date/time UTC:
- Operator:
- RIFT process ID:
- RIFT process name:
- Source plan path:
- Build commit:
- RiftScan command style:
  - `dotnet run --configuration Release --no-build`
  - packaged `riftscan`

## Capture sessions

| Stimulus | Session ID/path | Samples | Interval ms | Windows/region | Status | Notes |
|---|---|---:|---:|---:|---|---|
| `passive_idle` |  |  |  |  |  |  |
| `turn_left` |  |  |  |  |  |  |
| `turn_right` |  |  |  |  |  |  |
| `camera_only` |  |  |  |  |  |  |

## Required verification

Run for every session:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify session sessions/<session_id>
```

| Session | Verify success | Analyze success | Report path |
|---|---|---|---|
| passive |  |  |  |
| turn_left |  |  |  |
| turn_right |  |  |  |
| camera_only |  |  |  |

## Scalar-set aggregation

Command:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-set `
  sessions/<passive_id> `
  sessions/<turn_left_id> `
  sessions/<turn_right_id> `
  sessions/<camera_only_id> `
  --top 100 `
  --out reports/generated/<run>-scalar-evidence-set.json `
  --report-md reports/generated/<run>-scalar-evidence-set.md `
  --truth-out reports/generated/<run>-scalar-truth-candidates.jsonl
```

Results:

- Scalar evidence JSON:
- Scalar evidence report:
- Truth candidates JSONL:
- Ranked candidate count:
- Rejected summary count:
- Best candidate classification:
- Best candidate truth readiness:
- Best candidate warning:

## Optional corroboration

Corroboration file:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  verify scalar-corroboration reports/generated/<run>-scalar-truth-corroboration.jsonl
```

| Corroboration field | Value |
|---|---|
| Verify success |  |
| Corroborated candidates |  |
| Conflicted candidates |  |
| Notes |  |

## Repeat recovery

Second independent truth-candidate JSONL:

```powershell
dotnet run --project src/RiftScan.Cli/RiftScan.Cli.csproj --configuration Release --no-build -- `
  compare scalar-truth `
  reports/generated/<run-1>-scalar-truth-candidates.jsonl `
  reports/generated/<run-2>-scalar-truth-candidates.jsonl `
  --out reports/generated/<run>-scalar-truth-recovery.json
```

Recovery result:

- Recovery JSON:
- Input candidate count:
- Recovered candidate count:
- Best recovered candidate:
- Recovery warning:

## Promotion decision

Do not claim recovered scalar truth unless all required evidence is present.

| Evidence gate | Required | Observed |
|---|---|---|
| Passive baseline stable | yes |  |
| `turn_left` changed | yes for actor yaw |  |
| `turn_right` changed | yes for actor yaw |  |
| Opposite turn polarity | yes for actor yaw |  |
| `camera_only` stable for actor yaw OR changed for camera yaw | yes |  |
| Truth readiness `validated_candidate` or better | yes |  |
| Repeat recovery or external corroboration | recommended before recovered claim |  |
| No conflict status | yes |  |

Final status:

- `insufficient`
- `candidate`
- `strong_candidate`
- `validated_candidate`
- `recovered_candidate`

Decision notes:

- 

## Guardrail reminder

RiftScan scalar outputs are evidence artifacts. Do not convert them into final truth language unless the artifact claim level and supporting validation justify it.
