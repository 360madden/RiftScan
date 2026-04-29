# RiftScan xref-chain current evidence

Timestamp: 2026-04-29 18:36 America/New_York

Status: **validated pointer-chain evidence plus addon-coordinate-matched vec3 candidates, not final semantic truth**.

## Current verified chain

Fresh focused read-only capture against live `rift_x64` PID `41220` reproduced the same pointer graph across all 6 samples:

```text
0x975E5FE000 <-> 0x975E234000 -> 0x975E1D8000
```

Stable edges:

| Source base | Source offset | Pointer value | Classification | Support |
|---:|---:|---:|---|---:|
| `0x975E234000` | `0x10` | `0x975E5FE000` | `outside_exact_target_pointer_edge` | 6 |
| `0x975E234000` | `0x3010` | `0x975E1D8000` | `outside_exact_target_pointer_edge` | 6 |
| `0x975E5FE000` | `0x1538` | `0x975E234000` | `outside_exact_target_pointer_edge` | 6 |
| `0x975E5FE000` | `0x1838` | `0x975E234000` | `outside_exact_target_pointer_edge` | 6 |
| `0x975E5FE000` | `0x1BA8` | `0x975E234000` | `outside_exact_target_pointer_edge` | 6 |

Verified reciprocal pair:

| First base | Second base | Support |
|---:|---:|---:|
| `0x975E234000` | `0x975E5FE000` | 6 |

## Local evidence artifacts

These artifacts are intentionally under ignored capture/report directories and are not committed:

- `sessions/live-chain-focus-20260429-keepgoing-passive_idle`
- `reports/generated/addon-coordinate-observations-20260429-xref-chain.jsonl`
- `reports/generated/addon-coordinate-scan-20260429-xref-chain.json`
- `reports/generated/live-chain-region-inventory-20260429-keepgoing.json`
- `reports/generated/live-chain-focus-20260429-target-vector-xrefs.json`
- `reports/generated/live-chain-focus-20260429-owner-source-xrefs.json`
- `reports/generated/live-chain-focus-20260429-upstream-link-xrefs.json`
- `reports/generated/live-chain-focus-20260429-xref-chain-summary.json`
- `reports/generated/live-chain-focus-20260429-xref-chain-summary.md`
- `reports/generated/live-chain-focus-20260429-addon-coordinate-matches.json`
- `reports/generated/live-chain-focus-20260429-addon-coordinate-matches.md`

Capture result:

- samples: `6`
- regions: `3`
- snapshots: `18`
- bytes: `565248`
- read failures: `0`

Machine verifier result:

```powershell
riftscan verify xref-chain-summary `
  reports/generated/live-chain-focus-20260429-xref-chain-summary.json `
  --min-support 6 `
  --require-edge 0x975E234000=0x975E5FE000 `
  --require-edge 0x975E234000=0x975E1D8000 `
  --require-edge 0x975E5FE000=0x975E234000 `
  --require-reciprocal 0x975E234000=0x975E5FE000
```

Result: `success=true`, `issues=[]`.

## Addon-coordinate match result

The newest offline match pass used addon SavedVariables observations as validator input against the stored snapshots only:

```powershell
riftscan rift match-addon-coords `
  sessions/live-chain-focus-20260429-keepgoing-passive_idle `
  --observations reports/generated/addon-coordinate-observations-20260429-xref-chain.jsonl `
  --region-base 0x975E1D8000 `
  --tolerance 5 `
  --top 100 `
  --out reports/generated/live-chain-focus-20260429-addon-coordinate-matches.json `
  --report-md reports/generated/live-chain-focus-20260429-addon-coordinate-matches.md
```

Result summary:

- `success=true`
- observations scanned/used: `76/76`
- snapshots scanned: `6`
- bytes scanned: `393216`
- match count: `450`
- candidate count: `15`
- warning: `match_output_truncated_by_top_limit`

Top candidate:

| Candidate | Base | Offset | Absolute | Axis | Support | Observation support | Best max abs distance | Addon sources |
|---|---:|---:|---:|---|---:|---:|---:|---|
| `rift-addon-coordinate-candidate-000001` | `0x975E1D8000` | `0x47EC` | `0x975E1DC7EC` | `xyz` | 6 | 5 | `0.094238` | `AutoFish`, `ReaderBridgeExport`, `RiftReaderValidator` |

Matched memory value:

```text
memory xyz = 7222.555664, 873.196777, 3026.510986
addon  xyz = 7222.649902, 873.139954/873.190002, 3026.550049
zone       = z487C9102D2EA79BE
```

Important limitation: this proves addon-coordinate corroboration for repeated vec3 copies in the `0x975E1D8000` target region. It does **not** yet prove which copy is canonical live player position, nor that the address is durable across sessions.

## Interpretation

- This is stable owner/provenance evidence for the current process instance.
- Addon observations now corroborate coordinate-like vec3 copies inside the `0x975E1D8000` vector family.
- It does **not** prove that `0x975E1D8000+0x47EC` is final player position, actor yaw, or camera truth.
- The next semantic validation must prove behavior response after controlled movement and separate canonical live position from mirrored/cache copies.
- RiftScan core remains read-only. Do not add input/window control or launcher automation to scanner core.

## Next smallest proof step

Capture the same focused region after a small player translation and re-run `riftscan rift match-addon-coords` to determine which candidate offsets update with the player and which are static/mirrored copies.
