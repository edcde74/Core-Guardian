"""Tier reward cues: modal metal, stone chamber, harmonic magic. No Unity writes."""
from pathlib import Path
import sys,json,math,hashlib,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln
from scipy.signal import butter,sosfilt,resample_poly
from pedalboard import Pedalboard,Reverb,HighpassFilter,LowpassFilter
SR=44100;OUT=HERE/'audio';REF=HERE/'reference';OUT.mkdir(exist_ok=True);REF.mkdir(exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
R3=HERE.parent/'CoreGuardian_B_Revision3_20261019';r3=json.loads((R3/'catalog.json').read_text(encoding='utf-8'));after=json.loads((R3/'feedback_after_r3.json').read_text(encoding='utf-8'))
sources={};protected={}
for dirname in ['CoreGuardian_B_Revision3_20261019','CoreGuardian_B_Revision2_20261019','CoreGuardian_B_SoundLibrary_20261019','CoreGuardian_ConceptAB_v2_20261019','CoreGuardian_ConceptAB_20261019','CoreGuardian_BGM_20261013']:
 for p in (HERE.parent/dirname).rglob('*'):
  if p.is_file() and p.suffix.lower() in ['.wav','.ogg','.mid']:protected[str(p.relative_to(ROOT))]=sha(p)
def read(p):
 sources[str(p.relative_to(ROOT))]=sha(p);x,r=sf.read(p,always_2d=True);x=x.mean(axis=1)
 if r!=SR:x=resample_poly(x,SR//math.gcd(r,SR),r//math.gcd(r,SR))
 return x.astype(np.float32)
silver_record=next(c for c in r3['retained'] if c['id']=='aug_silver');silver_path=ROOT/silver_record['original_wav'];silver=read(silver_path)
hit_path=ROOT/'AudioProduction/CoreGuardian_Victory_20261019/Kenney_MusicJingles/Originals/Audio/Hit jingles/jingles_HIT00.ogg'
hit=read(hit_path);hit=hit[:round(.085*SR)];hit/=max(.001,np.max(abs(hit)))
rng=np.random.default_rng(2026101904)
def fade(x,a=.003,z=.06):
 y=np.array(x,dtype=np.float32,copy=True);aa=min(len(y)//2,round(a*SR));zz=min(len(y)//2,round(z*SR))
 if aa:y[:aa]*=np.linspace(0,1,aa).reshape((-1,)+(1,)*(y.ndim-1))
 if zz:y[-zz:]*=np.linspace(1,0,zz).reshape((-1,)+(1,)*(y.ndim-1))
 return y
def bp(x,lo,hi):return sosfilt(butter(3,[lo,hi],btype='bandpass',fs=SR,output='sos'),x).astype(np.float32)
def noise(dur,lo,hi,attack=.003,decay=.10):
 t=np.arange(round(dur*SR))/SR;x=bp(rng.normal(0,.4,len(t)),lo,hi)*np.exp(-t/decay);return fade(x,attack,min(.1,dur*.35))
def mix(events,duration):
 x=np.zeros(round(duration*SR),np.float32)
 for at,y,g in events:
  i=round(at*SR);n=min(len(y),len(x)-i)
  if n>0:x[i:i+n]+=y[:n]*g
 return x
def resonator(base,ratios,duration,decay,attack=.004):
 t=np.arange(round(duration*SR))/SR;x=np.zeros(len(t))
 for i,ratio in enumerate(ratios):
  # Fixed acoustic modes; no pitch glides, rhythmic note runs or repeated strikes.
  x+=np.sin(2*np.pi*base*ratio*t)*np.exp(-t/(decay/(1+i*.27)))/(1+i*.8)
 return fade(x*.18,attack,.18)
def bloom(freqs,duration,attack=.08,release=.6,texture=0):
 t=np.arange(round(duration*SR))/SR;env=(1-np.exp(-t/attack))*np.exp(-t/release);x=np.zeros(len(t))
 for i,f in enumerate(freqs):
  for harmonic in range(1,6):
   phase=.17*i+.3*harmonic
   x+=np.sin(2*np.pi*f*harmonic*t+phase)*env/(harmonic**(1.8 if texture==0 else 2.4)*len(freqs))
 return fade(x*.24,.015,.2)
anchor=fade(silver[:round(.09*SR)],.002,.025)
groups=[{'id':'aug_gold','title':'증강 공개 · 골드','category':'증강','status':'새 보상 컨셉 · 미선택','description':'실버보다 귀한 힘이 드러나는 한 번의 충격과 공명. 세 후보는 황동/석실/마력 화음의 서로 다른 재료입니다.','clips':[]},{'id':'aug_platinum','title':'증강 공개 · 플래티넘','category':'증강','status':'새 해방 컨셉 · 미선택','description':'응축→개방→확장으로 판을 바꿀 힘을 표현했습니다. 같은 번호의 골드와 같은 재료 계열이며 단순 음량·저역 변경이 아닙니다.','clips':[]}]
names=['황동 유물의 각성','고대 석실의 개방','코어 문양의 공명']
descs=['단단한 금속 충격 뒤 비정수 배음이 퍼지는 황동 공명','돌문이 풀린 뒤 낮고 두꺼운 석실 화음이 남는 공간','마력 문양이 열리며 따뜻한 화음이 층으로 펼쳐지는 보상']
rendered={};meter=pyln.Meter(SR);metrics={}
# Preserve the mono silver waveform in stereo chains and match its measured reference level.
ref_gap=max(.5,len(silver)/SR+.18);ref_piece=np.pad(np.repeat(silver[:,None],2,axis=1),((0,round((ref_gap-len(silver)/SR)*SR)),(0,0)));reference_target=float(meter.integrated_loudness(np.tile(ref_piece,(5,1))))
def stereo_space(x,room,wet):
 # Common direct attack with decorrelated quiet ambience; no hard L/R separation.
 left=x;right=np.pad(x,(round(.011*SR),0))[:len(x)]
 stereo=np.stack([left*.86+right*.14,right*.14+left*.86],axis=0)
 stereo[1]=left*.86+np.pad(x,(round(.019*SR),0))[:len(x)]*.14
 return Pedalboard([HighpassFilter(cutoff_frequency_hz=48),LowpassFilter(cutoff_frequency_hz=5700),Reverb(room_size=room,damping=.65,wet_level=wet,dry_level=1,width=1)])(stereo.astype(np.float32),SR).T
def peak(x):return float(np.max(abs(resample_poly(x,4,1,axis=0))))
def loud(x):
 if len(x)<SR*.4:x=np.pad(x,((0,SR//2),(0,0)))
 return float(meter.integrated_loudness(x))
for tier,g in enumerate(groups):
 for k in range(3):
  plat=bool(tier);duration=([1.28,1.45,1.38] if not plat else [2.05,2.22,2.12])[k];onset=.035 if not plat else [.19,.24,.17][k]
  pre=noise(onset+.02,160,1700,attack=onset*.6,decay=.5)*(.35 if plat else .06)
  body=noise(.30 if not plat else .46,48,320,decay=.085 if not plat else .14)
  events=[(0,pre,.7),(onset,anchor,.65),(onset,body,1.1 if not plat else 1.35)]
  if k==0:
   events.extend([(onset,hit,.065),(onset,resonator(132 if not plat else 98,[1,1.48,2.12,2.58,3.18,4.07,5.4],duration-onset,.48 if not plat else .78),.82),
     (onset+.035,bloom([196,247,294] if not plat else [147,196,247,294,392],duration-onset-.035,attack=.045 if not plat else .11,release=.43 if not plat else .8),.46)])
  elif k==1:
   events.extend([(0,noise(onset+.12,180,1300,attack=.03,decay=.35),.25),(onset,resonator(83,[1,1.33,2.02,2.99],duration-onset,.32 if not plat else .6),.8),
     (onset+.06,bloom([110,147,165] if not plat else [82.4,110,147,165,220],duration-onset-.06,attack=.10 if not plat else .17,release=.60 if not plat else 1,texture=1),.95)])
  else:
   events.extend([(onset,hit,.04),(onset,resonator(164,[1,2,3.01,4.98],duration-onset,.22 if not plat else .35),.44),
     (onset+.025,bloom([164.81,207.65,246.94] if not plat else [123.47,164.81,207.65,246.94,329.63,415.3],duration-onset-.025,attack=.065 if not plat else .12,release=.58 if not plat else .95),1.0),
     (onset+.06,noise(.40 if not plat else .7,800,3600,attack=.10,decay=.25 if not plat else .42),.12)])
  x=mix(events,duration);x=stereo_space(x,room=[.36,.58,.44][k],wet=.09 if not plat else .15)
  x=fade(x, .003, .20) # fades both channels along time
  x*=10**((reference_target-loud(x))/20);x*=min(1,.73/peak(x))
  p=OUT/f"{g['id']}_{k+1}.wav";sf.write(p,x,SR,subtype='PCM_16');z,r=sf.read(p);assert peak(z)<.9 and np.isfinite(z).all();rendered[g['id'],k]=z
  c={'number':k+1,'title':names[k],'description':descs[k],'url':'audio/'+p.name,'source_wav':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':sha(p),'duration':len(z)/r}
  gap=round(.55*SR);repeat=np.concatenate([np.concatenate([z,np.zeros((gap,2))]) for _ in range(3)]);rp=OUT/f"{g['id']}_{k+1}_repeat.wav";sf.write(rp,repeat,SR,subtype='PCM_16');c['repeat_url']='audio/'+rp.name;g['clips'].append(c)
  metrics[p.name]={'lufs':loud(z),'estimated_dbtp':20*math.log10(peak(z)),'channels':2}
retained=[]
for c in r3['retained']:
 p=ROOT/c['original_wav'];dest=REF/p.name;shutil.copyfile(p,dest);retained.append({**c,'url':'reference/'+dest.name})
tentative=[]
for ident,src,n,name in [('demolish',ROOT/after['demolish']['source_wav'],2,'3차 마력 회수'),('round_start',ROOT/'AudioProduction/CoreGuardian_B_Revision2_20261019/audio/round_start_3.wav',3,'2차 짧은 이중 출격')]:
 dest=REF/('tentative_'+src.name);shutil.copyfile(src,dest);tentative.append({'id':ident,'number':n,'title':name,'url':'reference/'+dest.name,'sha256':sha(dest),'note':'잠정 선호·게임 적용과 별도'})
# The chosen silver is copied exactly, not processed to fake the contrast.
silver_stereo=np.repeat(silver[:,None],2,axis=1);flow_records=[]
bgm_record=next(c for c in retained if c['id']=='aug_bgm');music=read(ROOT/bgm_record['original_wav'])
for k in range(3):
 parts=[silver_stereo,rendered['aug_gold',k],rendered['aug_platinum',k]];gap=np.zeros((round(.7*SR),2));chain=np.concatenate([np.concatenate([x,gap]) for x in parts]);p=OUT/f'tier_chain_{k+1}.wav';sf.write(p,chain,SR,subtype='PCM_16')
 bed=np.tile(music,math.ceil(len(chain)/len(music)))[:len(chain)];bed=np.repeat(bed[:,None],2,axis=1)*10**(-9/20)
 contextual=chain+bed;assert peak(contextual)<.95
 q=OUT/f'tier_bgm_{k+1}.wav';sf.write(q,contextual,SR,subtype='PCM_16');flow_records.append({'name':names[k],'dry':'audio/'+p.name,'music':'audio/'+q.name,'starts':[0,(len(parts[0])/SR+.7),(len(parts[0])+len(parts[1]))/SR+1.4]})
cat={'title':'코어 가디언 · 증강 등급 보상음 검사','version':'B-feedback-r4-20261019','direction':'B · 단단하고 빠른 전술','groups':groups,'retained':retained,'tentative':tentative,'extras':[],'multi':{'approved_policy':'실제 발사 수에 맞춰1·2·3발 구분 · 게임 미연결','clips':[]},'flows':[r[key] for r in flow_records for key in ['dry','music']],'tier_sets':flow_records}
for path,h in protected.items():assert sha(ROOT/path)==h
for c in retained:assert sha(HERE/c['url'])==c['sha256']
for name,obj in [('catalog.json',cat),('production.json',{'reference_target_lufs':reference_target,'metrics':metrics,'source_hashes':sources,'protected_hashes':protected,'method':'Fixed modal resonators and simultaneous harmonic blooms, chosen silver attack and CC0 hit onset. No Riot audio or AI service calls; no direct subjective audition.'})]:
 (HERE/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
(HERE/'catalog.js').write_text('window.SOUND_CATALOG='+json.dumps(cat,ensure_ascii=False)+';\n',encoding='utf-8',newline='\n')
print(json.dumps({'groups':2,'new_cues':6,'tier_chains':3,'bgm_chains':3,'retained':21,'protected':len(protected)}))
