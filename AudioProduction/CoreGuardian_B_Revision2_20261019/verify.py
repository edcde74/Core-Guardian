from pathlib import Path
import sys,json,hashlib,urllib.request,urllib.error
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import soundfile as sf,numpy as np
from scipy.signal import resample_poly
cat=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'));prod=json.loads((HERE/'production.json').read_text(encoding='utf-8'))
assert len(cat['groups'])==13 and len(cat['retained'])==12
assert 'remove' not in [g['id'] for g in cat['groups']]
urls=set(cat['flows']+cat['multi']['clips']);hashes=set();metrics={}
for g in cat['groups']:
 assert len(g['clips'])==3
 for c in g['clips']:
  p=ROOT/c['source_wav'];h=hashlib.sha256(p.read_bytes()).hexdigest();assert h==c['sha256'];hashes.add(h)
  x,r=sf.read(p);tp=float(max(abs(resample_poly(x,4,1))));assert r==44100 and np.isfinite(x).all() and tp<.9
  urls.add(c['url'])
  if c.get('repeat_url'):urls.add(c['repeat_url'])
  metrics[p.name]={'duration':len(x)/r,'estimated_dbtp':float(20*np.log10(tp))}
for c in cat['retained']:
 p=HERE/c['url'];assert hashlib.sha256(p.read_bytes()).hexdigest()==c['sha256'];urls.add(c['url'])
for path,h in {**prod['source_hashes'],**prod['protected_hashes']}.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==h
for path in sorted(urls):
 req=urllib.request.Request('http://127.0.0.1:8883/'+path,headers={'Range':'bytes=0-63'})
 with urllib.request.urlopen(req,timeout=10) as response:assert response.status==206 and response.read()==(HERE/path).read_bytes()[:64]
 x,r=sf.read(HERE/path);assert np.isfinite(x).all() and max(abs(x))<1
for path in ['/.git/config','/feedback.json']:
 try:urllib.request.urlopen('http://127.0.0.1:8883'+path,timeout=5);raise AssertionError(path)
 except urllib.error.HTTPError as e:assert e.code==404
report={'groups':13,'candidates':39,'unique_hashes':len(hashes),'retained':12,'http_media_checks':len(urls),'protected_hashes':len(prod['protected_hashes']),'sources':len(prod['source_hashes']),'metrics':metrics,'note':'File/HTTP checks only. No subjective direct listening or Unity tests.'}
assert len(hashes)==39
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n');print(json.dumps({k:v for k,v in report.items() if k!='metrics'},indent=2))
