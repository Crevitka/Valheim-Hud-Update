import cairosvg, os, math
# Writes ../Icons/food_<glyph>.png (128x128, #F2EDE3 with see-through details).
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Icons')
os.makedirs(OUT, exist_ok=True)
FILL = '#F2EDE3'

def svg(keep, cut=''):
    return (keep, cut)

def _old_svg(keep, cut=''):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
<defs><mask id="m" maskUnits="userSpaceOnUse" x="0" y="0" width="128" height="128">
<g fill="white" stroke="white" stroke-linejoin="round" stroke-linecap="round">{keep}</g>
<g fill="black" stroke="black" stroke-linejoin="round" stroke-linecap="round">{cut}</g>
</mask></defs><rect width="128" height="128" fill="{FILL}" mask="url(#m)"/></svg>'''

ICONS = {}

def reg(name):
    def d(f):
        ICONS[name] = f
        return f
    return d

@reg('raspberry')
def raspberry():
    keep = '<path d="M64 36 C88 34 104 48 104 68 C104 92 84 116 64 118 C44 116 24 92 24 68 C24 48 40 34 64 36 Z" stroke="none"/>'
    keep += '<path d="M34 40 L52 36 L46 22 L62 32 L72 18 L76 34 L96 30 L86 42 Z" stroke-width="3"/>'
    keep += '<path d="M62 26 C62 18 66 12 72 8" fill="none" stroke-width="6"/>'
    cut = '<path d="M30 46 C50 52 76 52 100 44" fill="none" stroke-width="4"/>'
    import math
    rows = [(62, [38, 54, 70, 86]), (76, [32, 46, 62, 78, 94]), (90, [40, 56, 72, 88]), (104, [50, 64, 78])]
    for y, xs in rows:
        for x in xs:
            cut += f'<path d="M{x-5} {y+1} C{x-4} {y-5} {x+4} {y-5} {x+5} {y+1}" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('berries')
def berries():
    def crown(cx, cy, r):
        pts = []
        for i in range(10):
            a = -math.pi / 2 + i * math.pi / 5
            rr = r if i % 2 == 0 else r * .5
            pts.append(f'{cx + rr*math.cos(a):.1f},{cy + rr*math.sin(a):.1f}')
        return f'<polygon points="{" ".join(pts)}" stroke="none"/>'
    ball = lambda x, y, r: f'<circle cx="{x}" cy="{y}" r="{r}" stroke="none"/>'
    ring = lambda x, y, r: f'<circle cx="{x}" cy="{y}" r="{r+2}" fill="none" stroke-width="3.2"/>'
    return [
        (ball(65, 46, 23), crown(65, 34, 7)),
        (ball(42, 84, 24), ring(42, 84, 24)),
        (ball(42, 84, 24), crown(38, 72, 7) + ring(86, 84, 24)),
        (ball(86, 84, 24), crown(90, 72, 7)),
    ]

@reg('mushroom')
def mushroom():
    keep = '<path d="M14 64 C14 30 40 14 64 14 C88 14 114 30 114 64 C100 70 28 70 14 64 Z" stroke="none"/>'
    keep += '<path d="M48 72 C46 88 44 104 42 114 L86 114 C84 104 82 88 80 72 Z" stroke="none"/>'
    cut = '<circle cx="44" cy="38" r="7" stroke="none"/><circle cx="78" cy="30" r="6" stroke="none"/>'
    cut += '<circle cx="92" cy="50" r="5" stroke="none"/><circle cx="62" cy="52" r="5" stroke="none"/><circle cx="30" cy="56" r="3.5" stroke="none"/>'
    cut += '<path d="M56 86 C56 96 55 104 54 110" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('carrot')
def carrot():
    keep = '<path d="M16 116 C24 96 44 62 66 44 C74 38 86 40 90 48 C96 58 90 66 84 72 C66 90 36 108 16 116 Z" stroke="none"/>'
    keep += '<path d="M82 44 C84 30 80 18 70 10 C84 12 92 22 92 36 C98 24 108 18 120 20 C112 26 104 36 98 44 C108 42 116 46 120 54 C108 54 98 52 90 50 Z" stroke="none"/>'
    cut = '<path d="M60 58 C64 62 68 66 70 72" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M44 78 C48 82 52 86 54 90" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M72 44 C80 50 86 54 94 56" fill="none" stroke-width="3.4"/>'
    return svg(keep, cut)

@reg('turnip')
def turnip():
    keep = '<path d="M64 42 C92 42 108 60 108 78 C108 98 88 108 72 110 C68 114 66 120 64 124 C62 120 60 114 56 110 C40 108 20 98 20 78 C20 60 36 42 64 42 Z" stroke="none"/>'
    keep += '<path d="M60 44 C52 30 40 22 26 20 C34 32 44 40 58 46 Z" stroke="none"/>'
    keep += '<path d="M62 44 C60 28 64 14 72 6 C78 18 74 32 66 46 Z" stroke="none"/>'
    keep += '<path d="M66 46 C76 32 90 26 104 26 C96 38 82 44 68 48 Z" stroke="none"/>'
    cut = '<path d="M20 70 C40 62 88 62 108 70" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M38 88 C42 96 50 102 58 104" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M56 48 L64 42 L72 48" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('onion')
def onion():
    keep = '<path d="M64 30 C74 44 104 58 104 84 C104 104 86 114 64 114 C42 114 24 104 24 84 C24 58 54 44 64 30 Z" stroke="none"/>'
    keep += '<path d="M58 36 C56 22 58 12 64 4 C70 12 72 22 70 36 Z" stroke="none"/>'
    keep += '<path d="M52 114 L46 124 M64 114 L64 125 M76 114 L82 124" fill="none" stroke-width="3.4"/>'
    cut = '<path d="M64 42 C50 58 42 76 46 110" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M64 42 C78 58 86 76 82 110" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M56 38 L72 38" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('honey')
def honey():
    keep = '<path d="M34 44 C22 56 20 80 26 96 C32 112 96 112 102 96 C108 80 106 56 94 44 Z" stroke="none"/>'
    keep += '<rect x="30" y="30" width="68" height="12" rx="5" stroke="none"/>'
    keep += '<path d="M40 30 C38 20 50 14 64 14 C78 14 90 20 88 30 Z" stroke="none"/>'
    cut = '<path d="M28 58 C50 64 78 64 100 58 L100 66 C96 66 94 70 94 76 C94 82 88 82 88 76 C88 70 84 68 80 68 C60 72 44 70 28 66 Z" stroke="none"/>'
    cut += '<path d="M28 43 L100 43" fill="none" stroke-width="3"/>'
    cut += '<path d="M40 88 C46 96 56 100 66 100" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('steak')
def steak():
    keep = '<path d="M22 58 C20 36 44 22 70 22 C96 22 114 38 110 60 C108 76 96 84 88 96 C80 110 60 112 44 104 C28 96 24 78 22 58 Z" stroke="none"/>'
    cut = '<circle cx="80" cy="52" r="12" fill="none" stroke-width="3.4"/><circle cx="80" cy="52" r="5" stroke="none"/>'
    cut += '<path d="M34 54 C40 44 52 38 62 38" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M40 78 C48 86 60 88 70 84" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M22 58 C22 70 26 82 34 92" fill="none" stroke-width="0"/>'
    return svg(keep, cut)

@reg('drumstick')
def drumstick():
    keep = '<path d="M72 18 C96 14 116 32 110 56 C106 72 88 80 74 78 L58 92 C54 96 50 98 46 96 Z" stroke="none"/>'
    keep += '<path d="M72 18 C54 22 42 40 46 58 L58 92 L74 78 Z" stroke="none"/>'
    keep += '<path d="M58 88 L34 108" fill="none" stroke-width="10"/>'
    keep += '<circle cx="24" cy="108" r="8" stroke="none"/><circle cx="34" cy="118" r="8" stroke="none"/>'
    cut = '<path d="M60 84 L76 72" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M66 30 C58 36 54 44 54 54" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M90 30 C98 36 100 44 98 52" fill="none" stroke-width="3.4"/>'
    return svg(keep, cut)

@reg('fish')
def fish():
    keep = '<path d="M10 64 C28 36 62 30 88 50 L112 32 C108 46 108 82 112 96 L88 78 C62 98 28 92 10 64 Z" stroke="none"/>'
    cut = '<circle cx="30" cy="58" r="4.5" stroke="none"/>'
    cut += '<path d="M44 44 C50 56 50 72 44 84" fill="none" stroke-width="3.4"/>'
    for x in (58, 70):
        cut += f'<path d="M{x} 46 L{x+10} 58 M{x} 62 L{x+10} 74" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('egg')
def egg():
    keep = '<path d="M30 34 C44 18 70 22 84 30 C104 30 118 48 110 66 C118 84 102 104 82 102 C68 116 42 112 34 98 C16 94 10 72 20 60 C12 48 18 36 30 34 Z" stroke="none"/>'
    cut = '<circle cx="62" cy="64" r="22" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M52 56 C54 50 60 48 64 48" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('jerky')
def jerky():
    keep = '<path d="M18 94 C30 70 58 38 86 18 C92 14 100 18 98 24 C88 48 56 88 34 110 C28 116 16 104 18 94 Z" stroke="none"/>'
    keep += '<path d="M50 112 C66 90 90 62 108 46 C114 42 120 48 116 54 C104 76 80 104 64 118 C58 122 48 118 50 112 Z" stroke="none"/>'
    cut = '<path d="M36 82 L50 88 M50 64 L64 72 M66 46 L78 52" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M70 96 L82 100 M86 76 L98 80" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('skewer')
def skewer():
    keep = '<path d="M10 118 L118 10" fill="none" stroke-width="6"/>'
    for cx, cy in ((42, 86), (64, 64), (86, 42)):
        keep += f'<rect x="{cx-14}" y="{cy-14}" width="28" height="28" rx="8" transform="rotate(20 {cx} {cy})" stroke="none"/>'
    cut = ''
    for cx, cy in ((42, 86), (64, 64), (86, 42)):
        cut += f'<rect x="{cx-15.5}" y="{cy-15.5}" width="31" height="31" rx="9" transform="rotate(20 {cx} {cy})" fill="none" stroke-width="3"/>'
    for cx, cy in ((42, 86), (64, 64), (86, 42)):
        cut += f'<path d="M{cx-6} {cy-2} C{cx-2} {cy-6} {cx+4} {cy-6} {cx+7} {cy-3}" fill="none" stroke-width="3"/>'
    keep2 = ''.join(f'<rect x="{cx-14}" y="{cy-14}" width="28" height="28" rx="8" transform="rotate(20 {cx} {cy})" stroke="none"/>' for cx, cy in ((42, 86), (64, 64), (86, 42)))
    return svg(keep, cut)

@reg('stew')
def stew():
    keep = '<path d="M12 62 L116 62 C114 90 94 106 64 106 C34 106 14 90 12 62 Z" stroke="none"/>'
    keep += '<rect x="44" y="106" width="40" height="10" rx="4" stroke="none"/>'
    for x in (42, 64, 86):
        keep += f'<path d="M{x} 50 C{x-8} 42 {x+8} 34 {x} 26 C{x-6} 20 {x+2} 14 {x} 10" fill="none" stroke-width="5"/>'
    cut = '<path d="M12 70 L116 70" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M30 82 C38 92 50 96 60 96" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('jam')
def jam():
    keep = '<path d="M30 42 L98 42 L102 108 C102 116 96 118 90 118 L38 118 C32 118 26 116 26 108 Z" stroke="none"/>'
    keep += '<path d="M24 34 C40 22 88 22 104 34 L100 44 L28 44 Z" stroke="none"/>'
    keep += '<path d="M40 24 C46 12 58 16 64 22 C70 16 82 12 88 24" fill="none" stroke-width="5"/>'
    cut = '<path d="M26 44 L102 44" fill="none" stroke-width="4"/>'
    cut += '<circle cx="54" cy="80" r="10" fill="none" stroke-width="3.2"/><circle cx="74" cy="80" r="10" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M64 70 C62 62 66 58 70 56" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('drink')
def drink():
    keep = '<path d="M30 36 L98 36 L90 112 C90 118 86 120 80 120 L48 120 C42 120 38 118 38 112 Z" stroke="none"/>'
    keep += '<path d="M72 36 L86 6" fill="none" stroke-width="6"/><path d="M84 8 L100 10" fill="none" stroke-width="6"/>'
    keep += '<path d="M26 36 C26 24 46 22 54 26 C60 16 80 16 84 26 C96 22 104 30 102 36 Z" stroke="none"/>'
    cut = '<path d="M26 38 L102 38" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M34 60 C52 66 76 54 94 60" fill="none" stroke-width="3.4"/>'
    cut += '<circle cx="54" cy="84" r="4" stroke="none"/><circle cx="70" cy="98" r="3" stroke="none"/><circle cx="74" cy="76" r="2.6" stroke="none"/>'
    return svg(keep, cut)

@reg('bread')
def bread():
    keep = '<path d="M10 78 C10 46 38 30 64 30 C90 30 118 46 118 78 C118 94 106 100 96 100 L32 100 C22 100 10 94 10 78 Z" stroke="none"/>'
    cut = ''.join(f'<path d="M{x} 50 C{x+8} 58 {x+12} 66 {x+14} 74" fill="none" stroke-width="3.6"/>' for x in (30, 52, 74))
    cut += '<path d="M14 88 C40 94 88 94 114 88" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('pie')
def pie():
    keep = '<path d="M14 70 C14 44 38 30 64 30 C90 30 114 44 114 70 Z" stroke="none"/>'
    keep += '<path d="M8 72 L120 72 L112 100 C110 106 106 108 100 108 L28 108 C22 108 18 106 16 100 Z" stroke="none"/>'
    cut = '<path d="M8 74 L120 74" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M44 52 L52 60 M64 44 L64 56 M84 52 L76 60" fill="none" stroke-width="3.6"/>'
    cut += ''.join(f'<path d="M{x} 80 L{x} 100" fill="none" stroke-width="3.2"/>' for x in (30, 48, 64, 80, 98))
    return svg(keep, cut)

@reg('salad')
def salad():
    keep = '<path d="M10 66 L118 66 C116 92 94 108 64 108 C34 108 12 92 10 66 Z" stroke="none"/>'
    keep += '<path d="M24 64 C18 44 30 28 46 30 C52 16 76 14 82 28 C98 24 112 40 104 64 Z" stroke="none"/>'
    cut = '<path d="M10 70 L118 70" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M46 62 C44 50 46 40 50 34 M66 62 C66 48 68 36 72 30 M86 62 C88 50 90 42 96 36" fill="none" stroke-width="3.2"/>'
    cut += '<circle cx="36" cy="52" r="4" stroke="none"/><circle cx="92" cy="54" r="3.5" stroke="none"/>'
    return svg(keep, cut)

@reg('icecream')
def icecream():
    keep = '<path d="M36 60 L92 60 L64 124 Z" stroke="none"/>'
    keep += '<circle cx="64" cy="42" r="30" stroke="none"/>'
    cut = '<path d="M30 60 L98 60" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M46 74 L70 104 M58 68 L80 84 M76 70 L52 96" fill="none" stroke-width="3"/>'
    cut += '<ellipse cx="64" cy="40" rx="14" ry="9" fill="none" stroke-width="3.2"/><circle cx="64" cy="40" r="5" stroke="none"/>'
    return svg(keep, cut)

@reg('fern')
def fern():
    keep = '<path d="M60 124 C56 96 58 72 70 56 C82 40 98 40 104 30 C110 18 100 6 86 8 C70 10 64 26 76 32 C84 36 90 28 86 22" fill="none" stroke-width="10"/>'
    keep += '<path d="M60 96 C44 90 30 90 18 96 C30 102 46 102 60 96 Z" stroke="none"/>'
    keep += '<path d="M64 74 C52 62 40 58 26 60 C36 72 50 76 64 74 Z" stroke="none"/>'
    keep += '<path d="M62 86 C76 78 90 78 102 84 C90 92 76 92 62 86 Z" stroke="none"/>'
    cut = '<path d="M22 96 L56 96 M30 62 L60 72 M98 84 L66 86" fill="none" stroke-width="2.6"/>'
    return svg(keep, cut)

@reg('aspic')
def aspic():
    keep = '<path d="M22 100 C22 60 36 26 64 26 C92 26 106 60 106 100 Z" stroke="none"/>'
    keep += '<path d="M10 104 L118 104 C116 114 108 118 98 118 L30 118 C20 118 12 114 10 104 Z" stroke="none"/>'
    cut = '<path d="M46 34 C40 56 38 78 40 100 M64 26 L64 100 M82 34 C88 56 90 78 88 100" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M10 102 L118 102" fill="none" stroke-width="3.6"/>'
    return svg(keep, cut)

@reg('omelette')
def omelette():
    keep = '<path d="M10 80 C10 50 36 28 64 28 C92 28 118 50 118 80 C118 92 108 96 96 96 L32 96 C20 96 10 92 10 80 Z" stroke="none"/>'
    cut = '<path d="M14 84 C44 72 84 72 114 84" fill="none" stroke-width="3.6"/>'
    cut += '<circle cx="44" cy="52" r="6" fill="none" stroke-width="3"/><circle cx="78" cy="46" r="5" stroke="none"/><circle cx="92" cy="62" r="4" stroke="none"/><circle cx="58" cy="64" r="3.5" stroke="none"/>'
    return svg(keep, cut)

@reg('feast')
def feast():
    # Roast bird on a serving platter, both legs pointing up-right.
    keep = '<ellipse cx="60" cy="104" rx="56" ry="14" stroke="none"/>'
    keep += '<path d="M14 92 C10 66 32 46 60 46 C84 46 100 60 100 80 C100 90 94 96 84 96 L26 96 C18 96 15 95 14 92 Z" stroke="none"/>'
    for (x1, y1, x2, y2) in ((78, 62, 102, 34), (90, 76, 116, 52)):
        keep += f'<path d="M{x1} {y1} L{x2} {y2}" fill="none" stroke-width="16"/>'
        keep += f'<path d="M{x2} {y2} L{x2+8} {y2-9}" fill="none" stroke-width="5"/>'
        keep += f'<circle cx="{x2+7}" cy="{y2-14}" r="5.5" stroke="none"/><circle cx="{x2+13}" cy="{y2-8}" r="5.5" stroke="none"/>'
    cut = '<path d="M6 100 C28 94 92 94 114 100" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M28 70 C36 60 48 56 60 56" fill="none" stroke-width="3.4"/>'
    cut += '<path d="M74 72 L94 50 M84 88 L106 66" fill="none" stroke-width="3.2"/>'
    cut += '<circle cx="16" cy="110" r="3" stroke="none"/><circle cx="104" cy="110" r="3" stroke="none"/>'
    return svg(keep, cut)

@reg('potato')
def potato():
    keep = '<path d="M12 70 C8 50 26 34 48 30 C64 27 72 34 88 32 C106 30 120 44 118 64 C116 84 100 96 78 98 C60 100 48 104 32 98 C20 94 14 84 12 70 Z" stroke="none"/>'
    cut = ''.join(f'<path d="M{x} {y} C{x+3} {y-2} {x+6} {y-1} {x+7} {y+2}" fill="none" stroke-width="3.4"/>'
                  for x, y in ((34, 52), (70, 44), (96, 62), (50, 80), (82, 84)))
    cut += '<path d="M22 62 C26 50 36 42 48 38" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('pancake')
def pancake():
    # Stack of three pancakes with a knob of butter and a drip.
    keep = ''.join(f'<rect x="{x}" y="{y}" width="{w}" height="18" rx="9" stroke="none"/>'
                   for x, y, w in ((14, 92, 100), (18, 72, 92), (22, 52, 84)))
    keep += '<path d="M50 38 L78 38 L80 52 L48 52 Z" stroke="none"/>'
    keep += '<path d="M96 60 C100 66 100 74 98 80 C96 84 92 84 92 80 C92 74 94 68 96 60 Z" stroke="none"/>'
    cut = '<path d="M16 91 L112 91 M20 71 L108 71" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M24 52 L104 52" fill="none" stroke-width="3.2"/>'
    cut += '<path d="M34 82 C44 86 56 86 66 84 M40 102 C54 106 70 106 84 102" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('cupcake')
def cupcake():
    keep = '<path d="M28 66 L100 66 L90 118 L38 118 Z" stroke="none"/>'
    keep += '<path d="M20 66 C16 50 30 40 42 42 C44 26 62 18 76 26 C90 22 106 34 104 48 C112 52 112 64 108 66 Z" stroke="none"/>'
    keep += '<circle cx="66" cy="18" r="9" stroke="none"/><path d="M68 10 C70 4 76 2 80 2" fill="none" stroke-width="3.4"/>'
    cut = '<path d="M22 68 L106 68" fill="none" stroke-width="3.8"/>'
    cut += ''.join(f'<path d="M{x1} 74 L{x2} 114" fill="none" stroke-width="3.2"/>' for x1, x2 in ((44, 48), (58, 60), (72, 70), (86, 82)))
    cut += '<path d="M32 56 C48 50 64 54 76 46 C86 40 96 44 100 52" fill="none" stroke-width="3.2"/>'
    return svg(keep, cut)

@reg('kale')
def kale():
    # Curly kale leaf with a stem.
    keep = '<path d="M60 120 C58 104 58 94 60 86" fill="none" stroke-width="9"/>'
    keep += ('<path d="M60 90 C40 92 22 80 20 64 C12 60 12 48 20 44 C16 34 24 24 34 26 C36 14 50 8 60 14 '
             'C70 6 86 10 88 22 C100 20 110 30 104 40 C114 44 114 58 104 62 C104 80 84 92 60 90 Z" stroke="none"/>')
    cut = '<path d="M60 88 C62 66 64 42 62 18" fill="none" stroke-width="3.6"/>'
    cut += '<path d="M62 70 C50 64 38 58 28 48 M62 54 C52 46 44 40 38 30 M62 70 C74 62 88 56 98 48 M63 52 C72 44 80 38 84 28" fill="none" stroke-width="3"/>'
    return svg(keep, cut)

@reg('grain')
def grain():
    # Oat stalk: stem with kernels on both sides.
    keep = '<path d="M64 124 C64 90 64 60 66 22" fill="none" stroke-width="6"/>'
    for y in (32, 52, 72, 92):
        keep += f'<ellipse cx="0" cy="0" rx="9" ry="17" transform="translate(48 {y}) rotate(-35)" stroke="none"/>'
        keep += f'<ellipse cx="0" cy="0" rx="9" ry="17" transform="translate(82 {y - 8}) rotate(35)" stroke="none"/>'
    keep += '<ellipse cx="0" cy="0" rx="8" ry="15" transform="translate(66 14)" stroke="none"/>'
    cut = ''.join(f'<path d="M{x} {y-8} L{x} {y+8}" transform="rotate({r} {x} {y})" fill="none" stroke-width="2.6"/>'
                  for y in (32, 52, 72, 92) for x, r, dy in ((48, -35, 0), (82, 35, -8)) for y in [y + dy])
    return svg(keep, cut)

@reg('worm')
def worm():
    # Glow worm: segmented curl with a few sparks.
    keep = '<path d="M24 104 C24 84 46 80 62 88 C80 98 104 92 104 70 C104 50 84 42 70 52" fill="none" stroke-width="22"/>'
    keep += '<circle cx="66" cy="52" r="13" stroke="none"/>'
    star = lambda x, y, r: (f'<path d="M{x} {y-r} L{x+r*.28:.1f} {y-r*.28:.1f} L{x+r} {y} L{x+r*.28:.1f} {y+r*.28:.1f} '
                            f'L{x} {y+r} L{x-r*.28:.1f} {y+r*.28:.1f} L{x-r} {y} L{x-r*.28:.1f} {y-r*.28:.1f} Z" stroke="none"/>')
    keep += star(30, 34, 12) + star(52, 18, 7) + star(106, 20, 9)
    cut = ''.join(f'<path d="{d}" fill="none" stroke-width="3.2"/>' for d in (
        'M30 92 L20 110', 'M46 82 L42 102', 'M64 86 L60 104', 'M82 90 L82 110', 'M96 80 L112 92', 'M94 60 L114 62', 'M84 48 L92 40'))
    cut += '<circle cx="62" cy="48" r="3.4" stroke="none"/>'
    return svg(keep, cut)

if __name__ == '__main__':
    import io
    import numpy as np
    from PIL import Image
    def layer(body, color):
        doc = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="512" height="512">'
               f'<g fill="{color}" stroke="{color}" stroke-linejoin="round" stroke-linecap="round">{body}</g></svg>')
        png = cairosvg.svg2png(bytestring=doc.encode())
        return np.asarray(Image.open(io.BytesIO(png)).convert('RGBA'))[:, :, 3].astype(np.float32) / 255
    rgb = tuple(int(FILL[i:i+2], 16) for i in (1, 3, 5))
    for name, f in ICONS.items():
        ops = f()
        if isinstance(ops, tuple): ops = [ops]
        a = np.zeros((512, 512), np.float32)
        for keep, cut in ops:
            if keep: a = np.maximum(a, layer(keep, 'white'))
            if cut: a = a * (1 - layer(cut, 'black'))
        img = np.zeros((512, 512, 4), np.uint8)
        img[..., :3] = rgb
        img[..., 3] = (a * 255).round().astype(np.uint8)
        Image.fromarray(img, 'RGBA').resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, 'food_' + name + '.png'))
    print(len(ICONS), 'icons')
