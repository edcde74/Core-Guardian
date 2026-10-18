from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).parent
data = json.loads((folder / 'capture.json').read_text(encoding='utf-8-sig'))
before = Image.open(folder / 'current_actual.png').convert('RGB')
after = Image.open(folder / 'shadow_only_actual.png').convert('RGB')
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24)
small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)
board = Image.new('RGB', (2320, 880), '#20252b')
draw = ImageDraw.Draw(board)
draw.text((20, 10), '실제 원본 렌더 + 별도 그림자 · 본체 재생성 없음 · 같은 배율 2배 확대', font=font, fill='white')
diff = Image.new('RGB', before.size)
from PIL import ImageChops
diff = ImageChops.difference(before, after)
results = []
for index, point in enumerate(data['points']):
    x, y = round(point['x']), round(point['y'])
    box = (x - 70, y - 150, x + 70, y + 30)
    col, row = index % 4, index // 4
    left, top = col * 580, 55 + row * 410
    draw.text((left + 10, top), point['name'], font=font, fill='#f2d99b')
    draw.text((left + 10, top + 30), '현재', font=small, fill='white')
    draw.text((left + 300, top + 30), '그림자만 변경', font=small, fill='white')
    board.paste(before.crop(box).resize((280, 360), Image.Resampling.NEAREST), (left + 5, top + 54))
    board.paste(after.crop(box).resize((280, 360), Image.Resampling.NEAREST), (left + 295, top + 54))
    # All upper-object pixels (including empty surroundings) stay byte-identical.
    upper = diff.crop((x - 65, y - 150, x + 65, y - 16))
    changed_upper = sum(pixel != (0, 0, 0) for pixel in upper.getdata())
    results.append({'role': point['role'], 'upper_region_changed_pixels': changed_upper})
    if point['role'] == 'Player':
        player = Image.new('RGB', (640, 480), '#20252b')
        pd = ImageDraw.Draw(player)
        pd.text((15, 10), '실제 플레이어 · 현재 / 그림자만 변경', font=font, fill='white')
        pb = (x-40, y-80, x+40, y+25)
        player.paste(before.crop(pb).resize((320, 420), Image.Resampling.NEAREST), (0, 50))
        player.paste(after.crop(pb).resize((320, 420), Image.Resampling.NEAREST), (320, 50))
        player.save(folder / 'player_shadow_only_4x.png')
board.save(folder / 'all_originals_shadow_only_2x.png')
(folder / 'image_checks.json').write_text(json.dumps(results, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(results, ensure_ascii=False))
