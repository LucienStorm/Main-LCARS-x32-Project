# Upload LCARS update files to the standalone file server on drepowernode.
# This does NOT use Home Assistant — files are served on port 8765 only.
#
# Usage:
#   powershell -File tools\Package-LCARSUpdate.ps1
#   powershell -File tools\Upload-LCARSUpdate.ps1

param(
    [string]$DeployFolder = "",
    [string]$RemoteHost = "drepowernode",
    [string]$RemotePath = "~/lcars-update"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "Build\update-version.txt"
$version = "unknown"
if (Test-Path $versionFile) {
    $version = (Get-Content $versionFile -Raw).Trim()
}

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

Write-Host "Done. Tablet Custom URL:"
Write-Host "  http://192.168.68.120:8765/CustomVersion.txt"
