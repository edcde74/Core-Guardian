from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
state = json.loads((root / 'actual_w_sequence.json').read_text(encoding='utf-8-sig'))
moving = [s for s in state['samples'] if s['y'] > state['start'][1] + .0001]
assert moving and all(s['direction'] == 2 for s in moving)
assert all(abs(s['x'] - state['start'][0]) < .0001 for s in state['samples'])
assert abs(state['end'][1] - state['start'][1] - .6) < .001
font = ImageFont.truetype(str(root.parents[2] / 'Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf'), 16)
frames = []
for path in sorted((root / 'actual_w_frames').glob('*.png')):
    frame = Image.open(path).convert('RGB').crop((542, 420, 622, 540)).resize((240, 360), Image.Resampling.NEAREST)
    out = Image.new('RGB', (240, 392), '#282c31')
    out.paste(frame, (0, 32))
    ImageDraw.Draw(out).text((120, 8), '실제 W 입력 · 상행', font=font, fill='white', anchor='mt')
    frames.append(out)
frames[0].save(root / 'actual_w.webp', save_all=True, append_images=frames[1:], duration=17, loop=0, lossless=True)
report = dict(input='InputSystem virtual Keyboard W state, normal player Update',
              frames=len(frames), upDisplacement=state['end'][1]-state['start'][1],
              horizontalDisplacement=state['end'][0]-state['start'][0],
              allMovingFramesRearDirection=True, render='actual scene Game camera, original camera settings',
              display='nearest 3x crop of 1600x900 captures; full PNGs retained')
(root / 'actual_w_checks.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(report))
