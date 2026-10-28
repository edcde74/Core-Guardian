import numpy as np
from scipy import ndimage
def qc(a, name, allow_holes=False):
    m = a[:, :, 3] > 0
    issues = []
    # isolated pixels: opaque with 0 opaque 4-neighbours
    n4 = np.zeros(m.shape, int)
    n4[1:] += m[:-1]; n4[:-1] += m[1:]; n4[:, 1:] += m[:, :-1]; n4[:, :-1] += m[:, 1:]
    iso = m & (n4 == 0)
    if iso.sum(): issues.append(f'떠다니는 점 {int(iso.sum())} @ {list(zip(*np.where(iso)))[:4]}')
    # 1px gaps: transparent with opaque on both left-right or up-down
    gap = (~m) & ((np.roll(m, 1, 1) & np.roll(m, -1, 1)) | (np.roll(m, 1, 0) & np.roll(m, -1, 0)))
    gap[0, :] = gap[-1, :] = False; gap[:, 0] = gap[:, -1] = False
    # enclosed transparent regions
    lab, n = ndimage.label(~m)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
    enc = [i for i in range(1, n+1) if i not in border]
    encpx = sum(int((lab == i).sum()) for i in enc)
    if not allow_holes:
        if enc: issues.append(f'갇힌 빈칸 {len(enc)}곳 {encpx}px')
        g = gap & ~np.isin(lab, enc) if enc else gap
        if gap.sum(): issues.append(f'1px 틈 {int(gap.sum())} @ {list(zip(*np.where(gap)))[:4]}')
    # edge without dark outline: opaque pixel touching transparent whose luminance is high
    edge = m & (n4 < 4)
    lum = a[:, :, :3].astype(int).sum(2)
    cyanring = (a[:, :, 0] == 156) & (a[:, :, 1] == 244) & (a[:, :, 2] == 255)
    bright_edge = edge & (lum > 200) & ~cyanring
    # count only boundary pixels touching the outer (border-connected) transparent region
    outer = np.isin(lab, list(border - {0}))
    touch = np.zeros_like(m)
    touch[1:] |= outer[:-1]; touch[:-1] |= outer[1:]; touch[:, 1:] |= outer[:, :-1]; touch[:, :-1] |= outer[:, 1:]
    be = bright_edge & touch
    if be.sum(): issues.append(f'외곽선 빠진 가장자리 {int(be.sum())} @ {list(zip(*np.where(be)))[:4]}')
    return issues

OUTC = np.array([10, 11, 20, 255], np.uint8)
def finalize(a, hole_max=12):
    """fill 1px gaps and tiny enclosed holes with the dark outline colour (repeat until stable)"""
    a = a.copy()
    for _ in range(4):
        m = a[:, :, 3] > 0
        gap = (~m) & ((np.roll(m, 1, 1) & np.roll(m, -1, 1)) | (np.roll(m, 1, 0) & np.roll(m, -1, 0)))
        gap[0, :] = gap[-1, :] = False; gap[:, 0] = gap[:, -1] = False
        lab, n = ndimage.label(~m)
        border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
        small = np.zeros_like(m)
        for i in range(1, n+1):
            if i not in border and (lab == i).sum() <= hole_max: small |= (lab == i)
        fill = gap | small
        if not fill.any(): break
        a[fill] = OUTC
    return a
