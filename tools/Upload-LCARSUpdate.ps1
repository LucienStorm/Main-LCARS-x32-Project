# Upload LCARS update files to the standalone update file server.
# This does NOT use Home Assistant — files are served on the configured port only.
#
# Host/URL defaults come from Build\update-server.local.ps1 (gitignored), not this file.
#
# Usage:
#   powershell -File tools\Package-LCARSUpdate.ps1
#   powershell -File tools\Upload-LCARSUpdate.ps1

param(
    [string]$DeployFolder = "",
    [string]$RemoteHost = "",
    [string]$RemotePath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "Build\update-version.txt"
$version = "unknown"
if (Test-Path $versionFile) {
    $version = (Get-Content $versionFile -Raw).Trim()
}

$localDefaults = Join-Path $repoRoot "Build\update-server.local.ps1"
if (Test-Path $localDefaults) {
    . $localDefaults
}
if (-not $RemoteHost -and $LcarsUpdateRemoteHost) { $RemoteHost = $LcarsUpdateRemoteHost }
if (-not $RemotePath -and $LcarsUpdateRemotePath) { $RemotePath = $LcarsUpdateRemotePath }
if (-not $RemotePath) { $RemotePath = "~/lcars-update" }
if (-not $RemoteHost) {
    throw "RemoteHost not set. Pass -RemoteHost or create Build\update-server.local.ps1 (see update-server.local.ps1.example)."
}

$publicBase = ""
if ($LcarsUpdateBaseUrl) { $publicBase = $LcarsUpdateBaseUrl }
if ($publicBase -and -not $publicBase.EndsWith("/")) { $publicBase += "/" }

if (-not $DeployFolder) {
    $safeVersion = ($version -replace "[^\d\.]", "_")
    $DeployFolder = Join-Path $repoRoot "Build\Deploy\LCARS-$safeVersion"
    if (-not (Test-Path $DeployFolder)) {
        # Fallback: newest Deploy folder
        $deployRoot = Join-Path $repoRoot "Build\Deploy"
        $latest = Get-ChildItem $deployRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if ($latest) { $DeployFolder = $latest.FullName }
    }
}

if (-not (Test-Path $DeployFolder)) {
    throw "Deploy folder not found: $DeployFolder. Run tools\Package-LCARSUpdate.ps1 first."
}

Write-Host "Uploading version $version from $DeployFolder to ${RemoteHost}:${RemotePath} ..."
# Upload files at root, then recurse directories (LibVLC lives under lib\vlc\).
Get-ChildItem $DeployFolder -File | ForEach-Object {
    scp -o BatchMode=yes $_.FullName "${RemoteHost}:${RemotePath}/"
    if ($LASTEXITCODE -ne 0) { throw "scp failed for $($_.Name)" }
}
Get-ChildItem $DeployFolder -Directory | ForEach-Object {
    Write-Host "  recursive: $($_.Name)/"
    scp -o BatchMode=yes -r $_.FullName "${RemoteHost}:${RemotePath}/"
    if ($LASTEXITCODE -ne 0) { throw "scp -r failed for $($_.Name)" }
}

Write-Host "Done."
if ($publicBase) {
    Write-Host "Tablet Custom URL:"
    Write-Host "  ${publicBase}CustomVersion.txt"
}
else {
    Write-Host "Set LcarsUpdateBaseUrl in Build\update-server.local.ps1 to print the tablet Custom URL."
}
