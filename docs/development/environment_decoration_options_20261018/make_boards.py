"""Layout/crop only: no painting, recoloring, masking or alteration of concept art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json

root = Path(__file__).resolve().parent
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
names = [('A_restrained_nature.png', 'A · 절제된 자연 장식'),
         ('B_abandoned_ruins.png', 'B · 오래 방치된 폐허'),
         ('C_nature_magitech.png', 'C · 자연과 마법공학')]
imgs = [Image.open(root / n).convert('RGB') for n, _ in names]
assert all(im.size == (1672, 941) for im in imgs)
base = Image.open(root.parent / 'object_emphasis_B2_applied_20261018/applied_combat.png').convert('RGB')
assert base.size == (1920, 1080)
bg = (23, 26, 29)
board = Image.new('RGB', (1704, 1084), bg)
d = ImageDraw.Draw(board)
d.text((16, 9), '환경 장식 비교 · 원본 / A / B / C · 생성 시안, 게임 미적용', font=font, fill='white')
for i, (im, title) in enumerate([(base, '현재 게임 캡처 · 장식 추가 전')] + list(zip(imgs, [t for _, t in names]))):
    x, y = 16 + (i % 2)*844, 52 + (i // 2)*512
    d.text((x, y), title, font=font, fill='white')
    board.paste(im.resize((828, 466), Image.Resampling.LANCZOS), (x, y+36))
board.save(root / 'comparison_original_ABC.png')

zooms = Image.new('RGB', (1952, 1144), bg)
zd = ImageDraw.Draw(zooms)
zd.text((16, 10), '동일 구역 2배 확대 · 최근접 보간 · 위: 좌하단 외곽 / 아래: 코어 주변', font=font, fill='white')
boxes = [(20, 590, 340, 840), (865, 445, 1185, 695)]
for col, (im, (_, title)) in enumerate(zip(imgs, names)):
    single = Image.new('RGB', (1296, 554), bg)
    sd = ImageDraw.Draw(single)
    for row, box in enumerate(boxes):
        crop = im.crop(box).resize((640,500), Image.Resampling.NEAREST)
        x, y = 16+col*648, 54+row*540
        zd.text((x, y), title + (' · 외곽' if row == 0 else ' · 코어'), font=font, fill='white')
        zooms.paste(crop, (x, y+34))
        sx = 8+row*648
        sd.text((sx, 8), title + (' · 외곽' if row == 0 else ' · 코어'), font=small, fill='white')
        single.paste(crop, (sx, 42))
    single.save(root / (names[col][0][0] + '_detail_2x.png'))
zooms.save(root / 'details_ABC_2x.png')
manifest = {'reference': 'object_emphasis_B2_applied_20261018/applied_combat.png',
    'reference_size': list(base.size), 'concept_size': [1672,941],
    'mode': 'built-in imagegen precise-object-edit',
    'detail_crop_boxes': boxes, 'detail_zoom': 2, 'detail_resampling': 'nearest',
    'note': 'Generated visual concepts, not Unity renders. No gameplay or approved asset edits. Generated pixels are not a 1:1 preservation guarantee.'}
(root / 'manifest.json').write_bytes((json.dumps(manifest, ensure_ascii=False, indent=2)+'\n').encode('utf-8'))
for p in sorted(root.glob('*.png')):
    with Image.open(p) as im:
        im.verify()
    with Image.open(p) as im:
        print(p.name, im.size)
