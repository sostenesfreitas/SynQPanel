# tools/2b-panel/03-install-profile.ps1
$ErrorActionPreference = 'Stop'
if (Get-Process SynQPanel -ErrorAction SilentlyContinue) { throw 'Feche o SynQPanel antes de instalar.' }
$GUID = '2b000001-c1ea-4001-b0e0-202607050002'
$app = Join-Path $env:LOCALAPPDATA 'SynQPanel'
$outDir = Join-Path $PSScriptRoot 'out'

# 1) Assets (sempre executam, mesmo com o perfil já registrado — o early-exit
#    de idempotência abaixo só pula o registro no profiles.xml)
$assetDir = Join-Path $app "assets\$GUID"
New-Item -ItemType Directory -Force $assetDir | Out-Null
Copy-Item (Join-Path $outDir '2b-bg.png') (Join-Path $assetDir '2b-bg.png') -Force

# 2) Display items -> profiles\<GUID>.xml
Copy-Item (Join-Path $outDir "$GUID.xml") (Join-Path $app "profiles\$GUID.xml") -Force

# 3) Registrar no profiles.xml (com backup)
$profilesPath = Join-Path $app 'profiles.xml'
$xml = [xml](Get-Content $profilesPath -Raw)
$existing = $xml.ArrayOfProfile.Profile | Where-Object { $_.Guid -eq $GUID }
if ($existing) { Write-Host 'Perfil ja registrado - pulando.'; exit 0 }

Copy-Item $profilesPath "$profilesPath.2b-bak" -Force
$frag = @"
<Profile>
  <Guid>$GUID</Guid>
  <Name>2B Clean 1100x3840</Name>
  <Width>1100</Width>
  <Height>3840</Height>
  <Drag>true</Drag>
  <BackgroundColor>#FFF5F4F2</BackgroundColor>
  <Active>false</Active>
  <Topmost>false</Topmost>
  <Font>Segoe UI</Font>
  <FontSize>26</FontSize>
  <Color>#FF1C1C1C</Color>
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
Write-Host 'OK: perfil 2B registrado.'
