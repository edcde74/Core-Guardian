"""Original Core Guardian sketches. No samples, soundfonts, or model weights.
Run with Python 3.12 + NumPy. Scores and timbres are authored in this file.
"""
from pathlib import Path
import json, math, wave, hashlib, sys
import numpy as np

OUT = Path(__file__).resolve().parent
SR = 48000
TAU = 2 * np.pi
RNG = np.random.default_rng(20261013)

def freq(note):
    return 440 * 2 ** ((note - 69) / 12)

def env(t, hold, attack, release):
    a = np.sin(np.clip(t / attack, 0, 1) * np.pi / 2) ** 2
    r = np.cos(np.clip((t - hold) / release, 0, 1) * np.pi / 2) ** 2
    return a * r

def filtered_noise(n, cutoff):
    noise = RNG.normal(0, 1, n)
    hz = np.fft.rfftfreq(n, 1 / SR)
    noise = np.fft.irfft(np.fft.rfft(noise) / (1 + (hz / cutoff) ** 4), n)
    return noise / max(np.std(noise), 1e-9)

def voice(kind, note, hold):
    release = {'pad': .85, 'horn': .32, 'flute': .23,
               'pluck': .65, 'crystal': 1.15, 'bass': .12}.get(kind, .15)
    t = np.arange(math.ceil((hold + release) * SR)) / SR
    f = freq(note)
    vibrato = .004 * np.sin(TAU * 4.7 * t) * (1 - np.exp(-t * 3))
    phase = TAU * f * t + f * .004 / 4.7 * (1 - np.cos(TAU * 4.7 * t))
    if kind == 'pad':
        y = np.zeros(len(t))
        for detune in (-.0017, .0017):
            for h, amp in [(1, .65), (2, .19), (3, .085), (4, .035)]:
                y += amp * np.sin(TAU * f * h * (1 + detune) * t) / 2
        y *= env(t, hold, .18, release)
    elif kind == 'horn':
        y = sum(amp * np.sin(h * phase) for h, amp in
                [(1, .70), (2, .23), (3, .12), (4, .035)])
        y *= env(t, hold, .075, release) * (.85 + .15 * np.exp(-t * 3))
    elif kind == 'flute':
        y = .85 * np.sin(phase) + .10 * np.sin(2 * phase) + .025 * np.sin(3 * phase)
        y += filtered_noise(len(t), 1700) * .012
        y *= env(t, hold, .045, release)
    elif kind == 'pluck':
        # Decaying harmonic string model, softly rolled off for SFX space.
        y = sum(np.sin(TAU * f * h * t) * np.exp(-t * (2.8 + h * 1.2)) / h ** 1.6
                for h in range(1, 8))
        y *= env(t, hold, .007, release)
    elif kind == 'crystal':
        y = (np.sin(TAU * f * t + 1.2 * np.sin(TAU * f * 2 * t) * np.exp(-t * 7))
             * np.exp(-t * 3.5) + .15 * np.sin(TAU * f * 3 * t) * np.exp(-t * 6))
        y *= env(t, hold, .012, release)
    elif kind == 'bass':
        y = .83 * np.sin(TAU * f * t) + .15 * np.sin(TAU * f * 2 * t)
        y *= env(t, hold, .018, release)
    else:
        if kind == 'tom':
            y = np.sin(TAU * (78 * t + 48 * .035 * (1 - np.exp(-t / .035)))) * np.exp(-t * 13)
            y += filtered_noise(len(t), 900) * np.exp(-t * 32) * .09
        elif kind == 'snare':
            y = filtered_noise(len(t), 2200) * np.exp(-t * 25) * .32
            y += np.sin(TAU * 165 * t) * np.exp(-t * 32) * .18
        elif kind == 'tick':
            y = (np.sin(TAU * 720 * t) + .35 * np.sin(TAU * 1080 * t)) * np.exp(-t * 75)
        else:
            raise ValueError(kind)
        y *= env(t, hold, .003, release)
    return y.astype(np.float32)

class Song:
    def __init__(self, name, title, bpm, beats, key):
        self.name, self.title, self.bpm, self.beats, self.key = name, title, bpm, beats, key
        self.spb = 60 / bpm
        self.length = 16 * beats * self.spb
        self.n = round(self.length * SR)
        self.dry = np.zeros((self.n, 2), np.float32)
        self.send = np.zeros_like(self.dry)
        self.events = []

    def add(self, bar, beat, note, dur, kind, amp, pan=0, wet=.15):
        start = (bar * self.beats + beat) * self.spb
        y = voice(kind, note, dur * self.spb) * amp
        p = np.array([math.cos((pan + 1) * np.pi / 4),
                      math.sin((pan + 1) * np.pi / 4)], np.float32)
        idx = (round(start * SR) + np.arange(len(y))) % self.n
        stereo = y[:, None] * p
        # Circular accumulation also carries note releases across loop end.
        np.add.at(self.dry, idx, stereo)
        np.add.at(self.send, idx, stereo * wet)
        self.events.append(dict(bar=bar+1, beat=beat, midi_note=note,
                                duration_beats=dur, instrument=kind,
                                gain=amp, pan=pan))

    def render(self):
        mix = self.dry.copy()
        # Circular multi-tap room: stationary looping reverb, no tail truncation.
        for delay, gain in [(.071, .23), (.113, .20), (.179, .17), (.263, .13),
                            (.373, .105), (.509, .075), (.683, .05), (.887, .03)]:
            mix += np.roll(self.send[:, ::-1], round(delay * SR), axis=0) * gain
        mix -= mix.mean(axis=0)
        # Gentle periodic low-pass; no state discontinuity at the wrap.
        hz = np.fft.rfftfreq(self.n, 1 / SR)
        spec = np.fft.rfft(mix, axis=0)
        spec *= (1 / np.sqrt(1 + (hz / 5200) ** 8))[:, None]
        mix = np.fft.irfft(spec, n=self.n, axis=0)
        rms = np.sqrt(np.mean(mix ** 2))
        gain = min(10 ** (-22 / 20) / rms, 10 ** (-4 / 20) / np.max(np.abs(mix)))
        mix *= gain
        # TPDF dither before signed 16-bit quantization.
        dither = (RNG.random(mix.shape) - RNG.random(mix.shape)) / 32768
        pcm = np.rint(np.clip(mix + dither, -1, 32767/32768) * 32768).astype('<i2')
        path = OUT / (self.name + '.wav')
        with wave.open(str(path), 'wb') as w:
            w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
        score = dict(title=self.title, bpm=self.bpm, meter='6/8' if self.beats == 3 else '4/4',
                     key=self.key, bars=16, loop_frames=self.n, sample_rate=SR, events=self.events)
        (OUT / (self.name + '_score.json')).write_text(json.dumps(score, ensure_ascii=False, indent=2), encoding='utf-8')
        return path

def fortress():
    s = Song('01_Fortress_March', '성채의 행진', 88, 4, 'D Dorian')
    chords = [[50,57,60,64], [48,55,60,64], [43,55,59,62], [50,57,60,64],
              [53,57,60,64], [48,55,60,64], [43,55,59,62], [45,57,61,64]] * 2
    motifs = [ [(0,62,1.25),(1.5,65,.75),(2.5,69,1)],
               [(0,67,1.5),(2,64,1.25)],
               [(0,62,.75),(1,64,.75),(2,67,1.5)],
               [(0,65,1.5),(2,64,.75),(3,62,.65)] ]
    for b,c in enumerate(chords):
        for j,n in enumerate(c[1:]): s.add(b,0,n,3.7,'pad',.072,(-.45,.35,.05)[j],.25)
        for beat in (0,2): s.add(b,beat,c[0]-12,1.45,'bass',.17)
        for beat in (0,1.5,2.5): s.add(b,beat,0,.12,'tom',.105 if beat==0 else .058, -.08,.12)
        for beat in (1,3): s.add(b,beat,0,.1,'snare',.042,.10,.09)
        for j,n in enumerate([c[1],c[2],c[3],c[2]]): s.add(b,j+.25,n, .6,'pluck',.048, -.35,.24)
        if b%8 not in (4,7):
            for beat,n,d in motifs[b%4]:
                s.add(b,beat,n,d,'horn' if b<8 else 'flute',.105 if b<8 else .095,.12,.25)
        if b in (6,14): s.add(b,3,69,.5,'crystal',.038,.4,.3)
    return s

def crystal():
    s = Song('02_Crystal_Engine', '수정 장치의 맥동', 104, 4, 'E minor / suspended harmony')
    chords = [[40,59,64,66],[36,55,62,64],[43,55,59,62],[38,57,62,64]] * 4
    hooks = [[(0,71,1),(1.5,69,.5),(2.5,66,1)],[(.5,67,1.5),(2.5,64,1)],
             [(0,66,.75),(1,67,.75),(2.5,71,1)],[(0,69,1.5),(2,66,1.25)]]
    for b,c in enumerate(chords):
        for j,n in enumerate(c[1:]): s.add(b,0,n,3.65,'pad',.065,(j-1)*.35,.26)
        for beat in (0,1.5,2.5): s.add(b,beat,c[0],.65,'bass',.135)
        arp = [c[1],c[2],c[3],c[2],c[1],c[3],c[2],c[3]]
        for j,n in enumerate(arp):
            s.add(b,j*.5,n,.25,'pluck',.06 if j%2==0 else .035, -.30 if j%2==0 else .30,.25)
        for beat in (0,2): s.add(b,beat,0,.12,'tom',.085,0,.1)
        for beat in (.75,1.75,2.75,3.75): s.add(b,beat,0,.08,'tick',.026,.28,.16)
        if b%8 in (0,1,2,5,6):
            for beat,n,d in hooks[b%4]: s.add(b,beat,n,d,'crystal',.063,.12,.38)
        if b%8 in (3,7): s.add(b,.5,66,2.1,'flute',.06,-.10,.28)
    return s

def siege():
    s = Song('03_Watchline_Embers', '방어선의 긴장', 84, 3, 'C minor, 6/8')
    chords = [[36,55,60,63],[36,55,58,62],[44,56,60,63],[43,55,58,62],
              [41,53,60,65],[44,56,60,63],[43,55,58,62],[43,55,59,62]] * 2
    hooks = [[(0,67,1.8),(2,63,.55)],[(0,62,1.7)],
             [(.5,63,.8),(1.5,65,.9)],[(0,62,1.8),(2,60,.55)]]
    for b,c in enumerate(chords):
        for j,n in enumerate(c[1:]): s.add(b,0,n,2.7,'pad',.07,(j-1)*.38,.30)
        for beat in (0,1.5):
            s.add(b,beat,c[0],1.1,'bass',.15)
            s.add(b,beat,0,.15,'tom',.115 if beat==0 else .075,-.1,.2)
        for j in (0,2,3,5): s.add(b,j*.5,c[1] if j%2==0 else c[2],.4,'pluck',.047,.27,.22)
        s.add(b,2.5,0,.1,'snare',.033,.15,.15)
        if b%8 in (0,2,3,5,6):
            for beat,n,d in hooks[b%4]: s.add(b,beat,n,d,'horn',.088,.06,.3)
        if b in (1,9,13): s.add(b,.5,72,1.35,'flute',.065,-.22,.3)
    return s

if __name__ == '__main__':
    for song in (fortress(), crystal(), siege()):
        p = song.render()
        print(f'{p.name}: {song.n / SR:.6f}s, {len(song.events)} events', flush=True)
