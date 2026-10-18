from pathlib import Path
import sys,json,hashlib,urllib.request,urllib.error
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import soundfile as sf,numpy as np
from scipy.signal import resample_poly
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
cat=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'));prod=json.loads((HERE/'production.json').read_text(encoding='utf-8'));feedback=json.loads((HERE/'feedback.json').read_text(encoding='utf-8'))
assert len(cat['groups'])==3 and len(cat['retained'])==21 and len(cat['tentative'])==2
assert set(feedback['rejected'])=={'aug_gold','aug_platinum'} and not feedback['game_application_authorized']
urls=set();hashes=set();metrics={}
for g in cat['groups']:
 assert len(g['clips'])==3
 for c in g['clips']:
  p=ROOT/c['source_wav'];assert sha(p)==c['sha256'];hashes.add(sha(p));x,r=sf.read(p);peak=float(np.max(abs(resample_poly(x,4,1))))
  assert r==44100 and np.isfinite(x).all() and peak<.9
  urls.update([c['url'],c['repeat_url']]);metrics[p.name]={'seconds':len(x)/r,'estimated_dbtp':float(20*np.log10(peak))}
assert len(hashes)==9
for c in cat['retained']+cat['tentative']+cat['extras']:
 assert sha(HERE/c['url'])==c['sha256'];urls.add(c['url'])
for path,h in {**prod['protected_hashes'],**prod['source_hashes']}.items():assert sha(ROOT/path)==h
for path in sorted(urls):
 request=urllib.request.Request('http://127.0.0.1:8884/'+path,headers={'Range':'bytes=0-63'})
 with urllib.request.urlopen(request,timeout=10) as response:assert response.status==206 and response.read()==(HERE/path).read_bytes()[:64]
 x,r=sf.read(HERE/path);assert np.isfinite(x).all() and np.max(abs(x))<1
for path in ['/.git/config','/feedback.json','/build_audio.py']:
 try:urllib.request.urlopen('http://127.0.0.1:8884'+path,timeout=5);raise AssertionError(path)
 except urllib.error.HTTPError as e:assert e.code==404
report={'groups':3,'candidates':9,'unique_hashes':len(hashes),'retained':21,'tentative':2,'current_game_reference':1,'http_media_checks':len(urls),'protected_hashes':len(prod['protected_hashes']),'sources':len(prod['source_hashes']),'metrics':metrics,'note':'File and HTTP checks, not subjective audition or Unity QA.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n');print(json.dumps({k:v for k,v in report.items() if k!='metrics'},indent=2))
