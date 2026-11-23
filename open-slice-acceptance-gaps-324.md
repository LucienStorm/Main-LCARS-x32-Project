# Open slice acceptance gaps @ 0.7.2.324

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @324 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — `PopularityIndex`, `RecordName` parse/export |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Open** |

**Ruling (not REAL):** More TB column coverage still ≠ byte-identical `.mab` fidelity.

## Slice 14 — CardDAV

| Bullet | Status @324 |
|--------|-------------|
| vCard read/write | **Partial+** — parse `X-AIM` → `AimScreenName` |
| Multi-book live merge | **Partial+** — secondary TB books merged on always-on refresh |
| 412 / live book | **Partial** (323+) |

**Ruling (not REAL):** Secondary book merge on timer ≠ full Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @324 |
|--------|-------------|
| Multi-calendar CalDAV | **Partial+** — PROPFIND VEVENT collections; secondary URL pull (mail + utilities) |
| Lightning UI chrome | **Partial+** — status shows **N CALS** when multiple calendar names visible |
| Lightning-class UX | **Open** |

**Ruling (not REAL):** Multi-cal PROPFIND + status hint ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @324 |
|--------|-------------|
| Background calendar | **Partial+** — `TrySyncEwsCalendarBidirectional` in background sync; **5-pass** reconcile |
| GAL push | **Partial+** — second `TryPushGalDirtyPersonalShadows` pass after harvest |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Background 2-way cal + GAL retry ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @324 |
|--------|-------------|
| Header facets | **Partial+** — `inreplyto:`/`references:`/`replyto:`/`list-id:`/`return-path:` → body corpus |
| Meta dates | **Partial+** — 8k backfill on date query; 2k per indexer run |
| Scale | **Partial+** — 7× backfill burst @ ≥60k missing bodies |

**Ruling (not REAL):** Extended header facets ≠ Gloda unified search REAL.

## Shipped @324 (code pointers)

- `MorkAbookParser`: PopularityIndex, RecordName  
- `CardDavSyncService` + `CardDavLiveBookCache`: X-AIM parse, secondary book merge  
- `CalDavSyncService.EnumerateVeventCollectionUrls`; `LcarsCalendarBridge` + `CalDavCalendarSync` secondary pulls  
- `frmCalendar`: multi-cal status  
- `EwsBackgroundSync`: full cal 2-way + GAL retry  
- `MailSearchGlodaFts` / `MailSearchBackgroundIndexer`: header facets, meta + 60k scale  
