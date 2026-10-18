"""Local contact seam polish of the registered photo shadow; static preview only."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
OUT=HERE/'contact_polish'
OUT.mkdir(exist_ok=True)
CAP=HERE.parent/'shadow_blunt_preview_20261018'
points=json.loads((CAP/'capture.json').read_text(encoding='utf-8-sig'))['points']
base=Image.open(CAP/'current_actual.png').convert('RGBA')
before=Image.open(HERE/'registered_on_actual_originals.png').convert('RGBA')
old=Image.open(HERE/'separate_registered_layer.png').convert('RGBA')
new=old.copy()
allowed_full=np.zeros((old.height,old.width),bool)
records=[]

def expand(mask,radius):
    return np.asarray(Image.fromarray((mask*255).astype('uint8')).filter(ImageFilter.MaxFilter(radius*2+1)))>0

for p in points:
    role=p['role']
    mask=np.asarray(Image.open(CAP/(role+'_body_mask.png')).getchannel('R'))>0
    x0,y0=p['maskX']-8,p['maskY']-8
    w,h=mask.shape[1]+16,mask.shape[0]+16
    body=np.zeros((h,w),bool);body[8:8+mask.shape[0],8:8+mask.shape[1]]=mask
    ys,xs=np.where(body);bottom=int(ys.max())
    # Only the lower foot/base rim. No silhouette around torso or equipment.
    ground=body.copy()
    ground[:bottom-(8 if role in ('Attack','Defense','Buff','Player') else 14)]=False
    near1=expand(ground,1)&~body
    near2=expand(ground,2)&~body&~near1
    local=np.array(old.crop((x0,y0,x0+w,y0+h)))
    alpha=local[:,:,3].copy();shadow=alpha>0
    # Extend only existing extracted pixels by at most one native capture pixel,
    # and only where the real foot/base is at most two pixels away.
    bridge=~shadow&(near1|near2)&expand(shadow,1)
    alpha[bridge&near1]=76
    alpha[bridge&near2]=46
    # A small contact density transition, leaving the outer cast shadow untouched.
    alpha[shadow&near1]=np.minimum(255,alpha[shadow&near1].astype(int)+16)
    alpha[shadow&near2]=np.minimum(255,alpha[shadow&near2].astype(int)+6)
    changed=alpha!=local[:,:,3]
    local[bridge,:3]=(12,10,8)
    local[:,:,3]=alpha
    new.paste(Image.fromarray(local),(x0,y0))
    allowed_full[y0:y0+h,x0:x0+w]|=(near1|near2)
    records.append(dict(role=role,contact_alpha_changed_pixels=int(changed.sum()),
                        added_seam_pixels=int(bridge.sum()),max_extension_from_existing_shadow_px=1,
                        max_distance_from_lower_body_px=2))

after=Image.alpha_composite(base,new)
# Preserve every pixel covered by the actual body union, including partial edges.
for p in points:
    mask=Image.open(CAP/(p['role']+'_body_mask.png')).getchannel('R').point(lambda a:255 if a else 0)
    full=Image.new('L',base.size);full.paste(mask,(p['maskX'],p['maskY']))
    after=Image.composite(before,after,full)
delta=np.any(np.asarray(before)!=np.asarray(after),axis=2)
assert not delta[~allowed_full].any(), 'Changes escaped contact region'
for p,r in zip(points,records):
    mask=np.asarray(Image.open(CAP/(p['role']+'_body_mask.png')).getchannel('R'))>0
    d=delta[p['maskY']:p['maskY']+mask.shape[0],p['maskX']:p['maskX']+mask.shape[1]]
    r['all_nonzero_body_mask_changed_pixels']=int(d[mask].sum())
    assert r['all_nonzero_body_mask_changed_pixels']==0
after.save(OUT/'polished_on_actual_originals.png')
new.save(OUT/'separate_polished_layer.png')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
board=Image.new('RGB',(2320,920),'#20252b');draw=ImageDraw.Draw(board)
draw.text((12,8),'본체·그림자 전체 위치 유지 / 발·받침 접점만 미세 보완 · 동일 Nearest 2배',font=font,fill='white')
for i,p in enumerate(points):
    x,y=round(p['x']),round(p['y']);box=(x-70,y-150,x+70,y+30)
    left,top=(i%4)*580,50+(i//4)*435
    draw.text((left+8,top),p['name'],font=font,fill='#eed99f')
    for j,(im,label) in enumerate(((before,'이전 접점'),(after,'접점 미세 보완'))):
        draw.text((left+8+j*290,top+28),label,font=small,fill='white')
        board.paste(im.crop(box).resize((280,360),Image.Resampling.NEAREST).convert('RGB'),(left+5+j*290,top+55))
    if p['role'] in ('Player','Single','Core'):
        b=(x-40,y-80,x+40,y+25) if p['role']=='Player' else (x-65,y-125,x+65,y+30)
        width,height=(b[2]-b[0])*4,(b[3]-b[1])*4
        pair=Image.new('RGB',(width*2,height+50),'#20252b');pd=ImageDraw.Draw(pair)
        for j,(im,label) in enumerate(((before,'이전 접점'),(after,'접점 미세 보완'))):
            pd.text((j*width+10,8),label,font=font,fill='white')
            pair.paste(im.crop(b).resize((width,height),Image.Resampling.NEAREST).convert('RGB'),(j*width,50))
        pair.save(OUT/(p['role'].lower()+'_before_after_4x.png'))
board.save(OUT/'all_before_after_2x.png')
(OUT/'checks.json').write_text(json.dumps(dict(method='Local extension of extracted shadow by at most1px only within2px of real lower contact; no global move/scale/shape regeneration',changed_outside_contact=0,objects=records),ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(records,ensure_ascii=False))
