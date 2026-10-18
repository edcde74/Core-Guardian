"""Sample-based magic artillery auditions, outside Unity pending listening feedback."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'AudioProduction/CoreGuardian_TowerShot_20261018/multi_artillery'
OUT.mkdir(parents=True,exist_ok=True)
RATE=44100
sources={
    'magic':'Assets/Resources/CoreGuardian/Audio/TowerShot/03_Aura.wav',
    'mechanism':'Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/39_Block_03.wav',
}
def read(relative):
    with wave.open(str(ROOT/relative),'rb') as w:
        assert w.getframerate()==RATE and w.getnchannels()==2
        width=w.getsampwidth();raw=w.readframes(w.getnframes())
    if width==3:
        b=np.frombuffer(raw,np.uint8).reshape(-1,3).astype(np.int32)
        z=b[:,0]|b[:,1]<<8|b[:,2]<<16;x=((z^0x800000)-0x800000)/8388608.
    else:
        assert width==2;x=np.frombuffer(raw,'<i2').astype(float)/32768.
    x=x.reshape(-1,2);env=np.abs(x).max(axis=1)
    start=max(0,np.flatnonzero(env>env.max()*.04)[0]-int(.003*RATE))
    return x[start:]

def layer(x,pitch,seconds,cutoff):
    n=int(seconds*RATE);positions=np.arange(n)*pitch
    y=np.stack([np.interp(positions,np.arange(len(x)),x[:,c],left=0,right=0) for c in range(2)],axis=1)
    # Windowed-sinc low pass removes long bright ringing without adding a synthetic oscillator.
    taps=np.arange(129)-64
    fir=2*cutoff/RATE*np.sinc(2*cutoff/RATE*taps)*np.hanning(129);fir/=fir.sum()
    y=np.stack([np.convolve(y[:,c],fir,'same') for c in range(2)],axis=1)
    y-=y.mean(axis=0)
    attack=int(.002*RATE);release=min(int(.12*RATE),n//2)
    y[:attack]*=np.linspace(0,1,attack)[:,None];y[-release:]*=np.linspace(1,0,release)[:,None]**1.5
    peak=np.abs(y).max()
    return y/max(peak,1e-8)

def add(target,x,delay,gain):
    at=round(delay*RATE);n=min(len(x),len(target)-at)
    target[at:at+n]+=x[:n]*gain

def save(name,x):
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(np.round(x*32767).astype('<i2').tobytes())

magic=read(sources['magic']);mech=read(sources['mechanism'])
variants=[]
# 1: a heavier single pressure discharge with a subdued mechanical front.
a=np.zeros((round(.72*RATE),2))
add(a,layer(magic,.78,.69,1800),0,1)
add(a,layer(mech,.60,.18,900),.008,.32)
variants.append(('01_HeavyDischarge',a))
# 2: one coherent three-barrel discharge rather than three unrelated spell tails.
b=np.zeros((round(.64*RATE),2))
for delay,gain in [(0,1),(.085,.82),(.17,.90)]:
    add(b,layer(magic,.86,.40,2100),delay,gain)
    add(b,layer(mech,.72,.11,1400),delay+.006,.20*gain)
variants.append(('02_ThreeBarrel',b))
# 3: a clearer dry mechanism stroke and short rear recoil after the magical body.
c=np.zeros((round(.72*RATE),2))
add(c,layer(mech,.72,.18,1700),0,.85)
add(c,layer(magic,.70,.65,1500),.018,.68)
add(c,layer(mech,.53,.10,750),.16,.30)
variants.append(('03_MechanicalRecoil',c))

report=[];comparison=[]
for name,y in variants:
    # Equal RMS to compare texture rather than merely selecting the loudest candidate.
    rms=np.sqrt(np.mean(y*y));y*=.12/max(rms,1e-8)
    y*=min(1,.75/max(np.abs(y).max(),1e-8))
    y[:90]*=np.linspace(0,1,90)[:,None];y[-400:]*=np.linspace(1,0,400)[:,None]
    save(name,y)
    comparison.extend([y,np.zeros((round(.45*RATE),2)),y,np.zeros((round(1.1*RATE),2))])
    pcm=np.round(y*32767).astype('<i2')
    report.append(dict(name=name,seconds=len(y)/RATE,peak=float(np.abs(y).max()),rms=float(np.sqrt(np.mean(y*y))),clipped=int(np.count_nonzero(np.abs(y)>=1)),last_sample=int(pcm[-1].max())))
save('Artillery_123',np.concatenate(comparison))
manifest=dict(sources={k:dict(path=p,sha256=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()) for k,p in sources.items()},variants=report,ordering='1 twice, 2 twice, 3 twice; silence separates groups',listening_approved=False)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8',newline='\n')
print(json.dumps(report))
