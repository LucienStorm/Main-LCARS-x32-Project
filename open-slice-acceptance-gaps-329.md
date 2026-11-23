# Open slice acceptance gaps @ 0.7.2.329

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @329 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **WorkAddress/HomeAddress/BirthDay/Anniversary** parse |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **CellularNumber** gate; multitable `(end` **fallback** |

**Ruling (not REAL):** Address/date columns + gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @329 |
|--------|-------------|
| vCard depth | **Partial+** — **REV** export; REV hint on pull when NOTE empty |
| Multi-book / live | **Partial+** — **412-SERVER** hint when ServerWins policy |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** REV vCard + policy hint ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @329 |
|--------|-------------|
| PROPFIND display names | **Partial+** (328+) |
| Lightning-class UX | **Partial+** — primary cal name on **primary pull** + month title chrome |

**Ruling (not REAL):** Primary pull labeling ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @329 |
|--------|-------------|
| Background calendar | **Partial+** — **3rd** cal 2-way attempt when reconcile leaves dirty |
| GAL visibility | **Partial+** — **FindPeople** persona/dir CK refresh for all GAL-dirty |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Extra bg cal pass + CK refresh ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @329 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **1500** rows/run; **Subject/From/To/Cc** in FTS corpus |
| Scale | **Partial+** — **12×** backfill @ ≥160k; meta backfill **4000**/run |

**Ruling (not REAL):** Header lines in corpus + 160k scale ≠ Gloda unified index REAL.

## Shipped @329 (code pointers)

- `MorkAbookParser`: Work/Home/BirthDay/Anniversary; table-end fallback; mobile gate  
- `CardDavSyncService`: REV; `CardDavLiveBookCache` 412-SERVER  
- `LcarsCalendarBridge`: primary event CalendarName on pull  
- `EwsLcarsBridge`: GAL CK refresh without id gate; `EwsBackgroundSync` 3rd cal pass  
- `MailSearchIndex.ComposeFtsBodyText`; `MailSearchBackgroundIndexer` 1500 / 160k 12×  
