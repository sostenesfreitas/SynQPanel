# tools/eva-panel/01-discover-sensors.ps1
# Lê a shared memory do AIDA64 e lista todos os sensores disponíveis.
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null

$mmf = [System.IO.MemoryMappedFiles.MemoryMappedFile]::OpenExisting(
    'AIDA64_SensorValues', [System.IO.MemoryMappedFiles.MemoryMappedFileRights]::Read)
$stream = $mmf.CreateViewStream(0, 0, [System.IO.MemoryMappedFiles.MemoryMappedFileAccess]::Read)
$reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
$raw = $reader.ReadToEnd().TrimEnd([char]0)
$reader.Dispose(); $stream.Dispose(); $mmf.Dispose()

# Formato: <categoria><id>X</id><label>Y</label><value>Z</value></categoria>...
$rx = [regex]'<(?<cat>\w+)><id>(?<id>[^<]*)</id><label>(?<label>[^<]*)</label><value>(?<value>[^<]*)</value></\w+>'
$lines = foreach ($m in $rx.Matches($raw)) {
    '{0,-6} {1,-16} {2,-40} {3}' -f $m.Groups['cat'].Value, $m.Groups['id'].Value,
        $m.Groups['label'].Value, $m.Groups['value'].Value
}

if ($lines.Count -eq 0) {
    $sample = $raw.Substring(0, [Math]::Min(2000, $raw.Length))
    $sample | Set-Content (Join-Path $outDir 'raw-sample.txt') -Encoding utf8
    Write-Host "AVISO: 0 sensores casados pelo regex. Amostra salva em out\raw-sample.txt para ajuste do regex."
    exit 1
}

$lines | Set-Content (Join-Path $outDir 'sensors.txt') -Encoding utf8
Write-Host "OK: $($lines.Count) sensores em out\sensors.txt"
