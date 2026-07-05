# Painel EVA-01 — 3840×1100 (Design)

**Data:** 2026-07-05
**Status:** Aprovado pelo usuário

## Objetivo

Gerar programaticamente um perfil nativo do SynQPanel para uma tela secundária de
3840×1100, com tema Neon Genesis Evangelion baseado no wallpaper do usuário
(`C:\Users\soste\Downloads\616750.jpg`, 5563×3621). O usuário validou o layout via
mockup (opção B): **EVA-01 visível à direita, dashboard de dados à esquerda**.

## Layout e visual

- **Resolução do perfil:** 3840×1100.
- **Fundo:** imagem única PNG/JPEG 3840×1100 gerada por script:
  - Crop do wallpaper mantendo o EVA-01 inteiro na metade direita
    (faixa vertical ~28% do topo, proporção 3,49:1).
  - Gradiente horizontal pré-renderizado na imagem: lado esquerdo escurecido
    (`#040612` a ~96% de opacidade em 0–42% da largura, transparente a partir de ~68%).
- **Paleta (EVA-01):** verde neon `#B8E621`, roxo `#8A63D2`, laranja `#FF7A1A`,
  texto branco, fundo `#040612`.
- **Tipografia:** monoespaçada/condensada estilo HUD (fonte disponível no Windows,
  ex.: Consolas ou similar).
- **Dashboard (lado esquerdo, ~55% da largura):** Flip Clock + data + clima em
  destaque no centro-topo; ao redor, grade 3×2 com os 6 blocos de sensores
  (CPU, GPU, RAM, Rede, Discos, FPS), cada um com borda roxa translúcida,
  rótulo verde neon e valor grande branco.

## Conteúdo do dashboard

| Bloco | Dados | Elementos |
|---|---|---|
| CPU | temperatura, clock, uso | SensorDisplayItem + BarDisplayItem |
| GPU | temperatura, uso, clock | SensorDisplayItem + BarDisplayItem |
| RAM | usada/total | SensorDisplayItem + BarDisplayItem |
| Rede | download/upload | SensorDisplayItem |
| Discos | temperatura, espaço livre | SensorDisplayItem |
| FPS | FPS do jogo ativo | SensorDisplayItem (sensor AIDA64/RTSS) |
| Relógio | hora animada + data | FlipDisplayItem + CalendarDisplayItem |
| Clima | temperatura e condição atual | itens do WeatherPlugin (SynQPanel.Extras) |

## Arquitetura da geração

1. **Imagem de fundo:** script PowerShell/System.Drawing faz crop + gradiente e salva
   em `%LocalAppData%\SynQPanel\assets\<guid-do-perfil>\eva-bg.png`.
2. **Perfil:** XML serializado no formato exato do app (referência:
   `SynQPanel/Models/Profile.cs`, `ConfigModel.cs` — `XmlSerializer`), registrado em
   `%LocalAppData%\SynQPanel\profiles.xml` e/ou pasta `profiles\`.
3. **Itens:** `ImageDisplayItem` (fundo), `SensorDisplayItem`/`BarDisplayItem`
   (métricas), `FlipDisplayItem` (relógio), `CalendarDisplayItem` (data), itens de
   plugin para clima.
4. **Sensor IDs:** lidos da shared memory do AIDA64 da máquina do usuário no momento
   da geração (não hard-coded de outra máquina).

## Dependências externas (setup do usuário)

- **AIDA64** com Shared Memory habilitada (já em execução na máquina).
- **FPS:** requer RTSS (RivaTuner Statistics Server) em execução; sem ele o campo
  fica vazio fora de jogos. Usuário informado.
- **Clima:** requer chave gratuita do WeatherAPI.com e cidade configuradas em
  `weather_config.ini` (lido pelo `WeatherPlugin`). Usuário informado.

## Verificação

1. Reiniciar o SynQPanel e ativar o perfil gerado.
2. Screenshot da janela do painel e comparação com o mockup aprovado
   (`.superpowers/brainstorm/371-1783263613/content/layout-eva.html`).
3. Conferir que cada campo exibe valor real do AIDA64 (não “—” ou vazio),
   exceto FPS/clima se as dependências externas não estiverem configuradas.

## Tratamento de erros / plano B

- Se o app rejeitar o XML do perfil (falha de desserialização registrada em
  `%LocalAppData%\SynQPanel\logs`), cair para o plano B: gerar `.sensorpanel`
  importável (fundo + sensores) e adicionar Flip Clock/clima manualmente no
  SynQ Manager. A imagem de fundo gerada é reaproveitada.

## Fora de escopo

- Alterações no código do SynQPanel.
- Painéis para outras resoluções.
- Automação da obtenção da chave do WeatherAPI.com.
