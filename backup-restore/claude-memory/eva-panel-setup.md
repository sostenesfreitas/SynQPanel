---
name: eva-panel-setup
description: "Estado do painel EVA-01 gerado, locais de instalação e pegadinhas do ambiente"
metadata: 
  node_type: memory
  type: project
  originSessionId: ca4eb73a-ef0f-4b59-8bc0-709f3c732011
---

Em 2026-07-05 geramos o painel EVA-01 3840×1100 via scripts em `tools/eva-panel/` (mergeado na main do fork do usuário). Fatos não deriváveis do repo:

- Perfil instalado em `%LOCALAPPDATA%\SynQPanel`: GUID `ae0a0001-ea01-4b0e-9c0d-202607050001`, assets em `assets\<GUID>\` (eva-bg.png + flip-digits\00-59.png). Backup pré-instalação: `profiles.xml.eva-bak`.
- O SynQPanel costuma rodar **elevado** — `Stop-Process` comum falha; usar `Start-Process powershell -Verb RunAs`.
- Build Release grava na pasta de onde o app roda → parar o app antes de `dotnet build` da solução.
- `weather_config.ini` fica ao lado da `SynQPanel.Extras.dll` deployada (`bin\...\win-x64\plugins\SynQPanel.Extras\`); config atual: `Location=Jaboatao dos Guararapes` (cidade do usuário, via CEP 54420-140).
- Wallpaper fonte: `C:\Users\soste\Downloads\616750.jpg` (EVA-01, 5563×3621) — parametrizado no script 02.
- Pegadinha recorrente: PS 5.1 lê `.ps1` sem BOM como ANSI → mojibake em `·`/`°C`; todos os scripts do projeto devem ser UTF-8 **com BOM**.
- Remotes: `origin` = fork do usuário (sostenesfreitas/SynQPanel), `upstream` = sursingh-hub/SynQPanel.

Fundo animado do painel 2B (2026-07-13): WebP 32fps em `assets\<GUID>\2b-bg-animated.webp`
(perfil aponta para ele por edição runtime — regenerar o perfil volta ao PNG estático;
re-aplicar via `tools/2b-panel/animated-bg/README.md`). ComfyUI Desktop: servidor em
`127.0.0.1:8188` (API funciona; Chrome bloqueia a página — usar a API direto),
modelos em `%LOCALAPPDATA%\Comfy-Desktop\ComfyUI-Shared\models`. Lições Wan 2.2 (receita final em tools/2b-panel/animated-bg/README.md):
com LoRA lightx2v (CFG=1) o prompt negativo é ignorado (inventa tiara/tranças na 2B);
**FLF com a mesma imagem nas 2 âncoras SUPRIME o movimento** (0,0% medido) — loop
correto = I2V livre + ping-pong na montagem; movimento de objetos (girar pirulito)
descola o doce do palito — animar só cabelo/tecido/partículas; validar movimento
NUMERICAMENTE (diff PIL ≥15% pixels >12), nunca por inspeção visual de frames.

Relacionado: [[usuario-sostenes]]
