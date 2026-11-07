"""Stamina strip for the Chevron style ([Chevron] StaminaInChevron = true).

A thin band that runs parallel to the health chevron on its outer side:
pixels EDGE + GAP .. EDGE + GAP + WIDTH units from the chevron's centre line,
on its outer side and between its two ends. The canvas is the chevron canvas grown by MARGIN units on every side.

Writes ../Icons/chevron_stamina.png (white on transparent, used as a mask).
"""
import os

import numpy as np
from PIL import Image

ICONS = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Icons')
# Chevron bounds and centre line, units relative to the food-grid centre C
# (same numbers as HudState.ChevX0.. and ChevPath in HudUpdatePlugin.cs).
X0, X1, Y0, Y1 = -79.71, 118.96, -87.96, 117.38
PATH = [(-85.04, 86.71), (-25.04, 86.71), (-1.04, 108.04), (107.89, 0.31), (16.29, -95.96)]
GAP, WIDTH, MARGIN, EDGE = 3.0, 7.0, 16.0, 9.0


def main():
    shape = Image.open(os.path.join(ICONS, 'chevron_shape.png')).getchannel('A')
    ppu = shape.width / (X1 - X0)                   # pixels per unit
    m = int(round(MARGIN * ppu))
    a = np.zeros((shape.height + 2 * m, shape.width + 2 * m), np.float32)
    a[m:m + shape.height, m:m + shape.width] = np.asarray(shape, np.float32) / 255
    h, w = a.shape

    # Unit coordinates of every pixel centre.
    ys, xs = np.mgrid[0:h, 0:w]
    ux = (xs + .5 - m) / ppu + X0
    uy = Y1 - (ys + .5 - m) / ppu
    best = np.full(a.shape, np.inf)
    side = np.zeros(a.shape)
    along = np.zeros(a.shape)
    total = 0.0
    lengths = []
    for (ax, ay), (bx, by) in zip(PATH, PATH[1:]):
        lengths.append(np.hypot(bx - ax, by - ay))
    for i, ((ax, ay), (bx, by)) in enumerate(zip(PATH, PATH[1:])):
        L = lengths[i]
        dx, dy = (bx - ax) / L, (by - ay) / L
        t = (ux - ax) * dx + (uy - ay) * dy
        tc = np.clip(t, 0, L)
        d = np.hypot(ux - (ax + dx * tc), uy - (ay + dy * tc))
        closer = d < best
        best[closer] = d[closer]
        # right of the direction = inside of the chevron
        side[closer] = ((ux - ax) * dy - (uy - ay) * dx)[closer]
        along[closer] = (total + t)[closer]
        total += L
    # Distance from the centre line, not from the shape: the shape has small steps
    # and notches, the strip should run clean. The outer edge of the band is about
    # EDGE units from the line on every arm.
    dist = best
    ring = np.clip(np.minimum(dist - (EDGE + GAP), EDGE + GAP + WIDTH - dist) * ppu + .5, 0, 1)
    keep = (side < 0) & (along > 6) & (along < total - 12)   # the shape ends short of the line
    out = np.zeros((h, w, 4), np.uint8)
    out[..., :3] = 255
    out[..., 3] = (ring * keep * 255).round().astype(np.uint8)
    Image.fromarray(out, 'RGBA').save(os.path.join(ICONS, 'chevron_stamina.png'), optimize=True)
    print('chevron_stamina.png', (w, h), 'margin', MARGIN)


if __name__ == '__main__':
    main()
