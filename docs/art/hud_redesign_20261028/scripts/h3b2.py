from h3lib import *
import h3b1
ATT = load('refs/Attack.png')
CHG = load('refs/Charger.png')
TOWER_ID = load(ART+'UI/Resources/CoreGuardian/Hud/TowerIdentity.png')
REROLL = load(ART+'UI/Resources/CoreGuardian/Hud/Reroll.png')
def new(): return Layer(N, N)

HELM = [
 "....LLLLL....",
 "..LLMMMMMMD..",
 ".LMMMMMMMMMD.",
 ".LMMMMMMMMMD.",
 ".LKKKKKKKKMD.",
 ".LMMMMKMMMMD.",
 ".LMMMMKMMMMD.",
 ".LMMMMMMMMDD.",
 "RRRRRRRRRRRSS",
 ".RRRRRRRRRSS.",
 "..RRRRRRRSS..",
]
def helm(L, x, y):
    T = Layer(N, N)
    for j, row in enumerate(HELM):
        for i, ch in enumerate(row):
            c = {'L': R('silver', 5), 'M': R('silver', 3), 'D': R('silver', 1), 'K': OUT, 'R': R('red', 3), 'S': R('red', 1)}.get(ch)
            if c: T.px(x+i, y+j, c)
    T.px(x+3, y+2, R('silver', 5)); T.px(x+4, y+2, (240, 242, 246, 255))
    T.outline(); L.comp(T)
def head_unused(x0=16, y0=11, w=20, h=17):
    """charger head + shoulders (original pixels), outlined"""
    H = Layer(w+2, h+2); H.comp(crop(CHG, x0, y0, x0+w-1, y0+h-1), 1, 1); H.outline(); return H

def pierce_line(L, y, x0, x1, mat='cyan'):
    for x in range(x0, x1+1):
        L.px(x, y-1, R(mat, 5)); L.px(x, y, (255, 255, 255, 255)); L.px(x, y+1, R(mat, 3))

def spark(L, cx, cy, mat='cyan'):
    for (dx, dy, c) in ((0, -3, 5), (0, -2, 4), (0, 2, 4), (0, 3, 3), (-2, -2, 4), (2, 2, 3), (2, -2, 4), (-2, 2, 3)):
        L.px(cx+dx, cy+dy, R(mat, c))

def bolt_tip(L, x, y):
    L.rect(x, y-1, x+3, y+1, R('gold', 4)); L.hline(x, x+3, y-1, R('gold', 6)); L.px(x+4, y, R('gold', 3)); L.px(x+5, y, R('gold', 2))

# 5 꿰뚫는 탄 (다시 그림) — 두 적(창병 투구 원본)을 일직선으로 꿰뚫음
def c5A():
    L = new(); helm(L, 1, 10); helm(L, 15, 10)
    pierce_line(L, 17, 0, 26); bolt_tip(L, 26, 17); return done(L)
def c5B():
    L = new(); helm(L, 1, 15); helm(L, 15, 6)
    for k in range(29):
        x = 1+k; y = 29-k*0.62
        L.px(x, y, (255, 255, 255, 255)); L.px(x, y+1, R('cyan', 3)); L.px(x, y-1, R('cyan', 5))
    spark(L, 8, 25); spark(L, 21, 17); return done(L)
def c5C():
    L = new(); helm(L, 0, 9); helm(L, 14, 9)
    pierce_line(L, 16, 0, 25); bolt_tip(L, 25, 16)
    spark(L, 6, 16); spark(L, 20, 16)
    return done(L)

# 9 성장통 C 수정 — 별을 안쪽으로
def c9C():
    L = new(); wrench2(L, 10, 13, 23, 26, 6)
    for (sx, sy) in ((19, 8), (24, 13), (15, 4)):
        L.px(sx, sy, (255, 240, 150, 255)); L.px(sx-1, sy, R('gold', 5)); L.px(sx+1, sy, R('gold', 5)); L.px(sx, sy-1, R('gold', 5)); L.px(sx, sy+1, R('gold', 5))
    return done(L)

# 11 저금통 — 100마리마다 처치 골드 +2% 누적
def pig(L, x, y):
    L.ellipse(x+10, y+8, 10, 7, lambda xx, yy: R('gold', 5 if xx+yy < x+y+14 else 4 if xx+yy < x+y+22 else 2))
    L.rect(x+3, y+13, x+5, y+16, R('gold', 2)); L.rect(x+14, y+13, x+16, y+16, R('gold', 2))
    L.ellipse(x+20, y+8, 2.5, 3, R('gold', 3)); L.px(x+20, y+7, OUT); L.px(x+20, y+9, OUT)
    L.px(x+5, y+1, R('gold', 4)); L.px(x+6, y, R('gold', 4)); L.hline(x+8, x+12, y+1, OUT)
    L.px(x+15, y+5, OUT)
def c11A():
    L = new(); pig(L, 3, 10); coin(L, 13, 5, 4); return done(L)
def c11B():
    L = new()
    L.rect(7, 9, 24, 28, (150, 200, 220, 255)); L.rect(8, 10, 23, 27, (190, 226, 238, 255)); L.rect(9, 7, 22, 9, R('wood', 3))
    for i, y in enumerate((24, 21, 18, 15)): L.hline(10, 21, y, R('gold', 5 if i % 2 else 4)); L.hline(10, 21, y+1, R('gold', 2))
    L.vline(9, 11, 26, (240, 250, 255, 255))
    return done(L)
def c11C():
    L = new(); coin(L, 11, 20, 8); sprout(L, 23, 12); L.rect(19, 21, 28, 24, R('wood', 3)); return done(L)

# 12 수정 조각 — 다음 3라운드 수정 ×2, 그 뒤 −5
def c12A():
    L = new(); crystal(L, 11, 3, 25, 6); crystal(L, 22, 9, 28, 5); return done(L)
def c12B():
    L = new(); crystal(L, 12, 3, 28, 8)
    arrow(L, 25, 3, 13, 5, 'cyan', up=True, head=4); arrow(L, 25, 18, 28, 5, 'red', up=False, head=4)
    return done(L)
def c12C():
    L = new(); crystal(L, 15, 2, 29, 9)
    for (ax, ay, bx, by) in ((15, 2, 13, 12), (13, 12, 17, 18)):
        line(L, ax, ay, bx, by, R('cyan', 0))
    for (sx, sy) in ((26, 6), (4, 20)): L.px(sx, sy, (255, 255, 255, 255)); L.px(sx-1, sy, R('cyan', 4)); L.px(sx+1, sy, R('cyan', 4)); L.px(sx, sy-1, R('cyan', 4)); L.px(sx, sy+1, R('cyan', 4))
    return done(L)

# 13 재활용 — 철거 환급 50% → 75%
def cycle_arrows(L, cx, cy, r, mat='cyan'):
    for a in range(20, 160, 4):
        rad = math.radians(a+180); L.px(cx+math.cos(rad)*r, cy+math.sin(rad)*r, R(mat, 4)); L.px(cx+math.cos(rad)*(r-1), cy+math.sin(rad)*(r-1), R(mat, 3))
    for a in range(200, 340, 4):
        rad = math.radians(a+180); L.px(cx+math.cos(rad)*r, cy+math.sin(rad)*r, R(mat, 4)); L.px(cx+math.cos(rad)*(r-1), cy+math.sin(rad)*(r-1), R(mat, 3))
    for (ex, ey, s) in ((cx+r*0.94, cy-r*0.34, -1), (cx-r*0.94, cy+r*0.34, 1)):
        for k in range(4): L.hline(int(ex-k), int(ex+k), int(ey+s*(-k+3)), R(mat, 4))
def c13A():
    L = new(); cycle_arrows(L, 16, 16, 12); crystal(L, 16, 9, 23, 4); return done(L)
def c13B():
    L = new(); L.comp(TOWER_ID, -3, 4, keep=True); arrow(L, 25, 3, 16, 6, 'cyan', up=True, head=4); return done(L)
def c13C():
    L = new(); cycle_arrows(L, 16, 16, 12, 'gold'); wrench2(L, 12, 12, 20, 20, 4); return done(L)

# 14 비상 장벽 — 코어 최대 에너지 +5
def c14A():
    L = new(); shield(L, 'blue', 'gold', cx=16, top=3, bot=29, hw=12); crystal(L, 16, 8, 23, 4); return done(L)
def c14B():
    L = new(); crystal(L, 16, 2, 17, 6)
    for row in range(3):
        y0 = 18 + row*4; off = 0 if row % 2 == 0 else 4
        for bx in range(-4, 30, 8):
            x0 = max(3, bx+off); x1 = min(28, bx+off+6)
            if x1 <= x0: continue
            L.rect(x0, y0, x1, y0+2, R('stone', 4)); L.hline(x0, x1, y0, R('stone', 6))
    return done(L)
def c14C():
    L = new(); crystal(L, 12, 3, 28, 8)
    L.rect(22, 6, 24, 16, R('green', 4)); L.rect(18, 10, 28, 12, R('green', 4)); L.px(22, 6, R('green', 4))
    return done(L)

# 15 끈끈이 바닥 — 경로 첫 5칸 이동속도 −10%
TAR = [(40, 18, 46, 255), (58, 30, 68, 255), (80, 46, 94, 255), (120, 80, 140, 255)]
def tar(L, cx, cy, rx, ry):
    L.ellipse(cx, cy, rx, ry, TAR[1]); L.ellipse(cx-1, cy-1, rx-2, ry-1.5, TAR[2])
    for (dx, dy) in ((-rx*0.4, -1), (rx*0.3, 0)): L.px(cx+dx, cy+dy, TAR[3])
def c15A():
    L = new(); tar(L, 16, 22, 13, 6)
    for (bx, by, r) in ((10, 17, 2.5), (20, 15, 3), (15, 11, 2)): L.ellipse(bx, by, r, r, TAR[2]); L.px(bx-1, by-1, TAR[3])
    return done(L)
def c15B():
    L = new(); tar(L, 16, 23, 13, 6); chevrons_down(L, 16, 4, 2); return done(L)
def c15C():
    L = new(); tar(L, 16, 24, 13, 5); boot(L, 10, 9)
    for x in range(9, 23): L.px(x, 20, TAR[2])
    for (x, y) in ((11, 21), (15, 22), (20, 21)): L.px(x, y, TAR[1])
    return done(L)

# 16 촘촘한 진지 — 옆에 포탑이 있으면 공속 +8%
def mini_tower(L, x, y):
    L.comp(TOWER_ID[2:30, 2:30] if False else TOWER_ID, x, y)
def mini_turret(L, x, y):
    T = Layer(N, N)
    T.rect(x, y+9, x+11, y+16, R('stone', 4)); T.hline(x, x+11, y+9, R('stone', 6)); T.vline(x+11, y+9, y+16, R('stone', 2))
    T.vline(x, y+9, y+16, R('gold', 4)); T.vline(x+11, y+9, y+16, R('gold', 3)); T.hline(x, x+11, y+16, R('gold', 2))
    T.rect(x+2, y+4, x+9, y+9, R('blue', 3)); T.hline(x+2, x+9, y+4, R('blue', 5)); T.vline(x+9, y+4, y+9, R('blue', 1))
    T.px(x+5, y+6, R('cyan', 4)); T.px(x+6, y+6, R('cyan', 3))
    for k in range(6): T.px(x+8+k, y+4-k*0.6, R('blue', 4)); T.px(x+8+k, y+5-k*0.6, R('blue', 1))
    T.outline(); L.comp(T)
def c16A():
    L = new(); mini_turret(L, 1, 8); mini_turret(L, 15, 8); return done(L)
def c16B():
    L = new()
    for bx in (4, 18):
        L.rect(bx, 16, bx+9, 25, R('stone', 4)); L.hline(bx, bx+9, 16, R('stone', 6)); L.rect(bx+2, 12, bx+7, 16, R('blue', 3)); L.hline(bx+2, bx+7, 12, R('gold', 5))
    L.hline(13, 18, 20, R('cyan', 5)); L.hline(13, 18, 21, R('cyan', 3))
    for (sx, sy) in ((16, 6),): L.rect(15, 4, 16, 8, R('gold', 5))
    return done(L)
def c16C():
    L = new(); mini_turret(L, 1, 4); mini_turret(L, 1, 14) if False else None; stopwatch(L, 22, 20, 7); mini_turret(L, 8, 12) if False else None; return done(L)

# 17 한 번 더 — 남은 증강 선택마다 리롤 +1
def c17A():
    L = new(); cycle_arrows(L, 16, 16, 11, 'gold'); cycle_arrows(L, 16, 16, 10, 'gold'); return done(L)
def card(L, x, y, w=12, h=17, tint='gold'):
    L.rect(x, y, x+w, y+h, R(tint, 3)); L.rect(x+1, y+1, x+w-1, y+h-1, R('blue', 1)); L.rect(x+3, y+4, x+w-3, y+h-4, R('cyan', 2))
    L.hline(x, x+w, y, R(tint, 5))
def c17B():
    L = new(); card(L, 3, 9); card(L, 9, 6); card(L, 15, 3)
    cycle_arrows(L, 23, 22, 6, 'gold')
    return done(L)
def c17C():
    L = new(); card(L, 9, 6, 14, 20); 
    L.rect(14, 12, 18, 13, R('gold', 5)); L.rect(17, 13, 18, 16, R('gold', 5)); L.rect(15, 16, 17, 17, R('gold', 5)); L.rect(15, 20, 16, 21, R('gold', 5))
    return done(L)

# 18 빚내기 — 즉시 골드 150, 다음 3라운드 처치 골드 −20%
def c18A():
    L = new()
    for (cx, cy) in ((9, 22), (18, 23), (13, 15)): coin(L, cx, cy, 6)
    arrow(L, 26, 12, 28, 5, 'red', up=False, head=4)
    return done(L)
def c18B():
    L = new(); poster(L, 5, 3, 17, 23)
    for y in (10, 13, 16): L.hline(8, 19, y, R('wood', 2))
    L.rect(15, 20, 20, 23, R('red', 3))
    coin(L, 24, 23, 6)
    return done(L)
def c18C():
    L = new(); coin(L, 13, 15, 10)
    for k in range(11): L.px(20-k//2, 5+k*2, OUT); L.px(21-k//2, 5+k*2, R('gold', 1))
    return done(L)

# 19 정비 완료 (골드) — 모든 포탑 피해 +20%
def c19A():
    L = new(); T = Layer(N, N); T.comp(TOWER_ID, -6, 3, keep=True); L.comp(T); wrench2(L, 20, 6, 28, 14, 4)
    for (x, y) in ((19, 22), (20, 23), (21, 24), (22, 23), (23, 22), (24, 21), (25, 20), (26, 19)): L.px(x, y, R('green', 4)); L.px(x, y+1, R('green', 2))
    return done(L)
def c19B():
    L = new()
    L.rect(3, 17, 26, 28, R('wood', 3)); L.hline(3, 26, 17, R('wood', 5)); L.hline(3, 26, 22, R('wood', 2))
    L.rect(12, 20, 17, 25, R('gold', 4))
    for i, bx in enumerate((5, 11, 17)): bullet(L, bx, 5+(i % 2), 13)
    arrow(L, 27, 2, 12, 5, 'cyan', up=True, head=4)
    return done(L)
def c19C():
    L = new(); T = Layer(N, N); T.comp(TOWER_ID, -2, 2, keep=True); L.comp(T)
    for (sx, sy) in ((26, 5), (6, 6)):
        for d in range(-2, 3): L.px(sx+d, sy, R('gold', 5)); L.px(sx, sy+d, R('gold', 5))
        L.px(sx, sy, (255, 250, 210, 255))
    return done(L)

# 20 전투 본능 (골드) — 기술병 공격력 +5, 사거리 +0.5
def c20A():
    L = new(); L.comp(HELMET, 0, 4)
    for (x, y) in ((3, 6), (27, 6), (2, 14), (28, 14)): L.vline(x, y, y+4, R('red', 4))
    return done(L)
def c20B():
    L = new(); wrench2(L, 9, 9, 18, 18, 5)
    for a in range(-40, 41, 5):
        rad = math.radians(a+45); L.px(9+math.cos(rad)*19, 9+math.sin(rad)*19, R('cyan', 4))
    L.px(26, 23, R('cyan', 5)); L.rect(24, 24, 26, 26, R('cyan', 4))
    return done(L)
def c20C():
    L = new(); crosshair(L, 16, 16, 11, 'red'); wrench2(L, 13, 13, 19, 19, 4); return done(L)

FIX = [
 (5, '꿰뚫는 탄', '기술병 투사체가 1명 관통', 'silver', [(c5A, '적 투구 둘을 일직선 관통'), (c5B, '비스듬히 둘을 관통 + 명중 섬광'), (c5C, '둘 관통 + 명중 섬광 + 금속 볼트')]),
 (9, '성장통', '기술병 처치마다 공격력 +0.05', 'silver', [(c9C, 'C안 수정: 별을 안쪽으로')]),
]
CARDS = [
 (11, '저금통', '100마리마다 처치 골드 +2% (누적, 상한 30%)', 'silver', [(c11A, '금 돼지 저금통'), (c11B, '금화가 쌓이는 유리병'), (c11C, '금화 + 새싹')]),
 (12, '수정 조각', '다음 3라운드 수정 ×2, 그 뒤 −5', 'silver', [(c12A, '수정 조각 둘'), (c12B, '수정 + 위·아래 화살'), (c12C, '금 간 수정 + 반짝임')]),
 (13, '재활용', '포탑 철거 환급 50% → 75%', 'silver', [(c13A, '순환 화살 + 수정'), (c13B, '포탑 + 돌아오는 화살'), (c13C, '금 순환 화살 + 스패너')]),
 (14, '비상 장벽', '코어 최대 에너지 +5', 'silver', [(c14A, '파랑 방패 + 코어 수정'), (c14B, '수정 + 돌벽'), (c14C, '수정 + 녹색 +')]),
 (15, '끈끈이 바닥', '경로 첫 5칸 이동속도 −10%', 'silver', [(c15A, '부글거리는 검보라 타르'), (c15B, '타르 + 둔화 표식'), (c15C, '타르에 빠진 장화')]),
 (16, '촘촘한 진지', '바로 옆에 포탑이 있으면 공속 +8%', 'silver', [(c16A, '나란히 선 포탑 둘'), (c16B, '받침 둘 + 청록 연결선'), (c16C, '포탑 + 시계')]),
 (17, '한 번 더', '남은 증강 선택마다 리롤 +1', 'silver', [(c17A, '금 순환 화살 둘(리롤 버튼 G4와 같은 모양)'), (c17B, '카드 3장 + 순환 화살'), (c17C, '카드 + 물음표')]),
 (18, '빚내기', '즉시 골드 150, 다음 3라운드 처치 골드 −20%', 'silver', [(c18A, '금화 더미 + 붉은 아래 화살'), (c18B, '차용증 + 금화'), (c18C, '금 간 금화')]),
 (19, '정비 완료', '모든 포탑 피해 +20%', 'gold', [(c19A, '포탑 + 스패너 + 체크'), (c19B, '큰 탄약 상자 + 위 화살(1번 계열)'), (c19C, '포탑 + 금 반짝임')]),
 (20, '전투 본능', '기술병 공격력 +5, 사거리 +0.5', 'gold', [(c20A, '헬멧 + 붉은 기운'), (c20B, '스패너 + 넓어진 사거리 호'), (c20C, '조준점 + 스패너')]),
]
