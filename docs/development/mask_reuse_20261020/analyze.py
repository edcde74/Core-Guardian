import json, pathlib, statistics, math
root=pathlib.Path(__file__).parent
def stat(xs):
 s=sorted(xs)
 if not s:return None
 def p(q):
  i=(len(s)-1)*q;a=int(i);b=min(a+1,len(s)-1);return s[a]+(s[b]-s[a])*(i-a)
 return dict(n=len(s),median=statistics.median(s),mean=statistics.mean(s),min=min(s),max=max(s),p95=p(.95))
result=[]
for side in ['before','after','before_controlled','after_controlled']:
 for f in sorted((root/side).glob('*_frames.json')):
  frames=json.loads(f.read_text(encoding='utf-8-sig'));raw=json.loads(f.with_name(f.name.replace('_frames','_markers')).read_text())
  main=[r for r in raw if r['threadName']=='Main Thread' and 'PlayerLoop' in r['costs']]
  names=set().union(*(r['costs'] for r in main))
  markers={n:{k:stat([r['costs'].get(n,{}).get(k,0) for r in main]) for k in ['totalMs','calls','gcBytes']} for n in names if any(s in n for s in ['CoreGuardian','GC.Alloc','GC.Collect','RenderTexture.Create','RenderTexture.Release','PlayerLoop','Gfx.Wait'])}
  dt=[x['deltaMs'] for x in frames];valid=[x for x in dt if math.isfinite(x) and x>0]
  result.append(dict(side=side,label=f.stem,enemies=frames[0]['enemies'],validMainFrames=len(main),fps=dict(min=1000/max(valid),max=1000/min(valid),average=len(valid)*1000/sum(valid),valid=len(valid),invalid=len(dt)-len(valid),frameGaps=sum(max(0,b['engineFrame']-a['engineFrame']-1) for a,b in zip(frames,frames[1:]))),frames={k:stat([x[k] for x in frames]) for k in ['deltaMs','enemies','towers','projectiles','drawCalls','setPass','monoUsed','totalAllocated']},markers=markers))
(root/'comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
for r in result:
 print(r['side'],r['label'],r['fps'], 'delta',round(r['frames']['deltaMs']['median'],3),'GC',round(r['markers']['GC.Alloc']['gcBytes']['median']))
 for key,v in r['markers'].items():
  if 'LateUpdate' in key or 'CoreGuardianPrototype.Update' in key or 'RenderTexture.Create' in key:print(' ',key,round(v['totalMs']['median'],3), 'calls',v['calls']['mean'])
