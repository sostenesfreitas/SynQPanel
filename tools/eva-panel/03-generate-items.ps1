# tools/eva-panel/03-generate-items.ps1
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null
$GUID = 'ae0a0001-ea01-4b0e-9c0d-202607050001'

# IDs reais da máquina: lidos de out\sensor-map.txt (contrato produzido pelo Task 1)
$mapPath = Join-Path $outDir 'sensor-map.txt'
if (-not (Test-Path $mapPath)) { throw 'sensor-map.txt nao encontrado - rode o Task 1 primeiro' }
$S = @{}
foreach ($line in Get-Content $mapPath) {
    if ($line -match '^\s*([A-Za-z]+)\s*=\s*(\S+)') { $S[$Matches[1]] = $Matches[2] }
}
foreach ($k in 'CpuTemp','CpuClk','CpuUti','GpuTemp','GpuUti','GpuClk','RamUsed','RamUti','NetDl','NetUl','DskTemp','DskAct','Fps',
               'CpuPwr','CpuVolt','CpuCcd','GpuPwr','GpuFan','VramUsed','VramClk','RamFree','VirtUsed','VirtUti') {
    if (-not $S.ContainsKey($k)) { throw "sensor-map.txt sem a chave $k" }
}

$GREEN='#B8E621'; $PURPLE='#8A63D2'; $LILAC='#B8A6F0'; $ORANGE='#FF7A1A'; $WHITE='#FFFFFF'
$FONT='Consolas'

function TextCommon($font,$size,$bold,$color) {
@"
    <Font>$font</Font>
    <FontSize>$size</FontSize>
    <Bold>$($bold.ToString().ToLower())</Bold>
    <Italic>false</Italic>
    <Underline>false</Underline>
    <Strikeout>false</Strikeout>
    <Color>$color</Color>
    <Uppercase>false</Uppercase>
    <FontWeight />
    <FontStyle />
    <RightAlign>false</RightAlign>
    <CenterAlign>false</CenterAlign>
    <Wrap>false</Wrap>
    <Ellipsis>false</Ellipsis>
    <Width>0</Width>
    <Height>0</Height>
    <Marquee>false</Marquee>
    <MarqueeSpeed>50</MarqueeSpeed>
    <MarqueeSpacing>40</MarqueeSpacing>
"@
}

function Head($type,$name,$sensorType,$x,$y) {
@"
  <DisplayItem xsi:type="$type">
    <Name>$name</Name>
    <SensorType>$sensorType</SensorType>
    <Hidden>false</Hidden>
    <OriginalLineIndex xsi:nil="true" />
    <X>$x</X>
    <Y>$y</Y>
    <IsLocked>false</IsLocked>
    <Rotation>0</Rotation>
"@
}

function StaticText($name,$x,$y,$size,$color,$bold=$true) {
    # TextDisplayItem renderiza o proprio Name (EvaluateText => Name) — nao existe <Text>
    (Head 'TextDisplayItem' $name 'None' $x $y) + (TextCommon $FONT $size $bold $color) + @"
  </DisplayItem>
"@
}

# $mult/$divToggle: modificadores de valor do SensorDisplayItem. Com
# DivisionToggle=true o valor é DIVIDIDO por MultiplicationModifier
# (SensorDisplayItem.cs) — ex.: MB -> GB com mult=1024 e divToggle=$true.
function SensorText($name,$x,$y,$size,$color,$id,$unit,$showUnit,$precision,$mult=1,$divToggle=$false) {
    (Head 'SensorDisplayItem' $name 'Plugin' $x $y) + (TextCommon $FONT $size $true $color) + @"
    <_valueType>NOW</_valueType>
    <SensorName>$id</SensorName>
    <Id>0</Id>
    <Instance>0</Instance>
    <EntryId>0</EntryId>
    <PluginSensorId>$id</PluginSensorId>
    <ValueType>NOW</ValueType>
    <Threshold1>0</Threshold1>
    <Threshold1Color>#000000</Threshold1Color>
    <Threshold2>0</Threshold2>
    <Threshold2Color>#000000</Threshold2Color>
    <ShowName>false</ShowName>
    <Unit>$unit</Unit>
    <OverrideUnit>true</OverrideUnit>
    <ShowUnit>$($showUnit.ToString().ToLower())</ShowUnit>
    <OverridePrecision>true</OverridePrecision>
    <Precision>$precision</Precision>
    <AdditionModifier>0</AdditionModifier>
    <AbsoluteAddition>true</AbsoluteAddition>
    <MultiplicationModifier>$mult</MultiplicationModifier>
    <DivisionToggle>$($divToggle.ToString().ToLower())</DivisionToggle>
  </DisplayItem>
"@
}

function Bar($name,$x,$y,$id,$max,$fg) {
    (Head 'BarDisplayItem' $name 'Plugin' $x $y) + @"
    <_valueType>NOW</_valueType>
    <SensorName>$id</SensorName>
    <Id>0</Id>
    <Instance>0</Instance>
    <EntryId>0</EntryId>
    <PluginSensorId>$id</PluginSensorId>
    <ValueType>NOW</ValueType>
    <MinValue>0</MinValue>
    <MaxValue>$max</MaxValue>
    <AutoValue>false</AutoValue>
    <Width>584</Width>
    <Height>12</Height>
    <FlipX>false</FlipX>
    <Frame>false</Frame>
    <FrameColor>#666666</FrameColor>
    <Background>true</Background>
    <BackgroundColor>#1A1F35</BackgroundColor>
    <Color>$fg</Color>
    <Gradient>false</Gradient>
    <GradientColor>#333333</GradientColor>
    <CornerRadius>6</CornerRadius>
  </DisplayItem>
"@
}

# Fundo do painel como ImageDisplayItem (primeiro item = fundo do z-order).
# Motivo: PanelDraw só desenha Profile.BackgroundImagePath para imports .rslcd
# (PanelDraw.cs, checagem isRslcdProfile). RelativePath=true resolve para
# assets\<GUID>\<FilePath> via ImageDisplayItem.CalculatedPath.
# Ordem dos elementos copiada da saída real do XmlSerializer (ordem importa).
function BgImage($file,$w,$h) {
    (Head 'ImageDisplayItem' $file 'None' 0 0) + @"
    <Type>FILE</Type>
    <ReadOnly>false</ReadOnly>
    <FilePath>$file</FilePath>
    <RelativePath>true</RelativePath>
    <Cache>true</Cache>
    <Scale>100</Scale>
    <Layer>false</Layer>
    <LayerColor>#77FFFFFF</LayerColor>
    <ShowPanel>false</ShowPanel>
    <Volume>0</Volume>
    <Width>$w</Width>
    <Height>$h</Height>
  </DisplayItem>
"@
}

# Relógio split-flap animado. Exige as placas 00.png..59.png geradas pelo
# 05-generate-flip-digits.ps1 e instaladas em assets\<GUID>\flip-digits.
# ImageFolder NÃO pode ser vazio (PanelDraw.cs aborta o draw) e, sendo
# relativo, CalculatedImageFolder resolve para assets\<GUID>\<ImageFolder>
# (FlipDisplayItem.cs).
function Flip($name,$x,$y,$unit) {
    (Head 'FlipDisplayItem' $name 'None' $x $y) + @"
    <Width>210</Width>
    <Height>270</Height>
    <ImageFolder>flip-digits</ImageFolder>
    <FlipStyle>SplitFlap</FlipStyle>
    <DigitSpacing>6</DigitSpacing>
    <PreviewValue>0</PreviewValue>
    <DigitCount>2</DigitCount>
    <FlipProgress>0</FlipProgress>
    <ResolvedFlipProgress>0</ResolvedFlipProgress>
    <AnimationDuration>0.6</AnimationDuration>
    <ShadowIntensity>0.7</ShadowIntensity>
    <LightingIntensity>0.4</LightingIntensity>
    <TimeUnit>$unit</TimeUnit>
  </DisplayItem>
"@
}

function DateItem($x,$y,$size) {
    (Head 'CalendarDisplayItem' 'Data' 'None' $x $y) +
    ((TextCommon $FONT $size $true $LILAC) -replace '<Uppercase>false</Uppercase>','<Uppercase>true</Uppercase>') + @"
    <Format>dddd · dd MMM yyyy</Format>
  </DisplayItem>
"@
}

# Célula da grade: rótulo + valor grande + sublinhas + barra.
# sub2 (opcional) fica à direita da sub1, em (cx+380, cy+168).
# valMult/valDiv (opcionais) aplicam os modificadores ao valor grande
# (ex.: RAM em MB -> GB com valMult=1024 e valDiv=$true).
function Cell($label,$cx,$cy,$valId,$valUnit,$valPrec,$sub1Id,$sub1Unit,$sub2Id,$sub2Unit,$barId,$barMax,$barFg,$valMult=1,$valDiv=$false,$sub1Prec=0,$sub1Mult=1,$sub1Div=$false) {
    $xml  = StaticText $label ($cx+28) ($cy+22) 24 $GREEN
    $xml += SensorText "$label valor" ($cx+28) ($cy+62) 72 $WHITE $valId $valUnit $true $valPrec $valMult $valDiv
    if ($sub1Id) { $xml += SensorText "$label sub" ($cx+28) ($cy+168) 22 $LILAC $sub1Id $sub1Unit $true $sub1Prec $sub1Mult $sub1Div }
    if ($sub2Id) { $xml += SensorText "$label sub2" ($cx+380) ($cy+168) 22 $LILAC $sub2Id $sub2Unit $true 0 }
    if ($barId)  { $xml += Bar "$label bar" ($cx+28) ($cy+210) $barId $barMax $barFg }
    $xml
}

# Células ricas da linha superior (estilo sensorpanel clássico do AIDA64).
# Geometria dentro da célula 640x250:
#   rótulo cy+22 (22px) · valor grande cy+58 (56px) · linhas de detalhe
#   cy+132 / cy+162 / cy+192 (19px) · barra cy+210 (GPU: cy+226, tem 3 linhas)
function CpuCell($cx,$cy) {
    $xml  = StaticText 'CPU · RYZEN 9 9950X3D' ($cx+28) ($cy+22) 22 $GREEN
    $xml += SensorText 'CPU temp' ($cx+28)  ($cy+58)  56 $WHITE $S.CpuTemp '°C' $true 1
    $xml += SensorText 'CPU clk'  ($cx+28)  ($cy+132) 19 $LILAC $S.CpuClk 'MHz' $true 0
    $xml += SensorText 'CPU uso'  ($cx+200) ($cy+132) 19 $LILAC $S.CpuUti '%'   $true 0
    $xml += SensorText 'CPU pwr'  ($cx+28)  ($cy+162) 19 $LILAC $S.CpuPwr 'W'   $true 0
    $xml += SensorText 'CPU volt' ($cx+180) ($cy+162) 19 $LILAC $S.CpuVolt 'V'  $true 2
    $xml += StaticText 'CCD1' ($cx+360) ($cy+162) 19 $PURPLE
    $xml += SensorText 'CPU ccd' ($cx+440) ($cy+162) 19 $LILAC $S.CpuCcd '°C' $true 0
    $xml += Bar 'CPU bar' ($cx+28) ($cy+210) $S.CpuUti 100 $GREEN
    $xml
}

function GpuCell($cx,$cy) {
    $xml  = StaticText 'GPU · RTX 5090' ($cx+28) ($cy+22) 22 $GREEN
    $xml += SensorText 'GPU temp'   ($cx+28)  ($cy+58)  56 $WHITE $S.GpuTemp '°C' $true 1
    $xml += SensorText 'GPU clk'    ($cx+28)  ($cy+132) 19 $LILAC $S.GpuClk 'MHz' $true 0
    $xml += SensorText 'GPU uso'    ($cx+200) ($cy+132) 19 $LILAC $S.GpuUti '%'   $true 0
    $xml += SensorText 'GPU pwr'    ($cx+28)  ($cy+162) 19 $LILAC $S.GpuPwr 'W'   $true 0
    $xml += SensorText 'GPU fan'    ($cx+180) ($cy+162) 19 $LILAC $S.GpuFan 'RPM' $true 0
    $xml += StaticText 'VRAM'       ($cx+28)  ($cy+192) 19 $PURPLE
    $xml += SensorText 'VRAM usada' ($cx+100) ($cy+192) 19 $LILAC $S.VramUsed 'MB' $true 0
    $xml += SensorText 'VRAM clk'   ($cx+280) ($cy+192) 19 $LILAC $S.VramClk 'MHz' $true 0
    $xml += Bar 'GPU bar' ($cx+28) ($cy+226) $S.GpuUti 100 $ORANGE
    $xml
}

function RamCell($cx,$cy) {
    $xml  = StaticText 'RAM · 64GB' ($cx+28) ($cy+22) 22 $GREEN
    $xml += SensorText 'RAM valor'  ($cx+28)  ($cy+58)  56 $WHITE $S.RamUsed 'GB' $true 1 1024 $true
    $xml += SensorText 'RAM uso'    ($cx+28)  ($cy+132) 19 $LILAC $S.RamUti '%' $true 0
    $xml += StaticText 'LIVRE'      ($cx+140) ($cy+132) 19 $PURPLE
    $xml += SensorText 'RAM livre'  ($cx+230) ($cy+132) 19 $LILAC $S.RamFree 'GB' $true 1 1024 $true
    $xml += StaticText 'VIRT'       ($cx+28)  ($cy+162) 19 $PURPLE
    $xml += SensorText 'VIRT usada' ($cx+100) ($cy+162) 19 $LILAC $S.VirtUsed 'GB' $true 1 1024 $true
    $xml += SensorText 'VIRT uso'   ($cx+250) ($cy+162) 19 $LILAC $S.VirtUti '%' $true 0
    $xml += Bar 'RAM bar' ($cx+28) ($cy+210) $S.RamUti 100 $PURPLE
    $xml
}

$items = @()
# --- Fundo (precisa ser o PRIMEIRO item: desenhado antes dos demais) ---
$items += BgImage 'eva-bg.png' 3840 1100
# --- Zona do relógio ---
$items += Flip 'Flip HH' 820 110 'Hour24'
$items += StaticText ':' 1042 140 120 $GREEN
$items += Flip 'Flip MM' 1090 110 'Minute'
$items += DateItem 820 400 34
# --- Clima (plugin weather) ---
$items += SensorText 'Clima temp' 1520 120 88 $WHITE '/weather/weather-current/temperature' '°C' $true 0
$items += SensorText 'Clima cond' 1520 270 32 $LILAC '/weather/weather-current/condition' '' $false 0
$items += SensorText 'Clima cidade' 1520 330 24 $PURPLE '/weather/weather-location/city_name' '' $false 0
# --- Grade 3x2 (linha superior: células ricas) ---
$items += CpuCell   96 480
$items += GpuCell  768 480
$items += RamCell 1440 480
$items += Cell 'REDE'  96 762 $S.NetDl 'MB/s' 1 $S.NetUl 'MB/s' $null '' $null 0 $null 1024 $true 1 1024 $true
$items += Cell 'DISCO' 768 762 $S.DskTemp '°C' 0 $S.DskAct '%' $null '' $S.DskAct 100 $GREEN
$items += Cell 'FPS' 1440 762 $S.Fps '' 0 $null '' $null '' $null 0 $null

$doc = @"
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfDisplayItem xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
$($items -join "`r`n")
</ArrayOfDisplayItem>
"@

$path = Join-Path $outDir "$GUID.xml"
[System.IO.File]::WriteAllText($path, $doc, (New-Object System.Text.UTF8Encoding $false))
[xml](Get-Content $path -Raw) | Out-Null   # valida well-formed
Write-Host "OK: $GUID.xml gerado e bem-formado"
