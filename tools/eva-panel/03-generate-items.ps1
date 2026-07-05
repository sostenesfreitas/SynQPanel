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
foreach ($k in 'CpuTemp','CpuClk','CpuUti','GpuTemp','GpuUti','GpuClk','RamUsed','RamUti','NetDl','NetUl','DskTemp','DskAct','Fps') {
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

function SensorText($name,$x,$y,$size,$color,$id,$unit,$showUnit,$precision) {
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

function Flip($name,$x,$y,$unit) {
    (Head 'FlipDisplayItem' $name 'None' $x $y) + @"
    <Width>210</Width>
    <Height>270</Height>
    <ImageFolder />
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

# Célula da grade: rótulo + valor grande + sublinha + barra
function Cell($label,$cx,$cy,$valId,$valUnit,$valPrec,$sub1Id,$sub1Unit,$barId,$barMax,$barFg) {
    $xml  = StaticText $label ($cx+28) ($cy+22) 24 $GREEN
    $xml += SensorText "$label valor" ($cx+28) ($cy+62) 72 $WHITE $valId $valUnit $true $valPrec
    if ($sub1Id) { $xml += SensorText "$label sub" ($cx+28) ($cy+168) 22 $LILAC $sub1Id $sub1Unit $true 0 }
    if ($barId)  { $xml += Bar "$label bar" ($cx+28) ($cy+210) $barId $barMax $barFg }
    $xml
}

$items = @()
# --- Zona do relógio ---
$items += Flip 'Flip HH' 820 110 'Hour24'
$items += StaticText ':' 1042 140 120 $GREEN
$items += Flip 'Flip MM' 1090 110 'Minute'
$items += DateItem 820 400 34
# --- Clima (plugin weather) ---
$items += SensorText 'Clima temp' 1520 120 88 $WHITE '/weather/weather-current/temperature' '°C' $true 0
$items += SensorText 'Clima cond' 1520 270 32 $LILAC '/weather/weather-current/condition' '' $false 0
$items += SensorText 'Clima cidade' 1520 330 24 $PURPLE '/weather/weather-location/city_name' '' $false 0
# --- Grade 3x2 ---
$items += Cell 'CPU'   96 480 $S.CpuTemp '°C' 1 $S.CpuClk 'MHz' $S.CpuUti 100 $GREEN
$items += Cell 'GPU'  768 480 $S.GpuTemp '°C' 1 $S.GpuClk 'MHz' $S.GpuUti 100 $ORANGE
$items += Cell 'RAM' 1440 480 $S.RamUsed 'MB' 0 $S.RamUti '%' $S.RamUti 100 $PURPLE
$items += Cell 'REDE'  96 762 $S.NetDl 'KB/s' 0 $S.NetUl 'KB/s' $null 0 $null
$items += Cell 'DISCO' 768 762 $S.DskTemp '°C' 0 $S.DskAct '%' $S.DskAct 100 $GREEN
$items += Cell 'FPS' 1440 762 $S.Fps '' 0 $null '' $null 0 $null

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
