from icons import *
import math

def line(L, x0, y0, x1, y1, c, w=1):
    n = max(abs(x1-x0), abs(y1-y0), 1)
    for k in range(n+1):
        x = x0+(x1-x0)*k/n; y = y0+(y1-y0)*k/n
        for i in range(w): L.px(x+i, y, c if not callable(c) else c(i))

def bullet(L, x, y, h=12, mat='cyan', vertical=True):
    """upright shell: gold casing + coloured tip, 5 wide"""
    for j in range(h):
        for i in range(5):
            if j < 4:   # tip
                if abs(i-2) > j*0.8+0.3: continue
                L.px(x+i, y+j, R(mat, 5 if i < 2 else 4 if i == 2 else 2))
            else:
                L.px(x+i, y+j, R('gold', 5 if i == 0 else 4 if i < 3 else 2))
    L.hline(x, x+4, y+h-1, R('gold', 1)); L.hline(x, x+4, y+h-3, R('gold', 2))

def flame(L, cx, by, h=14, w=6):
    for y in range(by-h, by+1):
        t = (y-(by-h))/h
        half = w*math.sin(min(1, t*1.25)*math.pi/2) * (1 - 0.15*t)
        sway = math.sin(t*5)*1.0*(1-t)
        for x in range(int(cx-half+sway), int(cx+half+sway)+1):
            inner = abs(x-cx-sway) < half*0.45 and t > 0.45
            c = (255, 236, 150, 255) if inner and t > 0.65 else (255, 176, 50, 255) if inner else (226, 88, 30, 255) if t > 0.3 else (200, 50, 25, 255)
            L.px(x, y, c)

def snowflake(L, cx, cy, r=6):
    for a in range(0, 180, 60):
        rad = math.radians(a)
        for t in range(-r, r+1):
            L.px(cx+math.cos(rad)*t, cy+math.sin(rad)*t, R('cyan', 5 if abs(t) < 2 else 4))
        for s in (-1, 1):
            bx, by = cx+math.cos(rad)*s*(r-2), cy+math.sin(rad)*s*(r-2)
            for b in (-1, 1):
                L.px(bx+math.cos(rad+b*1.0)*1.5*s, by+math.sin(rad+b*1.0)*1.5*s, R('cyan', 3))
    L.px(cx, cy, (255, 255, 255, 255))

def bolt_zig(L, pts, mat='cyan'):
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        line(L, ax, ay, bx, by, R(mat, 5)); line(L, ax+1, ay, bx+1, by, R(mat, 3))

def coin(L, cx, cy, r=6):
    L.ellipse(cx, cy, r, r, lambda x, y: R('gold', 5 if (x-cx)+(y-cy) < -2 else 4 if (x-cx)+(y-cy) < 3 else 2))
    L.ellipse(cx, cy, r-2.2, r-2.2, R('gold', 3))
    L.rect(cx-1, cy-2, cx, cy+1, R('gold', 5)); L.px(cx-1, cy-2, R('gold', 6))

def eye(L, cx, cy, w=11, h=6):
    for y in range(cy-h, cy+h+1):
        for x in range(cx-w, cx+w+1):
            if ((x-cx)/w)**2 + ((y-cy)/h)**2 <= 1 and abs(y-cy) <= h*(1-abs(x-cx)/w*0.55): L.px(x, y, (236, 238, 242, 255))
    L.ellipse(cx, cy, 4, 4, R('cyan', 3)); L.ellipse(cx, cy, 2, 2, OUT); L.px(cx-2, cy-2, (255, 255, 255, 255))

def crosshair(L, cx, cy, r=9, mat='red'):
    for a in range(0, 360, 6):
        rad = math.radians(a)
        L.px(cx+math.cos(rad)*r, cy+math.sin(rad)*r, R(mat, 3))
        L.px(cx+math.cos(rad)*(r-1), cy+math.sin(rad)*(r-1), R(mat, 4))
    for d in range(3, r+3):
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if d > r-3: L.px(cx+dx*d, cy+dy*d, R(mat, 4))
    L.px(cx, cy, R(mat, 4))

def big_wrench(L, x0, y0, x1, y1):
    """diagonal wrench: open jaw at (x0,y0), handle to (x1,y1) - 45deg down-right"""
    n = x1-x0
    for k in range(n+1):
        x, y = x0+k, y0+k
        L.px(x, y+1, R('silver', 5)); L.px(x+1, y+1, R('silver', 3)); L.px(x+1, y+2, R('silver', 2))
    # jaw
    for (dx, dy, s) in ((-3, -1, 5), (-3, 0, 5), (-2, -2, 5), (-1, -3, 5), (0, -3, 4), (-2, 1, 4), (-1, 2, 3), (1, -3, 3), (2, -2, 3), (1, 1, 3), (2, 0, 2)):
        L.px(x0+dx, y0+dy, R('silver', s))
    L.rect(x1, y1, x1+2, y1+2, R('silver', 3)); L.px(x1+1, y1+1, OUT)

def fist(L, x, y):
    """gloved fist 12x10 (leather glove, brass knuckle)"""
    T = ["..LLLMMD....",
         ".LLMMMMMD...",
         "LMMGGGGGMD..",
         "LMMMMMMMMMD.",
         "LMMMMMMMMMD.",
         "LMMMMMMMMMD.",
         ".LMMMMMMMD..",
         "..LMMMMMD...",
         "..LLMMMDD...",
         "..LMMMMMD..."]
    for j, row in enumerate(T):
        for i, ch in enumerate(row):
            if ch == 'G': L.px(x+i, y+j, R('gold', 5))
            elif ch in 'LMD': L.px(x+i, y+j, R('leather', {'L': 4, 'M': 3, 'D': 1}[ch]))
    for i in range(3, 9, 2): L.px(x+i, y+3, R('leather', 1))

def sprout(L, x, y):
    L.vline(x, y, y+8, R('green', 2)); L.vline(x+1, y+2, y+8, R('green', 1))
    for (dx, dy, c) in ((-1, 1, 4), (-2, 0, 4), (-3, 0, 3), (-2, 1, 3), (2, 2, 4), (3, 1, 4), (4, 1, 3), (3, 2, 3)):
        L.px(x+dx, y+dy, R('green', c))

def poster(L, x0, y0, w, h):
    L.rect(x0, y0, x0+w, y0+h, (226, 204, 156, 255))
    L.hline(x0, x0+w, y0, (244, 228, 188, 255)); L.vline(x0+w, y0, y0+h, (176, 150, 104, 255)); L.hline(x0, x0+w, y0+h, (176, 150, 104, 255))
    L.hline(x0+2, x0+w-2, y0+2, R('red', 3)); L.hline(x0+2, x0+w-2, y0+3, R('red', 3))

HELMET = load(ART+'UI/Resources/CoreGuardian/Hud/PlayerIdentity.png')

def wrench2(L, jx, jy, ex, ey, r=5):
    """thick wrench: ring jaw centred (jx,jy) opening toward up-left, 3px handle to (ex,ey)"""
    n = max(abs(ex-jx), abs(ey-jy)); dx = (ex-jx)/n; dy = (ey-jy)/n
    px_, py_ = -dy, dx
    for k in range(int(r*0.6), n+1):
        x = jx+dx*k; y = jy+dy*k
        xi, yi = round(x), round(y)
        L.px(xi-1, yi, R('silver', 5)); L.px(xi, yi, R('silver', 4)); L.px(xi+1, yi, R('silver', 2))
    for y in range(int(jy-r-1), int(jy+r+2)):
        for x in range(int(jx-r-1), int(jx+r+2)):
            d = math.hypot(x+0.5-jx, y+0.5-jy)
            ang = math.atan2(y+0.5-jy, x+0.5-jx)
            opening = abs(((ang - math.atan2(-dy, -dx)) + math.pi) % (2*math.pi) - math.pi) < 0.55
            if d <= r and d >= r*0.45 and not opening:
                lit = -(x-jx) - (y-jy)
                L.px(x, y, R('silver', 5 if lit > 2 else 4 if lit > -2 else 2))
    L.ellipse(ex, ey, 1.6, 1.6, R('silver', 3))

def slash(L, pts, mat='red'):
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = max(abs(bx-ax), abs(by-ay))
        for k in range(n+1):
            x = ax+(bx-ax)*k/n; y = ay+(by-ay)*k/n
            L.px(x, y, (255, 240, 235, 255)); L.px(x+1, y, R(mat, 4)); L.px(x, y+1, R(mat, 3)); L.px(x+1, y+1, R(mat, 2))

def chevrons_down(L, cx, y, n=2, mat='cyan'):
    for k in range(n):
        yy = y + k*5
        for i in range(-5, 6):
            L.px(cx+i, yy+(5-abs(i))*0.6, R(mat, 4 if i < 0 else 3)); L.px(cx+i, yy+1+(5-abs(i))*0.6, R(mat, 2))
