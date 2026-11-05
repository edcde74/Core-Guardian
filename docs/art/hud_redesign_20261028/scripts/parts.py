from px import *
import random

def plinth(L, cx, yc, hw, hd, h, k=None, seed=1, trim=True, gem=True, panel_color='blue'):
    """octagonal stone platform; yc = y of top face centre line (widest), hd = half depth"""
    rnd = random.Random(seed)
    k = k if k is not None else max(3, hw//4)
    top = [(cx-hw, yc), (cx-hw+k, yc-hd), (cx+hw-k, yc-hd), (cx+hw, yc), (cx+hw-k, yc+hd), (cx-hw+k, yc+hd)]
    P = Layer(L.w, L.h)
    # front faces (extrude lower half)
    left  = [(cx-hw, yc), (cx-hw+k, yc+hd), (cx-hw+k, yc+hd+h), (cx-hw, yc+h)]
    front = [(cx-hw+k, yc+hd), (cx+hw-k, yc+hd), (cx+hw-k, yc+hd+h), (cx-hw+k, yc+hd+h)]
    right = [(cx+hw-k, yc+hd), (cx+hw, yc), (cx+hw, yc+h), (cx+hw-k, yc+hd+h)]
    def brick(base, x0):
        def f(x, y):
            # rows of 5px measured from face top at this x
            rel = y - (yc+hd if cx-hw+k <= x <= cx+hw-k else (yc + (x-(cx-hw))*hd/k if x < cx else yc + ((cx+hw)-x)*hd/k))
            row = int(rel)//5
            if int(rel) % 5 == 4: return R('stone', base-2)
            off = 0 if row % 2 == 0 else 4
            if (x - x0 + off) % 9 == 0: return R('stone', base-2)
            if int(rel) % 5 == 0: return R('stone', base+1)
            return dither('stone', base, x, y, rnd)
        return f
    P.poly(left, brick(4, cx-hw)); P.poly(front, brick(3, cx-hw+k)); P.poly(right, brick(2, cx+hw-k))
    # top slab: tiles in perspective
    def topf(x, y):
        if (x - cx) % 10 == 0 or (y - (yc-hd)) % 6 == 5: return R('stone', 3)
        if (y - (yc-hd)) % 6 == 0: return R('stone', 6)
        return dither('stone', 5, x, y, rnd, 0.22)
    P.poly(top, topf)
    # top rim highlight along front edge of top face
    for x in range(cx-hw+k, cx+hw-k+1): P.px(x, yc+hd, R('stone', 6))
    if trim:
        # gold vertical brackets at the 4 front vertices
        for vx in [cx-hw, cx-hw+k, cx+hw-k, cx+hw]:
            x0 = vx-1 if vx not in (cx-hw,) else vx
            x0 = min(max(x0, cx-hw), cx+hw-2)
            ytop = yc if vx in (cx-hw, cx+hw) else yc+hd
            for y in range(ytop, ytop+h+1):
                P.px(x0, y, R('gold', 4)); P.px(x0+1, y, R('gold', 2))
            P.px(x0, ytop, R('gold', 6)); P.px(x0+1, ytop, R('gold', 5))
        # gold rim on top front edge
        for x in range(cx-hw+k, cx+hw-k+1): P.px(x, yc+hd+1, R('gold', 3) if x % 2 else R('gold', 4))
    if gem:
        gx = cx; gy = yc+hd+2
        P.rect(gx-3, gy, gx+3, gy+h-2, R('gold', 3))
        P.rect(gx-2, gy+1, gx+2, gy+h-3, R(panel_color, 1))
        P.vline(gx-1, gy+2, gy+h-4, R(panel_color, 3))
        P.rect(gx-1, gy+3, gx+1, gy+h-5, R('cyan', 3)); P.px(gx-1, gy+3, R('cyan', 5)); P.px(gx+1, gy+h-5, R('cyan', 1))
        P.hline(gx-3, gx+3, gy, R('gold', 5))
    P.outline()
    L.comp(P)
    return P

def box(L, x0, y0, w, h, d, mat_front, mat_top, mat_side=None, seed=2, front_fn=None):
    """3/4 box: front rect (x0,y0,w,h), top face of depth d skewed up, right side face of width d//2"""
    mat_side = mat_side or mat_front
    B = Layer(L.w, L.h)
    sx = max(2, d//2)
    top = [(x0, y0), (x0+sx, y0-d), (x0+w+sx, y0-d), (x0+w, y0)]
    side = [(x0+w, y0), (x0+w+sx, y0-d), (x0+w+sx, y0-d+h), (x0+w, y0+h)]
    B.poly(top, lambda x, y: R(mat_top[0], mat_top[1]))
    B.poly(side, lambda x, y: R(mat_side[0], mat_side[1]-2))
    B.rect(x0, y0, x0+w-1, y0+h-1, (lambda x, y: front_fn(x, y)) if front_fn else R(mat_front[0], mat_front[1]))
    return B

ART = '/mnt/user-data/uploads/CD/Assets/Art/'
SINGLE = load(ART+'Characters/Towers/Sprites/tower_single.png')
SUPPORT = load(ART+'Characters/Towers/Sprites/tower_support.png')
CORE = load(ART+'Characters/Core/Sprites/core.png')

def cyl_shade(mat, x, x0, x1, lo=1, hi=4):
    t = (x - x0) / max(1, (x1 - x0))
    # lit from left-front: brightest ~30%, darkest right edge
    if t < 0.12: i = hi-1
    elif t < 0.45: i = hi
    elif t < 0.75: i = hi-1
    elif t < 0.9: i = lo+1
    else: i = lo
    return R(mat, i)

def cylinder(L, x0, x1, ytop, ybot, mat, ry=3, lo=1, hi=4, cap=None):
    C = Layer(L.w, L.h)
    cx = (x0+x1)/2; rx = (x1-x0+1)/2
    # body
    for x in range(x0, x1+1):
        dy = ry*np.sqrt(max(0, 1-((x+0.5-cx)/rx)**2))
        for y in range(int(ytop), int(ybot+dy)+1):
            C.px(x, y, cyl_shade(mat, x, x0, x1, lo, hi))
    # top cap ellipse
    capm = cap or (mat, hi+1)
    C.ellipse(cx, ytop, rx, ry, lambda x, y: R(capm[0], capm[1]) if y < ytop else R(capm[0], capm[1]-1))
    return C

def band(L, x0, x1, y, cx, rx, ry, mat='gold', thick=2):
    """gold ring following the front curve of a cylinder"""
    for x in range(x0, x1+1):
        dy = ry*np.sqrt(max(0, 1-((x+0.5-cx)/rx)**2))
        for t in range(thick):
            i = 5 if t == 0 else 3
            t2 = (x-x0)/max(1,(x1-x0))
            if t2 > 0.75: i -= 1
            if t2 > 0.9: i -= 1
            L.px(x, int(round(y+dy))+t, R(mat, i))

def gear(L, cx, cy, r, teeth=8, mat='gold', hole=True, lit=True):
    import math
    G = Layer(L.w, L.h)
    for y in range(int(cy-r-2), int(cy+r+3)):
        for x in range(int(cx-r-2), int(cx+r+3)):
            dx, dy = x+0.5-cx, y+0.5-cy
            d = math.hypot(dx, dy); a = math.atan2(dy, dx)
            tooth = (math.cos(a*teeth) > 0.2)
            rr = r + (1.6 if tooth else 0)
            if d <= rr:
                if hole and d < r*0.38: continue
                lum = (-dx-dy)/(r*1.4)
                i = 5 if lum > 0.45 else 4 if lum > 0.0 else 3 if lum > -0.45 else 2
                if d > r - 0.8 and not tooth: i -= 1
                G.px(x, y, R(mat, i))
    G.outline()
    L.comp(G)

def coin_stack(L, x, y, n=3):
    C = Layer(L.w, L.h)
    for k in range(n):
        yy = y - k*2
        C.hline(x, x+6, yy, R('gold', 3)); C.hline(x, x+6, yy+1, R('gold', 2))
        C.hline(x+1, x+5, yy-1, R('gold', 5))
    C.px(x+2, y-2*(n-1)-1, R('gold', 6))
    C.outline(); L.comp(C)

def wrench(L, x0, y0, x1, y1, mat='silver', w=2):
    """thick wrench along a line, open jaw at (x0,y0), ring at (x1,y1)"""
    import math
    Wl = Layer(L.w, L.h)
    n = int(max(abs(x1-x0), abs(y1-y0)))
    for t in range(n+1):
        x = x0 + (x1-x0)*t/n; y = y0 + (y1-y0)*t/n
        for i in range(w):
            Wl.px(x+i, y, R(mat, 4 if i == 0 else 2))
    # jaw (C shape) at start
    for a in range(0, 360, 20):
        r = 3.2; rad = math.radians(a)
        if 200 < a < 300: continue
        Wl.px(x0 + math.cos(rad)*r, y0 + math.sin(rad)*r, R(mat, 3 if a < 180 else 4))
    # ring at end
    for a in range(0, 360, 20):
        rad = math.radians(a)
        Wl.px(x1 + math.cos(rad)*2.5, y1 + math.sin(rad)*2.5, R(mat, 3))
    Wl.outline(); L.comp(Wl)

WRENCH_T = [
 "LM...MD",
 "LM...MD",
 "LMM.MMD",
 ".LMMMD.",
 "..LMD..",
 "..LMD..",
 "..LMD..",
 "..LMD..",
 "..LMD..",
 "..LMD..",
 "..LMD..",
 ".LMMMD.",
 ".LM.MD.",
 ".LMMMD.",
]
def sprite(L, tmpl, x0, y0, mat, shades={'L': 5, 'M': 3, 'D': 1, 'H': 6}, outline=True):
    S = Layer(L.w, L.h)
    for j, row in enumerate(tmpl):
        for i, ch in enumerate(row):
            if ch in shades: S.px(x0+i, y0+j, R(mat, shades[ch]))
    if outline: S.outline()
    L.comp(S)

def big_crystal(L, cx, top, bot, hw, tilt=0):
    """faceted cyan crystal, lit from top-left"""
    C = Layer(L.w, L.h)
    mid = top + (bot-top)*0.3
    for y in range(int(top), int(bot)+1):
        if y < mid: w = hw*(y-top)/(mid-top)
        else: w = hw*(1 - 0.35*(y-mid)/(bot-mid))
        for x in range(int(cx-w), int(cx+w)+1):
            t = (x-(cx-w))/max(1, 2*w)
            i = 5 if t < 0.18 else 4 if t < 0.45 else 3 if t < 0.7 else 2
            if y < mid and t < 0.5: i = min(5, i+1)
            # facet lines
            if abs(x-cx) < 1 and y > mid: i = 2
            C.px(x, y, R('cyan', i))
    # highlight streaks
    for y in range(int(mid), int(bot)-2):
        C.px(cx-hw*0.55, y, R('cyan', 5))
    C.outline(); L.comp(C)

def pipe(L, pts, mat='gold', glow=False):
    P = Layer(L.w, L.h)
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = max(abs(bx-ax), abs(by-ay))
        for k in range(n+1):
            x = ax+(bx-ax)*k/n; y = ay+(by-ay)*k/n
            if ax == bx:   # vertical
                P.px(x-1, y, R(mat, 5)); P.px(x, y, R(mat, 4)); P.px(x+1, y, R(mat, 2))
            else:
                P.px(x, y-1, R(mat, 5)); P.px(x, y, R(mat, 4)); P.px(x, y+1, R(mat, 2))
    if glow:
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            n = max(abs(bx-ax), abs(by-ay))
            for k in range(0, n+1, 3):
                P.px(ax+(bx-ax)*k/n, ay+(by-ay)*k/n, R('cyan', 4))
    P.outline(); L.comp(P)

def gear2(L, cx, cy, r, teeth=8, mat='gold', keep=True):
    """crisp gear: square teeth, rim, dark inner disc, 4 spokes, hub bolt"""
    import math
    G = Layer(L.w, L.h)
    for y in range(int(cy-r-3), int(cy+r+4)):
        for x in range(int(cx-r-3), int(cx+r+4)):
            dx, dy = x+0.5-cx, y+0.5-cy
            d = math.hypot(dx, dy); a = math.atan2(dy, dx)
            ph = (a*teeth/(2*math.pi)) % 1.0
            tooth = ph < 0.45
            lit = (-dx-dy) / (r*1.41)
            if d <= r-0.5 or (tooth and d <= r+2.2):
                if d > r-2.5:                       # rim + teeth
                    i = 5 if lit > 0.35 else 4 if lit > -0.1 else 3 if lit > -0.5 else 2
                elif d < r*0.28:                    # hub
                    i = 5 if lit > 0 else 3
                elif any(abs(((a - k*math.pi/2 + math.pi) % (2*math.pi)) - math.pi) * d < 1.3 for k in range(4)):
                    i = 4 if lit > -0.2 else 3      # spokes
                else:
                    i = 1                           # dark inner disc
                G.px(x, y, R(mat, i))
    G.px(cx-1, cy-1, R(mat, 6))
    G.outline(); L.comp(G, keep=keep)

def core_crystal(top_rows=34):
    """the approved core crystal, cut above its gold spike, outlined"""
    src = CORE
    C = Layer(36, top_rows+3)
    for y in range(0, top_rows):
        for x in range(28, 62):
            r, g, b, a = [int(v) for v in src[y, x]]
            if a < 200: continue
            cyanish = (b > 140 and b >= g - 10 and r < 200) or (r > 180 and g > 230 and b > 230)
            navy = (b > 60 and r < 40 and g < 90)
            if cyanish or navy: C.a[y+1, x-28+1] = src[y, x]
    from scipy import ndimage as _nd
    m = C.a[:, :, 3] > 0; lab, n = _nd.label(m)
    if n > 1:
        big = 1 + int(np.argmax([(lab == i).sum() for i in range(1, n+1)]))
        C.a[(lab != big) & m] = 0
    if top_rows < 34:
        # flat cut at the bottom: shade the last row darker
        pass
    C.outline()
    return C
