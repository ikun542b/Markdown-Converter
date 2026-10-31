Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $repoRoot 'resources\icons'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$outputPath = Join-Path $outputDirectory 'app.ico'

function New-RoundedPath([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 256.0
    $background = New-RoundedPath (8 * $scale) (8 * $scale) (240 * $scale) (240 * $scale) (52 * $scale)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 31, 78, 121))
    $graphics.FillPath($brush, $background)

    $paper = New-RoundedPath (66 * $scale) (48 * $scale) (124 * $scale) (160 * $scale) (10 * $scale)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $graphics.FillPath($white, $paper)

    $linePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 46, 116, 181), [Math]::Max(2 * $scale, 1))
    $graphics.DrawLine($linePen, 88 * $scale, 92 * $scale, 168 * $scale, 92 * $scale)
    $graphics.DrawLine($linePen, 88 * $scale, 116 * $scale, 168 * $scale, 116 * $scale)
    $graphics.DrawLine($linePen, 88 * $scale, 140 * $scale, 148 * $scale, 140 * $scale)

    $arrowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 31, 78, 121))
    $arrow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $arrow.AddPolygon(@(
        (New-Object System.Drawing.PointF((108 * $scale), (158 * $scale))),
        (New-Object System.Drawing.PointF((148 * $scale), (158 * $scale))),
        (New-Object System.Drawing.PointF((128 * $scale), (186 * $scale)))
    ))
    $graphics.FillPath($arrowBrush, $arrow)

    $graphics.Dispose()
    return $bitmap
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = New-Object System.Collections.Generic.List[byte[]]
$preview = $null

foreach ($size in $sizes) {
    $bitmap = New-IconBitmap $size
    if ($size -eq 256) { $preview = $bitmap.Clone() }

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($stream.ToArray())
    $stream.Dispose()
    $bitmap.Dispose()
}

$file = [System.IO.File]::Create($outputPath)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

$offset = 6 + (16 * $images.Count)
for ($index = 0; $index -lt $images.Count; $index++) {
    $size = $sizes[$index]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$images[$index].Length)
    $writer.Write([UInt32]$offset)
    $offset += $images[$index].Length
}

foreach ($image in $images) {
    $writer.Write($image)
}

$writer.Dispose()
$file.Dispose()

if ($null -ne $preview) {
    $preview.Save((Join-Path $outputDirectory 'app-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    $preview.Dispose()
}

Write-Host "Icon written to $outputPath"
