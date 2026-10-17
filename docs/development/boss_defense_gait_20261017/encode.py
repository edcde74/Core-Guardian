"""Package unchanged Unity renders; no source sprite editing."""
from pathlib import Path
from PIL import Image, ImageDraw

p=Path(__file__).resolve().parent
frames=[]
for i in range(48):
    out=Image.new('RGB',(480,604),(35,40,46))
    for col,label in enumerate(('before','after')):
        out.paste(Image.open(p/(label+'_frames')/f'{i:03}.png').crop((0,0,240,576)),(240*col,28))
    ImageDraw.Draw(out).text((20,6),'BEFORE / AFTER - native 3x',fill='white')
    frames.append(out)
frames[0].save(p/'before_after.webp',save_all=True,append_images=frames[1:],duration=40,loop=0,lossless=True)
frames[12].save(p/'comparison.png')
route=[]
for f in sorted((p/'route_frames').glob('*.png')):
    src=Image.open(f)
    out=Image.new('RGB',(260,218),(35,40,46))
    out.paste(src.crop((1390,260,1520,450)),(0,28))
    out.paste(src.crop((480,380,610,570)),(130,28))
    ImageDraw.Draw(out).text((4,6),'Boss Down / Defense Up',fill='white')
    route.append(out)
assert len(route)==48
route[0].save(p/'actual_routes.webp',save_all=True,append_images=route[1:],duration=25,loop=0,lossless=True)
route[12].save(p/'actual_routes.png')
