"""Draws panel_dark and panel_light in the same flat style as the Kenney tiles:
rounded box, a thin lighter rim, and a darker bottom edge for depth.
128 x 128 images; the game stretches the middle and keeps the 32-pixel corners.
Usage: python gen_panels.py <out_folder>"""
import sys
from PIL import Image, ImageDraw

OUT = sys.argv[1]
S = 4

def panel(name, face, edge, rim):
    img = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([2 * S, 4 * S, 126 * S, 126 * S], radius=20 * S, fill=edge)   # depth edge
    d.rounded_rectangle([2 * S, 2 * S, 126 * S, 118 * S], radius=20 * S, fill=rim)    # rim
    d.rounded_rectangle([6 * S, 6 * S, 122 * S, 114 * S], radius=16 * S, fill=face)   # face
    img.resize((128, 128), Image.LANCZOS).save(f"{OUT}/{name}.png")

if __name__ == "__main__":
    panel("panel_dark", (44, 58, 72, 250), (24, 32, 40, 250), (66, 82, 98, 250))
    panel("panel_light", (252, 249, 242, 255), (196, 188, 172, 255), (232, 226, 214, 255))
    print("done")
