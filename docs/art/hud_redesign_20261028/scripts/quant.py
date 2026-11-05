import numpy as np
from scipy import ndimage
from PIL import Image
from parts import RAMP, ART
from pixelize import classify, MATS

# palette from the approved originals
def orig_palette(k=56, seed=0):
    files = ['Characters/Towers/Sprites/tower_single.png', 'Characters/Towers/Sprites/tower_support.png',
             'Characters/Towers/Sprites/tower_multi.png', 'Characters/Core/Sprites/core.png',
             'Characters/Player/Sprites/player.png', 'Characters/Towers/Sprites/tower_frost.png']
    px = []
    for f in files:
        a = np.array(Image.open(ART+f).convert('RGBA')).reshape(-1, 4)
        px.append(a[a[:, 3] > 200][:, :3])
    X = np.concatenate(px).astype(float)
    rng = np.random.default_rng(seed)
    C = X[rng.choice(len(X), k, replace=False)]
    for _ in range(25):
        lab = ((X[:, None] - C[None]) ** 2).sum(2).argmin(1)
        for j in range(k):
            s = X[lab == j]
            if len(s): C[j] = s.mean(0)
    return C
OPAL = orig_palette()
# extra colours the originals lack but our subjects need (wood/leather/red/steam) stay on their ramps
EXTRA = np.array([c[:3] for m in ('wood', 'leather', 'red', 'green') for c in RAMP[m]], float)

def quantize(a, keep=None, light=(-0.7, -0.7), amp_light=1.4, amp_noise=0.55, seed=3, exclude=()):
    h, w = a.shape[:2]
    keep = np.zeros((h, w), bool) if keep is None else keep
    mat, idx = classify(a)
    f = idx.astype(float)
    rng = np.random.default_rng(seed)
    noise = ndimage.gaussian_filter(rng.standard_normal((h, w)), 0.9)
    noise /= (noise.std() + 1e-6)
    lx, ly = light
    for mi in np.unique(mat):
        if mi < 0: continue
        if MATS[mi] in exclude: continue
        region = (mat == mi)
        # connected components so each part is lit on its own
        lab, n = ndimage.label(region)
        for j in range(1, n+1):
            r = lab == j
            if r.sum() < 6: continue
            sm = ndimage.gaussian_filter(r.astype(float), 1.3)
            gy, gx = np.gradient(sm)
            # outward normal = -gradient; lit when normal points to light
            shade = -(gx * lx + gy * ly) / 0.25
            shade = np.clip(shade, -1, 1)
            line = r & (idx <= 1)          # structural dark lines (mortar, planks, seams) keep their place
            body = r & ~line
            f[body] += amp_light * shade[body] + amp_noise * noise[body]
            f[line] += 0.5 * amp_light * np.clip(shade[line], -1, 0)
    out = a.copy()
    for y in range(h):
        for x in range(w):
            if keep[y, x] or mat[y, x] < 0: continue
            m = MATS[mat[y, x]]; ramp = RAMP[m]
            i = int(round(max(0, min(len(ramp)-1, f[y, x]))))
            c = np.array(ramp[i][:3], float)
            if m in ('wood', 'leather', 'red', 'green'):
                out[y, x, :3] = c
            else:
                out[y, x, :3] = OPAL[((OPAL - c) ** 2).sum(1).argmin()]
    return out
