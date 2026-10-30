"""Draws snakes and a ladder in a flat style that matches the Kenney tiles.
The snake shape (head at 6.4% of the height, tail at 94.5%, wave = 21% of width)
must stay the same, because BoardView.cs uses it to slide pawns along the body.
Usage: python gen_snakes_ladder.py <out_folder>"""
import sys, math
from PIL import Image, ImageDraw

OUT = sys.argv[1]
S = 4  # draw 4x bigger, then shrink for smooth edges

def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)

def snake(name, body, belly):
    W, H = 220, 1100
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = []
    for i in range(401):
        t = i / 400
        y = 90 + t * 950
        amp = 46 * (0.6 + 0.4 * (1 - t))
        x = W / 2 + amp * math.sin(t * math.pi * 4.2)
        r = 32 * (1 - t) ** 0.7 + 6
        pts.append((x, y, r))
    dark = shade(body, 0.72)

    def circle(x, y, r, fill):
        d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=fill)

    # 1. darker copy shifted down = the flat "depth" edge Kenney art uses
    for x, y, r in pts:
        circle(x, y + 5, r, dark)
    # 2. main body
    for x, y, r in pts:
        circle(x, y, r, body)
    # 3. simple light bands across the body
    for i in range(20, 380, 34):
        x, y, r = pts[i]
        for j in range(i, min(i + 12, 400)):
            bx, by, br = pts[j]
            circle(bx, by, br * 0.55, belly)
    # head: depth, face, eyes
    hx, hy = pts[0][0], 70
    d.ellipse([(hx - 46) * S, (hy - 38) * S, (hx + 46) * S, (hy + 48) * S], fill=dark)
    d.ellipse([(hx - 46) * S, (hy - 42) * S, (hx + 46) * S, (hy + 42) * S], fill=body)
    for ex in (-19, 19):
        d.ellipse([(hx + ex - 12) * S, (hy - 20) * S, (hx + ex + 12) * S, (hy + 4) * S], fill=(255, 255, 255, 255))
        d.ellipse([(hx + ex - 5) * S, (hy - 13) * S, (hx + ex + 5) * S, (hy - 1) * S], fill=(40, 40, 50, 255))
    # tongue
    tc = (230, 60, 80, 255)
    d.line([(hx * S, (hy - 40) * S), (hx * S, (hy - 60) * S)], fill=tc, width=5 * S)
    d.line([(hx * S, (hy - 58) * S), ((hx - 8) * S, (hy - 70) * S)], fill=tc, width=5 * S)
    d.line([(hx * S, (hy - 58) * S), ((hx + 8) * S, (hy - 70) * S)], fill=tc, width=5 * S)
    img.resize((W, H), Image.LANCZOS).save(f"{OUT}/{name}.png")

def ladder():
    W, H = 200, 1100
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    wood, dark = (232, 170, 92, 255), (184, 120, 56, 255)
    def rect(x0, y0, x1, y1, fill, r=6):
        d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=fill)
    for y in range(110, H - 60, 110):          # rungs with a darker bottom edge
        rect(40, y - 10, 160, y + 14, dark)
        rect(40, y - 10, 160, y + 8, wood)
    for x in (26, 150):                        # side rails
        rect(x, 20, x + 24, H - 16, dark, 10)
        rect(x, 20, x + 24, H - 24, wood, 10)
    img.resize((W, H), Image.LANCZOS).save(f"{OUT}/ladder.png")

if __name__ == "__main__":
    # File names stay the same so the game loads them. Colors chosen to stand
    # out on the blue, green, red and yellow tiles.
    snake("snake_green", (120, 200, 60, 255), (186, 236, 120, 255))
    snake("snake_purple", (156, 98, 220, 255), (210, 176, 250, 255))
    snake("snake_red", (250, 132, 40, 255), (255, 200, 120, 255))   # orange, so it shows on red tiles
    ladder()
    print("done")
