from h3lib import *
SINGLE_A = load(ART+'Characters/Towers/Sprites/tower_single.png')
def new(): return Layer(N, N)

# 1 든든한 탄약 — 모든 포탑 피해 +8%
def c1A():
    L = new()  # ammo crate + shells
    L.rect(4, 17, 27, 28, R('wood', 3)); L.hline(4, 27, 17, R('wood', 5)); L.hline(4, 27, 28, R('wood', 1))
    L.hline(4, 27, 22, R('wood', 2)); L.vline(4, 17, 28, R('wood', 4)); L.vline(27, 17, 28, R('wood', 1))
    L.rect(13, 20, 18, 25, R('gold', 4)); L.px(13, 20, R('gold', 6))
    for i, bx in enumerate((7, 13, 19)): bullet(L, bx, 4+(i % 2), 13)
    return done(L)
def c1B():
    L = new()  # three upright shells
    for i, bx in enumerate((5, 13, 21)): bullet(L, bx, 6 - (2 if i == 1 else 0), 20)
    return done(L)
def c1C():
    L = new()  # cyan shell + up arrow
    bullet(L, 6, 5, 22)
    arrow(L, 22, 4, 26, 9, 'cyan', up=True, head=6)
    return done(L)

# 2 단련된 손 — 기술병 공격력 +2, 공속 +10%
def c2A():
    L = new(); L.comp(HELMET, -2, 2); arrow(L, 26, 2, 13, 7, 'cyan', up=True, head=5); return done(L)
def c2B():
    L = new(); wrench2(L, 9, 9, 25, 25, 6); arrow(L, 26, 2, 13, 7, 'cyan', up=True, head=5); return done(L)
def c2C():
    L = new(); wrench2(L, 11, 9, 26, 24, 6)
    for (y, x0, x1) in ((17, 0, 7), (21, 1, 9), (25, 3, 11)): L.hline(x0, x1, y, R('cyan', 4)); L.px(x0, y, R('cyan', 2))
    return done(L)

# 3 차가운 손끝 — 기술병 명중 시 둔화
def c3A():
    L = new(); L.comp(HELMET, -4, 4); snowflake(L, 24, 8, 6); return done(L)
def c3B():
    L = new(); wrench2(L, 8, 8, 20, 20, 6); snowflake(L, 23, 23, 6); return done(L)
def c3C():
    L = new(); snowflake(L, 15, 10, 8); chevrons_down(L, 15, 20, 2); return done(L)

# 4 불씨 — 단일 고속 포탑 명중 시 화상 1스택
def c4A():
    L = new(); flame(L, 16, 28, 24, 9); return done(L)
def c4B():
    L = new(); bullet(L, 4, 10, 16); flame(L, 21, 27, 18, 7); return done(L)
def c4C():
    L = new()
    # single tower barrel (original pixels) with ember at the muzzle
    br = crop(SINGLE_A, 0, 0, 20, 14)
    L.comp(br, 8, 14, keep=True)
    flame(L, 8, 16, 12, 5)
    return done(L)

# 5 꿰뚫는 탄 — 기술병 투사체 1명 관통
def c5A():
    L = new()
    L.ellipse(16, 16, 7, 9, R('iron', 3)); L.ellipse(15, 15, 5, 7, R('iron', 4))
    line(L, 2, 16, 30, 16, R('cyan', 5)); line(L, 2, 17, 30, 17, R('cyan', 2))
    L.rect(26, 15, 29, 18, R('gold', 4))
    return done(L)
def c5B():
    L = new(); shield(L, 'silver', 'gold', cx=14, top=5, bot=27, hw=9)
    line(L, 2, 26, 29, 4, R('cyan', 5), 2); L.rect(26, 3, 29, 6, R('gold', 4))
    return done(L)
def c5C():
    L = new()
    for cx in (9, 20): L.ellipse(cx, 16, 4, 4, R('red', 3))
    line(L, 1, 16, 30, 16, R('cyan', 5)); line(L, 1, 15, 30, 15, R('cyan', 3))
    return done(L)

# 6 튕기는 탄 — 범위 포탑 공격 1회 연쇄
def c6A():
    L = new(); bolt_zig(L, [(4, 6), (14, 14), (10, 17), (26, 27)]); L.ellipse(4, 6, 2.5, 2.5, R('cyan', 4)); L.ellipse(26, 26, 2.5, 2.5, R('cyan', 4)); return done(L)
def c6B():
    L = new()
    for (cx, cy) in ((6, 22), (25, 22)): L.ellipse(cx, cy, 4, 4, R('red', 3))
    for a in range(0, 181, 6):
        rad = math.radians(180+a); L.px(15.5+math.cos(rad)*10, 20+math.sin(rad)*12, R('cyan', 5)); L.px(15.5+math.cos(rad)*10, 21+math.sin(rad)*12, R('cyan', 2))
    return done(L)
def c6C():
    L = new(); bullet(L, 3, 4, 14); bullet(L, 24, 14, 14); bolt_zig(L, [(9, 10), (16, 16), (13, 18), (22, 22)]); return done(L)

# 7 마무리 일격 — 처형 (체력 8% 이하)
def c7A():
    L = new(); slash(L, [(4, 5), (12, 11), (20, 18), (26, 27)]); slash(L, [(26, 4), (18, 12), (11, 19), (5, 26)]); return done(L)
def c7B():
    L = new()
    L.rect(3, 22, 28, 27, R('iron', 1)); L.rect(5, 24, 7, 25, R('red', 3))
    sword(L, 24, 2, 14, 12)
    return done(L)
def c7C():
    L = new(); sword(L, 27, 2, 15, 14)
    L.ellipse(9, 22, 6, 6, R('red', 3)); L.ellipse(8, 21, 3, 3, R('red', 4)); slash(L, [(3, 28), (14, 16)])
    return done(L)

# 8 날카로운 눈 — 치명타 확률 +10%
def c8A():
    L = new(); eye(L, 16, 16, 12, 7); return done(L)
def c8B():
    L = new(); crosshair(L, 16, 16, 11, 'red'); return done(L)
def c8C():
    L = new()
    L.rect(13, 3, 18, 21, (255, 220, 60, 255)); L.vline(13, 3, 21, (255, 244, 160, 255)); L.vline(18, 3, 21, (200, 150, 30, 255))
    L.rect(13, 24, 18, 29, (255, 220, 60, 255)); L.vline(18, 24, 29, (200, 150, 30, 255))
    return done(L)

# 9 성장통 — 처치마다 공격력 영구 증가
def c9A():
    L = new(); sword(L, 27, 2, 15, 14)
    S = Layer(N, N); sprout(L, 8, 14); sprout(L, 8, 18)
    L.rect(3, 26, 14, 29, R('wood', 3)); L.hline(3, 14, 26, R('wood', 5))
    return done(L)
def c9B():
    L = new()
    for i, h in enumerate((5, 10, 15, 20)):
        x0 = 3 + i*7; L.rect(x0, 28-h, x0+5, 28, R('cyan', 3)); L.hline(x0, x0+5, 28-h, R('cyan', 5)); L.vline(x0+5, 28-h, 28, R('cyan', 1))
    return done(L)
def c9C():
    L = new(); wrench2(L, 9, 12, 22, 25, 6)
    for (sx, sy) in ((23, 5), (26, 12), (20, 1)):
        L.px(sx, sy, (255, 240, 150, 255)); L.px(sx-1, sy, R('gold', 5)); L.px(sx+1, sy, R('gold', 5)); L.px(sx, sy-1, R('gold', 5)); L.px(sx, sy+1, R('gold', 5))
    return done(L)

# 10 현상금 — 처치 골드 +10%
def c10A():
    L = new(); poster(L, 4, 3, 18, 22)
    L.ellipse(13, 13, 4, 4, R('iron', 2)); L.hline(8, 18, 20, R('wood', 2)); L.hline(8, 16, 22, R('wood', 2))
    coin(L, 23, 24, 6)
    return done(L)
def c10B():
    L = new(); coin(L, 13, 18, 9); arrow(L, 26, 3, 15, 7, 'cyan', up=True, head=5); return done(L)
def c10C():
    L = new()
    L.ellipse(16, 19, 10, 9, R('leather', 3)); L.ellipse(14, 17, 6, 5, R('leather', 4))
    L.rect(12, 6, 20, 10, R('leather', 2)); L.hline(11, 21, 10, R('gold', 4))
    L.rect(14, 16, 18, 22, R('gold', 4)); L.px(14, 16, R('gold', 6))
    return done(L)

CARDS = [
 (1, '든든한 탄약', '모든 포탑 피해 +8%', 'silver', [(c1A, '탄약 상자 + 탄 3발'), (c1B, '세운 청록 탄 3발'), (c1C, '청록 탄 + 위 화살')]),
 (2, '단련된 손', '기술병 공격력 +2, 공속 +10%', 'silver', [(c2A, '기술병 헬멧 + 위 화살'), (c2B, '스패너 + 위 화살'), (c2C, '스패너 + 속도선')]),
 (3, '차가운 손끝', '기술병 명중 시 1초 둔화 15%', 'silver', [(c3A, '기술병 헬멧 + 눈꽃'), (c3B, '스패너 + 눈꽃'), (c3C, '눈꽃 + 둔화 표식(아래 화살)')]),
 (4, '불씨', '단일 고속 포탑 명중 시 화상 1스택', 'silver', [(c4A, '큰 불꽃'), (c4B, '탄 + 불꽃'), (c4C, '단일 포탑 포신 끝 불씨')]),
 (5, '꿰뚫는 탄', '기술병 투사체가 1명 관통', 'silver', [(c5A, '돌을 뚫는 청록 탄'), (c5B, '방패를 꿰뚫는 탄'), (c5C, '두 적을 잇는 관통선')]),
 (6, '튕기는 탄', '범위 공격 포탑 공격 1회 연쇄', 'silver', [(c6A, '청록 번개 튕김'), (c6B, '두 적 사이 튕김 곡선'), (c6C, '탄 두 발 + 번개')]),
 (7, '마무리 일격', '기술병 공격에 처형(체력 8% 이하)', 'silver', [(c7A, '붉은 X 칼자국'), (c7B, '거의 빈 체력 + 칼'), (c7C, '칼 + 붉은 칼자국')]),
 (8, '날카로운 눈', '모든 공격 치명타 확률 +10%', 'silver', [(c8A, '청록 눈'), (c8B, '붉은 조준점'), (c8C, '노란 느낌표(치명타 숫자)')]),
 (9, '성장통', '기술병 처치마다 공격력 +0.05', 'silver', [(c9A, '칼 + 자라는 새싹'), (c9B, '자라는 막대 4개'), (c9C, '스패너 + 쌓이는 별')]),
 (10, '현상금', '처치 골드 +10%', 'silver', [(c10A, '현상금 포스터 + 금화'), (c10B, '큰 금화 + 위 화살'), (c10C, '금화 주머니')]),
]
