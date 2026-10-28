"""Post-process to match the hand-pixel look of the originals:
   - classify every pixel to (material, shade index) using the ramps
   - remove 1px noise -> clusters, add 2px texture blobs on big flat areas
   - rim light on top/left edges of each material region, shadow on bottom/right
   - dark seams where two large material regions meet"""
import numpy as np, random
from parts import RAMP, OUT, R

MATS = list(RAMP.keys())
PAL = []
for mi, m in enumerate(MATS):
    for i, c in enumerate(RAMP[m]): PAL.append((mi, i, c[:3]))
PALC = np.array([p[2] for p in PAL], float)

def classify(a):
    h, w = a.shape[:2]
    mat = np.full((h, w), -1, int); idx = np.zeros((h, w), int)
    op = a[:, :, 3] > 0
    rgb = a[:, :, :3].astype(float)
    dark = op & (rgb.sum(2) < 60)
    mat[dark] = -2  # outline
    ys, xs = np.where(op & ~dark)
    if len(ys):
        d = ((rgb[ys, xs][:, None, :] - PALC[None]) ** 2).sum(2)
        k = d.argmin(1)
        for (y, x, kk) in zip(ys, xs, k):
            mat[y, x] = PAL[kk][0]; idx[y, x] = PAL[kk][1]
    return mat, idx

def render(a, mat, idx, keep):
    out = a.copy()
    h, w = mat.shape
    for y in range(h):
        for x in range(w):
            if keep[y, x] or mat[y, x] < 0: continue
            m = MATS[mat[y, x]]; r = RAMP[m]
            out[y, x] = r[max(0, min(len(r)-1, idx[y, x]))]
    return out

def pixelize(a, keep=None, seed=5, rim=True, seams=True, blobs=True, exclude=('cyan',)):
    rnd = random.Random(seed)
    h, w = a.shape[:2]
    keep = np.zeros((h, w), bool) if keep is None else keep.copy()
    mat, idx = classify(a)
    src_idx = idx.copy()
    ex = [MATS.index(e) for e in exclude if e in MATS]
    def same(y, x, yy, xx): return 0 <= yy < h and 0 <= xx < w and mat[yy, xx] == mat[y, x]
    # 1) denoise: a pixel whose shade differs from all 4 same-material neighbours that agree -> take theirs
    for y in range(h):
        for x in range(w):
            if mat[y, x] < 0 or keep[y, x]: continue
            nb = [src_idx[yy, xx] for yy, xx in ((y-1, x), (y+1, x), (y, x-1), (y, x+1)) if same(y, x, yy, xx)]
            if len(nb) >= 3 and all(n == nb[0] for n in nb) and src_idx[y, x] != nb[0]:
                idx[y, x] = nb[0]
    base = idx.copy()
    # 2) texture blobs on flat interiors (2x1/2x2 lighter or darker clusters)
    if blobs:
        for y in range(1, h-2):
            for x in range(1, w-2):
                if mat[y, x] < 0 or keep[y, x] or mat[y, x] in ex: continue
                block = [(y, x), (y, x+1), (y+1, x), (y+1, x+1)]
                if not all(mat[p] == mat[y, x] and base[p] == base[y, x] and not keep[p] for p in block): continue
                if rnd.random() < 0.035:
                    dv = 1 if rnd.random() < 0.5 else -1
                    shape = rnd.choice([block[:2], block, [block[0], block[2]]])
                    for p in shape: idx[p] = base[p] + dv
    # 3) rim light / shadow per material region
    if rim:
        add = np.zeros((h, w), int)
        for y in range(h):
            for x in range(w):
                if mat[y, x] < 0 or keep[y, x] or mat[y, x] in ex: continue
                up = same(y, x, y-1, x); lf = same(y, x, y, x-1)
                dn = same(y, x, y+1, x); rt = same(y, x, y, x+1)
                if not up or not lf: add[y, x] += 1
                elif not dn or not rt: add[y, x] -= 1
        idx += add
    # 4) dark seams where two different materials meet (both sides >=2px deep)
    if seams:
        for y in range(h-1):
            for x in range(w-1):
                for (yy, xx) in ((y+1, x), (y, x+1)):
                    m1, m2 = mat[y, x], mat[yy, xx]
                    if m1 < 0 or m2 < 0 or m1 == m2 or keep[y, x] or keep[yy, xx]: continue
                    if m1 in ex or m2 in ex: continue
                    # thickness of the lower/right region along the step direction
                    dy, dx = yy-y, xx-x
                    y2, x2 = yy+dy, xx+dx
                    y0, x0 = y-dy, x-dx
                    thick2 = 0 <= y2 < h and 0 <= x2 < w and mat[y2, x2] == m2
                    thick1 = 0 <= y0 < h and 0 <= x0 < w and mat[y0, x0] == m1
                    if thick1 and thick2:
                        idx[yy, xx] = min(idx[yy, xx], 0) if False else idx[yy, xx]
                        mat_dark = mat[yy, xx]
                        idx[yy, xx] = max(0, min(idx[yy, xx]-2, 1))
    return render(a, mat, idx, keep)
