from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).parent
board=Image.new('RGB',(1500,1700),'#24282e')
d=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21)
groups=[('처음 제작한 병사 원본 · 5배 확대','Assets/Art/ScreenTests/20260930/Sprites', ['enemy_attack.png','enemy_defense.png','enemy_buff.png'],0),('현재 게임의 수정 병사 · 5배 확대','Assets/Art/ScreenTests/20260930/Revision2/Sprites',['enemy_attack_v2.png','enemy_defense_v2.png','enemy_buff_v2.png'],570)]
for title,folder,names,y in groups:
    d.text((25,y+15),title,font=font,fill='white')
    for j,name in enumerate(names):
        im=Image.open(ROOT/folder/name).convert('RGBA')
        dims=im.size
        im=im.resize((im.width*5,im.height*5),Image.Resampling.NEAREST)
        board.paste(im,(250+j*500-im.width//2,y+535-im.height),im)
        d.text((90+j*500,y+540),f'{["공격형","수비형","버프형"][j]} · {dims[0]}×{dims[1]}px',font=small,fill='#ddd8d0')
    d.line((20,y+568,1480,y+568),fill='#62676c',width=2)
d.text((25,1155),'새 보스 V2 · 몸 비율/색감 비교 (게임 픽셀 정규화 전)',font=font,fill='white')
sheet=Image.open(OUT/'boss_hammer_pixel_v2.png').convert('RGBA')
for j,rect in enumerate([(0,0,817,sheet.height),(817,0,sheet.width,sheet.height)]):
    im=sheet.crop(rect)
    bbox=im.getchannel('A').getbbox()
    im=im.crop(bbox)
    im=im.resize((round(im.width*370/im.height),370),Image.Resampling.NEAREST)
    board.paste(im,(500+j*500-im.width//2,1600-im.height),im)
    d.text((380+j*500,1615),['보스 일반','보스 분노'][j],font=font,fill='#ddd8d0')
d.text((25,1660),'위 두 줄은 동일한 픽셀 확대 배율. 보스는 형태 비교용 크기이며 실제 게임 크기가 아닙니다.',font=small,fill='#aeb7c0')
board.save(OUT/'original_soldiers_vs_boss_v2.png')
print(OUT/'original_soldiers_vs_boss_v2.png')
