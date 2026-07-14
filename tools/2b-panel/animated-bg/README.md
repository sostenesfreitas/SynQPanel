# Fundo animado do painel 2B (cinemagraph via ComfyUI)

Transforma o wallpaper estático da 2B em fundo **animado em loop perfeito**
(WebP 1100×3840, 320 frames @ 32fps, loop ping-pong de ~10s) para o SynQPanel.

**Requisitos:** ComfyUI (testado no Desktop, servidor `127.0.0.1:8188`) com os modelos
do template **Wan 2.2 14B I2V** e **RIFE** (`rife_v4.26.safetensors` de
https://huggingface.co/Comfy-Org/frame_interpolation em `models/frame_interpolation/`).

## Receita (validada em 2026-07-13, 8 iterações)

1. **Upload** do wallpaper para os inputs (`POST /upload/image`).
2. **`10-wan22-i2v-workflow.json`** (`POST /prompt`): gera o vídeo com movimento
   (81 frames, 720×1280, 20 steps, CFG 3.5, sampling completo).
3. **Portões de qualidade (numéricos — não confie em inspeção visual!):**
   - *Movimento:* diff entre frames distantes do MP4 ≥ ~15% de pixels alterados
     (>12/255). Abaixo disso o painel parece estático.
   - *Fidelidade:* inspecionar o pior frame da região crítica (boca/pirulito) —
     medir a região com PIL e olhar o frame de maior mudança.
4. **`11-rife-interp-workflow.json`**: interpola 16→32fps (upload do MP4 gerado
   como `2b_cinemagraph_review.mp4` antes).
5. **`12-make-overlay.ps1`** + **`13-pingpong-composite.py`** (usar o python do
   venv do ComfyUI — tem `av` e `PIL`): decodifica o MP4 interpolado, monta a
   sequência **ping-pong** (ida+volta = loop matematicamente sem emenda), faz
   crop/upscale da arte, compõe no canvas claro + overlay de fades e grava o
   WebP animado.
6. **Instalar**: copiar para `%LOCALAPPDATA%\SynQPanel\assets\<GUID>\
   2b-bg-animated.webp` e trocar `<FilePath>2b-bg.png</FilePath>` por
   `2b-bg-animated.webp` no `profiles\<GUID>.xml` (app fechado). Regenerar o
   perfil pelo `02-generate-items.ps1` volta ao fundo estático — repetir este passo.

## Lições aprendidas (a caro preço de GPU)

- **NÃO usar FLF (first-last-frame) com a mesma imagem nas duas âncoras** para
  cinemagraph: o modelo aprende que a solução ótima é não mover NADA (medimos
  0,0% de movimento). Loop perfeito se obtém com ping-pong na montagem.
- **Com o LoRA lightx2v 4-steps (CFG=1) o prompt negativo é IGNORADO** — o modelo
  inventa acessórios (tiara, tranças na 2B). Fidelidade exige sampling completo.
- **Movimentos de objeto (girar o pirulito) quebram a coerência** — o doce descola
  do palito. Melhor congelar objetos no prompt e animar cabelo/tecido/partículas.
- **Validação visual de frames por LLM é não-confiável para deltas sutis** — os
  portões numéricos do passo 3 existem por isso.
- **Padrão do diff revela o TIPO de movimento**: diff oscilante/estável entre frames
  distantes = vento/tecido (bom); diff crescendo monotonicamente = zoom ou drift de
  câmera (ruim para wallpaper — vira zoom-in/out no ping-pong).
- **LTX-2.3 destilado (CFG=1) também ignora o negativo** e tende a zoom lento mesmo
  com 'static camera' no positivo; nos nossos testes o Wan 2.2 full-quality entregou
  vento oscilante de verdade e venceu (receita deste README).
