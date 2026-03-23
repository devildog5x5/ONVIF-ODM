param(
    [string]$SourcePng = "branding/master-icon.png",
    [string]$OutputIco = "warrior_icon.ico"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourcePng)) {
    throw "Source icon PNG not found: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

function Write-MultiSizeIcoFromPng {
    param(
        [string]$PngPath,
        [string]$IcoPath,
        [int[]]$Sizes = @(16, 24, 32, 48, 64, 128, 256)
    )

    $sourceImage = $null
    $pngByteArrays = New-Object System.Collections.Generic.List[byte[]]

    try {
        $sourceImage = [System.Drawing.Image]::FromFile($PngPath)

        foreach ($s in $Sizes) {
            $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                $g.Clear([System.Drawing.Color]::Transparent)
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $g.DrawImage($sourceImage, 0, 0, $s, $s)
            }
            finally {
                $g.Dispose()
            }

            $ms = New-Object System.IO.MemoryStream
            try {
                $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngByteArrays.Add($ms.ToArray())
            }
            finally {
                $ms.Dispose()
                $bmp.Dispose()
            }
        }
    }
    finally {
        if ($sourceImage) { $sourceImage.Dispose() }
    }

    $count = $pngByteArrays.Count
    $headerSize = 6 + (16 * $count)
    $offset = $headerSize

    $fs = [System.IO.File]::Create($IcoPath)
    $bw = New-Object System.IO.BinaryWriter($fs)
    try {
        $bw.Write([UInt16]0)
        $bw.Write([UInt16]1)
        $bw.Write([UInt16]$count)

        foreach ($i in 0..($count - 1)) {
            $dim = $Sizes[$i]
            $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
            $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
            $bw.Write([byte]0)
            $bw.Write([byte]0)
            # PNG payload: planes/bit depth often 0 per ICO+PNG convention (Windows Vista+)
            $bw.Write([UInt16]0)
            $bw.Write([UInt16]0)
            $bw.Write([UInt32]$pngByteArrays[$i].Length)
            $bw.Write([UInt32]$offset)
            $offset += $pngByteArrays[$i].Length
        }

        foreach ($bytes in $pngByteArrays) {
            $bw.Write($bytes)
        }
    }
    finally {
        $bw.Close()
    }
}

# Prefer ImageMagick when installed (optional best-quality path)
$magick = Get-Command magick -ErrorAction SilentlyContinue
if ($magick) {
    & magick $SourcePng -define icon:auto-resize=256,128,96,64,48,32,24,16 $OutputIco
    Write-Host "Updated application icon via ImageMagick: $OutputIco" -ForegroundColor Green
    return
}

Write-MultiSizeIcoFromPng -PngPath $SourcePng -IcoPath $OutputIco
Write-Host "Updated application icon (multi-size PNG/ICO, alpha preserved): $OutputIco" -ForegroundColor Green
