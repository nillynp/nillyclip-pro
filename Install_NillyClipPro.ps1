# ============================================================================
# NillyClip Pro — Universal Windows 10/11 Installer (Zero Admin · Portable)
# created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
# Ultra-Light Clipboard History, Pinned Snippets & DPAPI-Encrypted API Key Vault
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $root 'NillyClipPro.exe'
$icoPath = Join-Path $root 'NillyClipPro.ico'
$srcPath = Join-Path $root 'desktop\NillyClipPro.cs'

Write-Host ''
Write-Host '  ============================================================' -ForegroundColor Cyan
Write-Host '   NillyClip Pro — Clipboard History & DPAPI API Keys Vault' -ForegroundColor White
Write-Host '   created by Nilly graphics (Sotiris Christos)' -ForegroundColor DarkGray
Write-Host '   https://nillygraphics.com/' -ForegroundColor DarkGray
Write-Host '  ============================================================' -ForegroundColor Cyan
Write-Host ''

# 1. Unblock files if extracted from GitHub Portable ZIP
Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue

# 2. Auto-compile NillyClipPro.exe if missing or older than source
$needBuild = (-not (Test-Path $exePath)) -or ((Test-Path $srcPath) -and ((Get-Item $srcPath).LastWriteTimeUtc -gt (Get-Item $exePath).LastWriteTimeUtc))
if ($needBuild) {
    $fxDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
    if (-not (Test-Path "$fxDir\csc.exe")) { $fxDir = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319' }
    $csc = Join-Path $fxDir 'csc.exe'
    if (-not (Test-Path $csc)) { throw 'Δεν βρέθηκε ο .NET Framework 4.x C# compiler (csc.exe).' }
    Write-Host '  [1/3] Μεταγλώττιση NillyClipPro.exe (Native DPAPI Vault + Win32 Listener)...' -ForegroundColor Yellow
    $cscArgs = @(
        '/nologo', '/target:winexe', '/optimize+', '/utf8output', '/platform:anycpu', "/out:$exePath",
        '/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Security.dll'
    )
    if (Test-Path $icoPath) { $cscArgs += "/win32icon:$icoPath" }
    $cscArgs += $srcPath
    & $csc @cscArgs
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exePath)) {
        throw "Αποτυχία μεταγλώττισης NillyClipPro.exe (exit $LASTEXITCODE)"
    }
    Write-Host ("        OK -> " + [math]::Round((Get-Item $exePath).Length / 1KB, 1) + " KB") -ForegroundColor Green
} else {
    Write-Host '  [1/3] NillyClipPro.exe έτοιμο.' -ForegroundColor Green
}

# 3. Windows Auto-Start in Background Tray (-tray)
Write-Host '  [2/3] Προσθήκη στην αυτόματη εκκίνηση Windows (System Tray · 0% idle CPU)...' -ForegroundColor Yellow
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
New-ItemProperty -Path $runKey -Name 'NillyClipPro' -Value "`"$exePath`" -tray" -PropertyType String -Force | Out-Null
Write-Host '        OK -> Αυτόματη εκκίνηση έτοιμη!' -ForegroundColor Green

# 4. Desktop & Start Menu Shortcuts
Write-Host '  [3/3] Δημιουργία συντομεύσεων (Επιφάνεια Εργασίας & Start Menu)...' -ForegroundColor Yellow
$wsh = New-Object -ComObject WScript.Shell
$desktopDir = [Environment]::GetFolderPath('Desktop')
$startMenuDir = Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics'
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

foreach ($lnkPath in @(
    (Join-Path $desktopDir 'NillyClip Pro.lnk'),
    (Join-Path $startMenuDir 'NillyClip Pro.lnk')
)) {
    $sc = $wsh.CreateShortcut($lnkPath)
    $sc.TargetPath = $exePath
    $sc.WorkingDirectory = $root
    $sc.Description = 'NillyClip Pro — Clipboard History, Pinned Snippets & DPAPI-Encrypted API Keys Vault | created by Nilly graphics (Sotiris Christos)'
    $sc.IconLocation = "$exePath,0"
    $sc.Save()
}
Write-Host '        OK -> Έτοιμες οι συντομεύσεις!' -ForegroundColor Green
Write-Host ''
Write-Host '  ΟΛΟΚΛΗΡΩΘΗΚΕ! Πάτα Ctrl + Shift + V οπουδήποτε για άνοιγμα του NillyClip Pro.' -ForegroundColor Cyan
Write-Host ''

if (-not $Silent) {
    Start-Process -FilePath $exePath
}
