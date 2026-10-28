import h1, h2, icons, ui, numpy as np, os, json
from qc import qc, finalize
from parts import Layer, glow_outline, R, load, ART
from PIL import Image
O = 'out'; os.makedirs(O, exist_ok=True)
def save(a, name): Image.fromarray(a).save(f'{O}/{name}.png')
meta = {'buildings': {}, 'h4': [], 'h5': [], 'h6': []}
problems = []
for key, mod in (('h1', h1), ('h2', h2)):
    for code, name, fn in mod.VARIANTS:
        a = finalize(fn().a)
        F = Layer(a.shape[1], a.shape[0]); F.a = a
        g = finalize(glow_outline(F, R('cyan', 4)).a)
        ys = np.where(a[:, :, 3] > 0)[0]; y0 = max(0, ys.min()-2)
        save(a[y0:], f'{key}{code}'); save(g[y0:], f'{key}{code}_hl')
        problems += [(f'{key}{code}', qc(a, name)), (f'{key}{code}_hl', qc(g, name))]
        meta['buildings'][f'{key}{code}'] = name
for k, nm, fns, labels in icons.H5:
    for i, f in enumerate(fns):
        a = finalize(f().a); save(a, f'h5_{k}_{"ABC"[i]}'); problems.append((f'h5_{k}_{"ABC"[i]}', qc(a, nm)))
    meta['h5'].append([k, nm, labels])
hp = finalize(icons.hp_A().a)
for code, nm, fn in ui.H4:
    for t in ('silver', 'gold', 'platinum'):
        a = fn(t).a; save(a, f'h4{code}_{t}'); problems.append((f'h4{code}_{t}', qc(a, nm, allow_holes=True)))
    meta['h4'].append([code, nm])
for code, nm, fn in ui.H6:
    T, Hd = fn(); save(T.a, f'h6{code}_track'); save(Hd.a, f'h6{code}_handle')
    problems += [(f'h6{code}', qc(T.a, nm) + qc(Hd.a, nm))]
    meta['h6'].append([code, nm])
for i, f in enumerate((0, 0.4, 0.75)):
    ui.h7_state(f).save(f'{O}/h7_{i}.png')
# references
for nm, p in (('ref_single', 'Characters/Towers/Sprites/tower_single.png'), ('ref_core', 'Characters/Core/Sprites/core.png'),
              ('ref_player', 'Characters/Player/Sprites/player.png'), ('ref_stat', 'UI/Resources/CoreGuardian/Hud/StatDamage.png'),
              ('ref_cardgold', 'UI/Resources/CoreGuardian/Hud/CardBackGold.png'), ('ref_panel', 'UI/Resources/CoreGuardian/Hud/Panel.png')):
    Image.open(ART+p).convert('RGBA').save(f'{O}/{nm}.png')
bad = [(n, p) for n, p in problems if p]
json.dump(meta, open(f'{O}/meta.json', 'w'), ensure_ascii=False)
print('files', len(os.listdir(O)), 'qc problems:', bad)
