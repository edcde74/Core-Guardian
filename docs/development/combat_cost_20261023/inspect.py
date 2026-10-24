import json,pathlib,statistics
r=pathlib.Path('docs/development/combat_cost_20261023')
s=json.loads((r/'marker_summary.json').read_text())[0]
for n,v in s['markers'].items():
    if n.startswith('CG.') or n in ['Main Thread','Gfx.WaitForGfxCommandsFromMainThread','Gfx.ProcessCommands','Gfx.PresentFrame','RenderLoop.Draw','ScriptableRenderContext.Submit','Camera.Render','Gfx.WaitForPresentOnGfxThread']:
        print(n,round(v['totalMs']['mean'],4),round(v['selfMs']['mean'],4),round(v['calls']['mean'],2))
print('threads',s['capturedThreads'])
raw=json.loads((r/'normal/normal_0_n128_markers.json').read_text())
for t in s['capturedThreads']:
    if t=='Main Thread':continue
    a=[x for x in raw if x['threadName']==t]
    names=set().union(*(x['costs'] for x in a))
    top=sorted([(n,sum(x['costs'].get(n,{}).get('totalMs',0) for x in a)/len(a)) for n in names],key=lambda x:-x[1])[:8]
    print(t,len(a),top)
