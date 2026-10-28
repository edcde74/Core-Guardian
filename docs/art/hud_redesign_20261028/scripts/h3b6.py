from h3lib import *
from h3b5 import clock_face, blood_drips, BLOOD
import math
def new(): return Layer(N, N)

def loop_arrow(L, cx, cy, r=5):
    """counter-clockwise loop arrow: ring with a gap at the upper-left, big head at the gap"""
    for a in range(0, 360, 3):
        if 200 < a < 262: continue
        rad = math.radians(a)
        L.px(cx+math.cos(rad)*r, cy+math.sin(rad)*r, R('cyan', 4))
        L.px(cx+math.cos(rad)*(r+1), cy+math.sin(rad)*(r+1), R('cyan', 2))
    # head at angle 262 (just right of top), pointing counter-clockwise (toward the left)
    hx = cx + math.cos(math.radians(262))*r + 0.5; hy = cy + math.sin(math.radians(262))*r
    tri = [(-3, 0), (-2, -1), (-2, 0), (-2, 1), (-1, -2), (-1, -1), (-1, 0), (-1, 1), (-1, 2), (0, -2), (0, -1), (0, 0), (0, 1), (0, 2)]
    for (dx, dy) in tri: L.px(hx+dx, hy+dy, (235, 255, 255, 255) if dx <= -2 else R('cyan', 5))

# 25 처형인 A 수정 — 거꾸로 도는 표시를 오른쪽 위 한 바퀴 화살로
def c25():
    L = new(); clock_face(L, 12, 15, 7, 200)
    blood_drips(L, [(8, 3), (12, 5), (16, 2)], 21)
    loop_arrow(L, 22, 10, 5)
    return done(L)

# 32 수정 광산 — 45도 곡괭이, 반듯한 철 머리
def pickaxe45(L, x, y, s=1):
    """handle 45deg from bottom-left (x,y) up-right; symmetric iron head arching over the top end"""
    n = 12*s
    for k in range(n):
        L.px(x+k, y-k, R('wood', 4)); L.px(x+k+1, y-k, R('wood', 2))
    tx, ty = x+n-1, y-n+1            # top of handle
    # head: symmetric arc perpendicular to the handle, ends bending back toward the handle (like a classic pickaxe)
    for t10 in range(-55, 56, 5):
        t = t10/10.0
        bend = (t*t)/9.0
        hx = tx + t - bend; hy = ty + t + bend
        L.px(round(hx), round(hy), R('silver', 5)); L.px(round(hx)+1, round(hy), R('silver', 3)); L.px(round(hx)+1, round(hy)+1, R('silver', 1))
    L.rect(tx-1, ty-1, tx+1, ty+1, R('iron', 3))

def c32A():
    L = new(); crystal(L, 11, 5, 27, 7); pickaxe45(L, 14, 26); return done(L)
def c32B():
    L = new(); pickaxe45(L, 6, 24); crystal(L, 22, 15, 27, 4); return done(L)
def c32C():
    L = new()
    L.rect(4, 21, 27, 27, R('stone', 3)); L.hline(4, 27, 21, R('stone', 5))
    for (cx, top, hw) in ((9, 9, 4), (16, 6, 4)): crystal(L, cx, top, 22, hw)
    pickaxe45(L, 14, 22)
    return done(L)

FIX = [
 (25, '처형인', '처형마다 범위 폭발 쿨다운 −1초', 'gold', [(c25, 'A안 수정: 오른쪽 위 한 바퀴 거꾸로 화살')], 'A', '메모 반영: 거꾸로 도는 표시를 오른쪽 위로, 한 바퀴 도는 동그라미 모양'),
 (32, '수정 광산', '10라운드 뒤부터 매 라운드 수정 +10', 'gold', [(c32A, '수정 + 45도 곡괭이(오른쪽 아래)'), (c32B, '45도 곡괭이 크게 + 작은 수정'), (c32C, '수정 더미 + 45도 곡괭이')], 'A', '메모 반영: 곡괭이 45도, 철 머리를 반듯한 아치형으로'),
]
