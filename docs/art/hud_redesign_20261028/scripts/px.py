"""Tiny pixel-art toolkit matching Core Guardian originals (dark outline, stone/gold/blue/cyan ramps)."""
import numpy as np, random
from PIL import Image

def hx(s):
    s = s.lstrip('#'); return (int(s[0:2],16), int(s[2:4],16), int(s[4:6],16), 255)

OUT = hx('0a0b14')
RAMP = {
    'stone': [hx(c) for c in ['24262c','363840','4a4c53','5f6168','7a7b81','9c9da1','bdbdc0']],
    'gold':  [hx(c) for c in ['4a2a08','74460f','a46a1a','d39a2b','eebd47','fbe07e','fff4bf']],
    'blue':  [hx(c) for c in ['061233','0b2463','133c98','1d58c4','3277e0','5a9cf2']],
    'cyan':  [hx(c) for c in ['063a5e','0a76b0','04b4ec','30dcff','9cf4ff','e8feff']],
    'iron':  [hx(c) for c in ['15171c','23262d','343841','4a4f59','666b75','8d929b']],
    'wood':  [hx(c) for c in ['2a1408','482410','6a3818','8c5226','ad6f38','c98f52']],
    'leather':[hx(c) for c in ['2c1a0e','4a2c16','6b4222','8b5a30','a87442']],
    'red':   [hx(c) for c in ['3a0808','681010','9a1c18','c8302a','ee5a48']],
    'silver':[hx(c) for c in ['2a2d34','4b505a','737985','9ea4ae','c9cdd4','eef0f4']],
    'green': [hx(c) for c in ['0b2a12','14501f','1f7a2c','36a842','6fd16a']],
    'cloth': [hx(c) for c in ['1a2a4a','24406e','2f5794','4a74b4','7097cc']],
}

class Layer:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.a = np.zeros((h, w, 4), np.uint8)
        s.keep = np.zeros((h, w), bool)
    def px(s, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < s.w and 0 <= y < s.h and c is not None:
            s.a[y, x] = c
    def get(s, x, y):
        if 0 <= x < s.w and 0 <= y < s.h: return tuple(s.a[y, x])
        return (0,0,0,0)
    def rect(s, x0, y0, x1, y1, c):
        for y in range(y0, y1+1):
            for x in range(x0, x1+1): s.px(x, y, c)
    def hline(s, x0, x1, y, c):
        for x in range(x0, x1+1): s.px(x, y, c)
    def vline(s, x, y0, y1, c):
        for y in range(y0, y1+1): s.px(x, y, c)
    def poly(s, pts, cfn):
        """fill polygon; cfn(x,y)->color or color"""
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys))+1):
            for x in range(int(min(xs)), int(max(xs))+1):
                if inside(pts, x+0.5, y+0.5):
                    s.px(x, y, cfn(x, y) if callable(cfn) else cfn)
    def ellipse(s, cx, cy, rx, ry, cfn):
        for y in range(int(cy-ry)-1, int(cy+ry)+2):
            for x in range(int(cx-rx)-1, int(cx+rx)+2):
                if ((x+0.5-cx)/rx)**2 + ((y+0.5-cy)/ry)**2 <= 1.0:
                    s.px(x, y, cfn(x, y) if callable(cfn) else cfn)
    def mask(s): return s.a[:, :, 3] > 0
    def outline(s, c=OUT, diag=False):
        m = s.mask(); o = np.zeros_like(m)
        o[1:, :] |= m[:-1, :]; o[:-1, :] |= m[1:, :]; o[:, 1:] |= m[:, :-1]; o[:, :-1] |= m[:, 1:]
        if diag:
            o[1:,1:] |= m[:-1,:-1]; o[:-1,:-1] |= m[1:,1:]; o[1:,:-1] |= m[:-1,1:]; o[:-1,1:] |= m[1:,:-1]
        o &= ~m
        s.a[o] = c
        return s
    def comp(s, other, dx=0, dy=0, keep=False):
        b = other.a if isinstance(other, Layer) else other
        ok = other.keep if isinstance(other, Layer) else None
        h, w = b.shape[:2]
        for y in range(h):
            ty = y+dy
            if not (0 <= ty < s.h): continue
            for x in range(w):
                tx = x+dx
                if 0 <= tx < s.w and b[y, x, 3] > 0:
                    s.a[ty, tx] = b[y, x]
                    s.keep[ty, tx] = keep or (ok is not None and ok[y, x])
        return s
    def img(s): return Image.fromarray(s.a, 'RGBA')

def inside(pts, x, y):
    n = len(pts); ins = False; j = n-1
    for i in range(n):
        xi, yi = pts[i]; xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj-xi)*(y-yi)/(yj-yi+1e-9)+xi):
            ins = not ins
        j = i
    return ins

def load(path):
    im = Image.open(path).convert('RGBA'); return np.array(im)

def crop(arr, x0, y0, x1, y1):
    return arr[y0:y1+1, x0:x1+1].copy()

def recolor(arr, fn):
    a = arr.copy()
    for y in range(a.shape[0]):
        for x in range(a.shape[1]):
            if a[y,x,3] > 0:
                a[y,x] = fn(tuple(a[y,x]))
    return a

R = lambda m, i: RAMP[m][max(0, min(len(RAMP[m])-1, i))]

def dither(m, base, x, y, rnd, p=0.18):
    """stone-like texture: mostly base, sometimes +/-1"""
    r = rnd.random()
    if r < p/2: return R(m, base-1)
    if r < p: return R(m, base+1)
    return R(m, base)

def glow_outline(layer, c, width=1):
    """highlight state: colored ring outside the dark outline"""
    out = Layer(layer.w, layer.h); out.a = layer.a.copy()
    for _ in range(width):
        m = out.mask(); o = np.zeros_like(m)
        o[1:, :] |= m[:-1, :]; o[:-1, :] |= m[1:, :]; o[:, 1:] |= m[:, :-1]; o[:, :-1] |= m[:, 1:]
        o &= ~m; out.a[o] = c
    return out

def save_zoom(arr_or_layer, path, S=6, bg=(58,66,58,255)):
    a = arr_or_layer.a if isinstance(arr_or_layer, Layer) else arr_or_layer
    im = Image.fromarray(a, 'RGBA')
    c = Image.new('RGBA', (im.width*S, im.height*S), bg)
    c.alpha_composite(im.resize((im.width*S, im.height*S), Image.NEAREST))
    c.save(path)
