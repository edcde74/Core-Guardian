"""Distinct concept auditions, not three EQ variants. No Unity writes."""
from pathlib import Path
import sys,json,math,hashlib,shutil
from fractions import Fraction
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln
from scipy.signal import resample_poly,butter,sosfilt
SR=44100;OUT=HERE/'audio';REF=HERE/'reference'
OUT.mkdir(exist_ok=True);REF.mkdir(exist_ok=True)
R2=ROOT/'AudioProduction/CoreGuardian_B_Revision2_20261019'
LIB=ROOT/'AudioProduction/CoreGuardian_B_SoundLibrary_20261019'
r2=json.loads((R2/'catalog.json').read_text(encoding='utf-8'))
old=json.loads((LIB/'catalog.json').read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
protected={}
for dirname in ['CoreGuardian_B_Revision2_20261019','CoreGuardian_B_SoundLibrary_20261019','CoreGuardian_ConceptAB_v2_20261019','CoreGuardian_ConceptAB_20261019','CoreGuardian_BGM_20261013']:
 for p in (ROOT/'AudioProduction'/dirname).rglob('*'):
  if p.is_file() and p.suffix.lower() in ['.wav','.ogg','.mid']:protected[str(p.relative_to(ROOT))]=sha(p)
sources={}
def read(p):
 sources[str(p.relative_to(ROOT))]=sha(p);x,r=sf.read(p,always_2d=True);x=x.mean(axis=1)
 if r!=SR:x=resample_poly(x,SR//math.gcd(SR,r),r//math.gcd(SR,r))
 return x/max(.001,np.max(abs(x)))
def fade(x,a=.002,z=.035):
 x=np.array(x,dtype=np.float32,copy=True);aa=min(len(x)//2,round(a*SR));zz=min(len(x)//2,round(z*SR))
 if aa:x[:aa]*=np.linspace(0,1,aa)
 if zz:x[-zz:]*=np.linspace(1,0,zz)
 return x
def filt(x,lo=70,hi=3500):return sosfilt(butter(3,[lo,hi],fs=SR,btype='bandpass',output='sos'),x).astype(np.float32)
def cut(x,t,decay=None):
 y=x[:round(t*SR)].copy()
 if decay:y*=np.exp(-np.arange(len(y))/SR/decay)
 return fade(y)
def pitch(x,s):
 f=Fraction(2**(-s/12)).limit_denominator(512);return resample_poly(x,f.numerator,f.denominator)
def mix(events,duration=None):
 n=round(duration*SR) if duration else max(round(t*SR)+len(x) for t,x,g in events);y=np.zeros(n,dtype=np.float32)
 for t,x,g in events:
  i=round(t*SR);c=min(len(x),n-i)
  if c>0:y[i:i+c]+=g*x[:c]
 return fade(y)
rng=np.random.default_rng(2026101903)
def noise(t,lo,hi,attack=.003,decay=None):
 x=filt(rng.normal(0,.28,round(t*SR)),lo,hi)
 if decay:x*=np.exp(-np.arange(len(x))/SR/decay)
 return fade(x,attack,min(.08,t*.4))
def thud(t=.23):return noise(t,45,290,decay=t*.22)
ui=ROOT/'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio'
click=read(ui/'click_002.ogg');switch=read(ui/'switch_004.ogg');scratch=read(ui/'scratch_001.ogg')
unequip=read(ROOT/'Assets/Resources/CoreGuardian/Audio/TowerInstall/02_Unequip.wav')
# Three independent material/action stories: latch, magic suction, stone scrape.
dem=[
 mix([(0,cut(switch,.05),.38),(.04,cut(unequip,.16,.07),.4),(.12,cut(pitch(scratch,-5),.12,.05),.25),(.19,thud(.18),1.4)]),
 mix([(0,noise(.24,130,1800,.15),.8),(.16,noise(.27,60,700,.012,.12),1.3),(.32,cut(click,.02),.17)]),
 mix([(0,noise(.31,220,2100,.02),.55),(.04,cut(pitch(scratch,-12),.23),.45),(.27,thud(.29),2),(.29,noise(.20,600,3200,.003,.04),.24)])
]
groups=[];sets={'demolish':dem};aug_names=['무광 패 · 한 번에 탁','중량 봉인 · 철판 개방','마력 압력 · 넓은 해방']
groups.append({'id':'demolish','title':'포탑 철거','category':'설치·강화','status':'컨셉 비교 · 잠정①은 참고 보존','description':'① 금속 체결부를 풀고 회수 / ② 마력이 안으로 빨려 회수 / ③ 석재 봉인을 긁어 분리. 같은 소리의 강도 변형이 아닙니다.','clips':[{'title':n,'description':d} for n,d in zip(['기계식 회수','마력 회수','석재 봉인 해제'],['딸깍 → 짧은 금속 이동 → 둔한 회수','흡입이 모였다가 안으로 사라지는 마력','거친 마찰 → 무게 있는 분리'])]})
for tier,ident in enumerate(['aug_gold','aug_platinum']):
 # NO mallet, pitched note, bong, octave, double-bounce, or repeated impacts.
 # Higher tier extends material body/air only; not another ascending melody.
 t=tier
 a=mix([(0,noise(.09,.0+600,3000,.02),.44),(.075,thud(.24+t*.10),2.0),(.077,noise(.08,300,1700,.002,.025),.5),(.10,noise(.18+t*.1,100,750,.004,.06+t*.03),.45)])
 b=mix([(0,cut(pitch(scratch,-9),.17+t*.05),.36),(0,noise(.19+t*.06,140,1400,.03),.45),(.17+t*.04,thud(.36+t*.14),1.7),(.17+t*.04,cut(pitch(unequip,-8),.24+t*.06,.08),.24),(.19+t*.04,noise(.3+t*.18,250,950,.006,.11),.3)])
 c=mix([(0,noise(.22+t*.07,160,1400,.17+t*.04),.65),(.20+t*.05,noise(.46+t*.20,45,560,.012,.14+t*.06),1.6),(.21+t*.05,noise(.39+t*.18,400,2700,.025,.11+t*.04),.45)])
 sets[ident]=[a,b,c]
 groups.append({'id':ident,'title':'증강 공개 · '+['골드','플래티넘'][tier],'category':'증강','status':'이전③ 미선택 · 컨셉 재제작','description':'통통 튀던 음정 악기/추가 타격을 제거했습니다. 실버③은 그대로 두고, ① 건조한 카드 접촉 ② 낮은 장치 개방 ③ 공기와 마력 압력으로 방향을 나눴습니다.','clips':[{'title':n,'description':d} for n,d in zip(aug_names,['짧은 쓸림 뒤 한 번만 안착 · 마른 질감','거친 잠금 해제 뒤 낮은 몸통 · 금속 질감','서서히 모인 압력이 넓게 풀림 · 비음정 질감'])]})
meter=pyln.Meter(SR)
def loud(x):
 gap=max(.7,len(x)/SR+.25);return float(meter.integrated_loudness(mix([(i*gap,x,1) for i in range(5)],5*gap)))
def peak(x):return float(np.max(abs(resample_poly(x,4,1))))
metrics={}
for g in groups:
 normalized=[x*10**((-23-loud(x))/20) for x in sets[g['id']]];gain=min(1,.70/max(peak(x) for x in normalized))
 for i,x in enumerate(normalized):
  x*=gain;p=OUT/f"{g['id']}_{i+1}.wav";sf.write(p,x,SR,subtype='PCM_16');z,r=sf.read(p);assert peak(z)<.9 and np.isfinite(z).all()
  repeat=OUT/f"{g['id']}_{i+1}_repeat.wav";gap=max(.7,len(z)/SR+.25);sf.write(repeat,mix([(j*gap,z,1) for j in range(5)],5*gap),SR,subtype='PCM_16')
  g['clips'][i].update({'number':i+1,'url':str(p.relative_to(HERE)).replace('\\','/'),'repeat_url':str(repeat.relative_to(HERE)).replace('\\','/'),'source_wav':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':sha(p),'duration':len(z)/r})
  metrics[p.name]={'repeat_lufs':loud(z),'estimated_dbtp':20*math.log10(peak(z))}
new_choices={'cancel_install':3,'round_complete':2,'victory':3,'defeat':2,'ui_click':2,'ui_open':3,'ui_close':2,'aug_silver':3,'aug_bgm':3}
retained=[]
for c in r2['retained']:
 p=ROOT/c['original_wav'];dest=REF/p.name;shutil.copyfile(p,dest);retained.append({**c,'url':'reference/'+dest.name,'origin':'B72'})
for ident,n in new_choices.items():
 g=next(g for g in r2['groups'] if g['id']==ident);c=g['clips'][n-1];p=ROOT/c['source_wav'];dest=REF/p.name;shutil.copyfile(p,dest)
 retained.append({'id':ident,'title':g['title'],'number':n,'candidate':c['title'],'original_wav':c['source_wav'],'sha256':sha(dest),'url':'reference/'+dest.name,'origin':'2차'})
tentative=[]
for ident,n in [('demolish',1),('round_start',3)]:
 g=next(g for g in r2['groups'] if g['id']==ident);c=g['clips'][n-1];p=ROOT/c['source_wav'];dest=REF/('r2_'+p.name);shutil.copyfile(p,dest)
 tentative.append({'id':ident,'title':g['title'],'number':n,'url':'reference/'+dest.name,'sha256':sha(dest),'note':'잠정 선호 · 최종 채택 아님'})
current=ROOT/'Assets/Resources/CoreGuardian/Audio/RoundStart/03_Confirmation004.ogg'
assert current.exists();dest=REF/'current_game_round_start.ogg';shutil.copyfile(current,dest)
extras=[{'url':'reference/'+dest.name,'sha256':sha(dest),'title':'현재 게임 연결 라운드 시작음'}]
cat={'title':'코어 가디언 · 다른 컨셉으로 비교','version':'B-feedback-r3-20261019','direction':'B · 단단하고 빠른 전술','groups':groups,'retained':retained,'tentative':tentative,'extras':extras,'multi':{**r2['multi'],'clips':[]},'flows':[]}
feedback={'game_application_authorized':False,'new_preferences':new_choices,'tentative':{'demolish':1,'round_start':3},'rejected':{'aug_gold':'③ 출력 번호와 무관하게 불만족. 통통 튀는 소리 거부.','aug_platinum':'③ 출력 번호와 무관하게 불만족. 통통 튀는 소리 거부.'},'request':'같은 컨셉의 강도/길이 버전이 아닌 서로 다른 컨셉 비교','round_start':'준비에서 전투 단계 전환. 자동 타이머 종료/StartCombat 정상 호출. 이번 후보③ 게임 미적용.'}
for path,h in protected.items():assert sha(ROOT/path)==h
for name,obj in [('catalog.json',cat),('feedback.json',feedback),('production.json',{'metrics':metrics,'source_hashes':sources,'protected_hashes':protected,'note':'No Riot audio used or directly auditioned. No agent subjective listening or Unity tests.'})]:
 (HERE/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
(HERE/'catalog.js').write_text('window.SOUND_CATALOG='+json.dumps(cat,ensure_ascii=False)+';\n',encoding='utf-8',newline='\n')
print(json.dumps({'groups':len(groups),'new':9,'retained':len(retained),'tentative':2,'protected':len(protected)}))
