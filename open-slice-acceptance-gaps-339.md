# Open slice acceptance gaps @ 0.7.2.339

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md`  
**Open count:** **6** (13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @339 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial+** (338+) |
| LDIF/CardDAV fallback | **Closed** |
| Round-trip export vs Thunderbird `.mab` | **Partial+** — preserve-export keeps source column cells when LCARS omits empty known fields |

**Ruling (not REAL):** Preserve-on-empty export ≠ byte-identical Thunderbird `.mab` REAL.

## Slice 14 — CardDAV

| Bullet | Status @339 |
|--------|-------------|
| vCard depth | **Partial+** (338+) |
| Multi-book / live | **Partial+** (338+) |
| 412 / live | **Partial+** — **412 auto-merge** fills all blank vCard fields from remote before retry PUT |

**Ruling (not REAL):** Deep 412 blank-merge ≠ Thunderbird CardDAV conflict UX REAL.

## Slice 15 — CalDAV + Lightning

| Bullet | Status @339 |
|--------|-------------|
| PROPFIND display names | **Partial+** (337+) |
| Lightning-class UX | **Partial+** — **RECUR** recurring-series filter view (Lightning-style slice) |

**Ruling (not REAL):** Recurring filter view ≠ Lightning add-on REAL.

## Slice 17 — Exchange EWS

| Bullet | Status @339 |
|--------|-------------|
| Background calendar | **Partial+** (338 telemetry unchanged — no new BG-CAL tier @339) |
| GAL visibility | **Partial+** (338 — no new BG-GAL tier @339) |
| Conflict-safe calendar reconcile | **Partial+** — **FieldsMatchServer** includes category + reminder minutes |

**Ruling (not REAL):** Richer unchanged-dirty match ≠ full Exchange conflict-safe REAL.

## Slice 18 — Chat + OTR

**Unchanged Open** — `open-slice-acceptance-gaps-317.md` §18.

## Slice 21 — Global search

| Bullet | Status @339 |
|--------|-------------|
| Header corpus rebuild | **Partial+** (338 — **3400**/run unchanged @339) |
| Thread discovery | **Partial+** — Gloda **`thread:`** / **`threadroot:`** facet (Message-Id / In-Reply-To / References / computed root) |
| Scale | **Partial+** (338 — meta/corpus counters unchanged @339) |

**Ruling (not REAL):** Thread facet query ≠ Gloda unified index REAL.

## Shipped @339 (code pointers)

- `MorkAbookParser.AppendPreservedUnknownColumnCells` — retain source cells not re-emitted on preserve export  
- `CardDavSyncService.MergeContactForConflict` — full-field blank merge on 412  
- `frmCalendar` **RECUR** toggle + recurring-series list filter  
- `EwsCalendarConflictHelper.FieldsMatchServer` — category + alarm minutes  
- `MailSearchIndex` — `thread:` / `threadroot:` filters  

**Not shipped @339:** No corpus/meta backfill bump; no BG-GAL-N / BG-CAL-N counter tiers.
