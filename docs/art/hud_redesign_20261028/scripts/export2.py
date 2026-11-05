import h1v2, h2, ui, icons, numpy as np, os, json
from render_q import final
from quant import quantize
from qc import qc, finalize
from parts import Layer, glow_outline, R
from PIL import Image
O = 'out2/'
probs = []
def save(a, n): Image.fromarray(a).save(O+n+'.png')
def bld(key, fn, name):
    a = final(fn())
    F = Layer(a.shape[1], a.shape[0]); F.a = a
    g = finalize(glow_outline(F, R('cyan', 4)).a)
    ys = np.where(a[:, :, 3] > 0)[0]; y0 = max(0, ys.min()-2)
    save(a[y0:], key); save(g[y0:], key+'_hl')
    probs.append((key, qc(a, name))); probs.append((key+'_hl', qc(g, name)))
for c, n, f in h1v2.VARIANTS: bld('h1'+c, f, n)
bld('h2A', h2.workbench, '작업대')
for t in ('silver', 'gold', 'platinum'):
    a = finalize(quantize(ui.frame_A(t).a, amp_noise=0.25)) if False else quantize(ui.frame_A(t).a, amp_noise=0.25)
    save(a, 'h4A_'+t); probs.append(('h4A_'+t, qc(a, 'h4', allow_holes=True)))
Image.fromarray(finalize(icons.hp_A().a)).save(O+'icon_hp.png')
print([p for p in probs if p[1]])
