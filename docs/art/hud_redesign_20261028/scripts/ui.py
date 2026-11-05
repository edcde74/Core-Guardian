from parts import *
import icons, math
from PIL import Image
HUD = ART+'UI/Resources/CoreGuardian/Hud/'
F = 36
TIER = {
    'silver':   dict(m='silver', o=(4, 3, 2), gem=R('cyan', 3), gl=R('silver', 4), glow=R('silver', 5)),
    'gold':     dict(m='gold',   o=(5, 4, 2), gem=R('cyan', 3), gl=R('gold', 5),   glow=R('gold', 6)),
    'platinum': dict(m='silver', o=(5, 5, 3), gem=R('cyan', 5), gl=R('cyan', 4),   glow=R('cyan', 5)),
}

def frame_A(t):  # 모서리 보석 (3px 경사 테두리 + 모서리 판 + 보석)
    T = TIER[t]; L = Layer(F, F); m = T['m']
    hi, mid, lo = T['o']
    for y in range(F):
        for x in range(F):
            d = min(x, y, F-1-x, F-1-y)
            if d > 3: continue
            if d == 0: continue
            if d == 1: c = R(m, hi+1) if (x <= y and x < F-1-y) or (y < x and y < F-1-x) else R(m, lo)
            elif d == 2: c = R(m, mid)
            else: c = R(m, lo-1)
            # top/left faces lighter, bottom/right darker
            if d == 2 and (x == F-3 or y == F-3): c = R(m, lo)
            L.px(x, y, c)
    if t == 'platinum':
        for i in range(4, F-4): L.px(i, 4, R('cyan', 4)); L.px(4, i, R('cyan', 4)); L.px(i, F-5, R('cyan', 2)); L.px(F-5, i, R('cyan', 2))
    for (cx, cy) in ((3, 3), (F-4, 3), (3, F-4), (F-4, F-4)):
        L.rect(cx-3, cy-3, cx+3, cy+3, R(m, mid))
        L.hline(cx-3, cx+3, cy-3, R(m, hi+1)); L.vline(cx-3, cy-3, cy+3, R(m, hi+1))
        L.hline(cx-3, cx+3, cy+3, R(m, lo-1)); L.vline(cx+3, cy-3, cy+3, R(m, lo-1))
        L.rect(cx-1, cy-1, cx+1, cy+1, T['gem']); L.px(cx-1, cy-1, R('cyan', 5)); L.px(cx+1, cy+1, R('cyan', 1))
        for (ex, ey) in ((cx-2, cy-2), (cx+2, cy-2), (cx-2, cy+2), (cx+2, cy+2)): L.px(ex, ey, OUT)
    L.outline()
    # inner backing (dark, tinted by tier) so the transparent icon reads on any background
    tint = {'silver': (34, 38, 46), 'gold': (44, 36, 24), 'platinum': (22, 40, 52)}[t]
    for y in range(4, F-4):
        for x in range(4, F-4):
            if L.a[y, x, 3] == 0:
                v = 1.0 - 0.18*((x-4)+(y-4))/(2*(F-8))   # slightly lighter top-left
                L.a[y, x] = (int(tint[0]*v), int(tint[1]*v), int(tint[2]*v), 255)
    return L

def frame_B(t):  # 등급 띠 (아래 가운데 보석 탭)
    T = TIER[t]; L = Layer(F, F+3)
    for i in range(2):
        c = R(T['m'], T['o'][i]); d = R(T['m'], T['o'][2])
        L.hline(3, F-4, 1+i, c); L.hline(3, F-4, F-2-i, d)
        L.vline(1+i, 3, F-4, c); L.vline(F-2-i, 3, F-4, d)
    for (x, y) in ((2, 2), (F-3, 2), (2, F-3), (F-3, F-3)): L.px(x, y, R(T['m'], T['o'][1]))
    # tab
    cx = F//2
    L.rect(cx-4, F-4, cx+3, F, R(T['m'], T['o'][1])); L.hline(cx-4, cx+3, F-4, R(T['m'], T['o'][0]))
    L.rect(cx-2, F-3, cx+1, F-1, T['gem']); L.px(cx-2, F-3, R('cyan', 5))
    if t == 'platinum':
        L.hline(4, F-5, 3, R('cyan', 4)); L.hline(4, F-5, F-4, R('cyan', 2))
    L.outline()
    return L

def frame_C(t):  # 이중 테두리 + 안쪽 빛 선
    T = TIER[t]; L = Layer(F, F)
    for i in range(2):
        L.hline(1, F-2, 1+i, R(T['m'], T['o'][i])); L.hline(1, F-2, F-2-i, R(T['m'], T['o'][2]))
        L.vline(1+i, 1, F-2, R(T['m'], T['o'][i])); L.vline(F-2-i, 1, F-2, R(T['m'], T['o'][2]))
    L.hline(3, F-4, 3, OUT); L.hline(3, F-4, F-4, OUT); L.vline(3, 3, F-4, OUT); L.vline(F-4, 3, F-4, OUT)
    L.hline(4, F-5, 4, T['gl']); L.vline(4, 4, F-5, T['gl']); L.hline(4, F-5, F-5, T['gl']); L.vline(F-5, 4, F-5, T['gl'])
    # corner notches
    for (x, y) in ((1, 1), (F-2, 1), (1, F-2), (F-2, F-2)): L.px(x, y, T['glow'])
    L.outline()
    return L

H4 = [('A', '모서리 보석', frame_A), ('B', '아래 보석 탭', frame_B), ('C', '이중 테두리 + 안쪽 빛 선', frame_C)]

# ---------------- H6 scrollbar ----------------
SW, SH = 10, 120
def scroll_A():   # 홈 + 금 손잡이
    T = Layer(SW, SH)
    T.rect(2, 2, 7, SH-3, R('iron', 1)); T.vline(2, 2, SH-3, R('iron', 0)); T.vline(7, 2, SH-3, R('iron', 3))
    for y in (0, SH-5):
        T.rect(1, y, 8, y+4, R('gold', 3)); T.hline(1, 8, y, R('gold', 5)); T.hline(1, 8, y+4, R('gold', 1)); T.px(4, y+2, R('gold', 6))
    T.outline()
    Hd = Layer(SW, 28)
    Hd.rect(1, 1, 8, 26, R('gold', 4)); Hd.vline(1, 1, 26, R('gold', 5)); Hd.vline(8, 1, 26, R('gold', 2))
    Hd.hline(1, 8, 1, R('gold', 6)); Hd.hline(1, 8, 26, R('gold', 1))
    for y in (11, 13, 15): Hd.hline(3, 6, y, R('gold', 2)); Hd.hline(3, 6, y+1, R('gold', 6))
    Hd.px(4, 4, R('gold', 6)); Hd.px(4, 23, R('gold', 6))
    Hd.outline()
    return T, Hd
def scroll_B():   # 가는 홈 + 청록 손잡이
    T = Layer(SW, SH)
    T.rect(3, 1, 6, SH-2, R('iron', 1)); T.vline(6, 1, SH-2, R('iron', 2))
    T.outline()
    Hd = Layer(SW, 28)
    Hd.rect(1, 3, 8, 24, R('cyan', 3)); Hd.vline(1, 3, 24, R('cyan', 4)); Hd.vline(8, 3, 24, R('cyan', 2)); Hd.vline(4, 5, 22, R('cyan', 5))
    Hd.rect(1, 1, 8, 3, R('gold', 4)); Hd.hline(1, 8, 1, R('gold', 6)); Hd.rect(1, 24, 8, 26, R('gold', 3)); Hd.hline(1, 8, 26, R('gold', 1))
    Hd.outline()
    return T, Hd
def scroll_C():   # 황동 레일 + 어두운 손잡이(황동 끝)
    T = Layer(SW, SH)
    T.rect(4, 1, 5, SH-2, R('gold', 3)); T.vline(4, 1, SH-2, R('gold', 5))
    T.outline()
    Hd = Layer(SW, 28)
    Hd.rect(1, 2, 8, 25, R('iron', 2)); Hd.vline(1, 2, 25, R('iron', 4)); Hd.vline(8, 2, 25, R('iron', 1))
    Hd.rect(1, 1, 8, 4, R('gold', 4)); Hd.hline(1, 8, 1, R('gold', 6)); Hd.rect(1, 23, 8, 26, R('gold', 3))
    for y in (11, 14): Hd.hline(3, 6, y, (240, 240, 240, 255))
    Hd.outline()
    return T, Hd
H6 = [('A', '홈 + 금 손잡이', scroll_A), ('B', '가는 홈 + 청록 손잡이', scroll_B), ('C', '황동 레일 + 어두운 손잡이', scroll_C)]

def nine(path, w, h, b=16):
    im = Image.open(path).convert('RGBA'); W0, H0 = im.size
    out = Image.new('RGBA', (w, h))
    def part(x0, y0, x1, y1): return im.crop((x0, y0, x1, y1))
    out.paste(part(0, 0, b, b), (0, 0)); out.paste(part(W0-b, 0, W0, b), (w-b, 0))
    out.paste(part(0, H0-b, b, H0), (0, h-b)); out.paste(part(W0-b, H0-b, W0, H0), (w-b, h-b))
    out.paste(part(b, 0, W0-b, b).resize((w-2*b, b), Image.NEAREST), (b, 0))
    out.paste(part(b, H0-b, W0-b, H0).resize((w-2*b, b), Image.NEAREST), (b, h-b))
    out.paste(part(0, b, b, H0-b).resize((b, h-2*b), Image.NEAREST), (0, b))
    out.paste(part(W0-b, b, W0, H0-b).resize((b, h-2*b), Image.NEAREST), (w-b, b))
    out.paste(part(b, b, W0-b, H0-b).resize((w-2*b, h-2*b), Image.NEAREST), (b, b))
    return out

def h7_state(frac):
    """button + AreaBlast icon + clockwise cooldown shade (frac remaining 0..1)"""
    btn = nine(HUD+'Button.png', 64, 64, 16)
    ico = Image.open(HUD+'AreaBlast.png').convert('RGBA')
    btn.alpha_composite(ico, (8, 8))
    if frac > 0:
        a = np.array(btn); cx, cy = 32, 32
        for y in range(6, 58):
            for x in range(6, 58):
                ang = (math.degrees(math.atan2(x-cx, -(y-cy))) + 360) % 360  # 0 at top, clockwise
                if ang >= 360*(1-frac):
                    if a[y, x, 3] > 0: a[y, x, :3] = (a[y, x, :3] * 0.35).astype(np.uint8)
        btn = Image.fromarray(a)
    return btn

if __name__ == '__main__':
    S = 5
    c = Image.new('RGBA', (3*(3*(F*S+8)+30), F*S+30), (40, 40, 48, 255))
    ico = icons.hp_A().img()
    for gi, (_, _, fn) in enumerate(H4):
        for ti, t in enumerate(('silver', 'gold', 'platinum')):
            fr = fn(t).img(); base = Image.new('RGBA', fr.size); base.alpha_composite(ico, (2, 2)); base.alpha_composite(fr)
            c.alpha_composite(base.resize((fr.width*S, fr.height*S), Image.NEAREST), (gi*(3*(F*S+8)+30) + ti*(F*S+8), 0))
    c.save('t_h4.png')
    c = Image.new('RGBA', (3*(SW*S+40)+3*70*2, SH*S), (40, 40, 48, 255))
    for i, (_, _, fn) in enumerate(H6):
        T, Hd = fn(); im = T.img(); im.alpha_composite(Hd.img(), (0, 30))
        c.alpha_composite(im.resize((SW*S, SH*S), Image.NEAREST), (i*(SW*S+40), 0))
    for i, f in enumerate((0, 0.4, 0.75)):
        c.alpha_composite(h7_state(f).resize((128, 128), Image.NEAREST), (3*(SW*S+40)+i*140, 0))
    c.save('t_h6.png')
