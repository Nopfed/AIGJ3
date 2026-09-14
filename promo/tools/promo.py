import math, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
from sprites import load

SPR = load()
W, H = 320, 144
SCALE = 6
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

def C(r, g, b, a=1.0): return (r, g, b, int(round(a * 255)))
PAL = {
    'K': C(26,20,35), 'W': C(247,243,232), 'w': C(230,216,189), 'g': C(184,180,174), 'G': C(110,106,115),
    'm': C(142,140,133), 'M': C(90,89,85), 'r': C(217,65,47), 'R': C(143,43,30), 'o': C(232,118,43),
    'O': C(176,79,26), 'y': C(242,201,76), 'Y': C(201,148,42), 'l': C(139,195,74), 'L': C(79,138,58),
    'D': C(45,90,42), 'b': C(198,154,106), 'B': C(138,90,52), 'd': C(84,51,28), 's': C(107,74,47),
    'S': C(74,50,32), 'p': C(155,123,209), 'P': C(90,61,138), 'v': C(46,31,77), 'c': C(127,184,230),
    'C': C(59,111,182), 'n': C(27,33,72), 't': C(220,239,255,0.85), 'T': C(169,203,224,0.85), 'k': C(243,198,160),
    'i': C(229,138,180), 'e': C(58,156,138), 'x': C(46,42,42), 'a': C(98,160,72), 'A': C(72,128,56),
    'h': C(196,168,120),
    'f': C(178,176,168), 'N': C(224,194,150), '0': C(56,36,22), 'u': C(126,184,90), 'F': C(118,158,168),
    'H': C(92,146,104), 'X': C(84,80,84), 'E': C(36,108,96), 'U': C(214,158,118), 'Z': C(252,234,160),
    'J': C(150,105,30), 'j': C(176,214,242), 'z': C(204,220,238), 'I': C(184,96,140), 'V': C(190,165,230),
    'q': C(26,20,35,0.3), 'Q': C(176,214,242,0.3), '1': C(255,190,90,0.35),
}
def rgb(ch): return PAL[ch][:3]
def lerp(a, b, t): return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))

class Canvas:
    def __init__(self, bg=(0,0,0)):
        self.im = Image.new("RGBA", (W, H), bg + (255,))
        self.px = self.im.load()
    def put(self, x, y, col):
        if 0 <= x < W and 0 <= y < H:
            a = col[3] / 255 if len(col) > 3 else 1.0
            if a >= 1: self.px[x, y] = col[:3] + (255,)
            elif a > 0:
                d = self.px[x, y]
                self.px[x, y] = tuple(int(round(d[i] * (1 - a) + col[i] * a)) for i in range(3)) + (255,)
    def rect(self, x, y, w, h, col):
        for yy in range(y, y + h):
            for xx in range(x, x + w): self.put(xx, yy, col)
    def sprite(self, name, x, y, tint=None, alpha=1.0, flip=False, scale=1):
        rows = SPR[name]
        for j, row in enumerate(rows):
            if flip: row = row[::-1]
            for i, ch in enumerate(row):
                if ch == '.' : continue
                col = PAL[ch]
                if tint is not None and ch in 'WgG':
                    f = {'W': 1.0, 'g': 0.72, 'G': 0.45}[ch]
                    col = (int(tint[0]*f), int(tint[1]*f), int(tint[2]*f), 255)
                if alpha < 1: col = col[:3] + (int(col[3] * alpha),)
                for sy in range(scale):
                    for sx in range(scale): self.put(x + i*scale + sx, y + j*scale + sy, col)
    def size(self, name): return len(SPR[name][0]), len(SPR[name])
    def vgrad(self, x, y, w, h, top, bottom, bands=16):
        for i in range(bands):
            y0 = y + i * h // bands; y1 = y + (i + 1) * h // bands
            self.rect(x, y0, w, y1 - y0, lerp(top, bottom, i / (bands - 1)))
    def glow(self, cx, cy, rx, ry, col, a=0.35, steps=4):
        # Concentric soft rectangles, like the game's Glow helper but rounder.
        for s in range(steps, 0, -1):
            f = s / steps
            w, h = int(rx * f), int(ry * f)
            aa = a / steps
            for yy in range(cy - h, cy + h + 1):
                for xx in range(cx - w, cx + w + 1):
                    dx, dy = (xx - cx) / max(1, w), (yy - cy) / max(1, h)
                    if dx*dx + dy*dy <= 1: self.put(xx, yy, col + (int(aa * 255),))
    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        big = self.im.resize((W * SCALE, H * SCALE), Image.NEAREST).convert("RGB")
        big.save(os.path.join(OUT, name + ".png"))
        print("saved", name)

def hsh(*v):
    h = 2166136261
    for x in v:
        h = ((h ^ (x & 0xffffffff)) * 16777619) & 0xffffffff
    return h

# ---- Title font: chunky 2px-stroke capitals, 10 rows tall -------------------------------
FONT = {
'S': ["..######.","##....##.","##.......","##.......",".#####...","...####..","......##.","......##.","##....##.",".######.."],
'P': ["#######.","##....##","##....##","##....##","#######.","##......","##......","##......","##......","##......"],
'I': ["######","..##..","..##..","..##..","..##..","..##..","..##..","..##..","..##..","######"],
'C': [".######.","##....##","##......","##......","##......","##......","##......","##......","##....##",".######."],
'E': ["#######","##.....","##.....","##.....","######.","##.....","##.....","##.....","##.....","#######"],
'W': ["##......##","##......##","##......##","##......##","##..##..##","##..##..##","##..##..##","##..##..##","##.####.##",".###..###."],
'Z': ["########","......##",".....##.","....##..","...##...","..##....",".##.....","##......","##......","########"],
'A': [".######.","##....##","##....##","##....##","########","##....##","##....##","##....##","##....##","##....##"],
'R': ["#######.","##....##","##....##","##....##","#######.","##.##...","##..##..","##...##.","##....##","##....##"],
'D': ["######..","##...##.","##....##","##....##","##....##","##....##","##....##","##....##","##...##.","######.."],
' ': ["....","....","....","....","....","....","....","....","....","...."],
}
FH = 10
def text_width(s, scale, gap=1):
    return sum(len(FONT[c][0]) * scale + gap * scale for c in s) - gap * scale

def title(cv, s, x, y, scale=2, fill=None, shadow=(46,31,77), outline=(26,20,35), gap=1, shadow_off=None):
    """Draw chunky title text with a 1px outline, a gradient fill and a hard drop shadow."""
    fill = fill or [(252,234,160), (252,234,160), (242,201,76), (242,201,76), (242,201,76), (242,201,76), (201,148,42), (201,148,42), (201,148,42), (150,105,30)]
    so = shadow_off if shadow_off is not None else scale
    mask = set()
    cx = x
    for c in s:
        g = FONT[c]
        for j, row in enumerate(g):
            for i, ch in enumerate(row):
                if ch == '#':
                    for sy in range(scale):
                        for sx in range(scale): mask.add((cx + i*scale + sx, y + j*scale + sy, j))
        cx += (len(g[0]) + gap) * scale
    pts = {(px, py) for px, py, _ in mask}
    ring = set()
    for px, py in pts:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                if (px+dx, py+dy) not in pts: ring.add((px+dx, py+dy))
    if shadow:
        for px, py in pts | ring: cv.put(px + so, py + so, shadow)
    for px, py in ring: cv.put(px, py, outline)
    for px, py, j in mask: cv.put(px, py, fill[j])
    # a single glint row on the top edge of each letter
    for px, py, j in mask:
        if j == 0 and (px // scale) % 3 != 2: cv.put(px, py, (255, 252, 230))

FIRE_FILL = [(252,234,160),(242,201,76),(242,201,76),(232,118,43),(232,118,43),(217,65,47),(217,65,47),(217,65,47),(143,43,30),(143,43,30)]

def centered_title(cv, s, y, scale=2, **kw):
    title(cv, s, (W - text_width(s, scale)) // 2, y, scale, **kw)

# ---- Landscape helpers ------------------------------------------------------------------
HORIZON = 60
def far_hill(x): return HORIZON - 16 - int(9*math.sin(x*0.019) + 5*math.sin(x*0.047+1.3) + 3*math.sin(x*0.11+2.1))
def mid_hill(x): return HORIZON - 7 - int(6*math.sin(x*0.029+2.0) + 4*math.sin(x*0.071+0.4))

def hills(cv, far, far_rim, mid, mid_rim):
    for x in range(W):
        y = far_hill(x); cv.rect(x, y, 1, 1, far_rim); cv.rect(x, y+1, 1, HORIZON - y, far)
    for x in range(W):
        y = mid_hill(x); cv.rect(x, y, 1, 1, mid_rim); cv.rect(x, y+1, 1, HORIZON - y, mid)

def treeline(cv, col, dark):
    for x in range(W):
        h = 3 + (hsh(x // 3, 7) % 4)
        cv.rect(x, HORIZON - h, 1, h + 1, col)
        if hsh(x, 9) % 5 == 0: cv.rect(x, HORIZON - h + 1, 1, 1, dark)

def meadow(cv, top=HORIZON, base=(98,160,72), dark=(72,128,56), light=(126,184,90), n=900, seed=1):
    cv.rect(0, top, W, H - top, base)
    for i in range(n):
        x = hsh(i, seed) % W; y = top + hsh(i, seed + 1) % (H - top)
        k = hsh(i, seed + 2) % 3
        if k == 0: cv.rect(x, y, 1, 1, dark)
        elif k == 1: cv.rect(x, y, 1, 1, light)
        else: cv.rect(x, y, 2, 1, dark); cv.rect(x, y - 1, 1, 1, light)

def stars(cv, count=140, top=0, bottom=HORIZON - 20, a=1.0, seed=3):
    for i in range(count):
        x = hsh(i, seed) % W; y = top + hsh(i, seed + 1) % (bottom - top)
        tw = 0.45 + 0.55 * math.sin(i * 1.7)
        cv.put(x, y, (247,243,232, int(255 * tw * a)))
        if i % 9 == 0:
            cv.put(x-1, y, (247,243,232, int(60*a))); cv.put(x+1, y, (247,243,232, int(60*a)))
            cv.put(x, y-1, (247,243,232, int(60*a))); cv.put(x, y+1, (247,243,232, int(60*a)))

def tower(cv, x, y, w=48, h=120, lit=False, shade=None):
    """The wizard's round stone tower with the purple roof, drawn like SceneRenderer.DrawTower."""
    def sh(c): return lerp(c, shade[0], shade[1]) if shade else c
    cv.rect(x - 2, y + h, w + 10, 3, PAL['q']); cv.rect(x + w, y + h - 6, 8, 6, PAL['q'])
    cv.rect(x, y, 7, h, sh(rgb('f'))); cv.rect(x + 7, y, w - 18, h, sh(rgb('m'))); cv.rect(x + w - 11, y, 11, h, sh(rgb('M')))
    for yy in range(y + 5, y + h, 6):
        cv.rect(x + 1, yy, w - 2, 1, rgb('M') + (153,))
        off = 0 if ((yy - y) // 6) % 2 == 0 else 6
        for xx in range(x + 4 + off, x + w - 1, 12): cv.rect(xx, yy - 5, 1, 5, rgb('M') + (153,))
    cv.rect(x, y, 1, h, rgb('K')); cv.rect(x + w - 1, y, 1, h, rgb('K')); cv.rect(x, y + h - 1, w, 1, rgb('K'))
    if shade: cv.rect(x, y, w, h, shade[0] + (int(shade[1] * 120),))
    cv.sprite("roof", x - 2, y - 23)
    for wx, wy in ((x + 10, y + 16), (x + 30, y + 32)):
        if lit: cv.glow(wx + 4, wy + 5, 12, 11, (255,190,90), 0.5)
        cv.sprite("window_lit" if lit else "window", wx, wy)
    cv.sprite("door", x + 17, y + h - 26)

def plot_row(cv, x, y, kinds, wet=False, gap=28):
    for i, k in enumerate(kinds):
        px = x + i * gap
        cv.sprite("plot_wet" if wet else "plot", px, y)
        cv.sprite("mature_" + k, px + 4, y - 8)
        if k == "ghost": cv.sprite("ghost", px + 7, y - 16 + (i % 2))

def fence_run(cv, x, y, n):
    for i in range(n): cv.sprite(["fence", "fence_b", "fence_c", "fence_d"][hsh(i, 4) % 4], x + i * 12, y)
    cv.sprite("fence_post", x + n * 12, y)

def scatter(cv, names, x0, x1, y0, y1, n, seed):
    for i in range(n):
        nm = names[hsh(i, seed) % len(names)]
        cv.sprite(nm, x0 + hsh(i, seed+1) % (x1 - x0), y0 + hsh(i, seed+2) % (y1 - y0))

def vignette(cv, col=(26,20,35), strength=0.35):
    for y in range(H):
        for x in range(W):
            dx, dy = (x - W/2) / (W/2), (y - H/2) / (H/2)
            d = max(0.0, math.sqrt(dx*dx + dy*dy) - 0.55) / 0.6
            if d > 0: cv.put(x, y, col + (int(min(1, d*d) * strength * 255),))

def rain(cv, n=380, seed=11, col=(176,214,242), a=0.55, skip=None, fall=0):
    """`fall` scrolls the drops down (and a little left) so the trailer can animate the shower."""
    for i in range(n):
        x = (hsh(i, seed) - fall // 2) % W; y = (hsh(i, seed+1) + fall) % H; L = 3 + hsh(i, seed+2) % 4
        if skip and skip[0] <= x < skip[0] + skip[2] and skip[1] <= y + L < skip[1] + skip[3]: continue
        for k in range(L): cv.put(x - k // 2, y + k, col + (int(a*255*(1 - k/L)),))


# ---- The game's 5x7 bitmap font, read straight out of PixelFont.cs, for captions ------------------
_FONT5 = None
def font5():
    global _FONT5
    if _FONT5 is None:
        src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "src", "SpiceWizard.Web", "Art", "PixelFont.cs")
        txt = open(src, encoding="utf-8").read()
        body = txt.split("Data =")[1].split("};")[0]
        _FONT5 = [int(h, 16) for h in re.findall(r"0x([0-9A-Fa-f]{2})", body)]
    return _FONT5

def text5_width(s): return len(s) * 6 - 1

def text5(cv, s, x, y, col=(247,243,232), shadow=(26,20,35), scale=1):
    """Draw a caption in the game's own font; optional 1px drop shadow."""
    data = font5()
    for ch in s:
        code = max(32, min(126, ord(ch))) - 32
        for c in range(5):
            bits = data[code * 5 + c]
            for r in range(7):
                if bits >> r & 1:
                    for sy in range(scale):
                        for sx in range(scale):
                            if shadow: cv.put(x + c*scale + sx + scale, y + r*scale + sy + scale, shadow)
    for ch in s:
        code = max(32, min(126, ord(ch))) - 32
        for c in range(5):
            bits = data[code * 5 + c]
            for r in range(7):
                if bits >> r & 1:
                    for sy in range(scale):
                        for sx in range(scale): cv.put(x + c*scale + sx, y + r*scale + sy, col)
        x += 6 * scale
