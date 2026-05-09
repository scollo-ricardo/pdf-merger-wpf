# Builds a multi-resolution .ico from a high-res PNG.
# Embeds each size as PNG (Vista+ supports this, gives crisp 256x256).

param(
    [string]$Source = "$PSScriptRoot\PDFMerger\Assets\logo.png",
    [string]$Destination = "$PSScriptRoot\PDFMerger\Assets\icon.ico"
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256

$src = [System.Drawing.Image]::FromFile($Source)
$pngBytes = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.DrawImage($src, 0, 0, $size, $size)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes += , $ms.ToArray()
    $ms.Dispose()
    $bmp.Dispose()
}
$src.Dispose()

# Build ICO container
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out

# ICONDIR: reserved (2), type=1 icon (2), count (2)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Length)

# Each ICONDIRENTRY = 16 bytes; data starts after header(6) + entries(16*n)
$dataOffset = 6 + (16 * $sizes.Length)

for ($i = 0; $i -lt $sizes.Length; $i++) {
    $size = $sizes[$i]
    $data = $pngBytes[$i]
    $w = if ($size -ge 256) { 0 } else { $size }
    $h = if ($size -ge 256) { 0 } else { $size }

    $bw.Write([byte]$w)              # width (0 = 256)
    $bw.Write([byte]$h)              # height (0 = 256)
    $bw.Write([byte]0)               # color count (0 for >256 colors)
    $bw.Write([byte]0)               # reserved
    $bw.Write([uint16]1)             # planes
    $bw.Write([uint16]32)            # bits per pixel
    $bw.Write([uint32]$data.Length)  # bytes in resource
    $bw.Write([uint32]$dataOffset)   # offset to image data

    $dataOffset += $data.Length
}

# Image data
foreach ($data in $pngBytes) { $bw.Write($data) }

[System.IO.File]::WriteAllBytes($Destination, $out.ToArray())
$bw.Dispose()
$out.Dispose()

$kb = [math]::Round((Get-Item $Destination).Length / 1KB, 1)
Write-Host "Wrote $Destination ($kb KB, $($sizes.Length) sizes: $($sizes -join ', '))" -ForegroundColor Green
