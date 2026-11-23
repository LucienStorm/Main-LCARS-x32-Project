# Open slice acceptance gaps @ 0.7.2.335

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @335 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** — **COLS n** column-key telemetry on parse |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — **Display/First/Last/Middle/Office/Assistant** + lowercase email gate |

**Ruling (not REAL):** Column telemetry + name gates ≠ byte-identical `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @335 |
|--------|-------------|
| vCard depth | **Partial+** — **HOME,PREF** TEL export/pull when sole phone |
| Multi-book / live | **Partial+** — live **2ND+** / **412-FIX** hints on mail status |
| 412 / live | **Partial+** (334+) |

**Ruling (not REAL):** HOME PREF + live book hints ≠ Thunderbird CardDAV REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @335 |
|--------|-------------|
| PROPFIND display names | **Partial+** (331+) |
| Lightning-class UX | **Partial+** — **TASK-TAG n** on VTODO collection pulls |

**Ruling (not REAL):** Task calendar TAG ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @335 |
|--------|-------------|
| Background calendar | **Partial+** — **BG-CAL-4** + **BG-RECON-2** after push tier |
| GAL visibility | **Partial+** — **BG-GAL-5** fifth shadow push |
| Full GAL + conflict-safe REAL | **Open** |

**Ruling (not REAL):** Fifth GAL tier ≠ Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @335 |
|--------|-------------|
| Header corpus rebuild | **Partial+** — **2800** rows/run; **X-LCARS-Account** in corpus |
| Scale | **Partial+** — **18×** backfill @ ≥280k; meta backfill **10000**/run |

**Ruling (not REAL):** Account corpus + 280k scale ≠ Gloda unified index REAL.

## Shipped @335 (code pointers)

- `MorkAbookParser.LastMorkColumnKeyCount`; name/office/assistant gates  
- `CardDavSyncService`: HOME PREF TEL; mail **2ND+** / **412-FIX**  
- `LastTaskEventsTagged` / **TASK-TAG** (mail + LCARSCalendar)  
- `EwsBackgroundSync`: fourth cal pass, second reconcile, fifth GAL push  
- `MailSearchBackgroundIndexer` 2800 / 280k 18× / 10000 meta  
