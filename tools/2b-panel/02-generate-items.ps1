# tools/2b-panel/02-generate-items.ps1
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null
$GUID = '2b000001-c1ea-4001-b0e0-202607050002'

$mapPath = Join-Path $outDir 'sensor-map.txt'
if (-not (Test-Path $mapPath)) { throw 'sensor-map.txt nao encontrado' }
$S = @{}
foreach ($line in Get-Content $mapPath) {
    if ($line -match '^\s*([A-Za-z]+)\s*=\s*(\S+)') { $S[$Matches[1]] = $Matches[2] }
}
foreach ($k in 'CpuTemp','CpuClk','CpuUti','CpuPwr','CpuVolt','CpuCcd','GpuTemp','GpuClk','GpuUti','GpuPwr','GpuTdp','GpuFan','VramTemp','VramUsed','VramClk','RamUsed','RamUti','RamFree','RamClk','VirtUsed','NetDl','NetUl','DskTemp','DskAct','DskRead','DskWrite','MoboTemp','Uptime','Fps') {
    if (-not $S.ContainsKey($k)) { throw "sensor-map.txt sem a chave $k" }
}

$DARK='#1C1C1C'; $GRAY='#6A6A6A'; $GOLD='#A89670'; $TRACK='#E5E3DF'
$FONT='Segoe UI'; $FONTL='Segoe UI Light'

function TextCommon($font,$size,$color,$upper=$false) {
@"
    <Font>$font</Font>
    <FontSize>$size</FontSize>
    <Bold>false</Bold>
    <Italic>false</Italic>
    <Underline>false</Underline>
    <Strikeout>false</Strikeout>
    <Color>$color</Color>
    <Uppercase>$($upper.ToString().ToLower())</Uppercase>
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

function BgImage($file,$w,$h) {
    (Head 'ImageDisplayItem' $file 'None' 0 0) + @"
    <FilePath>$file</FilePath>
    <RelativePath>true</RelativePath>
    <Scale>100</Scale>
    <Width>$w</Width>
    <Height>$h</Height>
  </DisplayItem>
"@
}

function StaticText($name,$x,$y,$size,$color,$font=$null) {
    if (-not $font) { $font = $FONT }
    (Head 'TextDisplayItem' $name 'None' $x $y) + (TextCommon $font $size $color) + @"
  </DisplayItem>
"@
}

function SensorText($name,$x,$y,$size,$color,$id,$unit,$showUnit,$precision,$mult=1,$divToggle=$false,$font=$null) {
    if (-not $font) { $font = $FONT }
    (Head 'SensorDisplayItem' $name 'Plugin' $x $y) + (TextCommon $font $size $color) + @"
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
    <Width>880</Width>
    <Height>4</Height>
    <FlipX>false</FlipX>
    <Frame>false</Frame>
    <FrameColor>#666666</FrameColor>
    <Background>true</Background>
    <BackgroundColor>$TRACK</BackgroundColor>
    <Color>$fg</Color>
    <Gradient>false</Gradient>
    <GradientColor>#333333</GradientColor>
    <CornerRadius>2</CornerRadius>
  </DisplayItem>
"@
}

function ClockItem($name,$x,$y,$size,$color,$format,$font) {
    (Head 'ClockDisplayItem' $name 'None' $x $y) + (TextCommon $font $size $color) + @"
    <Format>$format</Format>
  </DisplayItem>
"@
}

function DateItem($x,$y,$size) {
    (Head 'CalendarDisplayItem' 'Data' 'None' $x $y) +
    ((TextCommon $FONT $size $GRAY) -replace '<Uppercase>false</Uppercase>','<Uppercase>true</Uppercase>') + @"
    <Format>dddd · dd MMM yyyy</Format>
  </DisplayItem>
"@
}

$items = @()
$items += BgImage '2b-bg.png' 1100 3840
# Relogio + data + clima
$items += ClockItem 'Hora' 330 96 150 $DARK 'HH:mm' $FONTL
$items += DateItem 280 310 36
$items += SensorText 'Clima temp' 330 385 44 $GOLD '/weather/weather-current/temperature' '°C' $true 0
$items += SensorText 'Clima cond' 455 392 32 $GRAY '/weather/weather-current/condition' '' $false 0
$items += SensorText 'Clima cidade' 330 445 26 $GRAY '/weather/weather-location/city_name' '' $false 0
# CPU
$items += StaticText 'CPU · RYZEN 9 9950X3D' 110 2230 26 $GRAY
$items += SensorText 'CPU temp' 110 2266 84 $DARK $S.CpuTemp '°C' $true 1 1 $false $FONTL
$items += SensorText 'CPU clk' 110 2400 24 $GRAY $S.CpuClk 'MHz' $true 0
$items += SensorText 'CPU uso' 430 2400 24 $GOLD $S.CpuUti '%' $true 0
$items += SensorText 'CPU pwr' 110 2440 24 $GRAY $S.CpuPwr 'W' $true 0
$items += SensorText 'CPU volt' 300 2440 24 $GRAY $S.CpuVolt 'V' $true 2
$items += StaticText 'CCD1' 500 2440 24 $GOLD
$items += SensorText 'CPU ccd' 580 2440 24 $GRAY $S.CpuCcd '°C' $true 0
$items += Bar 'CPU bar' 110 2492 $S.CpuUti 100 $DARK
# GPU
$items += StaticText 'GPU · RTX 5090' 110 2540 26 $GRAY
$items += SensorText 'GPU temp' 110 2576 84 $DARK $S.GpuTemp '°C' $true 1 1 $false $FONTL
$items += SensorText 'GPU clk' 110 2710 24 $GRAY $S.GpuClk 'MHz' $true 0
$items += SensorText 'GPU uso' 430 2710 24 $GOLD $S.GpuUti '%' $true 0
$items += SensorText 'VRAM temp' 110 2750 24 $GRAY $S.VramTemp '°C' $true 0
$items += SensorText 'GPU pwr' 300 2750 24 $GRAY $S.GpuPwr 'W' $true 0
$items += SensorText 'GPU tdp' 480 2750 24 $GRAY $S.GpuTdp '%' $true 0
$items += SensorText 'GPU fan' 640 2750 24 $GRAY $S.GpuFan 'RPM' $true 0
$items += StaticText 'VRAM' 110 2790 24 $GOLD
$items += SensorText 'VRAM usada' 210 2790 24 $GRAY $S.VramUsed 'MB' $true 0
$items += SensorText 'VRAM clk' 480 2790 24 $GRAY $S.VramClk 'MHz' $true 0
$items += Bar 'GPU bar' 110 2842 $S.GpuUti 100 $DARK
# RAM
$items += StaticText 'RAM · 64GB' 110 2890 26 $GRAY
$items += SensorText 'RAM valor' 110 2926 84 $DARK $S.RamUsed 'GB' $true 1 1024 $true $FONTL
$items += SensorText 'RAM uso' 110 3060 24 $GOLD $S.RamUti '%' $true 0
$items += StaticText 'LIVRE' 260 3060 24 $GRAY
$items += SensorText 'RAM livre' 390 3060 24 $GRAY $S.RamFree 'GB' $true 1 1024 $true
$items += SensorText 'RAM clk' 110 3100 24 $GRAY $S.RamClk 'MHz' $true 0
$items += StaticText 'VIRT' 400 3100 24 $GRAY
$items += SensorText 'VIRT usada' 500 3100 24 $GRAY $S.VirtUsed 'GB' $true 1 1024 $true
$items += Bar 'RAM bar' 110 3152 $S.RamUti 100 $DARK
# REDE
$items += StaticText 'REDE' 110 3250 26 $GRAY
$items += StaticText '↓' 110 3286 44 $GOLD
$items += SensorText 'Rede dl' 160 3286 44 $DARK $S.NetDl 'MB/s' $true 1 1024 $true $FONTL
$items += StaticText '↑' 480 3298 28 $GRAY
$items += SensorText 'Rede ul' 530 3298 28 $GRAY $S.NetUl 'MB/s' $true 1 1024 $true
# DISCO
$items += StaticText 'DISCO' 110 3390 26 $GRAY
$items += SensorText 'Disco temp' 110 3426 24 $DARK $S.DskTemp '°C' $true 0
$items += SensorText 'Disco act' 260 3426 24 $GRAY $S.DskAct '%' $true 0
$items += StaticText 'R' 400 3426 24 $GOLD
$items += SensorText 'Disco r' 440 3426 24 $GRAY $S.DskRead 'MB/s' $true 1 1024 $true
$items += StaticText 'W' 640 3426 24 $GOLD
$items += SensorText 'Disco w' 680 3426 24 $GRAY $S.DskWrite 'MB/s' $true 1 1024 $true
# SISTEMA
$items += StaticText 'SISTEMA' 110 3530 26 $GRAY
$items += StaticText 'MOBO' 110 3566 24 $GRAY
$items += SensorText 'Mobo temp' 200 3566 24 $DARK $S.MoboTemp '°C' $true 0
$items += StaticText 'UPTIME' 340 3566 24 $GRAY
$items += SensorText 'Uptime' 460 3566 24 $DARK $S.Uptime '' $false 0
$items += StaticText 'FPS' 680 3566 24 $GRAY
$items += SensorText 'Fps' 750 3566 28 $GOLD $S.Fps '' $false 0

$doc = @"
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfDisplayItem xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
$($items -join "`r`n")
</ArrayOfDisplayItem>
"@

$path = Join-Path $outDir "$GUID.xml"
[System.IO.File]::WriteAllText($path, $doc, (New-Object System.Text.UTF8Encoding $false))
[xml](Get-Content $path -Raw) | Out-Null
Write-Host "OK: $GUID.xml gerado e bem-formado"
