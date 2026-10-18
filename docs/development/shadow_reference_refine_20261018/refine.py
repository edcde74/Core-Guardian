"""Re-register visible photo extractions; never paint missing shadow or body pixels."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SRC = HERE.parent / 'shadow_reference_extract_20261018'
CAP = HERE.parent / 'shadow_blunt_preview_20261018'
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)
small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 16)
meta = json.loads((CAP/'capture.json').read_text(encoding='utf-8-sig'))
records = json.loads((SRC/'extraction.json').read_text(encoding='utf-8-sig'))['objects']
current = Image.open(CAP/'current_actual.png').convert('RGBA')
previous = Image.open(SRC/'extracted_shadow_on_actual_originals.png').convert('RGBA')
photo = Image.open(SRC/'reference_user_selected.png').convert('RGB')
layer = Image.new('RGBA', current.size)
out = []

def runs(values):
    return np.split(values, np.where(np.diff(values)>1)[0]+1)

for p, r in zip(meta['points'], records):
    role = p['role']
    mask = np.asarray(Image.open(CAP/(role+'_body_mask.png')).getchannel('R')) > 254
    ys, xs = np.where(mask)
    # Actual opaque sole/base, excluding the union texture's transparent padding.
    bottom = int(ys.max()+p['maskY'])
    inset = 8 if role in ('Attack','Defense','Buff','Player') else 14
    ty = bottom-inset
    tx = int(np.where(mask[ty-p['maskY']])[0].max()+p['maskX'])-1
    m = np.asarray(Image.open(SRC/(role+'_visible_mask.png')))>0
    sy = r['source_anchor'][1]-(14 if role in ('Attack','Defense','Buff','Player') else 22)
    source_runs = runs(np.where(m[sy-r['shadow_box'][1]])[0])
    # The exposed right-hand lobe, excluding separate left-foot remnants.
    right_run = max(source_runs, key=lambda a: a[-1])
    sx = int(right_run[0]+r['shadow_box'][0])
    crop = Image.open(SRC/(role+'_shadow_extracted.png')).convert('RGBA')
    scale = r['native_scale']
    size = (max(1,round(crop.width*scale)),max(1,round(crop.height*scale)))
    scaled = crop.resize(size,Image.Resampling.NEAREST)
    pos = (round(tx-(sx-r['shadow_box'][0])*scale),round(ty-(sy-r['shadow_box'][1])*scale))
    layer.alpha_composite(scaled,pos)
    scaled.save(HERE/(role+'_registered_shadow.png'))
    # The source selection itself is byte-identical; no silhouette regeneration.
    (HERE/(role+'_source_shadow.png')).write_bytes((SRC/(role+'_shadow_extracted.png')).read_bytes())
    out.append(dict(role=role,source_contact=[sx,sy],target_contact=[tx,ty],opaque_bottom=bottom,
                    scale=scale,size=size,previous_position=r['native_position'],position=pos,
                    translation=[pos[i]-r['native_position'][i] for i in range(2)],
                    source_sha256=hashlib.sha256((SRC/(role+'_shadow_extracted.png')).read_bytes()).hexdigest()))

composite = Image.alpha_composite(current,layer)
checks=[]
for p in meta['points']:
    m=Image.open(CAP/(p['role']+'_body_mask.png')).getchannel('R')
    full=Image.new('L',current.size);full.paste(m,(p['maskX'],p['maskY']))
    composite=Image.composite(current,composite,full)
delta=np.any(np.asarray(composite.convert('RGB'))!=np.asarray(current.convert('RGB')),axis=2)
for p in meta['points']:
    m=np.asarray(Image.open(CAP/(p['role']+'_body_mask.png')).getchannel('R'))>254
    region=delta[p['maskY']:p['maskY']+m.shape[0],p['maskX']:p['maskX']+m.shape[1]]
    checks.append(dict(role=p['role'],opaque_body_changed_pixels=int(region[m].sum())))
assert all(c['opaque_body_changed_pixels']==0 for c in checks)
composite.save(HERE/'registered_on_actual_originals.png')
layer.save(HERE/'separate_registered_layer.png')

board=Image.new('RGB',(2320,920),'#20252b');d=ImageDraw.Draw(board)
d.text((15,8),'실제 원본 정지 합성 · 왼쪽 이전 추출 / 오른쪽 접점 보정 · 모두 Nearest 2배',font=font,fill='white')
for i,p in enumerate(meta['points']):
    x,y=round(p['x']),round(p['y']);box=(x-70,y-150,x+70,y+30)
    left,top=(i%4)*580,50+(i//4)*435
    d.text((left+8,top),p['name'],font=font,fill='#eed99f')
    for j,(im,label) in enumerate(((previous,'이전 추출 합성'),(composite,'접점 보정 시안'))):
        d.text((left+8+j*290,top+28),label,font=small,fill='white')
        board.paste(im.crop(box).resize((280,360),Image.Resampling.NEAREST).convert('RGB'),(left+5+j*290,top+55))
board.save(HERE/'all_before_after_2x.png')

for role in ('Player','Single','Support','Core'):
    p=next(p for p in meta['points'] if p['role']==role)
    r=next(r for r in records if r['role']==role)
    x,y=round(p['x']),round(p['y']);box=(x-60,y-125,x+65,y+30)
    panel=Image.new('RGB',(1500,680),'#20252b');pd=ImageDraw.Draw(panel)
    pd.text((12,8),p['name']+' · 사진은 크기 정규화 참고 / 실제 원본 이전·보정은 동일 4배',font=font,fill='white')
    # Reference photo rescaled with the same recorded conversion, no body used in composite.
    s=r['native_scale'];a=r['source_anchor']
    refbox=(round(a[0]-60/s),round(a[1]-125/s),round(a[0]+65/s),round(a[1]+30/s))
    for j,(im,label) in enumerate(((photo.crop(refbox),'사용자 사진 (참고 전용)'),(previous.crop(box),'실제 원본 · 이전'),(composite.crop(box),'실제 원본 · 접점 보정'))):
        pd.text((j*500+8,42),label,font=small,fill='white')
        panel.paste(im.resize((500,620),Image.Resampling.NEAREST).convert('RGB'),(j*500,65))
    panel.save(HERE/(role.lower()+'_reference_before_after_4x.png'))
    if role=='Player':
        compact=Image.new('RGB',(640,480),'#20252b');cd=ImageDraw.Draw(compact)
        cd.text((10,8),'이전 추출 합성',font=font,fill='white')
        cd.text((330,8),'접점 보정 시안',font=font,fill='white')
        b=(x-40,y-80,x+40,y+25)
        for j,im in enumerate((previous,composite)):
            compact.paste(im.crop(b).resize((320,420),Image.Resampling.NEAREST).convert('RGB'),(j*320,50))
        compact.save(HERE/'player_before_after_4x.png')

# Inspect every resource actually loaded by CoreGuardianPlayerMotion, at a common scale.
parts=('Body','Arm','LeftLeg','RightLeg')
sheet=Image.new('RGB',(1280,420),'#33383e');sd=ImageDraw.Draw(sheet)
for direction in range(8):
    base_x=(direction%4)*320;base_y=(direction//4)*210
    sd.text((base_x+8,base_y+4),f'D{direction} · 동일 Nearest 2배',font=small,fill='white')
    for k,part in enumerate(parts):
        path=ROOT/'Assets/Resources/CoreGuardian/PlayerFacing'/f'D{direction}'/f'D{direction}_{part}.png'
        im=Image.open(path).convert('RGBA')
        cellx=base_x+k*80;celly=base_y+32
        sd.text((cellx+4,celly),('몸통','팔','왼다리','오른다리')[k],font=small,fill='white')
        im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
        sheet.paste(im,(cellx,celly+50),im)
sheet.save(HERE/'player_facing_resources_2x.png')
(HERE/'registration.json').write_text(json.dumps(dict(method='Existing visible-photo masks; right exposed contour to actual opaque body contact; translation only, original scale and alpha retained',objects=out,body_checks=checks),ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(translations=[(r['role'],r['translation']) for r in out],body_checks=checks),ensure_ascii=False))
