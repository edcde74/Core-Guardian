from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
p=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',28)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21)
items=[('00_original_environment.png','현재 원본 · A 원복'),('01_body_only.png','① 본체 밝기만 보강'),('02_body_dark_edge.png','② 본체 + 어두운 1px 윤곽'),('03_body_light_edge.png','③ 본체 + 밝은 1px 윤곽')]
out=Image.new('RGB',(2560,1600),(22,25,29));d=ImageDraw.Draw(out)
detail=Image.new('RGB',(2560,1360),(22,25,29));dd=ImageDraw.Draw(detail)
for i,(file,title) in enumerate(items):
    im=Image.open(p/file);assert im.size==(1920,1080)
    x=i%2*1280;y=i//2*800
    d.text((x+20,y+10),title,font=font,fill='white');d.text((x+20,y+50),'같은 전투 시점 · 배경/③ 색보정 동일',font=small,fill=(180,192,204))
    out.paste(im.resize((1280,720),Image.Resampling.LANCZOS),(x,y+80))
    yy=i//2*680
    dd.text((x+20,yy+10),title,font=font,fill='white');dd.text((x+20,yy+50),'동일 영역 · 2배 Nearest 확대',font=small,fill=(180,192,204))
    detail.paste(im.crop((80,450,720,750)).resize((1280,600),Image.Resampling.NEAREST),(x,yy+80))
out.save(p/'comparison_B_full.png');detail.save(p/'comparison_B_detail.png')
print('Created full and detail comparisons')
