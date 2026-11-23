# Open slice acceptance gaps @ 0.7.2.330

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @330 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **MiddleName/OfficeLocation/OtherPhone/AssistantName** parse |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **ScreenName** validate gate |

**Ruling (not REAL):** TB office/assistant columns + ScreenName gate ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @330 |
|--------|-------------|
| vCard depth | **Partial+** — **PRODID**; **CATEGORIES** ↔ Custom1 |
| Multi-book / live | **Partial+** — **412-CLIENT** hint when ClientMerge policy |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** CATEGORIES + PRODID ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @330 |
|--------|-------------|
| PROPFIND display names | **Partial+** (328+) |
| Lightning-class UX | **Partial+** — utilities **primary pull** CalendarName; **CAT** filter status |

**Ruling (not REAL):** Category filter chrome ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @330 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-CK** refresh when EWS-dirty remains |
| GAL visibility | **Partial+** — **BG-PUSH** GAL retry telemetry |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Background CK refresh + GAL retry hint ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @330 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **1800** rows/run; **Date** in FTS corpus |
| Scale | **Partial+** — **13×** backfill @ ≥180k; meta backfill **5000**/run |

**Ruling (not REAL):** Date corpus + 180k scale ≠ Gloda unified index REAL.

## Shipped @330 (code pointers)

- `MorkAbookParser`: MiddleName/Office/Other/Assistant; ScreenName gate  
- `CardDavSyncService`: PRODID, CATEGORIES; `CardDavLiveBookCache` 412-CLIENT  
- `CalDavCalendarSync`: primary label on pull; `frmCalendar` CAT status  
- `EwsBackgroundSync`: BG-CK + BG-PUSH counters  
- `MailSearchIndex.ComposeFtsBodyText`; `MailSearchBackgroundIndexer` 1800 / 180k 13×  
