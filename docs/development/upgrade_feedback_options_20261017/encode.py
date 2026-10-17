from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont, ImageChops

root = Path(__file__).resolve().parent
meta = json.loads((root / 'capture.json').read_text(encoding='utf-8-sig'))
font_path = root.parents[2] / 'Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf'
title = ImageFont.truetype(str(font_path), 23)
small = ImageFont.truetype(str(font_path), 17)
names = [('A · 상승 황동 링', '바닥에서 두 번 퍼지며 상승 · 약 0.55초'),
         ('B · 청록빛 본체 스캔', '본체 안쪽만 아래에서 위로 통과 · 약 0.55초'),
         ('C · 상승 황동 조각', '작은 조각과 위쪽 상승 표식 · 약 0.60초')]
labels = ['플레이어', '단일 포탑', '멀티 포탑', '지원 포탑']
report = []
for option, (heading, detail) in enumerate(names, 1):
    frames = []
    paths = sorted((root / f'frames_{option}').glob('*.png'))
    assert len(paths) == 48
    for path in paths:
        source = Image.open(path).convert('RGB')
        board = Image.new('RGB', (1056, 390), '#141b24')
        draw = ImageDraw.Draw(board)
        draw.text((16, 8), heading, font=title, fill='#eddbb2')
        draw.text((16, 39), detail, font=small, fill='#c4ccd5')
        for i, center in enumerate(meta['centers']):
            x, y = round(center['x']), round(meta['height'] - center['y'])
            crop = source.crop((x-66, y-74, x+66, y+74)).resize((264,296),Image.Resampling.NEAREST)
            board.paste(crop,(i*264,90))
            draw.text((i*264+16,66),labels[i],font=small,fill='#ffffff')
        frames.append(board)
    frames[0].save(root / f'option_{option}.gif',save_all=True,append_images=frames[1:],duration=33,loop=0,optimize=False)
    frames[16].save(root / f'option_{option}.png')
    baseline = Image.open(paths[0]).convert('RGB')
    changed = sum(ImageChops.difference(baseline,Image.open(p).convert('RGB')).getbbox() is not None for p in paths)
    assert changed > 8
    report.append({'option':option,'frames':48,'changed_from_baseline':changed,'scale':'native game pixels cropped then nearest 2x','preview_only':True})
(root/'gif_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
