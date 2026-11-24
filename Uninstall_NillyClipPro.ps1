# ============================================================================
# NillyClip Pro — Clean Uninstaller (Zero Admin)
# created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'SilentlyContinue'
Write-Host ''
Write-Host '  Αφαίρεση συντομεύσεων & αυτόματης εκκίνησης του NillyClip Pro...' -ForegroundColor Yellow

Stop-Process -Name 'NillyClipPro' -Force -ErrorAction SilentlyContinue

$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'NillyClip Pro.lnk'
$startLnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics\NillyClip Pro.lnk'
if (Test-Path $desktopLnk) { Remove-Item $desktopLnk -Force }
if (Test-Path $startLnk) { Remove-Item $startLnk -Force }
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'NillyClipPro' -ErrorAction SilentlyContinue

Write-Host '  OK -> Αφαιρέθηκε καθαρά το NillyClip Pro (η κρυπτογραφημένη Κλειδοθήκη στο LocalAppData έμεινε ασφαλής).' -ForegroundColor Green
Write-Host ''
if (-not $Silent) { Read-Host '  Πάτησε Enter για κλείσιμο' }
