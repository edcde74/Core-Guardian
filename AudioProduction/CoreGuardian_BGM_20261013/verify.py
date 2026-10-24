"""Read-back validation of deliverable WAV files, independent of renderer."""
from pathlib import Path
import wave, hashlib, json
import numpy as np

root = Path(__file__).resolve().parent
results = []
for path in sorted(root.glob('*.wav')):
    with wave.open(str(path), 'rb') as w:
        assert w.getcomptype() == 'NONE'
        assert (w.getframerate(), w.getsampwidth(), w.getnchannels()) == (48000, 2, 2)
        frames, rate = w.getnframes(), w.getframerate()
        data = np.frombuffer(w.readframes(frames), dtype='<i2').reshape(-1, 2).astype(float)/32768
    score = json.loads(path.with_name(path.stem + '_score.json').read_text(encoding='utf-8'))
    assert frames == score['loop_frames'] == round(16 * (3 if score['meter']=='6/8' else 4) * 60/score['bpm'] * rate)
    assert 30 <= frames/rate <= 60
    assert np.isfinite(data).all()
    peak = float(np.abs(data).max())
    assert 0.03 < peak < .64
    steps = np.diff(data, axis=0)
    seam = float(np.max(np.abs(data[0]-data[-1])))
    p99 = float(np.percentile(np.abs(steps), 99.9))
    assert seam < max(.003, p99 * 1.5), (path.name, seam, p99)
    rms = float(np.sqrt(np.mean(data**2)))
    dc = data.mean(axis=0)
    assert np.max(np.abs(dc)) < 1e-5
    # Measure the boundary with context; do not interpret a sample delta as a listening test.
    a = data[-round(.05*rate):]
    b = data[:round(.05*rate)]
    results.append(dict(file=path.name, seconds=frames/rate, frames=frames,
                        sample_rate=rate, channels=2, bit_depth=16,
                        peak_dbfs=20*np.log10(peak), rms_dbfs=20*np.log10(rms),
                        clipping_samples=int(np.sum(np.abs(data)>=32767/32768)),
                        dc_offset=dc.tolist(), seam_max_step=seam,
                        seam_max_step_dbfs=20*np.log10(max(seam,1e-12)),
                        internal_step_p999=p99,
                        boundary_50ms_rms_before=float(np.sqrt(np.mean(a*a))),
                        boundary_50ms_rms_after=float(np.sqrt(np.mean(b*b))),
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest(), passed=True))
assert len(results)==3
(root/'verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
for r in results:
    print(f"{r['file']}: {r['seconds']:.3f}s; peak {r['peak_dbfs']:.2f}dBFS; RMS {r['rms_dbfs']:.2f}dBFS; seam {r['seam_max_step_dbfs']:.2f}dBFS; PASS")
