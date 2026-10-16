from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
font = ImageFont.truetype(str(root.parents[2] / 'Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf'), 16)
frames = []
for i in range(48):
    before = Image.open(root / 'before_frames' / f'{i:03}.png').convert('RGB')
    after = Image.open(root / 'after_frames' / f'{i:03}.png').convert('RGB')
    out = Image.new('RGB', (672, 520), '#282c31')
    draw = ImageDraw.Draw(out)
    for x, label in enumerate(('원본', '수정 전', '수정 후')):
        draw.text((112+x*224, 9), label, anchor='mt', font=font, fill='white')
    for row, direction in enumerate((7, 1)):  # S and E, top row is ordinary movement only
        for column, (image, cell) in enumerate(((after, 0), (before, direction), (after, direction))):
            crop = image.crop((int(cell*1920/9), 0, int((cell+1)*1920/9), 240))
            out.paste(crop, (column*224+5, 38+row*240))
    frames.append(out)
for name, duration in (('before_after.webp', 17), ('before_after_slow.webp', 34)):
    frames[0].save(root / name, save_all=True, append_images=frames[1:], duration=duration, loop=0, lossless=True)
frames[5].save(root / 'comparison.png')
check = Image.open(root / 'before_after.webp')
total = 0
for i in range(check.n_frames):
    check.seek(i)
    check.load()
    total += check.info['duration']
report = dict(unityInputFrames=48, encodedFrames=check.n_frames, durationMs=total,
              nativeScale=4, displacementProfileSeconds=.12, tapIntervalSeconds=.4,
              note='Fixed-position Unity preview with actual tile-step velocity, not a human gameplay recording')
(root / 'render_checks.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(report))
assert total == 816
