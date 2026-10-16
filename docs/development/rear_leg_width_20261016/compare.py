from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
frames = []
down_diff = 0
for i in range(48):
    before = Image.open(root / 'before_frames' / f'{i:03}.png').convert('RGB')
    after = Image.open(root / 'after_frames' / f'{i:03}.png').convert('RGB')
    down_diff += sum(a != b for a, b in zip(before.crop((0, 0, 960, 288)).getdata(), after.crop((0, 0, 960, 288)).getdata()))
    # Same native3x rendering, one full rear-facing unit, before/after at identical phase.
    a = before.crop((0, 288, 240, 576))
    b = after.crop((0, 288, 240, 576))
    out = Image.new('RGB', (480, 320), '#23282e')
    out.paste(a, (0, 32))
    out.paste(b, (240, 32))
    draw = ImageDraw.Draw(out)
    draw.text((90, 12), 'BEFORE', fill='white')
    draw.text((332, 12), 'AFTER', fill='white')
    frames.append(out)
frames[0].save(root / 'before_after.webp', save_all=True, append_images=frames[1:], duration=42, loop=0, lossless=True)
frames[0].save(root / 'comparison.png')
print(f'Unity input frames={len(frames)}; Down rendered pixel differences={down_diff}')
assert down_diff == 0
