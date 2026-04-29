# RiftScan xref-chain current evidence

Timestamp: 2026-04-29 18:10 America/New_York

Status: **validated pointer-chain evidence, not final semantic truth**.

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
- `reports/generated/live-chain-region-inventory-20260429-keepgoing.json`
- `reports/generated/live-chain-focus-20260429-target-vector-xrefs.json`
- `reports/generated/live-chain-focus-20260429-owner-source-xrefs.json`
- `reports/generated/live-chain-focus-20260429-upstream-link-xrefs.json`
- `reports/generated/live-chain-focus-20260429-xref-chain-summary.json`
- `reports/generated/live-chain-focus-20260429-xref-chain-summary.md`

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

## Interpretation

- This is stable owner/provenance evidence for the current process instance.
- It does **not** prove that `0x975E1D8000` is final player position, actor yaw, or camera truth.
- The next semantic validation must compare values against behavior labels and addon coordinate truth.
- RiftScan core remains read-only. Do not add input/window control or launcher automation to scanner core.

## Next smallest proof step

Use addon coordinate observations plus a focused passive-vs-move or live promoted-coordinate validation against the `0x975E1D8000` vector family, while keeping this xref chain as the provenance anchor.
