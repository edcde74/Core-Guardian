import json,statistics,math,pathlib
r=pathlib.Path(__file__).parent
names=['Standard Draw Calls Count','SRP Batcher Draw Calls Count','SetPass Calls Count','Main Thread','CG.Emphasis.Present','CG.Emphasis.Tint','CG.Mask.Present','CG.SpriteReferences.Read','CG.PhotoShadow.Present','CG.PhotoShadow.Cast','GC Allocated In Frame']
def stats(xs):
    return {'n':len(xs),'min':min(xs),'max':max(xs),'mean':statistics.mean(xs),'median':statistics.median(xs),'p95NearestRank':sorted(xs)[math.ceil(.95*len(xs))-1],'p99':None} if xs else None
out=[]
for label in ['baseline','candidate']:
    p=r/(label+'.json')
    if not p.exists():continue
    f=json.loads(p.read_text(encoding='utf-8-sig'))['frames']
    times=[x['ms'] for x in f if math.isfinite(x['ms']) and x['ms']>0]
    counters={n:stats([x['counters'][n]['value'] for x in f if n in x['counters'] and x['counters'][n]['valid'] and x['counters'][n]['value'] is not None]) for n in names}
    total=[x['counters']['Standard Draw Calls Count']['value']+x['counters']['SRP Batcher Draw Calls Count']['value'] for x in f if all(n in x['counters'] and x['counters'][n]['valid'] for n in names[:2])]
    out.append({'label':label,'samples':len(f),'invalidTimes':len(f)-len(times),'gapCount':sum(max(0,x['gap']-1) for x in f),'frameMs':stats(times),'fps':{'min':1000/max(times),'max':1000/min(times),'avg':1000*len(times)/sum(times)},'enemies':stats([x['enemies'] for x in f]),'towers':stats([x['towers'] for x in f]),'projectiles':stats([x['projectiles'] for x in f]),'sources':stats([x['sources'] for x in f]),'unityStatsDraw':stats([x['draw'] for x in f]),'nativeTotalDraw':stats(total),'counters':counters,'textureBytesMax':max(x['textureBytes'] for x in f),'bufferBytesMax':max(x['bufferBytes'] for x in f)})
result={'runs':out,'limits':'One 90-frame pair in current Editor; fixed dt1/60; real wall frame intervals; diagnostic allocation included; no Deep/raw capture; not target-device certification.'}
if len(out)==2:
    def delta(a,b):return {'before':a,'after':b,'saved':a-b,'savedPercent':100*(a-b)/a if a else None}
    result['comparison']={'nativeDraw':delta(out[0]['nativeTotalDraw']['mean'],out[1]['nativeTotalDraw']['mean']),'unityDraw':delta(out[0]['unityStatsDraw']['mean'],out[1]['unityStatsDraw']['mean']),'frameMs':delta(out[0]['frameMs']['mean'],out[1]['frameMs']['mean']),**{n:delta(out[0]['counters'][n]['mean'],out[1]['counters'][n]['mean']) for n in names if out[0]['counters'][n] and out[1]['counters'][n]}}
(r/'summary.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
for x in out: print(x['label'],'nativeDraw',x['nativeTotalDraw'],'UnityDraw',x['unityStatsDraw']['mean'],'sources',x['sources'],'fps',x['fps'])
if 'comparison' in result:print(json.dumps(result['comparison']))
