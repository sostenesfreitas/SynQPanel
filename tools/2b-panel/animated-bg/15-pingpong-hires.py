# Montador para frames JA na resolucao da arte (1100x2688, supersampled):
# somente paste no canvas + overlay + ping-pong.
import sys
import av
from PIL import Image

mp4_path, overlay_path, out_path = sys.argv[1], sys.argv[2], sys.argv[3]

CANVAS = (1100, 3840)
BG = (245, 244, 242)
ART_Y = 192
FPS = 32

overlay = Image.open(overlay_path).convert("RGBA")

container = av.open(mp4_path)
frames = []
for frame in container.decode(video=0):
    img = frame.to_image().convert("RGB")
    canvas = Image.new("RGB", CANVAS, BG)
    canvas.paste(img, (0, ART_Y))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), overlay).convert("RGB")
    frames.append(canvas)
container.close()

print(f"frames compostos: {len(frames)} ({frames[0].size if frames else 'nenhum'})")

seq = frames + frames[-2:0:-1]
print(f"ping-pong: {len(seq)} frames ({len(seq)/FPS:.1f}s de loop)")

seq[0].save(
    out_path,
    save_all=True,
    append_images=seq[1:],
    duration=int(round(1000 / FPS)),
    loop=0,
    quality=88,
    method=4,
)
print(f"OK: {out_path}")
