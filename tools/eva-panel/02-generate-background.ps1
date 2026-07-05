# tools/eva-panel/02-generate-background.ps1
param([string]$SourceImage = 'C:\Users\soste\Downloads\616750.jpg')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null

if (-not (Test-Path $SourceImage)) {
    throw "Imagem de origem nao encontrada: $SourceImage"
}

$W = 3840; $H = 1100
$src = [System.Drawing.Image]::FromFile($SourceImage)

# Crop: faixa 3,49:1 da imagem, 28% a partir do topo da folga vertical
$cropH = [int]($src.Width * $H / $W)              # 5563 * 1100/3840 = 1594
$cropY = [int](0.28 * ($src.Height - $cropH))     # ~567
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = 'HighQualityBicubic'
$g.DrawImage($src, (New-Object System.Drawing.Rectangle(0,0,$W,$H)),
    (New-Object System.Drawing.Rectangle(0,$cropY,$src.Width,$cropH)),
    [System.Drawing.GraphicsUnit]::Pixel)
$src.Dispose()

# Gradiente: esquerda escura (#040612) -> transparente aos 68% da largura
$rect = New-Object System.Drawing.Rectangle(0,0,$W,$H)
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect, [System.Drawing.Color]::FromArgb(245,4,6,18),
    [System.Drawing.Color]::FromArgb(0,4,6,18), 0.0)
$blend = New-Object System.Drawing.Drawing2D.ColorBlend(4)
$blend.Colors = @(
    [System.Drawing.Color]::FromArgb(245,4,6,18),
    [System.Drawing.Color]::FromArgb(240,4,6,18),
    [System.Drawing.Color]::FromArgb(0,4,6,18),
    [System.Drawing.Color]::FromArgb(0,4,6,18))
$blend.Positions = @(0.0, 0.42, 0.68, 1.0)
$grad.InterpolationColors = $blend
$g.FillRectangle($grad, $rect)
$grad.Dispose()

# Molduras das 6 células (roxo translúcido, 2px)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110,138,99,210), 2)
foreach ($cy in 480, 762) { foreach ($cx in 96, 768, 1440) {
    $g.DrawRectangle($pen, $cx, $cy, 640, 250)
}}
$pen.Dispose(); $g.Dispose()

$bmp.Save((Join-Path $outDir 'eva-bg.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "OK: eva-bg.png gerado"
