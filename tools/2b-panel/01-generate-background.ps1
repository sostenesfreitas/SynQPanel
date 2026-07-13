param([string]$SourceImage = 'C:\Users\soste\Downloads\wallpaperflare.com_wallpaper.jpg')
# tools/2b-panel/01-generate-background.ps1
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $SourceImage)) { throw "Wallpaper nao encontrado: $SourceImage" }
Add-Type -AssemblyName System.Drawing
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null

$W = 1100; $H = 3840
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = 'HighQualityBicubic'

# Base clara
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,245,244,242))
$g.FillRectangle($bgBrush, 0, 0, $W, $H)
$bgBrush.Dispose()

# Arte: y 192..2880 (h 2688), crop central do 1440x2560 -> src x196 w1048
$src = [System.Drawing.Image]::FromFile($SourceImage)
$g.DrawImage($src, (New-Object System.Drawing.Rectangle(0,192,1100,2688)),
    (New-Object System.Drawing.Rectangle(196,0,1048,2560)),
    [System.Drawing.GraphicsUnit]::Pixel)
$src.Dispose()

# Fade do topo (atras do relogio): branco -> transparente, y192..576
$rTop = New-Object System.Drawing.Rectangle(0,192,1100,384)
$gTop = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rTop, [System.Drawing.Color]::FromArgb(255,245,244,242),
    [System.Drawing.Color]::FromArgb(0,245,244,242), 90.0)
$g.FillRectangle($gTop, $rTop); $gTop.Dispose()

# Fade da base (atras dos sensores): transparente -> quase solido, y2112..2880
$rBot = New-Object System.Drawing.Rectangle(0,2112,1100,769)
$gBot = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rBot, [System.Drawing.Color]::FromArgb(0,245,244,242),
    [System.Drawing.Color]::FromArgb(242,245,244,242), 90.0)
$blend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
$blend.Colors = @(
    [System.Drawing.Color]::FromArgb(0,245,244,242),
    [System.Drawing.Color]::FromArgb(215,245,244,242),
    [System.Drawing.Color]::FromArgb(242,245,244,242))
$blend.Positions = @(0.0, 0.55, 1.0)
$gBot.InterpolationColors = $blend
$g.FillRectangle($gBot, $rBot); $gBot.Dispose()

$g.Dispose()

$bmp.Save((Join-Path $outDir '2b-bg.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host 'OK: 2b-bg.png gerado'
