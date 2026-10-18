"""Create short auditions from acquired packs; leave vendor WAVs unchanged."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/CoreGuardian/Audio/TowerShot'
OUT.mkdir(parents=True, exist_ok=True)
SOURCES = [
    ('01_Earth', 'Assets/Leohpaz/RPG_Essentials_Free/8_Atk_Magic_SFX/30_Earth_02.wav', .70),
    ('02_Fire', 'Assets/Leohpaz/RPG_Essentials_Free/8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav', .60),
    ('03_Aura', 'Assets/AS_Magic_Aura_Construction_Kit_FREE/Sound Kit/Aura_Spawn_1.wav', .75),
]
report = []
for name, source, seconds in SOURCES:
    path = ROOT / source
    with wave.open(str(path), 'rb') as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if width == 3:
        b = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        ints = b[:, 0] | b[:, 1] << 8 | b[:, 2] << 16
        x = ((ints ^ 0x800000) - 0x800000) / 8388608.
    else:
        assert width == 2
        x = np.frombuffer(raw, '<i2').astype(np.float64) / 32768.
    x = x.reshape(-1, channels)
    envelope = np.max(np.abs(x), axis=1)
    onset = max(0, int(np.flatnonzero(envelope > envelope.max() * .04)[0]) - int(.004 * rate))
    y = x[onset:onset + round(seconds * rate)].copy()
    y -= y.mean(axis=0)
    # Preserve the source texture/pitch. Only trim, level match and soften the boundary.
    attack, release = round(.002 * rate), round(.12 * rate)
    y[:attack] *= np.linspace(0, 1, attack)[:, None]
    y[-release:] *= np.linspace(1, 0, release)[:, None] ** 1.5
    y *= .70 / np.max(np.abs(y))
    output = OUT / (name + '.wav')
    pcm = np.round(y * 32767).astype('<i2')
    with wave.open(str(output), 'wb') as w:
        w.setnchannels(channels); w.setsampwidth(2); w.setframerate(rate); w.writeframes(pcm.tobytes())
    report.append(dict(name=name, source=source, source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                       onset_seconds=onset/rate, duration_seconds=len(y)/rate, peak=float(np.max(np.abs(y))),
                       clipped_samples=int(np.count_nonzero(np.abs(y) >= 1)), output=str(output.relative_to(ROOT))))
(ROOT / 'docs/development/magic_cannon_audio_20261018/audio_processing.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
