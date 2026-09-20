# Stages a LCARSUpdate manifest + changed binaries for your update server.
#
# Versioning (required clarity for iterative tablet deploys):
#   Build\update-version.txt holds the current global ID, e.g. 0.7.2.1
#   Each publish should bump the last segment: 0.7.2.1 -> 0.7.2.2 -> ...
#   Pass -Bump to increment automatically when packaging.
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
    [string]$DownloadBaseUrl = "http://192.168.68.120:8765/",
    [switch]$Bump
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "Build\update-version.txt"

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

$deployFiles = @(
    "LCARSmain.exe",
    "LCARS.dll",
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
    Copy-Item $from (Join-Path $out $name) -Force
    $hash = [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($from))).Replace("-", "").ToLowerInvariant()
    $lines.Add($name)
    $lines.Add($ComponentVersion)
    $lines.Add("$downloadBase$name")
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
    $vlcStage = Join-Path $out "_vlc-zip-stage"
    $vlcStageLib = Join-Path $vlcStage "lib\vlc"
    New-Item -ItemType Directory -Force -Path $vlcStageLib | Out-Null
    Copy-Item (Join-Path $vlcInstall "*") $vlcStageLib -Recurse -Force
    $vlcZipPath = Join-Path $out $vlcZipName
    if (Test-Path $vlcZipPath) { Remove-Item $vlcZipPath -Force }
    # Compress-Archive paths: zip root must be "lib/..." so ExtractAll lands under install\lib\vlc\
    Compress-Archive -Path (Join-Path $vlcStage "lib") -DestinationPath $vlcZipPath -CompressionLevel Optimal
    Remove-Item $vlcStage -Recurse -Force
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
Example: http://192.168.68.120:8765/CustomVersion.txt

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
