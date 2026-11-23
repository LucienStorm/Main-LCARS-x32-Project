# Regenerates x86 MSTSC ActiveX interop into LCARSTerminal/lib
param()
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$lib = Join-Path $repoRoot "LCARSTerminal\LCARSTerminal\lib"
New-Item -ItemType Directory -Force -Path $lib | Out-Null
$aximpCandidates = @(
  "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\aximp.exe",
  "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\aximp.exe"
)
$aximp = $aximpCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $aximp) { throw "aximp.exe not found" }
$mstscax = "$env:SystemRoot\SysWOW64\mstscax.dll"
if (-not (Test-Path $mstscax)) { $mstscax = "$env:SystemRoot\System32\mstscax.dll" }
Push-Location $lib
& $aximp $mstscax
Pop-Location
Write-Host "Generated interop in $lib"
