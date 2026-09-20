# Network Places + Remappable Media Folders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable NETWORK PLACES in LCARSexplorer (`NETWORK:` root with mapped drives + SMB discovery) and add global remappable paths/labels for Documents, Pictures, Music, and Videos under Settings → SCREEN-SPECIFIC.

**Architecture:** Start Menu launches `LCARSexplorer.exe` with `NETWORK:` or a resolved media path. Explorer adds a network root mode (mapped drives + LAN SMB scan + share list + existing `loadDir`). Main adds GetSetting-backed path/label helpers and a Folders panel in Settings. Auth prompts only on access failure, with optional Credential Manager save.

**Tech Stack:** VB.NET WinForms (.NET Framework 4.x), LCARS controls, Win32 share enum / Credential Manager P/Invoke, existing `GetSetting`/`SaveSetting` registry store.

**Spec:** `docs/superpowers/specs/2026-09-20-network-places-folder-remap-design.md`

## Global Constraints

- Restore-original rule: do not invent new Start Menu chrome; enable existing `fbMyNetwork` and reuse explorer listing patterns.
- Tablet updates: after shipping build, run `Package-LCARSUpdate.ps1 -Bump` then `Upload-LCARSUpdate.ps1` unless user says not to upload.
- Videos path: keep reading legacy GetSetting key `Videos` as fallback when `VideosPath` is empty.
- Network Places button exists on View 1 only for this pass.
- Hide administrative shares (`C$`, `ADMIN$`, `IPC$`) by default.
- No new NuGet dependencies if avoidable.

## File map

| File | Role |
|------|------|
| `LCARSmain\...\Modules\modMediaFolders.vb` (new) | Path/label resolve, save, apply labels |
| `LCARSmain\...\Modules\modCommon.vb` | Keep `GetMyVideosPath` as thin wrapper → `modMediaFolders` |
| `LCARSmain\...\Modules\modBusiness.vb` | Media clicks use resolved paths; Network launch; apply labels after language load |
| `LCARSmain\...\Forms\Main screens\frmMainscreen1.Designer.vb` | `fbMyNetwork` Lit/Clickable true |
| `LCARSmain\...\Forms\Main screens\frmMainscreen1.vb` | Network click → business launch |
| `LCARSmain\...\Forms\frmSettings.vb` (+ designer) | Folders sub-panel under SCREEN-SPECIFIC |
| `LCARSexplorer\...\NetworkPlacesRoot.vb` (new) | Mapped drives + SMB scan helpers |
| `LCARSexplorer\...\SmbShareEnumerator.vb` (new) | List shares for a host |
| `LCARSexplorer\...\NetworkCredentialPrompt.vb` (new) | LCARS cred dialog + Cred Manager |
| `LCARSexplorer\...\frmMyComp.vb` | `NETWORK:` load, up-nav, scan button, auth-on-fail around dir open |
| `LCARSexplorer\...\LCARSexplorer.vbproj` | Compile new files |
| `LCARSbuttons\...\LCARSfileBrowseDialog.vb` | NETWORK button → network root behavior |
| `tools\Build-LCARS.ps1` / Package | Only if new deploy files appear (prefer none) |

---

### Task 1: Media folder path/label helpers (main)

**Files:**
- Create: `LCARSmain\LCARSmain\Modules\modMediaFolders.vb`
- Modify: `LCARSmain\LCARSmain\Modules\modCommon.vb` (`GetMyVideosPath`)
- Modify: `LCARSmain\LCARSmain\LCARSmain.vbproj` (include new module if not wildcard)

**Interfaces:**
- Produces:
  - `Public Enum MediaFolderKind` → `Documents`, `Pictures`, `Music`, `Videos`
  - `Public Function GetMediaFolderPath(kind As MediaFolderKind) As String`
  - `Public Function GetMediaFolderLabel(kind As MediaFolderKind) As String` (empty = no override)
  - `Public Sub SetMediaFolderPath(kind As MediaFolderKind, path As String)`
  - `Public Sub SetMediaFolderLabel(kind As MediaFolderKind, label As String)`
  - `Public Sub ResetMediaFolder(kind As MediaFolderKind)` (clears path + label keys)
  - `Public Sub ApplyMediaFolderLabels(root As Control)` (finds `myDocuments` / `myPictures` / `myMusic` / `myVideos` and sets ButtonText when label non-empty)
- Consumes: `GetSetting`/`SaveSetting` app name `"LCARS x32"`, section `"Application"`

**Setting keys:** `DocumentsPath`, `PicturesPath`, `MusicPath`, `VideosPath`, `DocumentsLabel`, `PicturesLabel`, `MusicLabel`, `VideosLabel`. For Videos path resolution: `VideosPath` then legacy `Videos` then Shell Folders `My Video` then empty.

- [ ] **Step 1: Add `modMediaFolders.vb`**

```vb
' LCARSmain/Modules/modMediaFolders.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.Windows.Forms
Imports LCARS

Public Enum MediaFolderKind
    Documents = 0
    Pictures = 1
    Music = 2
    Videos = 3
End Enum

Public Module modMediaFolders
    Private Const AppName As String = "LCARS x32"
    Private Const Section As String = "Application"

    Private Function PathKey(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "DocumentsPath"
            Case MediaFolderKind.Pictures : Return "PicturesPath"
            Case MediaFolderKind.Music : Return "MusicPath"
            Case Else : Return "VideosPath"
        End Select
    End Function

    Private Function LabelKey(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "DocumentsLabel"
            Case MediaFolderKind.Pictures : Return "PicturesLabel"
            Case MediaFolderKind.Music : Return "MusicLabel"
            Case Else : Return "VideosLabel"
        End Select
    End Function

    Private Function DefaultWindowsPath(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            Case MediaFolderKind.Pictures
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            Case MediaFolderKind.Music
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            Case Else
                Try
                    Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
                    myReg = myReg.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\explorer\Shell Folders\", False)
                    If myReg IsNot Nothing Then
                        Dim v As Object = myReg.GetValue("My Video")
                        If v IsNot Nothing Then Return CStr(v)
                    End If
                Catch
                End Try
                Return ""
        End Select
    End Function

    Public Function GetMediaFolderPath(ByVal kind As MediaFolderKind) As String
        Dim custom As String = GetSetting(AppName, Section, PathKey(kind), "").Trim()
        If custom = "" AndAlso kind = MediaFolderKind.Videos Then
            custom = GetSetting(AppName, Section, "Videos", "").Trim()
        End If
        If custom <> "" Then Return custom
        Return DefaultWindowsPath(kind)
    End Function

    Public Function GetMediaFolderLabel(ByVal kind As MediaFolderKind) As String
        Return GetSetting(AppName, Section, LabelKey(kind), "").Trim()
    End Function

    Public Sub SetMediaFolderPath(ByVal kind As MediaFolderKind, ByVal path As String)
        SaveSetting(AppName, Section, PathKey(kind), If(path, "").Trim())
        If kind = MediaFolderKind.Videos Then
            ' Keep legacy key in sync for older code paths.
            SaveSetting(AppName, Section, "Videos", If(path, "").Trim())
        End If
    End Sub

    Public Sub SetMediaFolderLabel(ByVal kind As MediaFolderKind, ByVal label As String)
        SaveSetting(AppName, Section, LabelKey(kind), If(label, "").Trim())
    End Sub

    Public Sub ResetMediaFolder(ByVal kind As MediaFolderKind)
        SetMediaFolderPath(kind, "")
        SetMediaFolderLabel(kind, "")
    End Sub

    Public Function ControlNameFor(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "myDocuments"
            Case MediaFolderKind.Pictures : Return "myPictures"
            Case MediaFolderKind.Music : Return "myMusic"
            Case Else : Return "myVideos"
        End Select
    End Function

    Public Sub ApplyMediaFolderLabels(ByVal root As Control)
        If root Is Nothing Then Return
        For Each kind As MediaFolderKind In [Enum].GetValues(GetType(MediaFolderKind))
            Dim label As String = GetMediaFolderLabel(kind)
            If label = "" Then Continue For
            Dim found() As Control = root.Controls.Find(ControlNameFor(kind), True)
            If found Is Nothing OrElse found.Length = 0 Then Continue For
            Dim btn As LCARSbuttonClass = TryCast(found(0), LCARSbuttonClass)
            If btn IsNot Nothing Then
                btn.ButtonText = label
                btn.Text = label
            End If
        Next
    End Sub
End Module
```

- [ ] **Step 2: Point `GetMyVideosPath` at the helper**

In `modCommon.vb`, replace body of `GetMyVideosPath` with:

```vb
Public Function GetMyVideosPath() As String
    Return GetMediaFolderPath(MediaFolderKind.Videos)
End Function
```

(Keep the region; remove the old InputBox auto-prompt from the hot path — Settings Folders is now the place to set Videos. If path is empty at click time, Task 2 shows a message.)

- [ ] **Step 3: Add module to vbproj if needed; build main**

Run:

```powershell
$msbuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
& $msbuild "d:\LCARS.fun\Main-LCARS-x32-Project\LCARSmain\LCARSmain\LCARSmain.vbproj" /t:Build /p:Configuration=Debug '/p:Platform=Any CPU' /v:minimal
```

Expected: build succeeds (or only pre-existing unrelated warnings).

- [ ] **Step 4: Commit**

```powershell
git add -f "LCARSmain/LCARSmain/Modules/modMediaFolders.vb" "LCARSmain/LCARSmain/Modules/modCommon.vb" "LCARSmain/LCARSmain/LCARSmain.vbproj"
git commit -m "$(cat <<'EOF'
Add media folder path/label helpers for remappable Start Menu folders.

EOF
)"
```

(On Windows PowerShell without bash HEREDOC, use: `git commit -m "Add media folder path/label helpers for remappable Start Menu folders."`)

---

### Task 2: Wire Start Menu media clicks + labels

**Files:**
- Modify: `LCARSmain\LCARSmain\Modules\modBusiness.vb`
- Consumes: `GetMediaFolderPath`, `ApplyMediaFolderLabels`

- [ ] **Step 1: Change the four click handlers**

Replace path arguments:

```vb
Public Sub myDocuments_Click(ByVal sender As Object, ByVal e As System.EventArgs)
    LaunchMediaExplorer(MediaFolderKind.Documents)
End Sub
' same for Pictures, Music, Videos

Private Sub LaunchMediaExplorer(ByVal kind As MediaFolderKind)
    Dim path As String = GetMediaFolderPath(kind)
    If String.IsNullOrWhiteSpace(path) Then
        LCARS.UI.MsgBox("No folder path is set for this button. Set it in Settings → SCREEN-SPECIFIC → Folders.", MsgBoxStyle.OkOnly, "ERROR:")
        Return
    End If
    If Not Directory.Exists(path) Then
        ' UNC may fail Exists when offline — still attempt launch; explorer/auth handles denial.
        If Not path.StartsWith("\\") Then
            LCARS.UI.MsgBox("Folder not found:" & vbCrLf & path, MsgBoxStyle.OkOnly, "ERROR:")
            Return
        End If
    End If
    Dim myProcess As New Process()
    myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
    myProcess.StartInfo.Arguments = """" & path & """"
    launchProcessOnScreen(myProcess)
End Sub
```

Quote arguments so paths with spaces work.

- [ ] **Step 2: Apply labels after language load**

At end of `loadLanguage()` (and after init associates buttons), call:

```vb
ApplyMediaFolderLabels(myForm)
```

- [ ] **Step 3: Manual check**

Set `DocumentsPath` via Immediate/registry or temporary SaveSetting; click MY DOCUMENTS; confirm explorer opens that path. Clear path; confirm Windows default.

- [ ] **Step 4: Commit**

```powershell
git add "LCARSmain/LCARSmain/Modules/modBusiness.vb"
git commit -m "Use remappable media folder paths and labels on Start Menu."
```

---

### Task 3: Settings → SCREEN-SPECIFIC → Folders panel

**Files:**
- Modify: `LCARSmain\LCARSmain\Forms\frmSettings.vb`
- Modify: `LCARSmain\LCARSmain\Forms\frmSettings.designer.vb` (or build panel in code like weather/browser home if that pattern is easier)

**UI:** Add `fbFolders` nav button next to Wallpaper / Main Screen / Language. Panel `pnlFolders` with four rows (name TextBox, path TextBox, Browse FlatButton, Reset FlatButton) + Save FlatButton.

- [ ] **Step 1: Add nav + panel (match existing RedAlert toggle pattern)**

Mirror `fbWallpaper_Click` / `fbLanguage_Click`:

```vb
Private Sub fbFolders_Click(...) Handles fbFolders.Click
    ShowScreenSpecificSubPanel(pnlFolders)
    fbFolders.RedAlert = LCARS.LCARSalert.White
    fbWallpaper.RedAlert = LCARS.LCARSalert.Normal
    ' ... clear others
    LoadFoldersPanel()
End Sub
```

- [ ] **Step 2: Load/Save/Browse/Reset**

```vb
Private Sub LoadFoldersPanel()
    txtDocName.Text = GetMediaFolderLabel(MediaFolderKind.Documents)
    txtDocPath.Text = GetSetting("LCARS x32", "Application", "DocumentsPath", "")
    ' ... pictures, music, videos (VideosPath then Videos legacy for display)
End Sub

Private Sub fbFoldersSave_Click(...)
    SetMediaFolderLabel(MediaFolderKind.Documents, txtDocName.Text)
    SetMediaFolderPath(MediaFolderKind.Documents, txtDocPath.Text)
    ' ... all four
    For Each b As modBusiness In curBusiness
        If b IsNot Nothing AndAlso b.myForm IsNot Nothing Then
            ApplyMediaFolderLabels(b.myForm)
        End If
    Next
    LCARS.UI.MsgBox("Folder settings saved.", MsgBoxStyle.OkOnly, "FOLDERS")
End Sub

Private Sub BrowseFor(ByVal pathBox As TextBox)
    Using dlg As New FolderBrowserDialog()
        dlg.SelectedPath = pathBox.Text
        If dlg.ShowDialog(Me) = DialogResult.OK Then pathBox.Text = dlg.SelectedPath
    End Using
End Sub
```

Reset clears that row’s name+path fields and calls `ResetMediaFolder`.

- [ ] **Step 3: Build + manual UI check**

Open Settings → SCREEN-SPECIFIC → pick a screen → Folders → set label `PHOTOS` and a path → Save → Start Menu shows PHOTOS.

- [ ] **Step 4: Commit**

```powershell
git add "LCARSmain/LCARSmain/Forms/frmSettings.vb" "LCARSmain/LCARSmain/Forms/frmSettings.designer.vb"
git commit -m "Add SCREEN-SPECIFIC Folders panel for media path and label remap."
```

---

### Task 4: Enable NETWORK PLACES launch

**Files:**
- Modify: `frmMainscreen1.Designer.vb` — `fbMyNetwork.Clickable = True`, `Lit = True`
- Modify: `frmMainscreen1.vb` — replace TODO click
- Modify: `modBusiness.vb` — `myNetworkPlaces_Click` + `tryAssocButton` if needed

- [ ] **Step 1: Designer enable**

```vb
Me.fbMyNetwork.Clickable = True
Me.fbMyNetwork.Lit = True
```

- [ ] **Step 2: Launch handler**

```vb
Public Sub myNetworkPlaces_Click(ByVal sender As Object, ByVal e As EventArgs)
    Dim myProcess As New Process()
    myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
    myProcess.StartInfo.Arguments = "NETWORK:"
    launchProcessOnScreen(myProcess)
End Sub
```

Wire from `frmMainscreen1.vb` `fbMyNetwork_Click` → `myStartMenu.myNetworkPlaces_Click` (or assoc like other buttons). Include in `startMenuItem_Click` Handles list **or** dedicated handler that closes start panel then launches.

- [ ] **Step 3: Commit**

```powershell
git commit -m "Enable Start Menu NETWORK PLACES to launch explorer NETWORK: root."
```

---

### Task 5: Explorer `NETWORK:` root — mapped drives + host scan UI

**Files:**
- Create: `LCARSexplorer\LCARSexplorer\NetworkPlacesRoot.vb`
- Modify: `frmMyComp.vb`, `LCARSexplorer.vbproj`

**Interfaces:**
- `Public Const NetworkRootToken As String = "NETWORK:"`
- `Public Function IsNetworkRoot(path As String) As Boolean`
- `Public Function GetMappedNetworkDrives() As List(Of DriveInfo)`
- `Public Function ScanSmbHosts(Optional timeoutMs As Integer = 350) As List(Of SmbDiscoveredHost)`
- `Public Class SmbDiscoveredHost` with `IpAddress`, `Hostname`, `DisplayText()`

Scan: local subnet TCP 445 and/or 139 (copy structure from Terminal `RemoteNetworkDiscovery`, change ports).

- [ ] **Step 1: Implement helpers**

- [ ] **Step 2: `frmMyComp_Load` recognize token**

```vb
Dim arg As String = Command().Trim().Trim(""""c)
If NetworkPlacesRoot.IsNetworkRoot(arg) Then
    curPath = NetworkPlacesRoot.NetworkRootToken
ElseIf arg <> "" AndAlso Directory.Exists(arg) Then
    curPath = arg
End If
```

- [ ] **Step 3: `loadNetworkPlaces()`**

Fill `gridMyComp` like `loadMyComp`:
1. Header-style buttons or side text for mapped drives; click → `loadDir(root)`.
2. Discovered hosts; click → load share list (Task 6).
3. Add a SCAN control (reuse an existing chrome button or grid button) that rescans on background worker and refreshes list.

Call `loadNetworkPlaces` from `loadDir` when `IsNetworkRoot(newpath)` **or** when `curPath` is the token and path empty.

- [ ] **Step 4: Up-directory**

In `sbUpDir_Click`, if parent would leave a UNC host with no path, return to `NETWORK:`. Never treat `NETWORK:` as a filesystem parent of My Computer.

- [ ] **Step 5: Build explorer; manual open `LCARSexplorer.exe NETWORK:`**

Expected: mapped drives listed; SCAN populates hosts on LAN with SMB.

- [ ] **Step 6: Commit**

```powershell
git commit -m "Add LCARSexplorer NETWORK: root with mapped drives and SMB host scan."
```

---

### Task 6: Share enumeration + browse + on-demand credentials

**Files:**
- Create: `SmbShareEnumerator.vb`
- Create: `NetworkCredentialStore.vb` + `frmNetworkCredentials.vb` (LCARS prompt; pattern after Terminal `frmRdpCredentials` / `RdpCredentialStore`, target names like `LCARSExplorer/SMB/<host>`)
- Modify: `frmMyComp.vb`

**Interfaces:**
- `SmbShareEnumerator.ListShares(host As String) As List(Of String)` — share names; filter out `IPC$`, `ADMIN$`, and `* $` admin shares by default
- `NetworkCredentialStore.TryGet` / `Save` / `Delete` for host
- `TryOpenDirectory(path As String) As Boolean` — Exists/Enumerate; on unauthorized, prompt, WNetAddConnection2 or equivalent with creds, retry once

- [ ] **Step 1: Share list view**

Host click sets `curPath` to `\\host` (logical) and shows share buttons; share click → `loadDir("\\host\share")`.

- [ ] **Step 2: Wrap `loadDir` filesystem access**

When `Directory.GetFileSystemInfos` throws unauthorized / network path denied → show cred dialog → if Remember, save → retry. On cancel/failure → MsgBox and stay put.

- [ ] **Step 3: Manual test**

Open host requiring creds; enter user/pass with Remember; reopen without prompt.

- [ ] **Step 4: Commit**

```powershell
git commit -m "Enumerate SMB shares and prompt for credentials on access failure."
```

---

### Task 7: File browse dialog NETWORK button

**Files:**
- Modify: `LCARSbuttons\LCARSbuttons\LCARSfileBrowseDialog.vb`

- [ ] **Step 1: Replace stub**

```vb
Private Sub sbNetwork_Click(...)
    loadDir(NetworkPlacesRoot.NetworkRootToken)
End Sub
```

If the dialog cannot reference explorer types, either:
- move `NetworkRootToken` constant to a shared place, or
- launch `LCARSexplorer.exe NETWORK:` and close dialog, or
- implement a minimal in-dialog network list later.

Preferred: if `loadDir` is local to the dialog, add a small shared constant string `"NETWORK:"` and special-case in that dialog’s `loadDir` **or** shell out to explorer. Match whichever keeps LCARSbuttons from taking a hard reference on explorer if that creates a cycle.

Check project references before choosing. If cycle: `Process.Start(StartupPath & "\LCARSexplorer.exe", "NETWORK:")`.

- [ ] **Step 2: Commit**

```powershell
git commit -m "Wire file browse dialog NETWORK button to Network Places."
```

---

### Task 8: Build, package, upload

- [ ] **Step 1: Full build**

```powershell
powershell -NoProfile -File "d:\LCARS.fun\Main-LCARS-x32-Project\tools\Build-LCARS.ps1" -Configuration Debug
```

Expected: `BUILD` complete; `LCARSexplorer.exe` and `LCARSmain.exe` refreshed in install folder.

- [ ] **Step 2: Package + upload**

```powershell
powershell -NoProfile -File "d:\LCARS.fun\Main-LCARS-x32-Project\tools\Package-LCARSUpdate.ps1" -Configuration Debug -Bump
powershell -NoProfile -File "d:\LCARS.fun\Main-LCARS-x32-Project\tools\Upload-LCARSUpdate.ps1"
```

Verify `http://192.168.68.120:8765/CustomVersion.txt` shows new version and includes `LCARSexplorer.exe` / `LCARSmain.exe`.

- [ ] **Step 3: Commit any script tweaks if required** (usually none)

---

## Plan self-review

| Spec requirement | Task |
|------------------|------|
| Enable Network Places lit/clickable + launch | 4 |
| Mapped drives + SMB discovery root | 5 |
| Share drill-down + loadDir UNC | 6 |
| On-demand creds + remember | 6 |
| Browse dialog NETWORK | 7 |
| Settings Folders under SCREEN-SPECIFIC, global | 3 |
| Path + label for four media buttons | 1–3 |
| Click handlers use resolved paths | 2 |
| Videos legacy key fallback | 1 |
| Tablet package/upload | 8 |
| Deferred: recent UNC, admin shares toggle, other views | (omitted) |

No TBD placeholders remain in tasks above.
