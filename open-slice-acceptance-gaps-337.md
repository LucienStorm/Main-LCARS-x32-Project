# Open slice acceptance gaps @ 0.7.2.337

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @337 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **TABLES n** + **ROWS n** parse telemetry |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** (336+) |

**Ruling (not REAL):** Table/row telemetry ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @337 |
|--------|-------------|
| vCard depth | **Partial+** — **LANG** pull → contact field |
| Multi-book / live | **Partial+** — live **REMOTE n** pull count |
| 412 / live | **Partial+** (336+) |

**Ruling (not REAL):** LANG pull + remote hint ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @337 |
|--------|-------------|
| PROPFIND display names | **Partial+** — **PROP n** stored URL label count |
| Lightning-class UX | **Partial+** — **2ND-PULL n** secondary calendar merges |

**Ruling (not REAL):** PROP/2ND-PULL status ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @337 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-RECON-3**, **BG-PUSH-CAL-4**, **BG-CAL-6** |
| GAL visibility | **Partial+** — **BG-GAL-7** seventh shadow push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Seventh GAL tier ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @337 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **3200** rows/run; **X-LCARS-HasBody** in corpus |
| Scale | **Partial+** — **20×** backfill @ ≥320k; meta backfill **12000**/run |

**Ruling (not REAL):** HasBody corpus + 320k scale ≠ Gloda unified index REAL.

## Shipped @337 (code pointers)

- `MorkAbookParser`: `LastMorkTableCount`, `LastMorkParsedContactCount`  
- `CardDavSyncService`: LANG pull; mail **REMOTE** hint  
- `LastSecondaryPullMergeAdded` / **2ND-PULL**; **PROP** label counts  
- `EwsBackgroundSync`: RECON-3, PUSH-CAL-4, CAL-6, GAL-7  
- `MailSearchBackgroundIndexer` 3200 / 320k 20× / 12000 meta  
