# Open slice acceptance gaps @ 0.7.2.327

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @327 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **LowercasePrimaryEmail** import fallback rows |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **PreferEmail** field gate on round-trip validate |

**Ruling (not REAL):** LowercasePrimaryEmail parse + PreferEmail gate ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @327 |
|--------|-------------|
| vCard depth | **Partial+** — **X-MOZILLA-SCREENNAME** pull/export |
| Multi-book / live | **Partial+** — **CYCLE** counter on always-on live book |
| 412 / live | **Partial+** — **412@email** hint; contact save **412 MERGED** |

**Ruling (not REAL):** Always-on cycle telemetry ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @327 |
|--------|-------------|
| PROPFIND display names | **Partial+** (326+) |
| Lightning-class UX | **Partial+** — multi-cal **[CAL]** list prefix; **DAV-LABELS** status |

**Ruling (not REAL):** List chrome + label count ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @327 |
|--------|-------------|
| Background calendar | **Partial+** — bg **auto-reconcile** pass when EWS-dirty remains |
| GAL visibility | **Partial+** — **DIR-CK** refresh before GAL push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Directory CK refresh + reconcile pass ≠ Exchange parity REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @327 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **1000** rows/run; **Folder** leaf in FTS corpus |
| Scale | **Partial+** — **10×** backfill burst @ ≥120k missing bodies |

**Ruling (not REAL):** Folder corpus + 120k scale ≠ Gloda unified index REAL.

## Shipped @327 (code pointers)

- `MorkAbookParser`: LowercasePrimaryEmail rows; PreferEmail validate gate  
- `CardDavSyncService`: X-MOZILLA-SCREENNAME; `CardDavLiveBookCache.AlwaysOnCycleCount`  
- `frmCalendar`: multi-cal list prefix; DAV-LABELS  
- `EwsMailService.TryRefreshGalDirectoryChangeKey`; `EwsBackgroundSync` reconcile  
- `MailSearchIndex.ComposeFtsBodyText`; `MailSearchBackgroundIndexer` 1000 / 120k 10×  
