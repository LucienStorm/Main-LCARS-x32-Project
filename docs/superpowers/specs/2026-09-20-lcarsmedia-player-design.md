# LCARSmedia — Full Media Player (from Photo Viewer)

**Date:** 2026-09-20  
**Status:** Approved for documentation (Approach 1; chrome section validated; remaining sections captured from locked decisions)  
**Project:** LCARS x32 (`Main-LCARS-x32-Project`)  
**Source app today:** `LCARSpic` → `LCARSpic.exe` (“Photo Viewer”)

## Summary

Evolve the Photo Viewer into **LCARSmedia**: a single LCARS app that plays **one** local media item at a time (photo, music, or video). The chrome **reshapes** (elbows/borders and controls) based on the active media type, with short **leave/enter** button animations. Playback uses **LibVLC** for broad format coverage (VLC replacement intent).

**Phase 1:** Local Photo / Music / Video + layout animation + rename/ship as `LCARSmedia.exe` + a **shell media-control API hook** (no strip UI yet).  
**Next slice:** Shell control strip in the weather/clock/battery area.  
**Phase 2:** Internet radio, music streaming, online photo galleries; video streaming after Win10 security research.

MY MUSIC / MY VIDEOS / MY PICTURES remain **folder launches into LCARSexplorer**. Only **PHOTO VIEWER** (and Browse inside the media app) opens LCARSmedia.

---

## Goals

1. Replace day-to-day VLC use for local files with an LCARS-native player.
2. One app, one media at a time — layout and controls follow content type (not a manual “mode picker”).
3. Photo capabilities retained and improved (slideshow settings, LCARS Browse).
4. Music and video local playback with comprehensive codecs via LibVLC.
5. Right-rail chrome cleaned up: shrink NAV D-pad, vertical stack, CLOSE under NAV.
6. Design for background playback + shell HUD without implementing the HUD strip in Phase 1.

## Non-goals (Phase 1)

- Changing MY MUSIC / MY VIDEOS / MY PICTURES Start Menu behavior
- Internet radio, Spotify/etc., online galleries, Netflix-style streaming
- Playlists / multi-track queue (single file / single slideshow folder set only unless already implied by photo folder browse)
- Embedding the player inside the mainscreen form
- Klingon mode or other theme takeovers
- Replacing Windows default file associations system-wide (optional later)

---

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Product shape | Evolve `LCARSpic` → `LCARSmedia` (Approach 1) |
| Launch | `myPhoto` → `LCARSmedia.exe`; folder buttons stay explorer |
| Modes | Auto by media type; not three simultaneous streams |
| Engine | LibVLC (max format coverage) |
| Chrome | Shrink NAV circle to BROWSE width; vertical right stack; CLOSE under NAV |
| Browse | In-app LCARS file browser popup |
| Background HUD | Shell strip near weather/clock/battery — **API designed in Phase 1, UI next slice** |
| Streaming | Phase 2 |
| Animation | Content elbows morph; controls leave/enter on type change |

---

## Feature — Chrome & layout

### Identity

- Rename project/assembly/exe: **LCARSmedia** / `LCARSmedia.exe`
- Window title: e.g. `LCARS Media` (or keep “Photo Viewer” until label pass — prefer **LCARS Media**)
- Installer / update package / `modBusiness.myPhoto_Click`: point at `LCARSmedia.exe`
- Keep launching from Start Menu control `myPhoto` (button text may remain “PHOTO VIEWER” in Phase 1)

### Right rail (vertical stack, right-justified)

Target order (top → bottom), no decorative spacers:

1. **Primary actions** (set depends on media type)  
   - Photo: `BROWSE`, slideshow start/stop (and later settings entry)  
   - Music: play/pause, stop, BROWSE (and room for future radio)  
   - Video: play/pause, stop, BROWSE  
2. **Photo-only:** zoom row (`−` / `FULL` / `+`) + ZOOM pie/readout  
3. **NAV** D-pad cluster — **diameter = width of BROWSE** (shrink from current oversized circle); pan/prev-next semantics as today for photos; for video/music, NAV may dim or map to seek/skip if useful (Phase 1: keep photo pan; music/video can hide or no-op pan)  
4. **CLOSE** immediately under NAV (shell-aligned CLOSE pill; eliminate tablet **X** leftover)

### Content stage

- Single stage: `PictureBox` **or** LibVLC video surface **or** music “now playing” panel (metadata / simple visualization)  
- Never show photo + video + music UI as concurrent players  
- Elbows / border geometry **animate** to fit content (e.g. widen for landscape video, compact panel for audio)

### Browse

- In-app LCARS browse dialog (reuse `LCARSfileBrowseDialog` / explorer patterns already used elsewhere)  
- Filters: images for photo context; audio extensions for music; video extensions for video; or “all supported media” from an idle state  
- Selecting a file loads it and triggers layout transition for that type

---

## Feature — Local playback (Phase 1)

### Photo

Retain and extend current behavior:

- Folder image list: jpg/jpeg/gif/bmp/png/tif/tiff (keep existing set; add common formats only if trivial)  
- Prev/next, zoom, pan, rotate (fix rotate if currently broken — both arrows call same flip today; Phase 1 may leave or fix as small bugfix)  
- Slideshow with **settings**: interval, shuffle on/off, loop on/off (minimal settings panel or LCARS dialog)  
- CLI: optional path argument opens that file/folder

### Music

- LibVLC audio playback for local files (mp3, flac, wav, m4a, ogg, etc. — whatever LibVLC ships)  
- Transport: play/pause, stop, seek bar if feasible in Phase 1  
- Now-playing UI: title/artist from tags when available; otherwise filename  
- Continues playing if window is minimized/obscured (process stays alive) — prerequisite for future shell strip

### Video

- LibVLC video into a WinForms panel (x86 build; ship `libvlc` / plugins with update package)  
- Transport: play/pause, stop, seek  
- Aspect-aware stage sizing with elbow morph  
- Audio through same LibVLC instance

### Engine notes

- Prefer official **VideoLAN LibVLC** native binaries for **win32 (x86)** to match tablet  
- Managed wrapper: e.g. LibVLCSharp or a thin P/Invoke host — choose based on .NET Framework 4.x / VB friendliness during implementation; document choice in the plan  
- Package: `Package-LCARSUpdate.ps1` / `Build-LCARS.ps1` must copy `LCARSmedia.exe` + required LibVLC DLLs/plugins  
- License: LGPL — keep LibVLC as dynamic link / ship notices as required (implementation plan includes a short compliance note)

---

## Feature — Animation & look

### Type-change transition

When media type changes (e.g. photo → video):

1. Current type-specific controls **animate out** (slide/fade off the rail)  
2. Elbows/borders **tween** toward the target geometry for the new type  
3. New type-specific controls **animate in**

Keep motion short (roughly 200–400ms), LCARS-like, not bounce-heavy. This is the first deliberate “controls leave / enter” pattern to reuse later on other menus.

### Idle / empty state

No file loaded: compact stage + BROWSE + CLOSE (+ optional PHOTO/MUSIC/VIDEO hints only if needed — prefer a single BROWSE that accepts all media).

---

## Feature — Shell media strip (designed now, built next)

### Intent

When `LCARSmedia` is playing music/video and the user switches to explorer/browser, show a **small control strip in main LCARS chrome** near weather/clock/battery: title, play/pause, stop, maybe skip, tap-to-focus player.

### Phase 1 deliverable (hook only)

Define a small IPC contract so the strip can be added without rewriting the player:

- **Transport:** WM_COPYDATA and/or named pipe / localhost UDP — pick one in implementation plan; prefer existing LCARS patterns (e.g. OSK-style messages) if present  
- **Messages (conceptual):** `MediaStateChanged` (type, title, playing/paused, position), `MediaCommand` (play/pause/stop/show)  
- **Shell:** `modBusiness` / mainscreen hosts a hidden or empty placeholder region reserved for the strip (optional visible stub “off” until next slice)  
- **Player:** emits state while playing; listens for commands even when not focused

### Explicitly not Phase 1 UI

Do not implement the weather-row strip visuals until the follow-up slice after Phase 1 playback is solid.

---

## Phase 2 (back burner)

| Area | Notes |
|------|--------|
| Internet radio | Provider list + LibVLC stream URLs; auth as needed |
| Music streaming | Research APIs / ToS; likely external browser or OAuth — security & store policies |
| Online photo galleries | Similar; LCARS gallery browser |
| Video streaming | Win10 / SmartScreen / HTTPS / DRM research before committing |

---

## Architecture

```
Start Menu myPhoto ──► LCARSmedia.exe [optional path]
MY MUSIC/VIDEOS/PICTURES ──► LCARSexplorer (unchanged)

LCARSmedia
  ├─ Content detector (ext / LibVLC probe)
  ├─ Photo pipeline (GDI+ / existing)
  ├─ LibVLC host (audio + video)
  ├─ Chrome controller (rail sets + elbow morph + transitions)
  └─ MediaSession IPC (state out / commands in) ──► (future) shell strip
```

### Primary touch points

| Area | Likely files |
|------|----------------|
| Rename / project | `LCARSpic\` → `LCARSmedia\`, vbproj, AssemblyInfo, Application |
| Main UI | `frmPic.vb` / designer (or rename `frmMedia`) |
| Launch / package | `modBusiness.vb`, `Build-LCARS.ps1`, `Package-LCARSUpdate.ps1`, installer lists |
| LibVLC | new `lib\` or `vlc\` under media project; package copy rules |
| Shell hook | small module in main + emitter in media app |

### Tablet X vs CLOSE

Current source uses `sbExit` CLOSE with shell alignment; tablet still shows **X** from an older build. Phase 1 ship must include updated `LCARSmedia.exe` so CLOSE under NAV is what tablets get.

---

## Testing (Phase 1)

- Open from PHOTO VIEWER; CLOSE under shrunk NAV; rail width consistent  
- Browse image → photo chrome; slideshow settings work  
- Browse audio → music chrome + playback; video → video chrome + playback  
- Type switch animates controls + elbows  
- LibVLC plays common formats the tablet uses today in VLC  
- Minimized player continues audio  
- Update package installs exe + LibVLC deps; CustomVersion bumps  
- MY MUSIC/VIDEOS/PICTURES still open explorer only  

## Delivery

Build, package (`-Bump`), upload per tablet-update rule after Phase 1 lands.

---

## Open follow-ups

1. Shell media strip UI (next slice; contract above)  
2. Phase 2 streaming / radio / galleries  
3. Optional Start Menu label rename PHOTO VIEWER → MEDIA  
4. File-association “Open with LCARSmedia”  
5. Playlist / queue (if ever desired — currently out of scope)
