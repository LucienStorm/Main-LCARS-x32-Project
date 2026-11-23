# Open slice acceptance gaps @ 0.7.2.334

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @334 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** (330+) |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **Birthday/Custom1–4/Notes/Other/Work+Home address** gates |

**Ruling (not REAL):** Custom/notes/address gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @334 |
|--------|-------------|
| vCard depth | **Partial+** — structured **ADR** pull (city/region/postal); **BuildVCardAdrValue** export |
| Multi-book / live | **Partial+** (330+) |
| 412 / live | **Partial+** — live status shows **412 email** after merge |

**Ruling (not REAL):** ADR depth + 412 email hint ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @334 |
|--------|-------------|
| PROPFIND display names | **Partial+** (331+) |
| Lightning-class UX | **Partial+** — **2ND-TAG n** on secondary calendar pulls |

**Ruling (not REAL):** Secondary TAG telemetry ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @334 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-PUSH-CAL-2** after third bidirectional pass |
| GAL visibility | **Partial+** — **BG-GAL-4** fourth shadow push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Fourth push tier ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @334 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **2600** rows/run; **X-LCARS-FolderPath** in corpus |
| Scale | **Partial+** — **17×** backfill @ ≥260k; meta backfill **9000**/run |

**Ruling (not REAL):** FolderPath corpus + 260k scale ≠ Gloda unified index REAL.

## Shipped @334 (code pointers)

- `MorkAbookParser`: birthday/custom/notes/address gates  
- `CardDavSyncService`: `FormatVCardAdr` + `BuildVCardAdrValue`; mail **412 email** hint  
- `LastSecondaryEventsTagged` / **2ND-TAG** (mail + LCARSCalendar)  
- `EwsBackgroundSync`: `LastBackgroundCalPushAfterThirdPass`, `LastBackgroundGalPushFourthRetry`  
- `MailSearchBackgroundIndexer` 2600 / 260k 17× / 9000 meta  
