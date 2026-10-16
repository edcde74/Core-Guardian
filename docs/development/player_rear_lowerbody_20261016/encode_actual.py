from pathlib import Path
import json
import hashlib
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
report = {}
for label in ('before', 'after'):
    state = json.loads((root / f'{label}_sequence.json').read_text(encoding='utf-8-sig'))
    moved = [s for s in state['samples'] if s['y'] > state['start'][1] + .0001]
    assert moved and all(s['direction'] == 2 for s in moved)
    assert all(abs(s['x'] - state['start'][0]) < .0001 for s in state['samples'])
    assert abs(state['end'][1] - state['start'][1] - .6) < .001
    report[label] = {'frames': len(state['samples']), 'dy': state['end'][1] - state['start'][1], 'dx': state['end'][0] - state['start'][0], 'allMovedFramesD2': True}
frames = []
for i in range(36):
    out = Image.new('RGB', (480, 392), '#282c31')
    for col, label in enumerate(('before', 'after')):
        source = Image.open(root / f'{label}_frames' / f'{i:03}.png').convert('RGB')
        crop = source.crop((542, 420, 622, 540)).resize((240, 360), Image.Resampling.NEAREST)
        out.paste(crop, (col * 240, 32))
        ImageDraw.Draw(out).text((col * 240 + 12, 8), label.upper() + ' - actual W', fill='white')
    frames.append(out)
frames[0].save(root / 'actual_w_comparison.webp', save_all=True, append_images=frames[1:], duration=17, loop=0, lossless=True)
frames[-1].save(root / 'actual_stopped_comparison.png')
original = root.parents[2] / 'Assets/Art/ScreenTests/20260930/Sprites/player.png'
report['originalSHA256'] = hashlib.sha256(original.read_bytes()).hexdigest()
assert report['originalSHA256'] == '4af7abc22e50834381367f04c8e9add3517d5831c9b6bf29f989eb14a4ae61e3'
(root / 'capture_checks.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report))
