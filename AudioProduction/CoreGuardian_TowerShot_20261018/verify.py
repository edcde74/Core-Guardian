"""Read exported PCM independently; do not infer listening quality from file checks."""
from pathlib import Path
import wave, json, hashlib, math
import numpy as np

root=Path(__file__).resolve().parent
rows=[]
for path in sorted(root.glob('*.wav')):
    with wave.open(str(path),'rb') as w:
        rate=w.getframerate(); channels=w.getnchannels(); bits=w.getsampwidth()*8
        x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
    peak=float(np.max(np.abs(x))); rms=float(np.sqrt(np.mean(x*x)))
    row={'file':path.name,'rate':rate,'channels':channels,'bits':bits,'seconds':len(x)/rate,
         'peak_dbfs':20*math.log10(max(peak,1e-12)), 'rms_dbfs':20*math.log10(max(rms,1e-12)),
         'clipped_samples':int(np.sum(np.abs(x)>=32767/32768)),
         'first_sample':float(x[0]),'last_sample':float(x[-1]),'dc_mean':float(np.mean(x)),
         'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    assert rate==48000 and channels==1 and bits==16
    assert row['clipped_samples']==0 and peak>.1
    assert abs(x[0])<=1/32768 and abs(x[-1])<=1/32768
    assert abs(row['dc_mean'])<.001
    if '_shot_' in path.name:
        assert len(x)==20160 and abs(row['rms_dbfs']+23)<.02
    rows.append(row)
assert len(rows)==13
(root/'verification.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
print(json.dumps({'wav_files':len(rows),'clips':sum(r['clipped_samples'] for r in rows),
                  'format':'48000Hz / 16-bit / mono',
                  'shot_peak_dbfs':[round(r['peak_dbfs'],2) for r in rows if '_shot_' in r['file']],
                  'result':'File format, clip, boundary, DC and equal-energy checks passed; listening not verified.'}))
