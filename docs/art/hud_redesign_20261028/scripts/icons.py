from parts import *
import math
N = 32

def lum_fill(L, shape_fn, mat, cx, cy, r, levels=(5, 4, 3, 2), hl=None):
    for y in range(N):
        for x in range(N):
            if shape_fn(x+0.5, y+0.5):
                lum = (-(x+0.5-cx) - (y+0.5-cy)) / (r*1.6)
                i = levels[0] if lum > 0.5 else levels[1] if lum > 0.05 else levels[2] if lum > -0.45 else levels[3]
                L.px(x, y, R(mat, i))

def heart(L, mat='red', cx=16, cy=17, s=11):
    def f(x, y):
        X = (x-cx)/s; Y = -(y-cy)/s
        return (X*X + Y*Y - 0.6)**3 - X*X*Y**3 * 1.2 < 0
    lum_fill(L, f, mat, cx-3, cy-4, s, (4, 3, 2, 1))

def shield(L, mat='silver', rim='gold', cx=16, top=4, bot=28, hw=11, crack=False, emblem=None):
    def f(x, y):
        if y < top or y > bot: return False
        t = (y-top)/(bot-top)
        w = hw if t < 0.5 else hw*math.sqrt(max(0, 1-((t-0.5)/0.5)**2))
        return abs(x-cx) <= w
    def inner(x, y):
        if y < top+2 or y > bot-2: return False
        t = (y-top-2)/(bot-top-4)
        w = hw-2 if t < 0.5 else (hw-2)*math.sqrt(max(0, 1-((t-0.5)/0.5)**2))
        return abs(x-cx) <= w
    lum_fill(L, f, rim, cx-4, top+4, hw)
    lum_fill(L, inner, mat, cx-4, top+5, hw, (4, 3, 2, 1))
    for y in range(top+3, bot-2): L.px(cx, y, R(mat, 4)) if L.get(cx, y)[3] else None
    if crack:
        pts = [(cx+2, top+2), (cx-1, top+8), (cx+3, top+12), (cx-2, top+17), (cx+1, top+21)]
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            n = max(abs(bx-ax), abs(by-ay))
            for k in range(n+1):
                L.px(ax+(bx-ax)*k/n, ay+(by-ay)*k/n, OUT)

def crystal(L, cx=16, top=3, bot=27, hw=8, mat='cyan'):
    mid = top + (bot-top)*0.35
    def f(x, y):
        if y < top or y > bot: return False
        w = hw*(y-top)/(mid-top) if y < mid else hw*(bot-y)/(bot-mid)
        return abs(x-cx) <= w
    for y in range(N):
        for x in range(N):
            if f(x+0.5, y+0.5):
                facet = 4 if x < cx-2 else 3 if x < cx+1 else 2
                if y < mid and x < cx: facet += 1
                L.px(x, y, R(mat, facet))
    for y in range(top+2, bot-1): L.px(cx-1, y, R(mat, 5))

def arrow(L, cx, y0, y1, w, mat, up=True, head=6):
    hw = w//2
    for y in range(min(y0, y1), max(y0, y1)+1):
        tip = y0 if up else y1
        d = abs(y - tip)
        if d < head:
            half = d + 1
        else:
            half = hw
        for x in range(cx-half, cx+half+1):
            i = 4 if x < cx else 3 if x == cx else 2
            if d < head and abs(x-cx) == half: i = 2 if x > cx else 4
            L.px(x, y, R(mat, i))

def done(L):
    L.outline(); return L

# ---------------- 체력 ----------------
def hp_A():
    L = Layer(N, N); heart(L); L.px(9, 10, R('red', 4)); L.px(10, 9, (255, 200, 190, 255)); L.px(9, 11, (255, 160, 150, 255)); return done(L)
def hp_B():
    L = Layer(N, N); shield(L, 'red', 'gold')
    L.rect(14, 9, 17, 22, (245, 240, 230, 255)); L.rect(10, 13, 21, 16, (245, 240, 230, 255))
    L.vline(17, 9, 22, (190, 185, 180, 255)); L.hline(10, 21, 16, (190, 185, 180, 255))
    return done(L)
def hp_C():
    L = Layer(N, N)
    L.rect(3, 11, 28, 20, R('iron', 1)); L.hline(3, 28, 11, R('iron', 3))
    for k in range(6):
        x0 = 5 + k*4
        c = R('red', 3) if k < 4 else R('iron', 0)
        L.rect(x0, 13, x0+2, 18, c)
        if k < 4: L.hline(x0, x0+2, 13, R('red', 4)); L.px(x0, 14, (255, 150, 140, 255))
    heart(L, 'red', 26, 10, 5)
    return done(L)

# ---------------- 방어 ----------------
def def_A():
    L = Layer(N, N); shield(L, 'silver', 'gold'); L.px(12, 8, R('silver', 5)); L.px(11, 9, R('silver', 5)); return done(L)
def def_B():
    L = Layer(N, N)
    # breastplate
    def f(x, y):
        if y < 6 or y > 27: return False
        if y < 11: return 6 <= x <= 25 and not (11 <= x <= 20 and y < 9)
        t = (y-11)/16
        return abs(x-16) <= 10 - 3*t
    lum_fill(L, f, 'silver', 12, 11, 10)
    for y in range(9, 27): L.px(16, y, R('silver', 5))
    for x in range(8, 25): L.px(x, 18, R('gold', 3)) if L.get(x, 18)[3] else None
    L.rect(14, 17, 18, 19, R('gold', 5))
    return done(L)
def def_C():
    L = Layer(N, N); shield(L, 'blue', 'silver')
    L.rect(12, 10, 19, 17, R('gold', 4)); L.rect(13, 11, 18, 16, R('gold', 3))
    L.px(13, 11, R('gold', 6)); L.rect(14, 18, 17, 21, R('gold', 2))
    return done(L)

# ---------------- 이동속도 ----------------
BOOT = [
 "LMMMMD......",
 "GGGGGG......",
 ".LMMMD......",
 ".LMMMD......",
 ".LMMMD......",
 ".LMMMD......",
 ".LMMMMMD....",
 ".LMMMMMMMD..",
 ".LMMMMMMMMD.",
 "LMMMMMMMMMMD",
 "DDDDDDDDDDDD",
]
BS = {'L': 4, 'M': 3, 'D': 1}
def boot(L, x, y):
    S = Layer(N, N)
    for j, row in enumerate(BOOT):
        for i, ch in enumerate(row):
            if ch == 'G': S.px(x+i, y+j, R('gold', 4))
            elif ch in BS: S.px(x+i, y+j, R('leather', BS[ch]))
    L.comp(S)

def sword(L, x0, y0, x1, y1):
    """45-degree sword, tip at (x0,y0) going down-left to guard at (x1,y1)"""
    n = x0 - x1
    for k in range(n+1):
        x = x0-k; y = y0+k
        L.px(x-1, y, R('silver', 5)); L.px(x, y, R('silver', 4)); L.px(x+1, y, R('silver', 2))
    for t in range(-4, 5): L.px(x1+t, y1+t, R('gold', 5 if t < 0 else 3)); L.px(x1+t, y1+t+1, R('gold', 2))
    for t in range(1, 5): L.px(x1-t, y1+t, R('wood', 4)); L.px(x1-t+1, y1+t, R('wood', 2))
    L.rect(x1-6, y1+5, x1-5, y1+6, R('gold', 4))

def spd_A():
    L = Layer(N, N)
    boot(L, 15, 10)
    for (y, x0, x1) in ((11, 3, 11), (16, 1, 11), (21, 4, 11)):
        L.hline(x0, x1, y, R('cyan', 4)); L.px(x0, y, R('cyan', 2))
    return done(L)
def spd_B():
    L = Layer(N, N)
    for ox in (2, 14):
        def f(x, y, ox=ox): return 0 <= x-ox <= 12 and abs(y-16) <= 12 - (x-ox)
        for y in range(N):
            for x in range(N):
                if f(x+0.5, y+0.5): L.px(x, y, R('cyan', 4 if y < 15 else 3 if y < 18 else 2))
    return done(L)
def spd_C():
    L = Layer(N, N)
    boot(L, 8, 12)
    for k, (x0, y0, ln) in enumerate(((1, 9, 9), (2, 12, 8), (3, 15, 6))):
        L.hline(x0, x0+ln, y0, R('silver', 5)); L.hline(x0+1, x0+ln, y0+1, R('silver', 3))
    return done(L)


# ---------------- 코어 피해 ----------------
def core_A():
    L = Layer(N, N); crystal(L, 15, 3, 28, 8)
    pts = [(17, 5), (14, 11), (18, 15), (13, 21), (16, 26)]
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = max(abs(bx-ax), abs(by-ay))
        for k in range(n+1): L.px(ax+(bx-ax)*k/n, ay+(by-ay)*k/n, R('red', 3))
    return done(L)
def core_B():
    L = Layer(N, N); crystal(L, 13, 2, 26, 8)
    for k in range(10):
        for d in (0, 1, 2):
            L.px(18+k+d, 17+k, R('red', 4 if d == 0 else 3)); L.px(28-k+d-1, 17+k, R('red', 3 if d < 2 else 2))
    return done(L)
def core_C():
    L = Layer(N, N); crystal(L, 12, 2, 28, 8)
    arrow(L, 25, 10, 27, 5, 'red', up=False, head=5)
    return done(L)

# ---------------- 초당 피해 ----------------
def stopwatch(L, cx, cy, r):
    L.ellipse(cx, cy, r, r, lambda x, y: R('silver', 4 if x+y < cx+cy else 2))
    L.ellipse(cx, cy, r-2, r-2, (235, 240, 245, 255))
    L.rect(cx-1, cy-r-3, cx+1, cy-r, R('silver', 4))
    L.vline(cx, cy-r+3, cy, OUT); L.hline(cx, cx+r-3, cy, R('red', 3))
def dps_A():
    L = Layer(N, N)
    sword(L, 22, 3, 8, 17)
    stopwatch(L, 22, 21, 7)
    return done(L)
def dps_B():
    L = Layer(N, N)
    # burst star
    for y in range(N):
        for x in range(N):
            dx, dy = x+0.5-13, y+0.5-14; a = math.atan2(dy, dx); d = math.hypot(dx, dy)
            rr = 7 + 4*max(0, math.cos(a*5))
            if d <= rr: L.px(x, y, R('gold', 5 if d < 4 else 4 if d < 7 else 3))
    L.rect(11, 12, 14, 15, (255, 250, 220, 255))
    stopwatch(L, 23, 22, 6)
    return done(L)
def dps_C():
    L = Layer(N, N)
    sword(L, 20, 3, 7, 16)
    # small hourglass
    for y in range(14, 29):
        w = abs(y-21)//2 + 1
        L.hline(24-w, 24+w, y, R('gold', 4) if y in (14, 28) else (R('gold', 5) if abs(y-21) > 5 else (240, 220, 160, 255)))
    return done(L)

# ---------------- 약점 / 저항 ----------------
def weak_A():
    L = Layer(N, N); shield(L, 'silver', 'gold', crack=True); return done(L)
def res_A():
    L = Layer(N, N); shield(L, 'iron', 'silver')
    L.rect(14, 9, 17, 21, R('silver', 4)); L.vline(17, 9, 21, R('silver', 2)); return done(L)
def weak_B():
    L = Layer(N, N); arrow(L, 16, 3, 28, 10, 'red', up=True, head=10); return done(L)
def res_B():
    L = Layer(N, N); arrow(L, 16, 3, 28, 10, 'silver', up=False, head=10); return done(L)
def weak_C():
    L = Layer(N, N)
    for r, c in ((12, R('red', 3)), (9, (240, 235, 225, 255)), (6, R('red', 3)), (3, (240, 235, 225, 255)), (1.5, R('red', 4))):
        L.ellipse(16, 16, r, r, c)
    L.ellipse(16, 16, 12, 12, lambda x, y: None)
    return done(L)
def res_C():
    L = Layer(N, N)
    for row in range(4):
        y0 = 7 + row*5; off = 0 if row % 2 == 0 else 4
        for bx in range(-4, 28, 8):
            x0 = max(4, bx+off); x1 = min(27, bx+off+6)
            if x1 <= x0: continue
            L.rect(x0, y0, x1, y0+3, R('stone', 4)); L.hline(x0, x1, y0, R('stone', 6)); L.hline(x0, x1, y0+3, R('stone', 2))
    return done(L)

H5 = [
    ('hp', '체력', [hp_A, hp_B, hp_C], ['붉은 하트', '십자 방패', '체력 칸 막대']),
    ('def', '방어', [def_A, def_B, def_C], ['금테 은방패', '흉갑', '청색 문장 방패']),
    ('spd', '이동속도', [spd_A, spd_B, spd_C], ['장화 + 속도선', '청록 이중 화살', '날개 장화']),
    ('coredmg', '코어 피해', [core_A, core_B, core_C], ['금 간 수정', '수정 + 붉은 X', '수정 + 아래 화살']),
    ('dps', '초당 피해', [dps_A, dps_B, dps_C], ['검 + 시계', '폭발 + 시계', '검 + 모래시계']),
    ('weak', '약점', [weak_A, weak_B, weak_C], ['금 간 방패', '붉은 위 화살', '붉은 과녁']),
    ('res', '저항', [res_A, res_B, res_C], ['단단한 강철 방패', '회색 아래 화살', '돌벽']),
]

if __name__ == '__main__':
    from PIL import Image
    S = 5; pad = 6
    rows = len(H5)
    c = Image.new('RGBA', (3*(N*S+pad)+N*S, rows*(N*S+pad)), (40, 40, 48, 255))
    ref = Image.open(ART+'UI/Resources/CoreGuardian/Hud/StatDamage.png').convert('RGBA')
    for r, (_, _, fns, _) in enumerate(H5):
        for i, f in enumerate(fns):
            c.alpha_composite(f().img().resize((N*S, N*S), Image.NEAREST), (i*(N*S+pad), r*(N*S+pad)))
        c.alpha_composite(ref.resize((N*S, N*S), Image.NEAREST), (3*(N*S+pad), r*(N*S+pad)))
    c.save('t_h5.png')
