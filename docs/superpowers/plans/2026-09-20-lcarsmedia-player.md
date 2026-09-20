# LCARSmedia Player Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve `LCARSpic` into `LCARSmedia.exe` — local Photo/Music/Video player with LibVLC, content-driven chrome, NAV/CLOSE rail cleanup, type-change animations, and a shell media IPC hook (strip UI later).

**Architecture:** Single WinForms app; GDI+ for images; LibVLC (x86) for audio/video; chrome controller swaps control sets and morphs elbows; `MediaSession` IPC for future shell strip.

**Tech Stack:** VB.NET / .NET Framework 4.x, LCARS controls, LibVLC + LibVLCSharp (or thin host), existing LCARS file browse dialog.

**Spec:** `docs/superpowers/specs/2026-09-20-lcarsmedia-player-design.md`

## Global Constraints

- MY MUSIC / MY VIDEOS / MY PICTURES stay explorer launches — do not retarget them to the player.
- Tablet x86: ship win32 LibVLC natives; update package must include them.
- Restore-original rule: do not invent mainscreen chrome; only change media app + launch path / package lists.
- Always package+upload after shipping builds unless user says not to.
- Phase 1 does **not** build the weather-row strip UI — only the IPC contract + emitter/listener stubs.

## File map

| Path | Role |
|------|------|
| `LCARSpic\` (project; output `LCARSmedia.exe`) | Media app (rename assembly; folder rename optional) |
| `LCARSpic\frmPic.vb` (+ designer) | Main UI / chrome / photo |
| `LCARSpic\Media\MediaKind.vb` | Photo/Music/Video enum + extension detect |
| `LCARSpic\Media\VlcPlaybackHost.vb` | LibVLC wrap |
| `LCARSpic\Media\ChromeController.vb` | Rail layout + transitions |
| `LCARSpic\Media\MediaSessionIpc.vb` | State/command hook for shell |
| `LCARSmain\...\modBusiness.vb` | Launch `LCARSmedia.exe` |
| `tools\Build-LCARS.ps1`, `Package-LCARSUpdate.ps1` | Copy exe + VLC deps |
| Installer lists | `LCARSmedia` instead of/in addition to `LCARSpic` |

---

### Task 1: Right-rail chrome (NAV shrink, stack, CLOSE under)

**Files:** `frmPic.designer.vb`, `frmPic.vb`

- [x] Measure `sbBrowse.Width`; set NAV cluster (`panel1`/`Panel2` D-pad) diameter/size to that width; scale children proportionally.
- [x] Stack vertically, right-aligned: BROWSE → slideshow → zoom row → NAV → CLOSE (`sbExit`).
- [x] Override `OnShellChromeLayout` to place CLOSE under NAV (not overlapping); remove reliance on misaligned X layout.
- [x] Build `LCARSpic`; run and confirm rail matches screenshot intent.
- [x] Commit: `Fix LCARSmedia (LCARSpic) right rail: shrink NAV, stack controls, CLOSE under.`

---

### Task 2: Output rename to LCARSmedia.exe + launch/package

**Files:** `LCARSpic.vbproj` (`AssemblyName`/`RootNamespace` carefully), `AssemblyInfo`, `modBusiness.vb`, Build/Package scripts, `Installing.vb`, solution entries as needed.

- [x] Set output exe to `LCARSmedia.exe` (keep project folder `LCARSpic` for less churn, or rename folder if clean).
- [x] `myPhoto_Click` → `LCARSmedia.exe`.
- [x] Package/Build copy `LCARSmedia.exe`; keep `LCARSpic.exe` copy as optional alias **or** replace — prefer replace + one release note.
- [x] Build + smoke launch from main.
- [x] Commit: `Rename Photo Viewer output to LCARSmedia.exe and update launch/package paths.`

---

### Task 3: MediaKind detection + stage host skeleton

**Files:** new `Media\MediaKind.vb`; `frmPic.vb`

- [x] `DetectMediaKind(path) As MediaKind` by extension (images / audio / video lists).
- [x] Add `Panel` for VLC video; keep `picturebox1` for photos; add simple music panel (labels).
- [x] `LoadMedia(path)` switches visible stage; stops prior VLC if any.
- [x] BROWSE uses LCARS dialog; on OK call `LoadMedia`.
- [x] Commit: `Add media kind detection and photo/audio/video stage hosts.`

---

### Task 4: LibVLC x86 integration

**Files:** `VlcPlaybackHost.vb`, vbproj refs, `lib\vlc\` or NuGet restore into output, package scripts.

- [x] Add LibVLCSharp + VideoLAN.LibVLC.Windows (x86) or vendored natives under `LCARSpic\lib\vlc\`.
- [x] `VlcPlaybackHost`: Play/Pause/Stop/Seek, attach to panel HWND, dispose cleanly.
- [x] Wire Music/Video `LoadMedia` through host; photo stays GDI+.
- [x] Package copies `libvlc.dll`, `libvlccore.dll`, `plugins\**` next to exe (or documented relative path).
- [x] Manual test: mp3 + mp4 on this PC.
- [x] Commit: `Integrate LibVLC for local audio and video playback.`

---

### Task 5: Content-driven chrome + leave/enter animation

**Files:** `ChromeController.vb`, `frmPic.vb`

- [x] Define control sets: PhotoControls, MusicControls, VideoControls.
- [x] On kind change: animate old set out (Left/Opacity or Top slide ~250ms), morph elbow bounds toward target, animate new set in.
- [x] Idle: BROWSE + CLOSE only (or BROWSE accepting all types).
- [x] Commit: `Animate chrome transitions when media type changes.`

---

### Task 6: Photo slideshow settings

**Files:** `frmPic.vb`, small settings dialog or inline LCARS panel

- [x] Settings: interval (seconds), loop, shuffle.
- [x] Persist via `GetSetting`/`SaveSetting` under `LCARS x32` / `LCARSmedia`.
- [x] Commit: `Add slideshow settings for photo mode.`

---

### Task 7: MediaSession IPC hook (no strip UI)

**Files:** `MediaSessionIpc.vb` in media app; stub receiver module in `LCARSmain` (can no-op UI)

- [x] Broadcast state: kind, title, playing, position (WM_COPYDATA or named event+file — pick simplest matching OSK patterns).
- [x] Accept commands: PlayPause, Stop, ShowWindow.
- [x] Document message IDs in code comments for strip slice.
- [x] Commit: `Add MediaSession IPC hook for future shell media strip.`

---

### Task 8: Build, package, upload

- [x] Full `Build-LCARS.ps1 -Configuration Debug`
- [x] `Package-LCARSUpdate.ps1 -Bump` including LibVLC files
- [x] `Upload-LCARSUpdate.ps1`
- [x] Verify CustomVersion lists `LCARSmedia.exe` (+ vlc deps if separate lines)

---

## Plan self-review

| Spec item | Task |
|-----------|------|
| NAV shrink + stack + CLOSE | 1 |
| LCARSmedia.exe rename/launch | 2 |
| Auto layout by media type | 3, 5 |
| LibVLC local A/V | 4 |
| Photo + slideshow settings | 6 (photo base in 1/3) |
| Browse in-app | 3 |
| Shell strip API only | 7 |
| Package/upload | 8 |
| Streaming Phase 2 | deferred |
| Folder Start Menu unchanged | enforced in Global Constraints |

After Task 8, stop for user tablet test before shell-strip UI slice.
