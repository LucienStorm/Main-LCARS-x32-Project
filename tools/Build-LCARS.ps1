# Rebuilds the full LCARS x32 solution for deploy.
# Requires: .NET Framework 4.8.1 SDK tools (resgen/tlbimp) under
#   C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\
# Override with -SdkToolsPath if needed.
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$SdkToolsPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$msbuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$sln = Join-Path $repoRoot "LCARSmain\LCARSmain.sln"
$props = Join-Path $repoRoot "build\LCARS.Build.props"

if (-not (Test-Path $msbuild)) {
    throw "MSBuild not found at $msbuild"
}

if ($SdkToolsPath -and -not $SdkToolsPath.EndsWith("\")) {
    $SdkToolsPath += "\"
}

if (-not $SdkToolsPath) {
    $SdkToolsPath = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\"
}

if (-not (Test-Path (Join-Path $SdkToolsPath "resgen.exe"))) {
    throw @"
resgen.exe not found at: $SdkToolsPath

Install the .NET Framework 4.8.1 developer pack / Windows SDK, or pass your tools folder:
  powershell -File tools\Build-LCARS.ps1 -SdkToolsPath `"D:\path\to\NETFX 4.8.1 Tools\`"
"@
}

Write-Host "Building LCARS x32 ($Configuration)..."
Write-Host "SDK tools: $SdkToolsPath"

$sdkForMsbuild = $SdkToolsPath.TrimEnd('\')
$sln = Join-Path $repoRoot "LCARSmain\LCARSmain.sln"

& $msbuild $sln /t:LCARSmain /p:Configuration=$Configuration '/p:Platform=Any CPU' /p:ToolsVersion=4.0 "/p:ResgenToolPath=$sdkForMsbuild" "/p:SDK40ToolsPath=$sdkForMsbuild" "/p:LcarsSdkToolsPath=$sdkForMsbuild" /m:1 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "LCARS main build failed with exit code $LASTEXITCODE" }

$satelliteProjects = @(
    "LCARSUpdate\LCARSUpdate\LCARSUpdate.vbproj",
    "runInstallScript\runInstallScript\runInstallScript.vbproj",
    "SetShell\SetShell\SetShell.vbproj",
    "OnScreenKeyboard\OnScreenKeyboard\OnScreenKeyboard.vbproj",
    "LCARSexplorer\LCARSexplorer\LCARSexplorer.vbproj",
    "LCARSshutdown\LCARSshutdown\LCARSshutdown.vbproj",
    "LCARSpic\LCARSpic.vbproj"
)
foreach ($rel in $satelliteProjects) {
    $proj = Join-Path $repoRoot $rel
    if (-not (Test-Path $proj)) { continue }
    & $msbuild $proj /t:Build /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $rel" }
}

$x86Satellites = @(
    "LCARSTerminal\LCARSTerminal\LCARSTerminal.vbproj"
)
foreach ($rel in $x86Satellites) {
    $proj = Join-Path $repoRoot $rel
    if (-not (Test-Path $proj)) { continue }
    & $msbuild $proj /t:Build /p:Configuration=$Configuration /p:Platform=x86 /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $rel" }
}

$out = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\Debug"
$satelliteOutputs = @{
    "LCARSUpdate.exe" = "LCARSUpdate\LCARSUpdate\bin\$Configuration\LCARSUpdate.exe"
    "runInstallScript.exe" = "runInstallScript\runInstallScript\bin\$Configuration\runInstallScript.exe"
    "SetShell.exe" = "SetShell\SetShell\bin\$Configuration\SetShell.exe"
    "OnScreenKeyboard.exe" = "OnScreenKeyboard\OnScreenKeyboard\bin\$Configuration\OnScreenKeyboard.exe"
    "OnScreenKeyboard.exe.config" = "OnScreenKeyboard\OnScreenKeyboard\bin\$Configuration\OnScreenKeyboard.exe.config"
    "LCARSexplorer.exe" = "LCARSexplorer\LCARSexplorer\bin\$Configuration\LCARSexplorer.exe"
    "LCARSshutdown.exe" = "LCARSshutdown\LCARSshutdown\bin\$Configuration\LCARSshutdown.exe"
    "LCARSmedia.exe" = "LCARSpic\bin\$Configuration\LCARSmedia.exe"
    "LibVLCSharp.dll" = "LCARSpic\bin\$Configuration\LibVLCSharp.dll"
    "LCARSTerminal.exe" = "LCARSTerminal\LCARSTerminal\bin\$Configuration\LCARSTerminal.exe"
    "AxMSTSCLib.dll" = "LCARSTerminal\LCARSTerminal\bin\$Configuration\AxMSTSCLib.dll"
    "MSTSCLib.dll" = "LCARSTerminal\LCARSTerminal\bin\$Configuration\MSTSCLib.dll"
    "RemoteViewing.dll" = "LCARSTerminal\LCARSTerminal\bin\$Configuration\RemoteViewing.dll"
    "RemoteViewing.Windows.Forms.dll" = "LCARSTerminal\LCARSTerminal\bin\$Configuration\RemoteViewing.Windows.Forms.dll"
    "Ionic.Zip.Reduced.dll" = "runInstallScript\runInstallScript\bin\$Configuration\Ionic.Zip.Reduced.dll"
}
foreach ($pair in $satelliteOutputs.GetEnumerator()) {
    $src = Join-Path $repoRoot $pair.Value
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $out $pair.Key) -Force
    }
}

# LibVLC natives for LCARSmedia (x86)
$vlcSrc = Join-Path $repoRoot "LCARSpic\bin\$Configuration\lib\vlc"
$vlcDst = Join-Path $out "lib\vlc"
if (Test-Path $vlcSrc) {
    if (Test-Path $vlcDst) { Remove-Item $vlcDst -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Split-Path $vlcDst) | Out-Null
    Copy-Item $vlcSrc $vlcDst -Recurse -Force
}

# Browser is x86 Release/Debug in a sibling project; build explicitly after the solution.
$browserProj = Join-Path $repoRoot "Lcars Web Browser\Lcars Web Browser.vbproj"
$browserCfg = if ($Configuration -eq "Release") { "Release" } else { "Debug" }
& $msbuild $browserProj /t:Build /p:Configuration=$browserCfg /p:Platform=x86 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Web browser build failed with exit code $LASTEXITCODE" }

$out = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\Debug"
if ($Configuration -eq "Release") {
    $releaseOut = Join-Path $repoRoot "LCARSmain\LCARSmain\bin\debug"
    if (Test-Path $releaseOut) { $out = $releaseOut }
}

$browserDir = Join-Path $repoRoot "Lcars Web Browser\bin\$browserCfg"
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
        Copy-Item $src (Join-Path $out $name) -Force
    }
}

Write-Host ""
Write-Host "Build complete. Install folder: $out"
Write-Host "Fresh install package: powershell -File tools\Build-LCARSInstall.ps1 -Configuration $Configuration"
Write-Host "Update package only:   powershell -File tools\Package-LCARSUpdate.ps1 -Configuration $Configuration"
