# Recover black desktop after mid-update hard reboot
# Copy this to the tablet (any path) and run from Task Manager → Run new task:
#   powershell -ExecutionPolicy Bypass -File Recover-LcarsShell.ps1
#
# Or from an attached keyboard: Ctrl+Shift+Esc → File → Run new task → powershell

$ErrorActionPreference = 'Continue'

Write-Host '=== LCARS shell recovery ==='

# 1) Desktop back
$explorer = Join-Path $env:WINDIR 'explorer.exe'
if (Test-Path $explorer) {
    Start-Process $explorer
    Write-Host 'Started explorer.exe'
} else {
    Start-Process 'explorer.exe'
}

# 2) Clear stuck update flags (this is why Deploy copy alone did not help)
$flagDir = 'C:\ProgramData\LCARS x32'
@(
    'update-in-progress.flag',
    'pending-shell-restart.flag'
) | ForEach-Object {
    $p = Join-Path $flagDir $_
    if (Test-Path $p) {
        Remove-Item $p -Force
        Write-Host "Removed $p"
    } else {
        Write-Host "No flag: $p"
    }
}

# 3) Find LCARSmain from Winlogon Shell or InstallPath
$shell = $null
try {
    $shell = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon' -ErrorAction Stop).Shell
} catch {}
Write-Host "HKCU Shell = $shell"

$installPath = $null
try {
    $installPath = (Get-ItemProperty 'HKCU:\Software\VB and VBA Program Settings\LCARS x32\Application' -ErrorAction Stop).InstallPath
} catch {}
Write-Host "InstallPath = $installPath"

$candidates = @()
if ($shell -match '(?i)(.+LCARSmain\.exe)') {
    $candidates += $Matches[1].Trim('"')
}
if ($installPath) {
    $candidates += (Join-Path $installPath 'LCARSmain.exe')
}
$candidates += 'C:\Program Files\LCARS x32\LCARSmain.exe'
$candidates += 'D:\LCARS x32\LCARSmain.exe'

$lcars = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $lcars) {
    Write-Host 'ERROR: Could not find LCARSmain.exe. Set $lcars manually and re-run.'
    Write-Host 'Searched:'
    $candidates | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Launching: $lcars -u"
Start-Process -FilePath $lcars -ArgumentList '-u' -WorkingDirectory (Split-Path $lcars)
Write-Host 'Done. If LCARS does not appear, check C:\ProgramData\LCARS x32\shell-diagnostics.log'
