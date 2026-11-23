# Open slice acceptance gaps @ 0.7.2.333

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @333 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** (330+) |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **Home/Fax/Pager/Web/Spouse/Anniversary** gates |

**Ruling (not REAL):** Extended optional-field gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @333 |
|--------|-------------|
| vCard depth | **Partial+** — **TEL PREF** export (`CELL,PREF` / `WORK,PREF`); **PickPreferredVCardTel** on pull |
| Multi-book / live | **Partial+** (330+) |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** PREF telephone vCard ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @333 |
|--------|-------------|
| PROPFIND display names | **Partial+** (331+) |
| Lightning-class UX | **Partial+** — live **TAG n** primary cal label on pull (mail + LCARSCalendar) |

**Ruling (not REAL):** TAG telemetry ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @333 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-CAL-3** third bidirectional pass when still dirty |
| GAL visibility | **Partial+** — **BG-GAL-3** third shadow push when GAL-dirty remains |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Third retry passes ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @333 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **2400** rows/run; **X-LCARS-Uid** in corpus |
| Scale | **Partial+** — **16×** backfill @ ≥240k; meta backfill **8000**/run |

**Ruling (not REAL):** Uid corpus + 240k scale ≠ Gloda unified index REAL.

## Shipped @333 (code pointers)

- `MorkAbookParser`: home/fax/pager/web/spouse/anniversary gates  
- `CardDavSyncService`: `PickPreferredVCardTel`; TEL PREF export  
- `CalDavCalendarSync.LastPrimaryEventsTagged`; mail/calendar **TAG** status  
- `EwsBackgroundSync`: `LastBackgroundCalBidirectionalThirdPass`, `LastBackgroundGalPushThirdRetry`  
- `MailSearchBackgroundIndexer` 2400 / 240k 16× / 8000 meta; `X-LCARS-Uid` FTS  
