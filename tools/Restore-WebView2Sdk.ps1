# Restores the Microsoft.Web.WebView2 SDK from nuget.org during build.
# Runtime install is separate: LCARS downloads Microsoft's Evergreen installer
# on first boot when the runtime is missing.
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$nugetPath = Join-Path $PSScriptRoot "nuget.exe"
$packagesConfig = Join-Path $repoRoot "Lcars Web Browser\packages.config"
$packagesDir = Join-Path $repoRoot "Lcars Web Browser\packages"

if (-not (Test-Path $packagesConfig)) {
    throw "packages.config not found: $packagesConfig"
}

if (-not (Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe from nuget.org..."
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
}

Write-Host "Restoring WebView2 SDK into $packagesDir"
& $nugetPath restore $packagesConfig -PackagesDirectory $packagesDir
if ($LASTEXITCODE -ne 0) {
    throw "nuget restore failed with exit code $LASTEXITCODE"
}

Write-Host "WebView2 SDK restore complete."
