"""Authorized experimental tables. Existing sprite kinds; per-spawn stats live in waves.csv."""
from pathlib import Path
import csv,json,hashlib
root=Path(__file__).resolve().parents[3]
out=Path(__file__).resolve().parent
data=root/'Assets/Resources/CoreGuardian'
baseline=out/'before';baseline.mkdir(exist_ok=True)
for name in ['enemies','towers','stage','upgrades','waves']:
 p=data/(name+'.csv');target=baseline/p.name
 if not target.exists():target.write_bytes(p.read_bytes())
protected=out/'protected_before.json'
if not protected.exists():protected.write_text(json.dumps({p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'Assets').rglob('*') if p.is_file()},indent=2),encoding='utf-8')
def write(name,header,rows):
 with (data/(name+'.csv')).open('w',encoding='utf-8',newline='') as f:
  w=csv.writer(f,lineterminator='\r\n');w.writerow(header);w.writerows(rows)
stage=dict(csv.reader((baseline/'stage.csv').read_text(encoding='utf-8-sig').splitlines()[1:]))
for k in list(stage):
 if k.startswith('waveReward'):del stage[k]
stage.update(waveReward=40,preparationSeconds=20,playerFirstCost=50,playerFirstUpgradeDiscount=10,playerSecondIncrease=15,playerIncreaseStep=5,towerFirstUpgradeDiscount=0,augmentationEvery=5,augmentationRequiresBoss=0)
for wave in range(1,31):
 reward=40 if wave<=4 else 45 if wave<=9 else 70 if wave==10 else 50 if wave<=14 else 55 if wave<=19 else 90 if wave==20 else 60 if wave<=24 else 65 if wave<=29 else 110
 stage['waveReward'+str(wave)]=reward+20
write('stage',['id','value'],stage.items())
towers=list(csv.DictReader((baseline/'towers.csv').read_text().splitlines()))
for t in towers:t['cost']=75 if t['id']=='Support' else 60
write('towers',list(towers[0]),[list(t.values()) for t in towers])
upgrades=list(csv.DictReader((baseline/'upgrades.csv').read_text().splitlines()))
for u in upgrades:
 if u['target']!='Player':u.update(cost=60,costIncrement=30)
write('upgrades',list(upgrades[0]),[list(u.values()) for u in upgrades])
counts=[(15,0,0),(16,0,0),(20,3,0),(22,4,0),(24,6,1),(28,5,2),(30,6,2),(32,7,3),(34,9,3),(26,8,3),(34,8,3),(36,10,4),(38,11,5),(40,12,5),(42,14,6),(44,15,6),(46,16,7),(48,18,8),(50,20,8),(42,18,8),(54,20,9),(56,22,10),(58,24,10),(60,25,11),(62,27,12),(64,29,12),(66,31,13),(68,33,14),(70,35,15),(60,30,12)]
gaps=[1.62,1.53,.55,.50,.48,.42,.40,.36,.34,.40,.34,.33,.32,.31,.30,.29,.28,.27,.26,.30,.25,.245,.24,.235,.23,.225,.22,.215,.21,.25]
def stats(w,k):
 if k=='Boss':return ({10:500,20:1600,30:4500}[w],{10:5,20:12,30:20}[w],.6,{10:120,20:240,30:400}[w])
 if w<=10:
  hp={'Attack':[40,40,40,42,44,46,48,50,52,55],'Defense':[50,50,60,65,70,75,80,85,90,100],'Buff':[30,30,30,30,35,40,45,45,50,55]}[k][w-1]
 elif w<=20:hp={'Attack':66+6*(w-11),'Defense':120+12*(w-11),'Buff':60+6*(w-11)}[k]
 else:hp={'Attack':140+15*(w-21),'Defense':280+40*(w-21),'Buff':140+15*(w-21)}[k]
 if w>=11:hp=int(hp*.75+.5)
 defense={'Attack':max(0,(w-5)*.35),'Defense':min(28,4+w*.7),'Buff':max(0,(w-10)*.25)}[k]
 speed={'Attack':1,'Defense':.8,'Buff':1}[k]*(1+max(0,w-10)*.005)
 gold={'Attack':3,'Defense':4,'Buff':6}[k]+(w-1)//10
 return hp,round(defense,2),round(speed,3),gold
rows=[];summary=[]
for w,(a,d,b) in enumerate(counts,1):
 gap=gaps[w-1];left={'Attack':a,'Defense':d,'Buff':b};order=[]
 if w%10==0:
  for j in range(3):rows.append([f'w{w}_screen{j}',w,'Defense',1,.25,j*.25,*stats(w,'Defense')]);left['Defense']-=1
  rows.append([f'w{w}_boss',w,'Boss',1,1,.75,*stats(w,'Boss')])
 totals=left.copy();total=sum(totals.values());acc={k:0 for k in left}
 for i in range(total):
  for k in left:acc[k]+=totals[k]
  k=max((k for k in left if left[k]>0),key=lambda k:acc[k]);acc[k]-=total;left[k]-=1;order.append(k)
 for i,k in enumerate(order):rows.append([f'w{w}_{i+1}_{k.lower()}',w,k,1,gap,round((1.5 if w%10==0 else 0)+i*gap,3),*stats(w,k)])
 summary.append({'wave':w,'attack':a,'defense':d,'buff':b,'boss':int(w%10==0),'interval':gap,'health':{k:stats(w,k)[0] for k in ['Attack','Defense','Buff']},'crystals':stage['waveReward'+str(w)]})
write('waves',['id','wave','enemy','count','interval','start','health','defense','speed','gold'],rows)
(out/'table_summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps({'rounds':30,'enemies':len(rows),'bosses':[500,1600,4500],'crystal_rewards':sum(s['crystals'] for s in summary)}))
