"""Builds game images from Kenney pieces (CC0):
  - Lucky Die faces: the red die with its dot removed, plus a big number
  - Life Event tile icon: the blue card with a "?"
  - Life Event cards: the green card (good) and red card (bad)
Usage: python make_from_kenney.py <kenney_folder> <out_folder> <font.ttf>"""
import sys, math
from PIL import Image, ImageDraw, ImageFont

KEN, OUT, FONT = sys.argv[1], sys.argv[2], sys.argv[3]

def font(px):
    return ImageFont.truetype(FONT, px)

# ---- Lucky Die: blank red die + number
die = Image.open(f"{KEN}/die_red_1.png").convert("RGBA")
w, h = die.size
red = die.getpixel((w // 4, h // 2))
px = die.load()
cx, cy = w / 2, h / 2
for y in range(h):
    for x in range(w):
        if (x - cx) ** 2 + (y - cy) ** 2 < (w * 0.16) ** 2:   # cover the center dot
            px[x, y] = red
big = die.resize((256, 256), Image.LANCZOS)                     # bigger, so the number is sharp
for value in (-2, 0, 3, 5, 8, 10):
    img = big.copy()
    d = ImageDraw.Draw(img)
    d.text((128, 124), str(value), font=font(120 if len(str(value)) < 2 else 104),
           fill=(255, 255, 255, 255), anchor="mm", stroke_width=6, stroke_fill=(120, 36, 36, 255))
    img.save(f"{OUT}/die_lucky_{'m2' if value == -2 else value}.png")

# ---- Life Event tile icon: blue card with "?", slightly tilted
card = Image.open(f"{KEN}/card_blue.png").convert("RGBA").resize((105, 142), Image.LANCZOS)
d = ImageDraw.Draw(card)
d.text((52, 74), "?", font=font(78), fill=(255, 255, 255, 255), anchor="mm")
card = card.rotate(-8, expand=True, resample=Image.BICUBIC)
icon = Image.new("RGBA", (160, 160), (0, 0, 0, 0))
icon.alpha_composite(card, ((160 - card.width) // 2, (160 - card.height) // 2))
icon.save(f"{OUT}/icon_event.png")

# ---- Life Event cards (made 3x bigger so they stay smooth on screen)
for src, name in (("card_green", "card_good"), ("card_red", "card_bad")):
    c = Image.open(f"{KEN}/{src}.png").convert("RGBA")
    c.resize((c.width * 3, c.height * 3), Image.LANCZOS).save(f"{OUT}/{name}.png")
print("done")
