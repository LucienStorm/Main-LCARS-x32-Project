# Stages a LCARSUpdate manifest + changed binaries for your update server.
#
# Versioning (required clarity for iterative tablet deploys):
#   Build\update-version.txt holds the current global ID, e.g. 0.7.2.1
#   Each publish should bump the last segment: 0.7.2.1 -> 0.7.2.2 -> ...
#   Pass -Bump to increment automatically when packaging.
#
# LAN URL defaults come from Build\update-server.local.ps1 (gitignored), not this file.
#
# Usage:
#   powershell -File tools\Package-LCARSUpdate.ps1 -Configuration Debug
#   powershell -File tools\Package-LCARSUpdate.ps1 -Configuration Debug -Bump
#   powershell -File tools\Upload-LCARSUpdate.ps1

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$GlobalVersion = "",
    [string]$ComponentVersion = "",
    [string]$DownloadBaseUrl = "",
    [switch]$Bump
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "Build\update-version.txt"

$localDefaults = Join-Path $repoRoot "Build\update-server.local.ps1"
if (Test-Path $localDefaults) {
    . $localDefaults
}
if (-not $DownloadBaseUrl -and $LcarsUpdateBaseUrl) {
    $DownloadBaseUrl = $LcarsUpdateBaseUrl
}
if (-not $DownloadBaseUrl) {
    throw "DownloadBaseUrl not set. Pass -DownloadBaseUrl or create Build\update-server.local.ps1 (see update-server.local.ps1.example)."
}

function Get-NextPatchVersion([string]$current) {
    $parts = $current.Trim().Split(".")
    if ($parts.Length -lt 2) {
        throw "update-version.txt must look like 0.7.2.1 (got '$current')"
    }
    $last = [int]$parts[$parts.Length - 1]
    $parts[$parts.Length - 1] = [string]($last + 1)
    return ($parts -join ".")
}

if (-not $GlobalVersion) {
    if (-not (Test-Path $versionFile)) {
        Set-Content -Path $versionFile -Value "0.7.2.1" -Encoding ASCII
    }
    $GlobalVersion = (Get-Content $versionFile -Raw).Trim()
    if ($Bump) {
        $GlobalVersion = Get-NextPatchVersion $GlobalVersion
        Set-Content -Path $versionFile -Value $GlobalVersion -Encoding ASCII
    }
}
if (-not $ComponentVersion) { $ComponentVersion = $GlobalVersion }

$installDir = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\Debug"
if ($Configuration -eq "Release") {
    $releaseDir = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\debug"
    if (Test-Path $releaseDir) { $installDir = $releaseDir }
}

# Web browser may build to its own folder; merge into install dir for packaging
$browserDir = Join-Path $repoRoot "Lcars Web Browser\bin\$Configuration"
if ($Configuration -eq "Release") { $browserDir = Join-Path $repoRoot "Lcars Web Browser\bin\Release" }
$browserFiles = @(
    "LCARSWebBrowser.exe",
    "LCARSWebBrowser.exe.config",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll",
    "ST-Bleep.wav"
)
foreach ($name in $browserFiles) {
    $src = Join-Path $browserDir $name
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $installDir $name) -Force
    }
}

if (-not (Test-Path (Join-Path $installDir "LCARSmain.exe"))) {
    throw "LCARSmain.exe not found in $installDir. Run tools\Build-LCARS.ps1 first."
}

# COM interops sometimes land only under obj\; ensure they sit next to LCARSmain.exe before packaging.
$interopNames = @("Interop.SpeechLib.dll", "Interop.IWshRuntimeLibrary.dll")
$interopSearchRoots = @(
    $installDir,
    (Join-Path $repoRoot "LCARSmain\LCARSmain\bin\Debug"),
    (Join-Path $repoRoot "LCARSmain\LCARSmain\obj\Debug"),
    (Join-Path $repoRoot "LCARSmain\LCARSmain\obj\Release")
)
foreach ($interopName in $interopNames) {
    $destInterop = Join-Path $installDir $interopName
    if (Test-Path $destInterop) { continue }
    foreach ($root in $interopSearchRoots) {
        $candidate = Join-Path $root $interopName
        if (Test-Path $candidate) {
            Copy-Item $candidate $destInterop -Force
            Write-Host "Staged $interopName from $candidate"
            break
        }
    }
}

$deployFiles = @(
    "LCARSmain.exe",
    "LCARS.dll",
    # COM interops required next to LCARSmain.exe (startup crashes if SpeechLib is missing)
    "Interop.SpeechLib.dll",
    "Interop.IWshRuntimeLibrary.dll",
    "LCARSWebBrowser.exe",
    "LCARSWebBrowser.exe.config",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll",
    "OnScreenKeyboard.exe",
    "OnScreenKeyboard.exe.config",
    "LCARSexplorer.exe",
    "LCARSshutdown.exe",
    "LCARSmedia.exe",
    "LibVLCSharp.dll",
    "LCARSTerminal.exe",
    "LCARSTaskManager.exe",
    "LCARSengineering.exe",
    "LCARSutilities.exe",
    "LCARSCalculator.exe",
    "LCARSTimer.exe",
    "LCARSScreenshot.exe",
    "LCARSWeather.exe",
    "LCARSCalendar.exe",
    "LCARSRss.exe",
    "LCARSChat.exe",
    "LCARScomms.exe",
    "LCARSMail.exe",
    "MailKit.dll",
    "MimeKit.dll",
    "BouncyCastle.Cryptography.dll",
    "System.Buffers.dll",
    "System.Formats.Asn1.dll",
    "System.Memory.dll",
    "System.Numerics.Vectors.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Threading.Tasks.Extensions.dll",
    "System.ValueTuple.dll",
    "System.Data.SQLite.dll",
    "x86\SQLite.Interop.dll",
    "AxMSTSCLib.dll",
    "MSTSCLib.dll",
    "RemoteViewing.dll",
    "RemoteViewing.Windows.Forms.dll",
    "LCARSUpdate.exe",
    "runInstallScript.exe",
    "Ionic.Zip.Reduced.dll",
    "ST-Bleep.wav"
)

$safeVersion = ($GlobalVersion -replace "[^\d\.]", "_")
$out = Join-Path $repoRoot "Build\Deploy\LCARS-$safeVersion"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

if (-not $DownloadBaseUrl.EndsWith("/")) { $DownloadBaseUrl += "/" }
$downloadBase = $DownloadBaseUrl
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add($GlobalVersion)
$md5 = [System.Security.Cryptography.MD5]::Create()
$missing = @()

foreach ($name in $deployFiles) {
    $from = Join-Path $installDir $name
    if (-not (Test-Path $from)) {
        $missing += $name
        continue
    }
    $dest = Join-Path $out $name
    $destParent = Split-Path $dest -Parent
    if ($destParent -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Force -Path $destParent | Out-Null
    }
    Copy-Item $from $dest -Force
    $hash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($from))).Replace("-", "").ToLowerInvariant()
    $lines.Add($name)
    $lines.Add($ComponentVersion)
    $urlName = $name.Replace("\", "/")
    $lines.Add("$downloadBase$urlName")
    $lines.Add($hash)
    $lines.Add("File")
    Write-Host ("{0}  {1}" -f $hash, $name)
}

# Ship LibVLC as ONE zip File (not hundreds of plugin File entries — that crashes LCARSUpdate UI).
# Class must be File (not Extract): Extract never left the archive on disk, so MD5 checks
# re-queued lib-vlc.zip forever. Installer copies the zip then extracts it in place.
$vlcInstall = Join-Path $installDir "lib\vlc"
$vlcZipName = "lib-vlc.zip"
if (Test-Path $vlcInstall) {
    $vlcZipPath = Join-Path $out $vlcZipName
    if (Test-Path $vlcZipPath) { Remove-Item $vlcZipPath -Force }
    $vlcStageRoot = Join-Path $env:TEMP ("lcars-vlc-zip-" + [Guid]::NewGuid().ToString("N"))
    $vlcStageLib = Join-Path $vlcStageRoot "lib\vlc"
    New-Item -ItemType Directory -Force -Path $vlcStageLib | Out-Null
    try {
        # Prefer robocopy so locked plugins don't abort the whole tree copy mid-Compress.
        & robocopy $vlcInstall $vlcStageLib /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            (Join-Path $vlcStageRoot "lib"),
            $vlcZipPath,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $true)
    } catch {
        Write-Warning "VLC zip failed ($($_.Exception.Message)); reusing prior package zip if available."
        $priorZip = Get-ChildItem (Join-Path $repoRoot "Build\Deploy") -Directory |
            Sort-Object LastWriteTime -Descending |
            ForEach-Object { Join-Path $_.FullName $vlcZipName } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if (-not $priorZip) { throw "Could not create or reuse $vlcZipName" }
        Copy-Item $priorZip $vlcZipPath -Force
        Write-Host "Reused $priorZip"
    } finally {
        if (Test-Path $vlcStageRoot) { Remove-Item $vlcStageRoot -Recurse -Force -ErrorAction SilentlyContinue }
    }
    $hash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($vlcZipPath))).Replace("-", "").ToLowerInvariant()
    $lines.Add($vlcZipName)
    $lines.Add($ComponentVersion)
    $lines.Add("$downloadBase$vlcZipName")
    $lines.Add($hash)
    $lines.Add("File")
    Write-Host ("{0}  {1} (File + install-time extract)" -f $hash, $vlcZipName)
    Write-Host "Packaged LibVLC tree as single zip from $vlcInstall"
} else {
    Write-Warning "LibVLC tree missing at $vlcInstall - LCARSmedia audio/video will not play until natives are present."
}

if ($missing.Count -gt 0) {
    $oskMissing = $missing | Where-Object { $_ -like "OnScreenKeyboard*" }
    if ($oskMissing) {
        throw "Required OSK file(s) missing from package (build incomplete): $($oskMissing -join ', '). Refusing to publish."
    }
    Write-Warning "Skipped missing files (build may be incomplete): $($missing -join ', ')"
}

# Keep Lang\*.lng synced in the local install folder for desktop runs, but do NOT
# put nested Lang\ paths in CustomVersion.txt — older LCARSUpdate cannot stage
# subdirectories and hits max retry on Standard.lng. Start Menu remaps in code.
$langSrcRoot = Join-Path $repoRoot "LCARSmain\LCARSmain\Lang"
if (Test-Path $langSrcRoot) {
    $installLangDir = Join-Path $installDir "Lang"
    if (-not (Test-Path $installLangDir)) { New-Item -ItemType Directory -Path $installLangDir | Out-Null }
    Copy-Item (Join-Path $langSrcRoot "*.lng") $installLangDir -Force
}

$manifestPath = Join-Path $out "CustomVersion.txt"
[IO.File]::WriteAllLines($manifestPath, $lines)

# Stamp so USB installs can confirm OSK binary without opening the keyboard.
$oskBuildStamp = Join-Path $out "OnScreenKeyboard.build.txt"
Set-Content -Path $oskBuildStamp -Value $GlobalVersion -Encoding ASCII
Copy-Item $oskBuildStamp (Join-Path $installDir "OnScreenKeyboard.build.txt") -Force
$stampHash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($oskBuildStamp))).Replace("-", "").ToLowerInvariant()
Add-Content -Path $manifestPath -Value "OnScreenKeyboard.build.txt" -Encoding ASCII
Add-Content -Path $manifestPath -Value $ComponentVersion -Encoding ASCII
Add-Content -Path $manifestPath -Value "${downloadBase}OnScreenKeyboard.build.txt" -Encoding ASCII
Add-Content -Path $manifestPath -Value $stampHash -Encoding ASCII
Add-Content -Path $manifestPath -Value "File" -Encoding ASCII

$phase7NotesSrc = Join-Path $repoRoot "docs\superpowers\specs\2026-10-12-lcars-mail-phase7-roadmap-notes.md"
$phase7NotesName = "LCARS-Mail-Phase7-Roadmap-Notes.txt"
if (Test-Path $phase7NotesSrc) {
    $phase7NotesDest = Join-Path $out $phase7NotesName
    Copy-Item $phase7NotesSrc $phase7NotesDest -Force
    Copy-Item $phase7NotesDest (Join-Path $installDir $phase7NotesName) -Force
    $notesHash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($phase7NotesDest))).Replace("-", "").ToLowerInvariant()
    Add-Content -Path $manifestPath -Value $phase7NotesName -Encoding ASCII
    Add-Content -Path $manifestPath -Value $ComponentVersion -Encoding ASCII
    Add-Content -Path $manifestPath -Value "${downloadBase}$phase7NotesName" -Encoding ASCII
    Add-Content -Path $manifestPath -Value $notesHash -Encoding ASCII
    Add-Content -Path $manifestPath -Value "File" -Encoding ASCII
}

$otrRulingSrc = Join-Path $repoRoot "docs\superpowers\specs\2026-10-12-lcars-chat-otr-ruling.md"
$otrRulingName = "LCARS-Chat-OTR-Ruling.txt"
if (Test-Path $otrRulingSrc) {
    $otrRulingDest = Join-Path $out $otrRulingName
    Copy-Item $otrRulingSrc $otrRulingDest -Force
    Copy-Item $otrRulingDest (Join-Path $installDir $otrRulingName) -Force
    $otrHash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($otrRulingDest))).Replace("-", "").ToLowerInvariant()
    Add-Content -Path $manifestPath -Value $otrRulingName -Encoding ASCII
    Add-Content -Path $manifestPath -Value $ComponentVersion -Encoding ASCII
    Add-Content -Path $manifestPath -Value "${downloadBase}$otrRulingName" -Encoding ASCII
    Add-Content -Path $manifestPath -Value $otrHash -Encoding ASCII
    Add-Content -Path $manifestPath -Value "File" -Encoding ASCII
}

$readme = @"
LCARS x32 update package ($GlobalVersion)
=======================================

Versioning
----------
Global ID format: 0.7.2.N  (increment N for each publish)
Tracked in: Build\update-version.txt
Bump on package: powershell -File tools\Package-LCARSUpdate.ps1 -Bump

Before uploading
----------------
Upload ALL files in this folder (including CustomVersion.txt) to the update server.

On the tablet
-------------
Settings -> Updates -> Custom -> paste the full URL to CustomVersion.txt
Example: (your update server)/CustomVersion.txt

The updater compares each file's MD5 to what is on disk. Matching files are skipped
even if versions.txt labels are stale (manual copy or partial install).

WebView2 Runtime is NOT in this package.

Files included
----------------
$(($deployFiles | ForEach-Object { "  - $_" }) -join "`r`n")
"@
Set-Content -Path (Join-Path $out "README.txt") -Value $readme -Encoding ASCII

Write-Host ""
Write-Host "Version: $GlobalVersion"
Write-Host "Deploy folder: $out"
Write-Host "Tablet Custom URL: ${DownloadBaseUrl}CustomVersion.txt"
