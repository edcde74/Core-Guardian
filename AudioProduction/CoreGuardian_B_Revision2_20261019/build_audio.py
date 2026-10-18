"""Feedback revision: preserve selected sources, rebuild rejected roles, no Unity writes."""
from pathlib import Path
from fractions import Fraction
import sys,json,math,hashlib,subprocess,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln,mido,imageio_ffmpeg
from scipy.signal import resample_poly
from pedalboard import Pedalboard,HighpassFilter,LowpassFilter,Compressor,Reverb
SR=44100;OUT=HERE/'audio';OUT.mkdir(exist_ok=True);REF=HERE/'reference';REF.mkdir(exist_ok=True)
LIB=ROOT/'AudioProduction/CoreGuardian_B_SoundLibrary_20261019'
original=json.loads((LIB/'catalog.json').read_text(encoding='utf-8'));feedback=json.loads((HERE/'feedback.json').read_text(encoding='utf-8'))
sources={};protected={}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for directory in ['CoreGuardian_B_SoundLibrary_20261019/audio','CoreGuardian_ConceptAB_v2_20261019/audio','CoreGuardian_ConceptAB_20261019/audio','CoreGuardian_BGM_20261013']:
 for p in (ROOT/'AudioProduction'/directory).glob('*'):
  if p.is_file():protected[str(p.relative_to(ROOT))]=sha(p)
def read(p):
 sources[str(p.relative_to(ROOT))]=sha(p);x,r=sf.read(p,always_2d=True);x=x.mean(axis=1)
 if r!=SR:x=resample_poly(x,SR//math.gcd(SR,r),r//math.gcd(SR,r))
 ii=np.flatnonzero(abs(x)>max(.001,max(abs(x))*.012))
 if len(ii):x=x[max(0,ii[0]-88):]
 return x/max(.001,max(abs(x)))
def fade(x,a=.002,z=.03):
 x=np.array(x,dtype=np.float32,copy=True);aa=min(len(x)//2,round(SR*a));zz=min(len(x)//2,round(SR*z))
 if aa:x[:aa]*=np.linspace(0,1,aa)
 if zz:x[-zz:]*=np.linspace(1,0,zz)
 return x
def pitch(x,s):
 f=Fraction(2**(-s/12)).limit_denominator(512);return resample_poly(x,f.numerator,f.denominator).astype(np.float32)
def cut(x,t,decay=None):
 x=x[:round(t*SR)].copy()
 if decay:x*=np.exp(-np.arange(len(x))/SR/decay)
 return fade(x)
def mix(events,duration=None):
 n=round(duration*SR) if duration else max(round(t*SR)+len(x) for t,x,g in events);y=np.zeros(n,dtype=np.float32)
 for t,x,g in events:
  i=round(t*SR);c=min(len(x),n-i)
  if c>0:y[i:i+c]+=x[:c]*g
 return y
def shape(x,lp=4200,hp=70,room=0,tail=.08):
 fx=[HighpassFilter(cutoff_frequency_hz=hp),LowpassFilter(cutoff_frequency_hz=lp),Compressor(threshold_db=-18,ratio=1.4,attack_ms=4,release_ms=70)]
 if room:fx.append(Reverb(room_size=.30,damping=.7,wet_level=room,dry_level=1,width=.5))
 return fade(Pedalboard(fx)(np.pad(x,(0,round(tail*SR))),SR),.001,.04)
def balance(x,rms=.12):
 a=x[abs(x)>.003];r=float(np.sqrt(np.mean(a*a))) if len(a) else .1
 return x*min(rms/max(.001,r),.7/max(.001,max(abs(x))))
UI=ROOT/'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio'
J=ROOT/'AudioProduction/CoreGuardian_Victory_20261019/Kenney_MusicJingles/Originals/Audio'
raw={n:read(UI/(n+'.ogg')) for n in ['bong_001','switch_004','click_002','click_003','drop_002','scratch_001','close_001']}
steel=cut(read(J/'Steel jingles/jingles_STEEL00.ogg'),.14)
string=cut(read(J/'Pizzicato jingles/jingles_PIZZI00.ogg'),.125)
unequip=read(ROOT/'Assets/Resources/CoreGuardian/Audio/TowerInstall/02_Unequip.wav')
magic=read(ROOT/'Assets/Resources/CoreGuardian/Audio/TowerShot/03_Aura.wav')
rng=np.random.default_rng(20261019)
paper=shape(fade(rng.normal(0,.12,round(.12*SR)).astype(np.float32),.025,.04),lp=2400,hp=700,tail=.01)
cache={}
def note(m,family=0):
 key=(m,family)
 if key not in cache:
  hz=440*2**((m-69)/12);p=pitch(string,12*math.log2(hz/464.3));s=pitch(steel,12*math.log2(hz/472.4));n=max(len(p),len(s));p=np.pad(p,(0,n-len(p)));s=np.pad(s,(0,n-len(s)))
  cache[key]=balance(fade(p*[.9,.65,.45][family]+s*[.1,.35,.55][family],.001,.03),.14)
 return cache[key]
def phrase(notes,times,family=0):return mix([(t,note(p,family),.6) for t,p in zip(times,notes)])
sets={};groups=[]
def add(ident,title,category,description,clips,names,notes=None):
 sets[ident]=clips;groups.append({'id':ident,'title':title,'category':category,'description':description,'status':'재제작 · 미선택','clips':[{'title':name,'description':(notes or ['새 컨셉으로 제작한 비교안']*3)[i],'number':i+1} for i,name in enumerate(names)]})

# UI contains NO pitched notes, glass or bell layer. Only short Foley transients.
ui_sets={'ui_click':[],'ui_open':[],'ui_close':[]};dem=[];cancel=[];starts=[]
for k in range(3):
 click=cut(raw['click_002'] if k==1 else raw['click_003'],[.025,.015,.035][k],.012)
 lever=cut(raw['switch_004'],.035,.012)
 body=cut(pitch(raw['bong_001'],-8),.15,.055)
 ui_sets['ui_click'].append(shape(mix([(0,click,.65),(0,lever,.1 if k!=2 else .22)]),lp=[2300,1600,3000][k],hp=180,tail=.025))
 ui_sets['ui_open'].append(shape(mix([(0,cut(paper,.07),.6),(.035,click,.3),(.015,cut(raw['scratch_001'],.055,.02),.15)]),lp=[2300,1800,3000][k],hp=240,tail=.025))
 ui_sets['ui_close'].append(shape(mix([(0,click,.4),(.015,cut(raw['close_001'],.045,.015),.25),(0,cut(paper,.035),.25)]),lp=[2300,1500,2800][k],hp=220,tail=.025))
 dem.append(shape(mix([(0,cut(unequip,.14,.055),.35),(0,lever,.3),(.065,pitch(body,-2),.55),(.1,cut(raw['drop_002'],.10,.04),.25)]),lp=3400+k*250,tail=.1))
 cancel.append(shape(mix([(0,click,.35),(.025,cut(paper,[.035,.045,.065][k]),.22),(.018,lever,.08)]),lp=1800+k*400,hp=200,tail=.03))
 # Start cue is kinetic readiness rather than a melodic notification.
 starts.append(shape(mix([(0,cut(paper,.08),[.4,.7,.3][k]),(.08,lever,.5),(.11,body,.6),(.17 if k!=1 else .23,lever,.32)]),lp=3600+k*250,tail=.08))
add('demolish','포탑 철거','설치·강화','기존 포탑을 회수하는 소리. 설치 모드 취소와 분리했습니다.',dem,['잠금 해제·회수','묵직한 분리','짧은 장치 회수'])
add('cancel_install','Esc · 설치 취소','설치·강화','아직 설치하지 않은 모드를 취소하는 작은 반응. 철거의 무게와 보상감을 주지 않습니다.',cancel,['작은 취소 클릭','조용한 해제','가벼운 접힘'])
add('round_start','라운드 전투 시작','라운드·결과','기존 멜로디 3안은 폐기했습니다. 장치 준비·잠금·출격의 짧은 동작감으로 다시 만들었습니다.',starts,['준비 잠금·출격','흡입 후 체결','짧은 이중 출격'])
complete=[];wins=[];losses=[];family_names=['따뜻한 해방','선명한 성취','여운 있는 마침']
for k in range(3):
 # Shared G-B-D identity. The defeat changes its direction/harmony, not its material family.
 motifs=[[67,71,74],[62,67,71,74],[67,74,71,79]][k]
 complete.append(shape(phrase(motifs,[i*[.13,.11,.18][k] for i in range(len(motifs))],k),lp=4900,room=.10,tail=.35))
 win_notes=[[67,71,74,76,74,79],[62,67,71,74,79,83,79],[67,74,71,76,74,79]][k]
 spacings=[.20,.17,.24][k];vict=phrase(win_notes,[i*spacings for i in range(len(win_notes))],k)
 end=(len(win_notes)-1)*spacings
 winmix=mix([(0,vict,1),(end+.04,note(55,k),.25),(end+.06,note(59,k),.23),(end+.08,note(62,k),.2)])
 wins.append(shape(winmix,lp=5200,room=.23,tail=1.0))
 fall=[[74,71,67,63,55],[71,67,62,58,50],[79,74,71,67,63,55]][k]
 losses.append(shape(phrase(fall,[i*[.20,.19,.23][k] for i in range(len(fall))],k),lp=3900,room=.16,tail=.65))
add('round_complete','라운드 완료','라운드·결과','기존 3안은 모두 폐기. 아래 승리·패배와 번호별로 같은 재료와 모티프를 공유합니다.',complete,family_names)
add('victory','최종 승리','라운드·결과','기존③은 선호 참고만 유지. 작은 성공 알림보다 길게 펼쳐지며, 힘들게 이긴 해방감을 가볍게 마무리하도록 재제작했습니다.',wins,family_names)
add('defeat','패배','라운드·결과','라운드 완료·승리와 같은 번호가 같은 결과음 계열입니다. 실패는 하행과 화성 변화로 구분합니다.',losses,family_names)
for ident,title in [('ui_click','UI 클릭·선택'),('ui_open','패널 열기'),('ui_close','패널 닫기·접기')]:
 add(ident,title,'UI','기존②는 비교상 선호였으며 최종 채택이 아닙니다. 앞쪽 띵띵·음정 모티프·유리 공명을 제거했습니다.',ui_sets[ident],['짧은 기계 동작','조용하고 둔한 동작','선명한 촉각 반응'],['음정 없는 짧은 폴리','고역과 꼬리를 더 줄인 폴리','작은 체결 질감을 남긴 폴리'])
aug={}
for tier,ident in enumerate(['aug_silver','aug_gold','aug_platinum']):
 clips=[]
 for k in range(3):
  low=cut(pitch(raw['bong_001'],-7-tier),.18+tier*.035,.065+tier*.02)
  swipe=cut(paper,.06+k*.02);foley=cut(unequip,.13,.05)
  x=mix([(0,swipe,[.35,.65,.48][k]),(.055,low,.65),(.03,foley,.25),(.07,cut(raw['drop_002'],.09,.035),.22),(.07,cut(raw['switch_004'],.025,.01),.1)])
  if tier: x=mix([(0,x,1),(.095,note(52+tier*7,2),.045+tier*.025)])
  if tier==2:x=mix([(0,x,1),(.13,low,.2)])
  clips.append(shape(x,lp=3000+k*400,room=.025 if k<2 else .06,tail=.08+tier*.04))
 aug[ident]=clips
 add(ident,'증강 공개 · '+['실버','골드','플래티넘'][tier],'증강','②를 출발점으로 멜로디를 덜고 패를 뒤집어 탁 놓는 쓸림·중저역 접촉을 중심으로 다시 만들었습니다.',clips,['탁 · 카드 안착','슥·탁 · 카드 뒤집기','묵직한 봉인 해제'])

# Dedicated augmentation bed: fewer notes, slower pulse, more room for reveal sounds.
bgms=[];durations=[]
for k,bpm in enumerate([80,88,92]):
 beat=60/bpm;duration=16*beat;durations.append(duration);events=[];mid=mido.MidiFile();tr=mido.MidiTrack();mid.tracks.append(tr);tr.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(bpm)));midi_events=[]
 chords=[[52,55,59],[48,52,55],[50,54,57],[47,50,54]]
 for bar,chord in enumerate(chords):
  t=bar*4*beat
  for b in [0,2]:
   p=chord[0]-12;events.append((t+b*beat,note(p,2),.25));midi_events.extend([(round((bar*4+b)*480),True,p),(round((bar*4+b+.5)*480),False,p)])
  accents=([1,3] if k!=1 else [1.5,3.5])
  for i,b in enumerate(accents):events.append((t+b*beat,note(chord[i+1]+12,1),.13 if k<2 else .18))
  if k==2:events.append((t+2.5*beat,cut(raw['click_002'],.01),.025))
 x=shape(mix(events,duration+.5),lp=3600,room=.13,tail=.25);loop=x[:round(duration*SR)].copy();tail=x[len(loop):];loop[:len(tail)]+=tail;bgms.append(loop)
 prev=0
 for tick,on,p in sorted(midi_events):tr.append(mido.Message('note_on' if on else 'note_off',note=p,velocity=55 if on else 0,time=tick-prev));prev=tick
 mid.save(OUT/f'aug_bgm_{k+1}.mid')
add('aug_bgm','증강 선택 BGM','BGM','전투 BGM① 선호는 유지합니다. 증강 공개·고민 시간에 전환할 별도의 차분한 음악 스케치입니다.',bgms,['조용한 패의 긴장','낮은 장치의 맥박','은은한 선택의 공명'],['80 BPM · 여백이 큰 선택 시간','88 BPM · 낮은 박동과 공간','92 BPM · 작은 움직임과 공명'])

meter=pyln.Meter(SR)
def loudness(x):
 if len(x)>3*SR:return float(meter.integrated_loudness(x))
 gap=max(.5,len(x)/SR+.18);return float(meter.integrated_loudness(mix([(i*gap,x,1) for i in range(5)],5*gap)))
def truepeak(x):return float(max(abs(resample_poly(x,4,1))))
metrics={};ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
for g in groups:
 ident=g['id'];clips=sets[ident];target=-30 if g['category']=='UI' or ident=='cancel_install' else -26 if ident=='aug_bgm' else -23
 normalized=[x*10**((target-loudness(x))/20) for x in clips];gain=min(1,10**(-2.4/20)/max(truepeak(x) for x in normalized))
 for k,x in enumerate(normalized):
  x*=gain;p=OUT/f'{ident}_{k+1}.wav';sf.write(p,x,SR,subtype='PCM_16');z,r=sf.read(p);assert np.isfinite(z).all() and truepeak(z)<.9
  preview=p
  if ident=='aug_bgm':
   preview=p.with_suffix('.ogg');subprocess.run([ffmpeg,'-nostdin','-hide_banner','-loglevel','error','-y','-i',str(p),'-c:a','libvorbis','-q:a','6',str(preview)],capture_output=True,check=True)
  c=g['clips'][k];c.update({'url':str(preview.relative_to(HERE)) .replace('\\','/'),'source_wav':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':sha(p),'duration':len(z)/r});metrics[p.name]={'lufs':loudness(z),'estimated_dbtp':20*math.log10(truepeak(z))}
  if len(z)<SR*3:
   gap=max(.5,len(z)/SR+.18);repeat=OUT/f'{ident}_{k+1}_repeat.wav';sf.write(repeat,mix([(i*gap,z,1) for i in range(5)],5*gap),SR,subtype='PCM_16');c['repeat_url']=str(repeat.relative_to(HERE)).replace('\\','/')
  sets[ident][k]=z

# Reconstruct the chosen multi ② layer recipe before its three pulses are combined.
body=cut(pitch(raw['bong_001'],-6),.23,.14);latch=cut(raw['switch_004'],.075,.025)
shot=mix([(0,body,.85),(.008,cut(pitch(magic,-2),.26,.10),.4),(0,latch,.25)])
multi=[]
for count in [1,2,3]:
 x=mix([(t,cut(pitch(shot,p),.29),g) for t,p,g in [(0,-3,.72),(.085,-1,.66),(.17,1,.6)][:count]])
 multi.append(shape(x,lp=4800,tail=.1))
mgain=min(.7/max(truepeak(x) for x in multi),1.5)
for i,x in enumerate(multi):sf.write(OUT/f'multi_{i+1}_targets.wav',x*mgain,SR,subtype='PCM_16')

retained=[]
for ident,n in feedback['retained_preferences'].items():
 g=next(g for g in original['groups'] if g['id']==ident);c=g['clips'][n-1];p=ROOT/c['source_wav'];dest=REF/f'{ident}_{n}.wav';shutil.copyfile(p,dest);assert sha(dest)==c['sha256']
 retained.append({'id':ident,'title':g['title'],'number':n,'candidate':c['title'],'url':str(dest.relative_to(HERE)).replace('\\','/'),'original_wav':c['source_wav'],'sha256':c['sha256']})

# Three listening flows compare music only; reveal cue and timing stay the same.
combat,_=sf.read(REF/'bgm_1.wav');reveal=sets['aug_gold'][1]
flows=[]
for k in range(3):
 n=SR*20;mixout=np.zeros(n,dtype=np.float32);combatloop=np.tile(combat,2)[:n]
 selection=np.tile(sets['aug_bgm'][k],3)[:n]
 env=np.zeros(n);i=4*SR;cross=round(.6*SR);end=16*SR
 env[i:i+cross]=np.linspace(0,1,cross);env[i+cross:end]=1;env[end:end+cross]=np.linspace(1,0,cross)
 mixout=combatloop*(1-env)+selection*env
 for t in [4.65,5.0,5.35]:
  start=round(t*SR);mixout[start:start+len(reveal)]+=reveal*.8
 mixout=fade(mixout,.02,.15);mixout*=min(1,.8/truepeak(mixout));p=OUT/f'augmentation_flow_{k+1}.wav';sf.write(p,mixout,SR,subtype='PCM_16');flows.append(str(p.relative_to(HERE)).replace('\\','/'))
catalog={'title':'코어 가디언 · 피드백 반영 2차 청취실','version':'B-feedback-r2-20261019','direction':'B · 단단하고 빠른 전술','groups':groups,'retained':retained,'multi':{'approved_policy':'실제 발사 수에 맞춰 1·2·3발 구분','clips':[f'audio/multi_{i}_targets.wav' for i in [1,2,3]]},'flows':flows}
assert len(groups)==13 and sum(len(g['clips']) for g in groups)==39
for p,h in {**protected,**sources}.items():assert sha(ROOT/p)==h
(HERE/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n');(HERE/'catalog.js').write_text('window.SOUND_CATALOG='+json.dumps(catalog,ensure_ascii=False)+';\n',encoding='utf-8',newline='\n')
(HERE/'production.json').write_text(json.dumps({'metrics':metrics,'source_hashes':sources,'protected_hashes':protected,'note':'TFT-specific audio was not directly auditioned. User card-reveal analogy drove new Foley; no Riot samples used.'},indent=2),encoding='utf-8',newline='\n')
print(json.dumps({'revised_groups':13,'candidates':39,'retained_preferences':12,'multi_counts':[1,2,3],'transition_examples':3,'protected':len(protected)},ensure_ascii=False))
