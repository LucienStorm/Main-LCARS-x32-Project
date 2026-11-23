# Open slice acceptance gaps @ 0.7.2.323

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @323 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — `AimScreenName`; multitable **PrimaryEmail** table pick for new rows |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Open** |

**Ruling (not REAL):** Multitable append targeting ≠ Thunderbird `.mab` byte-identical round-trip.

## Slice 14 — CardDAV

| Bullet | Status @323 |
|--------|-------------|
| vCard read/write depth | **Partial+** — `X-SCREENNAME` / `X-AIM`; parse into `ScreenName` |
| Multi-book discovery | **Partial+** — PROPFIND on address book open (background) |
| 412 conflict path | **Partial+** — per-save `TryResolveCardDav412ConflictForEmail` in contact editor |

**Ruling (not REAL):** vCard extensions + auto 412 retry ≠ Thunderbird CardDAV conflict UI REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @323 |
|--------|-------------|
| VTODO collection reliability | **Partial+** — rediscover + re-fetch when task collection empty |
| Lightning UI / multi-cal chrome | **Open** |

**Ruling (not REAL):** VTODO URL heal on empty pull does not close Lightning-class UX.

## Slice 17 — Exchange EWS

| Bullet | Status @323 |
|--------|-------------|
| GAL shadow push reliability | **Partial+** — pre-push directory ChangeKey refresh (`LastGalChangeKeyRefreshes`) |
| Full GAL + conflict-safe calendar REAL | **Open** |

**Ruling (not REAL):** GAL ChangeKey refresh ≠ enterprise directory sync REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — see `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @323 |
|--------|-------------|
| Header corpus in FTS body | **Partial+** — Message-Id / In-Reply-To / etc. indexed for facet search |
| Facets | **Partial+** — `messageid:` → body; `has:body` SQL filter |
| Dates at scale | **Partial+** — JSON→`mail_fts_meta` backfill (4k) on date facet queries |
| Scale | **Partial+** — 6× backfill burst @ ≥40k missing bodies |

**Ruling (not REAL):** Header-in-body FTS tricks ≠ Gloda unified index REAL.

## Shipped @323 (code pointers)

- `MorkAbookParser`: AimScreenName, `PickPrimaryEmailTableMap`  
- `CardDavSyncService`: X-SCREENNAME/X-AIM vCard  
- `frmAddressBook` / `frmContactEdit`: discovery + 412 on save  
- `LcarsCalendarBridge`: VTODO rediscover on empty pull  
- `EwsLcarsBridge`: GAL ChangeKey refresh before push  
- `MailSearchIndex` / `MailSearchGlodaFts`: header corpus, meta backfill, has:body SQL  
