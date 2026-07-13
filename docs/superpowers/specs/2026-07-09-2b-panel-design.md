# Painel 2B "Clean" — 1100×3840 (Design)

**Data:** 2026-07-09
**Status:** Aprovado pelo usuário (layout e conteúdo validados via mockups no companion visual)

## Objetivo

Segundo painel do usuário: vertical (1100×3840, a mesma tela do painel EVA em retrato),
estilo **clean/claro**, wallpaper NieR:Automata 2B
(`C:\Users\soste\Downloads\wallpaperflare.com_wallpaper.jpg`, 1440×2560).
Coexiste com o perfil EVA-01; o usuário alterna no SynQ Manager.

Inclui também a **correção do painel EVA-01** para o hardware atualizado (ver seção final).

## Hardware alvo (atualizado em 2026-07-09)

Ryzen 9 9950X3D · RTX 5090 · 64GB RAM. Particularidades confirmadas na shared memory
(124 sensores): a RTX 5090 é `GPU1` (iGPU Radeon não monitorada); **não há sensores de
fan de CPU/gabinete** (`FCPU` não existe mais — usar `TCCD1` como métrica extra da CPU);
**não há hotspot de GPU** (`TGPU1HOT` não existe — usar apenas `TGPU1MEM`).

## Layout (aprovado: mockup C + fade médio)

- **Fundo pré-renderizado** (PNG 1100×3840): base `#F5F4F2`; arte da 2B de ~5% a ~75% da
  altura (y≈192 a y≈2880), crop central preservando o rosto (escala pela altura, corte
  lateral simétrico); fade branco no topo da arte (atrás do relógio, y≈192–576) e fade
  longo na base (y≈2112–2880, chegando a ~95% de opacidade) — os sensores começam sobre
  o fade e terminam em fundo limpo.
- **Zona do relógio** (topo, centralizada): hora `HH:mm` em fonte fina ~150px, data por
  extenso, clima (temp + condição + cidade) com destaque dourado.
- **Coluna de sensores** (margens laterais 110px, de y≈2340 até y≈3740 — deslocada
  +110px na verificação para não colidir com o logo "NieR:Automata" da arte).

## Estilo

| Elemento | Valor |
|---|---|
| Fundo | `#F5F4F2` |
| Texto principal | `#1C1C1C` |
| Texto secundário | `#6A6A6A` |
| Destaque | dourado NieR `#A89670` |
| Barras | 3px, preenchimento `#1C1C1C`, trilha translúcida escura (12%) |
| Fontes | Segoe UI Light (números grandes), Segoe UI (rótulos) |
| Molduras | nenhuma; separadores em linha 1px `rgba(28,28,28,0.15)` |

## Conteúdo da coluna de sensores

(Enriquecido com base em pesquisa de painéis da comunidade; todos os IDs verificados na
shared memory desta máquina.)

| Bloco | Conteúdo | Sensores |
|---|---|---|
| CPU · RYZEN 9 9950X3D | temp grande · clock + uso% · potência W + tensão V + CCD1 °C · barra de uso | `TCPU` `SCPUCLK` `SCPUUTI` `PCPUPKG` `VCPU` `TCCD1` |
| GPU · RTX 5090 | temp grande · clock + uso% · VRAM °C + potência W + TDP% + fan RPM · VRAM usada MB + clock MHz · barra de uso | `TGPU1` `SGPU1CLK` `SGPU1UTI` `TGPU1MEM` `PGPU1` `PGPU1TDPP` `FGPU1` `SUSEDVMEM` `SGPU1MEMCLK` |
| RAM · 64GB | usada GB grande (÷1024, 1 casa) · uso% + livre GB · clock MHz + virtual GB · barra | `SUSEDMEM` `SMEMUTI` `SFREEMEM` `SMEMCLK` `SUSEDVIRTMEM` |
| — separador — | | |
| REDE | ↓ MB/s + ↑ MB/s (÷1024, 1 casa) | `SNIC2DLRATE` `SNIC2ULRATE` |
| DISCO | temp + atividade% + leitura MB/s + escrita MB/s | `THDD1` `SDSK1ACT` `SDSK1READSPD` `SDSK1WRITESPD` |
| SISTEMA | placa-mãe °C + uptime + FPS | `TMOBO` `SUPTIMENS` `SRTSSFPS` |

Sem flip clock (estilo clean usa `ClockDisplayItem` em texto fino) — sem tiles de dígitos.
Clima: plugin Open-Meteo já configurado globalmente (nada a fazer).

## Geração

- GUID novo: `2b000001-c1ea-4001-b0e0-202607050002`.
- Scripts em `tools/2b-panel/`: `01-generate-background.ps1` (composição fundo+arte+fades,
  `param($SourceImage)`), `02-generate-items.ps1`, `03-install-profile.ps1` — mesmos
  formatos XML e salvaguardas validados no painel EVA (UTF-8 BOM, guard de app rodando,
  backup só ao mutar, idempotência).
- Mapeamento de sensores: `tools/2b-panel/out/sensor-map.txt` próprio (gerado a partir da
  tabela acima; o `01-discover-sensors.ps1` do EVA continua sendo a ferramenta de descoberta).
- Perfil registrado com `Active=false` (usuário ativa no SynQ Manager quando girar a tela).

## Correção do painel EVA-01 (mesmo pacote)

O upgrade de hardware quebrou/desatualizou o painel EVA: `FCPU` não existe mais (campo
morto) e os rótulos citam o hardware antigo. Corrigir em `tools/eva-panel/`:
- `sensor-map.txt`: `CpuFan=FCPU` → `CpuCcd=TCCD1`.
- `03-generate-items.ps1`: rótulos `CPU · RYZEN 9 9950X3D`, `GPU · RTX 5090`, `RAM · 64GB`;
  linha da CPU troca fan RPM por `CCD1 <temp>°C`; regenerar e reinstalar.

## Verificação

1. Fundo: inspeção visual do PNG (rosto da 2B inteiro, fades suaves, sem distorção).
2. App reiniciado sem erros de desserialização nos logs.
3. Perfil 2B ativado temporariamente: screenshot comparado ao mockup aprovado
   (`.superpowers/brainstorm/1948-1783643903/content/layout-2b-v2.html`, opção A) —
   a tela está em paisagem, então a validação visual é do conteúdo/estilo; o encaixe
   físico final acontece quando o usuário girar a tela.
4. Painel EVA reativado ao final (é o painel em uso hoje); campos CCD1/rótulos conferidos.

## Fora de escopo

- Alterações no código do app (o plugin de clima já cobre tudo).
- Rotação da tela no Windows (ação manual do usuário).
- Ping e RAM por processo (AIDA64 não expõe).
