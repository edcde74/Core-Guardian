"""Four marked shadow corrections. Photo-derived cast masks and real sole/base contours."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
PRIOR=HERE.parent/'contact_polish'
CAP=HERE.parents[1]/'shadow_blunt_preview_20261018'
points=json.loads((CAP/'capture.json').read_text(encoding='utf-8-sig'))['points']
registration=json.loads((HERE.parent/'registration.json').read_text(encoding='utf-8-sig'))['objects']
before=Image.open(PRIOR/'polished_on_actual_originals.png').convert('RGBA')
clean=Image.open(HERE/'clean_ground_four.png').convert('RGBA')
after=before.copy()
selected=('Defense','Core','Multi','Support')
cast_layer=Image.new('RGBA',before.size)
contact_layer=Image.new('RGBA',before.size)
report=[]

def expand(m,r):
    return np.asarray(Image.fromarray((m*255).astype('uint8')).filter(ImageFilter.MaxFilter(2*r+1)))>0

for p in points:
    role=p['role']
    if role not in selected:continue
    rec=next(r for r in registration if r['role']==role)
    source=Image.open(HERE.parent/(role+'_registered_shadow.png')).convert('RGBA')
    pos=rec['position'].copy()
    if role=='Multi':
        # Match actual base depth: remove the tall, spear-like exposed tip by a
        # shorter vertical registration, retaining the extracted contour.
        source=source.resize((round(source.width*1.10),round(source.height*.80)),Image.Resampling.NEAREST)
        pos=[pos[0]-3,pos[1]+5]
    elif role=='Support':
        source=source.resize((round(source.width*1.10),round(source.height*.85)),Image.Resampling.NEAREST)
        pos=[pos[0]-5,pos[1]+2]
    x0,y0=p['maskX']-18,p['maskY']-18
    w,h=p['maskWidth']+36,p['maskHeight']+36
    body=np.zeros((h,w),bool)
    body[18:18+p['maskHeight'],18:18+p['maskWidth']]=np.asarray(Image.open(CAP/(role+'_body_mask.png')).getchannel('R'))>0
    yb=int(np.where(body)[0].max())
    cast=Image.new('RGBA',(w,h));cast.alpha_composite(source,(pos[0]-x0,pos[1]-y0))
    cast_array=np.array(cast);a=cast_array[:,:,3]
    ground=body.copy();ground[:yb-(16 if role=='Defense' else 24)]=False
    # Repair gaps between the *existing* cast and lower actual body. This is
    # local mask closing, not a newly drawn ellipse or replacement polygon.
    union=ground|(a>0)
    closed=np.asarray(Image.fromarray((union*255).astype('uint8')).filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.MinFilter(7)))>0
    bridge=closed&~union&expand(ground,4)&expand(a>0,3)
    a[bridge]=90
    cast_array[bridge,:3]=(12,10,8)
    cast_array[:,:,3]=a
    # Trace only real lower sole/base edges; never project torso, weapons or arms.
    # This contact strip is a static preview and is not an animated foot asset.
    contact=np.zeros((h,w),np.uint8)
    for x in range(w):
        yy=np.where(body[:,x])[0]
        if not len(yy):continue
        bottom=int(yy[-1])
        if bottom<yb-(12 if role=='Defense' else 24):continue
        for dy,opacity in ((1,142),(2,54)):
            y=bottom+dy
            if y<h and not body[y,x]:contact[y,x]=max(contact[y,x],opacity)
    # Darkest immediately at contact; one adjacent pixel at low density.
    near=expand(ground,1)&~body&(a>0)
    cast_array[near,3]=np.maximum(cast_array[near,3],116)
    cl=Image.fromarray(cast_array)
    ct=Image.new('RGBA',(w,h),(10,9,8,0));ct.putalpha(Image.fromarray(contact))
    # Combine with max alpha rather than multiply shadows into a thick rim.
    ca=np.array(cl);ta=np.array(ct)
    replace=ta[:,:,3]>ca[:,:,3];ca[replace]=ta[replace]
    local=Image.alpha_composite(clean.crop((x0,y0,x0+w,y0+h)),Image.fromarray(ca))
    # Keep original body and every partially covered edge pixel exactly.
    old=before.crop((x0,y0,x0+w,y0+h))
    local=Image.composite(old,local,Image.fromarray((body*255).astype('uint8')))
    after.paste(local,(x0,y0))
    cast_layer.alpha_composite(cl,(x0,y0));contact_layer.alpha_composite(ct,(x0,y0))
    Image.fromarray(ca).save(HERE/(role+'_corrected_shadow.png'))
    report.append(dict(role=role,position=pos,size=source.size,bridge_pixels=int(bridge.sum()),contact_pixels=int((contact>0).sum()),legacy_ground_disabled_on_temporary_clone=True,core_floor_glow_disabled_on_temporary_clone=role=='Core'))

delta=np.any(np.asarray(after)!=np.asarray(before),axis=2)
checks=[]
for p in points:
    mask=np.asarray(Image.open(CAP/(p['role']+'_body_mask.png')).getchannel('R'))>0
    local=delta[p['maskY']:p['maskY']+p['maskHeight'],p['maskX']:p['maskX']+p['maskWidth']]
    count=int(local[mask].sum());assert count==0,(p['role'],count)
    checks.append(dict(role=p['role'],all_nonzero_body_mask_changed_pixels=count))
    if p['role'] not in selected:
        x,y=round(p['x']),round(p['y'])
        assert not delta[y-150:y+30,x-70:x+70].any(),p['role']+' changed unexpectedly'
after.save(HERE/'corrected_on_actual_originals.png')
cast_layer.save(HERE/'corrected_cast_layer.png');contact_layer.save(HERE/'corrected_contact_layer.png')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
# Focused review: four marked objects, same 4x pixel magnification on both sides.
board=Image.new('RGB',(2120,1380),'#20252b');draw=ImageDraw.Draw(board)
draw.text((16,10),'빨간 원 4곳 수정 시안 · 본체 유지 / 왼쪽 이전 · 오른쪽 수정 / Nearest 4배',font=font,fill='white')
for i,role in enumerate(selected):
    p=next(p for p in points if p['role']==role)
    x,y=round(p['x']),round(p['y']);box=(x-65,y-125,x+65,y+25)
    lx=(i%2)*1060;ty=50+(i//2)*660
    draw.text((lx+8,ty),p['name'],font=font,fill='#eed99f')
    pair=Image.new('RGB',(1040,650),'#20252b');pd=ImageDraw.Draw(pair)
    for j,(im,label) in enumerate(((before,'이전'),(after,'수정 시안'))):
        draw.text((lx+8+j*530,ty+28),label,font=small,fill='white')
        crop=im.crop(box).resize((520,600),Image.Resampling.NEAREST).convert('RGB')
        board.paste(crop,(lx+3+j*530,ty+52))
        pd.text((j*520+10,5),label,font=font,fill='white');pair.paste(crop,(j*520,50))
    pair.save(HERE/(role.lower()+'_before_after_4x.png'))
board.save(HERE/'four_marked_before_after_4x.png')
# Result only, enlarged equally for all eight originals.
allboard=Image.new('RGB',(1160,860),'#20252b');ad=ImageDraw.Draw(allboard)
ad.text((12,8),'본체 원본 유지 · 표시한 4종 그림자 수정 시안 · 동일 Nearest 2배',font=font,fill='white')
for i,p in enumerate(points):
    x,y=round(p['x']),round(p['y']);box=(x-70,y-150,x+70,y+30)
    lx=(i%4)*290;ty=45+(i//4)*405
    ad.text((lx+8,ty),p['name']+(' · 수정' if p['role'] in selected else ' · 유지'),font=small,fill='#eed99f')
    allboard.paste(after.crop(box).resize((280,360),Image.Resampling.NEAREST).convert('RGB'),(lx+5,ty+30))
allboard.save(HERE/'all_result_2x.png')
(HERE/'checks.json').write_text(json.dumps(dict(adjustments=report,body_checks=checks,unmarked_four_unchanged=True,note='Source photo cast masks retained with role-specific re-registration; actual sole contour contact reconstruction; legacy shadow and core blue floor light isolated on temporary clones only; static preview, no runtime application'),ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(body_checks=checks,unmarked_four_unchanged=True),ensure_ascii=False))
