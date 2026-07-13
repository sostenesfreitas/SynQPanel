# Monta o WebP animado do painel a partir do mp4 interpolado:
# ping-pong (ida + volta = loop perfeito sem exigir FLF), crop/upscale da arte,
# composicao no canvas claro + overlay de fades.
import sys
import av
from PIL import Image

mp4_path, overlay_path, out_path = sys.argv[1], sys.argv[2], sys.argv[3]

CANVAS = (1100, 3840)
BG = (245, 244, 242)
ART_Y = 192
CROP = (98, 0, 98 + 524, 1280)   # crop central do 720x1280
ART_SIZE = (1100, 2688)
FPS = 32

overlay = Image.open(overlay_path).convert("RGBA")

container = av.open(mp4_path)
frames = []
for frame in container.decode(video=0):
    img = frame.to_image().convert("RGB")
    art = img.crop(CROP).resize(ART_SIZE, Image.LANCZOS)
    canvas = Image.new("RGB", CANVAS, BG)
    canvas.paste(art, (0, ART_Y))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), overlay).convert("RGB")
    frames.append(canvas)
container.close()

print(f"frames decodificados/compostos: {len(frames)}")

# ping-pong: ida + volta sem duplicar as pontas
seq = frames + frames[-2:0:-1]
print(f"sequencia ping-pong: {len(seq)} frames ({len(seq)/FPS:.1f}s de loop)")

seq[0].save(
    out_path,
    save_all=True,
    append_images=seq[1:],
    duration=int(round(1000 / FPS)),
    loop=0,
    quality=85,
    method=4,
)
print(f"OK: {out_path}")
