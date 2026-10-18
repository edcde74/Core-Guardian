from pathlib import Path
from collections import deque
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

here=Path(__file__).parent
source_path=Path('C:/Users/edcde/.codex/generated_images/01a14c3e-6f40-7cb0-a137-905a4e6a0ae7/exec-64c1db7a-4fd7-4356-86cf-e32b0ba049b7.png')
source=Image.open(source_path).convert('RGB')
rgb=np.asarray(source).astype(float)
specs=[
 ('Attack','창병',(160,715,301,805),(271,769),(220,781),185),
 ('Defense','방패병',(326,715,498,813),(471,771),(405,784),145),
 ('Buff','군기병',(535,710,690,803),(662,763),(586,782),281),
 ('Player','플레이어',(720,710,882,805),(845,765),(784,781),134),
 ('Core','코어',(900,690,1133,815),(1100,777),(1004,787),232),
 ('Single','단일',(1155,705,1342,805),(1314,769),(1237,784),191),
 ('Multi','멀티',(1370,710,1563,810),(1524,774),(1439,787),192),
 ('Support','지원',(1580,715,1773,808),(1733,774),(1653,785),213),
]
r,g,b=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
warm=(r>=48)&(r<=105)&(g/r.clip(1)>.75)&(g/r.clip(1)<.94)&(b/g.clip(1)>.70)&(b/g.clip(1)<.94)&((r-g)>3)&((g-b)>3)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
proof=Image.new('RGB',(1920,1050),'#22272d')
pd=ImageDraw.Draw(proof)
pd.text((16,8),'보내주신 사진 → 보이는 그림자 직접 추출 (원본 좌표 유지)',font=font,fill='white')
overlay=source.copy().convert('RGBA')
target_dir=here.parents[0]/'shadow_blunt_preview_20261018'
target=Image.open(target_dir/'current_actual.png').convert('RGBA')
current=target.copy()
meta=json.loads((target_dir/'capture.json').read_text(encoding='utf-8-sig'))
lookup={p['role']:p for p in meta['points']}
layer=Image.new('RGBA',target.size)
records=[]
source_layer=Image.new('RGBA',source.size)
for index,(role,name,roi,seed,anchor,ref_height) in enumerate(specs):
 x0,y0,x1,y1=roi
 allowed=np.zeros(warm.shape,dtype=bool);allowed[y0:y1,x0:x1]=warm[y0:y1,x0:x1]
 seen=np.zeros(warm.shape,dtype=bool)
 todo=deque([seed]);seen[seed[1],seed[0]]=True
 while todo:
  x,y=todo.popleft()
  for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
   if x0<=xx<x1 and y0<=yy<y1 and allowed[yy,xx] and not seen[yy,xx]:
    seen[yy,xx]=True;todo.append((xx,yy))
 mask=Image.fromarray((seen*255).astype('uint8'))
 # Remove thin paving-crack branches selected with the same brown-gray color.
 # This cleans a source selection; it does not draw a replacement footprint.
 mask=mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))
 seen=np.asarray(mask)>0
 box=mask.getbbox()
 if box is None:raise RuntimeError(role+' empty extraction')
 # This is selection of source pixels; no ellipse, polygon drawing or image generation.
 rgba=Image.new('RGBA',source.size,(12,10,8,0));rgba.putalpha(mask.point(lambda a:round(a*.40)))
 crop=rgba.crop(box);crop.save(here/(role+'_shadow_extracted.png'))
 source_layer=Image.alpha_composite(source_layer,rgba)
 mask.crop(box).save(here/(role+'_visible_mask.png'))
 source.crop(roi).save(here/(role+'_source_crop.png'))
 # Diagnostic overlay: selected pixels in cyan, aligned exactly with source coordinates.
 cyan=Image.new('RGBA',source.size,(0,220,255,0));cyan.putalpha(mask.point(lambda a:round(a*.42)))
 overlay=Image.alpha_composite(overlay,cyan)
 cellx=(index%4)*480;celly=45+(index//4)*490
 pd.text((cellx+8,celly),name,font=font,fill='#eed99f')
 src_crop=source.crop(roi).resize(((x1-x0)*2,(y1-y0)*2),Image.Resampling.NEAREST)
 proof.paste(src_crop,(cellx+5,celly+33))
 checker=Image.new('RGBA',(460,190),(205,205,205,255));cd=ImageDraw.Draw(checker)
 for yy in range(0,190,10):
  for xx in range(0,460,10):
   if (xx//10+yy//10)%2:cd.rectangle((xx,yy,xx+9,yy+9),fill=(235,235,235,255))
 extracted=crop.resize((crop.width*2,crop.height*2),Image.Resampling.NEAREST)
 checker.alpha_composite(extracted,(10,0));proof.paste(checker.convert('RGB'),(cellx+5,celly+295))
 # Only scale and translate extracted pixels. Keep their original offset from the soles.
 p=lookup[role]
 native_mask=Image.open(target_dir/(role+'_body_mask.png')).getchannel('R')
 mb=native_mask.getbbox();native_height=mb[3]-mb[1]
 scale=native_height/ref_height
 scaled=crop.resize((max(1,round(crop.width*scale)),max(1,round(crop.height*scale))),Image.Resampling.NEAREST)
 pos=(round(p['x']+(box[0]-anchor[0])*scale),round(p['y']+(box[1]-anchor[1])*scale))
 layer.alpha_composite(scaled,pos)
 records.append(dict(role=role,source_roi=roi,source_seed=seed,source_anchor=anchor,shadow_box=box,selected_pixels=int(seen.sum()),native_scale=scale,native_position=pos))
proof.save(here/'direct_extraction_proof.png')
source_layer.save(here/'shadows_from_reference_original_coordinates.png')
overlay.crop((111,690,1774,820)).save(here/'selected_source_pixels_overlay.png')
composite=Image.alpha_composite(target,layer)
for p in meta['points']:
 m=Image.open(target_dir/(p['role']+'_body_mask.png')).getchannel('R')
 mask=Image.new('L',target.size)
 mask.paste(m,(p['maskX'],p['maskY']))
 composite=Image.composite(current,composite,mask)
composite.save(here/'extracted_shadow_on_actual_originals.png')
layer.save(here/'separate_shadow_layer.png')
checks=[]
delta=np.asarray(composite.convert('RGB')).astype(int)-np.asarray(current.convert('RGB')).astype(int)
for p in meta['points']:
 m=np.asarray(Image.open(target_dir/(p['role']+'_body_mask.png')).getchannel('R'))>254
 x,y=p['maskX'],p['maskY'];region=delta[y:y+m.shape[0],x:x+m.shape[1]]
 checks.append(dict(role=p['role'],changed_original_body_pixels=int(np.any(region!=0,axis=2)[m].sum())))
(here/'body_preservation.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
actual=Image.new('RGB',(2320,920),'#20252b');ad=ImageDraw.Draw(actual)
ad.text((20,10),'실제 원본 + 사진에서 추출한 그림자 · 원본 발/받침 기준 상대 위치 유지',font=font,fill='white')
for index,p in enumerate(meta['points']):
 x,y=round(p['x']),round(p['y']);box=(x-70,y-150,x+70,y+30)
 left,top=(index%4)*580,55+(index//4)*435
 ad.text((left+10,top),p['name'],font=font,fill='#eed99f')
 ad.text((left+10,top+30),'현재',font=small,fill='white')
 ad.text((left+300,top+30),'사진 그림자 추출·합성',font=small,fill='white')
 actual.paste(current.crop(box).resize((280,360),Image.Resampling.NEAREST).convert('RGB'),(left+5,top+54))
 actual.paste(composite.crop(box).resize((280,360),Image.Resampling.NEAREST).convert('RGB'),(left+295,top+54))
 if p['role']=='Player':
  pair=Image.new('RGB',(640,480),'#20252b');d=ImageDraw.Draw(pair)
  d.text((10,8),'원본 / 보내주신 사진의 그림자만 추출·합성',font=font,fill='white')
  pb=(x-40,y-80,x+40,y+25)
  pair.paste(current.crop(pb).resize((320,420),Image.Resampling.NEAREST).convert('RGB'),(0,50))
  pair.paste(composite.crop(pb).resize((320,420),Image.Resampling.NEAREST).convert('RGB'),(320,50))
  pair.save(here/'player_direct_extraction_4x.png')
actual.save(here/'actual_originals_direct_extraction_2x.png')
(here/'extraction.json').write_text(json.dumps(dict(source=str(source_path),method='Seeded warm-dark source-pixel selection; visible parts only; no silhouette generation or new polygon; alpha reconstructed as 40% because source is flattened RGB',objects=records),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print([(r['role'],r['shadow_box'],r['selected_pixels']) for r in records])
