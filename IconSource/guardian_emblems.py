"""Knotwork emblems for the guardian powers that have no hand-drawn emblem yet.

They show a symbol of the power, not the boss itself:
  GP_Moder   - wind (a triskele of wind spirals)
  GP_Yagluth - fire (interlaced flames)
  GP_Queen   - eitr (a drop with interlaced curls)
  GP_Fader   - ash sun (a knotted ring with rays)

Ribbons are drawn in order; every ribbon gets a dark rim that cuts the ribbons
below it, which gives the over/under look of the hand-drawn emblems.
Writes ../Icons/GP_<Power>.png (400 px high, white on transparent)."""
import io
import math
import os

import cairosvg
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Icons')
S = 400          # canvas, px
RIBBON = 19      # ribbon width
RIM = 7          # gap cut around each ribbon


def path_d(points, closed=False):
    d = 'M' + ' L'.join(f'{x:.1f} {y:.1f}' for x, y in points)
    return d + (' Z' if closed else '')


def render(strokes):
    """strokes: list of (d, width, kind) with kind 'keep' or 'cut', painted in order."""
    def layer(d, width, fill=False):
        attrs = f'fill="white" stroke="none"' if fill else f'fill="none" stroke="white" stroke-width="{width}"'
        doc = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {S} {S}" width="{S*2}" height="{S*2}">'
               f'<path d="{d}" {attrs} stroke-linecap="round" stroke-linejoin="round"/></svg>')
        png = cairosvg.svg2png(bytestring=doc.encode())
        return np.asarray(Image.open(io.BytesIO(png)).convert('RGBA'))[:, :, 3].astype(np.float32) / 255
    a = np.zeros((S * 2, S * 2), np.float32)
    for d, width, kind in strokes:
        if kind == 'ribbon':
            a = a * (1 - layer(d, width + 2 * RIM))   # rim cuts what is below
            a = np.maximum(a, layer(d, width))
        elif kind == 'fill':
            a = np.maximum(a, layer(d, 0, fill=True))
        elif kind == 'hole':
            a = a * (1 - layer(d, 0, fill=True))
    img = np.zeros((S * 2, S * 2, 4), np.uint8)
    img[..., :3] = 255
    img[..., 3] = (a * 255).round().astype(np.uint8)
    im = Image.fromarray(img, 'RGBA').resize((S, S), Image.LANCZOS)
    return im.crop(im.getbbox())


def rot(points, angle, c=(200, 200)):
    ca, sa = math.cos(angle), math.sin(angle)
    return [(c[0] + (x - c[0]) * ca - (y - c[1]) * sa, c[1] + (x - c[0]) * sa + (y - c[1]) * ca) for x, y in points]


def spiral(cx, cy, r0, r1, a0, turns, n=140):
    return [(cx + (r0 + (r1 - r0) * t) * math.cos(a0 + turns * 2 * math.pi * t),
             cy + (r0 + (r1 - r0) * t) * math.sin(a0 + turns * 2 * math.pi * t)) for t in (i / n for i in range(n + 1))]


def moder():
    # Triskele of wind spirals: each arm sweeps out from the centre and curls back in.
    strokes = []
    for k in range(3):
        a = k * 2 * math.pi / 3
        arm = spiral(200, 200, 18, 150, a, .42)
        tip = arm[-1]
        ang = a + .42 * 2 * math.pi
        curl = spiral(tip[0] - 46 * math.cos(ang), tip[1] - 46 * math.sin(ang), 46, 10, ang, -1.1)
        strokes.append((path_d(arm + curl), RIBBON, 'ribbon'))
    for k in range(3):   # second pass over the centre so the arms interlace
        a = k * 2 * math.pi / 3
        strokes.append((path_d(spiral(200, 200, 18, 70, a, .2)), RIBBON, 'ribbon'))
    strokes.append(('M200 176 A24 24 0 1 1 199.9 176 Z', RIBBON - 4, 'ribbon'))
    return render(strokes)


FLAME = ('M0 -150 C30 -95 75 -50 70 20 C66 75 36 110 0 116 C-36 110 -66 75 -70 20 '
         'C-74 -30 -46 -60 -36 -96 C-24 -60 -14 -42 0 -36 C-12 -76 -14 -116 0 -150 Z')


def flame(cx, cy, scale, angle=0.0):
    """The flame outline above, moved, scaled and tilted (degrees)."""
    return f'<g transform="translate({cx} {cy}) rotate({angle}) scale({scale})">{FLAME}</g>'


def yagluth():
    # Three interlaced flames on a knotted base.
    def tongue(cx, cy, k, angle):
        import re
        nums = [float(v) for v in re.findall(r'-?\d+(?:\.\d+)?', FLAME)]
        pts = list(zip(nums[0::2], nums[1::2]))
        ca, sa = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        pts = [(cx + k * (x * ca - y * sa), cy + k * (x * sa + y * ca)) for x, y in pts]
        it = iter(pts)
        d = 'M%.1f %.1f' % next(it)
        rest = list(it)
        for i in range(0, len(rest) - 2, 3):
            d += ' C%.1f %.1f %.1f %.1f %.1f %.1f' % (*rest[i], *rest[i + 1], *rest[i + 2])
        return d + ' Z'
    strokes = [
        (tongue(118, 230, .62, -24), RIBBON - 2, 'ribbon'),
        (tongue(282, 230, .62, 24), RIBBON - 2, 'ribbon'),
        ('M60 318 C60 378 340 378 340 318 C340 282 60 282 60 318 Z', RIBBON, 'ribbon'),
        (tongue(200, 196, 1.0, 0), RIBBON, 'ribbon'),
        ('M100 300 C150 362 250 262 300 330 C250 382 150 282 100 330 Z', RIBBON - 4, 'ribbon'),
        (tongue(200, 252, .42, 0), 0, 'fill'),
    ]
    return render(strokes)


def drop(cx, top, w, bottom):
    pts = []
    for i in range(61):
        t = i / 60 * 2 * math.pi
        r = 1 - .55 * (1 + math.sin(t)) / 2          # narrow at the top
        pts.append((cx + w * math.cos(t) * r, (top + bottom) / 2 + (bottom - top) / 2 * -math.sin(t) * -1))
    return pts


def queen():
    strokes = []
    outer = 'M200 30 C240 110 320 170 320 250 C320 320 266 372 200 372 C134 372 80 320 80 250 C80 170 160 110 200 30 Z'
    inner = 'M200 140 C222 190 262 220 262 266 C262 304 234 330 200 330 C166 330 138 304 138 266 C138 220 178 190 200 140 Z'
    strokes.append((outer, RIBBON, 'ribbon'))
    # Side curls weave through the outer drop.
    strokes.append((path_d(spiral(118, 300, 44, 10, math.pi * .2, -1.2)), RIBBON - 2, 'ribbon'))
    strokes.append((path_d(spiral(282, 300, 44, 10, math.pi * .8, 1.2)), RIBBON - 2, 'ribbon'))
    strokes.append(('M116 214 C150 150 250 150 284 214', RIBBON - 2, 'ribbon'))
    strokes.append((inner, RIBBON, 'ribbon'))
    strokes.append(('M200 228 C214 252 226 262 226 280 C226 296 214 306 200 306 C186 306 174 296 174 280 C174 262 186 252 200 228 Z', 0, 'fill'))
    return render(strokes)


def fader():
    strokes = []
    for k in range(12):   # rays first, so the ring passes over them
        a = k * math.pi / 6
        r0, r1 = 150, 192 if k % 2 == 0 else 176
        strokes.append((path_d([(200 + r0 * math.cos(a), 200 + r0 * math.sin(a)), (200 + r1 * math.cos(a), 200 + r1 * math.sin(a))]),
                        RIBBON - (0 if k % 2 == 0 else 4), 'ribbon'))
    strokes.append(('M200 70 A130 130 0 1 1 199.9 70 Z', RIBBON, 'ribbon'))
    # Four loops interlaced with the ring (quatrefoil knot).
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        cx, cy = 200 + 78 * math.cos(a), 200 + 78 * math.sin(a)
        strokes.append((f'M{cx + 58:.1f} {cy:.1f} A58 58 0 1 1 {cx + 57.9:.1f} {cy - .5:.1f} Z', RIBBON - 2, 'ribbon'))
    strokes.append(('M200 70 A130 130 0 0 1 330 200', RIBBON, 'ribbon'))     # ring back over the loops
    strokes.append(('M200 330 A130 130 0 0 1 70 200', RIBBON, 'ribbon'))
    strokes.append(('M200 172 A28 28 0 1 1 199.9 172 Z', 0, 'fill'))
    return render(strokes)


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for name, f in (('GP_Moder', moder), ('GP_Yagluth', yagluth), ('GP_Queen', queen), ('GP_Fader', fader)):
        im = f()
        im.thumbnail((400, 372), Image.LANCZOS)
        im.save(os.path.join(OUT, name + '.png'))
        print(name, im.size)
