import numpy as np
from quant import quantize
from qc import finalize
FIRE = [(255, 236, 150), (255, 176, 50), (226, 88, 30), (200, 50, 25), (40, 18, 46), (58, 30, 68), (80, 46, 94), (120, 80, 140), (226, 204, 156), (244, 228, 188), (176, 150, 104), (52, 40, 70), (82, 66, 104), (30, 22, 42), (10, 8, 16)]
def qicon(L, extra_keep=None):
    a = finalize(L.a)
    keep = L.keep.copy()
    rgb = a[:, :, :3].astype(int)
    for c in FIRE: keep |= (np.abs(rgb - np.array(c)).sum(2) < 6)      # flame colours stay
    keep |= (rgb.min(2) > 225)                                         # pure highlights stay
    if extra_keep is not None: keep |= extra_keep
    q = quantize(a, keep=keep & (a[:, :, 3] > 0), amp_light=1.4, amp_noise=0.27)
    return finalize(q, 20)
