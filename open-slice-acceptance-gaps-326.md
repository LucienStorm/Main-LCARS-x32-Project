# Open slice acceptance gaps @ 0.7.2.326

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @326 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — multitable **PrimaryEmail `(end` insert** targets correct table |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Open** |

**Ruling (not REAL):** Correct-table row append ≠ Thunderbird `.mab` byte-identical round-trip.

## Slice 14 — CardDAV

| Bullet | Status @326 |
|--------|-------------|
| vCard depth | **Partial+** — parse **IMPP;TYPE=AIM** on pull |
| Multi-book / live | **Partial+** (325+) |
| 412 / live | **Partial+** — **412=MODE** hint on address book status |

**Ruling (not REAL):** IMPP pull + 412 policy hint ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @326 |
|--------|-------------|
| Multi-cal push routing | **Closed** @325 |
| PROPFIND display names | **Partial+** — **CalendarUrlLabels** + friendly combo labels |
| Lightning-class UX | **Open** |

**Ruling (not REAL):** DAV displayname in filter combo ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @326 |
|--------|-------------|
| Background calendar | **Partial+** (325+) |
| GAL visibility | **Partial+** — **PERSONA-CK** refresh before GAL push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Persona ChangeKey refresh ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @326 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **800** rows/run; **read/unread** in FTS corpus |
| Scale | **Partial+** — **9×** backfill burst @ ≥100k missing bodies |

**Ruling (not REAL):** Larger corpus + 100k scale ≠ Gloda unified index REAL.

## Shipped @326 (code pointers)

- `MorkAbookParser`: `FindPrimaryEmailTableEndIndex`  
- `CardDavSyncService`: `ExtractImppAimScreenName`  
- `CalDavSyncService.EnumerateVeventCalendarDescriptors`; `LcarsCalendarBridge.CalendarUrlLabels`  
- `CalDavCalendarSync.TryGetCalendarFriendlyLabel`; `frmCalendar` combo labels  
- `EwsMailService.TryRefreshGalPersonaChangeKey`; `EwsLcarsBridge.LastGalPersonaChangeKeyRefreshes`  
- `MailSearchIndex.ComposeFtsBodyText`; `MailSearchBackgroundIndexer` 800 / 100k 9×  
