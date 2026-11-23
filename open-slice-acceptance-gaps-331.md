# Open slice acceptance gaps @ 0.7.2.331

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @331 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** (330+) |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **JobTitle/NickName/AimScreenName** validate gates |

**Ruling (not REAL):** Extra field gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @331 |
|--------|-------------|
| vCard depth | **Partial+** — **TEL;TYPE=IPHONE** pull/export with CELL |
| Multi-book / live | **Partial+** (330+) |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** iPhone TEL alias ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @331 |
|--------|-------------|
| PROPFIND display names | **Partial+** — **LABELS N** on live/manual CalDAV status |
| Lightning-class UX | **Partial+** — live sync status shows **primary cal** name |

**Ruling (not REAL):** Live status chrome ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @331 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-PUSH-CAL** after CK refresh when still dirty |
| GAL visibility | **Partial+** (330+) |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Post-CK push attempt ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @331 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **2000** rows/run |
| Scale | **Partial+** — **14×** backfill @ ≥200k; meta backfill **6000**/run |

**Ruling (not REAL):** 200k scale ≠ Gloda unified index REAL.

## Shipped @331 (code pointers)

- `MorkAbookParser`: JobTitle/NickName/AimScreenName gates  
- `CardDavSyncService`: TEL IPHONE  
- `CalDavCalendarSync.LastBidirectionalSyncCount`; `frmCalendar` CalDAV status hints  
- `EwsBackgroundSync.LastBackgroundCalPushAfterCk`  
- `MailSearchBackgroundIndexer` 2000 / 200k 14× / 6000 meta  
