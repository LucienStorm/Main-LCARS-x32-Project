# Open slice acceptance gaps @ 0.7.2.328

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @328 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **CellularNumber** parse → `MobilePhone` |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **SecondEmail** + **MorkRowId** validate gates |

**Ruling (not REAL):** CellularNumber + extra field gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @328 |
|--------|-------------|
| vCard depth | **Partial+** — **ROLE** pull/export with **TITLE** |
| Multi-book / live | **Partial+** — always-on summary **2ND+** secondary merge count |
| 412 / live | **Partial+** (327+) |

**Ruling (not REAL):** ROLE vCard + 2ND+ telemetry ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @328 |
|--------|-------------|
| PROPFIND display names | **Partial+** — **primary** collection displayname + `PrimaryCalendarDisplayName` |
| Lightning-class UX | **Partial+** — **DEFAULT** events use primary friendly label |

**Ruling (not REAL):** Primary cal PROPFIND label ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @328 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-RECON** counter on background auto-reconcile |
| GAL visibility | **Partial+** (327+) |
| Full GAL + conflict-safe REAL | **Partial+** — **RECON-CLEARED** on cal 2-way passes |

**Ruling (not REAL):** Reconcile telemetry ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @328 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **1200** rows/run; **Size** in FTS corpus |
| Scale | **Partial+** — **11×** backfill burst @ ≥140k; meta backfill **3000**/run |

**Ruling (not REAL):** Size corpus + 140k scale ≠ Gloda unified index REAL.

## Shipped @328 (code pointers)

- `MorkAbookParser`: CellularNumber; SecondEmail/MorkRowId gates  
- `CardDavSyncService`: ROLE; `CardDavLiveBookCache` 2ND+  
- `CalDavSyncService.TryGetCollectionDisplayName`; primary label in settings  
- `EwsBackgroundSync.LastBackgroundCalAutoReconcile`; `LastCalendarAutoReconcileCleared`  
- `MailSearchBackgroundIndexer` 1200 / 140k 11× / 3000 meta  
