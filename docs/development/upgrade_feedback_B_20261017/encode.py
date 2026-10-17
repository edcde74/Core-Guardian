from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
root=Path(__file__).resolve().parent
preview=json.loads((root.parent/'upgrade_feedback_options_20261017/capture.json').read_text(encoding='utf-8-sig'))
timing=json.loads((root/'capture.json').read_text(encoding='utf-8-sig'))['result']
font=ImageFont.truetype(str(root.parents[2]/'Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf'),19)
frames=[]
for path in sorted((root/'frames').glob('*.png')):
    source=Image.open(path).convert('RGB');board=Image.new('RGB',(1056,350),'#141b24');draw=ImageDraw.Draw(board)
    draw.text((12,8),'B 적용 · 실제 강화 구매 성공 후 재생',font=font,fill='#b4edf3')
    for i,(center,label) in enumerate(zip(preview['centers'],['플레이어','단일 포탑','멀티 포탑','지원 포탑'])):
        x,y=round(center['x']),round(230-center['y']);board.paste(source.crop((x-66,y-74,x+66,y+74)).resize((264,296),Image.Resampling.NEAREST),(264*i,54));draw.text((264*i+12,30),label,font=font,fill='white')
    frames.append(board)
assert len(frames)==36
durations=[max(20,round((b-a)*1000/10)*10) for a,b in zip(timing['times'],timing['times'][1:])]+[500]
frames[0].save(root/'applied_B.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False)
index=next((i for i,t in enumerate(timing['times']) if t>.2),5);frames[index].save(root/'applied_B.png')
print(json.dumps({'frames':len(frames),'visible_frames':sum(x>0 for x in timing['active']),'triggers':timing['triggers'],'end_active':timing['active'][-1]}))
