from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
DEV=ROOT/'docs/development';REF=DEV/'shadow_reference_refine_20261018'
OUT=ROOT/'Assets/Resources/CoreGuardian/PhotoShadows';OUT.mkdir(parents=True,exist_ok=True)
points=json.loads((DEV/'shadow_blunt_preview_20261018/capture.json').read_text(encoding='utf-8-sig'))['points']
reg=json.loads((REF/'registration.json').read_text(encoding='utf-8-sig'))['objects']
data=[]
for p in points:
    role=p['role'];full=Image.new('RGBA',(1920,1080))
    if role in ('Core','Multi','Support'):
        full.alpha_composite(Image.open(REF/'marked_fix'/(role+'_corrected_shadow.png')),(p['maskX']-18,p['maskY']-18))
    elif role=='Defense':full=Image.open(REF/'marked_fix/corrected_cast_layer.png').convert('RGBA')
    elif role=='Single':full=Image.open(REF/'contact_polish/separate_polished_layer.png').convert('RGBA')
    else:
        r=next(r for r in reg if r['role']==role)
        full.alpha_composite(Image.open(REF/(role+'_registered_shadow.png')),tuple(r['position']))
    # Confine full-frame shared layers to this role's non-overlapping capture region.
    roi=(p['maskX']-24,p['maskY']-24,p['maskX']+p['maskWidth']+48,p['maskY']+p['maskHeight']+24)
    local=full.crop(roi);b=local.getbbox();assert b
    crop=local.crop(b);crop.save(OUT/(role+'.png'))
    cx=roi[0]+(b[0]+b[2])/2;cy=roi[1]+(b[1]+b[3])/2
    data.append(dict(role=role,x=(cx-p['x'])/63.52941,y=(430.411743-cy)/63.52941))
(OUT/'registration.json').write_text(json.dumps(dict(entries=data),indent=2)+'\n',encoding='utf-8',newline='\n')
print(data)
