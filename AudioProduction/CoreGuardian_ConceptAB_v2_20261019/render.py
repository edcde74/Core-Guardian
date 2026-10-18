"""Local audition production only. Does not write Unity, original sources or v1."""
from pathlib import Path
import sys,json,hashlib,math,base64,subprocess
from fractions import Fraction
HERE=Path(__file__).resolve().parent; ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np
import soundfile as sf
import pyloudnorm as pyln
import mido,imageio_ffmpeg
from scipy.signal import resample_poly
from pedalboard import Pedalboard,HighpassFilter,LowpassFilter,PeakFilter,HighShelfFilter,Compressor,Reverb
SR=44100; DURATION=32; N=SR*DURATION
OUT=HERE/'audio';OUT.mkdir(exist_ok=True)
UI=ROOT/'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio'
JINGLES=ROOT/'AudioProduction/CoreGuardian_Victory_20261019/Kenney_MusicJingles/Originals/Audio'
RUN=ROOT/'Assets/Resources/CoreGuardian/Audio'
sources={}; measurements={}; ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
# Record prior outputs so this render proves it did not overwrite v1 or old BGM sketches.
protected={str(p.relative_to(ROOT)):sha(p) for directory in ['CoreGuardian_ConceptAB_20261019/audio','CoreGuardian_BGM_20261013'] for p in (ROOT/'AudioProduction'/directory).glob('*.wav')}

def read(p):
 sources[str(p.relative_to(ROOT))]=sha(p)
 x,r=sf.read(p,always_2d=True);x=x.mean(axis=1)
 if r!=SR:x=resample_poly(x,SR//math.gcd(SR,r),r//math.gcd(SR,r))
 idx=np.flatnonzero(abs(x)>max(.001,float(np.max(abs(x)))*.012))
 if len(idx):x=x[max(0,idx[0]-88):]
 return x/max(.01,float(np.max(abs(x))))

def fade(x,attack=.002,release=.035):
 x=np.array(x,dtype=np.float32,copy=True);a=min(len(x)//2,round(SR*attack));r=min(len(x)//2,round(SR*release))
 if a:x[:a]*=np.linspace(0,1,a)
 if r:x[-r:]*=np.linspace(1,0,r)
 return x

def pitch(x,semitones):
 ratio=Fraction(2**(-semitones/12)).limit_denominator(512)
 return resample_poly(x,ratio.numerator,ratio.denominator).astype(np.float32)

def cut(x,seconds,decay=None):
 x=x[:round(seconds*SR)].copy()
 if decay:x*=np.exp(-np.arange(len(x))/SR/decay)
 return fade(x)

def blend(events,length=None):
 n=round(length*SR) if length else max(round(t*SR)+len(x) for t,x,g in events)
 y=np.zeros(n,dtype=np.float32)
 for t,x,g in events:
  i=round(t*SR);ncopy=min(len(x),n-i)
  if ncopy>0:y[i:i+ncopy]+=x[:ncopy]*g
 return y

def balance(x,target=.12,maxpeak=.6):
 active=x[abs(x)>.005]
 rms=float(np.sqrt(np.mean(active**2))) if len(active) else .1
 return x*min(target/max(rms,.001),maxpeak/max(float(np.max(abs(x))),.001))

def process(x,category,style):
 a=style=='A'
 filters=[HighpassFilter(cutoff_frequency_hz=100 if category=='combat' else 90),
  LowpassFilter(cutoff_frequency_hz=6400 if a else 5200),
  PeakFilter(cutoff_frequency_hz=300,gain_db=-2 if a else 1.5,q=.7),
  HighShelfFilter(cutoff_frequency_hz=3100,gain_db=-2.5 if category=='music' else -1),
  Compressor(threshold_db=-19,ratio=1.6,attack_ms=5,release_ms=65)]
 # Minimal shared short room: dry enough for repeated attacks, a little more for motifs.
 if category!='combat':filters.append(Reverb(room_size=.12 if a else .2,damping=.7,wet_level=.045 if category=='ui' else .09,dry_level=1,width=.6))
 padding=np.pad(x,(0,round(SR*.12)))
 return fade(Pedalboard(filters)(padding,SR),.001,.03)

raw={n:read(UI/(n+'.ogg')) for n in ['pluck_001','pluck_002','bong_001','click_001','switch_004','drop_002','glass_001']}
magic=read(RUN/'TowerShot/03_Aura.wav');glyph=read(RUN/'PlayerShot/03_Glyph.wav');hit=read(RUN/'MonsterHit/03_Hit.wav')
pluck=read(JINGLES/'Pizzicato jingles/jingles_PIZZI00.ogg')
steel=read(JINGLES/'Steel jingles/jingles_STEEL00.ogg')
# Extract the first isolated attack, then tune its measured dominant partial to concert notes.
# These are sample instruments, not a new generative model or the original jingle melody.
instruments={'A':(cut(pluck,.125),464.3),'B':(cut(steel,.14),472.4)}
labels={'click':'UI 선택','install':'포탑 설치','player':'플레이어 공격','single':'단일 발사','multi':'멀티 세 포신','hit':'적 타격','upgrade':'강화 성공','start':'전투 시작','complete':'라운드 완료','victory':'최종 승리','defeat':'패배'}
sets={};variant_sets={};music={}
for style in ['A','B']:
 a=style=='A';sample,sourcehz=instruments[style]
 cache={}
 def note(midi,decay=.18):
  key=(midi,decay)
  if key not in cache:
   freq=440*2**((midi-69)/12)
   z=pitch(sample,12*math.log2(freq/sourcehz));z=fade(z,.001,.02)
   z*=np.exp(-np.arange(len(z))/SR/decay)
   cache[key]=balance(z,.16,.65)
  return cache[key]
 def motif(notes,times,gain=.6):return blend([(t,note(n,.25),gain) for t,n in zip(times,notes)])
 body=cut(pitch(raw['pluck_001'] if a else raw['bong_001'],1.5 if a else -3),.13 if a else .18,.09 if a else .12)
 latch=cut(raw['click_001'] if a else raw['switch_004'],.04 if a else .065,.025)
 s={}
 s['click']=blend([(0,latch,.35),(0,note(76 if a else 64),.14)])
 s['install']=blend([(0,body,.55),(.035,latch,.35),(.065,note(64),.26),(.11,note(71),.24)])
 s['single']=blend([(0,body,.7),(.008,cut(pitch(magic,2 if a else -.6),.17 if a else .22,.065 if a else .10),.25 if a else .38),(0,latch,.25)])
 s['player']=blend([(0,cut(pitch(body,5),.09),.55),(0,cut(pitch(glyph,3 if a else 1),.12 if a else .16,.06),.34),(.006,latch,.13)])
 s['multi']=blend([(t,cut(pitch(s['single'],p),.23),g) for t,p,g in [(0,-3,.72),(.085,-1,.67),(.17,1,.61)]])
 s['hit']=blend([(0,cut(hit,.075 if a else .095,.035),.30),(0,cut(pitch(body,7 if a else 3),.06),.33)])
 # Shared E-G-B identity across growth, results and the authored music.
 s['upgrade']=blend([(0,latch,.2),(0,motif([64,67,71],[0,.095,.19]),1)])
 s['start']=motif([59,64,71],[0,.11,.25],.5)
 s['complete']=motif([64,67,71,76],[0,.105,.23,.38],.52)
 s['victory']=blend([(0,motif([64,67,71,74,71,76],[0,.12,.25,.44,.59,.81],.62),1),(.81,note(52,.32),.30)])
 s['defeat']=motif([71,67,63,52],[0,.15,.33,.58],.46)
 done={};variants={}
 for key,x in s.items():
  category='combat' if key in ['single','multi','player','hit'] else 'ui'
  target=.055 if key=='hit' else .065 if key=='click' else .105 if category=='combat' else .13
  done[key]=balance(process(x,category,style),target,.67)
  # Authored subtle variations only in audition, no game pitch or event changes.
  variants[key]=[done[key]]
  if category=='combat':
   for step,g in [(.22,.97),(-.18,1.02)]:
    # Preserve all three exact barrel onsets by varying the complete multi only in gain.
    variants[key].append(done[key]*g if key=='multi' else fade(pitch(done[key],step))*g)
  sf.write(OUT/f'{style}_{key}.wav',done[key],SR,subtype='PCM_16')
 sets[style]=done;variant_sets[style]=variants
 # Eight bars at 120 BPM; same written notes, tempo and arrangement for fair material comparison.
 # Bass, answering motif and light pulse share the sampled instrument family.
 beat=.5;events=[];mid=mido.MidiFile();track=mido.MidiTrack();mid.tracks.append(track)
 track.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(120)))
 chords=[[52,55,59],[48,52,55],[55,59,62],[50,54,57]]*2
 tune=[[76,74,71,67],[71,72,76,74],[74,71,67,71],[69,71,74,71],
       [76,79,78,74],[76,74,72,71],[74,71,67,69],[71,74,71,64]]
 midi_events=[]
 for bar,chord in enumerate(chords):
  t=bar*2
  for k in [0,2]:
   events.append((t+k*beat,note(chord[0]-12,.25),.42))
   midi_events.extend([(round((t+k*beat)*960),True,chord[0]-12),(round((t+k*beat+.2)*960),False,chord[0]-12)])
  for k,p in enumerate(tune[bar]):
   at=t+k*beat
   events.append((at,note(p,.20),.36 if bar<4 else .43))
   midi_events.extend([(round(at*960),True,p),(round((at+.17)*960),False,p)])
  for k in [1,3,5,7]:events.append((t+k*.25,note(chord[(k//2)%3]+12,.12),.12))
  for k in range(4):events.append((t+k*.5,cut(body,.055,.018),.08 if k%2==0 else .045))
 last=0
 for tick,on,p in sorted(midi_events):
  track.append(mido.Message('note_on' if on else 'note_off',note=p,velocity=65 if on else 0,time=tick-last));last=tick
 mid.save(OUT/f'{style}_music_sketch.mid')
 # Fold room tail into the loop beginning to preserve a continuous sixteen-second bed.
 bed=process(blend(events,16.4),'music',style);loop=bed[:SR*16].copy()
 tail=bed[SR*16:];loop[:len(tail)]+=tail
 music[style]=loop

# Shared, intentionally authored montage. No performance/gameplay simulation claims.
timeline=[(4,'click',.8),(4.4,'install',1),(5.2,'start',1),(6.1,'player',1),(6.27,'hit',.8),
 (6.85,'single',.95),(7.02,'hit',.8),(7.6,'multi',.85),(7.98,'hit',.8),(8.8,'upgrade',1)]
for t in np.arange(10,21,.6):
 key=['single','player','multi'][int(round((t-10)/.6))%3]
 timeline.extend([(float(t),key,.92),(float(t+.19 if key!='multi' else t+.36),'hit',.7)])
timeline.extend([(22.2,'complete',1),(24,'install',1),(24.8,'upgrade',1),(26.4,'victory',1),(30,'defeat',.85)])
timeline.sort()
renders={};counters={}
for style in ['A','B']:
 counters[style]={};fx=np.zeros(N,dtype=np.float32)
 for t,key,g in timeline:
  count=counters[style].get(key,0);counters[style][key]=count+1
  variants=variant_sets[style][key];x=variants[count%len(variants)];i=round(t*SR);n=min(len(x),N-i)
  fx[i:i+n]+=x[:n]*g
 # Music stays below effects and ducks only for important alerts, not each ordinary hit.
 bed=np.tile(music[style],2)[:N];bed=balance(bed,.035,.22)
 duck=np.ones(N,dtype=np.float32)
 for t,key,g in timeline:
  if key not in ['upgrade','start','complete','victory','defeat']:continue
  i=max(0,round((t-.015)*SR));hold=round((.8 if key=='victory' else .45)*SR);rel=round(.28*SR)
  envelope=np.r_[np.linspace(1,.48,round(.015*SR)),np.full(hold,.48),np.linspace(.48,1,rel)]
  n=min(len(envelope),N-i);duck[i:i+n]=np.minimum(duck[i:i+n],envelope[:n])
 bed[:round(.08*SR)]*=np.linspace(0,1,round(.08*SR))
 bed[round(28.5*SR):]*=np.linspace(1,0,N-round(28.5*SR))
 renders[(style,'scene')]=fade(fx+bed*duck,.003,.06)
 renders[(style,'fx_scene')]=fade(fx,.003,.06)
 renders[(style,'music_loop')]=music[style]

meter=pyln.Meter(SR)
def truepeak(x):return float(np.max(abs(resample_poly(x,4,1))))
for kind in ['scene','fx_scene','music_loop']:
 # Same integrated LUFS for A/B; lower BOTH if either needs extra true-peak headroom.
 target=-21 if kind!='music_loop' else -23
 norm={style:renders[(style,kind)]*10**((target-meter.integrated_loudness(renders[(style,kind)]))/20) for style in ['A','B']}
 common=min(1,10**(-1.7/20)/max(truepeak(x) for x in norm.values()))
 for style,x in norm.items():
  x=x*common;p=OUT/f'{style}_{kind}.wav';sf.write(p,x,SR,subtype='PCM_16')
  z,r=sf.read(p);tp=truepeak(z)
  measurements[f'{style}_{kind}']={'duration':len(z)/r,'lufs':float(meter.integrated_loudness(z)),'sample_peak_dbfs':float(20*np.log10(max(abs(z)))),'true_peak_dbtp':float(20*np.log10(tp)),'sha256':sha(p)}
  assert tp<10**(-1.5/20) and np.isfinite(z).all()
  subprocess.run([ffmpeg,'-nostdin','-hide_banner','-loglevel','error','-y','-i',str(p),'-c:a','libvorbis','-q:a','6',str(p.with_suffix('.ogg'))],check=True,capture_output=True)
for kind in ['scene','fx_scene','music_loop']:assert abs(measurements['A_'+kind]['lufs']-measurements['B_'+kind]['lufs'])<.05
for p in OUT.glob('*.wav'):
 x,r=sf.read(p);assert r==SR and np.isfinite(x).all() and max(abs(x))<.99
for path,digest in {**sources,**protected}.items():assert sha(ROOT/path)==digest,path
manifest={'purpose':'v2 local concept audition; not gameplay recording or approved game audio','sources':sources,'protected_originals_and_v1':protected,'measurements':measurements,'timeline':[{'t':float(t),'event':key,'gain':g} for t,key,g in timeline],'music':{'bpm':120,'bars':8,'seconds':16,'instrument':'first-attack sample from CC0 jingle; new written notes; not AI generation'},'tools':['audio-design skill','Pedalboard EQ/compression/reverb','SciPy polyphase resampling/4x true-peak estimate','pyloudnorm integrated LUFS','Mido MIDI score','FFmpeg Vorbis preview encoding'],'no_api_calls':True}
(HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')

def player(path,title):
 mime='audio/ogg' if path.suffix=='.ogg' else 'audio/wav'
 return '<audio aria-label="'+title+'" controls preload="none" src="data:'+mime+';base64,'+base64.b64encode(path.read_bytes()).decode()+'"></audio>'
cards=[]
for style,title,desc in [('A','통통 튀는 아케이드','튕기는 현악·목재 타격 / 밝고 민첩한 반응'),('B','단단하고 빠른 전술','금속 말렛·장치 체결 / 낮고 응축된 반응')]:
 parts=['<article><span class="tag">'+style+' / VERSION 02</span><h2>'+title+'</h2><p>'+desc+'</p>']
 for kind,label in [('scene','BGM 포함 · 32초'),('fx_scene','효과음만 · 32초'),('music_loop','BGM 스케치만 · 16초')]:
  parts+=['<h3>'+label+'</h3>',player(OUT/f'{style}_{kind}.ogg',style+' '+label)]
 parts+=['<small>전체 '+str(round(measurements[style+'_scene']['lufs'],1))+' LUFS / 추정 피크 '+str(round(measurements[style+'_scene']['true_peak_dbtp'],1))+' dBTP</small></article>']
 cards.append(''.join(parts))
rows=''.join('<tr><th>'+label+'</th><td>'+player(OUT/f'A_{key}.wav','A '+label)+'</td><td>'+player(OUT/f'B_{key}.wav','B '+label)+'</td></tr>' for key,label in labels.items())
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>코어 가디언 · 사운드 컨셉 v2</title><style>
body{background:#111a20;color:#eee9dc;font:16px/1.65 system-ui;max-width:1120px;margin:auto;padding:30px}h1{font-size:36px;margin-bottom:8px}p,small{color:#b5c5cd}.pair{display:grid;grid-template-columns:1fr 1fr;gap:22px;margin:22px 0}article{background:#1b2a33;border:1px solid #45606a;border-radius:14px;padding:24px}article:first-child{border-top:4px solid #e4bd76}article:last-child{border-top:4px solid #71c6b8}.tag{font-size:13px;letter-spacing:2px;color:#d5bd8d}h2{font-size:25px}h3{font-size:15px;margin:18px 0 7px}audio{width:100%;max-width:380px}table{width:100%;border-collapse:collapse}th,td{border-bottom:1px solid #324751;text-align:left;padding:12px 6px}button{background:#e4bd76;color:#111a20;padding:10px 22px;border:0;border-radius:5px;cursor:pointer}a{color:#87cec0}.timeline{padding:16px;background:#17252d;border-left:3px solid #7abdb5}@media(max-width:680px){body{padding:15px}.pair{grid-template-columns:1fr}audio{min-width:90px}th,td{padding:8px 2px}}
</style><main><small>CORE GUARDIAN / SOUND DIRECTION / VERSION 02</small><h1>한 게임의 소리로 묶기</h1><p>A/B의 사건·템포·음표는 같고, 재료와 타격 방식은 다릅니다.<br>전체·효과음만·음악만을 나눠 들어보세요. 각 비교 쌍은 통합 LUFS를 맞췄습니다.</p><button id="stop">모두 정지</button><div class="pair">'''+''.join(cards)+'''</div><div class="timeline">0–4초 음악 인상 → 4–9초 선택·설치·공격·강화 → 10–21초 반복 전투 → 22초 라운드 완료 → 24초 재설치·강화 → 26초 최종 승리 → 30초 별도 패배 예시</div><p>실제 게임 녹음이 아닌 청취용 편집입니다. 음악은 CC0 징글의 첫 타격을 악기 재료로 편곡한 스케치이며, 기존 BGM을 교체하거나 ElevenLabs API를 사용하지 않았습니다.</p><h2>같은 사건, 다른 재질</h2><table><thead><tr><th>사건</th><th>A · 아케이드</th><th>B · 전술</th></tr></thead><tbody>'''+rows+'''</tbody></table><p>기존 확정음·게임음량·지원전투무음은 유지합니다. 멀티는 0/85/170ms 세 타격을 유지합니다. 보스·코어위험·증강은 이번 시안 범위에 포함하지 않았습니다. 방향 선택과 게임 적용은 별도입니다.</p><small>재료: Kenney Interface/Jingles(CC0)와 기존 취득 마력·피격 음원의 로컬 파생. 소유자 PC 전용.<br><a href="https://kenney.nl/assets/music-jingles">Kenney Jingles</a> · <a href="https://kenney.nl/assets/interface-sounds">Interface</a> · <a href="https://assetstore.unity.com/browse/eula-faq">Unity 이용조건</a></small></main><script>document.querySelectorAll('audio').forEach(a=>{a.volume=.35;a.addEventListener('play',()=>document.querySelectorAll('audio').forEach(b=>{if(a!==b)b.pause()}))});document.getElementById('stop').addEventListener('click',()=>document.querySelectorAll('audio').forEach(a=>{a.pause();a.currentTime=0}));</script></html>'''
(HERE/'index.html').write_text(html,encoding='utf-8',newline='\n')
print(json.dumps({'wav_count':len(list(OUT.glob('*.wav'))),'measurements':measurements,'protected':len(protected),'source_count':len(sources)},ensure_ascii=False,indent=2))
