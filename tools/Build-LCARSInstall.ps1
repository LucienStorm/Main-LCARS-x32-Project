# Builds a complete LCARS x32 fresh-install package with the Standard Installer.
#
# Output: Build\Install\LCARS-<version>\
#   LCARSx32Installer.exe   — choose install folder, extracts payload, font, shortcut
#   CurrentVersion.zip      — full program tree (excludes .pdb / .xml)
#   Ionic.Zip.Reduced.dll   — required by the installer
#   README.txt
# (Update payloads stay in Build\Deploy\LCARS-<version>\ — do not overwrite those.)
#
# Versioning: Build\update-version.txt (0.7.2.N). Pass -Bump to increment before packaging.
#
# Usage:
#   powershell -File tools\Build-LCARSInstall.ps1
#   powershell -File tools\Build-LCARSInstall.ps1 -Configuration Debug -Bump

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Bump
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "Build\update-version.txt"
$msbuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

function Get-NextPatchVersion([string]$current) {
    $parts = $current.Trim().Split(".")
    if ($parts.Length -lt 2) {
        throw "update-version.txt must look like 0.7.2.1 (got '$current')"
    }
    $last = [int]$parts[$parts.Length - 1]
    $parts[$parts.Length - 1] = [string]($last + 1)
    return ($parts -join ".")
}

function Write-VersionsFile([string]$installDir, [string]$globalVersion, [string[]]$trackedFiles) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($globalVersion)
    foreach ($name in $trackedFiles) {
        $path = Join-Path $installDir $name
        if (Test-Path $path) {
            $lines.Add($name)
            $lines.Add($globalVersion)
        }
    }
    [IO.File]::WriteAllLines((Join-Path $installDir "versions.txt"), $lines)
}

function New-InstallZip([string]$sourceDir, [string]$zipPath) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem $sourceDir -Recurse -File | ForEach-Object {
            $rel = $_.FullName.Substring($sourceDir.Length).TrimStart('\')
            if ($rel -match '\.(pdb|xml)$') { return }
            $entryName = $rel.Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $_.FullName, $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally {
        $zip.Dispose()
    }
}

function New-PlaceholderZip([string]$zipPath) {
    $temp = Join-Path $env:TEMP ("lcars-placeholder-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $temp | Out-Null
    Set-Content -Path (Join-Path $temp "placeholder.txt") -Value "Build placeholder" -Encoding ASCII
    New-InstallZip $temp $zipPath
    Remove-Item $temp -Recurse -Force
}

if (-not (Test-Path $versionFile)) {
    Set-Content -Path $versionFile -Value "0.7.2.1" -Encoding ASCII
}
$version = (Get-Content $versionFile -Raw).Trim()
if ($Bump) {
    $version = Get-NextPatchVersion $version
    Set-Content -Path $versionFile -Value $version -Encoding ASCII
}

Write-Host "Building LCARS x32 fresh install ($Configuration) version $version ..."
Write-Host "This may take 1-2 minutes. If you see no output after 2 minutes, cancel and let me know."
Write-Host ""

& (Join-Path $repoRoot "tools\Build-LCARS.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build-LCARS.ps1 failed" }

$installDir = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\Debug"
if ($Configuration -eq "Release") {
    $releaseDir = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\debug"
    if (Test-Path $releaseDir) { $installDir = $releaseDir }
}

if (-not (Test-Path (Join-Path $installDir "LCARSmain.exe"))) {
    throw "Install tree missing after build: $installDir"
}

$trackedFiles = @(
    "LCARSmain.exe",
    "LCARS.dll",
    "LCARSWebBrowser.exe",
    "LCARSWebBrowser.exe.config",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll",
    "OnScreenKeyboard.exe",
    "LCARSexplorer.exe",
    "LCARSUpdate.exe",
    "runInstallScript.exe",
    "SetShell.exe",
    "Ionic.Zip.Reduced.dll",
    "ST-Bleep.wav"
)

Write-VersionsFile $installDir $version $trackedFiles
Write-Host "Wrote versions.txt ($version)"

$safeVersion = ($version -replace "[^\d\.]", "_")
# Keep Install packages out of Build\Deploy\LCARS-<ver> — that folder is the update payload
# and is often locked while the tablet/update tree is open.
$deployDir = Join-Path $repoRoot "Build\Install\LCARS-$safeVersion"
if (Test-Path $deployDir) {
    try {
        Remove-Item $deployDir -Recurse -Force -ErrorAction Stop
    } catch {
        $stale = "$deployDir.stale-$(Get-Date -Format 'yyyyMMddHHmmss')"
        Rename-Item -LiteralPath $deployDir -NewName (Split-Path $stale -Leaf) -Force
        Write-Warning "Could not delete previous install folder; renamed to $stale"
    }
}
New-Item -ItemType Directory -Path $deployDir -Force | Out-Null

$payloadZip = Join-Path $deployDir "CurrentVersion.zip"
Write-Host "Creating CurrentVersion.zip from $installDir (may take 1-2 minutes)..."
New-InstallZip $installDir $payloadZip
$zipSizeMb = [math]::Round((Get-Item $payloadZip).Length / 1MB, 1)
Write-Host "Payload zip: $zipSizeMb MB"

$installerResources = Join-Path $repoRoot "Standard Installer\Standard Installer\Resources"
New-Item -ItemType Directory -Force -Path $installerResources | Out-Null
$resourceZip = Join-Path $installerResources "CurrentVersion.zip"
New-PlaceholderZip $resourceZip

$installerProj = Join-Path $repoRoot "Standard Installer\Standard Installer\Standard Installer.vbproj"
Write-Host "Building Standard Installer..."
& $msbuild $installerProj /t:Build /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Standard Installer build failed" }

$installerOut = Join-Path $repoRoot "Standard Installer\Standard Installer\bin\$Configuration"
$installerExe = Join-Path $installerOut "Standard Installer.exe"
if (-not (Test-Path $installerExe)) {
    throw "Installer exe not found: $installerExe"
}

Copy-Item $installerExe (Join-Path $deployDir "LCARSx32Installer.exe") -Force
$installerConfig = Join-Path $installerOut "Standard Installer.exe.config"
if (Test-Path $installerConfig) {
    Copy-Item $installerConfig (Join-Path $deployDir "LCARSx32Installer.exe.config") -Force
}
Copy-Item (Join-Path $installDir "Ionic.Zip.Reduced.dll") (Join-Path $deployDir "Ionic.Zip.Reduced.dll") -Force

$readme = @"
LCARS x32 fresh install ($version)
=================================

1) Copy this entire folder to the target PC.
2) Run LCARSx32Installer.exe and choose the install folder.
3) Launch LCARS from the desktop shortcut or LCARSmain.exe.

Required files in this folder:
  LCARSx32Installer.exe
  LCARSx32Installer.exe.config (if present)
  CurrentVersion.zip
  Ionic.Zip.Reduced.dll

After install, use SetShell.exe or LCARS Settings if you want LCARS as the Windows shell.

Updates later: Settings -> Updates -> Custom URL to your update server's CustomVersion.txt

If install fails, the log is:
  %TEMP%\lcars-install-error.txt
On most PCs that is:
  C:\Users\<your-username>\AppData\Local\Temp\lcars-install-error.txt
"@
Set-Content -Path (Join-Path $deployDir "README.txt") -Value $readme -Encoding ASCII

Write-Host ""
Write-Host "Fresh install package ready."
Write-Host "  Version:     $version"
Write-Host "  Folder:      $deployDir"
Write-Host "  Run:         $deployDir\LCARSx32Installer.exe"
Write-Host ""
Write-Host "Copy the whole LCARS-$safeVersion folder and run LCARSx32Installer.exe."
