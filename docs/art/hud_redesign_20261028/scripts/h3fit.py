"""Augment icons fitted inside the confirmed H4 frame's visible area (icon coords 2..29, corner plates cut)."""
from h3lib import *
import h3b1, h3b2, h3b3
from h3b2 import helm, pierce_line, spark, bolt_tip, mini_turret, tar, cycle_arrows, pig, card
from h3b3 import big_helm, rail_bolt, red_x, range_blast
import numpy as np
SAFE = np.load('safe.npy')
def new(): return Layer(N, N)

# modifiers moved inside the safe area (top-right, clear of the corner plate)
def mod_up(L, mat='cyan'): arrow(L, 23, 4, 12, 6, mat, up=True, head=4)
def mod_down(L, mat='red'): arrow(L, 23, 3, 12, 6, mat, up=False, head=4)

def turret_sym(L, x, y):
    """20x19 turret symbol in the style of the HUD TowerIdentity (gold cap, cyan lens ring, gold/cyan base)"""
    T = Layer(N, N)
    T.rect(x+4, y, x+15, y+3, R('gold', 4)); T.hline(x+4, x+15, y, R('gold', 6)); T.hline(x+4, x+15, y+3, R('gold', 2))
    T.rect(x+16, y+1, x+18, y+2, R('gold', 5))                       # muzzle
    T.rect(x+9, y+4, x+10, y+5, R('gold', 3))
    T.ellipse(x+10, y+10, 5.5, 5.5, lambda xx, yy: R('gold', 5 if xx+yy < x+y+19 else 3))
    T.ellipse(x+10, y+10, 3.8, 3.8, lambda xx, yy: R('cyan', 4 if xx+yy < x+y+19 else 2))
    T.ellipse(x+10, y+10, 1.6, 1.6, OUT); T.px(x+8, y+8, (240, 255, 255, 255))
    T.rect(x+2, y+15, x+4, y+18, R('gold', 4)); T.rect(x+16, y+15, x+18, y+18, R('gold', 3))
    T.rect(x+5, y+15, x+6, y+18, R('cyan', 4)); T.rect(x+14, y+15, x+15, y+18, R('cyan', 3))
    T.rect(x+7, y+16, x+13, y+18, R('silver', 3)); T.hline(x+7, x+13, y+16, R('silver', 5))
    T.outline(); L.comp(T)

# ---------------- confirmed 1~20 (refitted) ----------------
def f1():
    L = new()
    L.rect(5, 16, 26, 26, R('wood', 3)); L.hline(5, 26, 16, R('wood', 5)); L.hline(5, 26, 26, R('wood', 1))
    L.hline(5, 26, 21, R('wood', 2)); L.vline(5, 16, 26, R('wood', 4)); L.vline(26, 16, 26, R('wood', 1))
    L.rect(13, 19, 18, 24, R('gold', 4)); L.px(13, 19, R('gold', 6))
    for i, bx in enumerate((8, 13, 18)): bullet(L, bx, 4+(i % 2), 13)
    return done(L)
def f2():
    L = new(); wrench2(L, 9, 12, 22, 25, 6); mod_up(L); return done(L)
def f3():
    L = new(); wrench2(L, 9, 12, 22, 25, 6); snowflake(L, 22, 10, 6); return done(L)
def f4():
    L = new(); br = crop(h3b1.SINGLE_A, 0, 0, 20, 14); L.comp(br, 8, 13, keep=True); flame(L, 9, 15, 10, 4); return done(L)
def f5():
    L = new(); helm(L, 4, 14); helm(L, 14, 7)
    for k in range(23):
        x = 4+k; y = 26-k*0.8
        L.px(x, y, (255, 255, 255, 255)); L.px(x, y+1, R('cyan', 3)); L.px(x, y-1, R('cyan', 5))
    spark(L, 9, 22); spark(L, 21, 13); return done(L)
def f6():
    L = new(); bolt_zig(L, [(7, 7), (15, 14), (11, 17), (23, 24)]); L.ellipse(7, 7, 2.5, 2.5, R('cyan', 4)); L.ellipse(23, 24, 2.5, 2.5, R('cyan', 4)); return done(L)
def f7():
    L = new(); slash(L, [(6, 6), (14, 13), (24, 24)]); slash(L, [(24, 6), (15, 14), (6, 24)]); return done(L)
def f8():
    L = new(); crosshair(L, 16, 16, 9, 'red'); return done(L)
def f9():
    return h3b2.c9C()
def f10():
    L = new(); poster(L, 5, 5, 16, 20)
    L.ellipse(13, 14, 4, 4, R('iron', 2)); L.hline(8, 18, 20, R('wood', 2)); L.hline(8, 16, 22, R('wood', 2))
    coin(L, 22, 22, 5)
    return done(L)
def f11():
    L = new(); pig(L, 3, 12); coin(L, 13, 7, 4); return done(L)
def f12():
    L = new(); crystal(L, 11, 4, 27, 8)
    arrow(L, 23, 4, 12, 5, 'cyan', up=True, head=4); arrow(L, 23, 16, 25, 5, 'red', up=False, head=4)
    return done(L)
def f13():
    L = new(); turret_sym(L, 3, 8); mod_up(L); return done(L)
def f14():
    L = new(); shield(L, 'blue', 'gold', cx=16, top=5, bot=28, hw=11); crystal(L, 16, 9, 24, 4); return done(L)
def f15():
    return h3b2.c15B()
def f16():
    L = new(); mini_turret(L, 3, 10); mini_turret(L, 12, 10); return done(L)
def f17():
    L = new(); card(L, 5, 10); card(L, 9, 8); card(L, 14, 5)
    cycle_arrows(L, 21, 21, 5, 'gold')
    return done(L)
def f18():
    L = new(); poster(L, 5, 5, 15, 21)
    for y in (11, 14, 17): L.hline(8, 17, y, R('wood', 2))
    L.rect(13, 21, 18, 24, R('red', 3))
    coin(L, 22, 22, 5)
    return done(L)
def f19():
    L = new(); turret_sym(L, 3, 9); wrench2(L, 21, 8, 26, 13, 4)
    for (x, y) in ((20, 24), (21, 25), (22, 26), (23, 25), (24, 24), (25, 23), (26, 22)): L.px(x, y, R('green', 4)); L.px(x, y+1, R('green', 2))
    return done(L)
def f20():
    return h3b2.c20B()
def f21():
    L = new(); big_helm(L, 4, 11, frozen=True); mod_up(L, 'red'); return done(L)
def f22():
    L = new()
    L.rect(5, 18, 26, 26, R('stone', 3)); L.hline(5, 26, 18, R('stone', 5)); L.vline(12, 18, 26, R('stone', 1)); L.vline(19, 18, 26, R('stone', 1))
    for fx in (8, 15, 22): flame(L, fx, 22, 10, 3)
    wrench2(L, 21, 7, 25, 11, 3)
    return done(L)
def f23():
    L = new(); rail_bolt(L, 4, 22, 18)
    for a in range(-90, 91, 6):
        rad = math.radians(a); L.px(21+math.cos(rad)*5, 15+math.sin(rad)*7, R('cyan', 4)); L.px(20+math.cos(rad)*5, 15+math.sin(rad)*7, R('cyan', 2))
    L.hline(13, 21, 8, R('cyan', 4))
    for (x, y) in ((11, 8), (12, 7), (12, 9), (13, 6), (13, 10)): L.px(x, y, R('cyan', 5))
    return done(L)
def f24():
    L = new(); bolt_zig(L, [(5, 9), (13, 17), (9, 19), (20, 26)]); mod_up(L); return done(L)
def f26():
    L = new(); crosshair(L, 13, 17, 8, 'red'); mod_up(L); return done(L)
def f28():
    L = new(); helm(L, 4, 16); flame(L, 10, 17, 9, 4); bolt_zig(L, [(13, 15), (17, 12), (16, 10), (20, 8)]); helm(L, 15, 10); flame(L, 21, 11, 7, 3); return done(L)
def f29():
    L = new()
    for k in range(23):
        x = 4+k; y = 26-k*0.9
        L.px(x, y, (255, 255, 255, 255)); L.px(x, y+1, R('cyan', 3)); L.px(x, y-1, R('cyan', 5))
    crosshair(L, 16, 16, 8, 'red')
    return done(L)

# ---------------- redraws ----------------
def back_arrow(L, cx, cy, r):
    """counter-clockwise cyan arrow around a clock (cool-down reduction)"""
    for a in range(200, 340, 8):
        rad = math.radians(a); L.px(cx+math.cos(rad)*r, cy+math.sin(rad)*r, R('cyan', 4)); L.px(cx+math.cos(rad)*(r+1), cy+math.sin(rad)*(r+1), R('cyan', 2))
    ex, ey = cx+math.cos(math.radians(200))*r, cy+math.sin(math.radians(200))*r
    for (dx, dy) in ((0, 0), (1, -1), (1, 1), (2, -2), (2, 0)): L.px(ex+dx, ey+dy, R('cyan', 5))
def axe(L, x, y):
    """executioner axe: wood haft down-left, red-steel blade"""
    for k in range(17): L.px(x+k*0.75, y+16-k, R('wood', 4)); L.px(x+1+k*0.75, y+16-k, R('wood', 2))
    T = ["..SSSS.", ".SSSSSS", "SSSSSSE", "SSSSSSE", "SSSSSSE", ".SSSSSE", "..SSSE."]
    for j, row in enumerate(T):
        for i, ch in enumerate(row):
            if ch == 'S': L.px(x+9+i, y-1+j, R('silver', 4 if i < 3 else 3))
            elif ch == 'E': L.px(x+9+i, y-1+j, R('red', 4))
def c25A():
    L = new()
    slash(L, [(6, 8), (12, 13), (16, 20), (18, 26)]); slash(L, [(12, 7), (15, 14), (17, 19)])
    stopwatch(L, 22, 10, 5); back_arrow(L, 22, 10, 7)
    return done(L)
def c25B():
    L = new(); axe(L, 4, 10); stopwatch(L, 22, 20, 5); back_arrow(L, 22, 20, 7); return done(L)
def c25C():
    L = new(); range_blast(L, 12, 17, 9)
    slash(L, [(19, 4), (22, 9), (24, 13)]); back_arrow(L, 22, 21, 5)
    return done(L)
def c27A():
    L = new(); snowflake(L, 15, 15, 11); red_x(L, 9, 9, 13); return done(L)
def c27B():
    L = new(); big_helm(L, 7, 9, frozen=True); red_x(L, 9, 10, 14); return done(L)
def c27C():
    L = new()
    L.rect(6, 6, 25, 25, (176, 222, 238, 255)); L.hline(6, 25, 6, (235, 252, 255, 255)); L.vline(6, 6, 25, (235, 252, 255, 255))
    L.hline(6, 25, 25, R('cyan', 1)); L.vline(25, 6, 25, R('cyan', 1))
    for (x0, y0, x1, y1) in ((8, 20, 13, 9), (18, 23, 23, 14)): line(L, x0, y0, x1, y1, (235, 252, 255, 255))
    red_x(L, 9, 9, 13)
    return done(L)
def c30A():
    L = new(); turret_sym(L, 4, 8); mod_up(L)
    wrench2(L, 22, 20, 25, 24, 3)
    return done(L)
def c30B():
    L = new(); turret_sym(L, 6, 7); mod_up(L)
    for a in range(95, 265, 9):
        rad = math.radians(a); L.px(16+math.cos(rad)*11, 17+math.sin(rad)*9, R('cyan', 3))
    wrench2(L, 5, 21, 8, 24, 3)
    return done(L)
def c30C():
    L = new(); turret_sym(L, 4, 4); wrench2(L, 18, 20, 25, 27, 4); mod_up(L)
    return done(L)

CONF = {1: f1, 2: f2, 3: f3, 4: f4, 5: f5, 6: f6, 7: f7, 8: f8, 9: f9, 10: f10, 11: f11, 12: f12, 13: f13, 14: f14, 15: f15, 16: f16, 17: f17, 18: f18, 19: f19, 20: f20,
        21: f21, 22: f22, 23: f23, 24: f24, 26: f26, 28: f28, 29: f29}
REDO = [
 (25, '처형인', '처형마다 범위 폭발 쿨다운 −1초', 'gold', [(c25A, '처형 칼자국(D5 2연타) + 거꾸로 감기는 시계'), (c25B, '처형인 도끼 + 거꾸로 감기는 시계'), (c25C, '범위 폭발 + 작은 칼자국 + 거꾸로 화살')]),
 (27, '얼음 감옥', '둔화된 적의 처형 기준 +5%p', 'gold', [(c27A, '큰 눈꽃 위에 붉은 X 겹침'), (c27B, '얼어붙은 투구 위에 붉은 X 겹침'), (c27C, '얼음 덩어리 위에 붉은 X 겹침')]),
 (30, '화력 지원', '기술병 주변 2칸 포탑 공속 +15%', 'gold', [(c30A, '포탑 크게 + 위 화살 + 작은 스패너(오른아래)'), (c30B, '포탑 + 위 화살 + 범위 호 + 작은 스패너(왼아래)'), (c30C, '포탑(왼위) + 스패너(오른아래) + 위 화살')]),
]
def outside(a):
    return int(((a[:, :, 3] > 0) & ~SAFE).sum())
