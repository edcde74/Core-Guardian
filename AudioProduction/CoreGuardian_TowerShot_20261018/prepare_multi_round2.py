"""New multi-tower auditions from three different acquired originals."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'AudioProduction/CoreGuardian_TowerShot_20261018/multi_round2'
OUT.mkdir(parents=True,exist_ok=True)
SOURCES=[
    ('01_Ice','Assets/Leohpaz/RPG_Essentials_Free/8_Atk_Magic_SFX/13_Ice_explosion_01.wav',.55),
    ('02_Poison','Assets/Leohpaz/RPG_Essentials_Free/8_Atk_Magic_SFX/46_Poison_01.wav',.50),
    ('03_Glyph2','Assets/AS_Magic_Aura_Construction_Kit_FREE/Sound Kit/Aura_Glyph_2.wav',.55),
]
def save(path,y,rate):
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes(np.round(y*32767).astype('<i2').tobytes())
report=[];sections=[]
for name,source,duration in SOURCES:
    path=ROOT/source
    with wave.open(str(path),'rb') as w:
        rate,channels,width=w.getframerate(),w.getnchannels(),w.getsampwidth();raw=w.readframes(w.getnframes())
    assert channels==2 and rate==44100
    if width==3:
        b=np.frombuffer(raw,np.uint8).reshape(-1,3).astype(np.int32)
        z=b[:,0]|b[:,1]<<8|b[:,2]<<16;x=((z^0x800000)-0x800000)/8388608.
    else:
        assert width==2;x=np.frombuffer(raw,'<i2').astype(np.float64)/32768.
    x=x.reshape(-1,2);env=np.max(np.abs(x),axis=1)
    onset=max(0,int(np.flatnonzero(env>env.max()*.04)[0])-round(.004*rate))
    y=x[onset:onset+round(duration*rate)].copy();y-=y.mean(axis=0)
    attack=round(.002*rate);release=round(.10*rate)
    y[:attack]*=np.linspace(0,1,attack)[:,None];y[-release:]*=np.linspace(1,0,release)[:,None]**1.5
    y*=.7/np.max(np.abs(y));save(OUT/(name+'.wav'),y,rate)
    sections.extend([y,np.zeros((round(.35*rate),2)),y,np.zeros((round(1.1*rate),2))])
    report.append(dict(name=name,source=source,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),onset=onset/rate,duration=len(y)/rate,peak=float(np.abs(y).max()),clipped=int(np.count_nonzero(np.abs(y)>=1))))
save(OUT/'MultiNew_123.wav',np.concatenate(sections),rate)
(ROOT/'docs/development/combat_audio_followup_20261018/processing.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print(json.dumps(report))
