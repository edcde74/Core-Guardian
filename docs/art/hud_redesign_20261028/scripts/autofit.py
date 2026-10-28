import numpy as np
SAFE = np.load('safe.npy')
def shift(a, dx, dy):
    b = np.zeros_like(a)
    h, w = a.shape[:2]
    ys, xs = np.where(a[:, :, 3] > 0)
    if len(ys) == 0: return a
    if ys.min()+dy < 0 or ys.max()+dy >= h or xs.min()+dx < 0 or xs.max()+dx >= w: return None
    b[ys+dy, xs+dx] = a[ys, xs]; return b
def fit(a):
    best = None
    for dy in range(-4, 5):
        for dx in range(-4, 5):
            b = shift(a, dx, dy)
            if b is None: continue
            out = int(((b[:, :, 3] > 0) & ~SAFE).sum())
            key = (out, abs(dx)+abs(dy))
            if best is None or key < best[0]: best = (key, b, dx, dy)
    return best[1], best[0][0], best[2], best[3]
