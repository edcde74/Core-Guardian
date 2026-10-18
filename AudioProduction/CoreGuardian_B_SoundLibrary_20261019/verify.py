"""Read-only audio integrity and local HTTP verification for the listening catalogue."""
from pathlib import Path
import sys,json,hashlib,urllib.request,urllib.error
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'AudioProduction/AudioTools_20261019/.local/packages'))
import numpy as np,soundfile as sf,pyloudnorm as pyln
from scipy.signal import resample_poly
cat=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'));prod=json.loads((HERE/'production.json').read_text(encoding='utf-8'))
urls=set();hashes=set();groups=cat['groups']
assert len(groups)==24
for g in groups:
 assert len(g['clips'])==3
 for c in g['clips']:
  p=ROOT/c['source_wav'];assert hashlib.sha256(p.read_bytes()).hexdigest()==c['sha256'];hashes.add(c['sha256'])
  x,r=sf.read(p);assert r==44100 and np.isfinite(x).all() and max(abs(x))<.99
  urls.add(c['url'])
  if c.get('repeat_url'):urls.add(c['repeat_url'])
  if p.suffix=='.ogg':assert len(x)>0
 assert len({c['sha256'] for c in g['clips']})==3
for p,h in {**prod['source_hashes'],**prod['protected_hashes']}.items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h
for url in sorted(urls):
 req=urllib.request.Request('http://127.0.0.1:8882/'+url,headers={'Range':'bytes=0-63'})
 with urllib.request.urlopen(req,timeout=15) as response:
  assert response.status==206 and response.read()==(HERE/url).read_bytes()[:64],url
for path in ['/.git/config','/production.json','/../../Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity']:
 try:urllib.request.urlopen('http://127.0.0.1:8882'+path,timeout=5);raise AssertionError(path)
 except urllib.error.HTTPError as e:assert e.code==404
bgm={}
for c in groups[-1]['clips']:
 x,r=sf.read(HERE/c['url']);peak=float(max(abs(resample_poly(x,4,1))));assert peak<1
 bgm[c['number']]={'seconds':len(x)/r,'lufs':float(pyln.Meter(r).integrated_loudness(x)),'peak_dbtp':float(20*np.log10(peak))}
report={'groups':24,'candidates':72,'unique_candidate_hashes':len(hashes),'http_range_verified':len(urls),'blocked_unlisted_urls':3,'source_hashes_unchanged':len(prod['source_hashes']),'protected_hashes_unchanged':len(prod['protected_hashes']),'bgm_preview':bgm,'note':'Offline files and HTTP checks only. No subjective listening or Unity verification.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n');print(json.dumps(report,indent=2))
