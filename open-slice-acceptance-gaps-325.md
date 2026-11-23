# Open slice acceptance gaps @ 0.7.2.325

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @325 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — `PreferEmail`; **preserve unknown column cells** on row replace |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Open** |

**Ruling (not REAL):** Unknown-cell preservation ≠ Thunderbird `.mab` byte-identical round-trip.

## Slice 14 — CardDAV

| Bullet | Status @325 |
|--------|-------------|
| vCard depth | **Partial+** — `IMPP;TYPE=AIM` on export |
| Multi-book / live | **Partial+** — **2ND+** merge hint in address book; **MorkRowId** persisted in JSON |
| 412 / live | **Partial** (323+) |

**Ruling (not REAL):** IMPP + row-id persistence ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @325 |
|--------|-------------|
| Multi-cal push routing | **Partial+** — push to secondary collection URL by `CalendarName` (mail + utilities) |
| Lightning-class UX | **Open** |

**Ruling (not REAL):** Named-cal push routing ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @325 |
|--------|-------------|
| Background calendar | **Partial+** — **second** full 2-way pass when EWS-dirty remains |
| GAL visibility | **Partial+** — **GAL-DIRTY** count on EWS sync UI |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Retry pass + dirty telemetry ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @325 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — `RebuildFtsHeaderCorpusForAccount` on indexer runs |
| Scale | **Partial+** — 8× backfill burst @ ≥80k missing bodies |

**Ruling (not REAL):** Periodic corpus rebuild ≠ Gloda unified index REAL.

## Shipped @325 (code pointers)

- `MorkAbookParser`: PreferEmail, `AppendPreservedUnknownColumnCells`  
- `AddressBookStore`: MorkRowId JSON, `CountGalPushDirtyContacts`  
- `CardDavSyncService`: IMPP AIM  
- `LcarsCalendarBridge` + `CalDavCalendarSync`: `ResolveCalDavPushUrl` / `ResolveCalendarPushUrl`  
- `EwsBackgroundSync`: conditional second cal 2-way  
- `MailSearchIndex.RebuildFtsHeaderCorpusForAccount`  
