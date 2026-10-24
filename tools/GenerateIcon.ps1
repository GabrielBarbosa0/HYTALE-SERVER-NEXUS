$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path $PSScriptRoot '..\src\HytaleServerManager\Assets\HytaleServerManager.ico'
$outputDirectory = Split-Path -Parent $outputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $padding = [single]($size * 0.06)
    $diameter = [single]($size * 0.34)
    $rect = New-Object System.Drawing.RectangleF($padding, $padding, ($size - 2 * $padding), ($size - 2 * $padding))
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.Left, $rect.Top, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Top, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.Left, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    $blue = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 37, 131, 255))
    $graphics.FillPath($blue, $path)

    $font = New-Object System.Drawing.Font('Segoe UI', [single]($size * 0.52), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $graphics.DrawString('H', $font, $white, $rect, $format)

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()

    $format.Dispose(); $white.Dispose(); $font.Dispose(); $blue.Dispose(); $path.Dispose()
    $graphics.Dispose(); $bitmap.Dispose(); $stream.Dispose()
    ,$bytes
}

$file = [System.IO.File]::Create($outputPath)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)
$offset = 6 + (16 * $sizes.Count)

for ($index = 0; $index -lt $sizes.Count; $index++) {
    $size = $sizes[$index]
    $bytes = $images[$index]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$bytes.Length)
    $writer.Write([uint32]$offset)
    $offset += $bytes.Length
}

foreach ($bytes in $images) { $writer.Write($bytes) }
$writer.Dispose()
$file.Dispose()
[System.IO.File]::WriteAllBytes((Join-Path $outputDirectory 'HytaleServerManager.png'), $images[-1])
Write-Host "Icone criado em $outputPath"
