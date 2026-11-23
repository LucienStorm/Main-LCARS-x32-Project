# Open slice acceptance gaps @ 0.7.2.321

**Spec:** `docs/superpowers/specs/2026-10-12-lcars-thunderbird-full-parity.md` (numbered table only)  
**Open count:** **6** (slices 13, 14, 15, 17, 18, 21) — **no REAL promotion**

## Slice 13 — Mork abook

| Bullet | Status @321 |
|--------|-------------|
| Parse additional Mork tables/columns | **Partial** — multitable col-map selection; legacy field encoder breadth |
| LDIF/CardDAV fallback | **Closed @266+** (unchanged) |
| Round-trip export vs Thunderbird `.mab` | **Open** — preserve-source + table-end append; **not** byte-identical TB fidelity |

**Ruling (not REAL):** LCARS-shaped export + self-parse gates ≠ Thunderbird importing exported `.mab` unchanged.

## Slice 14 — CardDAV

| Bullet | Status @321 |
|--------|-------------|
| Read/write vCard | **Partial** — bidirectional PUT/GET, etag, 412 FIX per contact |
| Multi-book Thunderbird accounts | **Partial** — PROPFIND discovery + secondary JSON + per-book DPAPI keys |
| LCARS picker uses **live book** | **Partial** — `CardDavLiveBookCache` always-on timer + bidirectional refresh |

**Ruling (not REAL):** Always-on sync ≠ full Thunderbird CardDAV feature depth (groups, photos, conflict UI parity).

## Slice 15 — CalDAV + Lightning

| Bullet | Status @321 |
|--------|-------------|
| Two-way VEVENT/VTODO | **Partial** — dirty push/pull; VTODO URL discovery |
| Recurrence / detach | **Partial** — EXDATE, COUNT−, UNTIL clamp |
| iMIP / meeting replies | **Partial** — SMTP `METHOD:REPLY` queue + EWS RSVP paths |
| Lightning-class LCARS UI | **Open** |

**Ruling (not REAL):** Basic `frmCalendar` + CalDAV ≠ Lightning add-on UX (multi-cal chrome, task grid parity).

## Slice 17 — Exchange EWS

| Bullet | Status @321 |
|--------|-------------|
| Mail + calendar + contacts | **Partial** — EWS mail/calendar merge; GAL resolve + shadow push |
| Bidirectional calendar conflict-safe | **Partial** — reconcile passes, server/client wins, per-event reconcile on failed push |
| Full GAL directory sync | **Open** |

**Ruling (not REAL):** ResolveNames harvest + persona/directory UpdateItem attempts ≠ enterprise GAL parity.

## Slice 18 — Chat + OTR

| Bullet | Status @321 |
|--------|-------------|
| XMPP/Matrix transport | **Partial REAL** (sub-criterion only) |
| In-process OTR | **Open** — see `open-slice-acceptance-gaps-317.md` §18 |

## Slice 21 — Global search

| Bullet | Status @321 |
|--------|-------------|
| FTS5 cross-folder index | **Partial** — WAL, thread rank, label/tag facets |
| Gloda-style query UX | **Partial** — newer/older post-filter, `-term` negation |
| Full-body backfill at scale | **Partial** — 4× burst @ ≥10k queue |

**Ruling (not REAL):** JSON header map + FTS5 progress ≠ Mozilla Gloda unified search parity.

## Shipped @321 (evidence pointers)

- Mork: `TryInsertMorkRowBeforeTableEnd`, `MorkRowId` on append  
- CardDAV: `EnumerateAddressBookUrls`, `EnsureMissingBookPasswords`  
- CalDAV: `TryClampRecurrenceUntilBeforeInstance`, `CalendarMeetingReplyQueue`  
- EWS: `TryReconcileSingleIfServerMatchesLocal`  
- Gloda: `ApplyDateFacetFilters`, `MapFacetTerm` negation  
