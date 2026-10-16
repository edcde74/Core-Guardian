from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(__file__).resolve().parent
frames = []
for index in range(24):
    canvas = Image.new('RGB', (864, 496), '#252a30')
    draw = ImageDraw.Draw(canvas)
    draw.text((12, 8), 'BEFORE - fixed sole marks', fill='white')
    draw.text((444, 8), 'AFTER - central shadow', fill='white')
    for col, label in enumerate(('before', 'after')):
        source = Image.open(folder / label / f'{index:03}.png')
        for d in range(4):
            x = d * 288 + 64
            tile = source.crop((x, 40, x + 160, 264))
            # Same source pixel scale on both sides; crop only, no resampling.
            canvas.paste(tile, (col * 432 + (d % 2) * 212 + 28, 28 + (d // 2) * 232))
    frames.append(canvas)
frames[0].save(folder / 'comparison.png')
frames[0].save(folder / 'comparison.webp', save_all=True, append_images=frames[1:], duration=42, loop=0, lossless=True)
