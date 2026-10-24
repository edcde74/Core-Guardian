import json, statistics, pathlib
root = pathlib.Path(__file__).parent
captures = root / 'normal'
def stats(xs):
    xs = sorted(xs)
    if not xs: return None
    def p(q):
        i=(len(xs)-1)*q; a=int(i); b=min(a+1,len(xs)-1)
        return xs[a]*(b-i)+xs[b]*(i-a) if a!=b else xs[a]
    return dict(n=len(xs),median=statistics.median(xs),mean=statistics.mean(xs),p95=p(.95),max=max(xs),min=min(xs))
result=[]
for f in sorted(list(captures.glob('*_markers.json'))+list((root/'deep').glob('*_markers.json'))):
    data=json.loads(f.read_text(encoding='utf-8-sig'))
    main=[r for r in data if r['threadName']=='Main Thread' and 'PlayerLoop' in r['costs']]
    if not main: continue
    names=set().union(*(r['costs'] for r in main))
    markers={name:{k:stats([r['costs'].get(name,{}).get(k,0) for r in main]) for k in ['totalMs','selfMs','calls','gcBytes']} for name in names}
    framefile=f.with_name(f.name.replace('_markers.json','_frames.json'))
    frames=json.loads(framefile.read_text(encoding='utf-8-sig'))
    run=dict(label=f.stem.replace('_markers',''),validMainFrames=len(main),capturedThreads=sorted(set(r['threadName'] for r in data)),frameStats={k:stats([x[k] for x in frames]) for k in ['deltaMs','enemies','towers','projectiles','drawCalls','setPass','monoUsed','totalAllocated']},markers=markers)
    result.append(run)
(root/'marker_summary.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
for r in result:
    print(r['label'], 'valid frames',r['validMainFrames'], 'frame median',round(r['frameStats']['deltaMs']['median'],3),'enemies',r['frameStats']['enemies']['median'],'draws',r['frameStats']['drawCalls']['median'])
    selected=[(n,v) for n,v in r['markers'].items() if 'CoreGuardian' in n or 'Canvas' in n or 'GC.Alloc'==n or 'RenderTexture' in n or n in ['PlayerLoop','EditorLoop','RenderLoop.Draw','Camera.Render','Gfx.WaitForPresentOnGfxThread','Selected unit sprite union']]
    for n,v in sorted(selected,key=lambda p:p[1]['totalMs']['mean'],reverse=True)[:20]:
        print(' ',n,'mean ms',round(v['totalMs']['mean'],4),'median ms',round(v['totalMs']['median'],4),'p95',round(v['totalMs']['p95'],4),'calls mean',round(v['calls']['mean'],1),'direct alloc mean B',round(v['gcBytes']['mean']))
