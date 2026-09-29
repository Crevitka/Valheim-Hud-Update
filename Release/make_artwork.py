"""Cover artwork (1600x900) for HUD Update. Artwork, not a gameplay screenshot:
both HUD styles are drawn from the mod's own icons and the Figma chevron."""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(ROOT)
ICONS = os.path.join(MOD, 'Icons')
BOLD = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
REG = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
W, H = 1600, 900
CELL = (0, 0, 0, 200)
RED = (173, 22, 22, 255)
STAM = (214, 166, 110, 255)


def icon(name, size, alpha=255):
    im = Image.open((os.path.join(ICONS, name) if os.path.exists(os.path.join(ICONS, name)) else os.path.join(ROOT, 'Artwork', 'src', name))).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    if alpha < 255:
        a = im.getchannel('A').point(lambda v: v * alpha // 255)
        im.putalpha(a)
    return im


def diamond(d, cx, cy, half, fill):
    d.polygon([(cx, cy - half), (cx + half, cy), (cx, cy + half), (cx - half, cy)], fill=fill)


def paste_center(img, im, cx, cy):
    img.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def text(d, xy, s, size, bold=True, anchor='la', fill=(255, 255, 255, 255)):
    d.text(xy, s, font=ImageFont.truetype(BOLD if bold else REG, size), fill=fill, anchor=anchor,
           stroke_width=0)


def bars_hud(img, ox, oy, k):
    """Bars style, 1 canvas unit = k px, (ox, oy) = guardian centre."""
    d = ImageDraw.Draw(img, 'RGBA')
    u = lambda x, y: (ox + x * k, oy - y * k)
    diamond(d, *u(0, 0), 70.7 * k, CELL)
    for (fx, fy), ic, t in (((78, 48), 'food_raspberry.png', '3m'), ((129, 0), 'food_carrot.png', '13s'), ((78, -48), None, '')):
        diamond(d, *u(fx, fy), 44 * k, CELL)
        cx, cy = u(fx, fy + 2)
        if ic:
            paste_center(img, icon(ic, int(28 * k)), cx, cy)
            text(d, u(fx + 12, fy - 13), t, int(14 * k), anchor='mm')
        else:
            paste_center(img, icon('default-food-icon.png', int(28 * k), 90), cx, cy)
    paste_center(img, icon('Eikthyr.png', int(98 * k)), *u(2, 12))
    text(d, u(0, -52), 'EIKTHYR', int(17 * k), anchor='mm')
    for y, col, val, ic, cut_bottom in ((17, RED, '120', 'HealthIcon.png', False), (-17, STAM, '100', 'StaminaIcon.png', True)):
        top, bot, left, right, plate = y + 13.5, y - 13.5, 158, 424, 34
        if cut_bottom:
            poly = [u(left + 27, top), u(right - plate, top), u(right - plate, bot), u(left, bot)]
        else:
            poly = [u(left, top), u(right - plate, top), u(right - plate, bot), u(left + 27, bot)]
        d.polygon(poly, fill=col)
        d.rectangle([u(right - plate, top), u(right, bot)], fill=(27, 27, 22, 240))
        text(d, u(right - plate / 2, y), val, int(15 * k), anchor='mm')
        paste_center(img, icon(ic, int(13 * k)), *u(right + 10, y))


def chevron_hud(img, ox, oy, k):
    """Chevron style from Figma, (ox, oy) = guardian centre."""
    d = ImageDraw.Draw(img, 'RGBA')
    u = lambda x, y: (ox + x * k, oy - y * k)
    c = (77, 0)
    diamond(d, *u(0, 0), 70.7 * k, CELL)
    cells = ((-1.28, 45.62, 'food_mushroom.png', '14m'), (46.72, 0.28, 'food_berries.png', '8m'), (-1.28, -46.38, 'food_stew.png', '19m'))
    for fx, fy, ic, t in cells:
        x, y = c[0] + fx, c[1] + fy
        diamond(d, *u(x, y), 42.4 * k, CELL)
        paste_center(img, icon(ic, int(33 * k)), *u(x, y))
        text(d, u(x + 10.2, y - 13.5), t, int(11 * k), bold=False, anchor='mm')
    shape = Image.open(os.path.join(ICONS, 'chevron_shape.png')).getchannel('A')
    outline = Image.open(os.path.join(ICONS, 'chevron_outline.png')).getchannel('A')
    size = (int(198.67 * k), int(205.33 * k))
    red = Image.new('RGBA', shape.size, RED)
    red.putalpha(shape)
    edge = Image.new('RGBA', shape.size, (0, 0, 0, 255))
    edge.putalpha(outline)
    red.alpha_composite(edge)
    red = red.resize(size, Image.LANCZOS)
    img.alpha_composite(red, tuple(int(v) for v in u(c[0] - 79.71, 117.38)))
    paste_center(img, icon('Eikthyr.png', int(98 * k)), *u(2, 12))
    text(d, u(0, -51.3), 'EIKTHYR', int(16 * k), anchor='mm')
    text(d, u(-2.7, 102), '120 HP', int(16 * k), anchor='lm')


def main():
    img = Image.new('RGBA', (W, H), (18, 17, 14, 255))
    glow = Image.new('L', (W, H), 0)
    ImageDraw.Draw(glow).ellipse([200, 150, 1400, 950], fill=110)
    glow = glow.filter(ImageFilter.GaussianBlur(220))
    img = Image.composite(Image.new('RGBA', (W, H), (74, 62, 40, 255)), img, glow)
    d = ImageDraw.Draw(img, 'RGBA')
    text(d, (W / 2, 120), 'HUD UPDATE', 92, anchor='mm')
    text(d, (W / 2, 200), 'Nordic HUD for Valheim  ·  two styles  ·  client-side', 30, bold=False, anchor='mm',
         fill=(225, 214, 190, 255))
    bars_hud(img, 170, 530, 1.45)
    chevron_hud(img, 1120, 540, 1.45)
    text(d, (470, 800), 'HudStyle = Bars', 26, bold=False, anchor='mm', fill=(200, 190, 170, 255))
    text(d, (1230, 800), 'HudStyle = Chevron', 26, bold=False, anchor='mm', fill=(200, 190, 170, 255))
    out = os.path.join(ROOT, 'Artwork')
    os.makedirs(out, exist_ok=True)
    img.convert('RGB').save(os.path.join(out, 'cover.png'))


if __name__ == '__main__':
    main()
