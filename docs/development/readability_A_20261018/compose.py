from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageStat, ImageChops
import json, subprocess

p=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',28)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21)
canvas=Image.new('RGB',(2560,1480),(21,25,29)); d=ImageDraw.Draw(canvas)
images=[Image.open(p/'combat_before.png').convert('RGB'),Image.open(p/'combat_after.png').convert('RGB')]
for i,(im,title) in enumerate(zip(images,['이전 · ③ 색보정 유지','A 적용 · 배경과 길의 질감 대비 완화'])):
    x=i*1280
    d.text((x+20,12),title,font=font,fill='white')
    d.text((x+20,50),'같은 전투 시점 / 같은 카메라 / HUD 포함',font=small,fill=(178,190,202))
    canvas.paste(im.resize((1280,720),Image.Resampling.LANCZOS),(x,80))
    d.text((x+20,812),'몬스터 · 포탑 · 플레이어 부분 확대 (동일 구간, 2배 Nearest)',font=small,fill='white')
    canvas.paste(im.crop((80,450,720,750)).resize((1280,600),Image.Resampling.NEAREST),(x,860))
canvas.save(p/'comparison_A.png')
# Disk scene delta must consist solely of the 243 material-reference replacements.
delta=subprocess.check_output(['git','-c','safe.directory=C:/Users/edcde/OneDrive/Desktop/CD','diff','--unified=0','--','Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity'],text=True)
changes=[line for line in delta.splitlines() if line[:1] in ['+','-'] and not line.startswith(('+++','---'))]
assert len(changes)==486 and all('fileID: 2100000, guid:' in line for line in changes)
report={'sceneDeltaOnlyMaterialReferences':True,'changedRenderers':243,'comparisonSize':canvas.size,'sourceSizes':[im.size for im in images],'sameFrameMeanAbsDifference':ImageStat.Stat(ImageChops.difference(*images)).mean}
(p/'disk_verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print(json.dumps(report))
