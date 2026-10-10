"""Procedural app icon for Village Clash: isometric town hall on a grass tile."""
import sys
from PIL import Image, ImageDraw

S = 1024
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# Rounded-square background (sky to grass gradient)
bg = Image.new("RGBA", (S, S), (0, 0, 0, 0))
bd = ImageDraw.Draw(bg)
for y in range(S):
    t = y / S
    r = int(120 + (72 - 120) * t); g = int(190 + (140 - 190) * t); b = int(235 + (60 - 235) * t)
    bd.line([(0, y), (S, y)], fill=(r, g, b, 255))
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=220, fill=255)
img.paste(bg, (0, 0), mask)
d = ImageDraw.Draw(img)

cx, cy = S // 2, S // 2 + 40
# Grass iso diamond
W, H = 420, 210
grass = [(cx, cy - H + 120), (cx + W, cy + 120), (cx, cy + H + 120), (cx - W, cy + 120)]
d.polygon(grass, fill=(92, 158, 58, 255))
d.polygon([(cx, cy + H + 120), (cx + W, cy + 120), (cx + W, cy + 150), (cx, cy + H + 150)], fill=(110, 80, 40, 255))
d.polygon([(cx, cy + H + 120), (cx - W, cy + 120), (cx - W, cy + 150), (cx, cy + H + 150)], fill=(85, 60, 30, 255))

# Town hall body (iso box)
bw, bh, bz = 230, 115, 230
top = cy - 20
left = [(cx - bw, top), (cx, top + bh), (cx, top + bh + bz), (cx - bw, top + bz)]
right = [(cx + bw, top), (cx, top + bh), (cx, top + bh + bz), (cx + bw, top + bz)]
d.polygon(left, fill=(205, 170, 120, 255))
d.polygon(right, fill=(170, 135, 90, 255))
# door
d.rectangle([cx - 60, top + bh + 60, cx - 8, top + bh + bz - 10], fill=(70, 45, 25, 255))
# windows
d.rectangle([cx + 40, top + 80, cx + 90, top + 150], fill=(60, 90, 130, 255))
d.rectangle([cx + 130, top + 40, cx + 180, top + 110], fill=(60, 90, 130, 255))
# Roof (iso pyramid, red)
apex = (cx, top - 190)
d.polygon([apex, (cx - bw - 25, top + 10), (cx, top + bh + 20)], fill=(205, 60, 50, 255))
d.polygon([apex, (cx + bw + 25, top + 10), (cx, top + bh + 20)], fill=(160, 40, 35, 255))
d.polygon([apex, (cx - bw - 25, top + 10), (cx, top - 100)], fill=(225, 80, 65, 255))
d.polygon([apex, (cx + bw + 25, top + 10), (cx, top - 100)], fill=(180, 50, 42, 255))
# Flag
d.line([(cx, apex[1]), (cx, apex[1] - 120)], fill=(70, 45, 25, 255), width=10)
d.polygon([(cx + 5, apex[1] - 120), (cx + 95, apex[1] - 95), (cx + 5, apex[1] - 70)], fill=(250, 200, 50, 255))

# Gold star badge bottom-right
import math
sx, sy, R, r = S - 220, S - 220, 120, 52
pts = []
for i in range(10):
    a = -math.pi / 2 + i * math.pi / 5
    rad = R if i % 2 == 0 else r
    pts.append((sx + rad * math.cos(a), sy + rad * math.sin(a)))
d.polygon(pts, fill=(255, 205, 60, 255), outline=(170, 120, 20, 255), width=8)

out = sys.argv[1]
img.save(out)
for n in (512, 256, 128, 64, 32, 16):
    img.resize((n, n), Image.LANCZOS).save(out.replace(".png", f"_{n}.png"))
print("wrote", out)
