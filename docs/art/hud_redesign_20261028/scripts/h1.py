from parts import *
import random
W, H = 84, 104
_c = Layer(13, 18); _c.comp(crop(SUPPORT, 17, 10, 25, 23), 2, 2); _c.outline()
CRYS = _c.a                                     # small floating crystal (outlined)
_b = Layer(26, 18); _b.comp(crop(SINGLE, 0, 0, 23, 15), 1, 1); _b.a[:, 20:] = 0; _b.outline()
BARREL = _b.a                                   # diagonal cannon barrel (outlined)

def clean(arr, keep_fn):
    a = arr.copy()
    for y in range(a.shape[0]):
        for x in range(a.shape[1]):
            if not keep_fn(x, y): a[y, x] = 0
    return a

def shade_rect(L, x0, y0, x1, y1, mat, top=4, bot=2, rnd=None, noise=0.1, lit_left=True):
    rnd = rnd or random.Random(7)
    for y in range(y0, y1+1):
        t = (y-y0)/max(1, y1-y0)
        base = round(top + (bot-top)*t)
        for x in range(x0, x1+1):
            b = base
            if lit_left and x == x0: b += 1
            if x == x1: b -= 1
            L.px(x, y, dither(mat, b, x, y, rnd, noise))

def gold_post(L, x, y0, y1, w=3):
    for y in range(y0, y1+1):
        for i in range(w):
            L.px(x+i, y, R('gold', [5, 4, 2, 1][min(i, 3)] if w > 2 else [4, 2][i]))
    for i in range(w): L.px(x+i, y0, R('gold', 6)); L.px(x+i, y1, R('gold', 1))

def bolt(L, x, y):
    L.px(x, y, R('gold', 6)); L.px(x+1, y+1, R('gold', 1))

# ---------- A: 수정 연구탑 (cylindrical lab tower + dome + crystal, cannon test stand) ----------
def lab_tower(seed=11):
    L = Layer(W, H)
    plinth(L, 42, 78, 36, 11, 12, seed=seed)
    T = Layer(W, H)
    # tower body
    C = cylinder(T, 26, 58, 52, 82, 'blue', ry=4, lo=1, hi=4, cap=('gold', 4))
    T.comp(C)
    # stone foot ring
    F = cylinder(Layer(W, H), 22, 62, 80, 86, 'stone', ry=5, lo=1, hi=5, cap=('stone', 5))
    for x in range(26, 59):
        cx, rx = 42, 16.5
    T2 = Layer(W, H); T2.comp(F); T2.comp(C)
    band(T2, 26, 58, 66, 42, 16.5, 4, 'gold', 2)
    band(T2, 26, 58, 52, 42, 16.5, 4, 'gold', 2)
    band(T2, 22, 62, 79, 42, 20.5, 5, 'gold', 1)
    # dome (glass) on top
    D = Layer(W, H)
    def domef(x, y):
        if y >= 52: return None
        dx, dy = (x-42)/13, (y-52)/12
        if (x-37)**2 + (y-44)**2 < 5: return R('cyan', 5)
        lum = -dx*0.8 - dy*0.6
        i = 4 if lum > 0.55 else 3 if lum > 0.05 else 2 if lum > -0.45 else 1
        return R('cyan', i)
    D.ellipse(42, 52, 13, 12, domef)
    for x in range(29, 56): D.px(x, 51, R('gold', 5 if x < 44 else 3)); D.px(x, 52, R('gold', 2))
    for y in range(41, 51): D.px(42, y, R('gold', 4))
    for t in range(2, 11):
        D.px(36 + t//4, 51-t, R('gold', 4)); D.px(48 - t//4, 51-t, R('gold', 2))
    D.rect(41, 38, 43, 40, R('gold', 5))
    T2.comp(D)
    # windows / panels on body
    for wx in (33, 47):
        T2.rect(wx-1, 69, wx+4, 77, R('gold', 3))
        T2.rect(wx, 70, wx+3, 76, R('cyan', 2))
        T2.vline(wx+1, 70, 76, R('cyan', 4)); T2.px(wx, 70, R('cyan', 5))
    T2.rect(40, 57, 44, 63, R('gold', 4)); T2.rect(41, 58, 43, 62, R('cyan', 3)); T2.px(41, 58, R('cyan', 5))
    for x in (28, 55): bolt(T2, x, 59)
    T2.outline()
    L.comp(T2)
    # crystal floating above dome
    L.comp(CRYS, 36, 18)
    # cannon on test stand (left)
    S = Layer(W, H)
    S.rect(8, 60, 11, 78, R('iron', 3)); S.vline(8, 60, 78, R('iron', 4)); S.vline(11, 60, 78, R('iron', 1))
    S.rect(5, 76, 14, 79, R('iron', 2)); S.hline(5, 14, 76, R('iron', 4))
    S.rect(6, 56, 13, 61, R('gold', 3)); S.hline(6, 13, 56, R('gold', 5))
    S.outline()
    L.comp(S)
    L.comp(BARREL, 0, 43)
    return L

# ---------- B: 강화 정비대 (turret on work pedestal, two crystal pylons scanning it) ----------
def scan_bay(seed=12):
    L = Layer(W, H)
    plinth(L, 42, 78, 36, 11, 12, seed=seed)
    rnd = random.Random(seed)
    # pedestal (3/4 box)
    P = Layer(W, H)
    x0, x1, yt, yb = 25, 59, 72, 84
    P.poly([(x0, yt), (x0+5, yt-6), (x1+5, yt-6), (x1, yt)], lambda x, y: R('gold', 4) if y == yt-6 else R('blue', 4 if y < yt-3 else 3))
    shade_rect(P, x0, yt, x1, yb, 'blue', top=3, bot=1, rnd=rnd, noise=0.06)
    P.poly([(x1+1, yt), (x1+5, yt-6), (x1+5, yb-6), (x1+1, yb)], lambda x, y: R('blue', 0))
    P.hline(x0, x1, yt, R('gold', 5)); P.hline(x0, x1, yt+1, R('gold', 3)); P.hline(x0, x1, yb, R('gold', 2))
    for gx in (x0, x1-2): gold_post(P, gx, yt, yb, 3)
    P.rect(39, yt+3, 45, yb-2, R('gold', 3)); P.rect(40, yt+4, 44, yb-3, R('cyan', 2)); P.vline(41, yt+4, yb-3, R('cyan', 4)); P.px(40, yt+4, R('cyan', 5))
    P.outline()
    L.comp(P)
    # turret top from the single tower, sitting on pedestal
    _h = Layer(54, 36); _h.comp(crop(SINGLE, 0, 0, 51, 33), 1, 1); _h.outline()
    L.comp(_h, 16, 35)
    # crystal pylons
    for px_, flip in ((9, 1), (73, -1)):
        Y = Layer(W, H)
        Y.rect(px_-2, 60, px_+2, 82, None)
        for y in range(62, 83):
            Y.px(px_-2, y, R('gold', 5 if flip > 0 else 4)); Y.px(px_-1, y, R('gold', 4)); Y.px(px_, y, R('blue', 3)); Y.px(px_+1, y, R('gold', 3)); Y.px(px_+2, y, R('gold', 1))
        Y.rect(px_-3, 80, px_+3, 83, R('gold', 3)); Y.hline(px_-3, px_+3, 80, R('gold', 5))
        Y.rect(px_-3, 59, px_+3, 62, R('gold', 4)); Y.hline(px_-3, px_+3, 59, R('gold', 6))
        Y.outline(); L.comp(Y)
        L.comp(CRYS, px_-6, 41)
    return L

# ---------- C: 연구 홀 (blue hall with gold frame, door, roof crystal) ----------
def research_hall(seed=13):
    rnd = random.Random(seed)
    L = Layer(W, H)
    plinth(L, 42, 78, 36, 11, 12, seed=seed)
    T = Layer(W, H)
    x0, x1, yt, yb = 16, 60, 60, 84
    shade_rect(T, x0, yt, x1, yb, 'blue', top=3, bot=2, rnd=rnd, noise=0.08)
    for y in range(yt, yb+1, 8): T.hline(x0, x1, y, R('blue', 1))
    for px_ in (x0, 30, 46, x1-2): gold_post(T, px_, yt, yb, 3)
    # side wall
    T.poly([(x1+1, yt), (x1+9, yt-7), (x1+9, yb-7), (x1+1, yb)], lambda x, y: dither('blue', 1, x, y, rnd, 0.1))
    gold_post(T, x1+7, yt-6, yb-7, 2)
    # roof
    roof = [(x0-3, yt), (x0+7, yt-15), (x1+13, yt-15), (x1+12, yt-7), (x1+3, yt)]
    def rf(x, y):
        t = (y - (yt-15)) / 15
        b = 5 if t < 0.25 else 4 if t < 0.6 else 3
        if (x - y) % 7 == 0: b -= 2
        return R('blue', b)
    T.poly(roof, rf)
    T.hline(x0-3, x1+3, yt, R('gold', 3)); T.hline(x0-3, x1+3, yt-1, R('gold', 5))
    T.hline(x0+7, x1+12, yt-15, R('gold', 5)); T.hline(x0+7, x1+12, yt-14, R('gold', 3))
    for bx in range(x0, x1+4, 8): bolt(T, bx, yt-1)
    T.outline()
    L.comp(T)
    # door
    D = Layer(W, H)
    for y in range(69, 85):
        for x in range(34, 45):
            r = ((x-39)/5.5)**2 + ((y-73)/4.5)**2
            if y < 73 and r > 1: continue
            edge = x in (34, 35, 43, 44) or (y < 73 and r > 0.55)
            D.px(x, y, R('gold', 4 if x < 39 else 3) if edge else (R('cyan', 5) if y < 72 else R('cyan', 3) if y < 78 else R('cyan', 2)))
    D.outline(); L.comp(D)
    # windows
    for wx in (21, 51):
        Wn = Layer(W, H)
        Wn.rect(wx-1, 66, wx+5, 73, R('gold', 3)); Wn.hline(wx-1, wx+5, 66, R('gold', 5))
        Wn.rect(wx, 67, wx+4, 72, R('cyan', 2)); Wn.vline(wx+2, 67, 72, R('gold', 2)); Wn.hline(wx, wx+4, 69, R('gold', 2))
        Wn.px(wx, 67, R('cyan', 5)); Wn.px(wx+3, 67, R('cyan', 4))
        Wn.outline(); L.comp(Wn)
    # roof crystal + gold mount
    M = Layer(W, H)
    M.rect(37, 42, 47, 46, R('gold', 3)); M.hline(37, 47, 42, R('gold', 5)); M.rect(40, 43, 44, 45, R('blue', 2))
    M.outline(); L.comp(M)
    L.comp(CRYS, 36, 24)
    # emblem: small turret silhouette sign above door
    return L

VARIANTS = [('A', '수정 연구탑', lab_tower), ('B', '강화 정비대', scan_bay), ('C', '연구 홀', research_hall)]

if __name__ == '__main__':
    from PIL import Image
    S = 5; pad = 10
    ims = [f().img() for _, _, f in VARIANTS]
    c = Image.new('RGBA', ((W*S+pad)*3, H*S), (58, 66, 58, 255))
    for i, im in enumerate(ims): c.alpha_composite(im.resize((W*S, H*S), Image.NEAREST), (i*(W*S+pad), 0))
    c.save('t_h1.png')
