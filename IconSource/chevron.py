"""Rasterise the Figma "Subtract" layer (HP chevron) into the two textures the
mod embeds: Icons/chevron_shape.png (fill mask) and Icons/chevron_outline.png
(1 px inside stroke). Scale: 1 Figma px = 4/3 HUD unit, 4 texture px per unit.
The path comes from figma_subtract_path.txt (node 2097:1256, 149 x 154 px)."""
import io, os
import numpy as np
import cairosvg
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ICONS = os.path.join(os.path.dirname(HERE), 'Icons')
K, PPU, SS = 4 / 3, 4, 3
S = K * PPU

def main():
    d = open(os.path.join(HERE, 'figma_subtract_path.txt')).read().strip()
    w, h = round(149 * S), round(154 * S)
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w*SS}" height="{h*SS}" viewBox="0 0 149 154">'
           f'<path d="{d}" fill="white"/></svg>')
    a = np.asarray(Image.open(io.BytesIO(cairosvg.svg2png(bytestring=svg.encode()))).convert('RGBA'))[..., 3]
    shape = Image.fromarray(a)
    k = int(round(S * SS)) | 1                       # 1 Figma px, inside
    eroded = shape.filter(ImageFilter.MinFilter(2 * k - 1))
    outline = Image.fromarray((np.asarray(shape).astype(int) - np.asarray(eroded).astype(int)).clip(0, 255).astype('uint8'))
    for name, mask in (('chevron_shape', shape), ('chevron_outline', outline)):
        img = Image.new('RGBA', (w, h), (255, 255, 255, 0))
        img.putalpha(mask.resize((w, h), Image.LANCZOS))
        img.save(os.path.join(ICONS, name + '.png'))

if __name__ == '__main__':
    main()
