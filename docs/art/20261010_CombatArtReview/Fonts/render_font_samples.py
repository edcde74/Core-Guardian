from pathlib import Path
import struct, json, hashlib
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
FONTS = [
    ('A / Pixelify Sans Bold', 'PixelifySans/PixelifySans-Variable.ttf', 700),
    ('B / NeoDunggeunmo', 'NeoDunggeunmo/NeoDunggeunmo-Regular.ttf', None),
    ('C / Silkscreen Bold', 'Silkscreen/Silkscreen-Bold.ttf', None),
]

def cmap_codes(path):
    data = path.read_bytes()
    count = struct.unpack_from('>H', data, 4)[0]
    tables = {}
    for n in range(count):
        tag, checksum, offset, length = struct.unpack_from('>4sIII', data, 12 + n * 16)
        tables[tag] = (offset, length)
    base, _ = tables[b'cmap']
    nc = struct.unpack_from('>H', data, base+2)[0]
    codes = set()
    for i in range(nc):
        plat, enc, off = struct.unpack_from('>HHI', data, base+4+i*8)
        if plat not in (0,3): continue
        pos = base+off
        fmt = struct.unpack_from('>H', data, pos)[0]
        if fmt == 4:
            segs = struct.unpack_from('>H', data, pos+6)[0]//2
            ends = struct.unpack_from('>'+str(segs)+'H',data,pos+14)
            starts = struct.unpack_from('>'+str(segs)+'H',data,pos+16+segs*2)
            delta_base = pos+16+segs*4
            range_base = delta_base+segs*2
            deltas = struct.unpack_from('>'+str(segs)+'h',data,delta_base)
            offsets = struct.unpack_from('>'+str(segs)+'H',data,range_base)
            for k,(a,b,delta,offset) in enumerate(zip(starts,ends,deltas,offsets)):
                if a == 65535: continue
                for code in range(a,b+1):
                    if offset == 0:
                        glyph=(code+delta)&65535
                    else:
                        glyph_pos=range_base+k*2+offset+2*(code-a)
                        glyph=struct.unpack_from('>H',data,glyph_pos)[0]
                        if glyph: glyph=(glyph+delta)&65535
                    if glyph: codes.add(code)
        elif fmt == 12:
            groups = struct.unpack_from('>I',data,pos+12)[0]
            for j in range(groups):
                a,b,g = struct.unpack_from('>III',data,pos+16+j*12)
                codes.update(c for c in range(a,b+1) if g+c-a != 0)
    return codes

def font(path,size,weight=None):
    f = ImageFont.truetype(str(path),size)
    if weight is not None: f.set_variation_by_axes([weight])
    return f

label = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',32)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
img = Image.new('RGB',(1600,1190),'#11181d')
d = ImageDraw.Draw(img)
d.text((40,26),'CORE GUARDIAN / DAMAGE FONT REVIEW',font=title,fill='#eff1e5')
d.text((40,74),'Actual downloaded TTF fonts | Native 24px and 32px | 1px black outline | Approval candidates',font=label,fill='#aab8be')
report = []
for index,(name,rel,weight) in enumerate(FONTS):
    path=ROOT/rel
    codes=cmap_codes(path)
    korean='피해 골드 코어 가디언'
    ko_support=all(ord(c) in codes for c in korean if c!=' ')
    row=125+index*345
    d.text((40,row),name,font=title,fill='#dbc6a0')
    d.text((40,row+43),('Korean sample supported' if ko_support else 'Latin / numerals only; Korean fallback needed'),font=small,fill='#aab8be')
    for col,bg in enumerate(['#27333a','#ada996']):
        x=40+col*780
        d.rounded_rectangle((x,row+80,x+740,row+313),radius=8,fill=bg)
        d.text((x+18,row+90),('DARK PATH' if col==0 else 'LIGHT STONE'),font=small,fill=('#aab8be' if col==0 else '#303638'))
        for j,size in enumerate([24,32]):
            f=font(path,size,weight)
            yy=row+130+j*83
            d.text((x+18,yy+7),f'{size}px',font=small,fill=('#aab8be' if col==0 else '#303638'))
            d.text((x+95,yy),'13   25   128   1024',font=f,fill='#fff0cc',stroke_width=1,stroke_fill='#141b21')
            d.text((x+95,yy+37),'+3 GOLD',font=f,fill='#ead58a',stroke_width=1,stroke_fill='#141b21')
            d.text((x+345,yy+37),'0 1 3 8',font=f,fill='#fff0cc',stroke_width=1,stroke_fill='#141b21')
            if ko_support:
                d.text((x+520,yy+37),'피해 골드',font=f,fill='#b7e3d5',stroke_width=1,stroke_fill='#141b21')
    report.append({'font':name,'file':rel,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size,'ASCII_sample_supported':all(ord(c) in codes for c in '0123456789+ GOLD'),'Korean_sample_supported':ko_support,'Hangul_syllables_in_cmap':sum(c in codes for c in range(0xAC00,0xD7A4)),'cmap_unicode_count':len(codes)})
img.save(ROOT/'damage_font_comparison.png')
(ROOT/'glyph_verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
