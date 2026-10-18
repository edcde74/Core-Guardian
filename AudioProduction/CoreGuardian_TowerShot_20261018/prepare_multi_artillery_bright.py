"""Refine the liked three-barrel rhythm with clearer sample-based metallic attacks."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'AudioProduction/CoreGuardian_TowerShot_20261018/multi_artillery_bright'
OUT.mkdir(parents=True,exist_ok=True)
RATE=44100
SOURCES={
    'magic':'Assets/Resources/CoreGuardian/Audio/TowerShot/03_Aura.wav',
    'mechanism':'Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/39_Block_03.wav',
    'reference':'AudioProduction/CoreGuardian_TowerShot_20261018/multi_artillery/02_ThreeBarrel.wav',
}
def read(path):
    with wave.open(str(ROOT/path),'rb') as w:
        assert w.getframerate()==RATE and w.getnchannels()==2
        width=w.getsampwidth();raw=w.readframes(w.getnframes())
    if width==3:
        b=np.frombuffer(raw,np.uint8).reshape(-1,3).astype(np.int32)
        z=b[:,0]|b[:,1]<<8|b[:,2]<<16;x=((z^0x800000)-0x800000)/8388608.
    else:
        assert width==2;x=np.frombuffer(raw,'<i2').astype(float)/32768.
    return x.reshape(-1,2)
def lowpass(x,cutoff):
    taps=np.arange(129)-64
    fir=2*cutoff/RATE*np.sinc(2*cutoff/RATE*taps)*np.hanning(129);fir/=fir.sum()
    return np.stack([np.convolve(x[:,c],fir,'same') for c in range(2)],axis=1)
def layer(x,pitch,seconds,cutoff,low_cut):
    env=np.abs(x).max(axis=1);onset=max(0,np.flatnonzero(env>env.max()*.04)[0]-int(.003*RATE));x=x[onset:]
    n=round(seconds*RATE);positions=np.arange(n)*pitch
    y=np.stack([np.interp(positions,np.arange(len(x)),x[:,c],left=0,right=0) for c in range(2)],axis=1)
    y=lowpass(y,cutoff);y-=low_cut*lowpass(y,550);y-=y.mean(axis=0)
    attack=round(.0015*RATE);release=min(round(.11*RATE),n//2)
    y[:attack]*=np.linspace(0,1,attack)[:,None];y[-release:]*=np.linspace(1,0,release)[:,None]**1.5
    return y/max(np.abs(y).max(),1e-8)
def add(target,x,delay,gain):
    at=round(delay*RATE);n=min(len(x),len(target)-at);target[at:at+n]+=x[:n]*gain
def save(name,x):
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(np.round(x*32767).astype('<i2').tobytes())
def stats(name,x):
    spectrum=np.abs(np.fft.rfft(x*np.hanning(len(x))[:,None],axis=0))**2
    f=np.fft.rfftfreq(len(x),1/RATE)
    ratio=spectrum[(f>=2000)&(f<6000)].sum()/max(spectrum[(f>=80)&(f<2000)].sum(),1e-12)
    return dict(name=name,seconds=len(x)/RATE,rms=float(np.sqrt(np.mean(x*x))),peak=float(np.abs(x).max()),clipped=int(np.count_nonzero(np.abs(x)>=1)),high_mid_ratio=float(ratio),last_sample=float(np.abs(x[-1]).max()))

magic=read(SOURCES['magic']);mechanism=read(SOURCES['mechanism']);reference=read(SOURCES['reference'])
clips=[('Reference_02',reference)]
for name,magic_cut,mech_cut,mech_pitch,mech_gain,low_cut in [
    ('A_WarmClear',3400,4800,.94,.30,.18),
    ('B_BrighterAttack',4300,6200,1.04,.40,.30),
]:
    x=np.zeros((round(.64*RATE),2))
    for delay,gain in [(0,1),(.085,.82),(.17,.90)]:
        # Same timing and magical body pitch as the liked reference; brighten the material attack.
        add(x,layer(magic,.86,.40,magic_cut,low_cut),delay,gain)
        add(x,layer(mechanism,mech_pitch,.13,mech_cut,.35),delay+.006,mech_gain*gain)
    x*=.12/max(np.sqrt(np.mean(x*x)),1e-8)
    strength=.35 if name=='A_WarmClear' else .60
    x=reference*(1-strength)+x*strength
    x*=.12/max(np.sqrt(np.mean(x*x)),1e-8);x*=min(1,.75/max(np.abs(x).max(),1e-8))
    x[:90]*=np.linspace(0,1,90)[:,None];x[-400:]*=np.linspace(1,0,400)[:,None]
    save(name,x);clips.append((name,x))
comparison=[]
for name,x in clips:
    comparison.extend([x,np.zeros((round(.45*RATE),2)),x,np.zeros((round(1.1*RATE),2))])
save('Reference_A_B',np.concatenate(comparison))
report=dict(source_hashes={k:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for k,p in SOURCES.items()},variants=[stats(n,x) for n,x in clips],timing_seconds=[0,.085,.17],order='reference02 twice, A twice, B twice',listening_approved=False)
(OUT/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print(json.dumps(report['variants']))
