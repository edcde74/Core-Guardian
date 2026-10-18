from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;DEV=HERE.parent
points=json.loads((DEV/'shadow_blunt_preview_20261018/capture.json').read_text(encoding='utf-8-sig'))['points']
approved=Image.open(DEV/'shadow_reference_refine_20261018/marked_fix/corrected_on_actual_originals.png').convert('RGB')
actual=Image.open(HERE/'applied_idle_actual.png').convert('RGB')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
board=Image.new('RGB',(2320,920),'#20252b');d=ImageDraw.Draw(board)
d.text((12,8),'승인 정지 시안 / 실제 적용 코드 렌더 · 동일 Nearest 2배 · Overlay HUD 미포함',font=font,fill='white')
checks=[];delta=np.any(np.asarray(approved)!=np.asarray(actual),axis=2)
for i,p in enumerate(points):
    x,y=round(p['x']),round(p['y']);box=(x-70,y-150,x+70,y+30)
    lx=(i%4)*580;ty=50+(i//4)*435
    d.text((lx+8,ty),p['name'],font=font,fill='#eed99f')
    for j,(im,label) in enumerate(((approved,'승인 시안'),(actual,'실제 적용 렌더'))):
        d.text((lx+8+j*290,ty+28),label,font=small,fill='white')
        board.paste(im.crop(box).resize((280,360),Image.Resampling.NEAREST),(lx+5+j*290,ty+55))
    mask=np.asarray(Image.open(DEV/'shadow_blunt_preview_20261018'/(p['role']+'_body_mask.png')).getchannel('R'))>254
    region=delta[p['maskY']:p['maskY']+p['maskHeight'],p['maskX']:p['maskX']+p['maskWidth']]
    checks.append(dict(role=p['role'],opaque_body_diff_pixels=int(region[mask].sum())))
board.save(HERE/'approved_vs_applied_2x.png')
(HERE/'idle_comparison.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8',newline='\n')
print(checks)
