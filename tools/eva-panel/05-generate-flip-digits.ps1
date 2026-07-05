# tools/eva-panel/05-generate-flip-digits.ps1
# Gera as placas de dígitos do relógio split-flap: 00.png..59.png (210x270)
# em out\flip-digits\. O FlipDisplayItem (SplitFlap) carrega
# "<valor com 2 dígitos>.png" de assets\<GUID>\<ImageFolder> em runtime
# (PanelDraw.cs), então minutos 00-59 cobrem também as horas 00-23.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot 'out\flip-digits'
New-Item -ItemType Directory -Force $outDir | Out-Null

$W = 210; $H = 270; $R = 14   # canvas, raio dos cantos

function New-RoundedRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

$cardColor   = [System.Drawing.Color]::FromArgb(255, 0x0D, 0x0F, 0x1E)  # #0D0F1E
$borderColor = [System.Drawing.Color]::FromArgb(255, 0x2A, 0x2F, 0x4A)  # #2A2F4A
$splitColor  = [System.Drawing.Color]::FromArgb(153, 0, 0, 0)           # #000000 @ ~60%
$textColor   = [System.Drawing.Color]::FromArgb(255, 0xFF, 0xFF, 0xFF)  # #FFFFFF

$font = New-Object System.Drawing.Font('Consolas', 170, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
# Sem NoWrap o DrawString quebra "37" em duas linhas (o padding interno do
# GDI+ estoura a largura de layout de 210px); sem Trimming=None ele descarta
# o segundo dígito. O rect de layout também é alargado além da cartela
# (centralização mantém o alinhamento) para o padding interno nunca apertar.
$sf.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap -bor [System.Drawing.StringFormatFlags]::NoClip
$sf.Trimming = [System.Drawing.StringTrimming]::None

for ($v = 0; $v -le 59; $v++) {
    $bmp = New-Object System.Drawing.Bitmap $W, $H
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)

        # Cartela escura com cantos arredondados + borda 1px
        $fillPath = New-RoundedRectPath 0 0 $W $H $R
        $cardBrush = New-Object System.Drawing.SolidBrush $cardColor
        $g.FillPath($cardBrush, $fillPath)
        $borderPath = New-RoundedRectPath 0.5 0.5 ($W - 1) ($H - 1) $R
        $borderPen = New-Object System.Drawing.Pen $borderColor, 1
        $g.DrawPath($borderPen, $borderPath)

        # Dígitos centralizados
        $text = $v.ToString('00')
        $textBrush = New-Object System.Drawing.SolidBrush $textColor
        $rect = New-Object System.Drawing.RectangleF -60, 0, ($W + 120), $H
        $g.DrawString($text, $font, $textBrush, $rect, $sf)

        # Linha de divisão horizontal no meio (por cima do dígito)
        $splitBrush = New-Object System.Drawing.SolidBrush $splitColor
        $g.FillRectangle($splitBrush, 1, [int]($H / 2) - 1, $W - 2, 2)

        $bmp.Save((Join-Path $outDir ($text + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)

        $cardBrush.Dispose(); $borderPen.Dispose(); $textBrush.Dispose(); $splitBrush.Dispose()
        $fillPath.Dispose(); $borderPath.Dispose()
    }
    finally {
        $g.Dispose(); $bmp.Dispose()
    }
}

$font.Dispose(); $sf.Dispose()

$expectedNames = 0..59 | ForEach-Object { '{0:D2}.png' -f $_ }
$count = (Get-ChildItem $outDir -Filter '*.png' | Where-Object { $expectedNames -contains $_.Name } | Measure-Object).Count
if ($count -ne 60) { throw "Esperados 60 PNGs (00.png..59.png), encontrados $count" }
Write-Host "OK: 60 placas split-flap geradas em $outDir"
