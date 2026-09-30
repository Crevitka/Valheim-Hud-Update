"""Builds docs/icon-comparison.png: every HUD Update icon next to the vanilla
icons it replaces. The vanilla icons come from the HUD-Update-IconDump helper
(BepInEx/HUD-Update-IconDump/food/*.png, guardian/*.png, foods.tsv).

Usage: python make_icon_comparison.py <IconDump folder> <mod folder> <out.png>

The food -> glyph mapping is read from HudUpdatePlugin.cs (FoodGlyphByPrefab and
FoodGlyphKeywords), so the picture always matches the code."""
import os
import re
import sys
from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FONT_B = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BG, CELL, FG, DIM, LINE = (22, 24, 27), (38, 41, 45), (236, 236, 236), (150, 156, 164), (60, 64, 70)
ICON, PAD, NAME_H, COLS = 64, 10, 30, 10
CELL_W = ICON + 2 * PAD + 26          # room for the food name under the icon
ROW_H = ICON + NAME_H + 2 * PAD
OURS_W = 176

GUARDIANS = [("GP_Eikthyr", "Eikthyr"), ("GP_TheElder", "TheElder"), ("GP_Bonemass", "Bonemass")]


def parse_mapping(source):
    table = dict(re.findall(r'\{\s*"(\w+)",\s*"(\w+)"\s*\}', source.split("FoodGlyphByPrefab", 1)[1].split("};", 1)[0]))
    keywords = re.findall(r'new\[\]\s*\{\s*"(\w+)",\s*"(\w+)"\s*\}', source.split("FoodGlyphKeywords", 1)[1].split("};", 1)[0])
    return table, keywords


def resolve(prefab, name, icons, table, keywords):
    normalized = name.replace("$item_", "").replace("_", "")
    for key in (name, prefab, normalized):
        if key in icons and not key.startswith("food_"):
            return key
    if prefab in table:
        return "food_" + table[prefab]
    hay = (prefab + " " + normalized).lower()
    for word, glyph in keywords:
        if word in hay:
            return "food_" + glyph
    return None


def fit(img, size):
    img = img.convert("RGBA")
    img.thumbnail((size, size), Image.LANCZOS)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(img, ((size - img.width) // 2, (size - img.height) // 2), img)
    return out


def short(text, font, width, draw):
    while draw.textlength(text, font=font) > width and len(text) > 3:
        text = text[:-2]
    return text if draw.textlength(text, font=font) <= width else text[:3]


def main(dump, mod, out):
    source = open(os.path.join(mod, "HudUpdatePlugin.cs"), encoding="utf-8").read()
    table, keywords = parse_mapping(source)
    icons = {f[:-4]: os.path.join(mod, "Icons", f) for f in os.listdir(os.path.join(mod, "Icons")) if f.endswith(".png")}

    groups = {}
    for line in open(os.path.join(dump, "foods.tsv"), encoding="utf-8").read().splitlines()[1:]:
        cols = line.split("\t")
        prefab, name = cols[:2]
        if len(cols) > 5 and cols[5] != "Consumable":   # raw ingredients carry food stats but cannot be eaten
            continue
        png = os.path.join(dump, "food", prefab + ".png")
        if not os.path.exists(png):
            continue
        key = resolve(prefab, name, icons, table, keywords)
        groups.setdefault(key, []).append((prefab, png))

    rows = []
    for gp, emblem in GUARDIANS:
        png = os.path.join(dump, "guardian", gp + ".png")
        if os.path.exists(png):
            rows.append((emblem, [(gp.replace("GP_", ""), png)]))
    for key in sorted(k for k in groups if k):
        rows.append((key, sorted(groups[key])))
    if None in groups:
        rows.append((None, sorted(groups[None])))

    # Split long groups over several lines.
    lines = []
    for key, items in rows:
        for i in range(0, len(items), COLS):
            lines.append((key if i == 0 else "", items[i:i + COLS]))

    width = OURS_W + COLS * CELL_W + PAD
    head = 56
    height = head + len(lines) * ROW_H + PAD
    img = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(img)
    f_head, f_name, f_key = ImageFont.truetype(FONT_B, 22), ImageFont.truetype(FONT, 12), ImageFont.truetype(FONT, 13)
    d.text((PAD + 4, head // 2), "HUD Update", font=f_head, fill=FG, anchor="lm")
    d.text((OURS_W + PAD, head // 2), "Vanilla icons it replaces", font=f_head, fill=DIM, anchor="lm")

    y = head
    for key, items in lines:
        if key != "":
            d.line((PAD, y, width - PAD, y), fill=LINE)
        if key:
            tile = Image.new("RGBA", (ICON + 16, ICON + 16), CELL + (255,))
            ours = fit(Image.open(icons[key]), ICON)
            tile.alpha_composite(ours, (8, 8))
            img.paste(tile.convert("RGB"), (PAD + (OURS_W - PAD - tile.width) // 2 - 8, y + PAD))
            label = key.replace("food_", "")
            d.text((PAD + (OURS_W - PAD) // 2 - 8, y + PAD + ICON + 24), label, font=f_key, fill=DIM, anchor="mm")
        elif key is None:
            d.text((PAD + (OURS_W - PAD) // 2 - 8, y + ROW_H // 2), "game icon", font=f_key, fill=DIM, anchor="mm")
        for i, (name, png) in enumerate(items):
            x = OURS_W + i * CELL_W
            van = fit(Image.open(png), ICON)
            img.paste(van, (x + (CELL_W - ICON) // 2, y + PAD), van)
            d.text((x + CELL_W // 2, y + PAD + ICON + 14), short(name, f_name, CELL_W - 6, d), font=f_name, fill=DIM, anchor="mm")
        y += ROW_H
    img.save(out, optimize=True)
    print(out, img.size, sum(len(v) for v in groups.values()), "foods")


if __name__ == "__main__":
    main(*sys.argv[1:4])
