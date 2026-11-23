# Open slice acceptance gaps @ 0.7.2.336

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @336 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **FIELDS n** row-property column telemetry |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** (335+) — full optional-field gate set |

**Ruling (not REAL):** Row-property field telemetry ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @336 |
|--------|-------------|
| vCard depth | **Partial+** — **FAX/PAGER/OTHER,PREF**; **LANG:en** export |
| Multi-book / live | **Partial+** — live **BOOKS n** + **412-CLIENT/SERVER** mode |
| 412 / live | **Partial+** (335+) |

**Ruling (not REAL):** Extended TEL PREF + book/mode hints ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @336 |
|--------|-------------|
| PROPFIND display names | **Partial+** — live **CALS n** discovered calendars (mail) |
| Lightning-class UX | **Partial+** — **PULL n** merge-added telemetry |

**Ruling (not REAL):** PULL/CALS status ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @336 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-CK-2**, **BG-PUSH-CAL-3**, **BG-CAL-5** |
| GAL visibility | **Partial+** — **BG-GAL-6** sixth shadow push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Sixth GAL tier ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @336 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **3000** rows/run; **X-LCARS-MessageSize** in corpus |
| Scale | **Partial+** — **19×** backfill @ ≥300k; meta backfill **11000**/run |

**Ruling (not REAL):** MessageSize corpus + 300k scale ≠ Gloda unified index REAL.

## Shipped @336 (code pointers)

- `MorkAbookParser.LastMorkRowPropertyFieldCount`  
- `CardDavSyncService`: FAX/PAGER/OTHER PREF + LANG  
- `LastPullMergeAdded` / **PULL** + **CALS** UX  
- `EwsBackgroundSync`: CK-2, PUSH-CAL-3, CAL-5, GAL-6  
- `MailSearchBackgroundIndexer` 3000 / 300k 19× / 11000 meta  
