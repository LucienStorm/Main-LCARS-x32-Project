# Open slice acceptance gaps @ 0.7.2.338

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @338 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **RMAP n** row-property map telemetry |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** (337+) |

**Ruling (not REAL):** Row-map telemetry ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @338 |
|--------|-------------|
| vCard depth | **Partial+** — **ADR WORK/HOME,PREF**; **LANG** from Custom3 on export |
| Multi-book / live | **Partial+** — live **CARD-PUSH n** |
| 412 / live | **Partial+** (337+) |

**Ruling (not REAL):** ADR PREF + push hint ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @338 |
|--------|-------------|
| PROPFIND display names | **Partial+** (337+) |
| Lightning-class UX | **Partial+** — **1ST-PULL n** + **TASK-PULL n** split telemetry |

**Ruling (not REAL):** Split pull telemetry ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @338 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-RECON-4**, **BG-CK-3**, **BG-PUSH-CAL-5**, **BG-CAL-7** |
| GAL visibility | **Partial+** — **BG-GAL-8** eighth shadow push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Eighth GAL tier ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @338 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **3400** rows/run; **X-LCARS-ThreadRoot** in corpus |
| Scale | **Partial+** — **21×** backfill @ ≥340k; meta backfill **13000**/run |

**Ruling (not REAL):** ThreadRoot corpus + 340k scale ≠ Gloda unified index REAL.

## Shipped @338 (code pointers)

- `MorkAbookParser.LastMorkRowMapCount`  
- `CardDavSyncService`: ADR PREF + LANG from Custom3; **CARD-PUSH** hint  
- `LastPrimaryPullMergeAdded` / `LastTaskPullMergeAdded` UX  
- `EwsBackgroundSync`: RECON-4, CK-3, PUSH-CAL-5, CAL-7, GAL-8  
- `MailSearchBackgroundIndexer` 3400 / 340k 21× / 13000 meta  
