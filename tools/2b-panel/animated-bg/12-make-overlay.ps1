# Overlay RGBA 1100x3840: fades + separador em transparente (mesma geometria do
# tools/2b-panel/01-generate-background.ps1, commit 487a098: separador em y3330)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$W = 1100; $H = 3840
$bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::Transparent)

# Faixas solidas fora da zona da arte (y<192 e y>2880): fundo opaco
$bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,245,244,242))
$g.FillRectangle($bg, 0, 0, $W, 192)
$g.FillRectangle($bg, 0, 2881, $W, $H-2881)
$bg.Dispose()

# Fade topo y192..576
$rTop = New-Object System.Drawing.Rectangle(0,192,1100,384)
$gTop = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rTop, [System.Drawing.Color]::FromArgb(255,245,244,242),
    [System.Drawing.Color]::FromArgb(0,245,244,242), 90.0)
$g.FillRectangle($gTop, $rTop); $gTop.Dispose()

# Fade base y2112..2880
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

# Separador RAM/REDE (y3330 pos-shift 487a098)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(38,28,28,28), 2)
$g.DrawLine($pen, 110, 3330, 990, 3330)
$pen.Dispose(); $g.Dispose()

$out = Join-Path $PSScriptRoot '2b_overlay.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "OK: $out"
