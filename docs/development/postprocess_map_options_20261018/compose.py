from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageStat
import json

root = Path(__file__).parent
font_path = 'C:/Windows/Fonts/malgun.ttf'
font = ImageFont.truetype(font_path, 29)
small = ImageFont.truetype(font_path, 21)
items = [
    ('00_current.png', '현재 적용', '대비 +35 / 채도 +12 / 노출 -0.45 EV'),
    ('01_calm_stone.png', '① 차분한 석조 요새 · 추천', '대비 +28 / 채도 +6 / 노출 -0.28 EV'),
    ('02_cool_ruins.png', '② 차가운 폐허', '대비 +30 / 채도 -5 / 노출 -0.40 EV'),
    ('03_warm_machinery.png', '③ 따뜻한 고대 설비', '대비 +26 / 채도 +7 / 노출 -0.20 EV'),
]
canvas=Image.new('RGB',(2560,1600),(22,25,29))
draw=ImageDraw.Draw(canvas)
stats={}
base=Image.open(root/items[0][0]).convert('RGB')
for i,(file,title,settings) in enumerate(items):
    im=Image.open(root/file).convert('RGB')
    assert im.size==(1920,1080)
    stats[file]={'mean_rgb':ImageStat.Stat(im).mean,'mean_abs_difference_current':ImageStat.Stat(ImageChops.difference(base,im)).mean}
    x=(i%2)*1280; y=(i//2)*800
    draw.text((x+20,y+8),title,font=font,fill=(235,239,242))
    draw.text((x+20,y+48),settings,font=small,fill=(173,186,197))
    canvas.paste(im.resize((1280,720),Image.Resampling.LANCZOS),(x,y+80))
canvas.save(root/'comparison_2x2.png')
(root/'image_stats.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats,indent=2))
