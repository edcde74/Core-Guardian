from PIL import Image,ImageDraw,ImageFont,ImageChops,ImageStat
from pathlib import Path
import json
p=Path(__file__).parent
data=json.loads((p/'check.json').read_text(encoding='utf-8-sig'));rect=tuple(data['crop'])
names=['00_before','01_during','02_after','03_null_block','00_before_emphasis_off','02_after_emphasis_off']
imgs={n:Image.open(p/(n+'.png')).convert('RGB').crop(rect) for n in names}
stats={}
for a,b in [('00_before','01_during'),('00_before','02_after'),('00_before','03_null_block'),('00_before_emphasis_off','02_after_emphasis_off')]:
    diff=ImageChops.difference(imgs[a],imgs[b]);stats[a+' vs '+b]={'meanAbsDifference':ImageStat.Stat(diff).mean,'extrema':diff.getextrema(),'changedPixels':sum(any(diff.getpixel((x,y))) for y in range(diff.height) for x in range(diff.width))}
print(json.dumps(stats,indent=2));(p/'pixel_comparison.json').write_text(json.dumps(stats,indent=2),encoding='utf-8',newline='\n')
labels=['피격 전 · 강조 ON','피격 중 · 강조 ON','피격 종료 · 강조 ON','속성 블록 초기화','피격 전 · 강조 OFF','피격 종료 · 강조 OFF']
w,h=imgs[names[0]].size;cw=w*4;ch=h*4+55
canvas=Image.new('RGB',(cw*3,ch*2),(22,25,29));d=ImageDraw.Draw(canvas);font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',19)
for i,(n,label) in enumerate(zip(names,labels)):
    x=i%3*cw;y=i//3*ch;d.text((x+8,y+10),label,font=font,fill='white');canvas.paste(imgs[n].resize((w*4,h*4),Image.Resampling.NEAREST),(x,y+55))
canvas.save(p/'comparison.png')
