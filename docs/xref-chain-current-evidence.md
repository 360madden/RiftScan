# RiftScan xref-chain current evidence

Timestamp: 2026-04-29 18:52 America/New_York

Status: **validated pointer-chain evidence plus addon-coordinate-matched vec3 candidates and movement-response evidence, not final semantic truth**.

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
- `sessions/live-coordinate-motion-20260429-before`
- `sessions/live-coordinate-motion-20260429-after-w`
- `reports/generated/addon-coordinate-observations-20260429-after-w.jsonl`
- `reports/generated/addon-coordinate-scan-20260429-after-w.json`
- `reports/generated/live-coordinate-motion-20260429-before-addon-coordinate-matches.json`
- `reports/generated/live-coordinate-motion-20260429-before-addon-coordinate-matches.md`
- `reports/generated/live-coordinate-motion-20260429-after-w-addon-coordinate-matches.json`
- `reports/generated/live-coordinate-motion-20260429-after-w-addon-coordinate-matches.md`
- `reports/generated/live-coordinate-motion-20260429-before-after-coordinate-motion.json`
- `reports/generated/live-coordinate-motion-20260429-before-after-coordinate-motion.md`

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

## Movement-response result

Live control note: the RIFT window was switched by exact window title and `W` was held for `1400ms` via Windows keyboard events against PID `41220`. This is external test control only; no input/window-control code was added to RiftScan core.

Before/after focused captures:

```powershell
riftscan capture passive --pid 41220 `
  --out sessions/live-coordinate-motion-20260429-before `
  --samples 6 --interval-ms 100 `
  --max-regions 1 --max-bytes-per-region 65536 --max-total-bytes 1048576 `
  --base-addresses 0x975E1D8000 `
  --stimulus passive_idle

riftscan capture passive --pid 41220 `
  --out sessions/live-coordinate-motion-20260429-after-w `
  --samples 6 --interval-ms 100 `
  --max-regions 1 --max-bytes-per-region 65536 --max-total-bytes 1048576 `
  --base-addresses 0x975E1D8000 `
  --stimulus move_forward
```

Comparison command:

```powershell
riftscan rift compare-addon-coordinate-motion `
  reports/generated/live-coordinate-motion-20260429-before-addon-coordinate-matches.json `
  reports/generated/live-coordinate-motion-20260429-after-w-addon-coordinate-matches.json `
  --min-delta-distance 1 `
  --top 100 `
  --out reports/generated/live-coordinate-motion-20260429-before-after-coordinate-motion.json `
  --report-md reports/generated/live-coordinate-motion-20260429-before-after-coordinate-motion.md
```

Result summary:

- `success=true`
- pre candidates: `15`
- post candidates: `15`
- common candidates: `15`
- moved candidates: `15`
- shared movement delta: `+1.830078, +0.000305, +0.754395`
- delta distance: `1.979469`
- warning: `addon_observations_may_be_stale_or_identical_between_pre_and_post`

Representative moved offsets:

| Offset | Absolute | Axis | Pre xyz | Post xyz | Delta xyz | Distance |
|---:|---:|---|---|---|---|---:|
| `0x47EC` | `0x975E1DC7EC` | `xyz` | `7222.555664, 873.196777, 3026.510986` | `7224.385742, 873.197083, 3027.265381` | `+1.830078, +0.000305, +0.754395` | `1.979469` |
| `0x482C` | `0x975E1DC82C` | `xyz` | `7222.555664, 873.196777, 3026.510986` | `7224.385742, 873.197083, 3027.265381` | `+1.830078, +0.000305, +0.754395` | `1.979469` |
| `0x3A50` | `0x975E1DBA50` | `xyz` | `7222.455566, 873.096802, 3026.410889` | `7224.285645, 873.097107, 3027.165283` | `+1.830078, +0.000305, +0.754395` | `1.979469` |

Interpretation: `0x975E1D8000` is now behavior-backed as a live coordinate-vector family for this process instance. Multiple offsets are synchronized mirrors/copies, so this still does not identify the single canonical owner field.

## Interpretation

- This is stable owner/provenance evidence for the current process instance.
- Addon observations now corroborate coordinate-like vec3 copies inside the `0x975E1D8000` vector family.
- Controlled movement now proves the matched vec3 copies respond to player translation.
- It does **not** prove that `0x975E1D8000+0x47EC` is the canonical owner field, actor yaw, or camera truth.
- The next semantic validation must separate canonical live position from synchronized mirrors/cache copies and refresh addon coordinates after movement.
- RiftScan core remains read-only. Do not add input/window control or launcher automation to scanner core.

## Next smallest proof step

Add a mirror-clustering/promotion gate that groups synchronized offsets, then require a fresh post-movement addon coordinate export before promoting any one offset as canonical.
