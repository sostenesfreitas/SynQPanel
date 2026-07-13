# Fundo animado do painel 2B (cinemagraph via ComfyUI)

Pipeline que transforma o wallpaper estático da 2B em fundo **animado em loop
perfeito** (WebP 1100×3840 @ 32fps) para o SynQPanel. Requer ComfyUI (testado no
Desktop, servidor em `127.0.0.1:8188`) com os modelos **Wan 2.2 14B I2V** (template
oficial) e **RIFE** (`rife_v4.26.safetensors` de
https://huggingface.co/Comfy-Org/frame_interpolation em `models/frame_interpolation/`).

## Passos (tudo via API — `POST /prompt`)

1. **Upload** do wallpaper original para os inputs do ComfyUI (`POST /upload/image`).
2. **`10-wan22-flf-workflow.json`** — gera o vídeo cinemagraph (81 frames, 720×1280,
   20 steps, CFG 3.5, sampling completo SEM o LoRA lightx2v):
   - `WanFirstLastFrameToVideo` com a MESMA imagem em `start_image` e `end_image`
     → o vídeo nasce em loop sem emenda.
   - Prompt positivo restringe o movimento (pose congelada, pirulito girando mas
     sempre na boca, fios de cabelo, partículas); negativo proíbe os artefatos
     recorrentes (tiara/headband, tranças, brincos, pirulito saindo da boca).
   - Com o LoRA de 4 steps (CFG=1) o negativo é IGNORADO e o modelo inventa
     acessórios — por isso o sampling completo (~30-45 min na RTX 5090).
3. **Upload do vídeo gerado** de volta aos inputs + **`12-make-overlay.ps1`**
   (gera/re-gera o overlay RGBA de fades+separador — mesma geometria do
   `01-generate-background.ps1`, separador em y3330).
4. **`11-composite-workflow.json`** — RIFE 2× (16→32fps), remove o último frame
   (duplicado do primeiro no loop FLF), crop central + upscale para a zona da arte,
   composição no canvas claro + overlay, exporta `SaveAnimatedWEBP`.
5. **Instalar**: copiar o WebP para `%LOCALAPPDATA%\SynQPanel\assets\<GUID>\
   2b-bg-animated.webp` e trocar `<FilePath>2b-bg.png</FilePath>` por
   `2b-bg-animated.webp` no `profiles\<GUID>.xml` (app fechado).

> Nota: o `02-generate-items.ps1` continua gerando o perfil com o fundo ESTÁTICO
> (`2b-bg.png`). Se regenerar/reinstalar o perfil, repita o passo 5 para voltar ao
> fundo animado.
