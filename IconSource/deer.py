"""White deer head (Eikthyr) for the second HUD style."""
import io, os
import numpy as np
import cairosvg
from PIL import Image

KEEP = '''
<path d="M64 58 C80 58 92 64 94 76 C96 88 88 100 80 110 C76 116 72 122 64 122 C56 122 52 116 48 110 C40 100 32 88 34 76 C36 64 48 58 64 58 Z" stroke="none"/>
<path d="M40 70 C30 62 16 60 6 64 C12 76 26 84 40 82 Z" stroke="none"/>
<path d="M88 70 C98 62 112 60 122 64 C116 76 102 84 88 82 Z" stroke="none"/>
<path d="M50 62 C40 56 24 46 20 30 C17 20 20 10 26 4" fill="none" stroke-width="7"/><path d="M42 50 C36 40 34 30 36 18" fill="none" stroke-width="6"/><path d="M30 40 C26 34 26 28 28 22" fill="none" stroke-width="0"/>
<path d="M78 62 C88 56 104 46 108 30 C111 20 108 10 102 4" fill="none" stroke-width="7"/><path d="M86 50 C92 40 94 30 92 18" fill="none" stroke-width="6"/>
'''
CUT = '''
<path d="M44 84 C48 78 56 80 58 86 C52 90 46 90 44 84 Z" stroke="none"/>
<path d="M84 84 C80 78 72 80 70 86 C76 90 82 90 84 84 Z" stroke="none"/>
<path d="M54 104 C58 100 70 100 74 104 C74 110 68 114 64 114 C60 114 54 110 54 104 Z" stroke="none"/>
<path d="M40 70 C32 66 22 65 14 66" fill="none" stroke-width="3"/>
<path d="M88 70 C96 66 106 65 114 66" fill="none" stroke-width="3"/>
'''
# nostril highlights put back inside the dark nose
KEEP2 = '<circle cx="59" cy="106" r="2.2" stroke="none"/><circle cx="69" cy="106" r="2.2" stroke="none"/>'

def layer(body, color, size=512):
    doc = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="{size}" height="{size}">'
           f'<g fill="{color}" stroke="{color}" stroke-linejoin="round" stroke-linecap="round">{body}</g></svg>')
    png = cairosvg.svg2png(bytestring=doc.encode())
    return np.asarray(Image.open(io.BytesIO(png)).convert('RGBA'))[:, :, 3].astype(np.float32) / 255

def render(path):
    a = layer(KEEP, 'white') * (1 - layer(CUT, 'black'))
    a = np.maximum(a, layer(KEEP2, 'white'))
    img = np.zeros((512, 512, 4), np.uint8)
    img[..., :3] = 255
    img[..., 3] = (a * 255).round().astype(np.uint8)
    Image.fromarray(img, 'RGBA').resize((256, 256), Image.LANCZOS).save(path)

if __name__ == '__main__':
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'png2')
    os.makedirs(out, exist_ok=True)
    render(os.path.join(out, 'EikthyrDeer.png'))
