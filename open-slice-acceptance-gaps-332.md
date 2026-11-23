# Open slice acceptance gaps @ 0.7.2.332

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @332 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** (330+) |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **Company/Department/WorkPhone/Popularity/RecordName** gates |

**Ruling (not REAL):** Shared validate helper + more field gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @332 |
|--------|-------------|
| vCard depth | **Partial+** — **EMAIL,PREF** export; **PREF** email pick on pull |
| Multi-book / live | **Partial+** (330+) |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** PREF email vCard ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @332 |
|--------|-------------|
| PROPFIND display names | **Partial+** (331+) |
| Lightning-class UX | **Partial+** — live **PUSH n**; **CAL** filter status; mail **primary tag** count |

**Ruling (not REAL):** Push/filter status ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @332 |
|--------|-------------|
| Background calendar | **Partial+** (331+) |
| GAL visibility | **Partial+** — **BG-GAL-2** second push when GAL-dirty remains |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Second GAL push retry ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @332 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **2200** rows/run; **X-LCARS-DateUnix** in corpus |
| Scale | **Partial+** — **15×** backfill @ ≥220k; meta backfill **7000**/run |

**Ruling (not REAL):** DateUnix corpus + 220k scale ≠ Gloda unified index REAL.

## Shipped @332 (code pointers)

- `MorkAbookParser.ValidateOptionalMorkFieldMatch`  
- `CardDavSyncService`: EMAIL PREF + `PickPreferredVCardEmail`  
- `LcarsCalendarBridge.LastCalDavPrimaryEventsTagged`; `CalDavCalendarSync.LastBidirectionalPushCount`  
- `EwsBackgroundSync.LastBackgroundGalPushSecondRetry`  
- `MailSearchBackgroundIndexer` 2200 / 220k 15× / 7000 meta  
