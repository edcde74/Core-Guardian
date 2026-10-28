from h3lib import *
from h3fit import mod_up, mod_down, turret_sym, back_arrow
from h3b2 import helm, pierce_line, spark, cycle_arrows, card, mini_turret
from h3b3 import big_helm, rail_bolt, red_x, range_blast
from h3b4 import clock_face, boss_helm, pickaxe, scythe
import math
def new(): return Layer(N, N)
BLOOD = [(120, 14, 20, 255), (170, 24, 28, 255), (210, 46, 40, 255)]
STATDMG = load(ART+'UI/Resources/CoreGuardian/Hud/StatDamage.png')

def blood_drips(L, xs, y0):
    for (x, ln) in xs:
        for k in range(ln):
            L.px(x, y0+k, BLOOD[2] if k == 0 else BLOOD[1]); L.px(x+1, y0+k, BLOOD[0])
        L.rect(x, y0+ln, x+1, y0+ln+1, BLOOD[1]); L.px(x, y0+ln, BLOOD[2])

# 25 처형인 — 시계가 거꾸로 돎 + 시계 아래로 피가 흐름
def c25A():
    L = new(); clock_face(L, 15, 12, 7, 200); back_arrow(L, 15, 12, 9)
    blood_drips(L, [(11, 4), (15, 7), (18, 3)], 18)
    return done(L)
def c25B():
    L = new(); clock_face(L, 15, 12, 7, -60)
    for t in range(5): L.px(15-t*0.6, 12-t*0.8, OUT)
    for a in range(160, 20, -12):
        rad = math.radians(-a); L.px(15+math.cos(rad)*9, 12+math.sin(rad)*9, R('red', 3))
    L.px(22, 7, R('red', 4)); L.px(23, 8, R('red', 4)); L.px(21, 8, R('red', 4))
    blood_drips(L, [(10, 4), (13, 7), (17, 5), (19, 2)], 18)
    return done(L)
def c25C():
    L = new(); clock_face(L, 14, 12, 7, 200); back_arrow(L, 14, 12, 9)
    for x in range(9, 20): L.px(x, 19, BLOOD[1])
    blood_drips(L, [(10, 4), (15, 6)], 20)
    slash(L, [(20, 18), (23, 23)])
    return done(L)

# 32 수정 광산 A 수정 — 곡괭이 작게, 각도 조정
def small_pickaxe(L, x, y):
    """handle 45deg down-left from head; head a curved iron bar"""
    for k in range(9): L.px(x-k, y+k, R('wood', 4)); L.px(x-k+1, y+k, R('wood', 2))
    for (dx, dy, c) in ((-4, 1, 4), (-3, 0, 4), (-2, -1, 5), (-1, -1, 5), (0, -1, 4), (1, -1, 4), (2, 0, 3), (3, 1, 3), (4, 2, 2)):
        L.px(x+dx, y+dy, R('silver', c))
def c32A():
    L = new(); crystal(L, 12, 6, 27, 7); small_pickaxe(L, 23, 13); return done(L)

# 37 위기 대응 B 수정 — 칼 대신 공격력 증가 심볼
def red_core(L):
    crystal(L, 12, 5, 27, 8)
    for y in range(5, 28):
        for x in range(3, 22):
            c = L.get(x, y)
            if c[3] and int(c[2]) > 150: L.px(x, y, (min(255, int(c[0])+130), int(c[1])//2, int(c[2])//2, 255))
def c37A():
    L = new(); red_core(L); mod_up(L, 'red'); return done(L)
def mini_swords(L, x, y):
    for k in range(8):
        L.px(x+k, y+k, R('silver', 5)); L.px(x+7-k, y+k, R('silver', 4))
    for (gx, gy) in ((x+5, y+6), (x+2, y+6)): L.rect(gx, gy, gx+1, gy, R('gold', 4))
    L.px(x+8, y+8, R('gold', 4)); L.px(x-1, y+8, R('gold', 4))
def c37B():
    L = new(); red_core(L); mini_swords(L, 18, 4); return done(L)
def c37C():
    L = new(); red_core(L); arrow(L, 22, 4, 12, 5, 'red', up=True, head=4); arrow(L, 22, 13, 21, 5, 'red', up=True, head=4); return done(L)

# 41 일직선 — 레일 관통 감쇠 없음, 관통할 때마다 피해 +10%
def c41A():
    L = new()
    for (hx, hy) in ((3, 11), (14, 11)): helm(L, hx, hy)
    rail_bolt(L, 3, 17, 24)
    return done(L)
def c41B():
    L = new(); rail_bolt(L, 3, 20, 22); mod_up(L); return done(L)
def c41C():
    L = new(); rail_bolt(L, 3, 17, 24)
    for i, x in enumerate((9, 15, 21)):
        for d in range(i+1): L.px(x, 13-d*2, R('red', 4)); L.px(x+1, 13-d*2, R('red', 3))
    return done(L)

# 42 연쇄 폭풍 — 처치하면 남은 피해가 다음 적에게 튕김
def c42A():
    L = new(); helm(L, 4, 15); red_x(L, 7, 16, 7); bolt_zig(L, [(15, 17), (18, 14), (16, 13), (19, 11)]); helm(L, 14, 4)
    return done(L)
def c42B():
    L = new()
    for a in range(0, 330, 10):
        r = 4 + a/40
        rad = math.radians(a); L.px(16+math.cos(rad)*r, 16+math.sin(rad)*r, R('cyan', 5)); L.px(16+math.cos(rad)*(r+1), 16+math.sin(rad)*(r+1), R('cyan', 2))
    return done(L)
def c42C():
    L = new()
    bolt_zig(L, [(5, 5), (13, 13)]); bolt_zig(L, [(13, 13), (25, 9)]); bolt_zig(L, [(13, 13), (20, 25)]); bolt_zig(L, [(13, 13), (6, 24)])
    L.ellipse(13, 13, 3, 3, R('cyan', 5))
    return done(L)

# 43 사신 — 범위 폭발에 처형 적용, 처형 기준 2배
def c43A():
    L = new(); range_blast(L, 13, 16, 10); slash(L, [(18, 5), (21, 10), (24, 14)]); slash(L, [(22, 4), (25, 9)]); return done(L)
def c43B():
    L = new()
    for a in range(0, 360, 5):
        rad = math.radians(a); L.px(16+math.cos(rad)*11, 22+math.sin(rad)*4, R('cyan', 4 if 180 < a < 360 else 3))
    scythe(L, 12, 26, 15, 6, blade_left=False)
    return done(L)
def c43C():
    L = new(); range_blast(L, 12, 17, 9); red_x(L, 18, 4, 8); L.px(24, 15, R('red', 4)); L.rect(20, 14, 26, 15, R('red', 3)) if False else None
    return done(L)

# 44 확정 타격 — 모든 공격의 4번째는 반드시 치명타
def c44A():
    L = new(); crosshair(L, 15, 13, 8, 'red')
    for i, x in enumerate((7, 12, 17, 22)):
        c = (255, 220, 60, 255) if i == 3 else R('silver', 4)
        L.rect(x, 24, x+2, 26, c)
    return done(L)
def c44B():
    L = new()
    for i, bx in enumerate((4, 10, 16)): bullet(L, bx, 9, 14, 'cyan')
    bullet(L, 22, 9, 14, 'red')
    L.rect(23, 4, 24, 6, (255, 220, 60, 255))
    return done(L)
def c44C():
    L = new(); crosshair(L, 13, 17, 8, 'red')
    for (x, y, i) in ((20, 4, 0), (24, 4, 1), (20, 8, 2), (24, 8, 3)):
        L.rect(x, y, x+2, y+2, (255, 220, 60, 255) if i == 3 else R('silver', 4))
    return done(L)

# 45 포탑 합체 — 같은 종류 포탑 2기를 붙여 지으면 합체(★)
def star(L, cx, cy):
    for (dx, dy) in ((0, -2), (-1, -1), (0, -1), (1, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (-1, 1), (1, 1), (-2, 2), (2, 2)):
        L.px(cx+dx, cy+dy, R('gold', 6) if dy < 0 else R('gold', 4))
def c45A():
    L = new(); mini_turret(L, 4, 10); mini_turret(L, 13, 10); star(L, 11, 6); star(L, 17, 6); return done(L)
def c45B():
    L = new(); turret_sym(L, 6, 4)
    for sx in (11, 16, 21): star(L, sx, 25)
    return done(L)
def c45C():
    L = new()
    for bx in (4, 18):
        L.rect(bx, 16, bx+8, 24, R('stone', 4)); L.hline(bx, bx+8, 16, R('stone', 6)); L.rect(bx+2, 12, bx+6, 16, R('blue', 3)); L.hline(bx+2, bx+6, 12, R('gold', 5))
    L.hline(13, 17, 19, R('cyan', 5)); L.hline(13, 17, 20, R('cyan', 3))
    star(L, 13, 6); star(L, 19, 6)
    return done(L)

# 46 순간 이동 — 범위 폭발 뒤 3칸 순간 이동
def particles(L, cx, cy, n=7, seed=1):
    import random
    r = random.Random(seed)
    for _ in range(n):
        x = cx + r.randint(-4, 4); y = cy + r.randint(-4, 4)
        L.rect(x, y, x+1, y+1, R('cyan', r.choice((3, 4, 5))))
def c46A():
    L = new(); particles(L, 7, 20, 8); boot(L, 15, 8)
    for x in range(8, 22, 3): L.px(x, 25, R('cyan', 4))
    return done(L)
def c46B():
    L = new()
    for i, x in enumerate((5, 12, 19)):
        L.rect(x, 16, x+5, 21, R('stone', 4 if i < 2 else 5)); L.hline(x, x+5, 16, R('stone', 6))
    particles(L, 7, 9, 5, 2); arrow(L, 22, 4, 13, 5, 'cyan', up=False, head=4) if False else None
    for x in range(8, 24, 3): L.px(x, 13, R('cyan', 4))
    L.px(24, 12, R('cyan', 5)); L.px(24, 14, R('cyan', 5)); L.px(25, 13, R('cyan', 5))
    return done(L)
def c46C():
    L = new(); wrench2(L, 18, 10, 25, 17, 4); particles(L, 8, 18, 9, 3)
    return done(L)

# 47 넓은 선택 — 이후 증강 선택마다 카드 4장
def c47A():
    L = new()
    for i, (x, y) in enumerate(((3, 11), (8, 8), (13, 6), (18, 8))): card(L, x, y, 10, 15)
    return done(L)
def c47B():
    L = new()
    for i, x in enumerate((3, 9, 15)): card(L, x, 10, 9, 14)
    card(L, 18, 10, 9, 14); mod_up(L)
    return done(L)
def c47C():
    L = new()
    for i, x in enumerate((3, 9, 15, 21)): card(L, x, 9, 6, 13)
    return done(L)

# 48 황금 거래 — 코어 최대 −5, 다음 증강은 반드시 플래티넘
def plat_card(L, x, y, w=12, h=17):
    L.rect(x, y, x+w, y+h, R('silver', 5)); L.rect(x+1, y+1, x+w-1, y+h-1, R('blue', 1)); L.rect(x+3, y+4, x+w-3, y+h-4, R('cyan', 4))
    L.hline(x, x+w, y, (255, 255, 255, 255)); L.px(x+w//2, y+h//2, (255, 255, 255, 255))
def c48A():
    L = new(); plat_card(L, 4, 6, 13, 19); crystal(L, 23, 13, 26, 4); line(L, 23, 14, 21, 19, R('red', 3)); line(L, 21, 19, 24, 23, R('red', 3)); return done(L)
def c48B():
    L = new(); plat_card(L, 4, 6, 13, 19); arrow(L, 23, 12, 24, 5, 'red', up=False, head=4); return done(L)
def c48C():
    L = new()
    L.vline(15, 6, 25, R('gold', 4)); L.vline(16, 6, 25, R('gold', 2)); L.hline(4, 27, 9, R('gold', 4)); L.rect(10, 25, 21, 27, R('gold', 3))
    plat_card(L, 4, 11, 7, 10); crystal(L, 23, 12, 22, 3)
    return done(L)

# 49 약점 공략 — 약점 ×1.6, 저항 ×0.85
def c49A():
    L = new(); arrow(L, 12, 4, 27, 10, 'red', up=True, head=9); mod_up(L, 'red'); return done(L)
def c49B():
    L = new(); crosshair(L, 13, 16, 9, 'red'); arrow(L, 13, 9, 22, 6, 'red', up=True, head=5); return done(L)
def c49C():
    L = new(); shield(L, 'silver', 'gold', cx=13, top=7, bot=27, hw=9, crack=True); mod_up(L, 'red'); return done(L)

# 50 유리 대포 — 코어 최대 에너지 절반, 모든 피해 +40%
def c50A():
    L = new(); turret_sym(L, 3, 8)
    for y in range(8, 28):
        for x in range(3, 25):
            c = L.get(x, y)
            if c[3] and not (c[0] < 20 and c[1] < 20): L.px(x, y, (min(255, int(c[0])//3+150), min(255, int(c[1])//3+170), min(255, int(c[2])//3+180), 255))
    mod_up(L, 'red'); return done(L)
def c50B():
    L = new(); crystal(L, 11, 5, 27, 7)
    for x in range(4, 19):
        for y in range(5, 28):
            c = L.get(x, y)
            if c[3] and x > 11: L.px(x, y, R('iron', 1))
    mod_up(L, 'red'); return done(L)
def c50C():
    L = new(); bullet(L, 5, 6, 20, 'cyan')
    for (ax, ay, bx, by) in ((16, 10, 20, 15), (20, 15, 17, 20), (20, 15, 25, 18), (20, 15, 24, 11)): line(L, ax, ay, bx, by, (220, 240, 250, 255))
    mod_up(L, 'red'); return done(L)

FIX = [
 (25, '처형인', '처형마다 범위 폭발 쿨다운 −1초', 'gold', [(c25A, '거꾸로 도는 시계 + 아래로 흐르는 피'), (c25B, '바늘이 거꾸로 + 붉은 거꾸로 화살 + 피'), (c25C, '거꾸로 도는 시계 + 고인 피 + 작은 칼자국')], 'A', '메모 반영: 시계가 반대로 돎 + 시계 아래로 피가 흐름'),
 (32, '수정 광산', '10라운드 뒤부터 매 라운드 수정 +10', 'gold', [(c32A, 'A안 수정: 작은 곡괭이, 각도 조정')], 'A', '메모 반영: 곡괭이 작게, 각도 수정'),
 (37, '위기 대응', '코어 30% 이하일 때 모든 피해 +30%', 'gold', [(c37A, '붉은 코어 + 붉은 위 화살(규칙: 피해 증가)'), (c37B, '붉은 코어 + 작은 교차한 검(공격력 심볼)'), (c37C, '붉은 코어 + 붉은 위 화살 둘')], 'A', '메모 반영: 칼 대신 공격력 증가 심볼'),
]
CARDS = [
 (41, '일직선', '레일 관통 감쇠 없음, 관통마다 피해 +10%', 'platinum', [(c41A, '레일 볼트가 투구 둘 관통'), (c41B, '레일 볼트 + 위 화살'), (c41C, '레일 볼트 + 갈수록 커지는 피해 눈금')]),
 (42, '연쇄 폭풍', '처치하면 남은 피해가 다음 적에게 튕김', 'platinum', [(c42A, '처치된 적(X) → 번개 → 다음'), (c42B, '청록 번개 소용돌이'), (c42C, '사방으로 튀는 번개')]),
 (43, '사신', '범위 폭발에 처형 적용, 처형 기준 2배', 'platinum', [(c43A, '범위 폭발 + 처형 칼자국 2연타'), (c43B, '충격파 고리 + 낫'), (c43C, '범위 폭발 + 붉은 X')]),
 (44, '확정 타격', '모든 공격의 4번째는 반드시 치명타', 'platinum', [(c44A, '조준점 + 칸 4개(4번째 노랑)'), (c44B, '탄 4발(4번째 붉은 탄)'), (c44C, '조준점 + 2×2 칸(4번째 노랑)')]),
 (45, '포탑 합체', '같은 종류 포탑 2기를 붙여 지으면 합체', 'platinum', [(c45A, '포탑 둘 + 금 별 둘(★2)'), (c45B, '포탑 + 받침 아래 금 별 셋(A4)'), (c45C, '받침 둘 + 청록 연결선 + 별(G3)')]),
 (46, '순간 이동', '범위 폭발 뒤 3칸 순간 이동', 'platinum', [(c46A, '청록 입자(C2) + 장화'), (c46B, '3칸 점선 이동 + 입자'), (c46C, '스패너 + 흩어지는 청록 입자')]),
 (47, '넓은 선택', '이후 증강 선택마다 카드 4장', 'platinum', [(c47A, '부채꼴 카드 4장'), (c47B, '카드 4장 + 위 화살'), (c47C, '나란한 카드 4장')]),
 (48, '황금 거래', '코어 최대 −5, 다음 증강은 반드시 플래티넘', 'platinum', [(c48A, '플래티넘 카드 + 금 간 코어'), (c48B, '플래티넘 카드 + 붉은 아래 화살'), (c48C, '플래티넘 카드 ↔ 코어 저울')]),
 (49, '약점 공략', '약점 ×1.6, 저항 ×0.85', 'platinum', [(c49A, '큰 약점 화살(H5 약점 B) + 위 화살'), (c49B, '조준점 + 약점 화살'), (c49C, '금 간 방패 + 붉은 위 화살')]),
 (50, '유리 대포', '코어 최대 에너지 절반, 모든 피해 +40%', 'platinum', [(c50A, '유리처럼 투명한 포탑 + 붉은 위 화살'), (c50B, '절반이 꺼진 코어 수정 + 붉은 위 화살'), (c50C, '탄 + 깨진 유리 + 붉은 위 화살')]),
]
