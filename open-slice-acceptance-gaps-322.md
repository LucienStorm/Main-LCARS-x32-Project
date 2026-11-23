# Open slice acceptance gaps @ 0.7.2.322

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @322 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — `ScreenName` parse/export; `LowercasePrimaryEmail` on append rows |
| LDIF/CardDAV fallback | **Closed** (unchanged) |
| Round-trip export vs Thunderbird `.mab` | **Open** |

**Ruling (not REAL):** Extra TB column coverage does not close byte-identical `.mab` fidelity.

## Slice 14 — CardDAV

| Bullet | Status @322 |
|--------|-------------|
| Read/write vCard + 412 handling | **Partial+** — batch `TryResolveAllCardDav412DirtyContacts`; always-on PROPFIND book refresh (1h) |
| Multi-book | **Partial** — discovery on live cache refresh |
| Live book in picker | **Partial+** — always-on timer + 412 batch on refresh |

**Ruling (not REAL):** Automated 412 retry ≠ Thunderbird conflict UX parity.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @322 |
|--------|-------------|
| VTODO collection | **Partial+** — `TryEnsureTaskCollectionUrlDiscovered` on bidirectional sync |
| Lightning UI / multi-cal chrome | **Open** |

**Ruling (not REAL):** Task URL discovery on sync does not close Lightning-class UX.

## Slice 17 — Exchange EWS

| Bullet | Status @322 |
|--------|-------------|
| Calendar push reliability | **Partial+** — pre-push ChangeKey refresh; 4 reconcile/push passes |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** ChangeKey refresh reduces stale pushes; not full Exchange calendar REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — OTR sub-criterion only (`open-slice-acceptance-gaps-317.md`).

## Slice 21 — Global search

| Bullet | Status @322 |
|--------|-------------|
| FTS schema depth | **Partial+** — `to_line`/`cc_line` columns + v322 migration; `mail_fts_meta.date_unix` |
| Facets | **Partial+** — `to:`/`cc:` FTS facets; SQL newer/older via meta join + JSON fallback |
| Scale | **Partial+** — 5× body backfill burst @ ≥20k queue; CORPUS status in Gloda search bar |

**Ruling (not REAL):** FTS5 + meta dates ≠ Gloda unified index parity at Thunderbird scale.

## Shipped @322 (code pointers)

- `MorkAbookParser`: ScreenName, LowercasePrimaryEmail  
- `CardDavLiveBookCache`: hourly PROPFIND books, 412 batch  
- `AddressBookStore.TryResolveAllCardDav412DirtyContacts`  
- `EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents`  
- `MailSearchGlodaFts`: meta table, to/cc columns, corpus stats  
- `MailSearchBackgroundIndexer`: 5× @ 20k missing bodies  
