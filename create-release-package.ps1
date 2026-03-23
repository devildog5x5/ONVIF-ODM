# ONVIF Device Manager - Release Package Creator
# Creates ZIP files for GitHub Releases

$ErrorActionPreference = "Stop"

$Version = "1.5.0"
$RootDir = $PSScriptRoot
$OutputDir = Join-Path $RootDir "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ONVIF Device Manager Release Packager" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
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
    & (Join-Path (Join-Path $RootDir "build") "build-all.ps1")
    if (-not (Test-Path $wpfFolder)) {
        Write-Host "ERROR: Build failed or output not found." -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Check installer
$installerPath = Join-Path (Join-Path $OutputDir "installers") "OnvifDeviceManager-Wpf-Setup-$Version.exe"
$hasInstaller = Test-Path $installerPath

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
$wpfZip = Join-Path $RootDir "OnvifDeviceManager-Wpf-win-x64-v$Version.zip"
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
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Go to: https://github.com/devildog5x5/ONVIF-ODM/releases/new" -ForegroundColor White
Write-Host "  2. Create a new release (e.g., 'v$Version')" -ForegroundColor White
Write-Host "  3. Upload the ZIP files from publish\ as release assets" -ForegroundColor White
Write-Host "  4. Update README: links, version, and Key files table dates (run snippet in README)" -ForegroundColor White
Write-Host ""
