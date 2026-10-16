"""Label and encode actual Unity captures; does not author/edit game sprite pixels."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parent
frames = []
for path in sorted((folder / 'frames').glob('*.png')):
    image = Image.open(path).convert('RGB')
    out = Image.new('RGB', (image.width, image.height + 50), '#282c31')
    out.paste(image, (0, 50))
    draw = ImageDraw.Draw(out)
    for col, text in enumerate(('ORIGINAL', 'E', 'NE', 'N', 'NW', 'W', 'SW', 'S', 'SE')):
        draw.text((int((col+.5)*image.width/9), 10), text, fill='white', anchor='mt')
    draw.text((10, 31), 'Unity native4x | top: walk / bottom: walk + Fire | review playback 0.5x', fill='#b9d3e9')
    frames.append(out)
frames[0].save(folder / 'eight_directions.webp', save_all=True, append_images=frames[1:], duration=42, loop=0, lossless=True)
frames[6].save(folder / 'unity_comparison.png')
check = Image.open(folder / 'eight_directions.webp')
duration = 0
for i in range(check.n_frames):
    check.seek(i)
    check.load()
    duration += check.info['duration']
print(f'Unity input frames={len(frames)}; encoded frames={check.n_frames} (identical adjacent frames merged); size={check.size}; duration={duration}ms; lossless WebP')
assert len(frames) == 48 and duration == 2016
