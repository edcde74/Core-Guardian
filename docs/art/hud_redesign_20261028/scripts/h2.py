from parts import *
from h1 import shade_rect, gold_post, bolt, W, H
import random
PLAYER = load(ART+'Characters/Player/Sprites/player.png')

def plinth_gold(L, seed):
    P = plinth(L, 42, 78, 36, 11, 12, seed=seed, gem=False)
    # coin emblem instead of cyan gem
    C = Layer(W, H)
    C.ellipse(42, 96, 4, 4, lambda x, y: R('gold', 5 if x+y < 136 else 3 if x+y < 140 else 2))
    C.rect(41, 94, 43, 98, R('gold', 4)); C.px(41, 94, R('gold', 6)); C.px(42, 95, R('gold', 6))
    C.outline(); L.comp(C)

# ---------- A: 작업대 (workbench + pegboard with tools + gear) ----------
def workbench(seed=21):
    rnd = random.Random(seed)
    L = Layer(W, H); plinth_gold(L, seed)
    # pegboard behind
    B = Layer(W, H)
    shade_rect(B, 18, 44, 66, 70, 'wood', top=3, bot=2, rnd=rnd, noise=0.12)
    for y in range(44, 71, 6): B.hline(18, 66, y, R('wood', 1))
    for x in (18, 19, 65, 66): B.vline(x, 42, 74, R('gold', 4 if x in (18, 65) else 2))
    B.hline(16, 68, 42, R('gold', 5)); B.hline(16, 68, 43, R('gold', 3))
    B.outline(); L.comp(B)
    gear2(L, 47, 56, 8, 8, 'gold')
    gear2(L, 60, 50, 5, 6, 'iron')
    sprite(L, WRENCH_T, 24, 47, 'silver')
    # bench (3/4 box)
    T = Layer(W, H)
    x0, x1, yt = 12, 66, 72
    T.poly([(x0, yt), (x0+4, yt-5), (x1+4, yt-5), (x1, yt)], lambda x, y: R('wood', 5) if y < yt-3 else R('wood', 4))
    shade_rect(T, x0, yt, x1, yt+3, 'wood', top=3, bot=2, rnd=rnd)
    T.poly([(x1+1, yt), (x1+4, yt-5), (x1+4, yt-2), (x1+1, yt+3)], R('wood', 1))
    for lx in (14, 62):
        T.rect(lx, yt+4, lx+3, 85, R('wood', 2)); T.vline(lx, yt+4, 85, R('wood', 3))
    T.hline(14, 65, 81, R('wood', 2))
    T.hline(x0, x1, yt, R('gold', 4))
    T.outline(); L.comp(T)
    coin_stack(L, 52, 68, 3); coin_stack(L, 22, 69, 2)
    return L

# ---------- B: 공방 천막 (brass-frame stall with striped awning, gear sign, toolbox) ----------
def workshop_stall(seed=22):
    rnd = random.Random(seed)
    L = Layer(W, H); plinth_gold(L, seed)
    F = Layer(W, H)
    for px_ in (12, 68): gold_post(F, px_, 44, 84, 3)
    # awning (striped leather/blue cloth)
    aw = [(8, 52), (16, 36), (70, 36), (76, 52)]
    def awf(x, y):
        stripe = ((x - 8) // 6) % 2
        t = (y-36)/16
        b = 4 if t < 0.3 else 3 if t < 0.75 else 2
        return R('cloth' if stripe else 'leather', b)
    F.poly(aw, awf)
    # scalloped edge
    for x in range(8, 77):
        if (x - 8) % 6 in (1, 2, 3, 4): F.px(x, 53, R('leather', 2) if ((x-8)//6) % 2 == 0 else R('cloth', 2))
    F.hline(16, 70, 36, R('gold', 5))
    F.outline(); L.comp(F)
    # back wall inside stall (shadowed planks + hanging tools)
    Bk = Layer(W, H)
    shade_rect(Bk, 15, 50, 67, 70, 'wood', top=1, bot=2, rnd=rnd, noise=0.1)
    for x in range(15, 68, 7): Bk.vline(x, 50, 70, R('wood', 0))
    Bk.hline(15, 67, 56, R('iron', 3))
    L.comp(Bk)
    sprite(L, WRENCH_T, 52, 54, 'silver')
    gear(L, 62, 62, 4, 6, 'gold')
    L.comp(F)
    # toolbox chest
    C = Layer(W, H)
    shade_rect(C, 20, 70, 42, 82, 'red', top=3, bot=2, rnd=rnd, noise=0.08)
    C.poly([(20, 70), (23, 66), (45, 66), (42, 70)], R('red', 4))
    C.poly([(43, 70), (45, 66), (45, 78), (43, 82)], R('red', 1))
    C.hline(20, 42, 74, R('gold', 4)); C.rect(29, 72, 33, 76, R('gold', 4)); C.px(31, 74, R('gold', 1))
    C.hline(27, 35, 64, R('iron', 4)); C.vline(27, 64, 66, R('iron', 3)); C.vline(35, 64, 66, R('iron', 3))
    C.outline(); L.comp(C)
    # anvil
    A = Layer(W, H)
    A.poly([(48, 70), (66, 70), (62, 74), (52, 74)], R('iron', 4))
    A.hline(48, 66, 70, R('iron', 5)); A.poly([(66, 70), (70, 71), (66, 72)], R('iron', 3))
    A.rect(54, 74, 60, 79, R('iron', 2)); A.rect(51, 79, 63, 82, R('iron', 3)); A.hline(51, 63, 79, R('iron', 4))
    A.outline(); L.comp(A)
    # gear sign on top of awning
    gear(L, 42, 30, 8, 8, 'gold')
    S = Layer(W, H)
    S.rect(40, 37, 44, 39, R('iron', 3)); S.hline(40, 44, 37, R('iron', 4)); S.outline()
    L.comp(S); gear(L, 42, 29, 8, 8, 'gold')
    return L

# ---------- C: 증기 공방 (brass boiler + chimney + gauge + gear wheel + anvil) ----------
def steam_forge(seed=23):
    rnd = random.Random(seed)
    L = Layer(W, H); plinth_gold(L, seed)
    T = Layer(W, H)
    # chimney
    T.comp(cylinder(T, 52, 59, 30, 60, 'iron', ry=2, lo=1, hi=4, cap=('iron', 2)))
    band(T, 52, 59, 34, 55.5, 4, 2, 'gold', 1)
    # boiler (brass cylinder)
    T.comp(cylinder(T, 20, 54, 50, 82, 'gold', ry=4, lo=1, hi=3, cap=('gold', 4)))
    band(T, 20, 54, 58, 37, 17.5, 4, 'iron', 2)
    band(T, 20, 54, 72, 37, 17.5, 4, 'iron', 2)
    for x in range(22, 54, 4): T.px(x, 63, R('gold', 6)); T.px(x, 77, R('gold', 6))
    T.outline(); L.comp(T)
    # pressure gauge
    G = Layer(W, H)
    G.ellipse(30, 66, 4.5, 4.5, R('silver', 4)); G.ellipse(30, 66, 3.2, 3.2, R('silver', 5))
    G.px(30, 66, OUT); G.px(31, 65, R('red', 3)); G.px(32, 64, R('red', 3))
    G.outline(); L.comp(G)
    # furnace mouth with glow
    M = Layer(W, H)
    M.rect(37, 72, 47, 82, R('iron', 2)); M.hline(37, 47, 72, R('iron', 4))
    M.rect(39, 75, 45, 82, R('red', 3)); M.rect(40, 77, 44, 82, (255, 170, 60, 255)); M.hline(40, 44, 81, (255, 220, 120, 255))
    M.outline(); L.comp(M)
    # pipe from boiler to the right gear wheel
    P = Layer(W, H)
    P.rect(54, 66, 64, 68, R('gold', 3)); P.hline(54, 64, 66, R('gold', 5))
    P.outline(); L.comp(P)
    gear(L, 68, 70, 7, 8, 'gold')
    # steam puffs
    for (sx, sy, r) in ((56, 24, 3), (60, 19, 2.5), (54, 15, 2)):
        Sp = Layer(W, H); Sp.ellipse(sx, sy, r, r, lambda x, y, sx=sx, sy=sy: (220, 226, 232, 255) if x+y < sx+sy else (170, 178, 188, 255)); Sp.outline(); L.comp(Sp)
    # coins + wrench on the left
    coin_stack(L, 10, 84, 3)
    sprite(L, WRENCH_T, 6, 64, 'silver')
    return L

VARIANTS = [('A', '작업대', workbench), ('B', '공방 천막', workshop_stall), ('C', '증기 공방', steam_forge)]

if __name__ == '__main__':
    from PIL import Image
    S = 5; pad = 10
    ims = [f().img() for _, _, f in VARIANTS]
    c = Image.new('RGBA', ((W*S+pad)*3, H*S), (58, 66, 58, 255))
    for i, im in enumerate(ims): c.alpha_composite(im.resize((W*S, H*S), Image.NEAREST), (i*(W*S+pad), 0))
    c.save('t_h2.png')
