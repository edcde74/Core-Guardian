"""Produce three B-direction candidates per sound event, outside Unity."""
from pathlib import Path
from fractions import Fraction
import sys,json,math,hashlib,base64,subprocess
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln,mido,imageio_ffmpeg
from scipy.signal import resample_poly
from pedalboard import Pedalboard,HighpassFilter,LowpassFilter,PeakFilter,Compressor,Reverb
SR=44100;OUT=HERE/'audio';OUT.mkdir(exist_ok=True)
old=json.loads((ROOT/'AudioProduction/CoreGuardian_AudioReview_20261019/catalog.json').read_text(encoding='utf-8'))
current={g['id']:g['clips'][g['current']]['path'] for g in old['groups'] if g['current'] is not None}
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
protected={str(p.relative_to(ROOT)):digest(p) for folder in ['CoreGuardian_ConceptAB_20261019/audio','CoreGuardian_ConceptAB_v2_20261019/audio','CoreGuardian_BGM_20261013'] for p in (ROOT/'AudioProduction'/folder).glob('*.wav')}
protected.update({p:digest(ROOT/p) for p in current.values()})
sources={}
def read(p):
 sources[str(p.relative_to(ROOT))]=digest(p);x,r=sf.read(p,always_2d=True);x=x.mean(axis=1)
 if r!=SR:x=resample_poly(x,SR//math.gcd(SR,r),r//math.gcd(SR,r))
 ii=np.flatnonzero(abs(x)>max(.001,float(np.max(abs(x)))*.012))
 if len(ii):x=x[max(0,ii[0]-88):]
 return x/max(.001,float(np.max(abs(x))))
def fade(x,a=.002,z=.035):
 x=np.array(x,dtype=np.float32,copy=True);aa=min(len(x)//2,round(a*SR));zz=min(len(x)//2,round(z*SR))
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
def level(x,rms=.12):
 a=x[abs(x)>.004];r=float(np.sqrt(np.mean(a*a))) if len(a) else .1
 return x*min(rms/max(r,.001),.68/max(.001,float(max(abs(x)))))
UI=ROOT/'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio'
JING=ROOT/'AudioProduction/CoreGuardian_Victory_20261019/Kenney_MusicJingles/Originals/Audio'
raw={n:read(UI/(n+'.ogg')) for n in ['bong_001','glass_001','switch_004','click_003','drop_002','close_001','open_002','scratch_001']}
steel=cut(read(JING/'Steel jingles/jingles_STEEL00.ogg'),.14)
magic=read(ROOT/current['single']);glyph=read(ROOT/current['player']);hit=read(ROOT/current['hit']);death=read(ROOT/current['death']);core=read(ROOT/current['core'])
cache={}
def note(m,decay=.23):
 key=(m,decay)
 if key not in cache:
  hz=440*2**((m-69)/12);x=pitch(steel,12*math.log2(hz/472.4));x*=np.exp(-np.arange(len(x))/SR/decay)
  cache[key]=level(fade(x,.001,.02),.14)
 return cache[key]
def phrase(notes,spacing=.12):return mix([(i*spacing,note(n),.6) for i,n in enumerate(notes)])
def dsp(x,k,category):
 wet=[.015,.025,.08][k];room=[.12,.14,.23][k]
 fx=[HighpassFilter(cutoff_frequency_hz=85 if category=='전투' else 110),LowpassFilter(cutoff_frequency_hz=[5600,4800,5800][k]),PeakFilter(cutoff_frequency_hz=[1200,260,1800][k],gain_db=[1,2,-1][k],q=.75),Compressor(threshold_db=-19,ratio=1.6,attack_ms=4,release_ms=60)]
 if category!='전투':fx.append(Reverb(room_size=room,damping=.75,wet_level=wet,dry_level=1,width=.5))
 return fade(Pedalboard(fx)(np.pad(x,(0,round(.1*SR))),SR),.001,.03)

definitions=[
 ('single','단일 포탑 발사','전투','짧게 응축되는 마력포. 실제 발사 1회에 대응합니다.'),
 ('multi','멀티 포탑 발사','전투','세 포신 0/85/170ms 리듬을 모두 유지한 한 파일입니다.'),
 ('player','플레이어 공격','전투','자동 포탑보다 민첩한 직접 공격의 반응입니다.'),
 ('hit','몬스터 피격','전투','살아남는 타격용. 처치음과 구분해 짧게 정리했습니다.'),
 ('death','몬스터 처치','전투','치명 타격의 짧은 마무리. 코어 도착 퇴장음이 아닙니다.'),
 ('core','코어 피해','전투','아군 공격과 구분되는 낮은 불협 공명. 반복 위험 알림용입니다.'),
 ('boss_spawn','보스 등장','전투','일반 등장과 구분되는 묵직한 도입 신호입니다.'),
 ('boss_accel','보스 가속 전환','전투','상승하는 긴장과 빠른 체결 리듬으로 변화를 알립니다.'),
 ('install','포탑 설치 완료','설치·강화','받침 체결과 코어 연결의 짧은 성공 반응입니다.'),
 ('install_fail','설치 실패','설치·강화','설치가 되지 않았다는 짧은 거부 반응. 기존 게임에는 미연결입니다.'),
 ('remove','철거·설치 취소','설치·강화','풀리는 체결과 내려가는 공명. 연결 범위는 선택 후 별도 결정합니다.'),
 ('upgrade_ok','강화 성공','설치·강화','E-G-B 계열이 상승하며 성장이 완성됩니다.'),
 ('upgrade_fail','강화 실패·재화 부족','설치·강화','설치 실패와 구분되는 짧은 이중 거부 신호입니다.'),
 ('round_start','라운드 전투 시작','라운드·결과','준비에서 전투로 넘어가는 짧고 또렷한 신호입니다.'),
 ('round_complete','라운드 완료','라운드·결과','반복해 들어도 부담이 적은 작은 완료 모티프입니다.'),
 ('victory','최종 승리','라운드·결과','마지막 라운드 클리어로 게임 전체가 승리 종료될 때의 멜로디입니다.'),
 ('defeat','패배','라운드·결과','차분하게 내려가는 종료 모티프. 과장된 경고음은 줄였습니다.'),
 ('ui_click','UI 클릭·선택','UI','조작 확인을 위한 작은 체결음. 모든 클릭에 연결할지는 별도 결정합니다.'),
 ('ui_open','패널 열기','UI','닫기와 짝을 이루는 짧은 펼침 반응입니다.'),
 ('ui_close','패널 닫기·접기','UI','열기와 짝을 이루는 짧은 정리 반응입니다.'),
 ('aug_silver','증강 공개 · 실버','증강','실버 등급용 새 시안 3개. 다른 등급과 택일하는 항목이 아닙니다.'),
 ('aug_gold','증강 공개 · 골드','증강','실버보다 한 겹 풍성한 공개 모티프의 시안 3개입니다.'),
 ('aug_platinum','증강 공개 · 플래티넘','증강','가장 풍성한 등급 공개 모티프의 시안 3개입니다.')]
names={
 'single':['정밀한 체결','묵직한 응축','수정 잔향'], 'multi':['선명한 3연격','무게 있는 3연격','공명하는 3연격'],
 'player':['민첩한 타격','단단한 반격','마력의 여운'], 'hit':['짧은 충돌','두꺼운 접촉','잔여 공명'],
 'death':['빠른 파쇄','무게 있는 붕괴','작은 공명 소멸'], 'core':['짧은 균열','깊은 경고','흔들리는 공명'],
 'boss_spawn':['낮은 장치 점화','중량 있는 도착','불길한 공명'], 'boss_accel':['빠른 잠금 해제','강한 재점화','상승하는 공명'],
 'install':['정밀한 결합','견고한 안착','마력 연결'], 'install_fail':['단발 거부','둔탁한 걸림','짧은 역공명'],
 'remove':['짧은 해제','기계적 회수','마력 회수'], 'upgrade_ok':['짧은 완성','단단한 상승','여운 있는 성장'],
 'upgrade_fail':['이중 거부','낮은 막힘','어긋난 공명'], 'round_start':['짧은 출격','무게 있는 개시','공명하는 개시'],
 'round_complete':['간결한 정리','안정된 마무리','작은 성취'], 'victory':['절제된 승리','당당한 승리','여운 있는 승리'],
 'defeat':['짧은 하행','낮은 종료','차분한 소멸'], 'ui_click':['짧은 클릭','작은 잠금','얇은 공명'],
 'ui_open':['경첩 펼침','장치 열림','공명 펼침'], 'ui_close':['경첩 닫힘','장치 정리','공명 접힘'],
 'aug_silver':['짧은 공개','또렷한 공개','은은한 공개'], 'aug_gold':['선명한 상승','풍성한 결합','빛나는 공명'],
 'aug_platinum':['완성된 상승','장엄한 결합','긴 공명']}
candidate_sets=[]
for ident,title,category,description in definitions:
 clips=[]
 for k in range(3):
  body=cut(pitch(raw['bong_001'],[-3,-6,-1][k]),[.16,.23,.2][k],[.1,.14,.12][k])
  latch=cut(raw['switch_004'] if k!=2 else raw['click_003'],[.05,.075,.04][k],.025)
  resonance=note([64,59,71][k]);stride=[.10,.135,.15][k]
  shot=mix([(0,body,[.65,.85,.5][k]),(.008,cut(pitch(magic,[-.5,-2,1][k]),[.19,.26,.23][k],.10),[.34,.4,.3][k]),(0,latch,.25),(.02,resonance,[.06,.03,.18][k])])
  if ident=='single':x=shot
  elif ident=='multi':x=mix([(t,cut(pitch(shot,p),.29),g) for t,p,g in [(0,-3,.72),(.085,-1,.66),(.17,1,.6)]])
  elif ident=='player':x=mix([(0,cut(pitch(body,5),.1),.6),(0,cut(pitch(glyph,[1,-1,3][k]),.15,.065),.36),(.006,latch,.2),(.018,note(76),.04 if k<2 else .16)])
  elif ident=='hit':x=mix([(0,cut(hit,[.07,.11,.095][k],.045),.42),(0,cut(pitch(body,5),.06),.2)])
  elif ident=='death':x=mix([(0,cut(death,[.18,.3,.23][k],.10),.35),(0,pitch(body,-2),.3),(.05,cut(pitch(raw['drop_002'],-3),.14,.06),.18),(.09,note(52),.08 if k<2 else .2)])
  elif ident=='core':x=mix([(0,cut(core,.25,.12),.16),(0,body,.4),(0,note(52),.45),(.10,note(51),.38),(.20,note(59),.25 if k<2 else .42)])
  elif ident=='boss_spawn':x=mix([(0,pitch(body,-9),.6),(.12,phrase([40,47,52],.18 if k<2 else .24),.65),(.1,cut(pitch(magic,-7),.45,.18),.2)])
  elif ident=='boss_accel':x=mix([(0,phrase([[52,59,64,71],[47,52,59,64],[59,64,71,76]][k],.075 if k==0 else .09),.7),(.02,latch,.3),(.17,body,.3)])
  elif ident=='install':x=mix([(0,body,.65),(.035,latch,.35),(.07,note(64),.25),(.13,note(71),.28)])
  elif ident=='install_fail':x=mix([(0,latch,.38),(0,pitch(body,-2),.35),(.06,note(63 if k<2 else 58),.24)])
  elif ident=='remove':x=mix([(0,cut(raw['close_001'],.11),.22),(0,latch,.35),(.05,note(64),.28),(.14,note(52),.25)])
  elif ident=='upgrade_ok':x=mix([(0,latch,.22),(0,phrase([[64,67,71],[59,64,67,71],[64,67,71,76]][k],stride),1)])
  elif ident=='upgrade_fail':x=mix([(0,latch,.3),(.085,latch,.25),(0,phrase([[59,58],[52,51],[64,63,59]][k],.11),.5)])
  elif ident=='round_start':x=mix([(0,body,.25),(0,phrase([[59,64,71],[52,59,64],[64,71,76]][k],.13),.8)])
  elif ident=='round_complete':x=phrase([[64,67,71],[59,64,71],[64,71,76,71]][k],stride)
  elif ident=='victory':x=mix([(0,phrase([[64,67,71,74,71,76],[52,59,64,67,71,76],[64,71,74,76,79,76]][k],.16 if k<2 else .19),1),(.8,note(52),.32)])
  elif ident=='defeat':x=phrase([[71,67,63,52],[64,59,55,40],[76,71,67,63,52]][k],.18 if k<2 else .23)
  elif ident=='ui_click':x=mix([(0,cut(latch,.035),.32),(0,note([71,64,76][k]),[.08,.06,.15][k])])
  elif ident=='ui_open':x=mix([(0,cut(raw['open_002'],.09),.12),(0,latch,.3),(.03,note(64),.18),(.09,note(71),.14 if k<2 else .25)])
  elif ident=='ui_close':x=mix([(0,cut(raw['close_001'],.09),.15),(0,latch,.27),(.02,note(71),.15),(.07,note(64),.18)])
  else:
   tier=['aug_silver','aug_gold','aug_platinum'].index(ident)
   notes=[[64,67,71],[59,64,67,71,76],[52,59,64,67,71,76,83]][tier]
   if k==1:notes=[n-12 if i==0 else n for i,n in enumerate(notes)]
   if k==2:notes=notes+[notes[-1]-12]
   x=mix([(0,phrase(notes,stride),1),(.07,body,.13+tier*.07)])
  clips.append(dsp(level(x),k,category))
 candidate_sets.append((ident,title,category,description,clips))

# New music arrangements share the B sample palette, but differ in tempo, motif and density.
bgms=[];bgm_names=['안뜰의 방어선','고대 장치의 맥박','공명하는 전술']
for k,bpm in enumerate([120,108,128]):
 beat=60/bpm;barlen=4*beat;duration=barlen*8;events=[];midi_events=[]
 chords=([[52,55,59],[48,52,55],[55,59,62],[50,54,57]] if k!=1 else [[52,55,59],[50,54,57],[48,52,55],[47,50,54]])*2
 motifs=[[[76,74,71,67],[71,72,76,74],[74,71,67,71],[69,71,74,71]],[[64,67,71,67],[66,69,74,69],[64,67,72,67],[62,66,71,66]],[[76,71,79,74],[76,72,79,76],[74,71,79,74],[74,69,78,74]]][k]*2
 def add(t,p,g,decay=.23):
  events.append((t,note(p,decay),g));midi_events.extend([(round(t/beat*480),True,p),(round((t/beat+.35)*480),False,p)])
 for bar,chord in enumerate(chords):
  t=bar*barlen
  for b in [0,2]:add(t+b*beat,chord[0]-12,.42 if k!=1 else .52,.32)
  for b,p in enumerate(motifs[bar]):add(t+b*beat,p,.38 if k!=2 else .32)
  for eighth in ([1,3,5,7] if k!=1 else [3,7]):add(t+eighth*beat/2,chord[(eighth//2)%3]+12,.12 if k!=2 else .19,.13)
  for b in range(4):events.append((t+b*beat,cut(raw['bong_001'],.055,.018),.035 if b%2 else .06))
 music=dsp(mix(events,duration+.5),k,'BGM');loop=music[:round(duration*SR)].copy();tail=music[len(loop):];loop[:len(tail)]+=tail
 bgms.append(loop)
 mid=mido.MidiFile();tr=mido.MidiTrack();mid.tracks.append(tr);tr.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(bpm)))
 prev=0
 for tick,on,p in sorted(midi_events):tr.append(mido.Message('note_on' if on else 'note_off',note=p,velocity=65 if on else 0,time=tick-prev));prev=tick
 mid.save(OUT/f'bgm_{k+1}.mid')
candidate_sets.append(('bgm','전투 BGM','BGM','B의 금속 말렛·장치 리듬을 공유하는 새 편곡 3개. 기존 BGM 원본과 별개입니다.',bgms))

meter=pyln.Meter(SR)
def audition_loudness(x):
 if len(x)>SR*3:return float(meter.integrated_loudness(x))
 gap=max(.45,len(x)/SR+.15);return float(meter.integrated_loudness(mix([(i*gap,x,1) for i in range(6)],6*gap)))
def tp(x):return float(max(abs(resample_poly(x,4,1))))
catalog={'title':'코어 가디언 · B 사운드 라이브러리','version':'B72-20261019-v1','direction':'B · 단단하고 빠른 전술','groups':[],'support':'지원 포탑 전투 무음 유지'}
allmetrics={};ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
for ident,title,category,description,clips in candidate_sets:
 target=-23 if category=='BGM' else -27 if category=='UI' else -23
 normalized=[x*10**((target-audition_loudness(x))/20) for x in clips]
 common=min(1,10**(-2.2/20)/max(tp(x) for x in normalized))
 group={'id':ident,'title':title,'category':category,'description':description,'current':current.get(ident),'status':'현재 게임 적용음 있음' if ident in current else '등급별 기존음 있음' if ident.startswith('aug_') else '게임 미연결','clips':[]}
 for k,x in enumerate(normalized):
  x*=common;p=OUT/f'{ident}_{k+1}.wav';sf.write(p,x,SR,subtype='PCM_16');z,r=sf.read(p);peak=tp(z)
  assert np.isfinite(z).all() and peak<.90
  data={'file':p.relative_to(HERE).as_posix(),'sha256':digest(p),'duration':len(z)/r,'audition_lufs':audition_loudness(z),'estimated_dbtp':20*math.log10(peak)}
  if category=='BGM':
   ogg=p.with_suffix('.ogg');subprocess.run([ffmpeg,'-nostdin','-hide_banner','-loglevel','error','-y','-i',str(p),'-c:a','libvorbis','-q:a','6',str(ogg)],check=True,capture_output=True)
   preview=ogg
  else:preview=p
  title_variant=bgm_names[k] if category=='BGM' else names[ident][k]
  subtitles=['짧고 또렷한 체결을 중심으로','낮은 몸통과 무게를 중심으로','수정 공명과 여운을 중심으로'] if category!='BGM' else ['120 BPM · 균형 잡힌 전투 리듬','108 BPM · 낮고 안정적인 박동','128 BPM · 촘촘하고 긴장된 움직임']
  group['clips'].append({'title':title_variant,'description':subtitles[k],'url':preview.relative_to(HERE).as_posix(),'source_wav':p.relative_to(ROOT).as_posix(),'sha256':data['sha256'],'duration':data['duration'],'number':k+1})
  allmetrics[p.name]=data
  if len(z)<SR*3:
   gap=max(.45,len(z)/SR+.15);repeat=OUT/f'{ident}_{k+1}_repeat.wav';sf.write(repeat,mix([(i*gap,z,1) for i in range(5)],5*gap),SR,subtype='PCM_16');group['clips'][-1]['repeat_url']=repeat.relative_to(HERE).as_posix()
 catalog['groups'].append(group)
 vals=[allmetrics[f'{ident}_{k+1}.wav']['audition_lufs'] for k in range(3)];assert max(vals)-min(vals)<.08
for path,h in {**protected,**sources}.items():assert digest(ROOT/path)==h,path
assert len(catalog['groups'])==24 and sum(len(g['clips']) for g in catalog['groups'])==72
(HERE/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')
(HERE/'catalog.js').write_text('window.SOUND_CATALOG='+json.dumps(catalog,ensure_ascii=False)+';\n',encoding='utf-8',newline='\n')
(HERE/'production.json').write_text(json.dumps({'source_hashes':sources,'protected_hashes':protected,'metrics':allmetrics,'notes':'New B-direction audition candidates only. Short SFX LUFS measured in six-repeat trains; music measured directly. No Unity/API changes.'},indent=2),encoding='utf-8',newline='\n')
print(json.dumps({'groups':24,'candidates':72,'sources':len(sources),'protected':len(protected),'categories':{c:sum(g['category']==c for g in catalog['groups']) for c in dict.fromkeys(g['category'] for g in catalog['groups'])}},ensure_ascii=False))
