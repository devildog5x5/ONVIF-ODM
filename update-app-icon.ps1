param(
    [string]$SourcePng = "branding/master-icon.png",
    [string]$OutputIco = "warrior_icon.ico"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourcePng)) {
    throw "Source icon PNG not found: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

$iconDestroyType = @"
using System;
using System.Runtime.InteropServices;
public static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);
}
"@

Add-Type -TypeDefinition $iconDestroyType -Language CSharp

$sourceImage = $null
$canvas = $null
$graphics = $null
$fileStream = $null
$icon = $null
$hIcon = [IntPtr]::Zero

try {
    $sourceImage = [System.Drawing.Image]::FromFile($SourcePng)
    $canvas = New-Object System.Drawing.Bitmap(256, 256)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($sourceImage, 0, 0, 256, 256)

    $hIcon = $canvas.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)

    $outputDir = Split-Path -Path $OutputIco -Parent
    if (-not [string]::IsNullOrWhiteSpace($outputDir) -and -not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $fileStream = New-Object System.IO.FileStream($OutputIco, [System.IO.FileMode]::Create)
    $icon.Save($fileStream)
}
finally {
    if ($fileStream) { $fileStream.Close() }
    if ($icon) { $icon.Dispose() }
    if ($hIcon -ne [IntPtr]::Zero) { [NativeMethods]::DestroyIcon($hIcon) | Out-Null }
    if ($graphics) { $graphics.Dispose() }
    if ($canvas) { $canvas.Dispose() }
    if ($sourceImage) { $sourceImage.Dispose() }
}

Write-Host "Updated application icon: $OutputIco" -ForegroundColor Green
