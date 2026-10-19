from pathlib import Path
import sys,json,hashlib,urllib.request,urllib.error
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln
from scipy.signal import resample_poly
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
cat=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'));prod=json.loads((HERE/'production.json').read_text(encoding='utf-8'))
assert len(cat['groups'])==2 and len(cat['retained'])==21 and len(cat['tier_sets'])==3
urls=set(cat['flows']);hashes=set();metrics={};meter=pyln.Meter(44100)
for g in cat['groups']:
 assert len(g['clips'])==3
 for c in g['clips']:
  p=ROOT/c['source_wav'];assert sha(p)==c['sha256'];hashes.add(sha(p));x,r=sf.read(p,always_2d=True);tp=float(np.max(abs(resample_poly(x,4,1,axis=0))));lu=float(meter.integrated_loudness(x))
  assert r==44100 and x.shape[1]==2 and np.isfinite(x).all() and tp<.9 and abs(lu-prod['reference_target_lufs'])<.1
  urls.update([c['url'],c['repeat_url']]);metrics[p.name]={'duration':len(x)/r,'lufs':lu,'estimated_dbtp':float(20*np.log10(tp))}
assert len(hashes)==6
for c in cat['retained']+cat['tentative']:
 assert sha(HERE/c['url'])==c['sha256'];urls.add(c['url'])
silver_c=next(c for c in cat['retained'] if c['id']=='aug_silver');silver,r=sf.read(HERE/silver_c['url'],always_2d=True);silver=np.repeat(silver,2,axis=1)
for i,flow in enumerate(cat['tier_sets']):
 x,r=sf.read(HERE/flow['dry'],always_2d=True);assert np.array_equal(x[:len(silver)],silver)
 for j,g in enumerate(cat['groups'],1):
  z,r=sf.read(ROOT/g['clips'][i]['source_wav'],always_2d=True);start=round(flow['starts'][j]*r);assert np.array_equal(x[start:start+len(z)],z)
for path,h in {**prod['source_hashes'],**prod['protected_hashes']}.items():assert sha(ROOT/path)==h
for path in sorted(urls):
 request=urllib.request.Request('http://127.0.0.1:8885/'+path,headers={'Range':'bytes=0-63'})
 with urllib.request.urlopen(request,timeout=10) as response:assert response.status==206 and response.read()==(HERE/path).read_bytes()[:64]
 x,r=sf.read(HERE/path);assert np.isfinite(x).all() and np.max(abs(x))<1
for path in ['/.git/config','/production.json','/build_audio.py']:
 try:urllib.request.urlopen('http://127.0.0.1:8885'+path,timeout=5);raise AssertionError(path)
 except urllib.error.HTTPError as e:assert e.code==404
report={'groups':2,'new_cues':6,'unique_hashes':len(hashes),'retained':21,'tentative':2,'tier_chains':3,'bgm_chains':3,'http_media_checks':len(urls),'protected_hashes':len(prod['protected_hashes']),'sources':len(prod['source_hashes']),'unchanged_silver_in_each_chain':True,'metrics':metrics,'note':'File, level and HTTP checks. No subjective listening, user approval or Unity QA.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n');print(json.dumps({k:v for k,v in report.items() if k!='metrics'},indent=2))
