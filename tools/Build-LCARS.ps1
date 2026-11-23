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
$buttonsProj = Join-Path $repoRoot "LCARSbuttons\LCARSbuttons\LCARSbuttons.vbproj"
$buttonsDll = Join-Path $repoRoot "LCARSbuttons\LCARSbuttons\bin\Debug\LCARS.dll"

# LCARS.dll must be CLR 2 (.NET 3.5). Build it first so a later net40 ProjectReference
# cannot leave a CLR 4 LCARS.dll in the deploy folder (breaks OSK/shutdown).
Write-Host "Building LCARSbuttons (net35 / CLR 2)..."
& $msbuild $buttonsProj /t:Rebuild /p:Configuration=$Configuration /p:Platform=AnyCPU /p:TargetFrameworkVersion=v3.5 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "LCARSbuttons (net35) build failed with exit code $LASTEXITCODE" }

& $msbuild $sln /t:LCARSmain /p:Configuration=$Configuration '/p:Platform=Any CPU' /p:ToolsVersion=4.0 "/p:ResgenToolPath=$sdkForMsbuild" "/p:SDK40ToolsPath=$sdkForMsbuild" "/p:LcarsSdkToolsPath=$sdkForMsbuild" /m:1 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "LCARS main build failed with exit code $LASTEXITCODE" }

$satelliteProjects = @(
    "LCARSUpdate\LCARSUpdate\LCARSUpdate.vbproj",
    "runInstallScript\runInstallScript\runInstallScript.vbproj",
    "SetShell\SetShell\SetShell.vbproj",
    "OnScreenKeyboard\OnScreenKeyboard\OnScreenKeyboard.vbproj",
    "LCARSexplorer\LCARSexplorer\LCARSexplorer.vbproj",
    "LCARSshutdown\LCARSshutdown\LCARSshutdown.vbproj",
    "LCARSpic\LCARSpic.vbproj",
    "LCARSTaskManager\LCARSTaskManager.vbproj",
    "LCARSengineering\LCARSengineering\LCARSengineering.vbproj"
)
foreach ($rel in $satelliteProjects) {
    $proj = Join-Path $repoRoot $rel
    if (-not (Test-Path $proj)) { continue }
    & $msbuild $proj /t:Build /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $rel" }
}

# Utilities + Comms are SDK-style C# (decompile-maintained) — require `dotnet` SDK.
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw "dotnet SDK not found (required to build LCARSutilities / LCARSMail)" }

$utilCs = Join-Path $repoRoot "LCARSutilities\LCARSutilities.cs\LCARSutilities.csproj"
Write-Host "Building LCARSutilities (C#)..."
& dotnet build $utilCs -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed: LCARSutilities (C#)" }
$utilBuilt = Join-Path $repoRoot "LCARSutilities\LCARSutilities.cs\bin\$Configuration\LCARSutilities.exe"
$utilLegacyOut = Join-Path $repoRoot "LCARSutilities\LCARSutilities\bin\$Configuration"
New-Item -ItemType Directory -Force -Path $utilLegacyOut | Out-Null
Copy-Item $utilBuilt (Join-Path $utilLegacyOut "LCARSutilities.exe") -Force

$mailCs = Join-Path $repoRoot "LCARSMail\LCARSMail.cs\LCARScomms.csproj"
Write-Host "Building LCARSMail/Comms (C#)..."
& dotnet build $mailCs -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed: LCARSMail (C#)" }
$mailBuiltDir = Join-Path $repoRoot "LCARSMail\LCARSMail.cs\bin\$Configuration"
$mailLegacyOut = Join-Path $repoRoot "LCARSMail\LCARSMail\bin\$Configuration"
New-Item -ItemType Directory -Force -Path $mailLegacyOut | Out-Null
Copy-Item (Join-Path $mailBuiltDir "LCARScomms.exe") (Join-Path $mailLegacyOut "LCARScomms.exe") -Force
# Carry private deps the C# build emitted next to the exe
Get-ChildItem $mailBuiltDir -File | Where-Object {
    $_.Extension -match '\.(dll|config)$' -and $_.Name -ne "LCARScomms.exe"
} | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $mailLegacyOut $_.Name) -Force
}
$sqliteInterop = Join-Path $repoRoot "LCARSMail\LCARSMail\bin\Debug\x86\SQLite.Interop.dll"
if (-not (Test-Path $sqliteInterop)) {
    $sqliteInterop = Join-Path $repoRoot "LCARSMail\LCARSMail\bin\Release\x86\SQLite.Interop.dll"
}
if (Test-Path $sqliteInterop) {
    $x86Out = Join-Path $mailLegacyOut "x86"
    $sqliteDest = Join-Path $x86Out "SQLite.Interop.dll"
    if ((Resolve-Path $sqliteInterop).Path -ne $sqliteDest) {
        New-Item -ItemType Directory -Force -Path $x86Out | Out-Null
        Copy-Item $sqliteInterop $sqliteDest -Force
    }
}

$x86Satellites = @(
    "LCARSTerminal\LCARSTerminal\LCARSTerminal.vbproj"
)
foreach ($rel in $x86Satellites) {
    $proj = Join-Path $repoRoot $rel
    if (-not (Test-Path $proj)) { continue }
    # Prefer AnyCPU so ProjectReferences to LCARSbuttons (AnyCPU-only) resolve.
    & $msbuild $proj /t:Build /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal
    if ($LASTEXITCODE -ne 0) {
        & $msbuild $proj /t:Build /p:Configuration=$Configuration /p:Platform=x86 /p:BuildProjectReferences=false /v:minimal
    }
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
    "LCARSTaskManager.exe" = "LCARSTaskManager\bin\$Configuration\LCARSTaskManager.exe"
    "LCARSengineering.exe" = "LCARSengineering\LCARSengineering\bin\$Configuration\LCARSengineering.exe"
    "LCARSutilities.exe" = "LCARSutilities\LCARSutilities\bin\$Configuration\LCARSutilities.exe"
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

# Same utilities binary under Start-menu executable names (non-Comms tools).
$utilExe = Join-Path $out "LCARSutilities.exe"
if (Test-Path $utilExe) {
    foreach ($alias in @(
            "LCARSCalculator.exe", "LCARSTimer.exe", "LCARSScreenshot.exe",
            "LCARSWeather.exe")) {
        Copy-Item $utilExe (Join-Path $out $alias) -Force
    }
}

$mailBin = Join-Path $repoRoot "LCARSMail\LCARSMail\bin\$Configuration"
$mailDeploy = @(
    "LCARScomms.exe",
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
    "System.Data.SQLite.dll"
)
foreach ($name in $mailDeploy) {
    $src = Join-Path $mailBin $name
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $out $name) -Force
    }
}
# Comms aliases: old Mail name + Calendar/RSS/Chat launch the same app (mode from exe name).
$commsExe = Join-Path $out "LCARScomms.exe"
if (Test-Path $commsExe) {
    foreach ($alias in @("LCARSMail.exe", "LCARSCalendar.exe", "LCARSRss.exe", "LCARSChat.exe")) {
        Copy-Item $commsExe (Join-Path $out $alias) -Force
    }
}
$sqliteInteropX86 = Join-Path $mailBin "x86\SQLite.Interop.dll"
if (Test-Path $sqliteInteropX86) {
    $x86Out = Join-Path $out "x86"
    if (-not (Test-Path $x86Out)) {
        New-Item -ItemType Directory -Force -Path $x86Out | Out-Null
    }
    Copy-Item $sqliteInteropX86 (Join-Path $x86Out "SQLite.Interop.dll") -Force
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

# Final guard: satellites built after main may have rebuilt LCARS.dll as CLR 4.
# Force net35 again and copy into the install folder Package-LCARSUpdate reads.
Write-Host "Re-asserting LCARS.dll as net35 / CLR 2..."
& $msbuild $buttonsProj /t:Rebuild /p:Configuration=$Configuration /p:Platform=AnyCPU /p:TargetFrameworkVersion=v3.5 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "LCARSbuttons final net35 rebuild failed with exit code $LASTEXITCODE" }
if (-not (Test-Path $buttonsDll)) { throw "LCARS.dll missing after net35 rebuild: $buttonsDll" }
Copy-Item $buttonsDll (Join-Path $out "LCARS.dll") -Force

$runtimeCheck = @"
using System;
using System.Reflection;
class P {
  static int Main(string[] a) {
    var asm = Assembly.LoadFrom(a[0]);
    Console.WriteLine(asm.ImageRuntimeVersion);
    return asm.ImageRuntimeVersion.StartsWith("v2.") ? 0 : 1;
  }
}
"@
$checkSrc = Join-Path $env:TEMP "LcarsClrCheck.cs"
$checkExe = Join-Path $env:TEMP "LcarsClrCheck.exe"
Set-Content -Path $checkSrc -Value $runtimeCheck -Encoding ASCII
& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /out:$checkExe $checkSrc
if ($LASTEXITCODE -ne 0) { throw "Failed to compile LCARS.dll runtime check" }
$deployDll = Join-Path $out "LCARS.dll"
$imageRuntime = (& $checkExe $deployDll).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "LCARS.dll at $deployDll is $imageRuntime — must be CLR 2 (v2.x) for OSK/shutdown. Aborting."
}
Write-Host "LCARS.dll ImageRuntimeVersion=$imageRuntime (OK)"

Write-Host ""
Write-Host "Build complete. Install folder: $out"
Write-Host "Fresh install package: powershell -File tools\Build-LCARSInstall.ps1 -Configuration $Configuration"
Write-Host "Update package only:   powershell -File tools\Package-LCARSUpdate.ps1 -Configuration $Configuration"
