"""Original procedural audition assets; no external samples or store audio inputs."""
from pathlib import Path
import wave, json, hashlib
import numpy as np

ROOT = Path(__file__).resolve().parent
SR = 48000
N = int(.42 * SR)
T = np.arange(N) / SR
TAU = 2 * np.pi

def shaped_noise(rng, low, high):
    raw = rng.normal(0, 1, N)
    freq = np.fft.rfftfreq(N, 1 / SR)
    shape = (1 - np.exp(-(freq / low)**4)) * np.exp(-(freq / high)**4)
    x = np.fft.irfft(np.fft.rfft(raw) * shape, n=N)
    return x / max(np.std(x), 1e-9)

def decay(tau, attack=.0008, delay=0):
    u = np.maximum(T - delay, 0)
    return (T >= delay) * (1 - np.exp(-u / attack)) * np.exp(-u / tau)

def modes(freq, tau, rng):
    ratios = [1, 1.47, 2.13, 3.19, 4.57]
    x = np.zeros(N)
    for i, ratio in enumerate(ratios):
        x += np.sin(TAU * freq * ratio * T + rng.uniform(-.2, .2)) * decay(tau / (1 + i * .35)) / (1 + i)**1.4
    return x

def shot(kind, variant=0):
    rng = np.random.default_rng(20261018 + kind * 100 + variant)
    tune = 1 + (variant - 1) * .017
    # Shared mechanical vocabulary: short pressure body, a latch, and a quiet return.
    thump = np.sin(TAU * (95 * T + 100 * .014 * (1 - np.exp(-T/.014)))) * decay(.025)
    pressure = shaped_noise(rng, 120, 2100) * decay(.014)
    latch = modes(910*tune, .014, rng) * .19
    return_tick = shaped_noise(rng, 1000, 4800) * decay(.006, delay=.075) * .055
    mechanical = .85*thump + .34*pressure + latch + return_tick
    # Brief FM discharge with a fast fall, avoiding a long musical bell or laser whistle.
    phase = TAU * (240*T + 440*.020*(1-np.exp(-T/.020))) * tune
    energy = np.sin(phase + 1.6*np.sin(phase*1.73)*np.exp(-T/.027)) * decay(.037)
    crystal = modes(1380*tune, .041, rng)
    air = shaped_noise(rng, 650, 5500) * decay(.018, .0015)
    if kind == 0:
        x = mechanical + .12*modes(430*tune, .028, rng)
    elif kind == 1:
        x = .79*mechanical + .33*energy + .10*crystal + .055*air
    else:
        x = .28*mechanical + .64*energy + .15*crystal + .16*air
    x = np.tanh(x*1.12)
    x -= np.mean(x)
    # Shared gentle high-frequency rolloff.
    f = np.fft.rfftfreq(N, 1/SR)
    x = np.fft.irfft(np.fft.rfft(x) / np.sqrt(1+(f/6200)**6), n=N)
    x[:96] *= np.sin(np.linspace(0, np.pi/2, 96))**2
    x[-960:] *= np.cos(np.linspace(0, np.pi/2, 960))**2
    # Equal shot energy, measured over equal duration. This is not perceptual LUFS matching.
    x *= 10**(-23/20) / np.sqrt(np.mean(x*x))
    assert np.max(np.abs(x)) < .92, 'Unexpected peak; revise synthesis before export'
    return x

def write(name, data):
    data = np.asarray(data)
    assert np.all(np.isfinite(data)) and np.max(np.abs(data)) < 1
    pcm = np.rint(data*32767).astype('<i2')
    with wave.open(str(ROOT / name), 'wb') as out:
        out.setparams((1, 2, SR, 0, 'NONE', 'not compressed'))
        out.writeframes(pcm.tobytes())

def put(dst, src, seconds):
    a = round(seconds*SR)
    dst[a:a+len(src)] += src

def main():
    names = ['01_Mechanical', '02_Metal_Crystal', '03_Energy']
    comparison = np.zeros(int(30*SR))
    for kind, name in enumerate(names):
        shots = [shot(kind,v) for v in range(3)]
        for v, x in enumerate(shots):
            write(f'{name}_shot_{v+1:02}.wav', x)
        # 0.4/1.4/2.4 seconds: three isolated variants; 4 seconds onward: six at 1.2/s.
        preview = np.zeros(int(9*SR))
        for v,x in enumerate(shots): put(preview,x,.4+v)
        for i in range(6): put(preview,shots[i%3],4+i/1.2)
        write(f'{name}_preview.wav',preview)
        put(comparison,preview,kind*10)
    write('00_ABC_comparison.wav',comparison)
    metadata = {'sample_rate':SR,'channels':1,'bit_depth':16,'shot_seconds':.42,
                'shot_rms_dbfs':-23,'preview_seconds':9,'repetition_rate_per_second':1.2,
                'note':'Synthetic audition only, no Unity playback or human listening verification.',
                'sources':'Original oscillators and seeded noise; no samples, soundfonts, AI audio model, or store media.'}
    (ROOT/'generation.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')
    print('Generated 9 one-shots, 3 previews and 1 comparison WAV.')

if __name__ == '__main__': main()
