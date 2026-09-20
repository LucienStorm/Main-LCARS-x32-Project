# Network Places + Remappable Media Folders

**Date:** 2026-09-20  
**Status:** Approved (user waived post-write review)  
**Project:** LCARS x32 (`Main-LCARS-x32-Project`)

## Summary

Finish the Start Menu **NETWORK PLACES** stub by opening `LCARSexplorer` on a dedicated network root (mapped drives + LAN SMB hosts). Separately, add Settings under **SCREEN-SPECIFIC** to remappoint and rename the four media Start Menu buttons (Documents, Pictures, Music, Videos). Settings values are **global** for the shell.

## Goals

1. Network Places feels like the LCARS file explorer, not a separate product.
2. First screen shows **mapped network drives** and **discovered LAN hosts**; drill into SMB shares with normal folder browsing.
3. Credentials: try Windows session + saved Credential Manager entries first; LCARS prompt only on access failure; optional remember.
4. Remap path and button label for Documents / Pictures / Music / Videos only (not Desktop).
5. Folder settings UI lives under SCREEN-SPECIFIC; stored values are not per-monitor.

## Non-goals

- Per-monitor folder paths or labels
- Remapping Desktop or renaming Network Places
- Full Windows NetHood / shell-namespace browsing
- AD/DFS tree browsers
- Changing the language-file system (blank custom label = leave Designer / `.lng` text alone)
- RDP/VNC discovery inside explorer (Terminal keeps that)

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Architecture | Extend `LCARSexplorer` with reserved root `NETWORK:` |
| Root listing | Mapped network drives + LAN hosts with SMB (445/139) |
| Auth | On-demand (try → prompt on failure → optional save) |
| Settings placement | SCREEN-SPECIFIC tab area; values global |
| Scope of remap | Four buttons only: Documents, Pictures, Music, Videos |
| Ship | One tablet update with both features |

---

## Feature A — Network Places

### Entry points

1. **Start Menu (View 1):** `fbMyNetwork` — set `Lit = True`, `Clickable = True`. Click launches:

   `LCARSexplorer.exe` with argument `NETWORK:`

   Same launch pattern as `myDocuments_Click` / `myCompButton_Click` in `modBusiness.vb`.

2. **LCARS file browse dialog:** `sbNetwork` in `LCARSfileBrowseDialog` — replace “not yet available” MsgBox with opening the same network root (in-process navigation to network root, or spawn explorer with `NETWORK:` as appropriate for that dialog’s model).

### Explorer root behavior (`NETWORK:`)

When `frmMyComp` starts with `Command()` equal to `NETWORK:` (case-insensitive), or when navigating “up” into the network root:

**Section 1 — Mapped network drives**

- Enumerate `DriveInfo.GetDrives()`.
- Include drives where `DriveType = Network` (and any other clearly remote mapped volumes the API exposes).
- Display letter + volume label when available.
- Selecting a drive calls existing `loadDir("X:\")` (or the drive root path).

**Section 2 — On this network**

- Background scan of the local IPv4 subnet for hosts with TCP **445** and/or **139** open (SMB).
- Pattern: same family as Terminal `RemoteNetworkDiscovery`, but ports are file-share ports only; keep implementation in explorer (or a small shared helper if duplication becomes painful).
- UI: list of host display names (DNS short name when resolvable, else IP) with a **SCAN** control to rescan.
- Selecting a host lists SMB shares for that host (e.g. `NetShareEnum` / equivalent Win32). Hide administrative shares by default (`C$`, `ADMIN$`, `IPC$`) unless we later add a “show admin shares” toggle (default: hide).
- Selecting a share calls `loadDir("\\host\share")` and continues with existing directory listing.

**Up-directory**

- From `\\host\share\…` → parent UNC / host share list / host list as appropriate.
- From network root, Up does **not** jump to My Computer unless the user uses GO TO / My Computer.

**Recent UNC (optional, deferrable)**

- If cheap: remember last few successfully opened UNC roots. Otherwise omit from v1.

### Authentication

1. Attempt list/open with current Windows identity.
2. Also try Credential Manager entries stored for that target (host or UNC) if present.
3. On access denied / auth failure: show LCARS username/password dialog (reuse Terminal remote-cred UI patterns where practical).
4. Optional **Remember** → save via Credential Manager; retry once.
5. If still failing: LCARS error; do not loop prompts endlessly.

### Errors / empty states

- No mapped drives and empty discovery → clear “none found” messaging; SCAN remains available.
- Host unreachable → status/MsgBox; stay on list.
- Share list empty → message on that host view.

---

## Feature B — Remappable media folders

### Settings UI

Under **SCREEN-SPECIFIC** in `frmSettings`, add a **Folders** sub-panel (sibling navigation style to Wallpaper / Main Screen / Language).

For each of Documents, Pictures, Music, Videos:

| Control | Behavior |
|---------|----------|
| Button name | Text; blank = do not override label |
| Path | Text; local path, other drive, or UNC |
| Browse… | Folder picker |
| Reset | Clears custom path (and optionally label) back to Windows default path / non-override label |

Save applies immediately to running Start Menu buttons where practical, or on next screen refresh.

### Persistence

VB `GetSetting` / `SaveSetting` under `LCARS x32` / `Application`:

| Key | Purpose |
|-----|---------|
| `DocumentsPath` | Custom path or empty |
| `PicturesPath` | Custom path or empty |
| `MusicPath` | Custom path or empty |
| `VideosPath` | Already partially used by `GetMyVideosPath()` — unify |
| `DocumentsLabel` | Custom button text or empty |
| `PicturesLabel` | Custom button text or empty |
| `MusicLabel` | Custom button text or empty |
| `VideosLabel` | Custom button text or empty |

Keys are **global** (not indexed by screen).

### Resolution helpers (main)

Extend / generalize `GetMyVideosPath()` into a small folder-target helper used by all four Start Menu click handlers:

1. If custom path is non-empty and exists / is reachable → use it.
2. Else Windows `Environment.SpecialFolder` (and existing Videos Shell Folders / InputBox fallback only if still needed for Videos with no custom path).
3. UNC custom path: first open follows Feature A auth rules when access fails.

Invalid or missing path → LCARS message; do not launch explorer on a bad path silently.

### Labels

After Settings save and on mainscreen / business init (after `loadLanguage()`):

- If custom label non-empty → set `ButtonText` / `Text` on the matching control on all loaded mainscreens.
- If empty → leave current text (Designer default or language file).

Controls: `myDocuments`, `myPictures`, `myMusic`, `myVideos` (present on Views 1–4).

### Click handlers

`myDocuments_Click`, `myPictures_Click`, `myMusic_Click`, `myVideos_Click` in `modBusiness.vb` pass the **resolved** path as `LCARSexplorer.exe` arguments (same as today, but path may be remapped).

---

## Architecture

```
Start Menu NETWORK PLACES ──► LCARSexplorer.exe NETWORK:
Start Menu media buttons ──► LCARSexplorer.exe <resolved path>
frmSettings Folders panel ──► GetSetting/SaveSetting ──► helpers + button labels
LCARSexplorer NETWORK: root ──► mapped drives + SMB scan ──► shares ──► loadDir(UNC)
Access denied ──► LCARS cred dialog ──► Credential Manager ──► retry
```

### Primary files (expected)

| Area | Files |
|------|--------|
| Enable Network + launch | `frmMainscreen1.Designer.vb`, `frmMainscreen1.vb`, `modBusiness.vb` |
| Path/label helpers | `modCommon.vb` (or new small module next to Videos helper) |
| Settings UI | `frmSettings.vb` / `.designer.vb` |
| Explorer network root | `LCARSexplorer\frmMyComp.vb` + new discovery/share/cred helpers under explorer |
| Browse dialog stub | `LCARSbuttons\LCARSfileBrowseDialog.vb` |

### Shared vs duplicated discovery

Terminal already has host port scanning for RDP/VNC. Explorer needs SMB ports. Prefer a dedicated explorer helper first to avoid cross-project coupling; extract a shared library later only if both diverge painfully.

---

## Testing

- Network Places lit/clickable on View 1; opens explorer on `NETWORK:`.
- Mapped network drive appears and opens.
- SCAN finds a known SMB host; share list; browse files.
- Access-denied path prompts; Remember persists; second open skips prompt when creds valid.
- File browse dialog NETWORK no longer shows stub MsgBox.
- Settings Folders: set path + label for each of four; Start Menu shows label; click opens remapped folder (including UNC).
- Reset restores Windows default path behavior and non-override label.
- Blank label leaves language-file / default text.
- Tablet package includes updated `LCARSexplorer.exe` and `LCARSmain.exe` (and any new DLLs if introduced — prefer none).

## Delivery

Build, `Package-LCARSUpdate.ps1 -Bump`, `Upload-LCARSUpdate.ps1` per project tablet-update rule after the feature lands.

## Open follow-ups (explicitly deferred)

- Recent UNC list on network root
- Show administrative shares toggle
- Shared discovery library with Terminal
- Network Places on mainscreens other than View 1 (button currently View 1 only)
