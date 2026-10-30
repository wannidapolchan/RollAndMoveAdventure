"""Generates all sprite art for Roll-and-Move Adventure.
Everything is drawn at 4x size and scaled down for smooth edges."""
import math, random, sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[1]
FONT = sys.argv[2]
S = 4  # supersample factor
random.seed(7)

def font(px):
    return ImageFont.truetype(FONT, px * S)

def canvas(w, h, color=(0, 0, 0, 0)):
    return Image.new("RGBA", (w * S, h * S), color)

def save(img, name):
    w, h = img.size
    img.resize((w // S, h // S), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))

def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + ((c[3],) if len(c) == 4 else (255,))

def rrect(d, box, r, fill):
    if len(fill) == 4 and 0 < fill[3] < 255:
        layer = Image.new("RGBA", d._image.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rounded_rectangle([v * S for v in box], radius=r * S, fill=fill)
        d._image.alpha_composite(layer)
    else:
        d.rounded_rectangle([v * S for v in box], radius=r * S, fill=fill)

def soft_ellipse(d, box, fill):
    layer = Image.new("RGBA", d._image.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(box, fill=fill)
    d._image.alpha_composite(layer)

def soft_polygon(d, pts, fill):
    layer = Image.new("RGBA", d._image.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).polygon(pts, fill=fill)
    d._image.alpha_composite(layer)

def text_center(d, cx, cy, txt, px, fill, stroke=0, stroke_fill=None):
    f = font(px)
    d.text((cx * S, cy * S), txt, font=f, fill=fill, anchor="mm",
           stroke_width=stroke * S, stroke_fill=stroke_fill)

# ---------- background ----------
def background():
    W, H = 1920, 1080
    img = Image.new("RGBA", (W, H))
    top, bot = (38, 70, 83), (24, 46, 58)
    px = img.load()
    for y in range(H):
        t = y / H
        c = tuple(int(top[i] * (1 - t) + bot[i] * t) for i in range(3)) + (255,)
        for x in range(W):
            px[x, y] = c
    d = ImageDraw.Draw(img, "RGBA")
    # soft dotted pattern
    for y in range(0, H, 48):
        for x in range((y // 48) % 2 * 24, W, 48):
            d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(255, 255, 255, 14))
    # vignette
    vig = Image.new("L", (W, H), 0)
    vd = ImageDraw.Draw(vig)
    vd.ellipse([-300, -250, W + 300, H + 250], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(160))
    dark = Image.new("RGBA", (W, H), (10, 20, 28, 255))
    img = Image.composite(img, dark, vig)
    img.save(os.path.join(OUT, "background.png"))

# ---------- tiles ----------
def tile(name, base, icon=None):
    T = 160
    img = canvas(T, T)
    d = ImageDraw.Draw(img, "RGBA")
    rrect(d, (6, 10, T - 6, T - 4), 28, shade(base, 0.62))     # bottom edge (3D)
    rrect(d, (6, 4, T - 6, T - 12), 28, base)                   # face
    rrect(d, (16, 12, T - 16, 40), 14, (255, 255, 255, 50))     # shine
    if icon:
        icon(d, T)
    save(img, name)

def coin_icon(d, T):
    cx, cy, r = T / 2 + 10, T / 2 + 6, 34
    d.ellipse([(cx - r) * S, (cy - r + 5) * S, (cx + r) * S, (cy + r + 5) * S], fill=(176, 120, 20, 255))
    d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=(255, 205, 60, 255))
    d.ellipse([(cx - r + 8) * S, (cy - r + 8) * S, (cx + r - 8) * S, (cy + r - 8) * S], outline=(210, 150, 30, 255), width=4 * S)
    text_center(d, cx, cy + 1, "$", 40, (200, 135, 20, 255))

def event_icon(d, T):
    cx, cy = T / 2 + 10, T / 2 + 6
    for i, (ox, rot) in enumerate(((-10, -12), (8, 10))):
        card = Image.new("RGBA", (60 * S, 80 * S), (0, 0, 0, 0))
        cd = ImageDraw.Draw(card)
        cd.rounded_rectangle([0, 0, 60 * S - 1, 80 * S - 1], radius=10 * S, fill=(255, 255, 255, 255), outline=(90, 60, 140, 255), width=4 * S)
        if i == 1:
            cd.text((30 * S, 42 * S), "?", font=font(46), fill=(120, 80, 190, 255), anchor="mm")
        card = card.rotate(rot, expand=True, resample=Image.BICUBIC)
        d._image.alpha_composite(card, (int((cx + ox) * S - card.width / 2), int(cy * S - card.height / 2)))

def start_icon(d, T):
    text_center(d, T / 2, T / 2 + 12, "START", 30, (255, 255, 255, 255), 3, (40, 120, 90, 255))

def finish_icon(d, T):
    # checkered flag
    x0, y0, sq = 52, 40, 12
    d.rectangle([(x0 - 6) * S, (y0) * S, (x0 - 2) * S, (y0 + 80) * S], fill=(90, 60, 30, 255))
    for r in range(4):
        for c in range(5):
            col = (30, 30, 30, 255) if (r + c) % 2 == 0 else (255, 255, 255, 255)
            d.rectangle([(x0 + c * sq) * S, (y0 + r * sq) * S, (x0 + (c + 1) * sq) * S, (y0 + (r + 1) * sq) * S], fill=col)
    text_center(d, T / 2, T / 2 + 46, "FINISH", 26, (255, 255, 255, 255), 3, (170, 100, 10, 255))

# ---------- snake ----------
def snake(name, body, belly, spot):
    W, H = 220, 1100
    img = canvas(W, H)
    d = ImageDraw.Draw(img, "RGBA")
    pts = []
    n = 400
    for i in range(n + 1):
        t = i / n
        y = 90 + t * (H - 150)
        amp = 46 * (0.6 + 0.4 * (1 - t))
        x = W / 2 + amp * math.sin(t * math.pi * 4.2)
        r = 34 * (1 - t) ** 0.7 + 6
        pts.append((x, y, r))
    # outline, body, belly stripe, spots
    for x, y, r in pts:
        d.ellipse([(x - r - 4) * S, (y - r - 4) * S, (x + r + 4) * S, (y + r + 4) * S], fill=shade(body, 0.45))
    for x, y, r in pts:
        d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=body)
    for x, y, r in pts:
        d.ellipse([(x - r * 0.45 - r * 0.25) * S, (y - r * 0.45) * S, (x + r * 0.45 - r * 0.25) * S, (y + r * 0.45) * S], fill=belly)
    for i in range(14, n - 20, 26):
        x, y, r = pts[i]
        rr = r * 0.42
        d.ellipse([(x + r * 0.25 - rr) * S, (y - rr) * S, (x + r * 0.25 + rr) * S, (y + rr) * S], fill=spot)
    # head
    hx, hy = pts[0][0], 70
    d.ellipse([(hx - 52) * S, (hy - 46) * S, (hx + 52) * S, (hy + 50) * S], fill=shade(body, 0.45))
    d.ellipse([(hx - 48) * S, (hy - 42) * S, (hx + 48) * S, (hy + 46) * S], fill=body)
    # tongue
    d.line([(hx * S, (hy - 40) * S), (hx * S, (hy - 64) * S)], fill=(220, 40, 60, 255), width=5 * S)
    d.line([(hx * S, (hy - 62) * S), ((hx - 9) * S, (hy - 74) * S)], fill=(220, 40, 60, 255), width=5 * S)
    d.line([(hx * S, (hy - 62) * S), ((hx + 9) * S, (hy - 74) * S)], fill=(220, 40, 60, 255), width=5 * S)
    for ex in (-22, 22):
        d.ellipse([(hx + ex - 14) * S, (hy - 22) * S, (hx + ex + 14) * S, (hy + 6) * S], fill=(255, 255, 255, 255))
        d.ellipse([(hx + ex - 6) * S, (hy - 16) * S, (hx + ex + 6) * S, (hy + 2) * S], fill=(20, 20, 20, 255))
        d.ellipse([(hx + ex - 3) * S, (hy - 14) * S, (hx + ex + 1) * S, (hy - 10) * S], fill=(255, 255, 255, 255))
    # nostrils and cheeks
    d.ellipse([(hx - 8) * S, (hy - 34) * S, (hx - 3) * S, (hy - 29) * S], fill=shade(body, 0.4))
    d.ellipse([(hx + 3) * S, (hy - 34) * S, (hx + 8) * S, (hy - 29) * S], fill=shade(body, 0.4))
    save(img, name)

# ---------- ladder ----------
def ladder():
    W, H = 200, 1100
    img = canvas(W, H)
    d = ImageDraw.Draw(img, "RGBA")
    wood, dark, light = (196, 132, 72, 255), (120, 74, 34, 255), (232, 178, 112, 255)
    for i, y in enumerate(range(110, H - 60, 110)):
        d.rounded_rectangle([40 * S, (y - 12) * S, 160 * S, (y + 12) * S], radius=8 * S, fill=dark)
        d.rounded_rectangle([40 * S, (y - 12) * S, 160 * S, (y + 7) * S], radius=8 * S, fill=wood)
    for x in (28, 152):
        d.rounded_rectangle([(x) * S, 20 * S, (x + 22) * S, (H - 20) * S], radius=10 * S, fill=dark)
        d.rounded_rectangle([(x) * S, 20 * S, (x + 16) * S, (H - 24) * S], radius=10 * S, fill=wood)
        d.rounded_rectangle([(x + 3) * S, 26 * S, (x + 8) * S, (H - 30) * S], radius=3 * S, fill=light)
    save(img, "ladder")

# ---------- pawn token ----------
def pawn(name, color):
    W, H = 140, 190
    img = canvas(W, H)
    d = ImageDraw.Draw(img, "RGBA")
    dk = shade(color, 0.55)
    d.ellipse([18 * S, 150 * S, 122 * S, 186 * S], fill=dk)
    d.ellipse([18 * S, 144 * S, 122 * S, 178 * S], fill=color)
    d.polygon([(38 * S, 160 * S), (102 * S, 160 * S), (84 * S, 80 * S), (56 * S, 80 * S)], fill=dk)
    d.polygon([(42 * S, 158 * S), (98 * S, 158 * S), (82 * S, 82 * S), (58 * S, 82 * S)], fill=color)
    d.ellipse([40 * S, 74 * S, 100 * S, 96 * S], fill=dk)
    d.ellipse([28 * S, 8 * S, 112 * S, 92 * S], fill=dk)
    d.ellipse([30 * S, 8 * S, 110 * S, 86 * S], fill=color)
    soft_ellipse(d, [46 * S, 20 * S, 72 * S, 42 * S], (255, 255, 255, 140))
    soft_polygon(d, [(52 * S, 95 * S), (58 * S, 95 * S), (48 * S, 150 * S), (44 * S, 150 * S)], (255, 255, 255, 70))
    # face
    for ex in (56, 84):
        d.ellipse([(ex - 7) * S, 42 * S, (ex + 7) * S, 58 * S], fill=(255, 255, 255, 255))
        d.ellipse([(ex - 3) * S, 46 * S, (ex + 4) * S, 56 * S], fill=(25, 25, 35, 255))
    d.arc([58 * S, 54 * S, 82 * S, 72 * S], 20, 160, fill=(25, 25, 35, 255), width=3 * S)
    save(img, name)

def shadow():
    img = canvas(128, 48)
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, 128 * S - 1, 48 * S - 1], fill=(0, 0, 0, 90))
    save(img, "shadow")

# ---------- dice ----------
PIPS = {1: [(0, 0)], 2: [(-1, -1), (1, 1)], 3: [(-1, -1), (0, 0), (1, 1)],
        4: [(-1, -1), (1, -1), (-1, 1), (1, 1)], 5: [(-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1)],
        6: [(-1, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1)]}

def die_base(face, edge):
    img = canvas(160, 160)
    d = ImageDraw.Draw(img, "RGBA")
    rrect(d, (8, 12, 152, 154), 30, edge)
    rrect(d, (8, 6, 152, 146), 30, face)
    rrect(d, (20, 14, 140, 36), 12, (255, 255, 255, 60))
    return img, d

def plain_die(n):
    img, d = die_base((250, 248, 240, 255), (180, 172, 160, 255))
    for px, py in PIPS[n]:
        cx, cy = 80 + px * 36, 76 + py * 36
        d.ellipse([(cx - 13) * S, (cy - 13) * S, (cx + 13) * S, (cy + 13) * S], fill=(40, 40, 52, 255))
    save(img, "die_plain_%d" % n)

def lucky_die(v):
    img, d = die_base((255, 196, 50, 255), (196, 110, 20, 255))
    for sx, sy in ((30, 28), (130, 28), (30, 124), (130, 124)):
        star(d, sx, sy, 9, (255, 250, 220, 255))
    label = str(v)
    col = (200, 40, 50, 255) if v <= 0 else (120, 50, 160, 255)
    text_center(d, 80, 78, label, 74 if len(label) < 2 else 64, col, 4, (255, 255, 255, 255))
    save(img, "die_lucky_%s" % ("m2" if v == -2 else str(v)))

def star(d, cx, cy, r, fill):
    pts = []
    for i in range(8):
        a = math.pi / 4 * i - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.38
        pts.append(((cx + rr * math.cos(a)) * S, (cy + rr * math.sin(a)) * S))
    d.polygon(pts, fill=fill)

# ---------- small sprites ----------
def coin():
    img = canvas(96, 96)
    d = ImageDraw.Draw(img, "RGBA")
    d.ellipse([6 * S, 10 * S, 90 * S, 94 * S], fill=(176, 120, 20, 255))
    d.ellipse([6 * S, 4 * S, 90 * S, 88 * S], fill=(255, 205, 60, 255))
    d.ellipse([16 * S, 14 * S, 80 * S, 78 * S], outline=(214, 154, 34, 255), width=5 * S)
    text_center(d, 48, 47, "$", 46, (205, 140, 25, 255))
    soft_ellipse(d, [22 * S, 16 * S, 40 * S, 30 * S], (255, 255, 255, 120))
    save(img, "coin")

def sparkle():
    img = canvas(64, 64)
    d = ImageDraw.Draw(img, "RGBA")
    star(d, 32, 32, 30, (255, 255, 255, 255))
    save(img, "sparkle")
    img = canvas(64, 64)
    d = ImageDraw.Draw(img, "RGBA")
    d.ellipse([4 * S, 4 * S, 60 * S, 60 * S], fill=(255, 255, 255, 255))
    save(img, "dot")
    img = canvas(24, 24)
    d = ImageDraw.Draw(img, "RGBA")
    d.rectangle([0, 0, 24 * S, 24 * S], fill=(255, 255, 255, 255))
    save(img, "square")

# ---------- UI ----------
def panel(name, fill, edge):
    img = canvas(128, 128)
    d = ImageDraw.Draw(img, "RGBA")
    rrect(d, (2, 2, 126, 126), 26, edge)
    rrect(d, (6, 6, 122, 122), 22, fill)
    save(img, name)

def button(name, base):
    img = canvas(128, 72)
    d = ImageDraw.Draw(img, "RGBA")
    rrect(d, (2, 8, 126, 70), 20, shade(base, 0.6))
    rrect(d, (2, 2, 126, 62), 20, base)
    rrect(d, (12, 6, 116, 22), 10, (255, 255, 255, 55))
    save(img, name)

def card(name, banner):
    img = canvas(520, 640)
    d = ImageDraw.Draw(img, "RGBA")
    rrect(d, (6, 14, 514, 634), 36, (120, 100, 80, 255))
    rrect(d, (6, 6, 514, 626), 36, (255, 248, 232, 255))
    rrect(d, (22, 22, 498, 150), 26, banner)
    rrect(d, (34, 28, 486, 56), 14, (255, 255, 255, 60))
    d.rounded_rectangle([22 * S, 166 * S, 498 * S, 610 * S], radius=22 * S, outline=(230, 214, 186, 255), width=4 * S)
    save(img, name)

if __name__ == "__main__":
    background()
    tile("tile_a", (255, 236, 205, 255))
    tile("tile_b", (250, 214, 176, 255))
    tile("tile_coin", (255, 226, 120, 255), coin_icon)
    tile("tile_event", (204, 180, 248, 255), event_icon)
    tile("tile_start", (110, 210, 160, 255), start_icon)
    tile("tile_finish", (255, 180, 70, 255), finish_icon)
    snake("snake_green", (96, 186, 74, 255), (220, 240, 140, 255), (52, 120, 50, 255))
    snake("snake_purple", (150, 96, 206, 255), (236, 200, 255, 255), (98, 56, 150, 255))
    snake("snake_red", (226, 88, 78, 255), (255, 214, 150, 255), (150, 46, 46, 255))
    ladder()
    pawn("pawn_blue", (70, 140, 240, 255))
    pawn("pawn_orange", (250, 140, 50, 255))
    shadow()
    for n in range(1, 7):
        plain_die(n)
    for v in (-2, 0, 3, 5, 8, 10):
        lucky_die(v)
    coin()
    sparkle()
    panel("panel_dark", (22, 36, 48, 235), (255, 255, 255, 40))
    panel("panel_light", (255, 248, 232, 255), (196, 170, 130, 255))
    button("button", (250, 140, 50, 255))
    button("button_hover", (255, 165, 80, 255))
    button("button_down", (220, 112, 30, 255))
    button("button_off", (130, 130, 140, 255))
    button("button_blue", (70, 140, 240, 255))
    button("button_blue_hover", (100, 165, 250, 255))
    card("card_good", (96, 190, 120, 255))
    card("card_bad", (226, 96, 86, 255))
    print("art done")
