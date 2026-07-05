# tools/eva-panel/04-install-profile.ps1
$ErrorActionPreference = 'Stop'
$GUID = 'ae0a0001-ea01-4b0e-9c0d-202607050001'
$app = Join-Path $env:LOCALAPPDATA 'SynQPanel'
$outDir = Join-Path $PSScriptRoot 'out'

# 1) Fundo -> assets\<GUID>\eva-bg.png
$assetDir = Join-Path $app "assets\$GUID"
New-Item -ItemType Directory -Force $assetDir | Out-Null
Copy-Item (Join-Path $outDir 'eva-bg.png') (Join-Path $assetDir 'eva-bg.png') -Force

# 2) Display items -> profiles\<GUID>.xml
Copy-Item (Join-Path $outDir "$GUID.xml") (Join-Path $app "profiles\$GUID.xml") -Force

# 3) Registrar no profiles.xml (com backup)
$profilesPath = Join-Path $app 'profiles.xml'
Copy-Item $profilesPath "$profilesPath.eva-bak" -Force
$xml = [xml](Get-Content $profilesPath -Raw)

$existing = $xml.ArrayOfProfile.Profile | Where-Object { $_.Guid -eq $GUID }
if ($existing) { Write-Host 'Perfil já registrado — pulando.'; exit 0 }

$bgPath = Join-Path $assetDir 'eva-bg.png'
$frag = @"
<Profile>
  <Guid>$GUID</Guid>
  <Name>EVA-01 3840x1100</Name>
  <Width>3840</Width>
  <Height>1100</Height>
  <Drag>true</Drag>
  <BackgroundColor>#FF040612</BackgroundColor>
  <Active>false</Active>
  <Topmost>false</Topmost>
  <Font>Consolas</Font>
  <FontSize>26</FontSize>
  <Color>#FFFFFFFF</Color>
  <BackgroundImagePath>$bgPath</BackgroundImagePath>
  <WindowX>0</WindowX>
  <WindowY>0</WindowY>
  <Resize>true</Resize>
  <StrictWindowMatching>false</StrictWindowMatching>
  <IsSelected>false</IsSelected>
  <ShowFps>false</ShowFps>
  <OpenGL>false</OpenGL>
  <FontScale>1</FontScale>
</Profile>
"@
$node = $xml.CreateDocumentFragment()
$node.InnerXml = $frag
$xml.DocumentElement.AppendChild($node) | Out-Null
$xml.Save($profilesPath)
Write-Host 'OK: perfil EVA-01 registrado.'
