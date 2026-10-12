from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).parent
board = Image.new('RGB', (1500, 990), '#24282e')
d = ImageDraw.Draw(board)
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 25)
small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)
d.text((30, 16), '현재 게임 병사 — 원본 픽셀을 6배 확대 (Nearest)', font=font, fill='white')
sprites = [('공격형 · 42×64px', 'enemy_attack_v2.png'), ('수비형 · 33×48px', 'enemy_defense_v2.png'), ('버프형 · 35×80px', 'enemy_buff_v2.png')]
for i, (label, name) in enumerate(sprites):
    im = Image.open(ROOT / 'Assets/Art/ScreenTests/20260930/Revision2/Sprites' / name).convert('RGBA')
    im = im.resize((im.width*6, im.height*6), Image.Resampling.NEAREST)
    board.paste(im, (250+i*500-im.width//2, 552-im.height), im)
    d.text((60+i*500, 563), label, font=font, fill='#e1e1e1')
d.line((20, 615, 1480, 615), fill='#64676c', width=2)
d.text((30, 626), '새 보스 시안 — 형태 비교용 축소, 최종 게임 스프라이트 아님', font=font, fill='white')
concept = Image.open(OUT / 'boss_concepts_v1.png').convert('RGB')
for i, (rect, label) in enumerate([((105,76,608,531),'A · 망치'), ((603,72,1040,533),'B · 도끼'), ((1044,91,1535,535),'C · 금속 주먹')]):
    im = concept.crop(rect)
    im = im.resize((round(im.width*255/im.height), 255), Image.Resampling.NEAREST)
    board.paste(im, (250+i*500-im.width//2, 672))
    d.text((60+i*500, 940), label, font=font, fill='#e1e1e1')
board.save(OUT / 'soldiers_vs_boss_v1.png')
print(OUT / 'soldiers_vs_boss_v1.png')
