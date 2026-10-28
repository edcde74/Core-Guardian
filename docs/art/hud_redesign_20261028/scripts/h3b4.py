from h3lib import *
from h3fit import mod_up, mod_down, turret_sym, back_arrow
from h3b2 import helm, pierce_line, spark, cycle_arrows, pig
from h3b3 import big_helm, HELM_BIG, rail_bolt, red_x, range_blast
import math
def new(): return Layer(N, N)
HOOD = (52, 40, 70, 255); HOOD_L = (82, 66, 104, 255); HOOD_D = (30, 22, 42, 255)

def clock_face(L, cx, cy, r, hand_a=-60):
    L.ellipse(cx, cy, r, r, lambda x, y: R('gold', 5 if x+y < cx+cy-1 else 3))
    L.ellipse(cx, cy, r-1.6, r-1.6, (236, 240, 244, 255))
    for a in range(0, 360, 90):
        rad = math.radians(a); L.px(cx+math.cos(rad)*(r-2.5), cy+math.sin(rad)*(r-2.5), R('iron', 2))
    rad = math.radians(hand_a)
    for t in range(int(r-2)): L.px(cx+math.cos(rad)*t, cy+math.sin(rad)*t, OUT)
    L.px(cx, cy, R('red', 3))

def scythe(L, x0, y0, x1, y1, blade_left=True):
    """haft from (x0,y0) bottom to (x1,y1) top, curved blade at top"""
    n = max(abs(x1-x0), abs(y1-y0))
    for k in range(n+1):
        x = x0+(x1-x0)*k/n; y = y0+(y1-y0)*k/n
        L.px(x, y, R('wood', 4)); L.px(x+1, y, R('wood', 2))
    s = -1 if blade_left else 1
    for t in range(11):
        bx = x1 + s*t; by = y1 + (t*t)/14.0
        L.px(bx, by, R('silver', 5)); L.px(bx, by+1, R('silver', 3)); L.px(bx, by+2, R('silver', 1)) if t < 8 else None

def reaper(L, x, y):
    """hooded reaper bust ~14w x 16h, faceless dark hood with two cyan eyes"""
    T = Layer(N, N)
    for j in range(16):
        w = min(7, 2 + j*0.9) if j < 7 else 7 + (j-7)*0.35
        for i in range(int(-w), int(w)+1):
            c = HOOD_L if i < -w+2 else HOOD_D if i > w-2 else HOOD
            T.px(x+7+i, y+j, c)
    T.ellipse(x+7, y+7, 3.6, 3.2, (10, 8, 16, 255))
    T.px(x+6, y+7, R('cyan', 5)); T.px(x+8, y+7, R('cyan', 5))
    T.outline(); L.comp(T)

# 25 처형인 — 사신이 시계를 되감음
def c25A():
    L = new(); clock_face(L, 19, 19, 5, 200); back_arrow(L, 19, 19, 6)
    reaper(L, 3, 9)
    for k in range(20): L.px(10+k*0.3, 27-k, R('wood', 4)); L.px(11+k*0.3, 27-k, R('wood', 2))
    for t in range(10):
        bx = 16 + t; by = 8 + (t*t)/12.0
        L.px(bx, by, R('silver', 5)); L.px(bx, by+1, R('silver', 2))
    return done(L)
def c25B():
    L = new(); clock_face(L, 15, 17, 10, 90)
    for k in range(10): L.px(15+k*0.5, 17-k, R('wood', 4)); L.px(16+k*0.5, 17-k, R('wood', 2)); L.px(14+k*0.5, 17-k, OUT)
    for t in range(12):
        bx = 20 - t; by = 7 + (t*t)/14.0
        L.px(bx, by-1, (240, 244, 248, 255)); L.px(bx, by, R('silver', 4)); L.px(bx, by+1, R('silver', 2)); L.px(bx, by+2, OUT)
    back_arrow(L, 15, 17, 12)
    return done(L)
def c25C():
    L = new(); reaper(L, 9, 3)
    clock_face(L, 16, 22, 6, 200); back_arrow(L, 16, 22, 8)
    for k in range(5): L.px(6+k, 17-k, R('silver', 5)); L.px(6+k, 18-k, R('silver', 2))
    return done(L)

# boss helm: big helm + gold band + horns
def boss_helm(L, x, y):
    big_helm(L, x, y)
    for i in range(1, 16): L.px(x+i, y+3, R('gold', 4))
    for (hx, s) in ((x+1, -1), (x+15, 1)):
        for k in range(4): L.px(hx+s*(k//2), y+2-k, R('silver', 5 if k < 2 else 4)); L.px(hx+s*(k//2)+s, y+2-k, R('silver', 2))
    L.outline() if False else None

# 31 사냥 기록 — 보스 처치마다 기술병 공격력 +5
def c31A():
    L = new(); wrench2(L, 9, 11, 22, 24, 6)
    helm(L, 14, 5)
    for (hx, s) in ((14, -1), (26, 1)):
        for k in range(3): L.px(hx+s*(k//2), 5-k, R('silver', 5))
    return done(L)
def c31B():
    L = new(); boss_helm(L, 4, 11)
    for k in range(4): L.vline(21+k*2, 4, 10, R('gold', 5))
    line(L, 20, 9, 28, 5, R('gold', 4))
    return done(L)
def trophy(L, x, y):
    L.rect(x+2, y, x+12, y+2, R('gold', 5))
    L.ellipse(x+7, y+3, 5, 5, lambda xx, yy: R('gold', 5 if xx < x+7 else 3) if yy >= y+1 else None)
    L.rect(x+6, y+8, x+8, y+11, R('gold', 3)); L.rect(x+3, y+11, x+11, y+13, R('gold', 4))
    L.px(x, y+2, R('gold', 4)); L.px(x+14, y+2, R('gold', 3)); L.px(x+1, y+4, R('gold', 4)); L.px(x+13, y+4, R('gold', 3))
def c31C():
    L = new(); wrench2(L, 9, 14, 21, 26, 5); trophy(L, 14, 4); return done(L)

# 32 수정 광산 — 10라운드 뒤부터 매 라운드 수정 +10
def pickaxe(L, x, y):
    for k in range(14): L.px(x+k, y+k, R('wood', 4)); L.px(x+k+1, y+k, R('wood', 2))
    for t in range(-6, 7):
        L.px(x+4+t, y+4-t+abs(t)*0.25, R('iron', 4)); L.px(x+4+t, y+5-t+abs(t)*0.25, R('iron', 2))
def c32A():
    L = new(); crystal(L, 11, 7, 27, 7); pickaxe(L, 13, 10); return done(L)
def c32B():
    L = new(); crystal(L, 12, 4, 27, 8)
    for (sx, sy) in ((22, 6), (25, 12), (20, 12)):
        for d in (-1, 1): L.px(sx+d, sy, R('gold', 5)); L.px(sx, sy+d, R('gold', 5))
        L.px(sx, sy, (255, 240, 150, 255))
    return done(L)
def c32C():
    L = new()
    L.rect(4, 20, 27, 27, R('stone', 3)); L.hline(4, 27, 20, R('stone', 5))
    for (cx, top, hw) in ((9, 8, 4), (16, 4, 5), (23, 10, 4)): crystal(L, cx, top, 21, hw)
    mod_up(L) if False else None
    return done(L)

# 33 환전소 — 남은 골드 10%를 수정으로
def c33A():
    L = new(); coin(L, 9, 20, 6); crystal(L, 23, 4, 18, 5)
    for k in range(6): L.px(12+k, 12-k*0.6, R('cyan', 4)); L.px(12+k, 13-k*0.6, R('cyan', 2))
    L.px(18, 8, R('cyan', 5)); L.px(17, 7, R('cyan', 5)); L.px(17, 9, R('cyan', 5))
    return done(L)
def c33B():
    L = new()
    L.vline(15, 6, 25, R('gold', 4)); L.vline(16, 6, 25, R('gold', 2)); L.hline(5, 26, 8, R('gold', 4)); L.rect(10, 25, 21, 27, R('gold', 3))
    for (cx, s) in ((7, 0), (24, 0)):
        L.hline(cx-4, cx+4, 16, R('gold', 3)); line(L, cx, 9, cx-4, 15, R('gold', 2)); line(L, cx, 9, cx+4, 15, R('gold', 2))
    coin(L, 7, 13, 3); crystal(L, 24, 9, 15, 3)
    return done(L)
def c33C():
    L = new(); coin(L, 11, 20, 5); crystal(L, 20, 6, 18, 4); cycle_arrows(L, 16, 16, 10, 'gold'); return done(L)

# 34 급속 건설 — 다음 2라운드 설치비 −40%, 그 뒤 +10%
def c34A():
    L = new(); turret_sym(L, 5, 8); arrow(L, 23, 4, 12, 6, 'cyan', up=False, head=4); return done(L)
def c34B():
    L = new(); turret_sym(L, 4, 9); stopwatch(L, 22, 11, 4)
    return done(L)
def c34C():
    L = new(); turret_sym(L, 3, 8); crystal(L, 23, 3, 13, 3); arrow(L, 23, 15, 22, 5, 'cyan', up=False, head=3); return done(L)

# 35 철벽 — 라운드마다 코어 에너지 1 회복
def c35A():
    L = new(); shield(L, 'blue', 'gold', cx=14, top=7, bot=27, hw=9); crystal(L, 14, 11, 23, 3)
    L.rect(22, 4, 24, 12, R('green', 4)); L.rect(19, 7, 27, 9, R('green', 4))
    return done(L)
def c35B():
    L = new(); shield(L, 'blue', 'gold', cx=16, top=6, bot=27, hw=9); crystal(L, 16, 10, 22, 3); cycle_arrows(L, 16, 16, 10, 'green'); return done(L)
def c35C():
    L = new(); shield(L, 'blue', 'gold', cx=13, top=8, bot=27, hw=9); heart(L, 'red', 22, 10, 6); return done(L)

# 36 무피해 보상 — 코어 피해 없는 라운드마다 모든 포탑 피해 +1% 누적
def stars3(L):
    for (sx, sy) in ((21, 6), (25, 10), (19, 11)):
        for d in (-1, 1): L.px(sx+d, sy, R('gold', 5)); L.px(sx, sy+d, R('gold', 5))
        L.px(sx, sy, (255, 240, 150, 255))
def c36A():
    L = new(); shield(L, 'blue', 'gold', cx=12, top=9, bot=27, hw=8); crystal(L, 12, 12, 22, 3); stars3(L); return done(L)
def c36B():
    L = new(); turret_sym(L, 3, 8); stars3(L); return done(L)
def c36C():
    L = new(); shield(L, 'blue', 'gold', cx=13, top=8, bot=27, hw=9)
    for (x, y) in ((8, 17), (9, 18), (10, 19), (11, 18), (12, 17), (13, 16), (14, 15), (15, 14), (16, 13), (17, 12)): L.px(x, y, R('green', 4)); L.px(x, y+1, R('green', 2))
    stars3(L)
    return done(L)

# 37 위기 대응 — 코어 에너지 30% 이하일 때 모든 피해 +30%
def c37A():
    L = new(); crystal(L, 12, 5, 27, 8)
    pts = [(14, 7), (11, 13), (15, 17), (10, 23)]
    for (ax, ay), (bx, by) in zip(pts, pts[1:]): line(L, ax, ay, bx, by, R('red', 3))
    mod_up(L, 'red'); return done(L)
def c37B():
    L = new(); crystal(L, 12, 5, 27, 8)
    for y in range(5, 28):
        for x in range(4, 21):
            c = L.get(x, y)
            if c[3] and int(c[2]) > 150: L.px(x, y, (min(255, int(c[0])+130), int(c[1])//2, int(c[2])//2, 255))
    sword(L, 27, 4, 19, 12)
    return done(L)
def c37C():
    L = new(); L.rect(4, 18, 26, 25, R('iron', 1)); L.hline(4, 26, 18, R('iron', 3))
    L.rect(6, 20, 9, 23, R('red', 4)); mod_up(L, 'red'); crystal(L, 10, 4, 15, 4); return done(L)

# 38 보스 사냥꾼 — 보스에게 주는 피해 +20%
def c38A():
    L = new(); boss_helm(L, 4, 12); mod_up(L, 'red'); return done(L)
def c38B():
    L = new(); boss_helm(L, 4, 13); sword(L, 26, 5, 19, 12); return done(L)
def c38C():
    L = new(); boss_helm(L, 6, 9); crosshair(L, 14, 15, 9, 'red'); return done(L)

# 39 절대 영도 (플래티넘) — 둔화 60% 이상 적이 2초 얼어붙었다 부서지며 주변 피해
def ice_block(L, x0, y0, x1, y1):
    L.rect(x0, y0, x1, y1, (176, 222, 238, 255)); L.hline(x0, x1, y0, (235, 252, 255, 255)); L.vline(x0, y0, y1, (235, 252, 255, 255))
    L.hline(x0, x1, y1, R('cyan', 1)); L.vline(x1, y0, y1, R('cyan', 1))
def c39A():
    L = new(); ice_block(L, 5, 6, 24, 25)
    T = Layer(N, N); big_helm(T, 6, 9, frozen=True); L.comp(T)
    for (x, y) in ((26, 8), (27, 13), (25, 19), (3, 10)): L.rect(x, y, x+1, y+1, (235, 252, 255, 255))
    return done(L)
def c39B():
    L = new(); snowflake(L, 15, 15, 11)
    for (x, y) in ((25, 5), (26, 24), (5, 25), (6, 5)): L.rect(x, y, x+1, y+1, R('cyan', 4))
    return done(L)
def c39C():
    L = new(); big_helm(L, 5, 9, frozen=True)
    for (ax, ay, bx, by) in ((13, 9, 11, 15), (11, 15, 15, 21)): line(L, ax, ay, bx, by, OUT)
    for (x, y) in ((25, 9), (26, 15), (24, 21), (21, 25)): L.rect(x, y, x+2, y+1, (235, 252, 255, 255))
    return done(L)

# 40 업화 (플래티넘) — 화상 최대 스택 적이 죽으면 폭발, 주변에 화상 3스택
def c40A():
    L = new()
    for a in range(0, 360, 6):
        rad = math.radians(a); L.px(16+math.cos(rad)*11, 17+math.sin(rad)*9, (255, 150, 40, 255)); L.px(16+math.cos(rad)*10, 17+math.sin(rad)*8, (226, 88, 30, 255))
    flame(L, 16, 24, 17, 7)
    return done(L)
def c40B():
    L = new(); helm(L, 9, 14); flame(L, 15, 15, 12, 5)
    for (fx, fy) in ((7, 12), (25, 12), (8, 25), (24, 25)): flame(L, fx, fy, 6, 2)
    return done(L)
def c40C():
    L = new()
    for y in range(N):
        for x in range(N):
            dx, dy = x+0.5-16, y+0.5-16; a = math.atan2(dy, dx); d = math.hypot(dx, dy)
            rr = 7 + 5*max(0, math.cos(a*6))
            if d <= rr: L.px(x, y, (255, 236, 150, 255) if d < 4 else (255, 176, 50, 255) if d < 7 else (226, 88, 30, 255))
    return done(L)

CARDS = [
 (25, '처형인', '처형마다 범위 폭발 쿨다운 −1초', 'gold', [(c25A, '사신이 낫을 시계에 걸어 되감음'), (c25B, '낫이 시곗바늘인 시계 + 거꾸로 화살'), (c25C, '사신이 든 되감기는 시계')]),
 (31, '사냥 기록', '보스 처치마다 기술병 공격력 +5', 'gold', [(c31A, '스패너 + 작은 보스 투구'), (c31B, '보스 투구 + 처치 기록 눈금'), (c31C, '스패너 + 트로피')]),
 (32, '수정 광산', '10라운드 뒤부터 매 라운드 수정 +10', 'gold', [(c32A, '수정 + 곡괭이'), (c32B, '수정 + 별(성장)'), (c32C, '돌 위에 자라는 수정 셋')]),
 (33, '환전소', '라운드 종료 시 남은 골드 10% → 수정', 'gold', [(c33A, '금화 → 수정 화살'), (c33B, '금화·수정 저울'), (c33C, '금화 ↔ 수정 순환')]),
 (34, '급속 건설', '다음 2라운드 설치비 −40%, 그 뒤 +10%', 'gold', [(c34A, '포탑 + 아래 화살(비용↓)'), (c34B, '포탑 + 시계(빨리)'), (c34C, '포탑 + 수정 + 아래 화살')]),
 (35, '철벽', '라운드마다 코어 에너지 1 회복', 'gold', [(c35A, '코어 방패 + 녹색 +'), (c35B, '코어 방패 + 녹색 순환'), (c35C, '코어 방패 + 하트')]),
 (36, '무피해 보상', '무피해 라운드마다 모든 포탑 피해 +1% 누적', 'gold', [(c36A, '코어 방패 + 별(성장)'), (c36B, '포탑 + 별(성장)'), (c36C, '체크한 코어 방패 + 별')]),
 (37, '위기 대응', '코어 30% 이하일 때 모든 피해 +30%', 'gold', [(c37A, '금 간 코어 수정 + 붉은 위 화살'), (c37B, '붉게 물든 코어 수정 + 검'), (c37C, '거의 빈 코어 막대 + 붉은 위 화살')]),
 (38, '보스 사냥꾼', '보스에게 주는 피해 +20%', 'gold', [(c38A, '보스 투구(뿔·금 띠) + 붉은 위 화살'), (c38B, '보스 투구 + 검'), (c38C, '보스 투구 + 조준점')]),
 (39, '절대 영도', '둔화 60% 이상 적이 얼어붙었다 부서지며 주변 피해', 'platinum', [(c39A, '얼음 덩어리에 갇힌 투구 + 파편'), (c39B, '큰 눈꽃 + 흩어지는 얼음'), (c39C, '금 가는 얼어붙은 투구 + 파편')]),
 (40, '업화', '화상 최대 스택 적이 죽으면 폭발해 주변 화상 3스택', 'platinum', [(c40A, '주황 고리 폭발 + 불길(E2)'), (c40B, '불붙은 적 + 사방으로 튀는 불'), (c40C, '불꽃 폭발 별')]),
]
