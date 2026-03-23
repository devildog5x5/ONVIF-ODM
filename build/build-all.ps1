$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$OutputDir = Join-Path $RootDir "publish"
$Version = "1.5.0"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " ONVIF Device Manager - Build All Platforms" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Path $OutputDir | Out-Null

function Publish-Project {
    param(
        [string]$Project,
        [string]$Rid,
        [string]$Name
    )
    $out = Join-Path $OutputDir $Name
    Write-Host ">> Publishing $Name ($Rid)..." -ForegroundColor Yellow
    $projPath = Join-Path (Join-Path $RootDir "src") $Project
    dotnet publish $projPath `
        -c Release `
        -r $Rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=false `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version `
        -o $out `
        --nologo -v quiet
    Write-Host "   Done: $out" -ForegroundColor Green
}

Write-Host "[1/5] Building WPF (Windows x64)..." -ForegroundColor White
Publish-Project "OnvifDeviceManager.Wpf\OnvifDeviceManager.Wpf.csproj" "win-x64" "OnvifDeviceManager-Wpf-win-x64"

Write-Host "[2/5] Building Avalonia (Windows x64)..." -ForegroundColor White
Publish-Project "OnvifDeviceManager\OnvifDeviceManager.csproj" "win-x64" "OnvifDeviceManager-Avalonia-win-x64"

Write-Host "[3/5] Building Avalonia (Linux x64)..." -ForegroundColor White
Publish-Project "OnvifDeviceManager\OnvifDeviceManager.csproj" "linux-x64" "OnvifDeviceManager-Avalonia-linux-x64"

Write-Host "[4/5] Building Avalonia (macOS x64)..." -ForegroundColor White
Publish-Project "OnvifDeviceManager\OnvifDeviceManager.csproj" "osx-x64" "OnvifDeviceManager-Avalonia-osx-x64"

Write-Host "[5/5] Building Avalonia (macOS ARM64)..." -ForegroundColor White
Publish-Project "OnvifDeviceManager\OnvifDeviceManager.csproj" "osx-arm64" "OnvifDeviceManager-Avalonia-osx-arm64"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Creating ZIP archives..." -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Get-ChildItem -Path $OutputDir -Directory | ForEach-Object {
    $zipPath = Join-Path $OutputDir "$($_.Name)-v$Version.zip"
    Write-Host ">> Archiving $($_.Name)..." -ForegroundColor Yellow
    Compress-Archive -Path $_.FullName -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " Build complete! Output:" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Get-ChildItem -Path $OutputDir -Filter "*.zip" | Format-Table Name, @{N="Size(MB)";E={[math]::Round($_.Length/1MB,1)}}
Write-Host "Done." -ForegroundColor Green
