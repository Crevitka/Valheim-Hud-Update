"""Builds docs/hud-comparison.png from full-screen in-game screenshots.
Row 1: vanilla / Bars / Chevron. Row 2: FoodIconColor Off / Tint / Glow.
Usage: python make_screenshot_grid.py vanilla.png bars_off.png chevron.png tint.png glow.png out.png"""
import sys
from PIL import Image, ImageDraw, ImageFont

CW, CH = 795, 320            # crop of the lower-left HUD corner
BAR, GAP, ROWHEAD = 44, 6, 40
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
FONT2 = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
BG, FG, DIM = (16, 18, 20), (240, 240, 240), (160, 168, 176)

def crop(path):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    return im.crop((0, h - 350, CW, h - 30))

def main(vanilla, bars, chevron, tint, glow, out):
    rows = [("HudStyle", [("Vanilla", vanilla), ("Bars (default)", bars), ("Chevron", chevron)]),
            ("FoodIconColor", [("Off (default)", bars), ("Tint", tint), ("Glow", glow)])]
    W = 3 * CW + 2 * GAP
    H = len(rows) * (ROWHEAD + BAR + CH) + GAP
    img = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(img)
    f1, f2 = ImageFont.truetype(FONT, 26), ImageFont.truetype(FONT2, 24)
    y = 0
    for head, cells in rows:
        d.text((14, y + ROWHEAD // 2), head, font=f2, fill=DIM, anchor="lm")
        y += ROWHEAD
        for i, (label, path) in enumerate(cells):
            x = i * (CW + GAP)
            d.text((x + 14, y + BAR // 2), label, font=f1, fill=FG, anchor="lm")
            img.paste(crop(path), (x, y + BAR))
        y += BAR + CH
    img.save(out, optimize=True)

if __name__ == "__main__":
    main(*sys.argv[1:7])
