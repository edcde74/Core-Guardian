from h3lib import *
from h3b2 import helm, HELM, pierce_line, spark, bolt_tip, mini_turret, tar, TAR, cycle_arrows
import math
def new(): return Layer(N, N)

# ---- modifiers (top-right, ~10px) ----
def mod_up(L, mat='cyan'): arrow(L, 26, 1, 11, 6, mat, up=True, head=4)
def mod_down(L, mat='red'): arrow(L, 26, 1, 11, 6, mat, up=False, head=4)
def mod_clock(L): stopwatch(L, 25, 7, 5)
def mod_stars(L):
    for (sx, sy) in ((24, 4), (28, 9), (21, 9)):
        for d in (-1, 1): L.px(sx+d, sy, R('gold', 5)); L.px(sx, sy+d, R('gold', 5))
        L.px(sx, sy, (255, 240, 150, 255))

# ---- subjects / tags ----
HELM_BIG = [
 ".....LLLLLLL......",
 "...LLMMMMMMMMD....",
 "..LMMMMMMMMMMMD...",
 ".LMMMMMMMMMMMMMD..",
 ".LMMMMMMMMMMMMMD..",
 ".LKKKKKKKKKKKKMD..",
 ".LMMMMMMKMMMMMMD..",
 ".LMMMMMMKMMMMMMD..",
 ".LMMMMMMKMMMMMDD..",
 ".LMMMMMMMMMMMDD...",
 "RRRRRRRRRRRRRRRSS.",
 ".RRRRRRRRRRRRRSS..",
 "..RRRRRRRRRRRSS...",
]
def big_helm(L, x, y, frozen=False):
    T = Layer(N, N)
    pal = {'L': R('silver', 5), 'M': R('silver', 3), 'D': R('silver', 1), 'K': OUT, 'R': R('red', 3), 'S': R('red', 1)}
    if frozen: pal = {'L': (235, 252, 255, 255), 'M': (176, 222, 238, 255), 'D': (92, 148, 178, 255), 'K': (20, 50, 80, 255), 'R': (156, 96, 112, 255), 'S': (104, 60, 76, 255)}
    for j, row in enumerate(HELM_BIG):
        for i, ch in enumerate(row):
            c = pal.get(ch)
            if c: T.px(x+i, y+j, c)
    T.px(x+4, y+2, (240, 244, 248, 255)); T.px(x+5, y+2, (240, 244, 248, 255)); T.px(x+3, y+3, R('silver', 5) if not frozen else (235, 252, 255, 255))
    if frozen:
        for (ix, iy, h) in ((x+3, y+13, 3), (x+8, y+13, 2), (x+12, y+13, 3)):
            for k in range(h): T.px(ix, iy+k, R('cyan', 5 - k))
        for (ix, iy) in ((x+6, y), (x+10, y+1), (x+13, y+3)): T.px(ix, iy-1, (235, 252, 255, 255))
    T.outline(); L.comp(T)
def frozen_helm(L, x, y):
    T = Layer(N, N)
    for j, row in enumerate(HELM):
        for i, ch in enumerate(row):
            c = {'L': R('cyan', 5), 'M': R('cyan', 4), 'D': R('cyan', 2), 'K': R('cyan', 0), 'R': R('cyan', 3), 'S': R('cyan', 1)}.get(ch)
            if c: T.px(x+i, y+j, c)
    for (ix, iy) in ((x+2, y+11), (x+3, y+12), (x+8, y+11), (x+9, y+12), (x+10, y+11)): T.px(ix, iy, (235, 252, 255, 255))
    T.outline(); L.comp(T)

def rail_bolt(L, x0, y, x1):
    """confirmed rail bolt: cyan light core + 3 gold rings, pointing right"""
    for x in range(x0, x1+1):
        L.px(x, y-1, R('cyan', 4)); L.px(x, y, (240, 255, 255, 255)); L.px(x, y+1, R('cyan', 2))
    for k in range(3):
        rx = x0 + 3 + k*((x1-x0-6)//2)
        L.vline(rx, y-3, y+3, R('gold', 4)); L.vline(rx+1, y-3, y+3, R('gold', 2)); L.px(rx, y-3, R('gold', 6))
    L.px(x1+1, y, R('cyan', 5)); L.px(x1+2, y, R('cyan', 3))

def red_x(L, x0, y0, s):
    slash(L, [(x0, y0), (x0+s, y0+s)]); slash(L, [(x0+s, y0), (x0, y0+s)])

def range_blast(L, cx, cy, r=8):
    for a in range(0, 360, 5):
        rad = math.radians(a)
        L.px(cx+math.cos(rad)*r, cy+5+math.sin(rad)*r*0.4, R('cyan', 4 if 180 < a < 360 else 3))
    wrench2(L, cx-3, cy-6, cx+3, cy+4, 4)

# 21 동상 — 둔화 40% 이상인 적이 받는 피해 +25%
def c21A():
    L = new(); big_helm(L, 1, 13, frozen=True); mod_up(L, 'red'); return done(L)
def c21B():
    L = new(); snowflake(L, 12, 18, 10); mod_up(L, 'red'); return done(L)
def c21C():
    L = new(); big_helm(L, 1, 15); snowflake(L, 23, 8, 7); return done(L)

# 22 불길 발자국 — 기술병이 지나간 경로 칸에 2초간 불길
def c22A():
    L = new(); boot(L, 2, 14)
    for (fx, fy) in ((20, 28), (25, 25), (29, 28)): flame(L, fx, fy, 7, 3)
    return done(L)
def c22B():
    L = new()
    L.rect(2, 20, 29, 28, R('stone', 3)); L.hline(2, 29, 20, R('stone', 5)); L.vline(11, 20, 28, R('stone', 1)); L.vline(20, 20, 28, R('stone', 1))
    for fx in (6, 15, 24): flame(L, fx, 24, 10, 4)
    wrench2(L, 21, 3, 28, 10, 3)
    return done(L)
def c22C():
    L = new(); wrench2(L, 7, 8, 18, 19, 5)
    for (fx, fy) in ((14, 29), (21, 27), (28, 29)): flame(L, fx, fy, 8, 3)
    return done(L)

# 23 레일 반사 — 레일 탄이 사거리 끝에서 한 번 되돌아옴
def c23A():
    L = new(); rail_bolt(L, 2, 22, 22)
    for a in range(-90, 91, 6):
        rad = math.radians(a); L.px(24+math.cos(rad)*6, 15+math.sin(rad)*7, R('cyan', 4)); L.px(23+math.cos(rad)*6, 15+math.sin(rad)*7, R('cyan', 2))
    for k in range(4): L.hline(18-k, 18, 8-k+3, R('cyan', 4)) if False else None
    arrow_l = [(16, 8), (17, 7), (17, 9), (18, 6), (18, 10)]
    for (x, y) in arrow_l: L.px(x, y, R('cyan', 5))
    L.hline(18, 24, 8, R('cyan', 4))
    return done(L)
def c23B():
    L = new(); rail_bolt(L, 1, 10, 18); rail_bolt(L, 1, 22, 18)
    # second bolt flipped (pointing left): draw tip on left
    L.px(0, 22, R('cyan', 5))
    L.vline(27, 4, 28, R('stone', 4)); L.vline(28, 4, 28, R('stone', 2)); L.vline(26, 4, 28, R('stone', 5))
    for (sx, sy) in ((23, 10), (23, 22)): spark(L, sx, sy)
    return done(L)
def c23C():
    L = new(); rail_bolt(L, 3, 22, 21); spark(L, 26, 22)
    for x in range(6, 25, 3): L.px(x, 11, R('cyan', 4)); L.px(x+1, 11, R('cyan', 2))
    for (x, y) in ((6, 10), (6, 12), (5, 11), (7, 9), (7, 13)): L.px(x, y, R('cyan', 5))
    L.vline(26, 12, 19, R('cyan', 3))
    return done(L)

# 24 번개 사슬 — 연쇄 튕김 +1, 감쇠 없음
def c24A():
    L = new(); bolt_zig(L, [(3, 4), (12, 12), (8, 16), (18, 20), (15, 24), (28, 29)])
    for (x, y) in ((3, 4), (12, 13), (28, 28)): L.ellipse(x, y, 2.5, 2.5, R('cyan', 4))
    return done(L)
def c24B():
    L = new(); bolt_zig(L, [(3, 8), (13, 16), (9, 19), (22, 28)]); mod_up(L); return done(L)
def c24C():
    L = new(); helm(L, 0, 19); helm(L, 9, 10); helm(L, 19, 1)
    bolt_zig(L, [(6, 22), (14, 16), (13, 14), (24, 6)])
    return done(L)

# 25 처형인 — 처형할 때마다 범위 폭발 쿨다운 −1초
def c25A():
    L = new(); red_x(L, 2, 11, 17); mod_clock(L); return done(L)
def c25B():
    L = new(); range_blast(L, 12, 18, 10); red_x(L, 20, 2, 9); return done(L)
def c25C():
    L = new(); red_x(L, 3, 11, 16); stopwatch(L, 24, 9, 6)
    for a in range(200, 330, 12):
        rad = math.radians(a); L.px(24+math.cos(rad)*9, 9+math.sin(rad)*9, R('cyan', 4))
    return done(L)

# 26 급소 사냥 — 치명타 배율 +50%, 치명타 시 다음 공격 치명타 확률 +20%
def c26A():
    L = new(); crosshair(L, 12, 18, 10, 'red'); mod_up(L); return done(L)
def c26B():
    L = new(); crosshair(L, 11, 19, 9, 'red'); crosshair(L, 21, 10, 7, 'red'); return done(L)
def c26C():
    L = new(); crosshair(L, 12, 18, 10, 'red')
    L.rect(25, 1, 28, 10, (255, 220, 60, 255)); L.vline(28, 1, 10, (200, 150, 30, 255)); L.rect(25, 12, 28, 14, (255, 220, 60, 255))
    return done(L)

# 27 얼음 감옥 — 둔화된 적의 처형 기준 +5%p (둔화 + 처형)
def c27A():
    L = new(); snowflake(L, 10, 10, 8); red_x(L, 15, 15, 13); return done(L)
def c27B():
    L = new()
    for bx in (6, 13, 20):
        L.rect(bx, 6, bx+3, 26, R('cyan', 3)); L.vline(bx, 6, 26, R('cyan', 5)); L.vline(bx+3, 6, 26, R('cyan', 1))
    L.hline(4, 26, 5, R('cyan', 4)); L.hline(4, 26, 27, R('cyan', 2))
    red_x(L, 18, 16, 11)
    return done(L)
def c27C():
    L = new(); red_x(L, 3, 3, 13); snowflake(L, 21, 21, 8); return done(L)

# 28 번지는 불 — 화상으로 죽은 적의 스택이 가장 가까운 적에게 옮겨붙음 (화상 + 연쇄)
def c28A():
    L = new(); flame(L, 9, 16, 14, 6); bolt_zig(L, [(14, 14), (21, 20), (18, 22), (27, 29)]); return done(L)
def c28B():
    L = new(); flame(L, 8, 28, 14, 6); flame(L, 24, 14, 10, 5)
    for k in range(9): L.px(12+k, 18-k*0.4, R('cyan', 4)) if k % 2 == 0 else None
    arrow_pts = [(21, 13), (20, 12), (20, 14)]
    for (x, y) in arrow_pts: L.px(x, y, R('cyan', 5))
    return done(L)
def c28C():
    L = new(); helm(L, 0, 18); flame(L, 6, 19, 10, 4); bolt_zig(L, [(12, 16), (17, 12), (15, 10), (21, 6)]); helm(L, 18, 9); flame(L, 24, 10, 8, 3); return done(L)

# 29 관통 치명 — 치명타가 나면 관통 감쇠 없음 (관통 + 치명타)
def c29A():
    L = new(); crosshair(L, 10, 10, 8, 'red'); rail_bolt(L, 6, 22, 26); return done(L)
def c29B():
    L = new()
    for k in range(29):
        x = 1+k; y = 29-k*0.9
        L.px(x, y, (255, 255, 255, 255)); L.px(x, y+1, R('cyan', 3)); L.px(x, y-1, R('cyan', 5))
    crosshair(L, 16, 16, 9, 'red')
    return done(L)
def c29C():
    L = new(); helm(L, 1, 15); helm(L, 15, 15); pierce_line(L, 22, 0, 28); crosshair(L, 22, 8, 7, 'red'); return done(L)

# 30 화력 지원 — 기술병 주변 2칸 안의 포탑 공격 속도 +15%
def c30A():
    L = new()
    for a in range(0, 360, 6):
        rad = math.radians(a); L.px(15+math.cos(rad)*13, 18+math.sin(rad)*9, R('cyan', 3))
    mini_turret(L, 3, 10); wrench2(L, 19, 15, 25, 21, 4); mod_clock(L)
    return done(L)
def c30B():
    L = new(); mini_turret(L, 2, 12); wrench2(L, 18, 18, 26, 26, 4); mod_up(L); return done(L)
def c30C():
    L = new(); wrench2(L, 13, 13, 19, 19, 4); mini_turret(L, -1, 13); mini_turret(L, 18, 1); return done(L)

CARDS = [
 (21, '동상', '둔화 40% 이상 적이 받는 피해 +25%', 'gold', [(c21A, '얼어붙은 큰 투구 + 붉은 위 화살(받는 피해↑)'), (c21B, '눈꽃 + 붉은 위 화살'), (c21C, '큰 투구 + 눈꽃')]),
 (22, '불길 발자국', '기술병이 지나간 경로 칸에 2초 불길', 'gold', [(c22A, '장화 + 뒤에 남는 불꽃(E1)'), (c22B, '경로 칸 불꽃 3개 + 작은 스패너'), (c22C, '스패너 + 뒤에 남는 불꽃')]),
 (23, '레일 반사', '레일 탄이 사거리 끝에서 되돌아옴', 'gold', [(c23A, '레일 볼트 + 되돌아오는 화살'), (c23B, '벽에 튕겨 오는 볼트 둘 + 스파크(E4)'), (c23C, '볼트 + 끝 스파크 + 되돌아오는 잔상')]),
 (24, '번개 사슬', '연쇄 튕김 +1, 감쇠 없음', 'gold', [(c24A, '세 번 꺾이는 청록 번개'), (c24B, '청록 번개 + 위 화살'), (c24C, '적 셋을 잇는 번개')]),
 (25, '처형인', '처형마다 범위 폭발 쿨다운 −1초', 'gold', [(c25A, '붉은 X + 시계'), (c25B, '범위 폭발(스패너+고리) + 붉은 X'), (c25C, '붉은 X + 거꾸로 도는 시계')]),
 (26, '급소 사냥', '치명타 배율 +50%, 치명 시 다음 치명 확률 +20%', 'gold', [(c26A, '붉은 조준점 + 위 화살'), (c26B, '이어지는 조준점 둘'), (c26C, '조준점 + 노란 !')]),
 (27, '얼음 감옥', '둔화된 적의 처형 기준 +5%p (둔화+처형)', 'gold', [(c27A, '눈꽃(왼위) + 붉은 X(오른아래)'), (c27B, '얼음 창살 + 붉은 X'), (c27C, '붉은 X(왼위) + 눈꽃(오른아래)')]),
 (28, '번지는 불', '화상으로 죽은 적의 스택이 옆 적에게 (화상+연쇄)', 'gold', [(c28A, '불꽃(왼위) + 청록 번개(오른아래)'), (c28B, '불꽃이 옆 불꽃으로 옮겨붙음'), (c28C, '불붙은 적 → 번개 → 옆 적에 불')]),
 (29, '관통 치명', '치명타가 나면 관통 감쇠 없음 (관통+치명타)', 'gold', [(c29A, '조준점(왼위) + 레일 볼트(오른아래)'), (c29B, '조준점을 꿰뚫는 관통선'), (c29C, '투구 둘 관통 + 조준점')]),
 (30, '화력 지원', '기술병 주변 2칸 포탑 공속 +15%', 'gold', [(c30A, '범위 원 안 포탑 + 스패너 + 시계'), (c30B, '포탑 + 스패너 + 위 화살'), (c30C, '스패너 주변 포탑 둘')]),
]
