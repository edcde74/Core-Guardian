"""Offline concept audition only. Never writes Assets, originals or preferences."""
import sys, json, hashlib, base64, math
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / '.local'))
import soundfile as sf
ROOT = HERE.parents[1]
SR = 44100
OUT = HERE / 'audio'
OUT.mkdir(exist_ok=True)
UI = ROOT / 'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio'
RUN = ROOT / 'Assets/Resources/CoreGuardian/Audio'
sources = {}

def load(path):
    sources[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    x, rate = sf.read(path, always_2d=True)
    x = x.mean(axis=1)
    if rate != SR:
        x = np.interp(np.arange(round(len(x)*SR/rate))*rate/SR, np.arange(len(x)), x)
    # Remove long leading silence, preserve 2ms of pre-roll.
    audible = np.flatnonzero(np.abs(x) > max(.001, np.max(np.abs(x))*.012))
    if len(audible): x = x[max(0, audible[0]-88):]
    return x / max(.01, np.max(np.abs(x)))

def edit(x, speed=1., duration=None, cutoff=6500):
    x = np.interp(np.arange(0, len(x)-1, speed), np.arange(len(x)), x)
    if duration: x = x[:round(duration*SR)]
    # Smooth low pass in frequency domain; no clipping, bitcrush or laser oscillator.
    n = len(x)
    f = np.fft.rfftfreq(n, 1/SR)
    x = np.fft.irfft(np.fft.rfft(x) / np.sqrt(1+(f/cutoff)**6), n=n)
    a, z = min(88,n), min(round(.035*SR),n//2)
    x[:a] *= np.linspace(0,1,a)
    x[-z:] *= np.linspace(1,0,z)
    return x

def mix(events, length=None):
    n = round(length*SR) if length else max(round(t*SR)+len(x) for t,x,g in events)
    y = np.zeros(n)
    for t,x,g in events:
        k = round(t*SR); count = min(len(x), n-k)
        if count>0: y[k:k+count] += x[:count]*g
    return y

def level(x, rms=.16, peak=.76):
    active = x[np.abs(x) > .003]
    gain = rms/max(.001, np.sqrt(np.mean(active*active))) if len(active) else 1
    return x*min(gain, peak/max(.001,np.max(np.abs(x))))

raw = {n:load(UI/(n+'.ogg')) for n in ['pluck_001','pluck_002','bong_001','glass_001','click_001','click_003','switch_004','drop_002']}
magic = load(RUN/'TowerShot/03_Aura.wav')
player = load(RUN/'PlayerShot/03_Glyph.wav')
hit = load(RUN/'MonsterHit/03_Hit.wav')
labels = {'click':'UI 선택','install':'포탑 설치','player':'플레이어 공격','single':'단일 발사','multi':'멀티 세 포신','hit':'적 타격','upgrade':'강화 성공','start':'전투 시작','complete':'라운드 완료','victory':'최종 승리','defeat':'패배'}
sets = {}
for style in ['A','B']:
    a = style=='A'
    body = edit(raw['pluck_001'] if a else raw['bong_001'], 1.18 if a else .88, .17 if a else .22, 4300 if a else 3200)
    tick = edit(raw['click_001'] if a else raw['switch_004'], 1.1 if a else 1., .055 if a else .075, 4800)
    ring = edit(raw['pluck_002'] if a else raw['glass_001'], 1.16 if a else .76, .24 if a else .3, 5200 if a else 3600)
    def note(semitones, length=.24):
        return edit(ring, 2**(semitones/12), length, 5100 if a else 3500)
    def phrase(notes, spacing=.105, gain=.48):
        return mix([(i*spacing,note(p,.32),gain) for i,p in enumerate(notes)])
    # One material family and recurring three-note motif across UI, growth and results.
    s={}
    s['click']=mix([(0,tick,.5),(0,body,.18)])
    s['install']=mix([(0,body,.65),(.045,tick,.3),(.075,note(0),.28)])
    s['single']=mix([(0,body,.7),(0,edit(magic,1.4 if a else 1.05,.19 if a else .28,4600 if a else 3000),.27 if a else .4),(0,tick,.22)])
    s['player']=mix([(0,edit(body,1.4,.12),.55),(0,edit(player,1.45 if a else 1.15,.14 if a else .2,5000 if a else 3400),.32)])
    s['multi']=mix([(t,edit(s['single'],speed,.28),g) for t,speed,g in [(0,.82,.65),(.085,.93,.6),(.17,1.04,.55)]])
    s['hit']=mix([(0,edit(hit,1.3 if a else 1.05,.085 if a else .115,4400 if a else 3100),.5),(0,edit(body,1.65 if a else 1.2,.08),.25)])
    s['upgrade']=mix([(0,phrase([0,4,7]),1),(0,tick,.23)])
    s['start']=phrase([0,7],.14)
    s['complete']=phrase([0,4,7],.13)
    s['victory']=phrase([0,4,7,4,7,12],.16,.5)
    s['defeat']=phrase([7,3,0,-5],.19,.43)
    s={k:level(v,.14 if k=='click' else .17) for k,v in s.items()}
    sets[style]=s
    for key,x in s.items(): sf.write(OUT/f'{style}_{key}.wav',x,SR,subtype='PCM_16')

# Identical 24-second event map for both. This is an authored sound scene, not gameplay.
timeline=[(0,'click',.7),(.45,'install',.9),(1.25,'start',.9),
 (2.2,'player',.85),(2.38,'hit',.6),(2.85,'single',.8),(3.04,'hit',.6),
 (3.55,'single',.8),(3.74,'hit',.6),(4.25,'multi',.8),(4.64,'hit',.65),
 (5.35,'upgrade',1),
 (6.5,'single',.8),(6.67,'hit',.55),(6.95,'player',.85),(7.13,'hit',.55),
 (7.4,'multi',.75),(7.75,'hit',.55),(8.1,'single',.8),(8.27,'hit',.55),
 (8.6,'player',.85),(8.78,'hit',.55),(9.05,'multi',.75),(9.4,'hit',.55),
 (9.7,'single',.8),(9.87,'hit',.55),(10.25,'player',.85),(10.43,'hit',.55),
 (10.8,'multi',.75),(11.15,'hit',.55),(12.2,'complete',1),
 (14.3,'install',.9),(15.1,'upgrade',1),(16.5,'single',.8),(16.68,'hit',.55),
 (17.2,'multi',.8),(17.55,'hit',.55),(18.5,'victory',1),(22,'defeat',.85)]
scenes={k:mix([(t,s[key],g) for t,key,g in timeline],24) for k,s in sets.items()}
# Match full-scene RMS, then apply a common peak safety gain to avoid loudness bias.
target=min(np.sqrt(np.mean(x*x)) for x in scenes.values())
for k,x in scenes.items(): scenes[k]=x*target/np.sqrt(np.mean(x*x))
gain=min(1,.88/max(np.max(np.abs(x)) for x in scenes.values()))
stats={}
for k,x in scenes.items():
    x=x*gain; sf.write(OUT/f'{k}_scene.wav',x,SR,subtype='PCM_16')
    stats[k]={'seconds':24,'peak':float(np.max(np.abs(x))),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(x*x))))}
manifest={'purpose':'청취용 컨셉 스케치 · 게임 녹음/적용 아님','rate':SR,'sources':sources,'scene_stats':stats,'timeline':[{'seconds':t,'event':key,'gain':g} for t,key,g in timeline]}
(HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')

def audio(path):
    return '<audio controls preload="none" src="data:audio/wav;base64,'+base64.b64encode(path.read_bytes()).decode()+'"></audio>'
rows=''.join('<tr><th>'+name+'</th><td>'+audio(OUT/f'A_{key}.wav')+'</td><td>'+audio(OUT/f'B_{key}.wav')+'</td></tr>' for key,name in labels.items())
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>코어 가디언 · 두 가지 사운드 방향</title><style>
body{background:#111b22;color:#e9e5d9;font:16px/1.7 system-ui;margin:0;padding:32px;max-width:1100px;margin:auto}h1{font-size:34px;margin-bottom:8px}p{color:#b7c7cd}small{color:#aac0c7}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}article{padding:22px;background:#1d2b35;border:1px solid #53616a;border-radius:14px}h2{margin:0}article:first-child{border-top:4px solid #e7bc71}article:last-child{border-top:4px solid #74bdb3}audio{width:100%;max-width:360px}table{width:100%;border-collapse:collapse;margin-top:20px}th,td{text-align:left;padding:12px 8px;border-bottom:1px solid #354852}button{background:#c6a66c;color:#111b22;padding:9px 20px;border:0;border-radius:5px;cursor:pointer}a{color:#91d8cf}@media(max-width:650px){body{padding:15px}.pair{grid-template-columns:1fr}th,td{padding:7px 2px}audio{min-width:100px}}
</style><main><small>CORE GUARDIAN / SOUND DIRECTION STUDIES / 2026.10.19</small><h1>같은 전투, 두 가지 소리</h1><p>각 24초 · 같은 사건과 타이밍 · 전체 평균 음량 맞춤 · BGM 없음<br>청취용으로 편집한 장면입니다. 실제 게임 녹음이나 최종 음원이 아닙니다.</p><button onclick="document.querySelectorAll('audio').forEach(a=>a.pause())">모두 정지</button><div class="pair" style="margin-top:20px"><article><h2>A · 통통 튀는 아케이드</h2><p>짧은 플럭과 둥근 타격. 가벼운 반응과 밝은 보상을 중심으로.</p>'''+audio(OUT/'A_scene.wav')+'''</article><article><h2>B · 단단하고 빠른 전술</h2><p>장치 체결과 낮은 몸통 울림. 절도 있는 타격과 응축된 보상을 중심으로.</p>'''+audio(OUT/'B_scene.wav')+'''</article></div><p>0–2초 선택·설치·시작 → 2–5초 개별 공격 → 5초 강화 → 6–12초 전투 중첩 → 12초 라운드 완료 → 14–18초 재설치·성장 → 18초 최종 승리 / 22초 별도 패배 예시</p><p>전체 인상을 먼저 듣고 아래에서 마음에 드는 부분을 비교해 주세요. A의 전투+B의 보상처럼 섞는 의견도 가능합니다.</p><table><thead><tr><th>같은 사건</th><th>A</th><th>B</th></tr></thead><tbody>'''+rows+'''</tbody></table><p>기존 확정14개·지원 전투 무음·멀티 세 포신 리듬·게임 저장 음량 보존. 이번 시안은 게임에 연결하지 않았습니다. 보스·코어 위험·증강·BGM은 이 첫 방향 비교에서 제작하지 않았습니다.</p><small>재료: Kenney Interface Sounds (CC0, 상업 사용 가능) + 기존 취득 Magic Aura/RPG 원본의 로컬 편집. 원본 보존·단독 재배포 금지.<br><a href="https://kenney.nl/assets/interface-sounds">Kenney 공식 출처</a> · <a href="https://assetstore.unity.com/browse/eula-faq">Unity 라이선스</a></small></main><script>document.querySelectorAll('audio').forEach(a=>{a.volume=.35;a.addEventListener('play',()=>document.querySelectorAll('audio').forEach(b=>{if(a!==b)b.pause()}))});</script></html>'''
(HERE/'index.html').write_text(html,encoding='utf-8',newline='\n')
for path in OUT.glob('*.wav'):
    x,r=sf.read(path); assert r==SR and np.isfinite(x).all() and np.max(np.abs(x))<.99, path
for path,digest in sources.items(): assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest
print(json.dumps({'files':len(list(OUT.glob('*.wav'))),'scenes':stats,'original_hashes_unchanged':len(sources)},ensure_ascii=False))
