# ONVIF Device Manager - Release Package Creator
# Creates ZIP files for GitHub Releases (filenames include version + local date/time stamp).

param(
    [string]$Version = "2.0.0",
    [string]$BuildStamp = "",
    # Used to print SOP "latest builds" direct URLs (releases/download/...).
    [string]$GithubRepo = "devildog5x5/ONVIF-ODM"
)

$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
$OutputDir = Join-Path $RootDir "publish"
if ([string]::IsNullOrWhiteSpace($BuildStamp)) {
    $BuildStamp = Get-Date -Format "yyyyMMdd-HHmmss"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ONVIF Device Manager Release Packager" -ForegroundColor Cyan
Write-Host "  Version: $Version  |  Build stamp: $BuildStamp" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ensure the app icon is always regenerated from the master source image.
$iconSyncScript = Join-Path $RootDir "update-app-icon.ps1"
if (Test-Path $iconSyncScript) {
    Write-Host "Refreshing warrior icon from branding/master-icon.png..." -ForegroundColor Yellow
    try {
        & $iconSyncScript
    } catch {
        Write-Host "WARNING: Icon refresh failed: $_" -ForegroundColor Yellow
    }
    if (Test-Path (Join-Path $RootDir "warrior_icon.ico")) {
        Write-Host "Icon refresh complete." -ForegroundColor Green
    }
}
Write-Host ""

# Build release artifacts if publish folder doesn't exist
$wpfFolder = Join-Path $OutputDir "OnvifDeviceManager-Wpf-win-x64"
if (-not (Test-Path $wpfFolder)) {
    Write-Host "Running build to create publish artifacts..." -ForegroundColor Yellow
    & (Join-Path (Join-Path $RootDir "build") "build-all.ps1") -Version $Version -BuildStamp $BuildStamp
    if (-not (Test-Path $wpfFolder)) {
        Write-Host "ERROR: Build failed or output not found." -ForegroundColor Red
        exit 1
    }
}

& (Join-Path (Join-Path $RootDir "build") "repair-win-apphost.ps1") -PublishDir $wpfFolder -ExeBaseName "OnvifDeviceManager.Wpf"
Write-Host ""

# Installer (Inno output may include date/time stamp — match any for this version)
$installerDir = Join-Path $OutputDir "installers"
$installerPath = $null
if (Test-Path $installerDir) {
    $installerPath = Get-ChildItem -Path $installerDir -Filter "OnvifDeviceManager-Wpf-Setup-$Version*.exe" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
$hasInstaller = -not [string]::IsNullOrWhiteSpace($installerPath) -and (Test-Path $installerPath)

# Optional: Sign binaries if script exists
$signScript = Join-Path $RootDir "sign-binaries.ps1"
if (Test-Path $signScript) {
    Write-Host "Signing release binaries..." -ForegroundColor Yellow
    $filesToSign = @(
        (Join-Path $wpfFolder "OnvifDeviceManager.Wpf.exe")
    )
    if ($hasInstaller) { $filesToSign += $installerPath }
    try {
        & $signScript -FilePaths $filesToSign -SkipMissing 2>$null
        Write-Host "Signing complete." -ForegroundColor Green
    } catch {
        Write-Host "WARNING: Signing skipped: $_" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "Creating release packages..." -ForegroundColor Yellow
Write-Host ""

# Create WPF portable ZIP
$wpfZip = Join-Path $RootDir "OnvifDeviceManager-Wpf-win-x64-v$Version-$BuildStamp.zip"
if (Test-Path $wpfZip) { Remove-Item $wpfZip -Force }
Compress-Archive -Path (Join-Path $wpfFolder "*") -DestinationPath $wpfZip -Force
$wpfSize = [math]::Round((Get-Item $wpfZip).Length / 1MB, 2)
Write-Host "  [OK] $wpfZip ($wpfSize MB)" -ForegroundColor Green

# List other existing zips
Get-ChildItem -Path $OutputDir -Filter "*.zip" | Where-Object { $_.Name -ne (Split-Path $wpfZip -Leaf) } | ForEach-Object {
    Write-Host "  [OK] $($_.Name) ($([math]::Round($_.Length/1MB,2)) MB)" -ForegroundColor Green
}

if ($hasInstaller) {
    $installerSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
    Write-Host ""
    Write-Host "Installer: $installerPath ($installerSize MB)" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "WARNING: Installer not found. Build with Inno Setup: build\installers\OnvifDeviceManager-Wpf-Setup.iss" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Release Packages Ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps (see README: Release Build SOP + After every new binary build):" -ForegroundColor Cyan
Write-Host "  1. Upload ZIPs to GitHub Release tag v$Version (remove older timestamped ZIPs on that tag if obsolete)." -ForegroundColor White
Write-Host "  2. Update README **Latest direct download links** + build stamp + Key files table (URLs printed below)." -ForegroundColor White
Write-Host "  3. Send users the same verbatim URLs (hotfix SOP)." -ForegroundColor White
Write-Host ""
Write-Host "SOP checklist (echo in PR/chat with done/N/A):" -ForegroundColor Yellow
Write-Host "  [A] Publish + package same stamp: v$Version-$BuildStamp" -ForegroundColor White
Write-Host "  [B] Icon refresh (this script ran update-app-icon.ps1)" -ForegroundColor White
Write-Host "  [C] Inno installer: $(if ($hasInstaller) { 'built' } else { 'N/A this run' })" -ForegroundColor White
Write-Host "  [D] GitHub Release upload + prune old assets" -ForegroundColor White
Write-Host "  [E] README links + dates table" -ForegroundColor White
Write-Host "  [F] User-facing direct URLs" -ForegroundColor White
Write-Host "  [G] dotnet build -c Release clean" -ForegroundColor White
Write-Host ""
$releaseBase = "https://github.com/$GithubRepo/releases/download/v$Version"
$platformZips = @(
    "OnvifDeviceManager-Wpf-win-x64-v$Version-$BuildStamp.zip",
    "OnvifDeviceManager-Avalonia-win-x64-v$Version-$BuildStamp.zip",
    "OnvifDeviceManager-Avalonia-linux-x64-v$Version-$BuildStamp.zip",
    "OnvifDeviceManager-Avalonia-osx-x64-v$Version-$BuildStamp.zip",
    "OnvifDeviceManager-Avalonia-osx-arm64-v$Version-$BuildStamp.zip"
)
Write-Host "Latest builds — direct links (SOP — use after assets are on GitHub Release v$Version):" -ForegroundColor Cyan
Write-Host "Copy into README **Latest direct download links**, release notes, and user handoff." -ForegroundColor DarkGray
foreach ($name in $platformZips) {
    Write-Host "$releaseBase/$name" -ForegroundColor White
}
Write-Host ""
