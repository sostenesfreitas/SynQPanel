# Restauração pós-format (backup de 2026-07-16)

Tudo que a máquina antiga tinha e o repositório sozinho não cobre.

## Passo a passo no PC novo

1. **Instalar**: AIDA64 (habilitar Shared Memory em Preferences → External
   Applications!), .NET 8 SDK, git, RTSS/MSI Afterburner (para FPS).
2. **Clonar**: `git clone https://github.com/sostenesfreitas/SynQPanel` e
   `dotnet build SynQPanel.sln -c Release`.
3. **Restaurar os perfis** (sem precisar regenerar nada):
   - Rodar o app UMA vez e fechar (cria `%LOCALAPPDATA%\SynQPanel`).
   - Copiar `profiles/profiles.xml` e os dois `<GUID>.xml` para
     `%LOCALAPPDATA%\SynQPanel\` e `%LOCALAPPDATA%\SynQPanel\profiles\`.
   - Criar `%LOCALAPPDATA%\SynQPanel\assets\2b000001-c1ea-4001-b0e0-202607050002\`
     e copiar `assets/2b-bg-animated-hires.webp` para lá **renomeando para
     `2b-bg-animated.webp`**.
   - Para o painel EVA: criar `assets\ae0a0001-ea01-4b0e-9c0d-202607050001\`,
     copiar `assets/eva-bg.png` e gerar as placas do flip
     (`tools/eva-panel/05-generate-flip-digits.ps1` → copiar `out/flip-digits/`
     para `assets\<GUID>\flip-digits\`).
4. **Sensor maps**: copiar `profiles/sensor-map-eva.txt` →
   `tools/eva-panel/out/sensor-map.txt` e `profiles/sensor-map-2b.txt` →
   `tools/2b-panel/out/sensor-map.txt`. ⚠️ Se o hardware mudar de novo, rodar
   `tools/eva-panel/01-discover-sensors.ps1` e conferir os IDs.
5. **Clima**: copiar `profiles/weather_config.ini` para a pasta do plugin
   (`...\win-x64\plugins\SynQPanel.Extras\`) após o build. Sem chave — Open-Meteo.
6. **Wallpapers-fonte** (para regenerar fundos no futuro): `wallpapers/` →
   copiar para Downloads ou ajustar os `param -SourceImage` dos scripts 01/02.
7. **Memória do Claude Code**: copiar `claude-memory/*.md` para
   `%USERPROFILE%\.claude\projects\C--Users-soste-Documents-aida-SynQPanel\memory\`
   (criar a pasta; o nome codifica o caminho do projeto — ajustar se clonar em
   outro lugar).
8. **ComfyUI** (só se for regenerar animações): instalar o Desktop, baixar os
   modelos do template Wan 2.2 14B I2V + `rife_v4.26.safetensors`
   (Comfy-Org/frame_interpolation) + `realesr-animevideov3.pth`
   (github xinntao/Real-ESRGAN). Receita completa em
   `tools/2b-panel/animated-bg/README.md`.

## Conteúdo

| Pasta | O quê |
|---|---|
| `assets/` | WebP final hi-res do painel 2B (o instalado), fundo do EVA, MP4s do take Wan v8 aprovado (fonte + interpolado 32fps) |
| `wallpapers/` | Imagens-fonte originais (EVA-01 e 2B) |
| `profiles/` | profiles.xml + display-items dos 2 painéis + sensor-maps + weather.ini |
| `claude-memory/` | Memória do projeto para o Claude Code |
