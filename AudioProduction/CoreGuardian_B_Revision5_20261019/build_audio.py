"""Gacha reward cues: bright cadence, heroic fanfare, brilliant reveal. No Unity writes."""
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
for dirname in ['CoreGuardian_B_Revision4_20261019','CoreGuardian_B_Revision3_20261019','CoreGuardian_B_Revision2_20261019','CoreGuardian_B_SoundLibrary_20261019','CoreGuardian_ConceptAB_v2_20261019','CoreGuardian_ConceptAB_20261019','CoreGuardian_BGM_20261013']:
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
groups=[{'id':'aug_gold','title':'증강 공개 · 골드','category':'증강','status':'새 보상 컨셉 · 미선택','description':'가챠에서 좋은 등급을 얻었다는 신호. 밝은 당첨/영웅 팡파르/빛의 폭발 세 방향입니다.','clips':[]},{'id':'aug_platinum','title':'증강 공개 · 플래티넘','category':'증강','status':'새 해방 컨셉 · 미선택','description':'같은 계열의 골드보다 큰 고등급 공개. 기대감·짧은 틈·확정 화음·고음 장식으로 최고등급을 구분합니다.','clips':[]}]
names=['금빛 당첨 · 반짝이는 확정','영웅 등장 · 짧은 팡파르','최고등급 · 빛의 폭발']
descs=['빠른 기대감 뒤 밝은 장조 화음과 반짝임으로 당첨 확정','짧은 금관 리듬이 상승하고 큰 화음으로 해결되는 영웅 보상','빛이 모인 뒤 짧은 틈을 지나 넓은 화음과 반짝임이 터지는 고등급 공개']
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
def note(midi,duration=.22,kind='bell',attack=.003):
 t=np.arange(round(duration*SR))/SR;f=440*2**((midi-69)/12);y=np.zeros(len(t))
 if kind=='bell':
  for j,ratio in enumerate([1,2,3,4,6]):y+=np.sin(2*np.pi*f*ratio*t)*np.exp(-t/(.17/(1+j*.55)))/((1+j)**1.9)
  env=1-np.exp(-t/.0015)
 elif kind=='horn':
  for h in range(1,11):y+=np.sin(2*np.pi*f*h*t)*np.exp(-h/5)/(h**.85)
  env=(1-np.exp(-t/.013))*np.exp(-t/.30)
 else:
  for h in range(1,8):y+=np.sin(2*np.pi*f*h*t+.14*h)/(h**2)
  env=(1-np.exp(-t/attack))*np.exp(-t/.4)
 return fade(y*env*.16,attack,min(.11,duration*.4))
def chord(midis,duration,kind='horn'):
 return mix([(0,note(m,duration,kind,attack=.008 if kind=='horn' else .004),1/math.sqrt(len(midis))) for m in midis],duration)
for tier,g in enumerate(groups):
 for k in range(3):
  plat=bool(tier);duration=([1.30,1.40,1.52] if not plat else [2.05,2.10,2.30])[k]
  reveal=([.21,.25,.33] if not plat else [.37,.43,.58])[k]
  events=[]
  if k==0:
   # A: bright upper-register arpeggio, then a clear major-sixth reward chord.
   lead=[67,72,76] if not plat else [67,72,76,79,84]
   times=np.linspace(.015,reveal-.045,len(lead))
   for at,m in zip(times,lead):events.append((float(at),note(m,.24,'bell'),.60))
   events.extend([(reveal,chord([60,64,67,72,76] if not plat else [48,60,64,67,72,76,79,84],duration-reveal,'horn'),1.10),
                  (reveal+.025,chord([79,84,88] if not plat else [84,88,91,96],.65 if not plat else .95,'bell'),.50)])
   ornaments=[84,88,91] if not plat else [84,88,91,96,100]
   for j,m in enumerate(ornaments):events.append((reveal+.12+j*.045,note(m,.25,'bell'),.20))
  elif k==1:
   # B: a recognizable short heroic fanfare, not an atmospheric drone.
   lead=[67,67,72] if not plat else [67,72,76,79]
   times=[.0,.10,.23] if not plat else [.02,.13,.25,.40]
   for at,m in zip(times,lead):events.append((at,note(m,.18 if not plat else .22,'horn'),.80))
   events.extend([(reveal,chord([48,60,64,67,72] if not plat else [48,55,60,64,67,72,76,84],duration-reveal,'horn'),1.10),
                  (reveal+.035,chord([72,76,79] if not plat else [76,79,84,88],.70 if not plat else 1.10,'bell'),.30)])
   if plat:events.append((reveal+.26,chord([72,76,79,84],.85,'horn'),.25))
  else:
   # C: a gathering cascade, deliberate small gap, then a brilliant resolved reveal.
   lead=[60,67,72,76,79] if not plat else [60,67,72,76,79,84,88]
   until=reveal-(.07 if not plat else .14)
   for at,m in zip(np.linspace(0,until,len(lead)),lead):events.append((float(at),note(m,.13,'bell'),.40))
   swell=chord([67,72,76],reveal-.055,'soft')[::-1].copy();events.append((0,fade(swell,.03,.045),.35 if not plat else .5))
   events.extend([(reveal,chord([48,60,64,67,72,79] if not plat else [48,55,60,64,67,72,76,79,84],duration-reveal,'horn'),.85),
                  (reveal,chord([76,79,84] if not plat else [79,84,88,91],duration-reveal,'bell'),.60),
                  (reveal+.035,noise(.30 if not plat else .5,1300,6500,attack=.01,decay=.16 if not plat else .28),.09)])
   if plat:
    for j,m in enumerate([91,88,84,79]):events.append((reveal+.19+j*.065,note(m,.32,'bell'),.18))
  # Shared tactile opening, low musical root and broadband accent under the resolved reveal.
  events.extend([(reveal,anchor,.55),(reveal,hit,.11),(reveal,resonator(130.81,[1,2,3],.26,.065),.55)])
  x=mix(events,duration);x=stereo_space(x,room=[.36,.40,.52][k],wet=.10 if not plat else .15)
  x=fade(x,.002,.18);x*=10**((reference_target-loud(x))/20);x*=min(1,.73/peak(x))
  p=OUT/f"{g['id']}_{k+1}.wav";sf.write(p,x,SR,subtype='PCM_16');z,r=sf.read(p);assert peak(z)<.9 and np.isfinite(z).all();rendered[g['id'],k]=z
  c={'number':k+1,'title':names[k],'description':descs[k],'url':'audio/'+p.name,'source_wav':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':sha(p),'duration':len(z)/r}
  gap=round(.55*SR);repeat=np.concatenate([np.concatenate([z,np.zeros((gap,2))]) for _ in range(3)]);rp=OUT/f"{g['id']}_{k+1}_repeat.wav";sf.write(rp,repeat,SR,subtype='PCM_16');c['repeat_url']='audio/'+rp.name;g['clips'].append(c)
  metrics[p.name]={'lufs':loud(z),'estimated_dbtp':20*math.log10(peak(z)),'channels':2,'reveal_seconds':reveal}
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
cat={'title':'코어 가디언 · 고등급 당첨음 검사','version':'B-feedback-r5-20261019','direction':'B · 단단하고 빠른 전술','groups':groups,'retained':retained,'tentative':tentative,'extras':[],'multi':{'approved_policy':'실제 발사 수에 맞춰1·2·3발 구분 · 게임 미연결','clips':[]},'flows':[r[key] for r in flow_records for key in ['dry','music']],'tier_sets':flow_records}
for path,h in protected.items():assert sha(ROOT/path)==h
for c in retained:assert sha(HERE/c['url'])==c['sha256']
for name,obj in [('catalog.json',cat),('production.json',{'reference_target_lufs':reference_target,'metrics':metrics,'source_hashes':sources,'protected_hashes':protected,'method':'Bright arpeggios, heroic fanfare rhythms, major-sixth and octave resolutions with sparkle; chosen silver attack and CC0 hit onset. No Riot audio or AI service calls; no direct subjective audition.'})]:
 (HERE/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
(HERE/'catalog.js').write_text('window.SOUND_CATALOG='+json.dumps(cat,ensure_ascii=False)+';\n',encoding='utf-8',newline='\n')
print(json.dumps({'groups':2,'new_cues':6,'tier_chains':3,'bgm_chains':3,'retained':21,'protected':len(protected)}))
