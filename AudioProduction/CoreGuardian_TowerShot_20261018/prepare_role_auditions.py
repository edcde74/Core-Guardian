"""Short, level-matched auditions from acquired originals; no synthetic layer."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'AudioProduction/CoreGuardian_TowerShot_20261018/archived_role_auditions'
DEMO=ROOT/'AudioProduction/CoreGuardian_TowerShot_20261018/role_auditions'
DEMO.mkdir(parents=True,exist_ok=True)
AURA='Assets/AS_Magic_Aura_Construction_Kit_FREE/Sound Kit/'
RPG='Assets/Leohpaz/RPG_Essentials_Free/'
SOURCES={
    'Multi': [('01_Aura2',AURA+'Aura_Spawn_2.wav',.55),('02_Wind',RPG+'8_Atk_Magic_SFX/25_Wind_01.wav',.50),('03_Water',RPG+'8_Atk_Magic_SFX/22_Water_02.wav',.55)],
    'Player':[('01_Wind',RPG+'8_Atk_Magic_SFX/25_Wind_01.wav',.32),('02_Poison',RPG+'8_Atk_Magic_SFX/46_Poison_01.wav',.35),('03_Glyph',AURA+'Aura_Glyph_1.wav',.40)],
    'Support':[('01_AttackBuff',RPG+'8_Buffs_Heals_SFX/16_Atk_buff_04.wav',.75),('02_DefenceBuff',RPG+'8_Buffs_Heals_SFX/17_Def_buff_01.wav',.75),('03_Glyph',AURA+'Aura_Glyph_2.wav',.65)]
}
def save(path,y,rate):
    path.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes(np.round(y*32767).astype('<i2').tobytes())

report=[]
for role,items in SOURCES.items():
    sections=[]
    for name,source,duration in items:
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
        y*=.7/np.max(np.abs(y));save(OUT/role/(name+'.wav'),y,rate)
        # Each candidate twice, followed by a distinct longer silent gap.
        sections.extend([y,np.zeros((round(.35*rate),2)),y,np.zeros((round(1.1*rate),2))])
        report.append(dict(role=role,name=name,source=source,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),onset=onset/rate,duration=len(y)/rate,peak=float(np.abs(y).max()),clipped=int(np.count_nonzero(np.abs(y)>=1))))
    save(DEMO/(role+'_123.wav'),np.concatenate(sections),rate)
(ROOT/'docs/development/combat_audio_auditions_20261018/processing.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print(json.dumps([dict(role=r,clips=len(v)) for r,v in SOURCES.items()]))
