from parts import *
from h1 import shade_rect, gold_post, bolt, CRYS
import random
W, H = 84, 108

def base(L, seed): plinth(L, 42, 82, 36, 11, 12, seed=seed)
def keep_comp(L, part, x, y): L.comp(part, x, y, keep=True)

def cradle(L, x0, x1, y):
    Cr = Layer(W, H)
    Cr.rect(x0, y, x1, y+6, R('gold', 3)); Cr.hline(x0, x1, y, R('gold', 5)); Cr.hline(x0, x1, y+6, R('gold', 1))
    Cr.rect(x0+3, y+2, x1-3, y+4, R('blue', 2)); Cr.px(x0+1, y+3, R('gold', 6)); Cr.px(x1-1, y+3, R('gold', 6))
    Cr.outline(); L.comp(Cr)

# ---------- A: 수정 동력 공장 ----------
def crystal_factory(seed=31):
    rnd = random.Random(seed)
    L = Layer(W, H); base(L, seed)
    T = Layer(W, H)
    x0, x1, yt, yb = 12, 54, 64, 88
    shade_rect(T, x0, yt, x1, yb, 'blue', top=3, bot=2, rnd=rnd, noise=0.0)
    for y in range(yt+7, yb, 7): T.hline(x0, x1, y, R('blue', 1))
    for px_ in (x0, 33, x1-2): gold_post(T, px_, yt, yb, 3)
    T.poly([(x1+1, yt), (x1+7, yt-5), (x1+7, yb-5), (x1+1, yb)], R('blue', 1))
    for k in range(3):
        ax = x0-2 + k*15
        T.poly([(ax, yt), (ax+11, yt-11), (ax+15, yt-11), (ax+15, yt)], lambda x, y: R('blue', 4) if y < yt-6 else R('blue', 3))
        T.poly([(ax+11, yt-11), (ax+15, yt-11), (ax+15, yt), (ax+12, yt)], R('cyan', 2))
        T.vline(ax+15, yt-11, yt, R('gold', 3))
    T.hline(x0-2, x1+1, yt, R('gold', 5)); T.hline(x0-2, x1+1, yt+1, R('gold', 3))
    T.outline(); L.comp(T)
    D = Layer(W, H)
    D.rect(18, 73, 29, 88, R('gold', 3)); D.rect(20, 75, 27, 88, R('cyan', 2)); D.rect(20, 75, 27, 78, R('cyan', 4))
    for y in range(79, 88, 3): D.hline(20, 27, y, R('cyan', 1))
    D.outline(); L.comp(D)
    Wn = Layer(W, H)
    Wn.rect(38, 72, 49, 79, R('gold', 3)); Wn.rect(39, 73, 48, 78, R('cyan', 2)); Wn.hline(39, 48, 73, R('cyan', 4)); Wn.vline(43, 73, 78, R('gold', 2))
    Wn.outline(); L.comp(Wn)
    # roof crystal in a cradle + pipe to gears
    Vt = Layer(W, H)
    Vt.rect(55, 50, 62, 64, R('iron', 3)); Vt.vline(55, 50, 64, R('iron', 4)); Vt.vline(62, 50, 64, R('iron', 1)); Vt.hline(55, 62, 50, R('iron', 5))
    for y in (54, 58): Vt.hline(56, 61, y, R('iron', 1))
    Vt.outline(); L.comp(Vt)
    cradle(L, 33, 53, 49)
    keep_comp(L, core_crystal(24), 26, 25)
    pipe(L, [(54, 52), (66, 52), (66, 62)], 'gold', glow=True)
    gear2(L, 66, 72, 9, 9, 'gold')
    gear2(L, 53, 84, 5, 7, 'iron')
    return L

# ---------- B: 수정 엔진 ----------
def crystal_engine(seed=32):
    rnd = random.Random(seed)
    L = Layer(W, H); base(L, seed)
    gear2(L, 19, 62, 12, 10, 'gold')
    gear2(L, 65, 66, 9, 9, 'iron')
    P = Layer(W, H)
    shade_rect(P, 30, 70, 54, 88, 'blue', top=3, bot=1, rnd=rnd, noise=0.0)
    for gx in (30, 52): gold_post(P, gx, 70, 88, 3)
    P.poly([(30, 70), (34, 65), (58, 65), (54, 70)], R('gold', 4)); P.hline(34, 58, 65, R('gold', 6))
    P.poly([(55, 70), (58, 65), (58, 83), (55, 88)], R('blue', 0))
    P.rect(38, 76, 46, 85, R('gold', 3)); P.rect(39, 77, 45, 84, R('cyan', 2)); P.vline(40, 77, 84, R('cyan', 4))
    P.outline(); L.comp(P)
    pipe(L, [(31, 73), (26, 67)], 'silver'); pipe(L, [(54, 73), (59, 69)], 'silver')
    keep_comp(L, core_crystal(34), 26, 31)
    # gold claws
    Cl = Layer(W, H)
    for sx in (-1, 1):
        for t in range(7):
            Cl.px(44+sx*(12-t//3), 67-t, R('gold', 4 if sx < 0 else 3)); Cl.px(44+sx*(13-t//3), 67-t, R('gold', 2))
    Cl.outline(); L.comp(Cl)
    return L

# ---------- C: 포탑 조립 공방 ----------
def assembly_shop(seed=33):
    rnd = random.Random(seed)
    L = Layer(W, H); base(L, seed)
    # back wall with big gear
    G = Layer(W, H)
    shade_rect(G, 34, 52, 76, 76, 'blue', top=2, bot=1, rnd=rnd, noise=0.0)
    for y in range(58, 76, 6): G.hline(34, 76, y, R('blue', 0))
    for gx in (32, 75): gold_post(G, gx, 48, 86, 3)
    G.rect(30, 46, 78, 50, R('gold', 4)); G.hline(30, 78, 46, R('gold', 6)); G.hline(30, 78, 50, R('gold', 2))
    for bx in range(33, 78, 6): bolt(G, bx, 47)
    G.outline(); L.comp(G)
    gear2(L, 64, 61, 9, 9, 'gold')
    # crystal reactor tank left
    Tk = Layer(W, H)
    Tk.comp(cylinder(Tk, 8, 26, 54, 86, 'cyan', ry=3, lo=1, hi=3, cap=('gold', 4)))
    band(Tk, 8, 26, 54, 17, 9.5, 3, 'gold', 2); band(Tk, 8, 26, 82, 17, 9.5, 3, 'gold', 2)
    Tk.outline(); L.comp(Tk)
    keep_comp(L, CRYS, 11, 60)
    Bx = Layer(W, H)
    Bx.rect(26, 63, 31, 76, R('iron', 3)); Bx.vline(26, 63, 76, R('iron', 4)); Bx.vline(31, 63, 76, R('iron', 1))
    Bx.px(28, 67, R('cyan', 4)); Bx.px(29, 70, R('red', 3))
    Bx.outline(); L.comp(Bx)
    pipe(L, [(26, 62), (31, 62)], 'gold', glow=True)
    # turret head on assembly stand
    S = Layer(W, H)
    shade_rect(S, 38, 78, 66, 88, 'iron', top=3, bot=1, rnd=rnd, noise=0.0)
    S.hline(38, 66, 78, R('gold', 5)); S.hline(38, 66, 79, R('gold', 3))
    S.outline(); L.comp(S)
    _h = Layer(54, 36); _h.comp(crop(SINGLE, 0, 0, 51, 33), 1, 1); _h.outline()
    keep_comp(L, _h, 26, 46)
    return L

VARIANTS = [('A', '수정 동력 공장', crystal_factory), ('B', '수정 엔진', crystal_engine), ('C', '포탑 조립 공방', assembly_shop)]
